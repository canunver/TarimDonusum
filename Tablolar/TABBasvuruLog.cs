using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Localization;
using TarimDonusum.Models;

namespace TarimDonusum.Tablolar
{
    public class TABBasvuruLog : TABTablo
    {
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            //PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public TABBasvuruLog(SqlConnection connection, IStringLocalizer<SharedResource>? localizer = null, SqlTransaction? transaction = null)
            : base(connection, localizer, transaction)
        {
        }

        public async Task EkleAsync(int basvuruId, Kullanici kullanici, string islem, object? islemDetayi = null)
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
            command.Parameters.AddWithValue("@BasvuruId", basvuruId);
            command.Parameters.AddWithValue("@KullaniciId", kullanici.Id);
            command.Parameters.AddWithValue("@IslemTarihi", DateTime.Now);
            command.Parameters.AddWithValue("@Islem", islem);
            command.Parameters.AddWithValue("@JsonText", JsonSerializer.Serialize(new
            {
                islemDetayi
            }, JsonOptions));

            await command.ExecuteNonQueryAsync();
        }
    }
}
