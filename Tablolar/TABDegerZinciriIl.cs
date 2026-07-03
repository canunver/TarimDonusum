using Microsoft.Data.SqlClient;
using TarimDonusum.Araclar;
using TarimDonusum.Models;

namespace TarimDonusum.Tablolar
{
    public class TABDegerZinciriIl : TABTablo
    {
        public TABDegerZinciriIl(SqlConnection connection, SqlTransaction? transaction = null)
            : base(connection, transaction)
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
