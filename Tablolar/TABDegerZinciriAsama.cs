using Microsoft.Data.SqlClient;
using TarimDonusum.Araclar;
using TarimDonusum.Models;

namespace TarimDonusum.Tablolar
{
    public class TABDegerZinciriAsama : TABTablo
    {
        public TABDegerZinciriAsama(SqlConnection connection, SqlTransaction? transaction = null)
            : base(connection, transaction)
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

        private static DegerZinciriAsama Oku(SqlDataReader reader)
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
