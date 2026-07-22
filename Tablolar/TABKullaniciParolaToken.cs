using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Localization;

namespace TarimDonusum.Tablolar
{
    public class TABKullaniciParolaToken : TABTablo
    {
        public TABKullaniciParolaToken(SqlConnection connection, IStringLocalizer<SharedResource>? localizer = null,
            SqlTransaction? transaction = null) : base(connection, localizer, transaction) { }

        public async Task EkleAsync(int kullaniciId, string tokenHash, DateTime sonKullanma)
        {
            const string sql = @"
                UPDATE dbo.KullaniciParolaToken SET Kullanildi = 1 WHERE KullaniciId = @KullaniciId AND Kullanildi = 0;
                INSERT INTO dbo.KullaniciParolaToken (KullaniciId, TokenHash, SonKullanma, Kullanildi)
                VALUES (@KullaniciId, @TokenHash, @SonKullanma, 0);";
            await using SqlCommand command = KomutOlustur(sql);
            command.Parameters.AddWithValue("@KullaniciId", kullaniciId);
            command.Parameters.AddWithValue("@TokenHash", tokenHash);
            command.Parameters.AddWithValue("@SonKullanma", sonKullanma);
            await command.ExecuteNonQueryAsync();
        }

        public async Task<int?> GecerliKullaniciIdAsync(string tokenHash)
        {
            const string sql = @"SELECT TOP 1 KullaniciId FROM dbo.KullaniciParolaToken
                WHERE TokenHash = @TokenHash AND Kullanildi = 0 AND SonKullanma > GETDATE();";
            await using SqlCommand command = KomutOlustur(sql);
            command.Parameters.AddWithValue("@TokenHash", tokenHash);
            object? value = await command.ExecuteScalarAsync();
            return value == null || value == DBNull.Value ? null : Convert.ToInt32(value);
        }

        public async Task KullanildiYapAsync(string tokenHash)
        {
            await using SqlCommand command = KomutOlustur(
                "UPDATE dbo.KullaniciParolaToken SET Kullanildi = 1 WHERE TokenHash = @TokenHash;");
            command.Parameters.AddWithValue("@TokenHash", tokenHash);
            await command.ExecuteNonQueryAsync();
        }
    }
}
