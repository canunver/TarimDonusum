using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Localization;
using TarimDonusum.Models;

namespace TarimDonusum.Tablolar
{
    public class TABPoz : TABTablo
    {
        public TABPoz(SqlConnection connection, IStringLocalizer<SharedResource>? localizer = null, SqlTransaction? transaction = null)
            : base(connection, localizer, transaction) { }

        public async Task<List<Poz>> ListeleAsync(bool sadeceAktif = false)
        {
            string sql = @"SELECT Id, PozNo, Ad, Birim, HesaplamaTuru, Aktif
                           FROM dbo.Poz" + (sadeceAktif ? " WHERE Aktif=1" : "") + " ORDER BY PozNo, Ad;";
            await using SqlCommand command = KomutOlustur(sql);
            await using SqlDataReader reader = await command.ExecuteReaderAsync();
            List<Poz> liste = new();
            while (await reader.ReadAsync())
                liste.Add(new Poz
                {
                    id = reader.GetInt32(0),
                    pozNo = reader.GetString(1),
                    ad = reader.GetString(2),
                    birim = reader.GetString(3),
                    hesaplamaTuru = (enumPozHesaplamaTuru)reader.GetInt32(4),
                    aktif = reader.GetInt32(5) == 1
                });
            return liste;
        }

        public async Task<int> EkleAsync(Poz model)
        {
            const string sql = @"INSERT dbo.Poz(PozNo,Ad,Birim,HesaplamaTuru,Aktif)
                                 OUTPUT INSERTED.Id VALUES(@PozNo,@Ad,@Birim,@HesaplamaTuru,@Aktif);";
            await using SqlCommand command = KomutOlustur(sql);
            ParametreEkle(command, model);
            return Convert.ToInt32(await command.ExecuteScalarAsync());
        }

        public async Task<bool> GuncelleAsync(Poz model)
        {
            const string sql = @"UPDATE dbo.Poz SET PozNo=@PozNo,Ad=@Ad,Birim=@Birim,
                                 HesaplamaTuru=@HesaplamaTuru,Aktif=@Aktif WHERE Id=@Id;";
            await using SqlCommand command = KomutOlustur(sql);
            command.Parameters.AddWithValue("@Id", model.id);
            ParametreEkle(command, model);
            return await command.ExecuteNonQueryAsync() > 0;
        }

        private static void ParametreEkle(SqlCommand command, Poz model)
        {
            command.Parameters.AddWithValue("@PozNo", model.pozNo);
            command.Parameters.AddWithValue("@Ad", model.ad);
            command.Parameters.AddWithValue("@Birim", model.birim);
            command.Parameters.AddWithValue("@HesaplamaTuru", (int)model.hesaplamaTuru);
            command.Parameters.AddWithValue("@Aktif", model.aktif ? 1 : 0);
        }
    }
}
