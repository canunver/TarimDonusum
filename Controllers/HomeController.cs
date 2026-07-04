using System.Diagnostics;
using System.Reflection;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using TarimDonusum.Araclar;
using TarimDonusum.FrameWork;
using TarimDonusum.FrameWork.Captcha;
using TarimDonusum.FrameWork.Logging;
using TarimDonusum.IsKurallari;
using TarimDonusum.Models;
using TarimDonusum.Servisler;
using TarimDonusum.ViewModels.Home;

namespace TarimDonusum.Controllers
{
    public class HomeController : BMYController
    {
        private const string HomeArkaPlanSessionKey = "Home.ArkaPlanResmi";
        private const string EpostaKodSessionKey = "YeniKullanici.EpostaKod";
        private const string EpostaKodZamanSessionKey = "YeniKullanici.EpostaKodZaman";
        private const string EpostaSessionKey = "YeniKullanici.Eposta";
        private const string EpostaDogrulandiSessionKey = "YeniKullanici.EpostaDogrulandi";

        private const string TelefonKodSessionKey = "YeniKullanici.TelefonKod";
        private const string TelefonKodZamanSessionKey = "YeniKullanici.TelefonKodZaman";
        private const string TelefonSessionKey = "YeniKullanici.Telefon";
        private const string TelefonDogrulandiSessionKey = "YeniKullanici.TelefonDogrulandi";
        private static readonly TimeSpan DogrulamaKoduGecerlilikSuresi = TimeSpan.FromMinutes(3);

        private readonly CaptchaGenerator _captcha;
        private readonly KullaniciIsKurallari _kullaniciIsKurallari;
        private readonly IMailServisi _mailServisi;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _environment;

        public HomeController(
            ILoggerFactory loggerFactory,
            IStringLocalizer<SharedResource> localizer,
            CaptchaGenerator captcha,
            KullaniciIsKurallari kullaniciIsKurallari,
            IMailServisi mailServisi,
            IConfiguration configuration,
            IWebHostEnvironment environment)
            : base(loggerFactory, localizer)
        {
            _captcha = captcha;
            _kullaniciIsKurallari = kullaniciIsKurallari;
            _mailServisi = mailServisi;
            _configuration = configuration;
            _environment = environment;
        }

        public static string RastgeleResim()
        {
            string klasor = Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot",
                "img",
                "home");

            string[] dosyalar = Directory.GetFiles(klasor, "*.jpg");
            string secilen = dosyalar[Random.Shared.Next(dosyalar.Length)];
            return "/img/home/" + Path.GetFileName(secilen);
        }

        public static string ArkaPlanResmi(HttpContext httpContext)
        {
            string? sessionResim = httpContext.Session.GetString(HomeArkaPlanSessionKey);
            if (!string.IsNullOrWhiteSpace(sessionResim))
                return sessionResim;

            string resim = RastgeleResim();
            httpContext.Session.SetString(HomeArkaPlanSessionKey, resim);

            return resim;
        }

