// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
var popupMesajSayac = 0;

function PopupMesajGoster(mesaj, basarili) {
    if (!mesaj) return;

    var $container = PopupMesajContainer();
    var id = "popupMesaj_" + (++popupMesajSayac);
    var $mesaj = $('<div/>', {
        id: id,
        class: "alert alert-dismissible floating-message-item " + (basarili ? "alert-info" : "alert-danger"),
        style: "display:none"
    });

    $mesaj.append($('<span/>').text(mesaj));
    $mesaj.append(
        $('<button/>', {
            type: "button",
            class: "close popup-mesaj-kapat",
            "aria-label": "Close"
        }).append($('<span/>', { "aria-hidden": "true" }).html("&times;"))
    );

    $container.append($mesaj);
    $mesaj.fadeIn(120);
    PopupMesajTimerBaslat($mesaj);
}

function PopupMesajContainer() {
    var $container = $("#popupMesajContainer");
    if ($container.length)
        return $container;

    $container = $('<div/>', { id: "popupMesajContainer", class: "floating-message-stack" });
    $("body").append($container);
    return $container;
}

function PopupMesajTimerBaslat($mesaj) {
    var timer = window.setTimeout(function () {
        PopupMesajKapat($mesaj);
    }, 10000);

    $mesaj.data("popupTimer", timer);
}

function PopupMesajKapat(mesaj) {
    var $mesaj = mesaj && mesaj.jquery ? mesaj : $(mesaj).closest(".floating-message-item");
    if (!$mesaj.length)
        $mesaj = $("#ajaxMesaj");

    var timer = $mesaj.data("popupTimer");
    if (timer)
        window.clearTimeout(timer);

    $mesaj.fadeOut(120, function () {
        $(this).remove();
    });
}

function PopupMesajIlklendir() {
    $(document)
        .off("click.popupMesaj", "#ajaxMesajKapat, .popup-mesaj-kapat")
        .on("click.popupMesaj", "#ajaxMesajKapat, .popup-mesaj-kapat", function () {
            PopupMesajKapat(this);
        });

    var $eskiMesaj = $("#ajaxMesaj");
    var ilkMesaj = $("#ajaxMesajMetin").text().trim();
    if (ilkMesaj && !$eskiMesaj.data("popupInitialized")) {
        PopupMesajContainer().append($eskiMesaj);
        $eskiMesaj
            .removeClass("floating-message")
            .addClass("floating-message-item")
            .show();
        $eskiMesaj.data("popupInitialized", true);
        PopupMesajTimerBaslat($eskiMesaj);
    }
}

window.AjaxPost = function (url, data, success) {
    // Sayfadaki anti-forgery token'ı alan fonksiyonunu çağırıyoruz
    // (Eğer fonksiyon adı farklıysa projenizdekine göre güncelleyin, örn: antiForgeryToken())
    const token = typeof antiForgeryToken === "function" ? antiForgeryToken() : $('[name="__RequestVerificationToken"]').val();

    $.ajax({
        url: url,
        type: "POST",
        contentType: "application/json; charset=utf-8", // Verinin JSON olduğunu belirtiyoruz
        dataType: "json",
        data: JSON.stringify(data), // JavaScript nesnesini JSON string'ine çeviriyoruz
        headers: {
            // ASP.NET Core'un doğrulaması için token'ı header'a ekliyoruz
            "RequestVerificationToken": token
        },
        cache: false
    })
        .done(function (result) {
            if (success) {
                success(result);
            }
        })
        .fail(function (xhr, status, error) {
            console.error("AjaxPost Hatası:", xhr);
            alert("İşlem sırasında bir hata oluştu.");
        });
};
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

// Global String Alıcı
window.degerStr = function (name) {
    return $(`[name="${name}"]`).val()?.trim() ?? '';
};

// Global Integer Alıcı
window.degerInt = function (name) {
    const v = parseInt($(`[name="${name}"]`).val());
    return isNaN(v) ? null : v;
};

// Global Decimal Alıcı
window.degerDec = function (name) {
    // Kullanıcı virgül girerse noktaya çevirip parseFloat yapıyoruz
    const hamDeger = $(`[name="${name}"]`).val()?.toString().replace(',', '.');
    const v = parseFloat(hamDeger);
    return isNaN(v) ? 0.0 : v;
};

// Global Boolean Alıcı
window.degerBool = function (name) {
    const $el = $(`[name="${name}"]`);
    if ($el.length === 0) return false;

    // Eğer eleman checkbox ise seçili olma durumuna, değilse text/select değerine bakar
    return $el.is(':checkbox') ? $el.is(':checked') : $el.val() === 'true';
};

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

            html = `<li class="nav-item menu-folder ${open ? "menu-open" : ""}" data-menu-id="${item.id}">
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
    var cookieAdi = window.aktifMenuCookieAdi || "HomeMenuAcik";
    var c = CookieOku(cookieAdi);
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

function HomeMenuCookieKaydet() {
    MenuCookieKaydet("#homeMenu", "HomeMenuAcik");
}

function BasvuruMenuYukle() {
    AjaxGet("/Menu/Basvuru", null,
        function (menu) {
            window.aktifMenuCookieAdi = "BasvuruMenuAcik";
            $("#basvuruMenu").html(MenuRender(menu));
            window.aktifMenuCookieAdi = null;
        }
    );
}

function BasvuruMenuCookieKaydet() {
    MenuCookieKaydet("#basvuruMenu", "BasvuruMenuAcik");
}

function MenuCookieKaydet(menuSecici, cookieAdi) {

    var aciklar = [];

    $(menuSecici + " .menu-folder.menu-open").each(function () {
        aciklar.push($(this).data("menu-id"));
    });

    CookieYaz(cookieAdi, JSON.stringify(aciklar), 30);
}

$(document).on("expanded.lte.treeview collapsed.lte.treeview",
    "#homeMenu .menu-folder",
    function () {
        HomeMenuCookieKaydet();
    });

$(document).on("expanded.lte.treeview collapsed.lte.treeview",
    "#basvuruMenu .menu-folder",
    function () {
        BasvuruMenuCookieKaydet();
    });
