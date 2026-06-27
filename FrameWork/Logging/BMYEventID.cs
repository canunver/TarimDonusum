namespace TarimDonusum.FrameWork.Logging
{
    /// <summary>
    /// Windows Event Viewer Event ID'leri
    /// </summary>
    public enum BMYEventID : int
    {
        Yok = 0,

        //1000-1999  Uygulama
        UygulamaBasladi = 1000,
        UygulamaSonlandi = 1001,

        //2000-2999  Güvenlik

        //3000-3999  Kullanıcı
        BasariliGiris = 3000,
        Cikis = 3001,
        BasarisizGiris = 3002,
        GirisSaldirisi = 3003,

        //4000-4999  Başvuru
        BasvuruOlusturuldu = 4000,
        BasvuruGonderildi = 4001,

        //5000-5999  Belge

        //6000-6999  Ödeme

        //7000-7999  Yönetim

        //8000-8999  Kullanıcı Yönetim
        KullaniciOlusturuldu = 8000,
        KullaniciPasifYapildi = 8001,
        KullaniciAktifYapildi = 8002,
        KullaniciRolleriDegisti = 8003,
        KullaniciSifresiDegisti = 8004,
        KullaniciBilgileriDegisti = 8005,


        //9000-9999  Sistem
        VeriTabaninaErisilemedi = 9000,
        VeriTabaniHatasi = 9001,
        VeriTabaniBeklenmeyenHata = 9002
    }
}
