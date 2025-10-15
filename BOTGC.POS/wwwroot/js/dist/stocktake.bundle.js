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

    let SHOW_ESTIMATED = false;

    // ===== Profiles & labels =====
    // Use token format "Code[@Location]" so we can require per-bar weights.
    const PROFILES = {
        "WINES|BOTTLE": [
            "CountInStoreRoom",
            "CountInLoungeBar",
            "OpenBottleWeightGrams@Lounge",
            "CountInColtBar",
            "OpenBottleWeightGrams@Colt"
        ],
        "MINERALS|BOTTLE": [
            "CountInStoreRoom",
            "CountInLoungeBar",
            "CountInColtBar"
        ],
        "SNACKS|EACH": [
            "CountInStoreRoom",
            "CountInLoungeBar",
            "CountInColtBar"
        ],
        "BEER CANS|CAN": [
            "CountInStoreRoom",
            "CountInLoungeBar",
            "CountInColtBar"
        ],
        "DRAUGHT BEER|PINT": [
            "CountInCellar",
            "KegWeightGrams@Cellar"
        ],
        "PANTRY|EACH": [
            "CountInStoreRoom",
            "CountInKitchen"
        ],
        "PANTRY|BOTTLE": [
            "CountInStoreRoom",
            "CountInKitchen"
        ],
        "PANTRY|*": [
            "CountInStoreRoom",
            "CountInKitchen"
        ],
        "*|*": [
            "CountInStoreRoom",
            "CountInLoungeBar",
            "CountInColtBar"
        ]
    };

    function profileFor(division, unit) {
        const key = `${(division || "").toUpperCase()}|${(unit || "").toUpperCase()}`;
        return PROFILES[key] || PROFILES["*|*"];
    }

    function parseToken(token) {
        const [code, loc] = String(token).split("@");
        return { code, location: loc || null };
    }

    function fieldLabel(token) {
        const { code, location } = parseToken(token);
        if (code === "CountInLoungeBar") return "Count (Lounge bar)";
        if (code === "CountInColtBar") return "Count (Colt bar)";
        if (code === "CountInStoreRoom") return "Count (store room)";
        if (code === "CountInCellar") return "Count (cellar)";
        if (code === "OpenBottleWeightGrams") return location ? `Total weight of open bottles (g, ${location})` : "Total weight of open bottles (g)";
        if (code === "KegWeightGrams") return "Weight of Keg in use (g)";
        if (code === "CountInKitchen") return "Count (kitchen)";

        return code;
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

    function getQueryParam(name) {
        const u = new URL(window.location.href);
        const v = u.searchParams.get(name);
        return v === null ? null : v;
    }

    function parseQueryBool(v) {
        if (v === null) return null;
        if (v === "") return true; // ?showEstimates
        const s = String(v).trim().toLowerCase();
        if (["1", "true", "yes", "on"].includes(s)) return true;
        if (["0", "false", "no", "off"].includes(s)) return false;
        return null;
    }

    async function loadUiConfig() {
        try {
            const res = await fetch("/stocktake/config", { headers: { "accept": "application/json" } });
            if (res.ok) {
                const cfg = await res.json();
                SHOW_ESTIMATED = !!cfg.showEstimatedInDialog;
            }
        } catch { /* ignore */ }
    }

    // Deterministic PRNG (xmur3 + sfc32)
    function xmur3(str) {
        let h = 1779033703 ^ str.length;
        for (let i = 0; i < str.length; i++) {
            h = Math.imul(h ^ str.charCodeAt(i), 3432918353);
            h = (h << 13) | (h >>> 19);
        }
        return function () {
            h = Math.imul(h ^ (h >>> 16), 2246822507);
            h = Math.imul(h ^ (h >>> 13), 3266489909);
            h ^= h >>> 16;
            return h >>> 0;
        };
    }
    function sfc32(a, b, c, d) {
        return function () {
            a >>>= 0; b >>>= 0; c >>>= 0; d >>>= 0;
            let t = (a + b) | 0;
            a = b ^ (b >>> 9);
            b = (c + (c << 3)) | 0;
            c = (c << 21) | (c >>> 11);
            d = (d + 1) | 0;
            t = (t + d) | 0;
            c = (c + t) | 0;
            return (t >>> 0) / 4294967296;
        };
    }
    function seededRng(seedStr) {
        const seed = xmur3(seedStr);
        return sfc32(seed(), seed(), seed(), seed());
    }

    // Fisher–Yates using deterministic RNG
    function shuffleDeterministic(arr, rng) {
        const a = arr.slice();
        for (let i = a.length - 1; i > 0; i--) {
            const j = Math.floor(rng() * (i + 1));
            [a[i], a[j]] = [a[j], a[i]];
        }
        return a;
    }

    // Pick N items deterministically for today
    function pickDivisionsForToday(allDivisions, n) {
        const today = new Date().toISOString().slice(0, 10); // YYYY-MM-DD
        // Seed includes date + a stable string so every device gets the same set
        const seed = `stocktake:${today}`;
        const rng = seededRng(seed);
        const shuffled = shuffleDeterministic(allDivisions, rng);
        return shuffled.slice(0, Math.min(n, shuffled.length));
    }


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

            if ((entry.observations?.length ?? 0) === 0) {
              // treat as removal/clear
              obsMap.delete(entry.stockItemId);
              document.querySelector(`#obsTbody tr[data-id='${entry.stockItemId}']`)?.remove();
              const t = tileFor(entry.stockItemId);
              if (t) {
                t.classList.remove("tile--partial", "tile--complete");
                t.querySelector(".tile__badge")?.remove();
              }
            } else {
              obsMap.set(entry.stockItemId, entry);
              upsertObsRow(entry.stockItemId);
              markTileState(entry.stockItemId, entry);
            }
            document.getElementById("observationsSection").hidden = obsMap.size === 0;
        });

        connection.on("ObservationRemoved", ({ stockItemId, division }) => {
            if (!currentDivision || division !== currentDivision.division) return;
            obsMap.delete(stockItemId);
            const tr = document.querySelector(`#obsTbody tr[data-id='${stockItemId}']`);
            if (tr) tr.remove();
            const t = tileFor(stockItemId);
            if (t) {
                t.classList.remove("tile--partial", "tile--complete");
                t.querySelector(".tile__badge")?.remove();
            }
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
        if (!res.ok) { obsMap.clear(); return []; }

        const raw = await res.json();

        // 🔧 Accept both array and keyed-object forms
        const list = Array.isArray(raw) ? raw : Object.values(raw ?? {});

        obsMap.clear();
        for (const e of list) {
            if ((e.observations?.length ?? e.Observations?.length ?? 0) > 0) {
                obsMap.set(e.stockItemId ?? e.StockItemId, {
                    stockItemId: e.stockItemId ?? e.StockItemId,
                    name: e.name ?? e.Name,
                    division: e.division ?? e.Division,
                    unit: e.unit ?? e.Unit,
                    operatorId: e.operatorId ?? e.OperatorId,
                    operatorName: e.operatorName ?? e.OperatorName,
                    at: e.at ?? e.At,
                    observations: e.observations ?? e.Observations ?? [],
                    estimatedQuantityAtCapture: e.estimatedQuantityAtCapture ?? e.EstimatedQuantityAtCapture ?? null
                });
            }
        }
        return list;
    }

    // ===== UI: Divisions & Products =====
    function avgDays(products) {
        if (!products?.length) return null;
        const vals = products
            .map(p => p.daysSinceLastStockTake)
            .filter(v => Number.isFinite(v));
        if (vals.length === 0) return null;
        const sum = vals.reduce((a, b) => a + b, 0);
        return Math.round(sum / vals.length);
    }

    function renderDivisions(list) {
        const host = document.getElementById("divisionTiles");
        host.innerHTML = "";
        const ordered = [...list].map(d => ({ ...d, _avg: avgDays(d.products) })).sort((a, b) => b._avg - a._avg);
        for (const d of ordered) {
            const tile = document.createElement("div");
            tile.className = `tile cat-${slug(d.division)}`;
            tile.dataset.division = d.division;
            const badge = (d._avg == null) ? "no history" : `${d._avg} days`;
            tile.innerHTML = `<div class="tile__name">${escapeHtml(d.division)}</div><small class="tile__division">${badge}</small>`;
            tile.addEventListener("click", () => selectDivision(d));
            host.appendChild(tile);
        }
    }

    function pluralizeUnit(unitRaw) {
        const u = String(unitRaw || "").trim().toLowerCase();
        if (!u) return "items";
        if (u === "bottle") return "bottles";
        if (u === "can") return "cans";
        if (u === "each") return "items";
        if (u === "pint") return "pints";
        if (u.endsWith("s")) return u; // already plural
        return u + "s";
    }


    async function selectDivision(d) {
        const sheetEntries = await loadDraftForDivision(d.division); // all products in sheet

        let listToRender = [];
        if (sheetEntries.length > 0) {
            listToRender = sheetEntries.map(e => ({
                stockItemId: e.stockItemId ?? e.StockItemId,
                name: e.name ?? e.Name,
                unit: e.unit ?? e.Unit,
                division: d.division,
                currentQuantity: e.estimatedQuantityAtCapture ?? e.EstimatedQuantityAtCapture ?? null
            }));
        } else {
            listToRender = (d.products || []).map(p => ({
                stockItemId: p.stockItemId ?? p.StockItemId,
                name: p.name ?? p.Name,
                unit: p.unit ?? p.Unit,
                division: d.division,
                currentQuantity: p.currentQuantity ?? p.CurrentQuantity ?? null
            }));
        }

        currentDivision = { ...d, products: listToRender };
        document.getElementById("divisionTitle").textContent = d.division;

        renderProducts();
        renderObsTable();

        document.getElementById("productsSection").hidden = false;
        document.getElementById("observationsSection").hidden = obsMap.size === 0;
    }

    function tileFor(stockItemId) {
        return Array.from(document.querySelectorAll(".tiles--products .tile"))
            .find(t => parseInt(t.dataset.stockItemId, 10) === stockItemId) || null;
    }
    function findProductInCurrentDivision(stockItemId) {
        const products = currentDivision?.products || [];
        return products.find(p => p.stockItemId === stockItemId) || null;
    }

    function requiredPairsFor(entry) {
        return profileFor(entry.division, entry.unit).map(t => parseToken(t));
    }

    function presentPairsFrom(entry) {
        const arr = entry.observations || [];
        return new Set(arr.map(o => `${o.code}@${o.location || ""}`));
    }

    function remainingCount(entry) {
        const reqTokens = profileFor(entry.division, entry.unit).map(t => parseToken(t));
        const present = new Set((entry.observations || []).map(o => `${o.code}@${o.location || ""}`));

        let left = 0;
        for (const r of reqTokens) {
            const key = `${r.code}@${r.location || ""}`;
            const isCount = r.code.startsWith("CountIn"); // counts match by code only
            const matched = isCount
                ? Array.from(present).some(k => k.startsWith(`${r.code}@`))
                : present.has(key);
            if (!matched) left++;
        }
        return left;
    }

    function markTileState(stockItemId, entry) {
        const t = tileFor(stockItemId);
        if (!t) return;

        t.classList.remove("tile--partial", "tile--complete");
        t.querySelector(".tile__badge")?.remove();

        const any = (entry.observations || []).length > 0;
        if (!any) return;

        const left = remainingCount(entry);
        if (left === 0) {
            t.classList.add("tile--complete");
        } else {
            t.classList.add("tile--partial");
            const badge = document.createElement("span");
            badge.className = "tile__badge";
            badge.textContent = `${left} left`;
            t.appendChild(badge);
        }
    }

    function renderProducts() {
        const host = document.getElementById("productTiles");
        host.innerHTML = "";
        const division = currentDivision?.division || "";
        const list = currentDivision?.products || [];

        for (const p of list) {
            const entry = obsMap.get(p.stockItemId);
            const tile = document.createElement("div");
            tile.className = "tile";
            tile.dataset.stockItemId = p.stockItemId;
            tile.dataset.name = p.name || "";
            tile.dataset.unit = p.unit || "";
            tile.dataset.division = division;
            // keep the estimate used by the dialog
            tile.dataset.estimate = (p.currentQuantity ?? "");
            tile.innerHTML = `<div class="tile__name">${escapeHtml(p.name)}</div>
                              <small class="tile__division">${escapeHtml(p.unit || "")}</small>`;
            tile.addEventListener("click", () =>
                openObsDialog(p.stockItemId, p.name, division, p.unit)
            );
            host.appendChild(tile);

            if (entry) markTileState(p.stockItemId, entry);
        }
    }

    // ===== Obs Modal =====
    function buildFields(container, requiredTokens, existing, productUnit, estimatedCurrent) {
        container.innerHTML = "";

        // Optional estimated banner
        if (SHOW_ESTIMATED && estimatedCurrent != null && isFinite(estimatedCurrent)) {
            const est = document.createElement("div");
            est.className = "obs-estimate";
            est.textContent = `Current estimated stock: ${estimatedCurrent} ${pluralizeUnit(productUnit)}`;
            container.appendChild(est);
        }

        // progress + hint
        const progress = document.createElement("div");
        progress.className = "obs-progress";
        container.appendChild(progress);

        const hint = document.createElement("div");
        hint.className = "obs-hint";
        hint.textContent = "Blank = not observed. 0 = none found.";
        container.appendChild(hint);

        // groups wrapper
        const groupsHost = document.createElement("div");
        groupsHost.className = "obs-groups";
        container.appendChild(groupsHost);

        // map existing
        const existingMap = new Map();
        (existing || []).forEach(o => {
            const key = `${o.code}@${o.location || ""}`;
            existingMap.set(key, o.value);
        });

        const unitLabel = pluralizeUnit(productUnit);

        function tokenToGroupAndLabel(token, unitLabel) {
            const [code, locRaw] = String(token).split("@");
            const loc = locRaw || null;

            const countLabel = `Number of unopened ${unitLabel}`;

            // For counts we now attach an explicit location string as well
            if (code === "CountInStoreRoom")
                return { group: "Store Room", label: countLabel, code, location: "Store" };
            if (code === "CountInColtBar")
                return { group: "Colt bar", label: countLabel, code, location: "Colt" };
            if (code === "CountInLoungeBar")
                return { group: "Lounge bar", label: countLabel, code, location: "Lounge" };
            if (code === "CountInCellar")
                return { group: "Cellar", label: "Count", code, location: "Cellar" };

            if (code === "OpenBottleWeightGrams" && loc === "Colt")
                return { group: "Colt bar", label: "Total weight of open bottles (g)", code, location: "Colt" };
            if (code === "OpenBottleWeightGrams" && loc === "Lounge")
                return { group: "Lounge bar", label: "Total weight of open bottles (g)", code, location: "Lounge" };
            if (code === "KegWeightGrams" && loc === "Cellar")
                return { group: "Cellar", label: "Weight of Keg being used (g)", code, location: "Cellar" };
            if (code === "CountInKitchen")
                return { group: "Kitchen", label: countLabel, code, location: "Kitchen" };


            return { group: "Other", label: code + (loc ? ` (${loc})` : ""), code, location: loc };
        }

        const order = ["Store Room", "Kitchen", "Colt bar", "Lounge bar", "Cellar", "Other"];

        const groups = new Map(order.map(n => [n, []]));
        for (const token of requiredTokens) {
            const meta = tokenToGroupAndLabel(token, unitLabel);
            groups.get(meta.group).push(meta);
        }

        for (const groupName of order) {
            const fields = groups.get(groupName) || [];
            if (!fields.length) continue;

            const g = document.createElement("section");
            g.className = "obs-group";

            const h = document.createElement("h4");
            h.className = "obs-group__title";
            h.textContent = groupName;
            g.appendChild(h);

            const row = document.createElement("div");
            row.className = "obs-row";
            g.appendChild(row);

            for (const f of fields) {
                const key = `${f.code}@${f.location || ""}`;
                const current = existingMap.has(key) ? existingMap.get(key) : "";

                const wrap = document.createElement("div");
                wrap.className = "obs-field";
                wrap.innerHTML =
                    `<label class="obs-label">${escapeHtml(f.label)}</label>
                    <div class="obs-input-row">
                        <input type="number" step="1" min="0"
                            class="obs-input"
                            data-code="${escapeHtml(f.code)}"
                            ${f.location ? `data-location="${escapeHtml(f.location)}"` : ""}
                            value="${current !== "" ? String(current) : ""}">
                        <button type="button" class="btn-quick-zero" data-for="${escapeHtml(key)}" aria-label="Set to none">None</button>
                    </div>`;

                row.appendChild(wrap);
            }

            groupsHost.appendChild(g);
        }

        // bottom note
        const note = document.createElement("div");
        note.className = "obs-note";
        note.textContent = "You can record some values now and come back later to complete this product. Observations should be completed before the end of the day.";

        // Move note into the dialog footer and group buttons to the right
        const footer = document.querySelector("#obsModal footer.actions");
        footer.classList.add("obs-footer");
        footer.querySelector(".obs-note")?.remove();
        
        // Wrap buttons once so we can align as a group
        let btnWrap = footer.querySelector(".obs-btns");
        if (!btnWrap) {
          const okBtn = document.getElementById("obsSaveBtn");
          const cancelBtn = document.getElementById("obsCancelBtn");
          btnWrap = document.createElement("div");
          btnWrap.className = "obs-btns";
          // append wrapper, then move the existing buttons into it
          footer.appendChild(btnWrap);
          btnWrap.appendChild(okBtn);
          btnWrap.appendChild(cancelBtn);
        }

        // Put the note at the start of the footer
        footer.prepend(note);

        function updateProgress() {
            const inputs = Array.from(container.querySelectorAll("input[data-code]"));
            const required = inputs.length;
            const completed = inputs.filter(i => i.value !== "").length;
            const done = completed === required;
            progress.textContent = `${completed} of ${required} observations recorded${done ? " — Complete" : ""}`;
        }

        container.addEventListener("input", e => {
            if (e.target && e.target.matches("input[data-code]")) updateProgress();
        });

        container.addEventListener("click", e => {
            const btn = e.target.closest(".btn-quick-zero");
            if (!btn) return;
            const key = btn.dataset.for;
            const input = Array.from(container.querySelectorAll("input[data-code]"))
                .find(i => `${i.getAttribute("data-code")}@${i.getAttribute("data-location") || ""}` === key);

            if (input) {
                input.value = "0";
                input.dispatchEvent(new Event("input", { bubbles: true }));
                input.focus();
                input.select();
            }
        });

        updateProgress();
    }

    function openObsDialog(stockItemId, name, division, unit) {
        const dlg = document.getElementById("obsModal");
        const title = document.getElementById("obsTitle");
        const fields = document.getElementById("obsFields");

        title.textContent = `Record observations: ${name}`;

        const product = findProductInCurrentDivision(stockItemId);
        const estimated = product?.currentQuantity ?? null;

        const existing = obsMap.get(stockItemId)?.observations || [];
        const requiredTokens = profileFor(division, unit);
        buildFields(fields, requiredTokens, existing, unit, estimated);

        const onSave = async () => {
            // Build observations: include any input that has a value (including 0). Blank = not observed.
            const inputs = Array.from(fields.querySelectorAll("input[data-code]"));
            const obs = [];
            for (const i of inputs) {
                const raw = i.value;
                if (raw === "") continue; // not observed
                const v = parseFloat(raw);
                if (!isFinite(v) || v < 0) continue;
                obs.push({
                    stockItemId,
                    code: i.getAttribute("data-code"),
                    location: i.getAttribute("data-location"),
                    value: v
                });
            }

            const opId = getCookie(OP_COOKIE);
            if (!opId || operatorExpired()) {
                deleteCookie(OP_COOKIE); deleteCookie(OP_TS_COOKIE);
                setOperatorStatus();
                dlg.close();
                openOperatorDialog();
                return;
            }

            const tileEl = tileFor(stockItemId);
            const estRaw = tileEl?.dataset?.estimate ?? "";
            const estNum = Number(estRaw);
            const estimatedQuantityAtCapture = Number.isFinite(estNum) ? estNum : 0;

            const entry = {
                stockItemId,
                name,
                division,
                unit,
                operatorId: opId,
                operatorName: document.querySelector("#operatorStatus .tile--op .tile__name")?.textContent?.trim() || "",
                at: new Date().toISOString(),
                observations: obs, 
                estimatedQuantityAtCapture
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
    function obsSummaryText(entry) {
        const req = profileFor(entry.division, entry.unit);
        const present = presentPairsFrom(entry);
        const byKey = new Map((entry.observations || []).map(o => [`${o.code}@${o.location || ""}`, o.value]));

        const parts = [];

        function renderPart(token, render) {
            const { code, location } = parseToken(token);
            const key = `${code}@${location || ""}`;
            if (present.has(key)) {
                render(byKey.get(key));
            } else {
                parts.push(`${fieldLabel(token)}: not observed`);
            }
        }

        for (const token of req) {
            const { code, location } = parseToken(token);
            if (code === "CountInLoungeBar") {
                renderPart(token, v => parts.push(`${v} in Lounge bar`));
            } else if (code === "CountInColtBar") {
                renderPart(token, v => parts.push(`${v} in Colt bar`));
            } else if (code === "CountInStoreRoom") {
                renderPart(token, v => parts.push(`${v} in store room`));
            } else if (code === "CountInCellar") {
                renderPart(token, v => parts.push(`${v} in cellar`));
            } else if (code === "OpenBottleWeightGrams") {
                renderPart(token, v => parts.push(`${v} g open bottle (${location})`));
            } else if (code === "KegWeightGrams") {
                renderPart(token, v => parts.push(`${v} g keg weight`));
            }
            else if (code === "CountInKitchen") {
                renderPart(token, v => parts.push(`${v} in kitchen`));
            } else {
                renderPart(token, v => parts.push(`${code}${location ? ` @ ${location}` : ""}: ${v}`));
            }
        }

        return parts.join("; ");
    }

    function upsertObsRow(stockItemId) {
        const tbody = document.getElementById("obsTbody");
        const data = obsMap.get(stockItemId);
        if (!data) return;

        const time = data.at ? fmtTimeISO(data.at) : "";
        const left = remainingCount(data);
        const statusText = left === 0 ? "Complete" : `Incomplete: ${left} remaining`;
        const summary = obsSummaryText(data);
        const annotated = `${statusText} — ${summary}`;

        let tr = tbody.querySelector(`tr[data-id='${stockItemId}']`);
        if (!tr) {
            tr = document.createElement("tr");
            tr.dataset.id = String(stockItemId);
            tr.innerHTML =
                `<td>${escapeHtml(time)}</td>
                 <td>${escapeHtml(data.operatorName || "")}</td>
                 <td class="cell--product">${escapeHtml(data.name)}</td>
                 <td class="cell--obs">${escapeHtml(annotated)}</td>
                 <td>
                    <button type="button" class="btn-icon js-edit" title="Edit"><i class="bi bi-pencil-square"></i></button>
                    <button type="button" class="btn-icon text-danger js-remove" title="Remove"><i class="bi bi-trash"></i></button>
                 </td>`;
            tbody.appendChild(tr);
        } else {
            tr.children[0].textContent = time;
            tr.children[1].textContent = data.operatorName || "";
            tr.querySelector(".cell--obs").textContent = annotated;
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
            if (tile) {
                tile.classList.remove("tile--partial", "tile--complete");
                tile.querySelector(".tile__badge")?.remove();
            }
            document.getElementById("observationsSection").hidden = obsMap.size === 0;
        };
    }

    function renderObsTable() {
        const tbody = document.getElementById("obsTbody");
        tbody.innerHTML = "";
        for (const [, v] of obsMap) upsertObsRow(v.stockItemId);
        }

    function pickDivisionsForTodayWithPinned(allDivisions, n, pinnedName) {
        const today = new Date().toISOString().slice(0, 10); // seed by day
        const seed = `stocktake:${today}`;
        const rng = seededRng(seed);

        // only divisions that actually have products
        const available = allDivisions.filter(d => Array.isArray(d.products) && d.products.length > 0);

        // try to find the pinned division (case-insensitive)
        const pinned = available.find(d => (d.division || "").toLowerCase() === String(pinnedName || "").toLowerCase());

        // shuffle the rest deterministically
        const rest = shuffleDeterministic(
            available.filter(d => d !== pinned),
            rng
        );

        const out = [];
        if (pinned) out.push(pinned);
        for (const d of rest) {
            if (out.length >= n) break;
            out.push(d);
        }
        return out;
    }

    // ===== Boot =====
    ready(async () => {
        bindOperatorSelection();
        setOperatorStatus();
        startOperatorTimeout();
        await startSignalR();
        await loadUiConfig(); 

        const qsOverride = parseQueryBool(getQueryParam("showEstimates"));
        if (qsOverride !== null) {
            SHOW_ESTIMATED = qsOverride;
        }

        plan = await fetchPlan();

        const showAll = getQueryParam("alldivisions") !== null;
        const n = parseInt(getQueryParam("count") || "2", 10); 
        const baseCount = (isFinite(n) && n > 0 ? n : 2); 
        const listToShow = showAll
            ? plan
            : pickDivisionsForTodayWithPinned(plan, baseCount + 1, "PANTRY"); // PANTRY + N others


        // Optional: small caption so staff know why fewer show
        const captionHost = document.getElementById("divisionTiles");
        if (!showAll) {
            const note = document.createElement("div");
            note.className = "muted";
            note.style.marginBottom = ".5rem";
            note.textContent = `Select a product division to get started.`;
            captionHost.parentElement.insertBefore(note, captionHost);
        }

        renderDivisions(listToShow);

        if (getCookie(OP_COOKIE)) markOperatorActivity();
    });
})();
