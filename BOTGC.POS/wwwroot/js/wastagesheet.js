
(() => {
    'use strict';

    // Run after DOM is ready
    function ready(fn) {
        if (document.readyState !== 'loading') fn();
        else document.addEventListener('DOMContentLoaded', fn);
    }

    ready(() => {

        // ===== Config =====
        const OP_COOKIE = "wastage_operator_id";
        const OP_TS_COOKIE = "wastage_operator_ts";
        const ACTIVITY_KEY = "wastage_last_activity";
        const OP_TIMEOUT_MINUTES = 3; // change if you want a different timeout

        // ===== Auto Refresh at Midnight =====
        let dayStamp = (new Date()).toDateString();

        // ===== State =====
        let selectedProduct = null;
        let selectedReasonId = null;
        let pollTimer = null;
        let timeoutTimer = null;
        let isLogging = false;
        let deleteTargetId = null;

        let stockTakeInfo = null;
        let currentOperatorName = "";

        let stockTakeChimeTimer = null;
        let stockTakeAcknowledged = false;

        let hasUserInteracted = false;

        document.addEventListener("click", e => {
            const btn = e.target.closest(".delete-btn");
            if (!btn) return;

            deleteTargetId = btn.dataset.id;
            if (!deleteTargetId) return;

            document.getElementById("confirmDeleteDialog").showModal();
        });

        document.querySelectorAll(".qty-quick button").forEach(b => {
            b.addEventListener("click", () => {
                const inc = parseFloat(b.dataset.add || "0");
                const input = document.getElementById("qty");
                const cur = parseFloat(input.value || "0");
                const next = (isNaN(cur) ? 0 : cur) + inc;
                input.value = (Math.round(next * 100) / 100).toString();
                input.dispatchEvent(new Event("input", { bubbles: true }));
            });
        });

        document.getElementById("confirmDeleteYes").addEventListener("click", async () => {
            if (!deleteTargetId) return;
            const res = await fetch(`/wastage/entry/${deleteTargetId}`, { method: "DELETE" });
            if (res.ok) {
                document.querySelector(`#sheetBody tr[data-id='${deleteTargetId}']`)?.remove();
            } else {
                alert("Failed to delete entry.");
            }
            document.getElementById("confirmDeleteDialog").close();
            deleteTargetId = null;
        });

        document.getElementById("confirmDeleteNo").addEventListener("click", () => {
            deleteTargetId = null;
            document.getElementById("confirmDeleteDialog").close();
        });

        const stockBtn = document.getElementById("stocktakeAlertBtn");
        if (stockBtn) {
            stockBtn.addEventListener("click", () => {
                stockTakeAcknowledged = true;
                stopStockTakeChimeTimer();

                if (!stockTakeInfo || !stockTakeInfo.url) return;
                const base = stockTakeInfo.url;
                const op = currentOperatorName || "";
                const sep = base.includes("?") ? "&" : "?";
                const url = `${base}${sep}operator=${encodeURIComponent(op)}`;
                window.open(url, "_blank", "noopener");
            });
        }

        // ===== Utils =====
        function guid() {
            return ([1e7] + -1e3 + -4e3 + -8e3 + -1e11).replace(/[018]/g, c =>
                (c ^ crypto.getRandomValues(new Uint8Array(1))[0] & 15 >> c / 4).toString(16)
            );
        }

        function existingIds() {
            return new Set(Array.from(document.querySelectorAll("#sheetBody tr")).map(tr => tr.dataset.id));
        }

        function getCookie(n) {
            const v = ("; " + document.cookie).split("; " + n + "=");
            if (v.length === 2) return v.pop().split(";").shift();
            return null;
        }

        function setCookie(n, val, days) {
            let exp = "";
            if (typeof days === "number") {
                const d = new Date();
                d.setTime(d.getTime() + days * 24 * 60 * 60 * 1000);
                exp = "; expires=" + d.toUTCString();
            }
            document.cookie = `${n}=${val}; path=/; SameSite=Lax${exp}`;
        }

        function deleteCookie(n) {
            document.cookie = `${n}=; expires=Thu, 01 Jan 1970 00:00:00 GMT; path=/; SameSite=Lax`;
        }

        function nowUtcMs() { return Date.now(); }

        function markOperatorActivity() {
            const now = String(nowUtcMs());
            setCookie(OP_TS_COOKIE, now, 365);
            try { localStorage.setItem(ACTIVITY_KEY, now); } catch { }
            updateOpTtlOnce();
        }

        function operatorExpired() {
            const ts = parseInt(getCookie(OP_TS_COOKIE) || "0", 10);
            if (!ts) return true;
            const elapsed = nowUtcMs() - ts;
            return elapsed > OP_TIMEOUT_MINUTES * 60 * 1000;
        }

        function clearOperatorSelection() {
            deleteCookie(OP_COOKIE);
            deleteCookie(OP_TS_COOKIE);
            stopOpTtlTimer();
            currentOperatorName = "";
            const el = document.getElementById("operatorStatus");
            if (el) el.innerHTML = '<span class="muted">No operator selected.</span>';
        }

        function enforceOperatorTimeout() {
            const hasOp = !!getCookie(OP_COOKIE);
            if (!hasOp) return;

            if (operatorExpired()) {
                clearOperatorSelection();      
                setOperatorStatus();           
                closeNonOperatorDialogs();     
                openOperatorDialog();          
            }
        }

        function startTimeoutEnforcer() {
            if (timeoutTimer) clearInterval(timeoutTimer);

            let lastTick = nowUtcMs();
            const INTERVAL_MS = 15000;

            const tick = () => {
                const now = nowUtcMs();
                // If the page slept (big drift), enforce immediately
                if (now - lastTick > INTERVAL_MS * 2) {
                    enforceOperatorTimeout();
                }
                enforceOperatorTimeout();
                lastTick = now;
            };

            timeoutTimer = setInterval(tick, INTERVAL_MS);

            // Re-check when the page becomes active/visible again
            window.addEventListener("focus", enforceOperatorTimeout);
            window.addEventListener("pageshow", enforceOperatorTimeout);
            document.addEventListener("visibilitychange", () => {
                if (!document.hidden) enforceOperatorTimeout();
            });

            // Cross-tab sync: if another tab updates activity, re-check here
            window.addEventListener("storage", (e) => {
                if (e.key === ACTIVITY_KEY) enforceOperatorTimeout();
            });
        }

        async function fetchSheet() {
            const res = await fetch("/wastage/sheet");
            if (!res.ok) return null;
            return await res.json();
        }

        function escapeHtml(s) {
            return String(s ?? "")
                .replace(/&/g, "&amp;")
                .replace(/</g, "&lt;")
                .replace(/>/g, "&gt;")
                .replace(/"/g, "&quot;")
                .replace(/'/g, "&#39;");
        }
        function softBreakSlash(s) {
            return String(s ?? "").replace(/\//g, "/\u200b");
        }

        function addRow(operatorName, productName, reason, qty, id, atIso) {
            const tr = document.createElement("tr");
            tr.dataset.id = id;

            const at = atIso ? new Date(atIso) : new Date();
            const hh = at.getHours().toString().padStart(2, "0");
            const mm = at.getMinutes().toString().padStart(2, "0");

            tr.innerHTML =
                `<td>${hh}:${mm}</td>
                 <td>${escapeHtml(operatorName)}</td>
                 <td class="cell--product" title="${escapeHtml(productName)}">${escapeHtml(productName)}</td>
                 <td class="cell--reason"  title="${escapeHtml(reason)}">${escapeHtml(softBreakSlash(reason))}</td>
                 <td>${escapeHtml(qty)}</td>
                 <td>
                    <button class="btn btn-link text-danger p-0 delete-btn" data-id="${id}" title="Delete">
                      <i class="bi bi-trash"></i>
                    </button>
                 </td>`;

            document.getElementById("sheetBody").prepend(tr);
        }

        function attachActivityListeners() {
            let lastMark = 0;
            const THROTTLE_MS = 5000;

            const maybeMark = () => {
                hasUserInteracted = true; 

                if (!getCookie(OP_COOKIE)) return; // only while an operator is selected
                const now = nowUtcMs();
                if (now - lastMark >= THROTTLE_MS) {
                    markOperatorActivity();
                    lastMark = now;
                }
            };

            document.addEventListener("click", maybeMark, { passive: true, capture: true });
            document.addEventListener("keydown", maybeMark, { passive: true, capture: true });
            document.addEventListener("pointerdown", maybeMark, { passive: true, capture: true });
            document.addEventListener("touchstart", maybeMark, { passive: true, capture: true });
        }

        async function pollSheet() {
            try {
                const sheet = await fetchSheet();
                if (!sheet) return;

                // map operator id -> name for display
                const opLookup = {};
                document.querySelectorAll("#operatorTiles .tile").forEach(t => opLookup[t.dataset.opid] = t.textContent);

                // sort newest first (as you had)
                sheet.entries.sort((a, b) => new Date(b.at) - new Date(a.at));

                // Build set of server-side ids
                const serverIds = new Set(sheet.entries.map(e => e.id));

                // 1) Remove any local rows that aren’t on the server anymore
                const body = document.getElementById("sheetBody");
                Array.from(body.querySelectorAll("tr")).forEach(tr => {
                    if (!serverIds.has(tr.dataset.id)) tr.remove();
                });

                // 2) Add any server rows we don’t already have
                const have = existingIds();
                sheet.entries.forEach(e => {
                    if (!have.has(e.id)) {
                        const opName = opLookup[e.operatorId] || "Unknown";
                        addRow(opName, e.productName, e.reason, e.quantity, e.id, e.at);
                    }
                });
            } catch { }
        }

        function startPolling() {
            if (pollTimer) clearInterval(pollTimer);
            pollTimer = setInterval(pollSheet, 7000);
        }

        function getContrast(hex) {
            if (!hex) return "#111";
            var c = hex.replace(/^#/, "");
            if (c.length === 3) c = c.split("").map(x => x + x).join("");
            if (c.length !== 6) return "#111";
            var r = parseInt(c.substr(0, 2), 16);
            var g = parseInt(c.substr(2, 2), 16);
            var b = parseInt(c.substr(4, 2), 16);
            var yiq = (r * 299 + g * 587 + b * 114) / 1000;
            return yiq >= 150 ? "#111" : "#fff";
        }

        function orderOperatorsByLocalPopularity() {
            const key = "op_popularity";
            const map = JSON.parse(localStorage.getItem(key) || "{}");
            const tiles = Array.from(document.querySelectorAll("#operatorTiles .tile"));
            tiles.sort((a, b) => (map[b.dataset.opid] || 0) - (map[a.dataset.opid] || 0));
            const host = document.getElementById("operatorTiles"); host.innerHTML = ""; tiles.forEach(t => host.appendChild(t));
        }

        function recordOperatorPopularity(opId) {
            const key = "op_popularity";
            const map = JSON.parse(localStorage.getItem(key) || "{}");
            map[opId] = (map[opId] || 0) + 1;
            localStorage.setItem(key, JSON.stringify(map));
        }

        function openOperatorDialog() {
            orderOperatorsByLocalPopularity();
            const dlg = document.getElementById("operatorModal");
            dlg.addEventListener("cancel", e => e.preventDefault());
            dlg.showModal();
        }

        function bindOperatorPillClick() {
            const el = document.querySelector("#operatorStatus .tile--op");
            if (!el) return;
            el.style.cursor = "pointer";
            el.addEventListener("click", openOperatorDialog);
        }

        function renderOperatorPill(name, color) {
            const el = document.getElementById("operatorStatus");
            const fg = getContrast(color);
            const badge = mixWithBlack(color, 0.12);

            el.innerHTML =
                `<div class="tile tile--op" style="background:${color}; color:${fg}; --badge:${badge};">
                    <span class="pill-dot" style="background:${fg};"></span>
                    <span class="tile__name">${name}</span>
                    <small class="tile__division"><span id="opTtl"></span></small>
                </div>`;
            bindOperatorPillClick();
            startOpTtlTimer();
            showStockTakeBannerIfNeeded();
        }

        function ensureOperatorSelected() {
            const opId = getCookie(OP_COOKIE);
            if (!opId) {
                openOperatorDialog();
                return;
            }
            if (operatorExpired()) {
                clearOperatorSelection();
                openOperatorDialog();
            }
        }

        function setOperatorStatus() {
            const id = getCookie(OP_COOKIE);
            const el = document.getElementById("operatorStatus");
            if (!id) { el.innerHTML = '<span class="muted">No operator selected.</span>'; return; }
            const tile = document.querySelector(`#operatorTiles .tile[data-opid='${id}']`);
            if (!tile) {
                clearOperatorSelection();
                ensureOperatorSelected();
                return;
            }
            renderOperatorPill(tile.textContent.trim(), tile.dataset.color || "#555555");
        }

        // ===== Bindings =====
        Array.from(document.querySelectorAll("#operatorTiles .tile")).forEach(t => {
            t.addEventListener("click", async () => {
                const id = t.dataset.opid;
                const fd = new FormData(); fd.append("id", id);
                const res = await fetch("/wastage/select-operator", { method: "POST", body: fd });
                if (res.ok) {
                    // Server sets OP_COOKIE; we ensure/refresh the timestamp on the client.
                    recordOperatorPopularity(id);
                    markOperatorActivity();
                    document.getElementById("operatorModal").close();
                    setOperatorStatus();
                }
            });
        });
                function productChosen(id, name, igid, unit) {
            if (!getCookie(OP_COOKIE)) { ensureOperatorSelected(); return; }
            if (operatorExpired()) { clearOperatorSelection(); ensureOperatorSelected(); return; }

            selectedProduct = { id, name, igid, unit, components: [] };

            // Fetch details (gets components + canonical unit/name from server)
            fetch(`/wastage/product/${id}`)
                .then(r => r.ok ? r.json() : null)
                .then(d => {
                    if (d) {
                        selectedProduct.name = d.name;
                        selectedProduct.igid = d.igProductId;
                        selectedProduct.unit = d.unit;
                        selectedProduct.components = Array.isArray(d.components) ? d.components : [];
                    }

                    document.getElementById("reasonTitle").textContent = `Reason for: ${selectedProduct.name}`;

                    const customEl = document.getElementById("customReason");
                    customEl.value = "";
                    customEl.dataset.autofill = "0";

                    document.getElementById("qty").value = "";

                    const u = (selectedProduct.unit || "").trim();
                    document.getElementById("qtyUnit").textContent = u ? `(${u})` : "";

                    const qtyInput = document.getElementById("qty");
                    if (/pint|half|ml|ltr|liter|litre/i.test(u)) qtyInput.step = "0.25";
                    else qtyInput.step = "1";

                    applyUnitConstraints(selectedProduct.unit);

                    document.getElementById("reasonModal").showModal();
                });
        }

        Array.from(document.querySelectorAll("#topTiles .tile")).forEach(t => {
            if (t.id === "openSearch") return;
            t.addEventListener("click", () => productChosen(t.dataset.id, t.dataset.name, t.dataset.igid, t.dataset.unit));
        });

        document.getElementById("openSearch").addEventListener("click", () => {
            if (!getCookie(OP_COOKIE)) { ensureOperatorSelected(); return; }
            if (operatorExpired()) { clearOperatorSelection(); ensureOperatorSelected(); return; }
            document.getElementById("searchResults").innerHTML = "";
            document.getElementById("searchInput").value = "";
            document.getElementById("searchModal").showModal();
            document.getElementById("searchInput").focus();
        });

        document.getElementById("searchInput").addEventListener("input", async (e) => {
            const q = e.target.value.trim();
            const host = document.getElementById("searchResults");
            host.innerHTML = "";
            if (q.length < 2) return;

            const res = await fetch(`/wastage/search?q=${encodeURIComponent(q)}`);
            if (!res.ok) return;
            const data = await res.json();

            const catSlug = (s) =>
                (s || "uncategorised")
                    .toLowerCase()
                    .normalize("NFKD")
                    .replace(/&/g, " ")
                    .replace(/[^a-z0-9]+/g, "-")
                    .replace(/^-+|-+$/g, "");

            data.forEach(p => {
                const slug = catSlug(p.category);
                const div = document.createElement("div");
                div.className = `tile cat-${slug}`;

                // datasets
                div.dataset.id = p.id;
                div.dataset.name = p.name;
                div.dataset.igid = p.igProductId;
                div.dataset.unit = p.unit;

                // content (name + bottom-right category label)
                div.innerHTML = `
                    <div class="tile__name">${p.name}</div>
                    <small class="tile__division">${p.category || ""}</small>
                `;

                div.addEventListener("click", () => {
                    document.getElementById("searchModal").close();
                    productChosen(p.id, p.name, p.igProductId, p.unit);
                });

                host.appendChild(div);
            });
        });

        const customEl = document.getElementById("customReason");

        Array.from(document.querySelectorAll(".reason-btn")).forEach(b => {
            b.addEventListener("click", () => {
                if (b.style.display === "none" || b.disabled) return;

                selectedReasonId = b.dataset.reasonid;

                document.querySelectorAll(".reason-btn.is-selected").forEach(x => x.classList.remove("is-selected"));
                b.classList.add("is-selected");

                const d = b.dataset.defaultqty;
                if (d) document.getElementById("qty").value = d;

                const name = (b.firstChild?.textContent || b.textContent || "").trim();
                customEl.value = name;
                customEl.dataset.autofill = "1";
            });
        });

        // when the user edits the text, treat it as a true custom reason
        customEl.addEventListener("input", () => { customEl.dataset.autofill = "0"; });


        // Map 'unit' strings to a canonical key
        function unitKey(u) {
            if (!u) return "";
            const s = u.trim().toLowerCase();
            if (s.includes("can")) return "can";
            if (s.includes("bottle")) return "bottle";
            if (s.includes("pint")) return "pint";
            if (s.includes("half")) return "half";
            if (s.includes("ml") || s.includes("shot") || s.includes("measure")) return "ml";
            return s;
        }

        // Fallback exclusions if server doesn't send data-excludedunits on buttons
        const defaultExclusions = {
            "drip tray": ["can", "bottle", "ml"],
            "barrel change": ["can", "bottle", "ml"],
            "pipe clean": ["can", "bottle", "ml"],
            "bar pull through": ["can", "bottle", "ml"]
        };

        function applyUnitConstraints(unit) {
            const ukey = unitKey(unit);
            const buttons = document.querySelectorAll(".reason-btn");
            let cleared = false;

            buttons.forEach(btn => {
                // from data-excludedunits or fallback by name
                let ex = (btn.dataset.excludedunits || "").toLowerCase();
                if (!ex) {
                    const name = btn.firstChild.textContent.trim().toLowerCase();
                    ex = (defaultExclusions[name] || []).join("|");
                }
                const excludes = ex.split("|").map(x => x.trim()).filter(Boolean);
                const blocked = ukey && excludes.includes(ukey);

                // hide instead of disable
                btn.style.display = blocked ? "none" : "";
                if (blocked && btn.classList.contains("is-selected")) {
                    btn.classList.remove("is-selected");
                    cleared = true;
                }
            });

            if (cleared) selectedReasonId = null;
        }

        document.getElementById("logBtn").addEventListener("click", async () => {
            if (isLogging) return;
            if (!selectedProduct) return;

            if (operatorExpired()) { clearOperatorSelection(); ensureOperatorSelected(); return; }

            const qty = parseFloat(document.getElementById("qty").value || "0");
            if (!(qty > 0)) { alert("Enter a quantity greater than zero."); return; }

            const customEl = document.getElementById("customReason");
            const custom = customEl.value.trim();
            const isAuto = customEl.dataset.autofill === "1";

            let reasonIdToSend = null;
            if (selectedReasonId && isAuto) reasonIdToSend = selectedReasonId;
            else if (!custom && selectedReasonId) reasonIdToSend = selectedReasonId;

            const btn = document.getElementById("logBtn");
            isLogging = true;
            btn.disabled = true;
            const oldText = btn.textContent;
            btn.textContent = "Logging…";

            try {
                const fd = new FormData();
                fd.append("productId", selectedProduct.id);
                fd.append("quantity", qty.toString());
                const clientId = guid();
                fd.append("clientId", clientId);
                if (reasonIdToSend) fd.append("reasonId", reasonIdToSend);
                else if (custom) fd.append("customReason", custom);

                let url = "/wastage/log";
                const hasComponents = Array.isArray(selectedProduct.components) && selectedProduct.components.length > 0;
                const isComposite = hasComponents && selectedProduct.components.length > 1;

                if (isComposite) {
                    url = "/wastage/log-product";
                } else {
                    fd.append("igProductId", (selectedProduct.igid || 0).toString());
                    fd.append("unit", selectedProduct.unit || "");
                    fd.append("productName", selectedProduct.name || "");
                }

                const res = await fetch(url, { method: "POST", body: fd });

                markOperatorActivity();
                document.getElementById("reasonModal").close();

                selectedProduct = null;
                selectedReasonId = null;
                document.getElementById("qty").value = "";
                customEl.value = "";
                customEl.dataset.autofill = "0";
                document.querySelectorAll(".reason-btn.is-selected").forEach(x => x.classList.remove("is-selected"));

                await pollSheet();
            } catch {
                alert("Failed to log waste.");
            } finally {
                isLogging = false;
                btn.disabled = false;
                btn.textContent = oldText;
            }
        });

        // ===== SignalR (existing) =====
        let connection = null;
        function startSignalR() {
            connection = new signalR.HubConnectionBuilder()
                .withUrl("/hubs/wastage")
                .withUrl("/hubs/wastage")
                .withAutomaticReconnect()
                .build();

            connection.on("EntryAdded", async () => {
                await pollSheet();
            });

            connection.on("EntriesAdded", async () => {
                await pollSheet();
            });

            connection.on("EntryDeleted", async () => {
                await pollSheet();
            });

            connection.on("SheetSubmitted", e => { /* optional */ });

            connection.start().catch(err => console.error(err));
        }

        function mixWithBlack(hex, amount = 0.12) {
            let c = (hex || "#999").replace(/^#/, "");
            if (c.length === 3) c = c.split("").map(x => x + x).join("");
            const r = parseInt(c.slice(0, 2), 16);
            const g = parseInt(c.slice(2, 4), 16);
            const b = parseInt(c.slice(4, 6), 16);
            const rr = Math.round((1 - amount) * r).toString(16).padStart(2, "0");
            const gg = Math.round((1 - amount) * g).toString(16).padStart(2, "0");
            const bb = Math.round((1 - amount) * b).toString(16).padStart(2, "0");
            return `#${rr}${gg}${bb}`;
        }

        function startDayGuard() {
            function checkDay() {
                const nowStamp = new Date().toDateString();
                if (nowStamp !== dayStamp) {
                    location.reload();
                }
            }

            // Periodic check
            setInterval(checkDay, 15000);

            // Also check when the tab becomes visible
            document.addEventListener("visibilitychange", () => {
                if (!document.hidden) checkDay();
            });

            // Belt-and-braces: run once just after midnight
            const now = new Date();
            const next = new Date(now.getFullYear(), now.getMonth(), now.getDate() + 1, 0, 0, 1, 0);
            setTimeout(checkDay, next.getTime() - now.getTime());
        }

        function closeNonOperatorDialogs() {
            document.getElementById("reasonModal")?.open && document.getElementById("reasonModal").close();
            document.getElementById("searchModal")?.open && document.getElementById("searchModal").close();
        }

        let opTtlTimer = null;

        function formatTtl(ms) {
            const s = Math.max(0, Math.floor(ms / 1000));
            const m = Math.floor(s / 60);
            const r = s % 60;
            return `${m}:${r.toString().padStart(2, "0")}`;
        }

        function updateOpTtlOnce() {
            const ttlEl = document.getElementById("opTtl");
            if (!ttlEl) return;

            const id = getCookie(OP_COOKIE);
            if (!id) { ttlEl.textContent = ""; stopOpTtlTimer(); return; }

            const ts = parseInt(getCookie(OP_TS_COOKIE) || "0", 10);
            const expiresAt = ts + OP_TIMEOUT_MINUTES * 60 * 1000;
            const remaining = expiresAt - nowUtcMs();

            if (remaining <= 0) {
                ttlEl.textContent = "0:00";
                enforceOperatorTimeout(); // will clear pill & show dialog
                return;
            }

            ttlEl.textContent = formatTtl(remaining);
        }

        function startOpTtlTimer() {
            stopOpTtlTimer();
            updateOpTtlOnce();
            opTtlTimer = setInterval(updateOpTtlOnce, 1000);
        }

        function stopOpTtlTimer() {
            if (opTtlTimer) { clearInterval(opTtlTimer); opTtlTimer = null; }
        }

        async function loadStockTakeStatus() {
            try {
                const res = await fetch("/wastage/stocktake-status");
                if (!res.ok) return;
                stockTakeInfo = await res.json();
                showStockTakeBannerIfNeeded();
            } catch { }
        }

        function showStockTakeBannerIfNeeded() {
            const host = document.getElementById("stocktakeAlert");
            if (!host) return;

            const shouldShow = stockTakeInfo && stockTakeInfo.due && stockTakeInfo.url;

            if (shouldShow && host.hidden) {
                host.hidden = false;
                startStockTakeChimeTimer();

                if (shouldChimeNow()) {
                    playStockTakeSound();
                }
            } else if (!shouldShow) {
                host.hidden = true;
                stopStockTakeChimeTimer();
            }
        }

        function clearStockTakeBanner() {
            const host = document.getElementById("stocktakeAlert");
            if (host) host.hidden = true;
            stopStockTakeChimeTimer();
        }

        function playStockTakeSound() {
            if (!hasUserInteracted) return;

            const audio = document.getElementById("stocktakeSound");
            const btn = document.getElementById("stocktakeAlertBtn");

            if (btn) {
                btn.classList.remove("stocktake-alert__btn--shiver");
                void btn.offsetWidth; // restart animation
                btn.classList.add("stocktake-alert__btn--shiver");
                setTimeout(() => btn.classList.remove("stocktake-alert__btn--shiver"), 500);
            }

            if (!audio) return;
            try {
                audio.currentTime = 0;
                audio.play();
            } catch {
                // ignore autoplay blocks
            }
        }

        function isWithinChimeWindow() {
            if (!stockTakeInfo) return false;

            const startStr = stockTakeInfo.chimeStart;
            const endStr = stockTakeInfo.chimeEnd;

            const start = parseTimeToMinutes(startStr);
            const end = parseTimeToMinutes(endStr);

            if (start === null || end === null) return true;

            const now = new Date();
            const cur = now.getHours() * 60 + now.getMinutes();

            if (start <= end) {
                return cur >= start && cur <= end;
            }

            // Handles windows that cross midnight (e.g. 22:00–02:00)
            return cur >= start || cur <= end;
        }

        function shouldChimeNow() {
            if (!hasUserInteracted) return false; // must satisfy autoplay rules
            if (!stockTakeInfo || !stockTakeInfo.due || !stockTakeInfo.url) return false;
            if (!stockTakeInfo.chimeEnabled) return false;
            if (stockTakeAcknowledged) return false;
            if (!isWithinChimeWindow()) return false;
            return true;
        }

        function startStockTakeChimeTimer() {
            stopStockTakeChimeTimer();

            if (!stockTakeInfo || !stockTakeInfo.chimeEnabled) return;

            let interval = parseInt(stockTakeInfo.chimeIntervalMinutes, 10);
            if (!interval || interval <= 0) interval = 15;

            stockTakeChimeTimer = setInterval(() => {
                if (shouldChimeNow()) {
                    playStockTakeSound();
                }
            }, interval * 60 * 1000);
        }

        function stopStockTakeChimeTimer() {
            if (stockTakeChimeTimer) {
                clearInterval(stockTakeChimeTimer);
                stockTakeChimeTimer = null;
            }
        }


        function parseTimeToMinutes(hhmm) {
            if (!hhmm || typeof hhmm !== "string") return null;
            const parts = hhmm.split(":");
            if (parts.length !== 2) return null;
            const h = parseInt(parts[0], 10);
            const m = parseInt(parts[1], 10);
            if (isNaN(h) || isNaN(m) || h < 0 || h > 23 || m < 0 || m > 59) return null;
            return h * 60 + m;
        }

        // ===== Boot =====
        setOperatorStatus();
        ensureOperatorSelected();
        startPolling();
        startSignalR();
        startTimeoutEnforcer();
        startDayGuard();
        attachActivityListeners();
        loadStockTakeStatus();

        // If an operator is already selected, (re)start the timeout window now.
        if (getCookie(OP_COOKIE)) markOperatorActivity();

    });
})();
