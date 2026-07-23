using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Localization;
using System.Collections.Generic;
using TarimDonusum.Araclar;
using TarimDonusum.Models;

namespace TarimDonusum.Tablolar
{
    public class TABDegerZinciri : TABTablo
    {
        public TABDegerZinciri(SqlConnection connection, IStringLocalizer<SharedResource> localizer, SqlTransaction? transaction = null)
            : base(connection, localizer, transaction)
        {
        }

        public async Task<List<DegerZinciri>> YonetimListesiAsync()
        {
            const string sql = @"SELECT Id, Ad, Aciklama, Aktif
                                 FROM dbo.DegerZinciri ORDER BY Ad;
                                 SELECT Id, DegerZinciriId, SiraNo, Ad, Aciklama, Aktif
                                 FROM dbo.DegerZinciriAsama ORDER BY DegerZinciriId, SiraNo, Ad;";
            await using SqlCommand command = KomutOlustur(sql);
            await using SqlDataReader reader = await command.ExecuteReaderAsync();
            List<DegerZinciri> liste = new();
            while (await reader.ReadAsync())
                liste.Add(new DegerZinciri {
                    id = reader.GetInt32(0), ad = reader.GetString(1),
                    aciklama = reader.GetString(2), aktif = OrtakFonksiyonlar.Int32Yap(reader.GetValue(3)) == 1
                });
            await reader.NextResultAsync();
            while (await reader.ReadAsync())
            {
                DegerZinciri? zincir = liste.FirstOrDefault(x => x.id == reader.GetInt32(1));
                zincir?.asamalar.Add(AsamaOku(reader));
            }
            return liste;
        }

        public async Task<int> EkleAsync(DegerZinciri model)
        {
            const string sql = @"INSERT INTO dbo.DegerZinciri(Ad, Aciklama, Aktif)
                                 OUTPUT INSERTED.Id VALUES(@Ad,@Aciklama,@Aktif);";
            await using SqlCommand command = KomutOlustur(sql);
            ParametreEkle(command, model);
            return Convert.ToInt32(await command.ExecuteScalarAsync());
        }

        public async Task<bool> GuncelleAsync(DegerZinciri model)
        {
            const string sql = @"UPDATE dbo.DegerZinciri SET Ad=@Ad,Aciklama=@Aciklama,Aktif=@Aktif WHERE Id=@Id;";
            await using SqlCommand command = KomutOlustur(sql);
            command.Parameters.AddWithValue("@Id", model.id);
            ParametreEkle(command, model);
            return await command.ExecuteNonQueryAsync() > 0;
        }

        private static void ParametreEkle(SqlCommand command, DegerZinciri model)
        {
            command.Parameters.AddWithValue("@Ad", model.ad);
            command.Parameters.AddWithValue("@Aciklama", model.aciklama);
            command.Parameters.AddWithValue("@Aktif", model.aktif ? 1 : 0);
        }

        //public async Task<DegerZinciri?> OkuAsync(int id)
        //{
        //    const string sql = @"
        //        SELECT Id, Kod, Ad, Aciklama, Aktif, KayitTarihi, GuncellemeTarihi
        //        FROM dbo.DegerZinciri
        //        WHERE Id = @Id;

        //        SELECT dzi.Id, dzi.DegerZinciriId, dzi.IlId, dzi.Aktif, i.Kod, i.Ad, i.Aktif
        //        FROM dbo.DegerZinciriIl dzi
        //        INNER JOIN dbo.Il i ON i.Id = dzi.IlId
        //        WHERE dzi.DegerZinciriId = @Id
        //        ORDER BY i.Ad;

        //        SELECT Id, DegerZinciriId, SiraNo, Ad, Aciklama, Aktif
        //        FROM dbo.DegerZinciriAsama
        //        WHERE DegerZinciriId = @Id
        //        ORDER BY SiraNo, Ad;";

        //    await using SqlCommand command = KomutOlustur(sql);
        //    command.Parameters.AddWithValue("@Id", id);

        //    await using SqlDataReader reader = await command.ExecuteReaderAsync();
        //    if (!await reader.ReadAsync())
        //        return null;

        //    DegerZinciri degerZinciri = Oku(reader);

