using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Localization;
using TarimDonusum.Araclar;
using TarimDonusum.Models;

namespace TarimDonusum.Tablolar
{
    public class TABKullaniciLog : TABTablo
    {
        public TABKullaniciLog(SqlConnection connection, IStringLocalizer<SharedResource>? localizer = null, SqlTransaction? transaction = null)
            : base(connection, localizer, transaction)
        {
        }

        public async Task<int> EkleAsync(KullaniciLog log)
        {
            const string sql = @"
                INSERT INTO dbo.KullaniciLog
                (
                    KullaniciId,
                    IslemYapanKullaniciId,
                    IslemTarihi,
                    Islem,
                    JsonText
                )
                OUTPUT INSERTED.Id
                VALUES
                (
                    @KullaniciId,
                    @IslemYapanKullaniciId,
                    @IslemTarihi,
                    @Islem,
                    @JsonText
                );";

            await using SqlCommand command = KomutOlustur(sql);
            object islemYapanKullaniciId = log.IslemYapanKullaniciId.HasValue ? (object)log.IslemYapanKullaniciId.Value : DBNull.Value;

            command.Parameters.AddWithValue("@KullaniciId", log.KullaniciId);
            command.Parameters.AddWithValue("@IslemYapanKullaniciId", islemYapanKullaniciId);
            command.Parameters.AddWithValue("@IslemTarihi", log.IslemTarihi);
            command.Parameters.AddWithValue("@Islem", log.Islem);
            command.Parameters.AddWithValue("@JsonText", log.JsonText);

            object? sonuc = await command.ExecuteScalarAsync();
            int id = OrtakFonksiyonlar.Int32Yap(sonuc);
            log.Id = id;

            return id;
        }

        public async Task<bool> ParolaBaglantisiGecerliMiAsync(
            int kullaniciId, long zamanUtc, string linkKodu, DateTime enErkenTarih)
        {
            const string sql = @"
                SELECT COUNT(1)
                FROM dbo.KullaniciLog L
                WHERE L.KullaniciId = @KullaniciId
                  AND L.Islem = N'ParolaBelirlemeBaglantisiGonderildi'
                  AND L.IslemTarihi >= @EnErkenTarih
                  AND TRY_CONVERT(BIGINT, JSON_VALUE(L.JsonText, '$.ZamanUtc')) = @ZamanUtc
                  AND JSON_VALUE(L.JsonText, '$.LinkKodu') = @LinkKodu
                  AND NOT EXISTS
                  (
                      SELECT 1 FROM dbo.KullaniciLog K
                      WHERE K.KullaniciId = L.KullaniciId
                        AND K.Islem = N'ParolaBelirlendi'
                        AND JSON_VALUE(K.JsonText, '$.LinkKodu') = @LinkKodu
                        AND K.IslemTarihi >= L.IslemTarihi
                  );";
            await using SqlCommand command = KomutOlustur(sql);
            command.Parameters.AddWithValue("@KullaniciId", kullaniciId);
            command.Parameters.AddWithValue("@ZamanUtc", zamanUtc);
            command.Parameters.AddWithValue("@LinkKodu", linkKodu);
            command.Parameters.AddWithValue("@EnErkenTarih", enErkenTarih);
            return Convert.ToInt32(await command.ExecuteScalarAsync()) > 0;
        }
    }
}
