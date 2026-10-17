using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Localization;
using TarimDonusum.Models;

namespace TarimDonusum.Tablolar;

/// <summary>Tanımlar > Sıralama Puanı Kriterleri: konu, alt kriter ve koşul tabloları.</summary>
public class TABSiralamaPuaniTanim : TABTablo
{
    public TABSiralamaPuaniTanim(SqlConnection connection, IStringLocalizer<SharedResource>? localizer = null, SqlTransaction? transaction = null)
        : base(connection, localizer, transaction) { }

    public async Task<List<SiralamaPuaniKonu>> ListeleAsync()
    {
        const string sql = @"SELECT Id,SiraNo,Ad,AzamiPuan,Aktif FROM dbo.SiralamaPuaniKonu ORDER BY SiraNo,Id;
            SELECT Id,KonuId,SiraNo,Ad,TerimKodu,SonucMetni,VeriKaynagi,Aktif FROM dbo.SiralamaPuaniAltKriter ORDER BY KonuId,SiraNo,Id;
            SELECT Id,AltKriterId,SiraNo,Metin,KosulTuru,Deger,AltSinir,AltDahil,UstSinir,UstDahil,Puan FROM dbo.SiralamaPuaniKosul ORDER BY AltKriterId,SiraNo,Id;";
        await using SqlCommand command = KomutOlustur(sql);
        await using SqlDataReader r = await command.ExecuteReaderAsync();
        List<SiralamaPuaniKonu> konular = [];
        while (await r.ReadAsync())
            konular.Add(new() { id = r.GetInt32(0), siraNo = r.GetInt32(1), ad = r.GetString(2), azamiPuan = r.IsDBNull(3) ? null : r.GetDecimal(3), aktif = r.GetBoolean(4) });
        await r.NextResultAsync();
        List<SiralamaPuaniAltKriter> altKriterler = [];
        while (await r.ReadAsync())
            altKriterler.Add(new() { id = r.GetInt32(0), konuId = r.GetInt32(1), siraNo = r.GetInt32(2), ad = r.GetString(3), terimKodu = r.IsDBNull(4) ? null : r.GetString(4),
                sonucMetni = r.IsDBNull(5) ? "" : r.GetString(5), veriKaynagi = r.IsDBNull(6) ? "" : r.GetString(6), aktif = r.GetBoolean(7) });
        await r.NextResultAsync();
        while (await r.ReadAsync())
        {
            SiralamaPuaniKosul k = new() { id = r.GetInt32(0), altKriterId = r.GetInt32(1), siraNo = r.GetInt32(2), metin = r.GetString(3), kosulTuru = (enumSiralamaKosulTuru)r.GetInt32(4),
                deger = r.IsDBNull(5) ? null : r.GetDecimal(5), altSinir = r.IsDBNull(6) ? null : r.GetDecimal(6), altDahil = r.GetBoolean(7),
                ustSinir = r.IsDBNull(8) ? null : r.GetDecimal(8), ustDahil = r.GetBoolean(9), puan = r.GetDecimal(10) };
            altKriterler.FirstOrDefault(x => x.id == k.altKriterId)?.kosullar.Add(k);
        }
        foreach (SiralamaPuaniAltKriter a in altKriterler) konular.FirstOrDefault(x => x.id == a.konuId)?.altKriterler.Add(a);
        return konular;
    }

    public async Task<int> KonuKaydetAsync(SiralamaPuaniKonu k)
    {
        const string sql = @"IF @Id=0
                INSERT dbo.SiralamaPuaniKonu(SiraNo,Ad,AzamiPuan,Aktif) OUTPUT INSERTED.Id VALUES(@SiraNo,@Ad,@AzamiPuan,@Aktif);
            ELSE
            BEGIN
                UPDATE dbo.SiralamaPuaniKonu SET SiraNo=@SiraNo,Ad=@Ad,AzamiPuan=@AzamiPuan,Aktif=@Aktif,GuncellemeTarihi=SYSUTCDATETIME() WHERE Id=@Id;
                SELECT CASE WHEN @@ROWCOUNT=1 THEN @Id ELSE 0 END;
            END";
        await using SqlCommand c = KomutOlustur(sql);
        c.Parameters.AddWithValue("@Id", k.id); c.Parameters.AddWithValue("@SiraNo", k.siraNo); c.Parameters.AddWithValue("@Ad", k.ad);
        c.Parameters.AddWithValue("@AzamiPuan", (object?)k.azamiPuan ?? DBNull.Value); c.Parameters.AddWithValue("@Aktif", k.aktif);
        return Convert.ToInt32(await c.ExecuteScalarAsync());
    }

    public async Task<int> AltKriterKaydetAsync(SiralamaPuaniAltKriter a)
    {
        const string sql = @"IF @Id=0
                INSERT dbo.SiralamaPuaniAltKriter(KonuId,SiraNo,Ad,TerimKodu,SonucMetni,VeriKaynagi,Aktif) OUTPUT INSERTED.Id
                SELECT @KonuId,@SiraNo,@Ad,@TerimKodu,@SonucMetni,@VeriKaynagi,@Aktif WHERE EXISTS(SELECT 1 FROM dbo.SiralamaPuaniKonu WHERE Id=@KonuId);
            ELSE
            BEGIN
                UPDATE dbo.SiralamaPuaniAltKriter SET SiraNo=@SiraNo,Ad=@Ad,TerimKodu=@TerimKodu,SonucMetni=@SonucMetni,VeriKaynagi=@VeriKaynagi,Aktif=@Aktif,GuncellemeTarihi=SYSUTCDATETIME()
                WHERE Id=@Id AND KonuId=@KonuId;
                SELECT CASE WHEN @@ROWCOUNT=1 THEN @Id ELSE 0 END;
            END";
        await using SqlCommand c = KomutOlustur(sql);
        AltKriterParametreleri(c, a); c.Parameters.AddWithValue("@Id", a.id);
        return Convert.ToInt32(await c.ExecuteScalarAsync() ?? 0);
    }

