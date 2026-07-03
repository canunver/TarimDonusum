using System.Text.Json;
using Microsoft.Data.SqlClient;
using TarimDonusum.Models;

namespace TarimDonusum.Tablolar
{
    public class TABBasvuruLog : TABTablo
    {
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public TABBasvuruLog(SqlConnection connection, SqlTransaction? transaction = null)
            : base(connection, transaction)
        {
        }

        public async Task EkleAsync(Basvuru basvuru, string islem, object? islemDetayi = null)
        {
            const string sql = @"
                INSERT INTO dbo.BasvuruLog
                (
                    BasvuruId,
                    KullaniciId,
                    IslemTarihi,
                    Islem,
                    JsonText
                )
                VALUES
                (
                    @BasvuruId,
                    @KullaniciId,
                    @IslemTarihi,
                    @Islem,
                    @JsonText
                );";

            await using SqlCommand command = KomutOlustur(sql);
            command.Parameters.AddWithValue("@BasvuruId", basvuru.Id);
            command.Parameters.AddWithValue("@KullaniciId", basvuru.KullaniciId);
            command.Parameters.AddWithValue("@IslemTarihi", DateTime.Now);
            command.Parameters.AddWithValue("@Islem", islem);
            command.Parameters.AddWithValue("@JsonText", JsonSerializer.Serialize(new
            {
                basvuru,
                islemDetayi
            }, JsonOptions));

            await command.ExecuteNonQueryAsync();
        }
    }
}
