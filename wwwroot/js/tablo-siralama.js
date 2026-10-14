// Başvuru / ön başvuru formundaki liste tablolarında başlığa tıklayarak sıralama.
// 1. tık: artan (sayı ve alfabetik, boşlar sonda), 2. tık: azalan (boşlar başta), 3. tık: ilk geldiği sıra.
// Satırlarında giriş alanı olan form tabloları (bilanço, bütçe, taahhüt vb.), çok satırlı başlıklar ve
// arada grup başlığı olan tablolar sıralanmaz; bir tablo data-siralama="yok" ile de dışarıda bırakılabilir.
// Tablolar yeniden çizildiğinde seçili sıralama yeniden uygulanır.
(function () {
    'use strict';

    const durumlar = new Map();
    const islemBasliklari = new Set(['işlem', 'işlemler', 'seç', 'dosyalar']);
    let uyguluyor = false;
    let gozlemci = null;

    function basliklar(t) {
        return [...t.tHead.rows[0].cells];
    }

    function tabloAnahtari(t) {
        const kap = t.id || t.closest('[id]')?.id || '';
        return kap + '::' + basliklar(t).map(c => c.textContent.trim()).join('|');
    }

    function siralanabilirBaslik(th) {
        return th.colSpan === 1
            && th.textContent.trim() !== ''
            && !islemBasliklari.has(th.textContent.trim().toLocaleLowerCase('tr'))
            && !th.querySelector('input, select, button, textarea');
    }

    function tabloUygunMu(t) {
        if (!t.tHead || t.tHead.rows.length !== 1 || t.tBodies.length !== 1) return false;
        if (t.closest('[data-siralama="yok"]')) return false;
        if (t.tBodies[0].querySelector('input:not([type=hidden]), select, textarea')) return false;
        return basliklar(t).some(siralanabilirBaslik);
    }

    // Satırları sıralanacak gruplara ayırır: rowspan ile birleşen satırlar tek grup olur.
    // Baştaki ve sondaki birleşik satırlar (not / toplam) yerinde kalır; arada birleşik satır varsa tablo sıralanmaz.
    function yapiCikar(t) {
        const sutunSayisi = basliklar(t).reduce((s, c) => s + c.colSpan, 0);
        const satirlar = [...t.tBodies[0].rows];
        const ust = [], alt = [], gruplar = [];
        const ozelMi = r => [...r.cells].some(c => c.colSpan > 1) || [...r.cells].reduce((s, c) => s + c.colSpan, 0) !== sutunSayisi;
        for (let i = 0; i < satirlar.length;) {
            const r = satirlar[i];
            if (ozelMi(r)) { (gruplar.length ? alt : ust).push(r); i++; continue; }
            if (alt.length) return null;
            const boy = Math.max(1, ...[...r.cells].map(c => c.rowSpan || 1));
            gruplar.push(satirlar.slice(i, i + boy));
            i += boy;
        }
        return { ust, alt, gruplar };
    }

    function hucreMetni(h) {
        if (!h) return '';
        if (h.dataset.siralamaDegeri !== undefined) return h.dataset.siralamaDegeri;
        return h.textContent.replace(/\s+/g, ' ').trim();
    }

    // Birleşik (rowspan) satır grubunda: birleşik sütunun değeri; yıl satırı gibi birleşik olmayan sütunda ilk dolu değer.
    function grupMetni(g, sutun) {
        const ilk = g[0].cells[sutun];
        if (!ilk || g.length === 1 || ilk.rowSpan > 1) return hucreMetni(ilk);
        const sira = [...g[0].cells].filter(c => c.rowSpan <= 1).indexOf(ilk);
        for (const r of g) {
            const metin = hucreMetni(r === g[0] ? ilk : r.cells[sira]);
            if (!anahtar(metin).bos) return metin;
        }
        return '';
    }

    // Boş, sayı (1.234,56 / %12 / 12,5 TL), tarih (gg.aa.yyyy) veya metin olarak karşılaştırma anahtarı.
    function anahtar(metin) {
        if (metin === '' || metin === '-' || metin === '—') return { bos: true };
        const tarih = /^(\d{1,2})\.(\d{1,2})\.(\d{4})(?:\s+(\d{1,2}):(\d{2}))?$/.exec(metin);
        if (tarih) return { sayi: +tarih[3] * 1e8 + +tarih[2] * 1e6 + +tarih[1] * 1e4 + (+tarih[4] || 0) * 100 + (+tarih[5] || 0) };
        const sade = metin.replace(/^%\s*|\s*(%|TL|₺|EUR|€|USD|\$)$/gi, '').replace(/\s/g, '');
        if (/^[-+]?\d{1,3}(\.\d{3})+(,\d+)?$/.test(sade) || /^[-+]?\d+(,\d+)?$/.test(sade))
            return { sayi: parseFloat(sade.replace(/\./g, '').replace(',', '.')) };
        return { metin };
    }

    function karsilastir(a, b) {
        if (a.bos || b.bos) return a.bos && b.bos ? 0 : a.bos ? 1 : -1;
        if (a.sayi !== undefined && b.sayi !== undefined) return a.sayi - b.sayi;
        if (a.sayi !== undefined) return -1;
        if (b.sayi !== undefined) return 1;
        return a.metin.localeCompare(b.metin, 'tr', { numeric: true, sensitivity: 'base' });
    }

    function degistir(fn) {
        uyguluyor = true;
        try { fn(); } finally { gozlemci?.takeRecords(); uyguluyor = false; }
    }

    function isaretle(t, durum) {
        basliklar(t).forEach((th, i) => {
            if (!siralanabilirBaslik(th)) return;
            const secili = durum && durum.sutun === i;
            const sinif = 'tablo-siralama-ikon fas ' + (secili ? (durum.yon === 'artan' ? 'fa-sort-up' : 'fa-sort-down') : 'fa-sort');
            const ariaSort = secili ? (durum.yon === 'artan' ? 'ascending' : 'descending') : 'none';
            let ikon = th.querySelector(':scope > .tablo-siralama-ikon');
            if (ikon && ikon.className === sinif && th.getAttribute('aria-sort') === ariaSort) return;
            degistir(() => {
                th.classList.add('tablo-siralanabilir');
                th.setAttribute('aria-sort', ariaSort);
                th.title = 'Sıralamak için tıklayın';
                if (!ikon) { ikon = document.createElement('i'); th.appendChild(ikon); }
                ikon.className = sinif;
            });
        });
    }

    function sirala(t, durum) {
        const yapi = yapiCikar(t);
        if (!yapi) return;
        let sonSira = Math.max(-1, ...yapi.gruplar.map(g => g[0].dataset.ilkSira === undefined ? -1 : +g[0].dataset.ilkSira));
        yapi.gruplar.forEach(g => { if (g[0].dataset.ilkSira === undefined) g[0].dataset.ilkSira = ++sonSira; });

        const dizi = yapi.gruplar.map(g => ({
            g,
            s: +g[0].dataset.ilkSira,
            k: durum ? anahtar(grupMetni(g, durum.sutun)) : null
        }));
        dizi.sort((x, y) => {
            if (!durum) return x.s - y.s;
            const fark = karsilastir(x.k, y.k);
            return (durum.yon === 'azalan' ? -fark : fark) || x.s - y.s;
        });

        const govde = t.tBodies[0];
        const hedef = [...yapi.ust, ...dizi.flatMap(x => x.g), ...yapi.alt];
        if (hedef.every((r, i) => govde.rows[i] === r)) return;
        degistir(() => {
            const parca = document.createDocumentFragment();
            hedef.forEach(r => parca.appendChild(r));
            govde.appendChild(parca);
        });
    }

    function hazirla(t) {
        if (!tabloUygunMu(t)) return;
        const durum = durumlar.get(tabloAnahtari(t));
        const yapi = yapiCikar(t);
        if (!yapi) return;
        // Yeni çizilen satırların ilk sırası, sıralama uygulanmadan önce kaydedilir.
        if (yapi.gruplar.some(g => g[0].dataset.ilkSira === undefined)) sirala(t, null);
        isaretle(t, durum);
        if (durum) sirala(t, durum);
    }

    function tumunuHazirla() {
        document.querySelectorAll('table').forEach(hazirla);
    }

    document.addEventListener('click', e => {
        const th = e.target.closest('th.tablo-siralanabilir');
        if (!th) return;
        const t = th.closest('table');
        if (!t || !tabloUygunMu(t) || th.parentElement !== t.tHead.rows[0] || !yapiCikar(t)) return;
        const sutun = th.cellIndex;
        const anahtarAdi = tabloAnahtari(t);
        const onceki = durumlar.get(anahtarAdi);
        let durum = null;
        if (!onceki || onceki.sutun !== sutun) durum = { sutun, yon: 'artan' };
        else if (onceki.yon === 'artan') durum = { sutun, yon: 'azalan' };
        if (durum) durumlar.set(anahtarAdi, durum); else durumlar.delete(anahtarAdi);
        sirala(t, durum);
        isaretle(t, durum);
    });

    function baslat() {
        tumunuHazirla();
        // Gözlemci ekran çizilmeden önce çalışır; yeniden çizilen tablo sırasız görünüp titremez.
        gozlemci = new MutationObserver(() => { if (!uyguluyor) tumunuHazirla(); });
        gozlemci.observe(document.body, { childList: true, subtree: true });
    }

    if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', baslat);
    else baslat();
})();