    public async Task<int> KosulKaydetAsync(SiralamaPuaniKosul k)
    {
        const string sql = @"IF @Id=0
                INSERT dbo.SiralamaPuaniKosul(AltKriterId,SiraNo,Metin,KosulTuru,Deger,AltSinir,AltDahil,UstSinir,UstDahil,Puan) OUTPUT INSERTED.Id
                SELECT @AltKriterId,@SiraNo,@Metin,@KosulTuru,@Deger,@AltSinir,@AltDahil,@UstSinir,@UstDahil,@Puan WHERE EXISTS(SELECT 1 FROM dbo.SiralamaPuaniAltKriter WHERE Id=@AltKriterId);
            ELSE
            BEGIN
                UPDATE dbo.SiralamaPuaniKosul SET SiraNo=@SiraNo,Metin=@Metin,KosulTuru=@KosulTuru,Deger=@Deger,AltSinir=@AltSinir,AltDahil=@AltDahil,UstSinir=@UstSinir,UstDahil=@UstDahil,Puan=@Puan
                WHERE Id=@Id AND AltKriterId=@AltKriterId;
                SELECT CASE WHEN @@ROWCOUNT=1 THEN @Id ELSE 0 END;
            END";
        await using SqlCommand c = KomutOlustur(sql);
        KosulParametreleri(c, k); c.Parameters.AddWithValue("@Id", k.id);
        return Convert.ToInt32(await c.ExecuteScalarAsync() ?? 0);
    }

    /// <summary>tablo: Konu, AltKriter veya Kosul; alt kayıtlar yabancı anahtarla (cascade) silinir.</summary>
    public async Task<bool> SilAsync(string tablo, int id)
    {
        string ad = tablo switch { "Konu" => "SiralamaPuaniKonu", "AltKriter" => "SiralamaPuaniAltKriter", "Kosul" => "SiralamaPuaniKosul", _ => throw new ArgumentException("Geçersiz tablo.", nameof(tablo)) };
        await using SqlCommand c = KomutOlustur($"DELETE FROM dbo.{ad} WHERE Id=@Id;");
        c.Parameters.AddWithValue("@Id", id);
        return await c.ExecuteNonQueryAsync() == 1;
    }

    /// <summary>Tablolar boşsa Excel'deki kriterleri ekler.</summary>
    public async Task VarsayilanlariEkleAsync(IEnumerable<SiralamaPuaniKonu> varsayilanlar)
    {
        await using SqlCommand say = KomutOlustur("SELECT COUNT(1) FROM dbo.SiralamaPuaniKonu;");
        if (Convert.ToInt32(await say.ExecuteScalarAsync()) > 0) return;
        foreach (SiralamaPuaniKonu konu in varsayilanlar)
        {
            konu.id = 0;
            int konuId = await KonuKaydetAsync(konu);
            foreach (SiralamaPuaniAltKriter a in konu.altKriterler)
            {
                a.id = 0; a.konuId = konuId;
                int altKriterId = await AltKriterKaydetAsync(a);
                foreach (SiralamaPuaniKosul k in a.kosullar) { k.id = 0; k.altKriterId = altKriterId; await KosulKaydetAsync(k); }
            }
        }
    }

    private static void AltKriterParametreleri(SqlCommand c, SiralamaPuaniAltKriter a)
    {
        c.Parameters.AddWithValue("@KonuId", a.konuId); c.Parameters.AddWithValue("@SiraNo", a.siraNo); c.Parameters.AddWithValue("@Ad", a.ad);
        c.Parameters.AddWithValue("@TerimKodu", string.IsNullOrWhiteSpace(a.terimKodu) ? DBNull.Value : a.terimKodu);
        c.Parameters.AddWithValue("@SonucMetni", a.sonucMetni ?? ""); c.Parameters.AddWithValue("@VeriKaynagi", a.veriKaynagi ?? ""); c.Parameters.AddWithValue("@Aktif", a.aktif);
    }

    private static void KosulParametreleri(SqlCommand c, SiralamaPuaniKosul k)
    {
        c.Parameters.AddWithValue("@AltKriterId", k.altKriterId); c.Parameters.AddWithValue("@SiraNo", k.siraNo); c.Parameters.AddWithValue("@Metin", k.metin);
        c.Parameters.AddWithValue("@KosulTuru", (int)k.kosulTuru);
        c.Parameters.AddWithValue("@Deger", (object?)k.deger ?? DBNull.Value); c.Parameters.AddWithValue("@AltSinir", (object?)k.altSinir ?? DBNull.Value);
        c.Parameters.AddWithValue("@AltDahil", k.altDahil); c.Parameters.AddWithValue("@UstSinir", (object?)k.ustSinir ?? DBNull.Value);
        c.Parameters.AddWithValue("@UstDahil", k.ustDahil); c.Parameters.AddWithValue("@Puan", k.puan);
    }
}