        //    await reader.NextResultAsync();
        //    while (await reader.ReadAsync())
        //    {
        //        degerZinciri.iller.Add(IlOku(reader));
        //    }

        //    await reader.NextResultAsync();
        //    while (await reader.ReadAsync())
        //    {
        //        degerZinciri.asamalar.Add(AsamaOku(reader));
        //    }

        //    return degerZinciri;
        //}

        public async Task<Sonuc<List<DegerZinciri>>> ListeleAsync(bool sadeceAktif, int? ilKod, int seciliZincirId)
        {
            Sonuc<List<DegerZinciri>> liste = new Sonuc<List<DegerZinciri>>();
            string sql = @" SELECT dz.Id,  dz.Ad,  dz.Aciklama,  dz.Aktif 
                    FROM dbo.DegerZinciri dz 
                    LEFT JOIN dbo.DegerZinciriIl dzi ON dz.Id  = dzi.DegerZinciriId 
                    WHERE (dzi.IlId = @IlKod OR dzi.IlId IS NULL)";

            if (sadeceAktif)
            {
                sql += " AND dz.Aktif = 1";
            }

            await using SqlCommand command = KomutOlustur(sql);
            command.Parameters.AddWithValue("@IlKod", ilKod.HasValue ? ilKod.Value : 0);

            await using SqlDataReader reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                DegerZinciri dz = DegerZinciriOku(reader);
                if (dz.id == seciliZincirId)
                    dz.secili = true;
                liste.nesne.Add(dz);
            }
            return liste;
        }

        private static DegerZinciri DegerZinciriOku(SqlDataReader reader)
        {
            return new DegerZinciri
            {
                id = reader.GetInt32(0),
                ad = reader.GetString(1),
                aciklama = reader.GetString(2),
                aktif = OrtakFonksiyonlar.Int32Yap(reader.GetValue(3)) == 1,
            };
        }

        private static DegerZinciriAsama AsamaOku(SqlDataReader reader)
        {
            return new DegerZinciriAsama
            {
                id = reader.GetInt32(0),
                degerZinciriId = reader.GetInt32(1),
                siraNo = reader.GetInt32(2),
                ad = reader.GetString(3),
                aciklama = reader.GetString(4),
                aktif = OrtakFonksiyonlar.Int32Yap(reader.GetValue(5)) == 1
            };
        }

        internal async Task<Sonuc<List<DegerZinciriAsama>>> AsamalariOku(int degerZinciriId, int basvuruId)
        {
            Sonuc<List<DegerZinciriAsama>> liste = new Sonuc<List<DegerZinciriAsama>>();
            string sql = @"SELECT dza.Id, dza.DegerZinciriId, dza.SiraNo, dza.Ad, dza.Aciklama, dza.Aktif, bdza.Id, bdza.YapilacakFaaliyetler
                           FROM dbo.DegerZinciriAsama dza
                           LEFT JOIN dbo.BasvuruDegerZinciriAsama bdza ON dza.Id = bdza.DegerZinciriAsamaId
                                AND bdza.BasvuruId = @BasvuruId 
                           WHERE dza.DegerZinciriId = @DegerZinciriId
                           ORDER BY dza.SiraNo ASC";
            await using SqlCommand command = KomutOlustur(sql);
            command.Parameters.AddWithValue("@DegerZinciriId", degerZinciriId);
            command.Parameters.AddWithValue("@BasvuruId", basvuruId);

            await using SqlDataReader reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                DegerZinciriAsama dz = DegerZinciriAsamaOku(reader);
                liste.nesne.Add(dz);
            }
            return liste;
        }

        private DegerZinciriAsama DegerZinciriAsamaOku(SqlDataReader reader)
        {
            return new DegerZinciriAsama
            {
                id = reader.GetInt32(0),
                degerZinciriId = reader.GetInt32(1),
                siraNo = reader.GetInt32(2),
                ad = reader.GetString(3),
                aciklama = reader.GetString(4),
                aktif = OrtakFonksiyonlar.Int32Yap(reader.GetValue(5)) == 1,
                secili = BoolYap(NullDuzeltInt(reader, 6)),
                yapilacakFaaliyetler = NullOkuString(reader, 7)
            };
        }
    }
}
