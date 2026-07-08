using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Localization;
using TarimDonusum.Models;

namespace TarimDonusum.Tablolar
{
    public class TABFirmaLog : TABTablo
    {
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public TABFirmaLog(SqlConnection connection, IStringLocalizer<SharedResource>? localizer = null, SqlTransaction? transaction = null)
            : base(connection, localizer, transaction)
        {
        }

        public async Task EkleAsync(Firma firma, string islem)
        {
            const string sql = @"
                INSERT INTO dbo.FirmaLog
                (
                    FirmaId,
                    KullaniciId,
                    IslemTarihi,
                    Islem,
                    JsonText
                )
                VALUES
                (
                    @FirmaId,
                    @KullaniciId,
                    @IslemTarihi,
                    @Islem,
                    @JsonText
                );";

            await using SqlCommand command = KomutOlustur(sql);
            command.Parameters.AddWithValue("@FirmaId", firma.id);
            command.Parameters.AddWithValue("@IslemTarihi", DateTime.Now);
            command.Parameters.AddWithValue("@Islem", islem);
            command.Parameters.AddWithValue("@JsonText", JsonSerializer.Serialize(firma, JsonOptions));

            await command.ExecuteNonQueryAsync();
        }
    }
}
