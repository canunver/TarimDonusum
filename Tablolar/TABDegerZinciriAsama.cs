using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Localization;
using TarimDonusum.Araclar;
using TarimDonusum.Models;

namespace TarimDonusum.Tablolar
{
    public class TABDegerZinciriAsama : TABTablo
    {
        public TABDegerZinciriAsama(SqlConnection connection, IStringLocalizer<SharedResource>? localizer = null, SqlTransaction? transaction = null)
            : base(connection, localizer, transaction)
        {
        }

        public async Task<List<DegerZinciriAsama>> DegerZinciriAsamalariniListeleAsync(int degerZinciriId, bool sadeceAktif = true)
        {
            const string sql = @"
                SELECT Id, DegerZinciriId, SiraNo, Ad, Aciklama, Aktif
                FROM dbo.DegerZinciriAsama
                WHERE DegerZinciriId = @DegerZinciriId
                    AND (@SadeceAktif = 0 OR Aktif = 1)
                ORDER BY SiraNo, Ad;";

            await using SqlCommand command = KomutOlustur(sql);
            command.Parameters.AddWithValue("@DegerZinciriId", degerZinciriId);
            command.Parameters.AddWithValue("@SadeceAktif", sadeceAktif ? 1 : 0);

            await using SqlDataReader reader = await command.ExecuteReaderAsync();
            List<DegerZinciriAsama> liste = new List<DegerZinciriAsama>();
            while (await reader.ReadAsync())
            {
                liste.Add(Oku(reader));
            }

            return liste;
        }

        public async Task<int> EkleAsync(DegerZinciriAsama model)
        {
            const string sql = @"INSERT INTO dbo.DegerZinciriAsama(DegerZinciriId,SiraNo,Ad,Aciklama,Aktif)
                                 OUTPUT INSERTED.Id VALUES(@DegerZinciriId,@SiraNo,@Ad,@Aciklama,@Aktif);";
            await using SqlCommand command = KomutOlustur(sql);
            ParametreEkle(command, model);
            return Convert.ToInt32(await command.ExecuteScalarAsync());
        }

        public async Task<bool> GuncelleAsync(DegerZinciriAsama model)
        {
            const string sql = @"UPDATE dbo.DegerZinciriAsama SET SiraNo=@SiraNo,Ad=@Ad,Aciklama=@Aciklama,Aktif=@Aktif
                                 WHERE Id=@Id AND DegerZinciriId=@DegerZinciriId;";
            await using SqlCommand command = KomutOlustur(sql);
            command.Parameters.AddWithValue("@Id", model.id);
            ParametreEkle(command, model);
            return await command.ExecuteNonQueryAsync() > 0;
        }

        private static void ParametreEkle(SqlCommand command, DegerZinciriAsama model)
        {
            command.Parameters.AddWithValue("@DegerZinciriId", model.degerZinciriId);
            command.Parameters.AddWithValue("@SiraNo", model.siraNo);
            command.Parameters.AddWithValue("@Ad", model.ad);
            command.Parameters.AddWithValue("@Aciklama", model.aciklama);
            command.Parameters.AddWithValue("@Aktif", model.aktif ? 1 : 0);
        }

        private static DegerZinciriAsama Oku(SqlDataReader reader)
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
