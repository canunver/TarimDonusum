using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Localization;
using System.Text.Json;
using TarimDonusum.Models;

namespace TarimDonusum.Tablolar;

public class TABUygunlukSorusu : TABTablo
{
    public TABUygunlukSorusu(SqlConnection connection, IStringLocalizer<SharedResource>? localizer = null, SqlTransaction? transaction = null)
        : base(connection, localizer, transaction) { }

    public async Task<List<UygunlukSorusu>> ListeleAsync(bool sadeceAktif = false)
    {
        await VarsayilanlariEkleAsync();
        const string sql = @"SELECT Id,SiraNo,Konu,Soru,Kaynak,BirimTuru,EvetSonucu,HayirSonucu,Aktif,ZorunluBelgeNolariJson
            FROM dbo.UygunlukSorusu WHERE @Aktif=0 OR Aktif=1 ORDER BY SiraNo,Id;";
        await using SqlCommand c = KomutOlustur(sql);
        c.Parameters.AddWithValue("@Aktif", sadeceAktif ? 1 : 0);
        await using SqlDataReader r = await c.ExecuteReaderAsync();
        List<UygunlukSorusu> liste = [];
        while (await r.ReadAsync()) liste.Add(new UygunlukSorusu {
            id=r.GetInt32(0), siraNo=r.GetInt32(1), konu=r.GetString(2), soru=r.GetString(3), kaynak=r.GetString(4),
            birimTuru=r.IsDBNull(5)?"":r.GetString(5), evetSonucu=r.GetString(6), hayirSonucu=r.GetString(7), aktif=r.GetInt32(8)==1,
            zorunluBelgeNolari=JsonSerializer.Deserialize<List<int>>(r.GetString(9))??[]
        });
        return liste;
    }

    public async Task<int> KaydetAsync(UygunlukSorusu x)
    {
        const string sql = @"IF @Id=0 BEGIN INSERT dbo.UygunlukSorusu(SiraNo,Konu,Soru,Kaynak,BirimTuru,EvetSonucu,HayirSonucu,Aktif,ZorunluBelgeNolariJson)
            OUTPUT INSERTED.Id VALUES(@SiraNo,@Konu,@Soru,@Kaynak,@BirimTuru,@EvetSonucu,@HayirSonucu,@Aktif,@Belgeler); END
            ELSE BEGIN UPDATE dbo.UygunlukSorusu SET SiraNo=@SiraNo,Konu=@Konu,Soru=@Soru,Kaynak=@Kaynak,BirimTuru=@BirimTuru,
            EvetSonucu=@EvetSonucu,HayirSonucu=@HayirSonucu,Aktif=@Aktif,ZorunluBelgeNolariJson=@Belgeler WHERE Id=@Id; SELECT @Id; END";
        await using SqlCommand c=KomutOlustur(sql); ParametreEkle(c,x);
        return Convert.ToInt32(await c.ExecuteScalarAsync());
    }

    private static void ParametreEkle(SqlCommand c,UygunlukSorusu x)
    {
        c.Parameters.AddWithValue("@Id",x.id); c.Parameters.AddWithValue("@SiraNo",x.siraNo);
        c.Parameters.AddWithValue("@Konu",x.konu); c.Parameters.AddWithValue("@Soru",x.soru); c.Parameters.AddWithValue("@Kaynak",x.kaynak);
        c.Parameters.AddWithValue("@BirimTuru",string.IsNullOrWhiteSpace(x.birimTuru)?DBNull.Value:x.birimTuru);
        c.Parameters.AddWithValue("@EvetSonucu",x.evetSonucu); c.Parameters.AddWithValue("@HayirSonucu",x.hayirSonucu); c.Parameters.AddWithValue("@Aktif",x.aktif?1:0);
        c.Parameters.AddWithValue("@Belgeler",JsonSerializer.Serialize(x.zorunluBelgeNolari.Distinct().OrderBy(n=>n)));
    }

    private async Task VarsayilanlariEkleAsync()
    {
        await using SqlCommand say=KomutOlustur("SELECT COUNT(1) FROM dbo.UygunlukSorusu;");
        if(Convert.ToInt32(await say.ExecuteScalarAsync())>0) return;
        List<UzmanSonucSozlukMaddesi> maddeler=JsonSerializer.Deserialize<List<UzmanSonucSozlukMaddesi>>(UzmanKontrolListesi.Json)??[];
        foreach(var m in maddeler.Where(x=>x.no is not (22 or 23))) await KaydetAsync(new UygunlukSorusu {
            siraNo=m.no, konu=m.konu, soru=m.soru, kaynak=m.kaynak, birimTuru=m.birimTuru, evetSonucu="Kabul", hayirSonucu="Ret", aktif=true
        });
    }
}
