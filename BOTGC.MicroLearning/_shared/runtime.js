(() => {
  const qs = new URLSearchParams(window.location.search);
  const packId = qs.get("pack") || "template-pack";

  const elPackTitle = document.getElementById("packTitle");
  const elPageTitle = document.getElementById("pageTitle");
  const elContent = document.getElementById("content");
  const elProgressBar = document.getElementById("progressBar");
  const elProgressText = document.getElementById("progressText");
  const btnPrev = document.getElementById("prevBtn");
  const btnNext = document.getElementById("nextBtn");
  const btnBack = document.getElementById("backBtn");

  let pack = null;
  let index = 0;

  const fetchText = async (url) => {
    const res = await fetch(url, { cache: "no-store" });
    if (!res.ok) throw new Error(`Failed to load ${url}`);
    return await res.text();
  };

  const escapeHtml = (s) =>
    s.replaceAll("&", "&amp;")
     .replaceAll("<", "&lt;")
     .replaceAll(">", "&gt;")
     .replaceAll('"', "&quot;")
     .replaceAll("'", "&#039;");

  const renderMarkdown = (md) => {
    const lines = md.replaceAll("\r\n", "\n").split("\n");
    const out = [];
    let inList = false;

    const closeList = () => {
      if (inList) {
        out.push("</ul>");
        inList = false;
      }
    };

    for (const raw of lines) {
      const line = raw.trimEnd();
      if (!line.trim()) {
        closeList();
        continue;
      }

      const h1 = line.match(/^#\s+(.*)$/);
      if (h1) {
        closeList();
        out.push(`<h1>${escapeHtml(h1[1])}</h1>`);
        continue;
      }

      const h2 = line.match(/^##\s+(.*)$/);
      if (h2) {
        closeList();
        out.push(`<h2>${escapeHtml(h2[1])}</h2>`);
        continue;
      }

      const h3 = line.match(/^###\s+(.*)$/);
      if (h3) {
        closeList();
        out.push(`<h3>${escapeHtml(h3[1])}</h3>`);
        continue;
      }

      const img = line.match(/^!\[(.*)\]\((.*)\)$/);
      if (img) {
        closeList();
        const alt = escapeHtml(img[1]);
        const src = img[2].trim();
        out.push(`<img alt="${alt}" src="${src}">`);
        continue;
      }

      const li = line.match(/^[-*]\s+(.*)$/);
      if (li) {
        if (!inList) {
          out.push("<ul>");
          inList = true;
        }
        out.push(`<li>${escapeHtml(li[1])}</li>`);
        continue;
      }

      const bq = line.match(/^>\s?(.*)$/);
      if (bq) {
        closeList();
        out.push(`<blockquote><p>${escapeHtml(bq[1])}</p></blockquote>`);
        continue;
      }

      closeList();
      out.push(`<p>${escapeHtml(line)}</p>`);
    }

    closeList();
    return out.join("\n");
  };

  const loadPack = async () => {
    const packUrl = `../packs/${packId}/pack.json`;
    const txt = await fetchText(packUrl);
    pack = JSON.parse(txt);
    elPackTitle.textContent = pack.title || "Learning";
    index = 0;
    await render();
  };

  const setProgress = () => {
    const total = pack.pages.length;
    const current = index + 1;
    const pct = Math.round((current / total) * 100);
    elProgressBar.style.width = `${pct}%`;
    elProgressText.textContent = `Page ${current} of ${total}`;
    btnPrev.disabled = index <= 0;
    btnNext.textContent = index >= total - 1 ? "Finish" : "Next";
  };

  const render = async () => {
    const page = pack.pages[index];
    elPageTitle.textContent = page.title || "";
    const md = await fetchText(`../packs/${packId}/${page.file}`);
    elContent.innerHTML = renderMarkdown(md);
    setProgress();
    elContent.focus();
  };

  const go = async (delta) => {
    const next = index + delta;
    if (next < 0) return;
    if (next >= pack.pages.length) {
      window.history.back();
      return;
    }
    index = next;
    await render();
  };

  btnPrev.addEventListener("click", () => go(-1));
  btnNext.addEventListener("click", () => go(1));
  btnBack.addEventListener("click", () => window.history.back());

  loadPack().catch((e) => {
    elContent.innerHTML = `<div class="callout"><div class="callout__title">Could not load pack</div><p>${escapeHtml(e.message)}</p></div>`;
  });
})();
