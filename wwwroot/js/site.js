// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

function AjaxGet(url, data, success) {
    $.ajax({
        url: url,
        type: "GET",
        cache: false,
        data: data,
        dataType: "json"
    })
        .done(function (result) {

            if (success)
                success(result);

        })
        .fail(function (xhr, status, error) {

            console.error(xhr);

            alert("İşlem sırasında bir hata oluştu.");

        });
}

function MenuRenderItems(items, aktifUrl, acikMenuler) {

    var html = "";
    var herhangiAktif = false;

    items.forEach(function (item) {

        var r = MenuRenderItem(item, aktifUrl, acikMenuler);

        html += r.html;

        if (r.active)
            herhangiAktif = true;
    });

    return {
        html: html,
        active: herhangiAktif
    };
}

function MenuRenderItem(item, aktifUrl, acikMenuler) {

    var icon = item.icon || "far fa-circle";

    var active = false;

    var html = "";

    switch (item.type) {

        // Menu
        case 0:
            var child = MenuRenderItems(item.children || [], aktifUrl, acikMenuler);
            active = child.active;
            var open = active || acikMenuler.indexOf(item.id) >= 0;

            html = `<li class="nav-item ${open ? "menu-open" : ""}" data-menu-id="${item.id}">
    <a href="#" class="nav-link ${active ? "active" : ""}">
        <i class="nav-icon ${icon}"></i>
        <p>
            ${item.text}
            <i class="right fas fa-angle-left"></i>
        </p>
    </a>
    <ul class="nav nav-treeview" ${open ? "" : 'style="display:none"'}>
        ${child.html}
    </ul>
</li>`;
            break;

        // Url / Html / Link
        default:
            active = NormalizeUrl(item.url) === aktifUrl;
            var target = item.target ? ` target="${item.target}"` : "";
            html = `<li class="nav-item" data-menu-id="${item.id}">
    <a href="${item.url}"${target} class="nav-link ${active ? "active" : ""}">
        <i class="nav-icon ${icon}"></i>
        <p>${item.text}</p>
    </a>
</li>`;
            break;
    }

    return {
        html: html,
        active: active
    };
}

function NormalizeUrl(url) {

    return (url || "")
        .toLowerCase()
        .replace(/\/$/, "");
}
function CookieOku(ad) {
    var cerezler = document.cookie.split(";");
    for (var i = 0; i < cerezler.length; i++) {
        var cerez = cerezler[i].trim();
        if (cerez.startsWith(ad + "="))
            return decodeURIComponent(cerez.substring(ad.length + 1));
    }

    return null;
}

function CookieYaz(ad, deger, gun) {
    var tarih = new Date();
    tarih.setTime(tarih.getTime() + (gun * 24 * 60 * 60 * 1000));

    document.cookie = ad + "=" +
        encodeURIComponent(deger) +
        "; expires=" + tarih.toUTCString() +
        "; path=/";
}

function MenuRender(menu) {

    var aktifUrl = NormalizeUrl(window.location.pathname + window.location.search);
    var acikMenuler = [];
    var c = CookieOku("HomeMenuAcik");
    if (c)
        acikMenuler = JSON.parse(c);

    return MenuRenderItems(menu, aktifUrl, acikMenuler).html;
}

function HomeMenuYukle() {
    AjaxGet("/Menu/Giris", null,
        function (menu) {
            $("#homeMenu").html(MenuRender(menu));
        }
    );
}


// Aktif sayfayı işaretle
function HomeMenuActive() {

}

// Açık menüleri geri aç
function HomeMenuCookieYukle() {

}

// Tıklamaları bağla
function HomeMenuEvents()
{

}




