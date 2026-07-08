using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Localization;
using TarimDonusum.Models;

namespace TarimDonusum.Tablolar
{
    public class TABDosyaLog : TABTablo
    {
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public TABDosyaLog(SqlConnection connection, IStringLocalizer<SharedResource>? localizer = null, SqlTransaction? transaction = null)
            : base(connection, localizer, transaction)
        {
        }

        public async Task EkleAsync(DosyaAnahtari anahtar, int? dosyaId, string islem, object? islemDetayi = null)
        {
            const string sql = @"
                INSERT INTO dbo.DosyaBilgisiLog
                (
                    DosyaId,
                    ModulKod,
                    FormAd,
                    FormAnahtar,
                    DosyaNo,
                    IslemTarihi,
                    Islem,
                    JsonText
                )
                VALUES
                (
                    @DosyaId,
                    @ModulKod,
                    @FormAd,
                    @FormAnahtar,
                    @DosyaNo,
                    @IslemTarihi,
                    @Islem,
                    @JsonText
                );";

            await using SqlCommand command = KomutOlustur(sql);
            command.Parameters.AddWithValue("@DosyaId", dosyaId.HasValue ? dosyaId.Value : DBNull.Value);
            command.Parameters.AddWithValue("@ModulKod", anahtar.ModulKod);
            command.Parameters.AddWithValue("@FormAd", anahtar.FormAd);
            command.Parameters.AddWithValue("@FormAnahtar", anahtar.FormAnahtar);
            command.Parameters.AddWithValue("@DosyaNo", anahtar.DosyaNo);
            command.Parameters.AddWithValue("@IslemTarihi", DateTime.Now);
            command.Parameters.AddWithValue("@Islem", islem);
            command.Parameters.AddWithValue("@JsonText", JsonSerializer.Serialize(islemDetayi ?? anahtar, JsonOptions));

            await command.ExecuteNonQueryAsync();
        }
    }
}
