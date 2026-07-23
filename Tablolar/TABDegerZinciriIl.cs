using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Localization;
using TarimDonusum.Araclar;
using TarimDonusum.Models;

namespace TarimDonusum.Tablolar
{
    public class TABDegerZinciriIl : TABTablo
    {
        public TABDegerZinciriIl(SqlConnection connection, IStringLocalizer<SharedResource> localizer, SqlTransaction? transaction = null)
            : base(connection, localizer, transaction)
        {
        }

        public async Task<List<DegerZinciriIl>> DegerZinciriIlleriniListeleAsync(int degerZinciriId, bool sadeceAktif = true)
        {
            const string sql = @"
                SELECT Id, DegerZinciriId, IlId, Aktif
                FROM dbo.DegerZinciriIl
                WHERE DegerZinciriId = @DegerZinciriId
                    AND (@SadeceAktif = 0 OR Aktif = 1)
                ORDER BY IlId;";

            await using SqlCommand command = KomutOlustur(sql);
            command.Parameters.AddWithValue("@DegerZinciriId", degerZinciriId);
            command.Parameters.AddWithValue("@SadeceAktif", sadeceAktif ? 1 : 0);

            await using SqlDataReader reader = await command.ExecuteReaderAsync();
            List<DegerZinciriIl> liste = new List<DegerZinciriIl>();
            while (await reader.ReadAsync())
            {
                liste.Add(Oku(reader));
            }

            return liste;
        }

        public async Task<List<Il>> IlleriListeleAsync(int degerZinciriId)
        {
            const string sql = @"SELECT i.Id,i.Kod,i.Ad,i.Aktif FROM dbo.DegerZinciriIl dzi
                                 INNER JOIN dbo.Il i ON i.Id=dzi.IlId
                                 WHERE dzi.DegerZinciriId=@DegerZinciriId ORDER BY i.Ad;";
            await using SqlCommand command = KomutOlustur(sql);
            command.Parameters.AddWithValue("@DegerZinciriId", degerZinciriId);
            await using SqlDataReader reader = await command.ExecuteReaderAsync();
            List<Il> liste = new();
            while (await reader.ReadAsync())
                liste.Add(new Il { id=reader.GetInt32(0), kod=reader.GetInt32(1), ad=reader.GetString(2),
                    aktif=OrtakFonksiyonlar.Int32Yap(reader.GetValue(3)) == 1 });
            return liste;
        }

        public async Task<int> EkleAsync(int degerZinciriId, int ilId)
        {
            const string sql = @"INSERT INTO dbo.DegerZinciriIl(DegerZinciriId,IlId,Aktif)
                                 OUTPUT INSERTED.Id VALUES(@DegerZinciriId,@IlId,1);";
            await using SqlCommand command = KomutOlustur(sql);
            command.Parameters.AddWithValue("@DegerZinciriId", degerZinciriId);
            command.Parameters.AddWithValue("@IlId", ilId);
            return Convert.ToInt32(await command.ExecuteScalarAsync());
        }

        public async Task<bool> SilAsync(int degerZinciriId, int ilId)
        {
            const string sql = @"DELETE FROM dbo.DegerZinciriIl WHERE DegerZinciriId=@DegerZinciriId AND IlId=@IlId;";
            await using SqlCommand command = KomutOlustur(sql);
            command.Parameters.AddWithValue("@DegerZinciriId", degerZinciriId);
            command.Parameters.AddWithValue("@IlId", ilId);
            return await command.ExecuteNonQueryAsync() > 0;
        }

        private static DegerZinciriIl Oku(SqlDataReader reader)
        {
            return new DegerZinciriIl
            {
                Id = reader.GetInt32(0),
                DegerZinciriId = reader.GetInt32(1),
                IlId = reader.GetInt32(2),
                Aktif = OrtakFonksiyonlar.Int32Yap(reader.GetValue(3)) == 1
            };
        }
    }
}
