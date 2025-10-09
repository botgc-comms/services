(() => {
    "use strict";

    const OP_COOKIE = "wastage_operator_id";
    const PRODUCTS_URL = "/stocktake/products";
    const SELECT_OP_URL = "/stocktake/select-operator";
    const COMMIT_URL = "/stocktake/commit";

    const PROFILES = {
        "WINES|BOTTLE": [
            { code: "CountInStoreRoom", label: "Count (store)", loc: "Store", step: 1 },
            { code: "CountInFridge", label: "Count (fridge)", loc: "Fridge", step: 1 },
            { code: "OpenBottleWeightGrams", label: "Open bottle weight (g)", loc: "Bar", step: 1 }
        ],
        "MINERALS|BOTTLE": [
            { code: "CountInStoreRoom", label: "Count (store)", loc: "Store", step: 1 },
            { code: "CountInFridge", label: "Count (fridge)", loc: "Fridge", step: 1 }
        ],
        "SNACKS|EACH": [
            { code: "CountInStoreRoom", label: "Count", loc: "Store", step: 1 }
        ],
        "BEER CANS|CAN": [
            { code: "CountInStoreRoom", label: "Count (store)", loc: "Store", step: 1 },
            { code: "CountInFridge", label: "Count (fridge)", loc: "Fridge", step: 1 }
        ]
    };

    function profileFor(division, unit) {
        const key = `${(division || "").toUpperCase()}|${(unit || "").toUpperCase()}`;
        return PROFILES[key] || [{ code: "CountInStoreRoom", label: "Count", loc: "Store", step: 1 }];
    }

    function ready(fn) { if (document.readyState !== "loading") fn(); else document.addEventListener("DOMContentLoaded", fn); }
    function getCookie(n) { const v = ("; " + document.cookie).split("; " + n + "="); if (v.length === 2) return v.pop().split(";").shift(); return null; }
    function slug(s) { return String(s || "").toLowerCase().replace(/\s+/g, "-").replace(/\//g, "-"); }
    function escapeHtml(s) { return String(s ?? "").replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;").replace(/"/g, "&quot;").replace(/'/g, "&#39;"); }

    let plan = [];
    let currentDivision = null;
    const observationsMap = new Map(); // stockItemId -> { stockItemId, name, division, unit, observations: [{code,location,value}] }

    function setOperatorStatus() {
        const id = getCookie(OP_COOKIE);
        const el = document.getElementById("operatorStatus");
        if (!id) {
            el.innerHTML = '<span class="muted">No operator selected.</span>';
            openOperatorDialog();
            return;
        }
        const tile = document.querySelector(`#operatorTiles .tile[data-opid='${id}']`);
        if (!tile) {
            document.cookie = `${OP_COOKIE}=; expires=Thu, 01 Jan 1970 00:00:00 GMT; path=/; SameSite=Lax`;
            el.innerHTML = '<span class="muted">No operator selected.</span>';
            openOperatorDialog();
            return;
        }
        const colour = tile.dataset.color || "#cccccc";
        el.innerHTML = `<div class="tile tile--op" style="background:${colour};"><span class="tile__name">${tile.textContent.trim()}</span></div>`;
        el.querySelector(".tile--op").addEventListener("click", openOperatorDialog);
    }

    function openOperatorDialog() {
        const dlg = document.getElementById("operatorModal");
        dlg.addEventListener("cancel", e => e.preventDefault(), { once: true });
        dlg.showModal();
    }

    async function fetchPlan() {
        const res = await fetch(PRODUCTS_URL, { headers: { "accept": "application/json" } });
        if (!res.ok) return [];
        return await res.json();
    }

    function avgDays(products) {
        if (!products?.length) return 9999;
        let sum = 0;
        for (const p of products) sum += (p.daysSinceLastStockTake ?? 9999);
        return sum / products.length;
    }

    function renderDivisions(list) {
        const host = document.getElementById("divisionTiles");
        host.innerHTML = "";
        const ordered = [...list].map(d => ({ ...d, _avg: avgDays(d.products) })).sort((a, b) => b._avg - a._avg);
        for (const d of ordered) {
            const tile = document.createElement("div");
            tile.className = `tile cat-${slug(d.division)}`;
            tile.dataset.division = d.division;
            tile.innerHTML = `<div class="tile__name">${escapeHtml(d.division)}</div><small class="tile__division">${Math.round(d._avg)} days</small>`;
            tile.addEventListener("click", () => selectDivision(d));
            host.appendChild(tile);
        }
    }

    function selectDivision(d) {
        currentDivision = d;
        document.getElementById("divisionTitle").textContent = d.division;
        document.getElementById("divisionAge").textContent = `${Math.round(avgDays(d.products))} days on average`;
        renderProducts(d.products, d.division);
        document.getElementById("productsSection").hidden = false;
    }

    function isDone(stockItemId) {
        return observationsMap.has(stockItemId);
    }

    function renderProducts(products, division) {
        const host = document.getElementById("productTiles");
        host.innerHTML = "";
        for (const p of products) {
            const tile = document.createElement("div");
            const done = isDone(p.stockItemId);
            tile.className = `tile cat-${slug(division)} ${done ? "tile--done" : ""}`;
            tile.dataset.stockItemId = p.stockItemId;
            tile.dataset.name = p.name || "";
            tile.dataset.unit = p.unit || "";
            tile.dataset.division = division || "";
            tile.innerHTML = `<div class="tile__name">${escapeHtml(p.name)}</div><small class="tile__division">${escapeHtml(p.unit || "")}</small>`;
            tile.addEventListener("click", () => openObsDialog(p.stockItemId, p.name, division, p.unit));
            host.appendChild(tile);
        }
    }

    function buildFields(container, profile, existing) {
        container.innerHTML = "";
        for (const f of profile) {
            const wrap = document.createElement("div");
            wrap.className = "obs-field";
            const val = existing?.find(o => o.code === f.code)?.value ?? "";
            wrap.innerHTML = `<label>${escapeHtml(f.label)}</label><input type="number" step="${f.step}" min="0" data-code="${f.code}" data-location="${f.loc}" value="${val}">`;
            container.appendChild(wrap);
        }
    }

    function openObsDialog(stockItemId, name, division, unit) {
        const dlg = document.getElementById("obsModal");
        const title = document.getElementById("obsTitle");
        const fields = document.getElementById("obsFields");

        title.textContent = `Record observations: ${name}`;
        const existing = observationsMap.get(stockItemId)?.observations || [];
        const profile = profileFor(division, unit);
        buildFields(fields, profile, existing);

        const onSave = () => {
            const inputs = Array.from(fields.querySelectorAll("input[data-code]"));
            const obs = [];
            for (const i of inputs) {
                const v = parseFloat(i.value || "0");
                if (v > 0) {
                    obs.push({
                        code: i.getAttribute("data-code"),
                        location: i.getAttribute("data-location"),
                        value: v
                    });
                }
            }
            observationsMap.set(stockItemId, {
                stockItemId: stockItemId,
                name: name,
                division: division,
                unit: unit,
                observations: obs
            });
            upsertObsRow(stockItemId);
            markTileDone(stockItemId);
            document.getElementById("observationsSection").hidden = observationsMap.size === 0;
            dlg.close();
            dlg.removeEventListener("close", onCancel);
            document.getElementById("obsSaveBtn").removeEventListener("click", onSave);
            document.getElementById("obsCancelBtn").removeEventListener("click", onCancel);
        };

        const onCancel = () => {
            dlg.close();
            dlg.removeEventListener("close", onCancel);
            document.getElementById("obsSaveBtn").removeEventListener("click", onSave);
            document.getElementById("obsCancelBtn").removeEventListener("click", onCancel);
        };

        document.getElementById("obsSaveBtn").addEventListener("click", onSave);
        document.getElementById("obsCancelBtn").addEventListener("click", onCancel);
        dlg.addEventListener("close", onCancel, { once: true });
        dlg.showModal();
    }

    function markTileDone(stockItemId) {
        const el = document.querySelector(`.tiles--products .tile[data-stock-item-id='${stockItemId}']`);
        const byData = Array.from(document.querySelectorAll(".tiles--products .tile")).find(t => parseInt(t.dataset.stockItemId, 10) === stockItemId);
        const tile = el || byData;
        if (tile) tile.classList.add("tile--done");
    }

    function obsSummaryText(obsArr) {
        if (!obsArr?.length) return "No values";
        const parts = [];
        for (const o of obsArr) {
            if (o.code === "OpenBottleWeightGrams") parts.push(`${o.value} g open bottle`);
            else if (o.code === "CountInStoreRoom") parts.push(`${o.value} in store`);
            else if (o.code === "CountInFridge") parts.push(`${o.value} in fridge`);
            else parts.push(`${o.code}: ${o.value}`);
        }
        return parts.join("; ");
    }

    function upsertObsRow(stockItemId) {
        const tbody = document.getElementById("obsTbody");
        const data = observationsMap.get(stockItemId);
        if (!data) return;
        let tr = tbody.querySelector(`tr[data-id='${stockItemId}']`);
        const summary = obsSummaryText(data.observations);

        if (!tr) {
            tr = document.createElement("tr");
            tr.dataset.id = String(stockItemId);
            tr.innerHTML =
                `<td class="cell--product">${escapeHtml(data.name)}</td>
         <td class="cell--obs">${escapeHtml(summary)}</td>
         <td>
            <button type="button" class="btn btn-link p-0 js-edit">Edit</button>
            <button type="button" class="btn btn-link text-danger p-0 js-remove">Remove</button>
         </td>`;
            tbody.appendChild(tr);
        } else {
            tr.querySelector(".cell--obs").textContent = summary;
        }

        tr.querySelector(".js-edit").addEventListener("click", () => {
            openObsDialog(data.stockItemId, data.name, data.division, data.unit);
        });

        tr.querySelector(".js-remove").addEventListener("click", () => {
            observationsMap.delete(data.stockItemId);
            tr.remove();
            const t = Array.from(document.querySelectorAll(".tiles--products .tile")).find(x => parseInt(x.dataset.stockItemId, 10) === data.stockItemId);
            if (t) t.classList.remove("tile--done");
            document.getElementById("observationsSection").hidden = observationsMap.size === 0;
        });
    }

    async function commitAll() {
        const opId = getCookie(OP_COOKIE);
        if (!opId) { openOperatorDialog(); return; }

        const flat = [];
        for (const [, v] of observationsMap) {
            for (const o of v.observations) {
                flat.push({
                    stockItemId: v.stockItemId,
                    code: o.code,
                    location: o.location,
                    value: o.value
                });
            }
        }
        if (flat.length === 0) { alert("Add at least one observation."); return; }

        const payload = { timestamp: new Date().toISOString(), observations: flat };
        const btn = document.getElementById("commitBtn");
        btn.disabled = true;
        const old = btn.textContent;
        btn.textContent = "Committing…";

        try {
            const res = await fetch(COMMIT_URL, { method: "POST", headers: { "content-type": "application/json" }, body: JSON.stringify(payload) });
            if (!res.ok) { alert("Failed to commit."); return; }
            alert("Stock take committed.");
            observationsMap.clear();
            document.getElementById("obsTbody").innerHTML = "";
            document.getElementById("observationsSection").hidden = true;
            if (currentDivision) renderProducts(currentDivision.products, currentDivision.division);
        } finally {
            btn.disabled = false;
            btn.textContent = old;
        }
    }

    function bindOperatorSelection() {
        Array.from(document.querySelectorAll("#operatorTiles .tile")).forEach(t => {
            t.addEventListener("click", async () => {
                const id = t.dataset.opid;
                const fd = new FormData();
                fd.append("id", id);
                const res = await fetch(SELECT_OP_URL, { method: "POST", body: fd });
                if (res.ok) {
                    document.getElementById("operatorModal").close();
                    setOperatorStatus();
                }
            });
        });
    }

    ready(async () => {
        bindOperatorSelection();
        setOperatorStatus();

        plan = await fetchPlan();
        renderDivisions(plan);

        document.getElementById("commitBtn").addEventListener("click", commitAll);
    });
})();
