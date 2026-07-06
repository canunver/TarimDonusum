using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Localization;
using TarimDonusum.Araclar;
using TarimDonusum.Models;

namespace TarimDonusum.Tablolar
{
    public class TABFirmaKullanici : TABTablo
    {
        public TABFirmaKullanici(SqlConnection connection, IStringLocalizer<SharedResource>? localizer = null, SqlTransaction? transaction = null)
            : base(connection, localizer, transaction)
        {
        }

        public async Task<bool> IliskiVarMiAsync(int firmaId, int kullaniciId)
        {
            const string sql = @"
                SELECT COUNT(1)
                FROM dbo.FirmaKullanici
                WHERE FirmaId = @FirmaId
                    AND KullaniciId = @KullaniciId
                    AND Aktif = 1;";

            await using SqlCommand command = KomutOlustur(sql);
            command.Parameters.AddWithValue("@FirmaId", firmaId);
            command.Parameters.AddWithValue("@KullaniciId", kullaniciId);

            return OrtakFonksiyonlar.Int32Yap(await command.ExecuteScalarAsync()) > 0;
        }

        public async Task EkleYoksaAsync(FirmaKullanici iliski)
        {
            const string sql = @"
                IF NOT EXISTS
                (
                    SELECT 1
                    FROM dbo.FirmaKullanici
                    WHERE FirmaId = @FirmaId AND KullaniciId = @KullaniciId
                )
                BEGIN
                    INSERT INTO dbo.FirmaKullanici
                    (
                        FirmaId,
                        KullaniciId,
                        Aktif,
                        IliskiTarihi,
                        IliskiyiKuranKullaniciId
                    )
                    VALUES
                    (
                        @FirmaId,
                        @KullaniciId,
                        @Aktif,
                        @IliskiTarihi,
                        @IliskiyiKuranKullaniciId
                    );
                END
                ELSE
                BEGIN
                    UPDATE dbo.FirmaKullanici
                    SET Aktif = 1
                    WHERE FirmaId = @FirmaId AND KullaniciId = @KullaniciId;
                END";

            await using SqlCommand command = KomutOlustur(sql);
            command.Parameters.AddWithValue("@FirmaId", iliski.FirmaId);
            command.Parameters.AddWithValue("@KullaniciId", iliski.KullaniciId);
            command.Parameters.AddWithValue("@Aktif", iliski.Aktif ? 1 : 0);
            command.Parameters.AddWithValue("@IliskiTarihi", iliski.IliskiTarihi);
            command.Parameters.AddWithValue("@IliskiyiKuranKullaniciId", (object?)iliski.IliskiyiKuranKullaniciId ?? DBNull.Value);

            await command.ExecuteNonQueryAsync();
        }
    }
}
