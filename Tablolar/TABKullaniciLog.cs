using Microsoft.Data.SqlClient;
using TarimDonusum.Araclar;
using TarimDonusum.Models;

namespace TarimDonusum.Tablolar
{
    public class TABKullaniciLog : TABTablo
    {
        public TABKullaniciLog(SqlConnection connection, SqlTransaction? transaction = null)
            : base(connection, transaction)
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
    }
}
