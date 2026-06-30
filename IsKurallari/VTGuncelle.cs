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
            new(
                1,
                @"
                IF OBJECT_ID(N'dbo.Kullanici', N'U') IS NULL
                BEGIN
                    CREATE TABLE dbo.Kullanici
                    (
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
                END
                "),
            new(
                2,
                @"
                IF OBJECT_ID(N'dbo.KullaniciYetki', N'U') IS NULL
                BEGIN
                    CREATE TABLE dbo.KullaniciYetki
                    (
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
                END
                "),
            new(
                3,
                @"
                IF OBJECT_ID(N'dbo.KullaniciLog', N'U') IS NULL
                BEGIN
                    CREATE TABLE dbo.KullaniciLog
                    (
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
                END
                ")
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
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
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

        private static async Task LogaEkleAsync(SqlConnection connection, SqlTransaction transaction, int komutNo)
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
