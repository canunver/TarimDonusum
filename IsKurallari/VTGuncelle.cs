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
                        KayitTarihi DATETIME NOT NULL CONSTRAINT DF_Donem_KayitTarihi DEFAULT GETDATE(),
                        GuncellemeTarihi DATETIME NOT NULL CONSTRAINT DF_Donem_GuncellemeTarihi DEFAULT GETDATE()
                    );

                    CREATE UNIQUE INDEX UX_Donem_Ad ON dbo.Donem(Ad);
                    CREATE INDEX IX_Donem_BasvuruyaAcikMi ON dbo.Donem(BasvuruyaAcikMi);
                "),
            new(5,
                @"INSERT INTO dbo.Donem(Ad, BasvuruyaAcikMi, BasvuruBaslangicTarihi, BasvuruBitisTarihi,
                    OnBasvuruBitisTarihi, MinimumYatirimTutari, MaksimumYatirimTutari, MaksimumDestekTutari, DestekOrani, Aciklama)
                    VALUES (N'2026 1. Dönem', 1, '2026-07-01', '2026-12-31', '2026-08-31', 100000, 5000000, 2500000, 80, N'Test dönemi');"),
            new(6,
                @"CREATE TABLE dbo.Firma
                    (
                        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Firma PRIMARY KEY,
                        KullaniciId INT NOT NULL,
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
                    CREATE INDEX IX_Firma_KullaniciId ON dbo.Firma(KullaniciId);
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
            new (10,
                @"INSERT INTO dbo.Il (Kod, Ad, Aktif)
                    VALUES
                    (N'06', N'Ankara', 1),
                    (N'34', N'İstanbul', 1),
                    (N'35', N'İzmir', 1);"),

            new (11,
                @"CREATE TABLE dbo.Basvuru(
                    Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Basvuru PRIMARY KEY,
                    KullaniciId INT NOT NULL,
                    FirmaId INT NOT NULL,
                    DonemId INT NOT NULL,
                    IlId INT NOT NULL,
                    BasvuruKonusu NVARCHAR(250) NOT NULL,
                    YatirimAdi NVARCHAR(250) NOT NULL CONSTRAINT DF_Basvuru_YatirimAdi DEFAULT N'',
                    ToplamYatirimTutari DECIMAL(18,2) NULL,
                    UygunHarcamaTutari DECIMAL(18,2) NULL,
                    TalepEdilenDestekTutari DECIMAL(18,2) NULL,
                    BasvuruSahibiKatkisi DECIMAL(18,2) NULL,
                    DestekOrani DECIMAL(5,2) NULL,
                    YatiriminAmaci NVARCHAR(MAX) NOT NULL CONSTRAINT DF_Basvuru_YatiriminAmaci DEFAULT N'',
                    OzelSektorPayi DECIMAL(5,2) NULL,
                    BagliOrtakIsletmeVarMi NVARCHAR(10) NULL,
                    BagliOrtakAciklama NVARCHAR(MAX) NULL,
                    Durum NVARCHAR(50) NOT NULL CONSTRAINT DF_Basvuru_Durum DEFAULT N'Ön Başvuru',
                    AktifBolum INT NOT NULL CONSTRAINT DF_Basvuru_AktifBolum DEFAULT 1,
                    KayitTarihi DATETIME NOT NULL CONSTRAINT DF_Basvuru_KayitTarihi DEFAULT GETDATE(),
                    GuncellemeTarihi DATETIME NOT NULL CONSTRAINT DF_Basvuru_GuncellemeTarihi DEFAULT GETDATE(),
                    JsonText NVARCHAR(MAX) NOT NULL,
                    CONSTRAINT FK_Basvuru_Kullanici
                        FOREIGN KEY (KullaniciId) REFERENCES dbo.Kullanici(Id),
                    CONSTRAINT FK_Basvuru_Firma
                        FOREIGN KEY (FirmaId) REFERENCES dbo.Firma(Id),
                    CONSTRAINT FK_Basvuru_Donem
                        FOREIGN KEY (DonemId) REFERENCES dbo.Donem(Id),
                    CONSTRAINT FK_Basvuru_Il
                        FOREIGN KEY (IlId) REFERENCES dbo.Il(Id)
                );

                CREATE INDEX IX_Basvuru_KullaniciId ON dbo.Basvuru(KullaniciId);
                CREATE INDEX IX_Basvuru_FirmaId ON dbo.Basvuru(FirmaId);
                CREATE INDEX IX_Basvuru_DonemId ON dbo.Basvuru(DonemId);
                CREATE INDEX IX_Basvuru_IlId ON dbo.Basvuru(IlId);
                CREATE INDEX IX_Basvuru_GuncellemeTarihi ON dbo.Basvuru(GuncellemeTarihi);
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
                @"IF COL_LENGTH(N'dbo.Basvuru', N'OzelSektorPayi') IS NULL
                    ALTER TABLE dbo.Basvuru ADD OzelSektorPayi DECIMAL(5,2) NULL;

                  IF COL_LENGTH(N'dbo.Basvuru', N'BagliOrtakIsletmeVarMi') IS NULL
                    ALTER TABLE dbo.Basvuru ADD BagliOrtakIsletmeVarMi NVARCHAR(10) NULL;

                  IF COL_LENGTH(N'dbo.Basvuru', N'BagliOrtakAciklama') IS NULL
                    ALTER TABLE dbo.Basvuru ADD BagliOrtakAciklama NVARCHAR(MAX) NULL;"),
            new(14,
                @"CREATE TABLE dbo.DegerZinciri
                    (
                        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_DegerZinciri PRIMARY KEY,
                        Kod NVARCHAR(50) NOT NULL,
                        Ad NVARCHAR(250) NOT NULL,
                        Aciklama NVARCHAR(MAX) NOT NULL CONSTRAINT DF_DegerZinciri_Aciklama DEFAULT N'',
                        Aktif INT NOT NULL CONSTRAINT DF_DegerZinciri_Aktif DEFAULT 1,
                        KayitTarihi DATETIME NOT NULL CONSTRAINT DF_DegerZinciri_KayitTarihi DEFAULT GETDATE(),
                        GuncellemeTarihi DATETIME NOT NULL CONSTRAINT DF_DegerZinciri_GuncellemeTarihi DEFAULT GETDATE()
                    );

                    CREATE UNIQUE INDEX UX_DegerZinciri_Kod ON dbo.DegerZinciri(Kod);
                    CREATE UNIQUE INDEX UX_DegerZinciri_Ad ON dbo.DegerZinciri(Ad);
                    CREATE INDEX IX_DegerZinciri_Aktif ON dbo.DegerZinciri(Aktif);
                "),
            new(15,
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
            new(16,
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
            new(17,
                @"DECLARE @DegerZinciri TABLE
                  (
                    Kod NVARCHAR(50) NOT NULL,
                    Ad NVARCHAR(250) NOT NULL,
                    Aciklama NVARCHAR(MAX) NOT NULL
                  );

                  INSERT INTO @DegerZinciri(Kod, Ad, Aciklama)
                  VALUES
                    (N'DZ001', N'Alternatif Tarım ve Gıda Ürünleri Değer Zinciri', N''),
                    (N'DZ002', N'Arıcılık Ürünleri Değer Zinciri', N''),
                    (N'DZ003', N'Baklagiller ve Bitkisel Protein Değer Zinciri', N''),
                    (N'DZ004', N'Domates Değer Zinciri', N''),
                    (N'DZ005', N'Kanatlı Sektörü Değer Zinciri', N''),
                    (N'DZ006', N'Kırmızı Et Değer Zinciri', N''),
                    (N'DZ007', N'Mantar Değer Zinciri', N''),
                    (N'DZ008', N'Meyve ve Sebze Değer Zinciri', N''),
                    (N'DZ009', N'Sert Kabuklu Meyveler Değer Zinciri', N''),
                    (N'DZ010', N'Su Ürünleri Değer Zinciri', N''),
                    (N'DZ011', N'Süt Değer Zinciri', N''),
                    (N'DZ012', N'Tahıl Değer Zinciri', N''),
                    (N'DZ013', N'Tıbbi ve Aromatik Bitkiler Değer Zinciri', N''),
                    (N'DZ014', N'Yağlı Tohumlar Sektörü Değer Zinciri', N''),
                    (N'DZ015', N'Yem Değer Zinciri', N''),
                    (N'DZ016', N'Zeytin Değer Zinciri', N'');

                  INSERT INTO dbo.DegerZinciri(Kod, Ad, Aciklama, Aktif)
                  SELECT v.Kod, v.Ad, v.Aciklama, 1
                  FROM @DegerZinciri v
                  WHERE NOT EXISTS (
                    SELECT 1 FROM dbo.DegerZinciri dz WHERE dz.Kod = v.Kod OR dz.Ad = v.Ad
                  );

                  DECLARE @Asama TABLE
                  (
                    SiraNo INT NOT NULL,
                    Ad NVARCHAR(250) NOT NULL,
                    Aciklama NVARCHAR(MAX) NOT NULL
                  );

                  INSERT INTO @Asama(SiraNo, Ad, Aciklama)
                  VALUES
                    (1, N'Birincil Üretim', N'Bitkisel, hayvansal veya su ürünleri üretiminde verim, kalite ve kapasiteyi artıran yatırımlar.'),
                    (2, N'Depolama / Soğuk Zincir', N'Ürünlerin kalite kaybını azaltan depo, silo, soğuk hava, ön soğutma ve muhafaza yatırımları.'),
                    (3, N'Lojistik', N'Ürünlerin toplama, taşıma, dağıtım ve sevkiyat süreçlerini iyileştiren yatırımlar.'),
                    (4, N'İşleme', N'Tarımsal hammaddenin gıda ürünü veya ara ürüne dönüştürülmesine yönelik temel işleme yatırımları.'),
                    (5, N'İleri İşleme', N'Ürüne daha yüksek katma değer kazandıran, ürün çeşitlendirme ve gelişmiş işleme yatırımları.'),
                    (6, N'Tarımsal Bileşen Üretimi', N'Tarımsal hammaddelerden gıda bileşeni, katkı, protein, enzim, ekstrakt veya benzeri girdilerin üretilmesi.'),
                    (7, N'Atık ve Yan Ürün Değerlendirme', N'Üretimden kaynaklanan atık, fire ve yan ürünlerin yeniden kullanımı, geri dönüşümü veya ekonomik değere dönüştürülmesi.');

                  INSERT INTO dbo.DegerZinciriAsama(DegerZinciriId, SiraNo, Ad, Aciklama, Aktif)
                  SELECT dz.Id, a.SiraNo, a.Ad, a.Aciklama, 1
                  FROM dbo.DegerZinciri dz
                  INNER JOIN @DegerZinciri v ON v.Ad = dz.Ad
                  CROSS JOIN @Asama a
                  WHERE NOT EXISTS (
                    SELECT 1
                    FROM dbo.DegerZinciriAsama dza
                    WHERE dza.DegerZinciriId = dz.Id
                      AND dza.SiraNo = a.SiraNo
                  );
                "),
            new(18,
                @"IF COL_LENGTH(N'dbo.Basvuru', N'YatirimTuru') IS NULL
                    ALTER TABLE dbo.Basvuru ADD YatirimTuru NVARCHAR(100) NOT NULL CONSTRAINT DF_Basvuru_YatirimTuru DEFAULT N'';"),
            new(19,
                @"SELECT 1;"),
            new(20,
                @"CREATE TABLE dbo.BasvuruHarcamaTuru
                    (
                        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_BasvuruHarcamaTuru PRIMARY KEY,
                        BasvuruId INT NOT NULL,
                        HarcamaTuru NVARCHAR(150) NOT NULL,
                        CONSTRAINT FK_BasvuruHarcamaTuru_Basvuru
                            FOREIGN KEY (BasvuruId) REFERENCES dbo.Basvuru(Id)
                    );

                    CREATE UNIQUE INDEX UX_BasvuruHarcamaTuru_BasvuruHarcamaTuru ON dbo.BasvuruHarcamaTuru(BasvuruId, HarcamaTuru);
                    CREATE INDEX IX_BasvuruHarcamaTuru_BasvuruId ON dbo.BasvuruHarcamaTuru(BasvuruId);
                "),
            new(21,
                @"CREATE TABLE dbo.BasvuruDegerZinciriAsama
                    (
                        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_BasvuruDegerZinciriAsama PRIMARY KEY,
                        BasvuruId INT NOT NULL,
                        DegerZinciriAsamaId INT NULL,
                        AsamaAdi NVARCHAR(250) NOT NULL,
                        CONSTRAINT FK_BasvuruDegerZinciriAsama_Basvuru
                            FOREIGN KEY (BasvuruId) REFERENCES dbo.Basvuru(Id),
                        CONSTRAINT FK_BasvuruDegerZinciriAsama_DegerZinciriAsama
                            FOREIGN KEY (DegerZinciriAsamaId) REFERENCES dbo.DegerZinciriAsama(Id)
                    );

                    CREATE UNIQUE INDEX UX_BasvuruDegerZinciriAsama_BasvuruAsama ON dbo.BasvuruDegerZinciriAsama(BasvuruId, AsamaAdi);
                    CREATE INDEX IX_BasvuruDegerZinciriAsama_BasvuruId ON dbo.BasvuruDegerZinciriAsama(BasvuruId);
                    CREATE INDEX IX_BasvuruDegerZinciriAsama_DegerZinciriAsamaId ON dbo.BasvuruDegerZinciriAsama(DegerZinciriAsamaId);
                "),
            new(22,
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
            new(23,
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
            new(24,
                @"ALTER TABLE dbo.Basvuru ADD ToplamYatirimTutari DECIMAL(18,2) NULL, UygunHarcamaTutari DECIMAL(18,2) NULL, TalepEdilenDestekTutari DECIMAL(18,2) NULL, BasvuruSahibiKatkisi DECIMAL(18,2) NULL, DestekOrani DECIMAL(5,2) NULL, YatiriminAmaci NVARCHAR(MAX) NOT NULL
                        CONSTRAINT DF_Basvuru_YatiriminAmaci DEFAULT N'';
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

            await using SqlConnection connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            await VTGuncelleLogTablosuOlusturAsync(connection);
            await EksikTabloLoglariniTemizleAsync(connection);

            HashSet<int> calisanKomutNolari = await CalisanKomutNolariniOkuAsync(connection);
            VTKomut[] calisacakKomutlar = Komutlar
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

        private static async Task EksikTabloLoglariniTemizleAsync(SqlConnection connection)
        {
            const string sql = @"
                DELETE FROM dbo.VTGuncelleLog
                WHERE
                    (KomutNo = 4 AND OBJECT_ID(N'dbo.Donem', N'U') IS NULL)
                    OR (KomutNo = 5 AND OBJECT_ID(N'dbo.Firma', N'U') IS NULL)
                    OR (KomutNo = 6 AND (OBJECT_ID(N'dbo.Il', N'U') IS NULL OR OBJECT_ID(N'dbo.Basvuru', N'U') IS NULL))
                    OR (KomutNo = 8 AND OBJECT_ID(N'dbo.BasvuruLog', N'U') IS NULL)
                    OR (KomutNo = 9 AND OBJECT_ID(N'dbo.FirmaLog', N'U') IS NULL)
                    OR (KomutNo = 10 AND OBJECT_ID(N'dbo.FirmaKullanici', N'U') IS NULL)
                    OR (KomutNo = 14 AND OBJECT_ID(N'dbo.DegerZinciri', N'U') IS NULL)
                    OR (KomutNo = 15 AND OBJECT_ID(N'dbo.DegerZinciriIl', N'U') IS NULL)
                    OR (KomutNo = 16 AND OBJECT_ID(N'dbo.DegerZinciriAsama', N'U') IS NULL)
                    OR (KomutNo = 17 AND (
                        OBJECT_ID(N'dbo.DegerZinciri', N'U') IS NULL
                        OR OBJECT_ID(N'dbo.DegerZinciriIl', N'U') IS NULL
                        OR OBJECT_ID(N'dbo.DegerZinciriAsama', N'U') IS NULL
                    ))
                    OR (KomutNo = 18 AND COL_LENGTH(N'dbo.Basvuru', N'YatirimTuru') IS NULL)
                    OR (KomutNo = 20 AND OBJECT_ID(N'dbo.BasvuruHarcamaTuru', N'U') IS NULL)
                    OR (KomutNo = 21 AND OBJECT_ID(N'dbo.BasvuruDegerZinciriAsama', N'U') IS NULL)
                    OR (KomutNo = 22 AND OBJECT_ID(N'dbo.Ilce', N'U') IS NULL)
                    OR (KomutNo = 23 AND OBJECT_ID(N'dbo.BasvuruUygulamaAdresleri', N'U') IS NULL)
                    OR (KomutNo = 24 AND (
                        COL_LENGTH(N'dbo.Basvuru', N'ToplamYatirimTutari') IS NULL
                        OR COL_LENGTH(N'dbo.Basvuru', N'UygunHarcamaTutari') IS NULL
                        OR COL_LENGTH(N'dbo.Basvuru', N'TalepEdilenDestekTutari') IS NULL
                        OR COL_LENGTH(N'dbo.Basvuru', N'BasvuruSahibiKatkisi') IS NULL
                        OR COL_LENGTH(N'dbo.Basvuru', N'DestekOrani') IS NULL
                        OR COL_LENGTH(N'dbo.Basvuru', N'YatiriminAmaci') IS NULL
                    ));
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
