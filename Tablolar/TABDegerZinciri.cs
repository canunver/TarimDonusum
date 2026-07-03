using Microsoft.Data.SqlClient;
using TarimDonusum.Araclar;
using TarimDonusum.Models;

namespace TarimDonusum.Tablolar
{
    public class TABDegerZinciri : TABTablo
    {
        public TABDegerZinciri(SqlConnection connection, SqlTransaction? transaction = null)
            : base(connection, transaction)
        {
        }

        public async Task<DegerZinciri?> OkuAsync(int id)
        {
            const string sql = @"
                SELECT Id, Kod, Ad, Aciklama, Aktif, KayitTarihi, GuncellemeTarihi
                FROM dbo.DegerZinciri
                WHERE Id = @Id;

                SELECT dzi.Id, dzi.DegerZinciriId, dzi.IlId, dzi.Aktif, i.Kod, i.Ad, i.Aktif
                FROM dbo.DegerZinciriIl dzi
                INNER JOIN dbo.Il i ON i.Id = dzi.IlId
                WHERE dzi.DegerZinciriId = @Id
                ORDER BY i.Ad;

                SELECT Id, DegerZinciriId, SiraNo, Ad, Aciklama, Aktif
                FROM dbo.DegerZinciriAsama
                WHERE DegerZinciriId = @Id
                ORDER BY SiraNo, Ad;";

            await using SqlCommand command = KomutOlustur(sql);
            command.Parameters.AddWithValue("@Id", id);

            await using SqlDataReader reader = await command.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
                return null;

            DegerZinciri degerZinciri = Oku(reader);

            await reader.NextResultAsync();
            while (await reader.ReadAsync())
            {
                degerZinciri.Iller.Add(IlOku(reader));
            }

            await reader.NextResultAsync();
            while (await reader.ReadAsync())
            {
                degerZinciri.Asamalar.Add(AsamaOku(reader));
            }

            return degerZinciri;
        }

        public async Task<List<DegerZinciri>> ListeleAsync(bool sadeceAktif = true, int? ilKod = null, bool asamalariYukle = true)
        {
            const string sql = @"
                DECLARE @SeciliDegerZinciri TABLE
                (
                    Id INT NOT NULL PRIMARY KEY
                );

                INSERT INTO @SeciliDegerZinciri(Id)
                SELECT dz.Id
                FROM dbo.DegerZinciri dz
                WHERE (@SadeceAktif = 0 OR dz.Aktif = 1)
                    AND (
                        @IlKod IS NULL
                        OR NOT EXISTS (
                            SELECT 1
                            FROM dbo.DegerZinciriIl dziTanim
                            INNER JOIN dbo.Il iTanim ON iTanim.Id = dziTanim.IlId
                            WHERE dziTanim.DegerZinciriId = dz.Id
                                AND (@SadeceAktif = 0 OR (dziTanim.Aktif = 1 AND iTanim.Aktif = 1))
                        )
                        OR EXISTS (
                            SELECT 1
                            FROM dbo.DegerZinciriIl dzi
                            INNER JOIN dbo.Il i ON i.Id = dzi.IlId
                            WHERE dzi.DegerZinciriId = dz.Id
                                AND (@SadeceAktif = 0 OR (dzi.Aktif = 1 AND i.Aktif = 1))
                                AND i.Kod = @IlKod
                        )
                    );

                SELECT dz.Id, dz.Kod, dz.Ad, dz.Aciklama, dz.Aktif, dz.KayitTarihi, dz.GuncellemeTarihi
                FROM dbo.DegerZinciri dz
                INNER JOIN @SeciliDegerZinciri sdz ON sdz.Id = dz.Id
                ORDER BY Ad;

                SELECT dzi.Id, dzi.DegerZinciriId, dzi.IlId, dzi.Aktif, i.Kod, i.Ad, i.Aktif
                FROM dbo.DegerZinciriIl dzi
                INNER JOIN dbo.Il i ON i.Id = dzi.IlId
                INNER JOIN @SeciliDegerZinciri sdz ON sdz.Id = dzi.DegerZinciriId
                WHERE @SadeceAktif = 0 OR (dzi.Aktif = 1 AND i.Aktif = 1)
                ORDER BY dzi.DegerZinciriId, i.Ad;

                SELECT dza.Id, dza.DegerZinciriId, dza.SiraNo, dza.Ad, dza.Aciklama, dza.Aktif
                FROM dbo.DegerZinciriAsama dza
                INNER JOIN @SeciliDegerZinciri sdz ON sdz.Id = dza.DegerZinciriId
                WHERE @AsamalariYukle = 1
                    AND (@SadeceAktif = 0 OR dza.Aktif = 1)
                ORDER BY dza.DegerZinciriId, dza.SiraNo, dza.Ad;";

            await using SqlCommand command = KomutOlustur(sql);
            command.Parameters.AddWithValue("@SadeceAktif", sadeceAktif ? 1 : 0);
            command.Parameters.AddWithValue("@IlKod", ilKod.HasValue ? ilKod.Value : DBNull.Value);
            command.Parameters.AddWithValue("@AsamalariYukle", asamalariYukle ? 1 : 0);

            await using SqlDataReader reader = await command.ExecuteReaderAsync();
            List<DegerZinciri> liste = new List<DegerZinciri>();
            while (await reader.ReadAsync())
            {
                liste.Add(Oku(reader));
            }

            Dictionary<int, DegerZinciri> sozluk = liste.ToDictionary(x => x.Id);

            await reader.NextResultAsync();
            while (await reader.ReadAsync())
            {
                int degerZinciriId = reader.GetInt32(1);
                if (sozluk.TryGetValue(degerZinciriId, out DegerZinciri? degerZinciri))
                    degerZinciri.Iller.Add(IlOku(reader));
            }

            if (await reader.NextResultAsync())
            {
                while (await reader.ReadAsync())
                {
                    DegerZinciriAsama asama = AsamaOku(reader);
                    if (sozluk.TryGetValue(asama.DegerZinciriId, out DegerZinciri? degerZinciri))
                        degerZinciri.Asamalar.Add(asama);
                }
            }

            return liste;
        }

        private static DegerZinciri Oku(SqlDataReader reader)
        {
            return new DegerZinciri
            {
                Id = reader.GetInt32(0),
                Kod = reader.GetString(1),
                Ad = reader.GetString(2),
                Aciklama = reader.GetString(3),
                Aktif = OrtakFonksiyonlar.Int32Yap(reader.GetValue(4)) == 1,
                KayitTarihi = reader.GetDateTime(5),
                GuncellemeTarihi = reader.GetDateTime(6)
            };
        }

        private static Il IlOku(SqlDataReader reader)
        {
            return new Il
            {
                Id = reader.GetInt32(2),
                Kod = OrtakFonksiyonlar.Int32Yap(reader.GetValue(4)),
                Ad = reader.GetString(5),
                Aktif = OrtakFonksiyonlar.Int32Yap(reader.GetValue(6)) == 1
            };
        }

        private static DegerZinciriAsama AsamaOku(SqlDataReader reader)
        {
            return new DegerZinciriAsama
            {
                Id = reader.GetInt32(0),
                DegerZinciriId = reader.GetInt32(1),
                SiraNo = reader.GetInt32(2),
                Ad = reader.GetString(3),
                Aciklama = reader.GetString(4),
                Aktif = OrtakFonksiyonlar.Int32Yap(reader.GetValue(5)) == 1
            };
        }
    }
}