        public IActionResult Index()
        {
            Log(LogLevel.Information, BMYEventID.Yok, null, "Ana sayfa açıldı.");
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> TestBasvuru()
        {
            if (!_environment.IsDevelopment())
                return NotFound();

            Sonuc<Kullanici> kullaniciSonuc = await _kullaniciIsKurallari.IlkAktifKullaniciyiOkuAsync();
            if (!kullaniciSonuc.basarili || kullaniciSonuc.nesne == null)
            {
                TempData["Mesaj"] = string.Join(" ", kullaniciSonuc.hatalar);
                return RedirectToAction(nameof(Index));
            }

            HttpContext.Session.SetString("KULLANICI_ID", kullaniciSonuc.nesne.Id.ToString());
            HttpContext.Session.SetString("KULLANICI_ADSOYAD", $"{kullaniciSonuc.nesne.Ad} {kullaniciSonuc.nesne.Soyad}");

            Log(
                LogLevel.Information,
                BMYEventID.Yok,
                null,
                "Development test girişi yapıldı. KullaniciId: {KullaniciId}",
                kullaniciSonuc.nesne.Id);

            return RedirectToAction("Index", "Basvuru");
        }

        [HttpGet]
        public IActionResult YeniKullanici()
        {
            _captcha.Yenile(HttpContext);

            YeniKullaniciViewModel model = new YeniKullaniciViewModel();
            DogrulamaDurumlariniViewDataYaz();

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> YeniKullanici(YeniKullaniciViewModel model)
        {
            DogrulamaDurumlariniViewDataYaz();
            YeniKullaniciNormalizeEt(model);

            if (!CaptchaGecerliMi(model.GuvenlikKodu))
            {
                ModelState.AddModelError(nameof(model.GuvenlikKodu), L["YeniKullanici.Hata.GuvenlikKoduHatali"]);
            }

            if (!EpostaDogrulandiMi())
            {
                ModelState.AddModelError("epostaDogrulamaKodu", L["YeniKullanici.Hata.EpostaDogrulanmalidir"]);
            }
            else if (!EpostaDogrulananDegerIleAyniMi(model.Kullanici.Eposta))
            {
                ModelState.AddModelError("epostaDogrulamaKodu", L["YeniKullanici.Hata.EpostaDogrulanmalidir"]);
            }

            if (!TelefonDogrulandiMi())
            {
                ModelState.AddModelError("telefonDogrulamaKodu", L["YeniKullanici.Hata.TelefonDogrulanmalidir"]);
            }
            else if (!TelefonDogrulananDegerIleAyniMi(model.Kullanici.Telefon))
            {
                ModelState.AddModelError("telefonDogrulamaKodu", L["YeniKullanici.Hata.TelefonDogrulanmalidir"]);
            }

            if (!model.SozlesmeKabulEdildi)
            {
                ModelState.AddModelError(nameof(model.SozlesmeKabulEdildi), L["YeniKullanici.Hata.SozlesmeKabulEdilmelidir"]);
            }

            if (!string.Equals(model.Kullanici.Parola, model.ParolaTekrar))
            {
                ModelState.AddModelError(nameof(model.ParolaTekrar), L["YeniKullanici.Hata.ParolalarEslesmiyor"]);
            }

            if (!ModelState.IsValid)
            {
                DogrulananDegerleriModeleYaz(model);
                return View(model);
            }

            Sonuc<int> sonuc = await _kullaniciIsKurallari.YeniBasvuruKullanicisiAsync(model.Kullanici);

            if (!sonuc.basarili)
            {
                foreach (string hata in sonuc.hatalar)
                {
                    ModelState.AddModelError("", hata);
                }

                DogrulananDegerleriModeleYaz(model);
                return View(model);
            }

            Log(
                LogLevel.Information,
                BMYEventID.KullaniciOlusturuldu,
                null,
                "Yeni kullanıcı kaydı alındı. TCKN: {TCKN}, Eposta: {Eposta}",
                model.Kullanici.TCKN,
                model.Kullanici.Eposta);

            YeniKullaniciSessionTemizle();
            TempData["Mesaj"] = L["YeniKullanici.Bilgi.KayitBasarili"].ToString();

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EpostaKoduGonder(YeniKullaniciViewModel model)
        {
            DogrulamaDurumlariniViewDataYaz();

            if (EpostaDogrulandiMi())
            {
                DogrulananDegerleriModeleYaz(model);
                return Json(DogrulamaCevabi(true, "", model.Kullanici.Eposta, null));
            }

            if (!CaptchaGecerliMi(model.GuvenlikKodu))
            {
                return Json(DogrulamaCevabi(false, L["YeniKullanici.Hata.GuvenlikKoduHatali"].ToString(), null, null, nameof(model.GuvenlikKodu)));
            }

            if (string.IsNullOrWhiteSpace(model.Kullanici.Eposta))
            {
                return Json(DogrulamaCevabi(false, L["YeniKullanici.Hata.EpostaGirilmelidir"].ToString(), null, null, "Kullanici.Eposta"));
            }

            model.Kullanici.Eposta = EpostaNormalize(model.Kullanici.Eposta);

            if (!OrtakFonksiyonlar.EPostaGecerliMi(model.Kullanici.Eposta))
            {
                return Json(DogrulamaCevabi(false, L["YeniKullanici.Hata.EpostaGirilmelidir"].ToString(), null, null, "Kullanici.Eposta"));
            }

            string kod = DogrulamaKoduUret();
            string mailHatasi = await _mailServisi.MailAtAsync(
                "",
                model.Kullanici.Eposta,
                L["YeniKullanici.Mail.EpostaDogrulamaKoduKonu"].ToString(),
                string.Format(L["YeniKullanici.Mail.EpostaDogrulamaKoduGovde"].ToString(), kod),
                false,
                false);

            if (!string.IsNullOrWhiteSpace(mailHatasi))
            {
                return Json(DogrulamaCevabi(false, mailHatasi, null, null, "Kullanici.Eposta"));
            }

            HttpContext.Session.SetString(EpostaKodSessionKey, kod);
            HttpContext.Session.SetString(EpostaKodZamanSessionKey, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString());
            HttpContext.Session.SetString(EpostaSessionKey, model.Kullanici.Eposta);
            HttpContext.Session.SetString(EpostaDogrulandiSessionKey, "false");

            DogrulamaDurumlariniViewDataYaz();

            return Json(DogrulamaCevabi(
                true,
                L["YeniKullanici.Bilgi.EpostaKoduOlusturuldu"].ToString(),
                model.Kullanici.Eposta,
                null,
                kalanSaniye: DogrulamaKoduKalanSaniye(EpostaKodZamanSessionKey)));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EpostaDogrula(YeniKullaniciViewModel model, string? epostaDogrulamaKodu)
        {
            DogrulamaDurumlariniViewDataYaz();

            if (EpostaDogrulandiMi())
            {
                DogrulananDegerleriModeleYaz(model);
                return Json(DogrulamaCevabi(true, "", model.Kullanici.Eposta, null));
            }

            if (!CaptchaGecerliMi(model.GuvenlikKodu))
            {
                return Json(DogrulamaCevabi(false, L["YeniKullanici.Hata.GuvenlikKoduHatali"].ToString(), null, null, nameof(model.GuvenlikKodu)));
            }

            string? sessionKod = HttpContext.Session.GetString(EpostaKodSessionKey);
            string? sessionEposta = HttpContext.Session.GetString(EpostaSessionKey);
            string girilenEposta = EpostaNormalize(model.Kullanici.Eposta);

            if (EpostaKoduSuresiDolduMu())
            {
                EpostaDogrulamaSessionTemizle();
                return Json(DogrulamaCevabi(false, L["YeniKullanici.Hata.EpostaDogrulamaKoduHatali"].ToString(), null, null, nameof(epostaDogrulamaKodu)));
            }

            if (string.IsNullOrWhiteSpace(sessionKod) ||
                string.IsNullOrWhiteSpace(sessionEposta) ||
                !string.Equals(sessionEposta, girilenEposta, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(sessionKod, epostaDogrulamaKodu?.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                return Json(DogrulamaCevabi(false, L["YeniKullanici.Hata.EpostaDogrulamaKoduHatali"].ToString(), null, null, nameof(epostaDogrulamaKodu)));
            }

            model.Kullanici.Eposta = girilenEposta;
            HttpContext.Session.SetString(EpostaDogrulandiSessionKey, "true");
            DogrulamaDurumlariniViewDataYaz();

            return Json(DogrulamaCevabi(
                true,
                L["YeniKullanici.Bilgi.EpostaDogrulandi"].ToString(),
                model.Kullanici.Eposta,
                null,
                epostaDogrulandi: true));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult TelefonKoduGonder(YeniKullaniciViewModel model)
        {
            DogrulamaDurumlariniViewDataYaz();

            if (TelefonDogrulandiMi())
            {
                DogrulananDegerleriModeleYaz(model);
                return Json(DogrulamaCevabi(true, "", null, model.Kullanici.Telefon));
            }

            if (!EpostaDogrulandiMi())
            {
                return Json(DogrulamaCevabi(false, L["YeniKullanici.Hata.EpostaDogrulanmalidir"].ToString(), null, null, "epostaDogrulamaKodu"));
            }

            if (!CaptchaGecerliMi(model.GuvenlikKodu))
            {
                return Json(DogrulamaCevabi(false, L["YeniKullanici.Hata.GuvenlikKoduHatali"].ToString(), null, null, nameof(model.GuvenlikKodu)));
            }

            if (string.IsNullOrWhiteSpace(model.Kullanici.Telefon))
            {
                return Json(DogrulamaCevabi(false, L["YeniKullanici.Hata.TelefonGirilmelidir"].ToString(), null, null, "Kullanici.Telefon"));
            }

            string telefon = TelefonNormalize(model.Kullanici.Telefon);
            if (string.IsNullOrWhiteSpace(telefon))
            {
                return Json(DogrulamaCevabi(false, L["YeniKullanici.Hata.TelefonGirilmelidir"].ToString(), null, null, "Kullanici.Telefon"));
            }

            model.Kullanici.Telefon = telefon;

            string kod = TelefonDogrulamaKoduUret();

            HttpContext.Session.SetString(TelefonKodSessionKey, kod);
            HttpContext.Session.SetString(TelefonKodZamanSessionKey, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString());
            HttpContext.Session.SetString(TelefonSessionKey, model.Kullanici.Telefon);
            HttpContext.Session.SetString(TelefonDogrulandiSessionKey, "false");

            DogrulamaDurumlariniViewDataYaz();

            // TODO: Gerçek SMS servisi bağlanınca kod burada gönderilecek.
            return Json(DogrulamaCevabi(
                true,
                L["YeniKullanici.Bilgi.TelefonKoduOlusturuldu"].ToString(),
                null,
                model.Kullanici.Telefon,
                kalanSaniye: DogrulamaKoduKalanSaniye(TelefonKodZamanSessionKey)));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult TelefonDogrula(YeniKullaniciViewModel model, string? telefonDogrulamaKodu)
        {
            DogrulamaDurumlariniViewDataYaz();

            if (TelefonDogrulandiMi())
            {
                DogrulananDegerleriModeleYaz(model);
                return Json(DogrulamaCevabi(true, "", null, model.Kullanici.Telefon));
            }

            if (!EpostaDogrulandiMi())
            {
                return Json(DogrulamaCevabi(false, L["YeniKullanici.Hata.EpostaDogrulanmalidir"].ToString(), null, null, "epostaDogrulamaKodu"));
            }

            if (!CaptchaGecerliMi(model.GuvenlikKodu))
            {
                return Json(DogrulamaCevabi(false, L["YeniKullanici.Hata.GuvenlikKoduHatali"].ToString(), null, null, nameof(model.GuvenlikKodu)));
            }

            string? sessionKod = HttpContext.Session.GetString(TelefonKodSessionKey);
            string? sessionTelefon = HttpContext.Session.GetString(TelefonSessionKey);
            string girilenTelefon = TelefonNormalize(model.Kullanici.Telefon);

            if (TelefonKoduSuresiDolduMu())
            {
                TelefonDogrulamaSessionTemizle();
                return Json(DogrulamaCevabi(false, L["YeniKullanici.Hata.TelefonDogrulamaKoduHatali"].ToString(), null, null, nameof(telefonDogrulamaKodu)));
            }

            if (string.IsNullOrWhiteSpace(sessionKod) ||
                string.IsNullOrWhiteSpace(sessionTelefon) ||
                !string.Equals(sessionTelefon, girilenTelefon, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(sessionKod, telefonDogrulamaKodu?.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                return Json(DogrulamaCevabi(false, L["YeniKullanici.Hata.TelefonDogrulamaKoduHatali"].ToString(), null, null, nameof(telefonDogrulamaKodu)));
            }

            model.Kullanici.Telefon = girilenTelefon;
            HttpContext.Session.SetString(TelefonDogrulandiSessionKey, "true");
            DogrulamaDurumlariniViewDataYaz();

            return Json(DogrulamaCevabi(
                true,
                L["YeniKullanici.Bilgi.TelefonDogrulandi"].ToString(),
                null,
                model.Kullanici.Telefon,
                telefonDogrulandi: true));
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            IExceptionHandlerPathFeature? exceptionFeature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();
            if (exceptionFeature?.Error != null)
            {
                Log(
                    LogLevel.Error,
                    BMYEventID.Yok,
                    exceptionFeature.Error,
                    "Beklenmeyen uygulama hatası. Path: {Path}",
                    exceptionFeature.Path);
            }

            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        private bool CaptchaGecerliMi(string modelGuvenlikKodu)
        {
            return _captcha.Validate(HttpContext, modelGuvenlikKodu);
        }

        private static object DogrulamaCevabi(
            bool basarili,
            string mesaj,
            string? eposta,
            string? telefon,
            string? alan = null,
            int kalanSaniye = 0,
            bool epostaDogrulandi = false,
            bool telefonDogrulandi = false)
        {
            return new
            {
                basarili,
                mesaj,
                alan,
                eposta,
                telefon,
                kalanSaniye,
                epostaDogrulandi,
                telefonDogrulandi
            };
        }

        private static string DogrulamaKoduUret()
        {
            return Random.Shared.Next(100000, 999999).ToString();
        }

        private string TelefonDogrulamaKoduUret()
        {
            if (SmsServisiVarMi())
                return DogrulamaKoduUret();

            return "111111";
        }

        private bool SmsServisiVarMi()
        {
            return string.Equals(_configuration["Sms:ServisVar"], "true", StringComparison.OrdinalIgnoreCase) ||
                OrtakFonksiyonlar.Int32Yap(_configuration["Sms:ServisVar"], 0) > 0;
        }

        private bool EpostaDogrulandiMi()
        {
            string? epostaDogrulandi = HttpContext.Session.GetString(EpostaDogrulandiSessionKey);

            return string.Equals(epostaDogrulandi, "true", StringComparison.OrdinalIgnoreCase);
        }

        private bool TelefonDogrulandiMi()
        {
            string? telefonDogrulandi = HttpContext.Session.GetString(TelefonDogrulandiSessionKey);

            return string.Equals(telefonDogrulandi, "true", StringComparison.OrdinalIgnoreCase);
        }

        private bool EpostaDogrulananDegerIleAyniMi(string? eposta)
        {
            string? sessionEposta = HttpContext.Session.GetString(EpostaSessionKey);

            return !string.IsNullOrWhiteSpace(sessionEposta) &&
                string.Equals(sessionEposta, EpostaNormalize(eposta), StringComparison.OrdinalIgnoreCase);
        }

        private bool TelefonDogrulananDegerIleAyniMi(string? telefon)
        {
            string? sessionTelefon = HttpContext.Session.GetString(TelefonSessionKey);

            return !string.IsNullOrWhiteSpace(sessionTelefon) &&
                string.Equals(sessionTelefon, TelefonNormalize(telefon), StringComparison.OrdinalIgnoreCase);
        }

        private void DogrulamaDurumlariniViewDataYaz()
        {
            ViewData["EpostaDogrulandi"] = EpostaDogrulandiMi();
            ViewData["TelefonDogrulandi"] = TelefonDogrulandiMi();
            ViewData["EpostaKodKalanSaniye"] = DogrulamaKoduKalanSaniye(EpostaKodZamanSessionKey);
            ViewData["TelefonKodKalanSaniye"] = DogrulamaKoduKalanSaniye(TelefonKodZamanSessionKey);
        }

        private void DogrulananDegerleriModeleYaz(YeniKullaniciViewModel model)
        {
            string? sessionEposta = HttpContext.Session.GetString(EpostaSessionKey);
            if (EpostaDogrulandiMi() && !string.IsNullOrWhiteSpace(sessionEposta))
            {
                model.Kullanici.Eposta = sessionEposta;
                ModelState.Remove("Kullanici.Eposta");
            }

            string? sessionTelefon = HttpContext.Session.GetString(TelefonSessionKey);
            if (TelefonDogrulandiMi() && !string.IsNullOrWhiteSpace(sessionTelefon))
            {
                model.Kullanici.Telefon = sessionTelefon;
                ModelState.Remove("Kullanici.Telefon");
            }
        }

        private void YeniKullaniciSessionTemizle()
        {
            EpostaDogrulamaSessionTemizle();
            TelefonDogrulamaSessionTemizle();
        }

        private void EpostaDogrulamaSessionTemizle()
        {
            HttpContext.Session.Remove(EpostaKodSessionKey);
            HttpContext.Session.Remove(EpostaKodZamanSessionKey);
            HttpContext.Session.Remove(EpostaSessionKey);
            HttpContext.Session.Remove(EpostaDogrulandiSessionKey);
        }

        private void TelefonDogrulamaSessionTemizle()
        {
            HttpContext.Session.Remove(TelefonKodSessionKey);
            HttpContext.Session.Remove(TelefonKodZamanSessionKey);
            HttpContext.Session.Remove(TelefonSessionKey);
            HttpContext.Session.Remove(TelefonDogrulandiSessionKey);
        }

        private bool EpostaKoduSuresiDolduMu()
        {
            return DogrulamaKoduSuresiDolduMu(EpostaKodZamanSessionKey);
        }

        private bool TelefonKoduSuresiDolduMu()
        {
            return DogrulamaKoduSuresiDolduMu(TelefonKodZamanSessionKey);
        }

        private bool DogrulamaKoduSuresiDolduMu(string zamanSessionKey)
        {
            return DogrulamaKoduKalanSaniye(zamanSessionKey) <= 0;
        }

        private int DogrulamaKoduKalanSaniye(string zamanSessionKey)
        {
            string? zamanDegeri = HttpContext.Session.GetString(zamanSessionKey);
            if (string.IsNullOrWhiteSpace(zamanDegeri))
                return 0;

            if (!long.TryParse(zamanDegeri, out long unixZaman))
                return 0;

            DateTimeOffset kodZamani = DateTimeOffset.FromUnixTimeSeconds(unixZaman);
            TimeSpan kalanSure = DogrulamaKoduGecerlilikSuresi - (DateTimeOffset.UtcNow - kodZamani);
            if (kalanSure <= TimeSpan.Zero)
                return 0;

            return (int)Math.Ceiling(kalanSure.TotalSeconds);
        }

        private static void YeniKullaniciNormalizeEt(YeniKullaniciViewModel model)
        {
            model.Kullanici.TCKN = model.Kullanici.TCKN?.Trim() ?? "";
            model.Kullanici.Eposta = EpostaNormalize(model.Kullanici.Eposta);

            string telefon = TelefonNormalize(model.Kullanici.Telefon);
            if (!string.IsNullOrWhiteSpace(telefon))
                model.Kullanici.Telefon = telefon;
        }

        private static string EpostaNormalize(string? eposta)
        {
            return eposta?.Trim() ?? "";
        }

        private static string TelefonNormalize(string? telefon)
        {
            return OrtakFonksiyonlar.TelNormalize(telefon);
        }

        [HttpPost]
        public async Task<IActionResult> Login1(string kulKod, string sifre, string captcha)
        {
            if (!CaptchaGecerliMi(captcha))
                return Json(LoginCevabi(false, L["Home.Hata.GuvenlikKoduHatali"].ToString()));

            Sonuc<Kullanici> kullaniciSonuc = await KullaniciOku(kulKod, sifre);
            Kullanici? kullanici = kullaniciSonuc.nesne;

            if (!kullaniciSonuc.basarili || kullanici == null)
                return Json(LoginCevabi(false, L["Home.Hata.KullaniciKodSifreHatali"].ToString()));

            var kod = Random.Shared.Next(100000, 999999).ToString();

            HttpContext.Session.SetString("LOGIN_KULKOD", kulKod);
            HttpContext.Session.SetString("LOGIN_VERIFY_CODE", kod);
            HttpContext.Session.SetString("LOGIN_VERIFY_EXPIRE", DateTime.Now.AddMinutes(5).ToString("O"));


            string mailHatasi = await _mailServisi.MailAtAsync(
                "",
                kullanici.Eposta,
                L["Home.Mail.DogrulamaKoduKonu"].ToString(),
                string.Format(L["Home.Mail.DogrulamaKoduGovde"].ToString(), kod),
                false,
                false);

            if (!string.IsNullOrWhiteSpace(mailHatasi))
            {
                return Json(DogrulamaCevabi(false, mailHatasi, null, null, "Kullanici.Eposta"));
            }

            HttpContext.Session.SetString("LOGIN_VERIFY_EXPIRE", DateTime.Now.AddMinutes(3).ToString("O"));
            return Json(LoginCevabi(true, L["Home.Bilgi.DogrulamaKoduGonderildi"].ToString(), kalanSaniye: 180));
        }

        private async Task<Sonuc<Kullanici>> KullaniciOku(string kulKod, string sifre)
        {
            return await _kullaniciIsKurallari.KullaniciOkuAsync(0, kulKod, sifre);
        }

        [HttpPost]
        public async Task<IActionResult> Login2(string kulKod, string sifre, string captcha, string dogrulamaKodu)
        {
            if (!CaptchaGecerliMi(captcha))
                return Json(LoginCevabi(false, L["Home.Hata.GuvenlikKoduHatali"].ToString()));

            Sonuc<Kullanici> kullaniciSonuc = await KullaniciOku(kulKod, sifre);

            if (!kullaniciSonuc.basarili || kullaniciSonuc.nesne == null)
                return Json(LoginCevabi(false, L["Home.Hata.KullaniciKodSifreHatali"].ToString()));

            var sessionKulKod = HttpContext.Session.GetString("LOGIN_KULKOD");
            var sessionKod = HttpContext.Session.GetString("LOGIN_VERIFY_CODE");
            var expireText = HttpContext.Session.GetString("LOGIN_VERIFY_EXPIRE");

            if (sessionKulKod != kulKod)
                return Json(LoginCevabi(false, L["Home.Hata.OturumDogrulamasiGecersiz"].ToString()));

            if (!DateTime.TryParse(expireText, out var expire) || DateTime.Now > expire)
                return Json(LoginCevabi(false, L["Home.Hata.DogrulamaKoduSuresiDoldu"].ToString()));

            if (sessionKod != dogrulamaKodu)
                return Json(LoginCevabi(false, L["Home.Hata.DogrulamaKoduHatali"].ToString()));

            HttpContext.Session.Remove("LOGIN_VERIFY_CODE");
            HttpContext.Session.Remove("LOGIN_VERIFY_EXPIRE");
            HttpContext.Session.SetString("KULLANICI_ID", kullaniciSonuc.nesne.Id.ToString());
            HttpContext.Session.SetString("KULLANICI_ADSOYAD", $"{kullaniciSonuc.nesne.Ad} {kullaniciSonuc.nesne.Soyad}");

            // Burada gerçek login cookie işlemi yapılacak
            // await HttpContext.SignInAsync(...);

            return Json(LoginCevabi(true, L["Home.Bilgi.GirisBasarili"].ToString(), Url.Action("Index", "Basvuru")));
        }

        private static object LoginCevabi(bool basarili, string mesaj, string? redirectUrl = null, int kalanSaniye = 0)
        {
            return new
            {
                basarili,
                mesaj,
                redirectUrl,
                kalanSaniye
            };
        }

    }
}
