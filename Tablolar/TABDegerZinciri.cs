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
                DegerZinciri dz = Oku(reader);
                if (dz.id == seciliZincirId)
                    dz.secili = true;
                liste.nesne.Add(dz);
            }
            return liste;
        }

        private static DegerZinciri Oku(SqlDataReader reader)
        {
            return new DegerZinciri
            {
                id = reader.GetInt32(0),
                ad = reader.GetString(1),
                aciklama = reader.GetString(2),
                aktif = OrtakFonksiyonlar.Int32Yap(reader.GetValue(3)) == 1,
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
                id = reader.GetInt32(0),
                degerZinciriId = reader.GetInt32(1),
                siraNo = reader.GetInt32(2),
                ad = reader.GetString(3),
                aciklama = reader.GetString(4),
                aktif = OrtakFonksiyonlar.Int32Yap(reader.GetValue(5)) == 1
            };
        }
    }
}
