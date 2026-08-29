using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace TarimDonusum.IsKurallari
{
    public static class VTGuncelle
    {
        private sealed record VTKomut(int KomutNo, string SqlKomut);

        private static readonly VTKomut[] Komutlar =
        [
            new(1,
                @"CREATE TABLE dbo.Kullanici(
                        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Kullanici PRIMARY KEY,
                        TCKN NVARCHAR(11) NOT NULL,
                        Ad NVARCHAR(100) NOT NULL,
                        Soyad NVARCHAR(100) NOT NULL,
                        DogumTarihi DATETIME NOT NULL,
                        Cinsiyet NVARCHAR(20) NOT NULL,
                        Eposta NVARCHAR(256) NOT NULL,
                        Telefon NVARCHAR(30) NOT NULL,
                        ParolaHash NVARCHAR(500) NOT NULL,
                        KayitTarihi DATETIME NOT NULL CONSTRAINT DF_Kullanici_KayitTarihi DEFAULT GETDATE(),
                        Aktif INT NOT NULL CONSTRAINT DF_Kullanici_Aktif DEFAULT 1
                    );

                    CREATE UNIQUE INDEX UX_Kullanici_TCKN ON dbo.Kullanici(TCKN);
                    CREATE UNIQUE INDEX UX_Kullanici_Eposta ON dbo.Kullanici(Eposta);
                    CREATE UNIQUE INDEX UX_Kullanici_Telefon ON dbo.Kullanici(Telefon);
                "),
            new(2,
                @"CREATE TABLE dbo.KullaniciYetki (
                        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_KullaniciYetki PRIMARY KEY,
                        KullaniciId INT NOT NULL,
                        Rol INT NOT NULL CONSTRAINT CK_KullaniciYetki_Rol CHECK (Rol IN (1, 2)),
                        YetkiKodu INT NOT NULL,
                        Birim INT NULL,
                        CONSTRAINT FK_KullaniciYetki_Kullanici
                            FOREIGN KEY (KullaniciId) REFERENCES dbo.Kullanici(Id)
                    );

                    CREATE INDEX IX_KullaniciYetki_KullaniciId ON dbo.KullaniciYetki(KullaniciId);
                    CREATE UNIQUE INDEX UX_KullaniciYetki_KullaniciRolBirim
                        ON dbo.KullaniciYetki(KullaniciId, Rol, Birim);
                "),
            new(3,
                @"CREATE TABLE dbo.KullaniciLog(
                        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_KullaniciLog PRIMARY KEY,
                        KullaniciId INT NOT NULL,
                        IslemYapanKullaniciId INT NULL,
                        IslemTarihi DATETIME NOT NULL CONSTRAINT DF_KullaniciLog_IslemTarihi DEFAULT GETDATE(),
                        Islem NVARCHAR(100) NOT NULL,
                        JsonText NVARCHAR(MAX) NOT NULL,
                        CONSTRAINT FK_KullaniciLog_Kullanici
                            FOREIGN KEY (KullaniciId) REFERENCES dbo.Kullanici(Id),
                        CONSTRAINT FK_KullaniciLog_IslemYapanKullanici
                            FOREIGN KEY (IslemYapanKullaniciId) REFERENCES dbo.Kullanici(Id)
                    );

                    CREATE INDEX IX_KullaniciLog_KullaniciId ON dbo.KullaniciLog(KullaniciId);
                    CREATE INDEX IX_KullaniciLog_IslemYapanKullaniciId ON dbo.KullaniciLog(IslemYapanKullaniciId);
                    CREATE INDEX IX_KullaniciLog_IslemTarihi ON dbo.KullaniciLog(IslemTarihi);
                "),
            new(4,
                @"CREATE TABLE dbo.Donem(
                        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Donem PRIMARY KEY,
                        Yil INT NOT NULL,
                        Ad NVARCHAR(150) NOT NULL,
                        BasvuruyaAcikMi INT NOT NULL CONSTRAINT DF_Donem_BasvuruyaAcikMi DEFAULT 0,
                        BasvuruBaslangicTarihi DATETIME NULL,
                        BasvuruBitisTarihi DATETIME NULL,
                        OnBasvuruBitisTarihi DATETIME NULL,
                        MinimumYatirimTutari DECIMAL(18,2) NULL,
                        MaksimumYatirimTutari DECIMAL(18,2) NULL,
                        MaksimumDestekTutari DECIMAL(18,2) NULL,
                        DestekOrani DECIMAL(5,2) NULL,
                        Aciklama NVARCHAR(MAX) NOT NULL CONSTRAINT DF_Donem_Aciklama DEFAULT N''
                    );

                    CREATE UNIQUE INDEX UX_Donem_Ad ON dbo.Donem(Ad);
                    CREATE INDEX IX_Donem_BasvuruyaAcikMi ON dbo.Donem(BasvuruyaAcikMi);
                "),
            new(5,
                @"CREATE TABLE dbo.Firma
                    (
                        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Firma PRIMARY KEY,
                        VergiKimlikNo NVARCHAR(20) NOT NULL,
                        TicaretUnvani NVARCHAR(250) NOT NULL,
                        TicaretSicilNo NVARCHAR(100) NOT NULL CONSTRAINT DF_Firma_TicaretSicilNo DEFAULT N'',
                        KurulusTarihi DATETIME NULL,
                        MersisNo NVARCHAR(50) NOT NULL CONSTRAINT DF_Firma_MersisNo DEFAULT N'',
                        NaceKodu NVARCHAR(50) NOT NULL CONSTRAINT DF_Firma_NaceKodu DEFAULT N'',
                        WebSitesi NVARCHAR(250) NOT NULL CONSTRAINT DF_Firma_WebSitesi DEFAULT N'',
                        Telefon NVARCHAR(30) NOT NULL CONSTRAINT DF_Firma_Telefon DEFAULT N'',
                        KepAdresi NVARCHAR(250) NOT NULL CONSTRAINT DF_Firma_KepAdresi DEFAULT N'',
                        Eposta NVARCHAR(256) NOT NULL CONSTRAINT DF_Firma_Eposta DEFAULT N'',
                        FaaliyetKonusu NVARCHAR(MAX) NOT NULL CONSTRAINT DF_Firma_FaaliyetKonusu DEFAULT N'',
                        Adres NVARCHAR(MAX) NOT NULL CONSTRAINT DF_Firma_Adres DEFAULT N''
                    );

                    CREATE UNIQUE INDEX UX_Firma_VergiKimlikNo ON dbo.Firma(VergiKimlikNo);
                "),
            new(6,
                @"CREATE TABLE dbo.FirmaKullanici
                    (
                        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_FirmaKullanici PRIMARY KEY,
                        FirmaId INT NOT NULL,
                        KullaniciId INT NOT NULL,
                        Aktif INT NOT NULL CONSTRAINT DF_FirmaKullanici_Aktif DEFAULT 1,
                        IliskiTarihi DATETIME NOT NULL CONSTRAINT DF_FirmaKullanici_IliskiTarihi DEFAULT GETDATE(),
                        IliskiyiKuranKullaniciId INT NULL,
                        CONSTRAINT FK_FirmaKullanici_Firma
                            FOREIGN KEY (FirmaId) REFERENCES dbo.Firma(Id),
                        CONSTRAINT FK_FirmaKullanici_Kullanici
                            FOREIGN KEY (KullaniciId) REFERENCES dbo.Kullanici(Id),
                        CONSTRAINT FK_FirmaKullanici_IliskiyiKuranKullanici
                            FOREIGN KEY (IliskiyiKuranKullaniciId) REFERENCES dbo.Kullanici(Id)
                    );

                    CREATE UNIQUE INDEX UX_FirmaKullanici_FirmaKullanici ON dbo.FirmaKullanici(FirmaId, KullaniciId);
                    CREATE INDEX IX_FirmaKullanici_KullaniciId ON dbo.FirmaKullanici(KullaniciId);
                    CREATE INDEX IX_FirmaKullanici_Aktif ON dbo.FirmaKullanici(Aktif);
                "),
            new(7,
                @"CREATE TABLE dbo.FirmaLog
                    (
                        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_FirmaLog PRIMARY KEY,
                        FirmaId INT NOT NULL,
                        KullaniciId INT NOT NULL,
                        IslemTarihi DATETIME NOT NULL CONSTRAINT DF_FirmaLog_IslemTarihi DEFAULT GETDATE(),
                        Islem NVARCHAR(100) NOT NULL,
                        JsonText NVARCHAR(MAX) NOT NULL,
                        CONSTRAINT FK_FirmaLog_Firma
                            FOREIGN KEY (FirmaId) REFERENCES dbo.Firma(Id),
                        CONSTRAINT FK_FirmaLog_Kullanici
                            FOREIGN KEY (KullaniciId) REFERENCES dbo.Kullanici(Id)
                    );

                    CREATE INDEX IX_FirmaLog_FirmaId ON dbo.FirmaLog(FirmaId);
                    CREATE INDEX IX_FirmaLog_KullaniciId ON dbo.FirmaLog(KullaniciId);
                    CREATE INDEX IX_FirmaLog_IslemTarihi ON dbo.FirmaLog(IslemTarihi);
                "),
            new(8,
                @"CREATE TABLE dbo.Il
                    (
                        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Il PRIMARY KEY,
                        Kod INT NOT NULL,
                        Ad NVARCHAR(100) NOT NULL,
                        Aktif INT NOT NULL CONSTRAINT DF_Il_Aktif DEFAULT 1
                    );

                    CREATE UNIQUE INDEX UX_Il_Kod ON dbo.Il(Kod);
                    CREATE UNIQUE INDEX UX_Il_Ad ON dbo.Il(Ad);
                    CREATE INDEX IX_Il_Aktif ON dbo.Il(Aktif);
                "),
            new(9,
                @"CREATE TABLE dbo.Ilce
                    (
                        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Ilce PRIMARY KEY,
                        IlId INT NOT NULL,
                        Ad NVARCHAR(100) NOT NULL,
                        Aktif INT NOT NULL CONSTRAINT DF_Ilce_Aktif DEFAULT 1,
                        CONSTRAINT FK_Ilce_Il
                            FOREIGN KEY (IlId) REFERENCES dbo.Il(Id)
                    );

                    CREATE UNIQUE INDEX UX_Ilce_IlAd ON dbo.Ilce(IlId, Ad);
                    CREATE INDEX IX_Ilce_IlId ON dbo.Ilce(IlId);
                    CREATE INDEX IX_Ilce_Aktif ON dbo.Ilce(Aktif);
                "),
            new(10,
                @"CREATE TABLE dbo.BasvuruAna
                    (
                        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_BasvuruAna PRIMARY KEY,
                        FirmaId INT NOT NULL,
                        DonemId INT NOT NULL,
                        IlId INT NOT NULL,
                        Durum INT NOT NULL CONSTRAINT DF_BasvuruAna_Durum DEFAULT 1,
                        CONSTRAINT FK_BasvuruAna_Firma
                            FOREIGN KEY (FirmaId) REFERENCES dbo.Firma(Id),
                        CONSTRAINT FK_BasvuruAna_Donem
                            FOREIGN KEY (DonemId) REFERENCES dbo.Donem(Id),
                        CONSTRAINT FK_BasvuruAna_Il
                            FOREIGN KEY (IlId) REFERENCES dbo.Il(Id)
                    );

                    CREATE INDEX IX_BasvuruAna_FirmaId ON dbo.BasvuruAna(FirmaId);
                    CREATE INDEX IX_BasvuruAna_DonemId ON dbo.BasvuruAna(DonemId);
                    CREATE INDEX IX_BasvuruAna_IlId ON dbo.BasvuruAna(IlId);
                "),
            new(11,
                @"CREATE TABLE dbo.Basvuru(
                        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Basvuru PRIMARY KEY,
                        BasvuruAnaId INT NOT NULL,
                        RevizyonNo INT NOT NULL CONSTRAINT DF_Basvuru_RevizyonNo DEFAULT 0,
                        SiraNo INT NOT NULL CONSTRAINT DF_Basvuru_SiraNo DEFAULT 1,
                        BasvuruKonusu NVARCHAR(250) NULL,
                        BasvuruSahibiTuru INT NULL,
                        HukukiTurSirketTuru INT NULL,
                        YonetimKuruluUyeleriAdliSicilKisiler NVARCHAR(MAX) NULL,
                        SonIkiYildirFaalMi INT NULL,
                        YatirimAdi NVARCHAR(250) NULL,
                        YatirimTuru INT NULL,
                        YatiriminAmaci NVARCHAR(MAX) NULL,
                        OzelSektorPayi DECIMAL(5,2) NULL,
                        BagliOrtakIsletmeVarMi INT NULL,
                        BagliOrtakAciklama NVARCHAR(MAX) NULL,
                        BagliOrtakUnvani NVARCHAR(250) NULL,
                        BagliOrtakKimlikNo NVARCHAR(50) NULL,
                        BagliOrtakOncekiYilNetSatis DECIMAL(18,2) NULL,
                        BagliOrtakSonYilNetSatis DECIMAL(18,2) NULL,
                        BagliOrtakOncekiYilAktifToplami DECIMAL(18,2) NULL,
                        BagliOrtakSonYilAktifToplami DECIMAL(18,2) NULL,
                        ToplamYatirimTutari DECIMAL(18,2) NULL,
                        UygunHarcamaTutari DECIMAL(18,2) NULL,
                        TalepEdilenDestekTutari DECIMAL(18,2) NULL,
                        TalepEdilenFinansmanOrani DECIMAL(5,2) NULL,
                        OnBasvuruSahibiKatkisi DECIMAL(18,2) NULL,
                        BasvuruSahibiKatkisi DECIMAL(18,2) NULL,
                        TalepEdilenVadeSuresiAy INT NULL,
                        DestekOrani DECIMAL(5,2) NULL,
                        DigerFinansmanKaynaklariAciklama NVARCHAR(MAX) NULL,
                        PikkListesiJson NVARCHAR(MAX) NULL,
                        YatirimOzetiJson NVARCHAR(MAX) NULL,
                        DbCtpTeknikProjeJson NVARCHAR(MAX) NULL,
                        CevreselSosyalJson NVARCHAR(MAX) NULL,
                        IrtibatKisi NVARCHAR(150) NULL,
                        IrtibatUnvan NVARCHAR(100) NULL,
                        IrtibatTelefon NVARCHAR(30) NULL,
                        IrtibatePosta NVARCHAR(256) NULL,
                        IrtibatAdres NVARCHAR(1000) NULL,
                        IrtibatYetkiliKisiler NVARCHAR(1000) NULL,
                        OncekiYilNetSatis DECIMAL(18,2) NULL,
                        SonYilNetSatis DECIMAL(18,2) NULL,
                        OncekiYilAktifToplami DECIMAL(18,2) NULL,
                        SonYilAktifToplami DECIMAL(18,2) NULL,
                        BagimsizDenetimeTabiMi INT NULL,
                        DenetimDosyaAdi NVARCHAR(260) NULL,
                        DenetimDosyaId INT NULL,
                        BelgePaketiDosyaAdi NVARCHAR(260) NULL,
                        BelgePaketiDosyaId INT NULL,
                        BelgePaketiAciklama NVARCHAR(1000) NULL,
                        BelgeBeyani NVARCHAR(20) NULL,
                        TaahhutDosyaAdi NVARCHAR(260) NULL,
                        TaahhutDosyaId INT NULL,
                        TaahhutAciklama NVARCHAR(1000) NULL,
                        TaahhutBeyanlarJson NVARCHAR(MAX) NULL,
                        CONSTRAINT FK_Basvuru_BasvuruAna
                            FOREIGN KEY (BasvuruAnaId) REFERENCES dbo.BasvuruAna(Id)
                    );

                    CREATE INDEX IX_Basvuru_BasvuruAnaId ON dbo.Basvuru(BasvuruAnaId);
                    CREATE UNIQUE INDEX UX_Basvuru_BasvuruAnaRevizyon ON dbo.Basvuru(BasvuruAnaId, RevizyonNo);
                "),
            new(12,
                @"CREATE TABLE dbo.BasvuruLog
                    (
                        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_BasvuruLog PRIMARY KEY,
                        BasvuruId INT NOT NULL,
                        KullaniciId INT NOT NULL,
                        IslemTarihi DATETIME NOT NULL CONSTRAINT DF_BasvuruLog_IslemTarihi DEFAULT GETDATE(),
                        Islem NVARCHAR(100) NOT NULL,
                        JsonText NVARCHAR(MAX) NOT NULL,
                        CONSTRAINT FK_BasvuruLog_Basvuru
                            FOREIGN KEY (BasvuruId) REFERENCES dbo.Basvuru(Id),
                        CONSTRAINT FK_BasvuruLog_Kullanici
                            FOREIGN KEY (KullaniciId) REFERENCES dbo.Kullanici(Id)
                    );

                    CREATE INDEX IX_BasvuruLog_BasvuruId ON dbo.BasvuruLog(BasvuruId);
                    CREATE INDEX IX_BasvuruLog_KullaniciId ON dbo.BasvuruLog(KullaniciId);
                    CREATE INDEX IX_BasvuruLog_IslemTarihi ON dbo.BasvuruLog(IslemTarihi);
                "),
            new(13,
                @"CREATE TABLE dbo.DegerZinciri
                    (
                        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_DegerZinciri PRIMARY KEY,
                        Kod NVARCHAR(50) NOT NULL,
                        Ad NVARCHAR(250) NOT NULL,
                        Aciklama NVARCHAR(MAX) NOT NULL CONSTRAINT DF_DegerZinciri_Aciklama DEFAULT N'',
                        Aktif INT NOT NULL CONSTRAINT DF_DegerZinciri_Aktif DEFAULT 1
                    );

                    CREATE UNIQUE INDEX UX_DegerZinciri_Kod ON dbo.DegerZinciri(Kod);
                    CREATE UNIQUE INDEX UX_DegerZinciri_Ad ON dbo.DegerZinciri(Ad);
                    CREATE INDEX IX_DegerZinciri_Aktif ON dbo.DegerZinciri(Aktif);
                "),
            new(14,
                @"CREATE TABLE dbo.DegerZinciriIl
                    (
                        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_DegerZinciriIl PRIMARY KEY,
                        DegerZinciriId INT NOT NULL,
                        IlId INT NOT NULL,
                        Aktif INT NOT NULL CONSTRAINT DF_DegerZinciriIl_Aktif DEFAULT 1,
                        CONSTRAINT FK_DegerZinciriIl_DegerZinciri
                            FOREIGN KEY (DegerZinciriId) REFERENCES dbo.DegerZinciri(Id),
                        CONSTRAINT FK_DegerZinciriIl_Il
                            FOREIGN KEY (IlId) REFERENCES dbo.Il(Id)
                    );

                    CREATE UNIQUE INDEX UX_DegerZinciriIl_DegerZinciriIl ON dbo.DegerZinciriIl(DegerZinciriId, IlId);
                    CREATE INDEX IX_DegerZinciriIl_IlId ON dbo.DegerZinciriIl(IlId);
                    CREATE INDEX IX_DegerZinciriIl_Aktif ON dbo.DegerZinciriIl(Aktif);
                "),
            new(15,
                @"CREATE TABLE dbo.DegerZinciriAsama
                    (
                        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_DegerZinciriAsama PRIMARY KEY,
                        DegerZinciriId INT NOT NULL,
                        SiraNo INT NOT NULL,
                        Ad NVARCHAR(250) NOT NULL,
                        Aciklama NVARCHAR(MAX) NOT NULL CONSTRAINT DF_DegerZinciriAsama_Aciklama DEFAULT N'',
                        Aktif INT NOT NULL CONSTRAINT DF_DegerZinciriAsama_Aktif DEFAULT 1,
                        CONSTRAINT FK_DegerZinciriAsama_DegerZinciri
                            FOREIGN KEY (DegerZinciriId) REFERENCES dbo.DegerZinciri(Id)
                    );

                    CREATE UNIQUE INDEX UX_DegerZinciriAsama_DegerZinciriSira ON dbo.DegerZinciriAsama(DegerZinciriId, SiraNo);
                    CREATE INDEX IX_DegerZinciriAsama_DegerZinciriId ON dbo.DegerZinciriAsama(DegerZinciriId);
                    CREATE INDEX IX_DegerZinciriAsama_Aktif ON dbo.DegerZinciriAsama(Aktif);
                "),
            new(16,
                @"CREATE TABLE dbo.BasvuruHarcamaTuru
                    (
                        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_BasvuruHarcamaTuru PRIMARY KEY,
                        BasvuruId INT NOT NULL,
                        HarcamaTuru INT NOT NULL,
                        CONSTRAINT FK_BasvuruHarcamaTuru_Basvuru
                            FOREIGN KEY (BasvuruId) REFERENCES dbo.Basvuru(Id)
                    );

                    CREATE UNIQUE INDEX UX_BasvuruHarcamaTuru_BasvuruHarcamaTuru ON dbo.BasvuruHarcamaTuru(BasvuruId, HarcamaTuru);
                    CREATE INDEX IX_BasvuruHarcamaTuru_BasvuruId ON dbo.BasvuruHarcamaTuru(BasvuruId);
                "),
            new(17,
                @"CREATE TABLE dbo.BasvuruDegerZinciriAsama
                    (
                        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_BasvuruDegerZinciriAsama PRIMARY KEY,
                        BasvuruId INT NOT NULL,
                        DegerZinciriAsamaId INT NULL,
                        YapilacakFaaliyetler NVARCHAR(500) NULL,
                        CONSTRAINT FK_BasvuruDegerZinciriAsama_Basvuru
                            FOREIGN KEY (BasvuruId) REFERENCES dbo.Basvuru(Id),
                        CONSTRAINT FK_BasvuruDegerZinciriAsama_DegerZinciriAsama
                            FOREIGN KEY (DegerZinciriAsamaId) REFERENCES dbo.DegerZinciriAsama(Id)
                    );

                    CREATE INDEX IX_BasvuruDegerZinciriAsama_BasvuruId ON dbo.BasvuruDegerZinciriAsama(BasvuruId);
                    CREATE INDEX IX_BasvuruDegerZinciriAsama_DegerZinciriAsamaId ON dbo.BasvuruDegerZinciriAsama(DegerZinciriAsamaId);
                "),
            new(18,
                @"CREATE TABLE dbo.BasvuruUygulamaAdresleri
                    (
                        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_BasvuruUygulamaAdresleri PRIMARY KEY,
                        BasvuruId INT NOT NULL,
                        SiraNo INT NOT NULL,
                        IlceId INT NULL,
                        TamAdres NVARCHAR(MAX) NOT NULL CONSTRAINT DF_BasvuruUygulamaAdresleri_TamAdres DEFAULT N'',
                        YatirimYeriStatusu INT NULL,
                        KiraVeyaTahsisSuresi INT NULL,
                        KiraTahsisBitisTarihi DATE NULL,
                        YapiRuhsatiDurumu INT NULL,
                        CONSTRAINT FK_BasvuruUygulamaAdresleri_Basvuru
                            FOREIGN KEY (BasvuruId) REFERENCES dbo.Basvuru(Id),
                        CONSTRAINT FK_BasvuruUygulamaAdresleri_Ilce
                            FOREIGN KEY (IlceId) REFERENCES dbo.Ilce(Id)
                    );

                    CREATE INDEX IX_BasvuruUygulamaAdresleri_BasvuruSira ON dbo.BasvuruUygulamaAdresleri(BasvuruId, SiraNo);
                    CREATE INDEX IX_BasvuruUygulamaAdresleri_BasvuruId ON dbo.BasvuruUygulamaAdresleri(BasvuruId);
                    CREATE INDEX IX_BasvuruUygulamaAdresleri_IlceId ON dbo.BasvuruUygulamaAdresleri(IlceId);
                "),
            new(19,
                @"CREATE TABLE dbo.BasvuruOrtaklar
                    (
                        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_BasvuruOrtaklar PRIMARY KEY,
                        BasvuruId INT NOT NULL,
                        SiraNo INT NOT NULL,
                        AdUnvan NVARCHAR(250) NOT NULL,
                        TcknVkn NVARCHAR(20) NULL,
                        KisiTuru NVARCHAR(30) NULL,
                        PayOrani DECIMAL(18,2) NULL,
                        HesabaDahilOran DECIMAL(18,2) NULL,
                        OzelKamuNiteligi NVARCHAR(30) NULL,
                        NihaiFaydalaniciBilgisi NVARCHAR(250) NULL,
                        UboKycBelgeAdi NVARCHAR(260) NULL,
                        UboKycDosyaId INT NULL,
                        OncekiYilNetSatis DECIMAL(18,2) NULL,
                        SonYilNetSatis DECIMAL(18,2) NULL,
                        OncekiYilAktifToplami DECIMAL(18,2) NULL,
                        SonYilAktifToplami DECIMAL(18,2) NULL,
                        CONSTRAINT FK_BasvuruOrtaklar_Basvuru
                            FOREIGN KEY (BasvuruId) REFERENCES dbo.Basvuru(Id)
                    );

                    CREATE INDEX IX_BasvuruOrtaklar_BasvuruSira ON dbo.BasvuruOrtaklar(BasvuruId, SiraNo);
                    CREATE INDEX IX_BasvuruOrtaklar_AdUnvan ON dbo.BasvuruOrtaklar(AdUnvan);
                    CREATE UNIQUE INDEX UX_BasvuruOrtaklar_Basvuru_TcknVkn
                        ON dbo.BasvuruOrtaklar(BasvuruId, TcknVkn)
                        WHERE TcknVkn IS NOT NULL AND TcknVkn <> N'';
                "),
            new(20,
                @"CREATE TABLE dbo.BasvuruAdliSicilKisiler
                    (
                        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_BasvuruAdliSicilKisiler PRIMARY KEY,
                        BasvuruId INT NOT NULL,
                        SiraNo INT NOT NULL,
                        Tckn NVARCHAR(20) NOT NULL,
                        Ad NVARCHAR(100) NOT NULL,
                        Soyad NVARCHAR(100) NOT NULL,
                        Gorev NVARCHAR(100) NOT NULL,
                        DosyaAdi NVARCHAR(260) NULL,
                        DosyaId INT NULL,
                        CONSTRAINT FK_BasvuruAdliSicilKisiler_Basvuru
                            FOREIGN KEY (BasvuruId) REFERENCES dbo.Basvuru(Id)
                    );

                    CREATE UNIQUE INDEX UX_BasvuruAdliSicilKisiler_Basvuru_Tckn
                        ON dbo.BasvuruAdliSicilKisiler(BasvuruId, Tckn);
                    CREATE INDEX IX_BasvuruAdliSicilKisiler_BasvuruSira
                        ON dbo.BasvuruAdliSicilKisiler(BasvuruId, SiraNo);
                "),
            new(21,
                @"CREATE TABLE dbo.DosyaBilgisi
                    (
                        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_DosyaBilgisi PRIMARY KEY,
                        ModulKod NVARCHAR(100) NOT NULL,
                        FormAd NVARCHAR(150) NOT NULL,
                        FormAnahtar NVARCHAR(150) NOT NULL,
                        DosyaNo INT NOT NULL,
                        DosyaAdi NVARCHAR(260) NOT NULL,
                        Buyukluk BIGINT NOT NULL,
                        IlkYuklemeTarihi DATETIME NOT NULL CONSTRAINT DF_DosyaBilgisi_IlkYuklemeTarihi DEFAULT GETDATE(),
                        STarihi DATETIME NOT NULL CONSTRAINT DF_DosyaBilgisi_STarihi DEFAULT GETDATE(),
                        Aciklama NVARCHAR(1000) NULL
                    );

                    CREATE UNIQUE INDEX UX_DosyaBilgisi_Anahtar
                        ON dbo.DosyaBilgisi(ModulKod, FormAd, FormAnahtar, DosyaNo);
                    CREATE INDEX IX_DosyaBilgisi_ModulKod
                        ON dbo.DosyaBilgisi(ModulKod);
                    CREATE INDEX IX_DosyaBilgisi_Form
                        ON dbo.DosyaBilgisi(ModulKod, FormAd, FormAnahtar);

                    CREATE TABLE dbo.DosyaIcerik
                    (
                        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_DosyaIcerik PRIMARY KEY,
                        DosyaId INT NOT NULL,
                        PaketNo INT NOT NULL,
                        PaketIcerik VARBINARY(MAX) NOT NULL,
                        CONSTRAINT FK_DosyaIcerik_DosyaBilgisi
                            FOREIGN KEY (DosyaId) REFERENCES dbo.DosyaBilgisi(Id) ON DELETE CASCADE
                    );

                    CREATE UNIQUE INDEX UX_DosyaIcerik_DosyaPaket
                        ON dbo.DosyaIcerik(DosyaId, PaketNo);

                    CREATE TABLE dbo.DosyaBilgisiLog
                    (
                        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_DosyaBilgisiLog PRIMARY KEY,
                        DosyaId INT NULL,
                        ModulKod NVARCHAR(100) NOT NULL,
                        FormAd NVARCHAR(150) NOT NULL,
                        FormAnahtar NVARCHAR(150) NOT NULL,
                        DosyaNo INT NOT NULL,
                        IslemTarihi DATETIME NOT NULL CONSTRAINT DF_DosyaBilgisiLog_IslemTarihi DEFAULT GETDATE(),
                        Islem NVARCHAR(100) NOT NULL,
                        JsonText NVARCHAR(MAX) NOT NULL
                    );

                    CREATE INDEX IX_DosyaBilgisiLog_DosyaId
                        ON dbo.DosyaBilgisiLog(DosyaId);
                    CREATE INDEX IX_DosyaBilgisiLog_Anahtar
                        ON dbo.DosyaBilgisiLog(ModulKod, FormAd, FormAnahtar, DosyaNo);
                    CREATE INDEX IX_DosyaBilgisiLog_IslemTarihi
                        ON dbo.DosyaBilgisiLog(IslemTarihi);
                "),
            new(22,
                @"CREATE TABLE dbo.Birim
                    (
                        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Birim PRIMARY KEY,
                        BirimAdi NVARCHAR(150) NOT NULL,
                        BirimTuru INT NOT NULL CONSTRAINT CK_Birim_BirimTuru CHECK (BirimTuru IN (1, 2)),
                        IlKod INT NULL,
                        SiraNo INT NOT NULL,
                        Aktif INT NOT NULL CONSTRAINT DF_Birim_Aktif DEFAULT 1,
                        CONSTRAINT CK_Birim_TasraIlKod
                            CHECK ((BirimTuru = 1 AND IlKod IS NULL) OR (BirimTuru = 2 AND IlKod IS NOT NULL)),
                        CONSTRAINT FK_Birim_IlKod
                            FOREIGN KEY (IlKod) REFERENCES dbo.Il(Kod)
                    );

                    CREATE INDEX IX_Birim_SiraNo ON dbo.Birim(SiraNo);
                    CREATE INDEX IX_Birim_Aktif ON dbo.Birim(Aktif);
                    CREATE INDEX IX_Birim_IlKod ON dbo.Birim(IlKod);
                "),
            new(23,
                @"IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_KullaniciYetki_Rol')
                    ALTER TABLE dbo.KullaniciYetki DROP CONSTRAINT CK_KullaniciYetki_Rol;

                  ALTER TABLE dbo.KullaniciYetki WITH CHECK
                    ADD CONSTRAINT CK_KullaniciYetki_Rol CHECK (Rol IN (1, 2, 3));

                  ALTER TABLE dbo.KullaniciYetki WITH CHECK
                    ADD CONSTRAINT FK_KullaniciYetki_Birim FOREIGN KEY (Birim) REFERENCES dbo.Birim(Id);
                "),
            new(24,
                @"IF OBJECT_ID(N'dbo.Birim', N'U') IS NULL
                  BEGIN
                    CREATE TABLE dbo.Birim
                    (
                        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Birim PRIMARY KEY,
                        BirimAdi NVARCHAR(150) NOT NULL,
                        BirimTuru INT NOT NULL CONSTRAINT CK_Birim_BirimTuru CHECK (BirimTuru IN (1, 2)),
                        IlKod INT NULL,
                        SiraNo INT NOT NULL,
                        Aktif INT NOT NULL CONSTRAINT DF_Birim_Aktif DEFAULT 1,
                        CONSTRAINT CK_Birim_TasraIlKod
                            CHECK ((BirimTuru = 1 AND IlKod IS NULL) OR (BirimTuru = 2 AND IlKod IS NOT NULL)),
                        CONSTRAINT FK_Birim_IlKod FOREIGN KEY (IlKod) REFERENCES dbo.Il(Kod)
                    );
                    CREATE INDEX IX_Birim_SiraNo ON dbo.Birim(SiraNo);
                    CREATE INDEX IX_Birim_Aktif ON dbo.Birim(Aktif);
                    CREATE INDEX IX_Birim_IlKod ON dbo.Birim(IlKod);
                  END;

                  IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_KullaniciYetki_Rol')
                    ALTER TABLE dbo.KullaniciYetki DROP CONSTRAINT CK_KullaniciYetki_Rol;
                  ALTER TABLE dbo.KullaniciYetki WITH CHECK
                    ADD CONSTRAINT CK_KullaniciYetki_Rol CHECK (Rol IN (1, 2, 3));

                  IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_KullaniciYetki_Birim')
                    ALTER TABLE dbo.KullaniciYetki WITH CHECK
                      ADD CONSTRAINT FK_KullaniciYetki_Birim FOREIGN KEY (Birim) REFERENCES dbo.Birim(Id);
                "),
            new(27,
                @"IF OBJECT_ID(N'dbo.KullaniciParolaToken', N'U') IS NOT NULL
                    DROP TABLE dbo.KullaniciParolaToken;"),
            new(28,
                @"IF COL_LENGTH(N'dbo.Basvuru', N'DenetimAnketi') IS NULL
                    ALTER TABLE dbo.Basvuru ADD DenetimAnketi NVARCHAR(MAX) NULL;
                  IF COL_LENGTH(N'dbo.Basvuru', N'DenetimGerekcesi') IS NULL
                    ALTER TABLE dbo.Basvuru ADD DenetimGerekcesi NVARCHAR(2000) NULL;
                  IF COL_LENGTH(N'dbo.Basvuru', N'DenetimSonucu') IS NULL
                    ALTER TABLE dbo.Basvuru ADD DenetimSonucu INT NULL;"),
            new(29,
                @";WITH Sirali AS
                  (
                      SELECT Id,
                             ROW_NUMBER() OVER
                                 (PARTITION BY BasvuruAnaId ORDER BY RevizyonNo DESC, Id DESC) - 1 AS YeniSiraNo
                      FROM dbo.Basvuru
                  )
                  UPDATE B SET SiraNo = S.YeniSiraNo
                  FROM dbo.Basvuru B
                  INNER JOIN Sirali S ON S.Id = B.Id;"),
            new(30,
                @"IF COL_LENGTH(N'dbo.Basvuru', N'DbCtpTeknikProjeJson') IS NULL
                    ALTER TABLE dbo.Basvuru ADD DbCtpTeknikProjeJson NVARCHAR(MAX) NULL;"),
            new(31,
                @"IF COL_LENGTH(N'dbo.Basvuru', N'TaahhutBeyanlarJson') IS NULL
                    ALTER TABLE dbo.Basvuru ADD TaahhutBeyanlarJson NVARCHAR(MAX) NULL;"),
            new(32,
                @"IF COL_LENGTH(N'dbo.Basvuru', N'TaahhutBeyanlarJson') IS NULL
                    ALTER TABLE dbo.Basvuru ADD TaahhutBeyanlarJson NVARCHAR(MAX) NULL;"),
            new(33,
                @"IF COL_LENGTH(N'dbo.Basvuru', N'SistemDenetimAnketi') IS NULL
                    ALTER TABLE dbo.Basvuru ADD SistemDenetimAnketi NVARCHAR(MAX) NULL;"),
            new(34,
                @"IF OBJECT_ID(N'dbo.BasvuruYatirimTuru', N'U') IS NULL
                  BEGIN
                    CREATE TABLE dbo.BasvuruYatirimTuru
                    (
                        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_BasvuruYatirimTuru PRIMARY KEY,
                        BasvuruId INT NOT NULL,
                        YatirimTuru INT NOT NULL,
                        CONSTRAINT FK_BasvuruYatirimTuru_Basvuru FOREIGN KEY (BasvuruId) REFERENCES dbo.Basvuru(Id)
                    );
                    CREATE UNIQUE INDEX UX_BasvuruYatirimTuru_BasvuruYatirimTuru ON dbo.BasvuruYatirimTuru(BasvuruId, YatirimTuru);
                    INSERT INTO dbo.BasvuruYatirimTuru(BasvuruId, YatirimTuru)
                    SELECT Id, YatirimTuru FROM dbo.Basvuru WHERE YatirimTuru IS NOT NULL AND YatirimTuru > 0;
                  END;"),
            new(35,
                @"IF COL_LENGTH(N'dbo.Basvuru', N'YatirimFaaliyetleri') IS NULL
                    ALTER TABLE dbo.Basvuru ADD YatirimFaaliyetleri NVARCHAR(MAX) NULL;
                  IF COL_LENGTH(N'dbo.Basvuru', N'YatirimGirdileri') IS NULL
                    ALTER TABLE dbo.Basvuru ADD YatirimGirdileri NVARCHAR(MAX) NULL;
                  IF COL_LENGTH(N'dbo.Basvuru', N'YatirimCiktilari') IS NULL
                    ALTER TABLE dbo.Basvuru ADD YatirimCiktilari NVARCHAR(MAX) NULL;"),
            new(36,
                @"IF COL_LENGTH(N'dbo.Donem', N'OnBasvuruBaslangicTarihi') IS NULL
                    ALTER TABLE dbo.Donem ADD OnBasvuruBaslangicTarihi DATETIME NULL;
                  IF COL_LENGTH(N'dbo.Donem', N'OnBasvuruCevrimKuru') IS NULL
                    ALTER TABLE dbo.Donem ADD OnBasvuruCevrimKuru DECIMAL(18,6) NULL;
                  IF COL_LENGTH(N'dbo.Donem', N'BasvuruCevrimKuru') IS NULL
                    ALTER TABLE dbo.Donem ADD BasvuruCevrimKuru DECIMAL(18,6) NULL;"),
            new(37,
                @"IF COL_LENGTH(N'dbo.Basvuru', N'YatirimSuresiAy') IS NULL
                    ALTER TABLE dbo.Basvuru ADD YatirimSuresiAy INT NULL;
                  IF COL_LENGTH(N'dbo.Basvuru', N'OdemeSuresiAy') IS NULL
                    ALTER TABLE dbo.Basvuru ADD OdemeSuresiAy INT NULL;"),
            new(38,
                @"IF COL_LENGTH(N'dbo.Basvuru', N'OdemesizDonemAy') IS NULL
                    ALTER TABLE dbo.Basvuru ADD OdemesizDonemAy INT NULL;
                  IF COL_LENGTH(N'dbo.Basvuru', N'OdemeSuresiAy') IS NOT NULL
                    EXEC(N'UPDATE dbo.Basvuru SET OdemesizDonemAy = OdemeSuresiAy WHERE OdemesizDonemAy IS NULL;');"),
            new(39,
                @"IF COL_LENGTH(N'dbo.Basvuru', N'OdemeSuresiAy') IS NULL
                    ALTER TABLE dbo.Basvuru ADD OdemeSuresiAy INT NULL;
                  IF COL_LENGTH(N'dbo.Basvuru', N'OdemesizDonemAy') IS NOT NULL
                    EXEC(N'UPDATE dbo.Basvuru SET OdemeSuresiAy = OdemesizDonemAy WHERE OdemeSuresiAy IS NULL;');"),
            new(40,
                @"IF COL_LENGTH(N'dbo.Basvuru', N'KayitTuru') IS NULL
                    ALTER TABLE dbo.Basvuru ADD KayitTuru INT NOT NULL CONSTRAINT DF_Basvuru_KayitTuru DEFAULT 1;
                  IF COL_LENGTH(N'dbo.Basvuru', N'OnBasvuruSonrasiDegisiklikVarMi') IS NULL
                    ALTER TABLE dbo.Basvuru ADD OnBasvuruSonrasiDegisiklikVarMi INT NULL;
                  IF COL_LENGTH(N'dbo.Basvuru', N'OnBasvuruSonrasiDegisiklikSebebi') IS NULL
                    ALTER TABLE dbo.Basvuru ADD OnBasvuruSonrasiDegisiklikSebebi NVARCHAR(2000) NULL;"),
            new(41,
                @"IF COL_LENGTH(N'dbo.Basvuru', N'OncekiYilIhracatSatis') IS NULL
                    ALTER TABLE dbo.Basvuru ADD OncekiYilIhracatSatis DECIMAL(18,2) NULL;
                  IF COL_LENGTH(N'dbo.Basvuru', N'SonYilIhracatSatis') IS NULL
                    ALTER TABLE dbo.Basvuru ADD SonYilIhracatSatis DECIMAL(18,2) NULL;
                  IF COL_LENGTH(N'dbo.Basvuru', N'OncekiYilCalisanSayisi') IS NULL
                    ALTER TABLE dbo.Basvuru ADD OncekiYilCalisanSayisi INT NULL;
                  IF COL_LENGTH(N'dbo.Basvuru', N'SonYilCalisanSayisi') IS NULL
                    ALTER TABLE dbo.Basvuru ADD SonYilCalisanSayisi INT NULL;
                  IF COL_LENGTH(N'dbo.Basvuru', N'MaliAciklama') IS NULL
                    ALTER TABLE dbo.Basvuru ADD MaliAciklama NVARCHAR(2000) NULL;
                  IF COL_LENGTH(N'dbo.BasvuruOrtaklar', N'IliskiTuru') IS NULL
                    ALTER TABLE dbo.BasvuruOrtaklar ADD IliskiTuru NVARCHAR(50) NULL;
                  IF COL_LENGTH(N'dbo.BasvuruOrtaklar', N'BelgeReferansi') IS NULL
                    ALTER TABLE dbo.BasvuruOrtaklar ADD BelgeReferansi NVARCHAR(500) NULL;"),
            new(42,
                @"IF COL_LENGTH(N'dbo.Basvuru', N'MaliBelgeReferanslariJson') IS NULL
                    ALTER TABLE dbo.Basvuru ADD MaliBelgeReferanslariJson NVARCHAR(MAX) NULL;"),
            new(43,
                @"IF COL_LENGTH(N'dbo.BasvuruAdliSicilKisiler', N'YetkiKapsami') IS NULL
                    ALTER TABLE dbo.BasvuruAdliSicilKisiler ADD YetkiKapsami NVARCHAR(500) NULL;
                  IF COL_LENGTH(N'dbo.BasvuruAdliSicilKisiler', N'Aciklama') IS NULL
                    ALTER TABLE dbo.BasvuruAdliSicilKisiler ADD Aciklama NVARCHAR(1000) NULL;
                  IF COL_LENGTH(N'dbo.BasvuruAdliSicilKisiler', N'ImzaYetkiDosyaAdi') IS NULL
                    ALTER TABLE dbo.BasvuruAdliSicilKisiler ADD ImzaYetkiDosyaAdi NVARCHAR(260) NULL;
                  IF COL_LENGTH(N'dbo.BasvuruAdliSicilKisiler', N'ImzaYetkiDosyaId') IS NULL
                    ALTER TABLE dbo.BasvuruAdliSicilKisiler ADD ImzaYetkiDosyaId INT NULL;"),            new(44,
                @"IF COL_LENGTH(N'dbo.BasvuruOrtaklar', N'DogumTarihi') IS NULL
                    ALTER TABLE dbo.BasvuruOrtaklar ADD DogumTarihi DATE NULL;
                  IF COL_LENGTH(N'dbo.BasvuruOrtaklar', N'Cinsiyet') IS NULL
                    ALTER TABLE dbo.BasvuruOrtaklar ADD Cinsiyet NVARCHAR(20) NULL;
                  IF COL_LENGTH(N'dbo.BasvuruOrtaklar', N'SahiplikNiteligi') IS NULL
                    ALTER TABLE dbo.BasvuruOrtaklar ADD SahiplikNiteligi NVARCHAR(30) NULL;"),
            new(45,
                @"IF COL_LENGTH(N'dbo.Basvuru', N'BasvuruKonusuTesis') IS NULL ALTER TABLE dbo.Basvuru ADD BasvuruKonusuTesis NVARCHAR(250) NULL;
                  IF COL_LENGTH(N'dbo.Basvuru', N'OrganizeAlanTuru') IS NULL ALTER TABLE dbo.Basvuru ADD OrganizeAlanTuru NVARCHAR(150) NULL;
                  IF COL_LENGTH(N'dbo.Basvuru', N'PlanlananBaslangicTarihi') IS NULL ALTER TABLE dbo.Basvuru ADD PlanlananBaslangicTarihi DATE NULL;
                  IF COL_LENGTH(N'dbo.Basvuru', N'PlanlananTamamlanmaTarihi') IS NULL ALTER TABLE dbo.Basvuru ADD PlanlananTamamlanmaTarihi DATE NULL;
                  IF COL_LENGTH(N'dbo.BasvuruUygulamaAdresleri', N'Koordinat') IS NULL ALTER TABLE dbo.BasvuruUygulamaAdresleri ADD Koordinat NVARCHAR(100) NULL;
                  IF COL_LENGTH(N'dbo.BasvuruUygulamaAdresleri', N'AdaParsel') IS NULL ALTER TABLE dbo.BasvuruUygulamaAdresleri ADD AdaParsel NVARCHAR(100) NULL;
                  IF COL_LENGTH(N'dbo.BasvuruUygulamaAdresleri', N'SegeKademesi') IS NULL ALTER TABLE dbo.BasvuruUygulamaAdresleri ADD SegeKademesi NVARCHAR(50) NULL;
                  IF COL_LENGTH(N'dbo.BasvuruUygulamaAdresleri', N'KullanimHakkiBaslangicTarihi') IS NULL ALTER TABLE dbo.BasvuruUygulamaAdresleri ADD KullanimHakkiBaslangicTarihi DATE NULL;
                  IF COL_LENGTH(N'dbo.BasvuruUygulamaAdresleri', N'DonemleriKapsiyorMu') IS NULL ALTER TABLE dbo.BasvuruUygulamaAdresleri ADD DonemleriKapsiyorMu BIT NULL;
                  IF COL_LENGTH(N'dbo.BasvuruUygulamaAdresleri', N'IzinTakvimAciklama') IS NULL ALTER TABLE dbo.BasvuruUygulamaAdresleri ADD IzinTakvimAciklama NVARCHAR(1000) NULL;
                  IF COL_LENGTH(N'dbo.BasvuruUygulamaAdresleri', N'AdresBelgeDosyaId') IS NULL ALTER TABLE dbo.BasvuruUygulamaAdresleri ADD AdresBelgeDosyaId INT NULL, AdresBelgeDosyaAdi NVARCHAR(260) NULL;
                  IF COL_LENGTH(N'dbo.BasvuruUygulamaAdresleri', N'KullanimHakkiDosyaId') IS NULL ALTER TABLE dbo.BasvuruUygulamaAdresleri ADD KullanimHakkiDosyaId INT NULL, KullanimHakkiDosyaAdi NVARCHAR(260) NULL;
                  IF COL_LENGTH(N'dbo.BasvuruUygulamaAdresleri', N'KanitDosyaId') IS NULL ALTER TABLE dbo.BasvuruUygulamaAdresleri ADD KanitDosyaId INT NULL, KanitDosyaAdi NVARCHAR(260) NULL;"),
            new(46,
                @"IF COL_LENGTH(N'dbo.Basvuru', N'IlDegerZinciriEslesmesi') IS NULL ALTER TABLE dbo.Basvuru ADD IlDegerZinciriEslesmesi NVARCHAR(50) NULL;
                  IF COL_LENGTH(N'dbo.Basvuru', N'TarimGidaBaglantiTuru') IS NULL ALTER TABLE dbo.Basvuru ADD TarimGidaBaglantiTuru NVARCHAR(100) NULL;
                  IF COL_LENGTH(N'dbo.Basvuru', N'TarimGidaBaglantiAciklamasi') IS NULL ALTER TABLE dbo.Basvuru ADD TarimGidaBaglantiAciklamasi NVARCHAR(2000) NULL;
                  IF COL_LENGTH(N'dbo.Basvuru', N'YatirimAlaniTipolojisi') IS NULL ALTER TABLE dbo.Basvuru ADD YatirimAlaniTipolojisi NVARCHAR(500) NULL;
                  IF COL_LENGTH(N'dbo.Basvuru', N'DegerZinciriUygunlukAciklamasi') IS NULL ALTER TABLE dbo.Basvuru ADD DegerZinciriUygunlukAciklamasi NVARCHAR(2000) NULL;
                  IF COL_LENGTH(N'dbo.Basvuru', N'OncelikliYatirimUyumu') IS NULL ALTER TABLE dbo.Basvuru ADD OncelikliYatirimUyumu NVARCHAR(50) NULL;
                  IF COL_LENGTH(N'dbo.Basvuru', N'OncelikliYatirimKonuKodu') IS NULL ALTER TABLE dbo.Basvuru ADD OncelikliYatirimKonuKodu NVARCHAR(500) NULL;
                  IF COL_LENGTH(N'dbo.Basvuru', N'IthalatBagimliligiUyumu') IS NULL ALTER TABLE dbo.Basvuru ADD IthalatBagimliligiUyumu NVARCHAR(50) NULL;
                  IF COL_LENGTH(N'dbo.Basvuru', N'IthalatBagimliligiUrunKodu') IS NULL ALTER TABLE dbo.Basvuru ADD IthalatBagimliligiUrunKodu NVARCHAR(500) NULL;
                  IF COL_LENGTH(N'dbo.Basvuru', N'HedefUrunlerPazarCiktisi') IS NULL ALTER TABLE dbo.Basvuru ADD HedefUrunlerPazarCiktisi NVARCHAR(2000) NULL;
                  IF COL_LENGTH(N'dbo.Basvuru', N'RekabetcilikAciklamasi') IS NULL ALTER TABLE dbo.Basvuru ADD RekabetcilikAciklamasi NVARCHAR(2000) NULL;"),
            new(47,
                @"IF COL_LENGTH(N'dbo.Basvuru', N'FinansmanParaBirimi') IS NULL ALTER TABLE dbo.Basvuru ADD FinansmanParaBirimi NVARCHAR(20) NULL;
                  IF COL_LENGTH(N'dbo.Basvuru', N'DigerFinansmanKaynaklari') IS NULL ALTER TABLE dbo.Basvuru ADD DigerFinansmanKaynaklari NVARCHAR(1000) NULL;
                  IF COL_LENGTH(N'dbo.Basvuru', N'OncekiRffOnayliTutar') IS NULL ALTER TABLE dbo.Basvuru ADD OncekiRffOnayliTutar DECIMAL(18,2) NULL;
                  IF COL_LENGTH(N'dbo.Basvuru', N'OncekiRffSozlesmesiKapaliMi') IS NULL ALTER TABLE dbo.Basvuru ADD OncekiRffSozlesmesiKapaliMi NVARCHAR(20) NULL;
                  IF COL_LENGTH(N'dbo.Basvuru', N'BankaTeminatMektubuSaglanabilirMi') IS NULL ALTER TABLE dbo.Basvuru ADD BankaTeminatMektubuSaglanabilirMi NVARCHAR(10) NULL;"),
            new(48,
                @"IF COL_LENGTH(N'dbo.Basvuru', N'TalepEdilenVadeSuresiYil') IS NOT NULL
                      AND COL_LENGTH(N'dbo.Basvuru', N'TalepEdilenVadeSuresiAy') IS NULL
                    EXEC sp_rename N'dbo.Basvuru.TalepEdilenVadeSuresiYil', N'TalepEdilenVadeSuresiAy', N'COLUMN';
                  IF COL_LENGTH(N'dbo.Basvuru', N'TalepEdilenVadeSuresiAy') IS NULL
                    ALTER TABLE dbo.Basvuru ADD TalepEdilenVadeSuresiAy INT NULL;
                  IF COL_LENGTH(N'dbo.Basvuru', N'TalepEdilenVadeSuresiYil') IS NOT NULL
                      AND COL_LENGTH(N'dbo.Basvuru', N'TalepEdilenVadeSuresiAy') IS NOT NULL
                    EXEC(N'UPDATE dbo.Basvuru SET TalepEdilenVadeSuresiAy = COALESCE(TalepEdilenVadeSuresiAy, TalepEdilenVadeSuresiYil);');"),
            new(49,
                @"CREATE TABLE dbo.BasvuruMakine(
                    Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_BasvuruMakine PRIMARY KEY,
                    BasvuruId INT NOT NULL, SiraNo INT NOT NULL, Ad NVARCHAR(250) NOT NULL,
                    Birim NVARCHAR(50) NOT NULL, Miktar DECIMAL(18,3) NOT NULL, Aciklama NVARCHAR(2000) NULL,
                    UzmanParaBirimi NVARCHAR(20) NULL, UzmanKur DECIMAL(18,6) NULL,
                    UzmanMinimumFiyat DECIMAL(18,2) NULL, UzmanMaksimumFiyat DECIMAL(18,2) NULL,
                    UzmanSecilenTeklifId INT NULL, UzmanOnerilenFiyatTl DECIMAL(18,2) NULL,
                    UzmanKontrolSonucu NVARCHAR(50) NULL, UzmanAciklama NVARCHAR(2000) NULL,
                    CONSTRAINT FK_BasvuruMakine_Basvuru FOREIGN KEY(BasvuruId) REFERENCES dbo.Basvuru(Id) ON DELETE CASCADE);
                  CREATE UNIQUE INDEX UX_BasvuruMakine_BasvuruSira ON dbo.BasvuruMakine(BasvuruId,SiraNo);
                  CREATE TABLE dbo.BasvuruMakineOzellik(
                    Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_BasvuruMakineOzellik PRIMARY KEY,
                    MakineId INT NOT NULL, SiraNo INT NOT NULL, Baslik NVARCHAR(250) NOT NULL,
                    AciklamaAsgariGereklilik NVARCHAR(2000) NOT NULL, ZorunluMu BIT NOT NULL,
                    CONSTRAINT FK_BasvuruMakineOzellik_Makine FOREIGN KEY(MakineId) REFERENCES dbo.BasvuruMakine(Id) ON DELETE CASCADE);
                  CREATE UNIQUE INDEX UX_BasvuruMakineOzellik_MakineSira ON dbo.BasvuruMakineOzellik(MakineId,SiraNo);
                  CREATE TABLE dbo.BasvuruMakineTeklif(
                    Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_BasvuruMakineTeklif PRIMARY KEY,
                    MakineId INT NOT NULL, SiraNo INT NOT NULL, BasvuruyaEsas BIT NOT NULL,
                    Tedarikci NVARCHAR(250) NOT NULL, Marka NVARCHAR(150) NULL, Model NVARCHAR(150) NULL,
                    ParaBirimi NVARCHAR(20) NOT NULL, Kur DECIMAL(18,6) NULL, BirimFiyat DECIMAL(18,2) NULL,
                    TeklifTarihi DATE NULL, GecerlilikTarihi DATE NULL, TeklifBelgesiDosyaId INT NULL,
                    TeklifBelgesiDosyaAdi NVARCHAR(260) NULL, Aciklama NVARCHAR(2000) NULL,
                    CONSTRAINT FK_BasvuruMakineTeklif_Makine FOREIGN KEY(MakineId) REFERENCES dbo.BasvuruMakine(Id) ON DELETE CASCADE);
                  CREATE UNIQUE INDEX UX_BasvuruMakineTeklif_MakineSira ON dbo.BasvuruMakineTeklif(MakineId,SiraNo);"),
            new(50,
                @"IF OBJECT_ID(N'dbo.BasvuruYatirimOnBilgi', N'U') IS NULL
                  BEGIN
                    CREATE TABLE dbo.BasvuruYatirimOnBilgi(
                      Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_BasvuruYatirimOnBilgi PRIMARY KEY,
                      BasvuruId INT NOT NULL, Tur INT NOT NULL, SiraNo INT NOT NULL,
                      Ad NVARCHAR(250) NOT NULL, Miktar DECIMAL(18,3) NULL, Birim NVARCHAR(50) NULL,
                      TekPanelGucu DECIMAL(18,3) NULL, TekPanelGucuBirim NVARCHAR(50) NULL,
                      ToplamGuc DECIMAL(18,3) NULL, ToplamGucBirim NVARCHAR(50) NULL,
                      CONSTRAINT CK_BasvuruYatirimOnBilgi_Tur CHECK(Tur BETWEEN 1 AND 5),
                      CONSTRAINT FK_BasvuruYatirimOnBilgi_Basvuru FOREIGN KEY(BasvuruId) REFERENCES dbo.Basvuru(Id) ON DELETE CASCADE);
                    CREATE UNIQUE INDEX UX_BasvuruYatirimOnBilgi_BasvuruTurSira ON dbo.BasvuruYatirimOnBilgi(BasvuruId,Tur,SiraNo);
                  END"),
            new(51,
                @"IF COL_LENGTH(N'dbo.BasvuruMakine',N'Marka') IS NULL ALTER TABLE dbo.BasvuruMakine ADD Marka NVARCHAR(150) NULL;
                  IF COL_LENGTH(N'dbo.BasvuruMakine',N'Model') IS NULL ALTER TABLE dbo.BasvuruMakine ADD Model NVARCHAR(150) NULL;
                  IF COL_LENGTH(N'dbo.BasvuruMakine',N'KapasiteOzellikleri') IS NULL ALTER TABLE dbo.BasvuruMakine ADD KapasiteOzellikleri NVARCHAR(2000) NULL;
                  IF COL_LENGTH(N'dbo.BasvuruMakine',N'YerlesimPlaniSiraNo') IS NULL ALTER TABLE dbo.BasvuruMakine ADD YerlesimPlaniSiraNo INT NULL;
                  IF COL_LENGTH(N'dbo.BasvuruMakine',N'KullanimAmaci') IS NULL ALTER TABLE dbo.BasvuruMakine ADD KullanimAmaci NVARCHAR(2000) NULL;
                  IF COL_LENGTH(N'dbo.BasvuruMakine',N'Durum') IS NULL ALTER TABLE dbo.BasvuruMakine ADD Durum NVARCHAR(50) NULL;
                  IF COL_LENGTH(N'dbo.BasvuruMakine',N'KapasiteSecimGerekcesi') IS NULL ALTER TABLE dbo.BasvuruMakine ADD KapasiteSecimGerekcesi NVARCHAR(4000) NULL;"),
            new(52,
                @"IF OBJECT_ID(N'dbo.BasvuruMakineUzmanDokuman', N'U') IS NULL
                  BEGIN
                    CREATE TABLE dbo.BasvuruMakineUzmanDokuman(
                      Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_BasvuruMakineUzmanDokuman PRIMARY KEY,
                      MakineId INT NOT NULL, SiraNo INT NOT NULL,
                      DokumanAdi NVARCHAR(250) NOT NULL, DokumanTuru NVARCHAR(100) NOT NULL,
                      KaynakTedarikci NVARCHAR(250) NOT NULL, BelgeTarihi DATE NULL,
                      Aciklama NVARCHAR(2000) NULL, DosyaId INT NULL, DosyaAdi NVARCHAR(260) NULL,
                      CONSTRAINT FK_BasvuruMakineUzmanDokuman_Makine FOREIGN KEY(MakineId) REFERENCES dbo.BasvuruMakine(Id) ON DELETE CASCADE);
                    CREATE UNIQUE INDEX UX_BasvuruMakineUzmanDokuman_MakineSira ON dbo.BasvuruMakineUzmanDokuman(MakineId,SiraNo);
                  END"),
            new(53,
                @"IF OBJECT_ID(N'dbo.BasvuruUrunSurec',N'U') IS NULL
                  BEGIN
                    CREATE TABLE dbo.BasvuruUrunSurec(Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_BasvuruUrunSurec PRIMARY KEY,BasvuruId INT NOT NULL,UrunId INT NOT NULL,SiraNo INT NOT NULL,SurecAdi NVARCHAR(250) NOT NULL,CONSTRAINT FK_BasvuruUrunSurec_Basvuru FOREIGN KEY(BasvuruId) REFERENCES dbo.Basvuru(Id) ON DELETE CASCADE,CONSTRAINT FK_BasvuruUrunSurec_Urun FOREIGN KEY(UrunId) REFERENCES dbo.BasvuruYatirimOnBilgi(Id));
                    CREATE UNIQUE INDEX UX_BasvuruUrunSurec_BasvuruUrunSira ON dbo.BasvuruUrunSurec(BasvuruId,UrunId,SiraNo);
                    CREATE TABLE dbo.BasvuruUrunSurecMakine(Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_BasvuruUrunSurecMakine PRIMARY KEY,SurecId INT NOT NULL,MakineId INT NOT NULL,SiraNo INT NOT NULL,Adet DECIMAL(18,3) NOT NULL,YerlesimPlaniNo NVARCHAR(100) NULL,GirdilerMiktarlar NVARCHAR(2000) NULL,CiktilarMiktarlar NVARCHAR(2000) NULL,IslemeKapasitesi NVARCHAR(500) NULL,GunlukCalismaSuresi DECIMAL(18,2) NULL,Aciklama NVARCHAR(2000) NULL,CONSTRAINT FK_BasvuruUrunSurecMakine_Surec FOREIGN KEY(SurecId) REFERENCES dbo.BasvuruUrunSurec(Id) ON DELETE CASCADE,CONSTRAINT FK_BasvuruUrunSurecMakine_Makine FOREIGN KEY(MakineId) REFERENCES dbo.BasvuruMakine(Id));
                    CREATE UNIQUE INDEX UX_BasvuruUrunSurecMakine_SurecSira ON dbo.BasvuruUrunSurecMakine(SurecId,SiraNo);
                  END"),
            new(54,
                @"IF COL_LENGTH(N'dbo.BasvuruUrunSurecMakine',N'GunlukCalismaSuresiBirimi') IS NULL
                    ALTER TABLE dbo.BasvuruUrunSurecMakine ADD GunlukCalismaSuresiBirimi NVARCHAR(10) NOT NULL CONSTRAINT DF_BasvuruUrunSurecMakine_SureBirimi DEFAULT N'Saat';"),
            new(55,
                @"IF OBJECT_ID(N'dbo.BasvuruBina',N'U') IS NULL
                  BEGIN
                    CREATE TABLE dbo.BasvuruBina(Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_BasvuruBina PRIMARY KEY,BasvuruId INT NOT NULL,SiraNo INT NOT NULL,Ad NVARCHAR(250) NOT NULL,MevcutYeni NVARCHAR(50) NULL,YatirimSekli NVARCHAR(100) NULL,DestekTalebi NVARCHAR(20) NULL,VaziyetPlaniNo NVARCHAR(100) NULL,CONSTRAINT FK_BasvuruBina_Basvuru FOREIGN KEY(BasvuruId) REFERENCES dbo.Basvuru(Id) ON DELETE CASCADE);
                    CREATE UNIQUE INDEX UX_BasvuruBina_BasvuruSira ON dbo.BasvuruBina(BasvuruId,SiraNo);
                    CREATE TABLE dbo.BasvuruBinaMahal(Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_BasvuruBinaMahal PRIMARY KEY,BinaId INT NOT NULL,SiraNo INT NOT NULL,MahalAdi NVARCHAR(250) NOT NULL,AlanM2 DECIMAL(18,2) NOT NULL,CONSTRAINT FK_BasvuruBinaMahal_Bina FOREIGN KEY(BinaId) REFERENCES dbo.BasvuruBina(Id) ON DELETE CASCADE);
                    CREATE UNIQUE INDEX UX_BasvuruBinaMahal_BinaSira ON dbo.BasvuruBinaMahal(BinaId,SiraNo);
                  END"),
            new(56,
                @"IF COL_LENGTH(N'dbo.Basvuru',N'IstihdamJson') IS NULL ALTER TABLE dbo.Basvuru ADD IstihdamJson NVARCHAR(MAX) NULL;
                  IF COL_LENGTH(N'dbo.Basvuru',N'IstihdamSgkDosyaId') IS NULL ALTER TABLE dbo.Basvuru ADD IstihdamSgkDosyaId INT NULL;
                  IF COL_LENGTH(N'dbo.Basvuru',N'IstihdamSgkDosyaAdi') IS NULL ALTER TABLE dbo.Basvuru ADD IstihdamSgkDosyaAdi NVARCHAR(260) NULL;"),
            new(57,
                @"IF OBJECT_ID(N'dbo.BasvuruIstihdamSatir',N'U') IS NULL
                  BEGIN
                    CREATE TABLE dbo.BasvuruIstihdamSatir(Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_BasvuruIstihdamSatir PRIMARY KEY,BasvuruId INT NOT NULL,SiraNo INT NOT NULL,BirimUnite NVARCHAR(250) NOT NULL,GorevUretimHatti NVARCHAR(250) NOT NULL,Cinsiyet NVARCHAR(20) NOT NULL,YasDurumu NVARCHAR(30) NOT NULL,MevcutCalisan DECIMAL(18,2) NOT NULL,NetCalisanArtisi DECIMAL(18,2) NOT NULL,BazAylikBrutUcret DECIMAL(18,2) NOT NULL,HedefAylikBrutUcret DECIMAL(18,2) NOT NULL,CONSTRAINT FK_BasvuruIstihdamSatir_Basvuru FOREIGN KEY(BasvuruId) REFERENCES dbo.Basvuru(Id) ON DELETE CASCADE);
                    CREATE UNIQUE INDEX UX_BasvuruIstihdamSatir_BasvuruSira ON dbo.BasvuruIstihdamSatir(BasvuruId,SiraNo);
                  END"),
            new(58, @"IF COL_LENGTH(N'dbo.Ilce',N'SegeKademesi') IS NULL ALTER TABLE dbo.Ilce ADD SegeKademesi INT NULL;"),
            new(59,
                @"IF OBJECT_ID(N'dbo.BasvuruTedarikciEntegrasyonu',N'U') IS NULL
                  BEGIN
                    CREATE TABLE dbo.BasvuruTedarikciEntegrasyonu(
                      Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_BasvuruTedarikciEntegrasyonu PRIMARY KEY,
                      BasvuruId INT NOT NULL, UrunId INT NOT NULL, TarimsalUrun NVARCHAR(250) NOT NULL,
                      IlId INT NOT NULL, IlceId INT NOT NULL, Birim NVARCHAR(50) NOT NULL,
                      MevcutYillikMiktar DECIMAL(18,3) NOT NULL, HedefYillikMiktar DECIMAL(18,3) NOT NULL,
                      MevcutKayitliCiftci INT NOT NULL, EklenecekKayitliCiftci INT NOT NULL,
                      TedarikSekli NVARCHAR(250) NOT NULL, DayanakBelgeDosyaId INT NULL,
                      DayanakBelgeDosyaAdi NVARCHAR(260) NULL, KisaAciklama NVARCHAR(1000) NULL,
                      CONSTRAINT FK_BasvuruTedarikciEntegrasyonu_Basvuru FOREIGN KEY(BasvuruId) REFERENCES dbo.Basvuru(Id) ON DELETE CASCADE,
                      CONSTRAINT FK_BasvuruTedarikciEntegrasyonu_Urun FOREIGN KEY(UrunId) REFERENCES dbo.BasvuruYatirimOnBilgi(Id),
                      CONSTRAINT FK_BasvuruTedarikciEntegrasyonu_Il FOREIGN KEY(IlId) REFERENCES dbo.Il(Id),
                      CONSTRAINT FK_BasvuruTedarikciEntegrasyonu_Ilce FOREIGN KEY(IlceId) REFERENCES dbo.Ilce(Id),
                      CONSTRAINT CK_BasvuruTedarikciEntegrasyonu_Degerler CHECK(MevcutYillikMiktar>=0 AND HedefYillikMiktar>=0 AND MevcutKayitliCiftci>=0 AND EklenecekKayitliCiftci>=0));
                    CREATE INDEX IX_BasvuruTedarikciEntegrasyonu_BasvuruUrun ON dbo.BasvuruTedarikciEntegrasyonu(BasvuruId,UrunId);
                  END"),
            new(60,
                @"IF COL_LENGTH(N'dbo.BasvuruTedarikciEntegrasyonu',N'TedarikSekli') IS NOT NULL
                  BEGIN
                    UPDATE dbo.BasvuruTedarikciEntegrasyonu
                    SET TedarikSekli = CASE
                      WHEN TRY_CONVERT(INT,TedarikSekli) IN (1,2) THEN CONVERT(NVARCHAR(10),TRY_CONVERT(INT,TedarikSekli))
                      WHEN TedarikSekli LIKE N'%Niyet%' OR TedarikSekli LIKE N'%protokol%' THEN N'2'
                      ELSE N'1' END;
                    ALTER TABLE dbo.BasvuruTedarikciEntegrasyonu ALTER COLUMN TedarikSekli INT NOT NULL;
                  END"),
            new(61,
                @"IF COL_LENGTH(N'dbo.Basvuru',N'TedarikciEntegrasyonuAciklama') IS NULL
                    ALTER TABLE dbo.Basvuru ADD TedarikciEntegrasyonuAciklama NVARCHAR(2000) NULL;"),
        ];

        public static async Task GuncelleAsync(IConfiguration configuration, ILogger logger)
        {
            string? connectionString = configuration.GetConnectionString("DefaultConnection");

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                logger.LogWarning("VTGuncelle calistirilmadi. ConnectionStrings:DefaultConnection tanimli degil.");
                return;
            }

            await GuncelleAsync(connectionString, logger, Komutlar);

            string? dosyaConnectionString = configuration.GetConnectionString("DosyaConnection");
            if (!string.IsNullOrWhiteSpace(dosyaConnectionString)
                && !string.Equals(dosyaConnectionString, connectionString, StringComparison.OrdinalIgnoreCase))
            {
                await GuncelleAsync(
                    dosyaConnectionString,
                    logger,
                    Komutlar.Where(komut => komut.KomutNo == 21).ToArray());
            }
        }

        private static async Task GuncelleAsync(string connectionString, ILogger logger, VTKomut[] komutlar)
        {
            await using SqlConnection connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            await VTGuncelleLogTablosuOlusturAsync(connection);

            HashSet<int> calisanKomutNolari = await CalisanKomutNolariniOkuAsync(connection);
            VTKomut[] calisacakKomutlar = komutlar
                .Where(komut => !calisanKomutNolari.Contains(komut.KomutNo))
                .OrderBy(komut => komut.KomutNo)
                .ToArray();

            foreach (VTKomut komut in calisacakKomutlar)
            {
                await using SqlTransaction transaction = (SqlTransaction)await connection.BeginTransactionAsync();

                try
                {
                    await KomutCalistirAsync(connection, transaction, komut.SqlKomut);
                    await LogaEkleAsync(connection, transaction, komut.KomutNo);
                    await transaction.CommitAsync();

                    logger.LogInformation("VTGuncelle komutu calistirildi. KomutNo: {KomutNo}", komut.KomutNo);
                }
                catch (Exception ex)
                {
                    try
                    {
                        await transaction.RollbackAsync();
                    }
                    catch (Exception rollbackEx)
                    {
                        logger.LogError(
                            rollbackEx,
                            "VTGuncelle rollback islemi basarisiz oldu. KomutNo: {KomutNo}",
                            komut.KomutNo);
                    }

                    logger.LogError(
                        ex,
                        "VTGuncelle komutu calistirilamadi. KomutNo: {KomutNo}. Komut loglanip sonraki komuta gecilecek.",
                        komut.KomutNo);

                    try
                    {
                        await LogaEkleAsync(connection, null, komut.KomutNo);
                    }
                    catch (Exception logEx)
                    {
                        logger.LogError(
                            logEx,
                            "Basarisiz VTGuncelle komutu loglanamadi. KomutNo: {KomutNo}",
                            komut.KomutNo);
                    }
                }
            }
        }

        private static async Task VTGuncelleLogTablosuOlusturAsync(SqlConnection connection)
        {
            const string sql = @"
                IF OBJECT_ID(N'dbo.VTGuncelleLog', N'U') IS NULL
                BEGIN
                    CREATE TABLE dbo.VTGuncelleLog
                    (
                        KomutNo INT NOT NULL CONSTRAINT PK_VTGuncelleLog PRIMARY KEY,
                        Zaman DATETIME NOT NULL CONSTRAINT DF_VTGuncelleLog_Zaman DEFAULT GETDATE()
                    );
                END
                ";

            await KomutCalistirAsync(connection, null, sql);
        }

        private static async Task<HashSet<int>> CalisanKomutNolariniOkuAsync(SqlConnection connection)
        {
            const string sql = "SELECT KomutNo FROM dbo.VTGuncelleLog;";

            await using SqlCommand command = new SqlCommand(sql, connection);
            await using SqlDataReader reader = await command.ExecuteReaderAsync();

            HashSet<int> komutNolari = new HashSet<int>();
            while (await reader.ReadAsync())
            {
                komutNolari.Add(reader.GetInt32(0));
            }

            return komutNolari;
        }

        private static async Task LogaEkleAsync(SqlConnection connection, SqlTransaction? transaction, int komutNo)
        {
            const string sql = "INSERT INTO dbo.VTGuncelleLog (KomutNo, Zaman) VALUES (@KomutNo, GETDATE());";

            await using SqlCommand command = new SqlCommand(sql, connection, transaction);
            command.Parameters.AddWithValue("@KomutNo", komutNo);

            await command.ExecuteNonQueryAsync();
        }

        private static async Task KomutCalistirAsync(
            SqlConnection connection,
            SqlTransaction? transaction,
            string sql)
        {
            await using SqlCommand command = new SqlCommand(sql, connection, transaction);
            await command.ExecuteNonQueryAsync();
        }
    }
}
