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
                        Rol INT NOT NULL,
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
                        Aciklama NVARCHAR(MAX) NOT NULL CONSTRAINT DF_Donem_Aciklama DEFAULT N'',
                    );

                    CREATE UNIQUE INDEX UX_Donem_Ad ON dbo.Donem(Ad);
                    CREATE INDEX IX_Donem_BasvuruyaAcikMi ON dbo.Donem(BasvuruyaAcikMi);
                "),
            new(5,
                @"INSERT INTO dbo.Donem(Ad, BasvuruyaAcikMi, BasvuruBaslangicTarihi, BasvuruBitisTarihi,
                    OnBasvuruBitisTarihi, MinimumYatirimTutari, MaksimumYatirimTutari, MaksimumDestekTutari, DestekOrani, Aciklama)
                    VALUES (N'2026 1. DÃ¶nem', 1, '2026-07-01', '2026-12-31', '2026-08-31', 100000, 5000000, 2500000, 80, N'Test dÃ¶nemi');"),
            new(6,
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
                        Adres NVARCHAR(MAX) NOT NULL CONSTRAINT DF_Firma_Adres DEFAULT N'',
                        CONSTRAINT FK_Firma_Kullanici
                            FOREIGN KEY (KullaniciId) REFERENCES dbo.Kullanici(Id)
                    );

                    CREATE UNIQUE INDEX UX_Firma_VergiKimlikNo ON dbo.Firma(VergiKimlikNo);
                "),
            new(
                7,
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
            new(8,
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
            new(9,
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
            new(10,
                @"CREATE TABLE dbo.Basvuru(
                    Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Basvuru PRIMARY KEY,
                    FirmaId INT NOT NULL,
                    DonemId INT NOT NULL,
                    IlId INT NOT NULL,
                    BasvuruKonusu NVARCHAR(250) NOT NULL,
                    BasvuruSahibiTuru INT NULL,
                    SonIkiYildirFaalMi INT NOT NULL CONSTRAINT DF_Basvuru_SonIkiYildirFaalMi DEFAULT 0,
                    YatirimAdi NVARCHAR(250) NOT NULL CONSTRAINT DF_Basvuru_YatirimAdi DEFAULT N'',
                    ToplamYatirimTutari DECIMAL(18,2) NULL,
                    UygunHarcamaTutari DECIMAL(18,2) NULL,
                    TalepEdilenDestekTutari DECIMAL(18,2) NULL,
                    BasvuruSahibiKatkisi DECIMAL(18,2) NULL,
                    DestekOrani DECIMAL(5,2) NULL,
                    YatirimTuru INT NULL,
                    YatiriminAmaci NVARCHAR(MAX) NOT NULL CONSTRAINT DF_Basvuru_YatiriminAmaci DEFAULT N'',
                    OzelSektorPayi DECIMAL(5,2) NULL,
                    BagliOrtakIsletmeVarMi NVARCHAR(10) NULL,
                    BagliOrtakAciklama NVARCHAR(MAX) NULL,
                    Durum INT NOT NULL CONSTRAINT DF_Basvuru_Durum DEFAULT 1,
                    CONSTRAINT FK_Basvuru_Firma
                        FOREIGN KEY (FirmaId) REFERENCES dbo.Firma(Id),
                    CONSTRAINT FK_Basvuru_Donem
                        FOREIGN KEY (DonemId) REFERENCES dbo.Donem(Id),
                    CONSTRAINT FK_Basvuru_Il
                        FOREIGN KEY (IlId) REFERENCES dbo.Il(Id)
                );

                CREATE INDEX IX_Basvuru_FirmaId ON dbo.Basvuru(FirmaId);
                CREATE INDEX IX_Basvuru_DonemId ON dbo.Basvuru(DonemId);
                CREATE INDEX IX_Basvuru_IlId ON dbo.Basvuru(IlId);
                "),
            new(11,
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
            new(12,
                @"CREATE TABLE dbo.DegerZinciri
                    (
                        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_DegerZinciri PRIMARY KEY,
                        Kod NVARCHAR(50) NOT NULL,
                        Ad NVARCHAR(250) NOT NULL,
                        Aciklama NVARCHAR(MAX) NOT NULL CONSTRAINT DF_DegerZinciri_Aciklama DEFAULT N'',
                        Aktif INT NOT NULL CONSTRAINT DF_DegerZinciri_Aktif DEFAULT 1,
                    );

                    CREATE UNIQUE INDEX UX_DegerZinciri_Kod ON dbo.DegerZinciri(Kod);
                    CREATE UNIQUE INDEX UX_DegerZinciri_Ad ON dbo.DegerZinciri(Ad);
                    CREATE INDEX IX_DegerZinciri_Aktif ON dbo.DegerZinciri(Aktif);
                "),
            new(13,
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
            new(14,
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
            new(15,
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
            new(16,
                @"CREATE TABLE dbo.BasvuruDegerZinciriAsama
                    (
                        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_BasvuruDegerZinciriAsama PRIMARY KEY,
                        BasvuruId INT NOT NULL,
                        DegerZinciriAsamaId INT NULL,
                        CONSTRAINT FK_BasvuruDegerZinciriAsama_Basvuru
                            FOREIGN KEY (BasvuruId) REFERENCES dbo.Basvuru(Id),
                        CONSTRAINT FK_BasvuruDegerZinciriAsama_DegerZinciriAsama
                            FOREIGN KEY (DegerZinciriAsamaId) REFERENCES dbo.DegerZinciriAsama(Id)
                    );

                    CREATE INDEX IX_BasvuruDegerZinciriAsama_BasvuruId ON dbo.BasvuruDegerZinciriAsama(BasvuruId);
                    CREATE INDEX IX_BasvuruDegerZinciriAsama_DegerZinciriAsamaId ON dbo.BasvuruDegerZinciriAsama(DegerZinciriAsamaId);
                "),
            new(17,
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
                @"ALTER TABLE Basvuru ADD 
                    IrtibatKisi nvarchar(150) NULL,
                    IrtibatUnvan nvarchar(100) NULL,
                    IrtibatTelefon nvarchar(30) NULL,
                    IrtibatePosta nvarchar(256) NULL,
                    IrtibatAdres nvarchar(1000) NULL,
                    IrtibatYetkiliKisiler nvarchar(1000) NULL
                "),
            new(20,
                @"ALTER TABLE Basvuru ADD 
                    OncekiYilNetSatis DECIMAL(18,2) NULL, 
                    SonYilNetSatis DECIMAL(18,2) NULL, 
                    OncekiYilAktifToplami DECIMAL(18,2) NULL, 
                    SonYilAktifToplami DECIMAL(18,2) NULL
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
                @"ALTER TABLE dbo.Basvuru ADD
                    BelgePaketiDosyaAdi NVARCHAR(260) NOT NULL CONSTRAINT DF_Basvuru_BelgePaketiDosyaAdi DEFAULT N'',
                    BelgePaketiDosyaId INT NULL,
                    BelgePaketiAciklama NVARCHAR(1000) NOT NULL CONSTRAINT DF_Basvuru_BelgePaketiAciklama DEFAULT N'',
                    BelgeBeyani NVARCHAR(20) NOT NULL CONSTRAINT DF_Basvuru_BelgeBeyani DEFAULT N'',
                    TaahhutDosyaAdi NVARCHAR(260) NOT NULL CONSTRAINT DF_Basvuru_TaahhutDosyaAdi DEFAULT N'',
                    TaahhutDosyaId INT NULL,
                    TaahhutAciklama NVARCHAR(1000) NOT NULL CONSTRAINT DF_Basvuru_TaahhutAciklama DEFAULT N''
                "),
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

