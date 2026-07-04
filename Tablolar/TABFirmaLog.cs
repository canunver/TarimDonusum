using System.Text.Json;
using Microsoft.Data.SqlClient;
using TarimDonusum.Models;

namespace TarimDonusum.Tablolar
{
    public class TABFirmaLog : TABTablo
    {
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public TABFirmaLog(SqlConnection connection, SqlTransaction? transaction = null)
            : base(connection, transaction)
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
            command.Parameters.AddWithValue("@FirmaId", firma.Id);
            command.Parameters.AddWithValue("@IslemTarihi", DateTime.Now);
            command.Parameters.AddWithValue("@Islem", islem);
            command.Parameters.AddWithValue("@JsonText", JsonSerializer.Serialize(firma, JsonOptions));

            await command.ExecuteNonQueryAsync();
        }
    }
}
