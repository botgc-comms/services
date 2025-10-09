(() => {
    "use strict";

    // ===== Config (15-minute operator session) =====
    const OP_COOKIE = "wastage_operator_id";
    const OP_TS_COOKIE = "wastage_operator_ts";
    const OP_TIMEOUT_MINUTES = 15;

    const PRODUCTS_URL = "/stocktake/products";
    const SELECT_OP_URL = "/stocktake/select-operator";

    // Shared draft endpoints (immediate save; nightly commit handled server-side)
    const DRAFT_GET_URL = (division) => `/stocktake/draft?division=${encodeURIComponent(division)}`;
    const DRAFT_UPSERT_URL = "/stocktake/observe";
    const DRAFT_REMOVE_URL = (stockItemId, division) =>
        `/stocktake/observe/${stockItemId}?division=${encodeURIComponent(division)}`;

    // ===== Profiles & labels =====
    const PROFILES = {
        "WINES|BOTTLE": ["CountInStoreRoom", "CountInFridge", "OpenBottleWeightGrams"],
        "MINERALS|BOTTLE": ["CountInStoreRoom", "CountInFridge"],
        "SNACKS|EACH": ["CountInStoreRoom"],
        "BEER CANS|CAN": ["CountInStoreRoom", "CountInFridge"]
    };

    function isLikelyRedWine(name) {
        const s = (name || "").toLowerCase();
        return /(merlot|shiraz|syrah|malbec|rioja|tempranillo|cabernet|pinot noir|red)/.test(s);
    }

    function fieldLabel(code, productName) {
        if (code === "CountInStoreRoom") return "Count (store)";
        if (code === "CountInFridge") return isLikelyRedWine(productName) ? "Count (back bar)" : "Count (fridge)";
        if (code === "OpenBottleWeightGrams") return "Open bottle weight (g)";
        return code;
    }

    function profileFor(division, unit) {
        const key = `${(division || "").toUpperCase()}|${(unit || "").toUpperCase()}`;
        return PROFILES[key] || ["CountInStoreRoom"];
    }

    // ===== Utilities =====
    function ready(fn) { if (document.readyState !== "loading") fn(); else document.addEventListener("DOMContentLoaded", fn); }
    function getCookie(n) { const v = ("; " + document.cookie).split("; " + n + "="); if (v.length === 2) return v.pop().split(";").shift(); return null; }
    function setCookie(n, val, days) { let exp = ""; if (typeof days === "number") { const d = new Date(); d.setTime(d.getTime() + days * 24 * 60 * 60 * 1000); exp = "; expires=" + d.toUTCString(); } document.cookie = `${n}=${val}; path=/; SameSite=Lax${exp}`; }
    function deleteCookie(n) { document.cookie = `${n}=; expires=Thu, 01 Jan 1970 00:00:00 GMT; path=/; SameSite=Lax`; }
    function nowUtcMs() { return Date.now(); }
    function markOperatorActivity() { setCookie(OP_TS_COOKIE, String(nowUtcMs()), 365); }
    function operatorExpired() { const ts = parseInt(getCookie(OP_TS_COOKIE) || "0", 10); if (!ts) return true; return (nowUtcMs() - ts) > OP_TIMEOUT_MINUTES * 60 * 1000; }
    function escapeHtml(s) { return String(s ?? "").replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;").replace(/"/g, "&quot;").replace(/'/g, "&#39;"); }
    function slug(s) { return String(s || "").toLowerCase().replace(/\s+/g, "-").replace(/\//g, "-"); }
    function fmtTimeISO(iso) { const d = new Date(iso); const hh = d.getHours().toString().padStart(2, "0"); const mm = d.getMinutes().toString().padStart(2, "0"); return `${hh}:${mm}`; }

    // ===== State =====
    let plan = [];
    let currentDivision = null;
    const obsMap = new Map(); // stockItemId -> { stockItemId, name, division, unit, operatorId, operatorName, at, observations[] }

    // ===== SignalR (safe if not present) =====
    let connection = null;
    async function startSignalR() {
        if (!window.signalR || !window.signalR.HubConnectionBuilder) return;
        connection = new signalR.HubConnectionBuilder()
            .withUrl("/hubs/stocktake")
            .withAutomaticReconnect()
            .build();

        connection.on("OperatorSelected", (e) => {
            const colour = e.colorHex || "#cccccc";
            const el = document.getElementById("operatorStatus");
            el.innerHTML = `<div class="tile tile--op" style="background:${colour};"><span class="tile__name">${escapeHtml(e.name)}</span></div>`;
            el.querySelector(".tile--op").addEventListener("click", openOperatorDialog);
            setCookie(OP_COOKIE, e.id, 365);
            markOperatorActivity();
        });

        connection.on("ObservationUpserted", (entry) => {
            if (!currentDivision || entry.division !== currentDivision.division) return;
            obsMap.set(entry.stockItemId, entry);
            upsertObsRow(entry.stockItemId);
            markTileState(entry.stockItemId, entry);
            document.getElementById("observationsSection").hidden = obsMap.size === 0;
        });

        connection.on("ObservationRemoved", ({ stockItemId, division }) => {
            if (!currentDivision || division !== currentDivision.division) return;
            obsMap.delete(stockItemId);
            const tr = document.querySelector(`#obsTbody tr[data-id='${stockItemId}']`);
            if (tr) tr.remove();
            const t = tileFor(stockItemId);
            if (t) t.classList.remove("tile--partial", "tile--complete");
            document.getElementById("observationsSection").hidden = obsMap.size === 0;
        });

        try { await connection.start(); } catch { /* ignore */ }
    }

    // ===== Operator =====
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
            deleteCookie(OP_COOKIE); deleteCookie(OP_TS_COOKIE);
            el.innerHTML = '<span class="muted">No operator selected.</span>';
            openOperatorDialog();
            return;
        }
        const colour = tile.dataset.color || "#cccccc";
        el.innerHTML = `<div class="tile tile--op" style="background:${colour};"><span class="tile__name">${escapeHtml(tile.textContent.trim())}</span></div>`;
        el.querySelector(".tile--op").addEventListener("click", openOperatorDialog);
    }

    function openOperatorDialog() {
        const dlg = document.getElementById("operatorModal");
        dlg.addEventListener("cancel", e => e.preventDefault(), { once: true });
        dlg.showModal();
    }

    function bindOperatorSelection() {
        Array.from(document.querySelectorAll("#operatorTiles .tile")).forEach(t => {
            t.addEventListener("click", async () => {
                const id = t.dataset.opid;
                const fd = new FormData(); fd.append("id", id);
                const res = await fetch(SELECT_OP_URL, { method: "POST", body: fd });
                if (res.ok) {
                    setCookie(OP_COOKIE, id, 365);
                    markOperatorActivity();
                    document.getElementById("operatorModal").close();
                    setOperatorStatus();
                }
            });
        });
    }

    function startOperatorTimeout() {
        const enforce = () => {
            const id = getCookie(OP_COOKIE);
            if (!id) return;
            if (operatorExpired()) {
                deleteCookie(OP_COOKIE); deleteCookie(OP_TS_COOKIE);
                setOperatorStatus();
                const m = document.getElementById("obsModal"); if (m?.open) m.close();
                openOperatorDialog();
            }
        };
        setInterval(enforce, 15000);
        const activity = () => { if (getCookie(OP_COOKIE)) markOperatorActivity(); };
        document.addEventListener("click", activity, { capture: true });
        document.addEventListener("keydown", activity, { capture: true });
        document.addEventListener("pointerdown", activity, { capture: true });
    }

    // ===== Data =====
    async function fetchPlan() {
        const res = await fetch(PRODUCTS_URL, { headers: { "accept": "application/json" } });
        if (!res.ok) return [];
        return await res.json();
    }

    async function loadDraftForDivision(divName) {
        const res = await fetch(DRAFT_GET_URL(divName));
        if (!res.ok) { obsMap.clear(); return; }
        const list = await res.json();
        obsMap.clear();
        for (const e of list) obsMap.set(e.stockItemId, e);
    }

    // ===== UI: Divisions & Products =====
    function avgDays(products) {
        if (!products?.length) return 9999;
        let sum = 0; for (const p of products) sum += (p.daysSinceLastStockTake ?? 9999);
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

    async function selectDivision(d) {
        currentDivision = d;
        document.getElementById("divisionTitle").textContent = d.division;
        document.getElementById("divisionAge").textContent = `${Math.round(avgDays(d.products))} days on average`;

        await loadDraftForDivision(d.division);
        renderProducts();
        renderObsTable();

        document.getElementById("productsSection").hidden = false;
        document.getElementById("observationsSection").hidden = obsMap.size === 0;
    }

    function tileFor(stockItemId) {
        return Array.from(document.querySelectorAll(".tiles--products .tile"))
            .find(t => parseInt(t.dataset.stockItemId, 10) === stockItemId) || null;
    }

    function markTileState(stockItemId, entry) {
        const t = tileFor(stockItemId);
        if (!t) return;
        t.classList.remove("tile--partial", "tile--complete");
        const required = profileFor(entry.division, entry.unit);
        const present = new Set((entry.observations || []).map(o => o.code));
        const nonZeroCount = (entry.observations || []).length;
        if (nonZeroCount === 0) return;
        const allFilled = required.every(code => present.has(code));
        t.classList.add(allFilled ? "tile--complete" : "tile--partial"); // green vs grey
    }

    function renderProducts() {
        const host = document.getElementById("productTiles");
        host.innerHTML = "";
        const division = currentDivision?.division || "";
        const list = currentDivision?.products || [];
        for (const p of list) {
            const entry = obsMap.get(p.stockItemId);
            const tile = document.createElement("div");
            tile.className = `tile cat-${slug(division)}`;
            tile.dataset.stockItemId = p.stockItemId;
            tile.dataset.name = p.name || "";
            tile.dataset.unit = p.unit || "";
            tile.dataset.division = division;
            tile.innerHTML = `<div class="tile__name">${escapeHtml(p.name)}</div><small class="tile__division">${escapeHtml(p.unit || "")}</small>`;
            tile.addEventListener("click", () => openObsDialog(p.stockItemId, p.name, division, p.unit));
            host.appendChild(tile);

            if (entry) markTileState(p.stockItemId, entry);
        }
    }

    // ===== Obs Modal =====
    function buildFields(container, requiredCodes, existing, productName) {
        container.innerHTML = "";
        for (const code of requiredCodes) {
            const wrap = document.createElement("div");
            wrap.className = "obs-field";
            const val = existing?.find(o => o.code === code)?.value ?? "";
            const label = fieldLabel(code, productName);
            const loc = (code === "CountInFridge" ? (isLikelyRedWine(productName) ? "BackBar" : "Fridge")
                : code === "CountInStoreRoom" ? "Store" : "Bar");
            wrap.innerHTML = `<label>${escapeHtml(label)}</label><input type="number" step="1" min="0" data-code="${code}" data-location="${loc}" value="${val}">`;
            container.appendChild(wrap);
        }
    }

    function openObsDialog(stockItemId, name, division, unit) {
        const dlg = document.getElementById("obsModal");
        const title = document.getElementById("obsTitle");
        const fields = document.getElementById("obsFields");

        title.textContent = `Record observations: ${name}`;

        const existing = obsMap.get(stockItemId)?.observations || [];
        const requiredCodes = profileFor(division, unit);
        buildFields(fields, requiredCodes, existing, name);

        const onSave = async () => {
            const inputs = Array.from(fields.querySelectorAll("input[data-code]"));
            const obs = [];
            for (const i of inputs) {
                const v = parseFloat(i.value || "0");
                if (v > 0) {
                    obs.push({
                        stockItemId,
                        code: i.getAttribute("data-code"),
                        location: i.getAttribute("data-location"),
                        value: v
                    });
                }
            }

            const opId = getCookie(OP_COOKIE);
            if (!opId || operatorExpired()) {
                deleteCookie(OP_COOKIE); deleteCookie(OP_TS_COOKIE);
                setOperatorStatus();
                dlg.close();
                openOperatorDialog();
                return;
            }

            const entry = {
                stockItemId,
                name,
                division,
                unit,
                operatorId: opId,
                operatorName: document.querySelector("#operatorStatus .tile--op .tile__name")?.textContent?.trim() || "",
                at: new Date().toISOString(),
                observations: obs
            };

            const res = await fetch(DRAFT_UPSERT_URL, {
                method: "POST",
                headers: { "content-type": "application/json" },
                body: JSON.stringify(entry)
            });
            if (!res.ok) { alert("Failed to save observations."); return; }

            obsMap.set(stockItemId, { ...entry });
            upsertObsRow(stockItemId);
            markTileState(stockItemId, entry);
            document.getElementById("observationsSection").hidden = obsMap.size === 0;

            dlg.close();
        };

        const onCancel = () => dlg.close();

        document.getElementById("obsSaveBtn").onclick = onSave;
        document.getElementById("obsCancelBtn").onclick = onCancel;
        dlg.showModal();
    }

    // ===== Bottom table (Time + Operator + Product + Observations + icons) =====
    function obsSummaryText(obsArr) {
        if (!obsArr?.length) return "No values";
        const parts = [];
        for (const o of obsArr) {
            if (o.code === "OpenBottleWeightGrams") parts.push(`${o.value} g open bottle`);
            else if (o.code === "CountInStoreRoom") parts.push(`${o.value} in store`);
            else if (o.code === "CountInFridge") parts.push(`${o.value} in ${o.location === "BackBar" ? "back bar" : "fridge"}`);
            else parts.push(`${o.code}: ${o.value}`);
        }
        return parts.join("; ");
    }

    function upsertObsRow(stockItemId) {
        const tbody = document.getElementById("obsTbody");
        const data = obsMap.get(stockItemId);
        if (!data) return;

        const time = data.at ? fmtTimeISO(data.at) : "";
        const summary = obsSummaryText(data.observations);

        let tr = tbody.querySelector(`tr[data-id='${stockItemId}']`);
        if (!tr) {
            tr = document.createElement("tr");
            tr.dataset.id = String(stockItemId);
            tr.innerHTML =
                `<td>${escapeHtml(time)}</td>
         <td>${escapeHtml(data.operatorName || "")}</td>
         <td class="cell--product">${escapeHtml(data.name)}</td>
         <td class="cell--obs">${escapeHtml(summary)}</td>
         <td>
            <button type="button" class="btn-icon js-edit" title="Edit"><i class="bi bi-pencil-square"></i></button>
            <button type="button" class="btn-icon text-danger js-remove" title="Remove"><i class="bi bi-trash"></i></button>
         </td>`;
            tbody.appendChild(tr);
        } else {
            tr.children[0].textContent = time;
            tr.children[1].textContent = data.operatorName || "";
            tr.querySelector(".cell--obs").textContent = summary;
        }

        tr.querySelector(".js-edit").onclick = () => {
            openObsDialog(data.stockItemId, data.name, data.division, data.unit);
        };

        tr.querySelector(".js-remove").onclick = async () => {
            const res = await fetch(DRAFT_REMOVE_URL(data.stockItemId, data.division), { method: "DELETE" });
            if (!res.ok) { alert("Failed to remove."); return; }
            obsMap.delete(data.stockItemId);
            tr.remove();
            const tile = tileFor(data.stockItemId);
            if (tile) tile.classList.remove("tile--partial", "tile--complete");
            document.getElementById("observationsSection").hidden = obsMap.size === 0;
        };
    }

    function renderObsTable() {
        const tbody = document.getElementById("obsTbody");
        tbody.innerHTML = "";
        for (const [, v] of obsMap) upsertObsRow(v.stockItemId);
    }

    // ===== Boot =====
    ready(async () => {
        bindOperatorSelection();
        setOperatorStatus();
        startOperatorTimeout();
        await startSignalR();

        plan = await fetchPlan();
        renderDivisions(plan);

        if (getCookie(OP_COOKIE)) markOperatorActivity();
    });
})();
