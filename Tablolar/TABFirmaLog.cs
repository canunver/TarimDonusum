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

        public async Task EkleAsync(Firma firma, string islem, int kullaniciId = 0, object? detay = null)
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
            command.Parameters.AddWithValue("@KullaniciId", kullaniciId);
            command.Parameters.AddWithValue("@IslemTarihi", DateTime.Now);
            command.Parameters.AddWithValue("@Islem", islem);
            command.Parameters.AddWithValue("@JsonText", JsonSerializer.Serialize(detay ?? firma, JsonOptions));

            await command.ExecuteNonQueryAsync();
        }

        public async Task<List<FirmaLogGorunum>> ListeleAsync(int firmaId)
        {
            const string sql = @"
                SELECT fl.Id, fl.IslemTarihi, fl.Islem,
                       LTRIM(RTRIM(ISNULL(k.Ad, N'') + N' ' + ISNULL(k.Soyad, N''))), fl.JsonText
                FROM dbo.FirmaLog fl
                LEFT JOIN dbo.Kullanici k ON k.Id = fl.KullaniciId
                WHERE fl.FirmaId = @FirmaId
                ORDER BY fl.IslemTarihi DESC, fl.Id DESC;";
            await using SqlCommand command = KomutOlustur(sql);
            command.Parameters.AddWithValue("@FirmaId", firmaId);
            await using SqlDataReader reader = await command.ExecuteReaderAsync();
            List<FirmaLogGorunum> liste = new();
            while (await reader.ReadAsync())
            {
                liste.Add(new FirmaLogGorunum
                {
                    Id = reader.GetInt32(0),
                    IslemTarihi = reader.GetDateTime(1),
                    Islem = reader.GetString(2),
                    KullaniciAdSoyad = reader.GetString(3),
                    JsonText = reader.GetString(4)
                });
            }
            return liste;
        }
    }
}
