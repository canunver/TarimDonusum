using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Localization;
using TarimDonusum.Models;

namespace TarimDonusum.Tablolar
{
    public class TABBasvuruMetraj : TABTablo
    {
        public TABBasvuruMetraj(SqlConnection connection, IStringLocalizer<SharedResource>? localizer = null, SqlTransaction? transaction = null) : base(connection, localizer, transaction) { }

        public async Task<BasvuruMetrajVerisi> OkuAsync(int basvuruId, int donemId)
        {
            const string sql = @"SELECT Id,SiraNo,Ad FROM dbo.BasvuruBina WHERE BasvuruId=@BasvuruId AND MevcutYeni=N'Yeni' ORDER BY SiraNo,Id;
SELECT mb.Id,mb.BinaId,mb.SiraNo,mb.Ad FROM dbo.BasvuruMetrajBolum mb INNER JOIN dbo.BasvuruBina b ON b.Id=mb.BinaId WHERE b.BasvuruId=@BasvuruId ORDER BY mb.BinaId,mb.SiraNo,mb.Id;
SELECT mp.Id,mp.BolumId,mp.PozId,mp.SiraNo,mp.BirimFiyat,p.PozNo,p.Ad,p.Birim,p.HesaplamaTuru FROM dbo.BasvuruMetrajPoz mp INNER JOIN dbo.BasvuruMetrajBolum mb ON mb.Id=mp.BolumId INNER JOIN dbo.BasvuruBina b ON b.Id=mb.BinaId INNER JOIN dbo.Poz p ON p.Id=mp.PozId WHERE b.BasvuruId=@BasvuruId ORDER BY mp.BolumId,mp.SiraNo,mp.Id;
SELECT d.Id,d.MetrajPozId,d.SiraNo,d.Aciklama,d.Adet,d.Boy,d.En,d.Yukseklik FROM dbo.BasvuruMetrajDetay d INNER JOIN dbo.BasvuruMetrajPoz mp ON mp.Id=d.MetrajPozId INNER JOIN dbo.BasvuruMetrajBolum mb ON mb.Id=mp.BolumId INNER JOIN dbo.BasvuruBina b ON b.Id=mb.BinaId WHERE b.BasvuruId=@BasvuruId ORDER BY d.MetrajPozId,d.SiraNo,d.Id;
SELECT p.Id,@DonemId,p.Id,f.BirimFiyat,p.PozNo,p.Ad,p.Birim FROM dbo.Poz p LEFT JOIN dbo.PozDonemFiyat f ON f.PozId=p.Id AND f.DonemId=@DonemId WHERE p.Aktif=1 ORDER BY p.PozNo;";
            await using SqlCommand c=KomutOlustur(sql);c.Parameters.AddWithValue("@BasvuruId",basvuruId);c.Parameters.AddWithValue("@DonemId",donemId);
            await using SqlDataReader r=await c.ExecuteReaderAsync();BasvuruMetrajVerisi v=new(){basvuruId=basvuruId};
            while(await r.ReadAsync())v.binalar.Add(new(){id=r.GetInt32(0),siraNo=r.GetInt32(1),ad=r.GetString(2)});
            await r.NextResultAsync();while(await r.ReadAsync()){BasvuruMetrajBolum x=new(){id=r.GetInt32(0),basvuruId=basvuruId,binaId=r.GetInt32(1),siraNo=r.GetInt32(2),ad=r.GetString(3)};v.binalar.FirstOrDefault(b=>b.id==x.binaId)?.bolumler.Add(x);}
            await r.NextResultAsync();List<BasvuruMetrajPoz> pozlar=[];while(await r.ReadAsync()){BasvuruMetrajPoz x=new(){id=r.GetInt32(0),basvuruId=basvuruId,bolumId=r.GetInt32(1),pozId=r.GetInt32(2),siraNo=r.GetInt32(3),birimFiyat=r.GetDecimal(4),pozNo=r.GetString(5),pozAdi=r.GetString(6),birim=r.GetString(7),hesaplamaTuru=r.GetInt32(8)};pozlar.Add(x);v.binalar.SelectMany(b=>b.bolumler).FirstOrDefault(b=>b.id==x.bolumId)?.pozlar.Add(x);}
            await r.NextResultAsync();while(await r.ReadAsync()){BasvuruMetrajDetay d=new(){id=r.GetInt32(0),siraNo=r.GetInt32(2),aciklama=r.IsDBNull(3)?"":r.GetString(3),adet=NullOkuDecimal(r,4),boy=NullOkuDecimal(r,5),en=NullOkuDecimal(r,6),yukseklik=NullOkuDecimal(r,7)};pozlar.FirstOrDefault(x=>x.id==r.GetInt32(1))?.detaylar.Add(d);}
            await r.NextResultAsync();while(await r.ReadAsync())v.pozlar.Add(new(){id=r.GetInt32(0),donemId=r.GetInt32(1),pozId=r.GetInt32(2),birimFiyat=NullOkuDecimal(r,3),pozNo=r.GetString(4),pozAdi=r.GetString(5),birim=r.GetString(6)});
            return v;
        }

        public async Task BolumKaydetAsync(BasvuruMetrajBolum x)
        {
            string sql=x.id>0?"UPDATE mb SET SiraNo=@SiraNo,Ad=@Ad FROM dbo.BasvuruMetrajBolum mb INNER JOIN dbo.BasvuruBina b ON b.Id=mb.BinaId WHERE mb.Id=@Id AND b.BasvuruId=@BasvuruId;":"INSERT dbo.BasvuruMetrajBolum(BinaId,SiraNo,Ad) SELECT @BinaId,@SiraNo,@Ad FROM dbo.BasvuruBina WHERE Id=@BinaId AND BasvuruId=@BasvuruId AND MevcutYeni=N'Yeni';SELECT CAST(SCOPE_IDENTITY() AS INT);";
            await using SqlCommand c=KomutOlustur(sql);c.Parameters.AddWithValue("@Id",x.id);c.Parameters.AddWithValue("@BasvuruId",x.basvuruId);c.Parameters.AddWithValue("@BinaId",x.binaId);c.Parameters.AddWithValue("@SiraNo",x.siraNo);c.Parameters.AddWithValue("@Ad",x.ad);if(x.id>0){if(await c.ExecuteNonQueryAsync()==0)throw new InvalidOperationException("Yapım bölümü bulunamadı.");}else x.id=Convert.ToInt32(await c.ExecuteScalarAsync()??throw new InvalidOperationException("Yeni bina bulunamadı."));
        }
        public async Task<bool> BolumSilAsync(int basvuruId,int id){await using SqlCommand c=KomutOlustur("DELETE mb FROM dbo.BasvuruMetrajBolum mb INNER JOIN dbo.BasvuruBina b ON b.Id=mb.BinaId WHERE mb.Id=@Id AND b.BasvuruId=@BasvuruId;");c.Parameters.AddWithValue("@Id",id);c.Parameters.AddWithValue("@BasvuruId",basvuruId);return await c.ExecuteNonQueryAsync()>0;}

        public async Task PozKaydetAsync(BasvuruMetrajPoz x)
        {
            if(x.id>0){await using SqlCommand c=KomutOlustur("UPDATE mp SET PozId=@PozId,SiraNo=@SiraNo,BirimFiyat=@BirimFiyat FROM dbo.BasvuruMetrajPoz mp INNER JOIN dbo.BasvuruMetrajBolum mb ON mb.Id=mp.BolumId INNER JOIN dbo.BasvuruBina b ON b.Id=mb.BinaId WHERE mp.Id=@Id AND b.BasvuruId=@BasvuruId;");Parametreler(c,x);if(await c.ExecuteNonQueryAsync()==0)throw new InvalidOperationException("Metraj pozu bulunamadı.");await using SqlCommand sil=KomutOlustur("DELETE dbo.BasvuruMetrajDetay WHERE MetrajPozId=@Id;");sil.Parameters.AddWithValue("@Id",x.id);await sil.ExecuteNonQueryAsync();}
            else{await using SqlCommand c=KomutOlustur("INSERT dbo.BasvuruMetrajPoz(BolumId,PozId,SiraNo,BirimFiyat) SELECT @BolumId,@PozId,@SiraNo,@BirimFiyat FROM dbo.BasvuruMetrajBolum mb INNER JOIN dbo.BasvuruBina b ON b.Id=mb.BinaId WHERE mb.Id=@BolumId AND b.BasvuruId=@BasvuruId;SELECT CAST(SCOPE_IDENTITY() AS INT);");Parametreler(c,x);x.id=Convert.ToInt32(await c.ExecuteScalarAsync()??throw new InvalidOperationException("Yapım bölümü bulunamadı."));}
            foreach(BasvuruMetrajDetay d in x.detaylar){await using SqlCommand c=KomutOlustur("INSERT dbo.BasvuruMetrajDetay(MetrajPozId,SiraNo,Aciklama,Adet,Boy,En,Yukseklik) VALUES(@PozId,@SiraNo,@Aciklama,@Adet,@Boy,@En,@Yukseklik);");c.Parameters.AddWithValue("@PozId",x.id);c.Parameters.AddWithValue("@SiraNo",d.siraNo);c.Parameters.AddWithValue("@Aciklama",string.IsNullOrWhiteSpace(d.aciklama)?DBNull.Value:d.aciklama);c.Parameters.AddWithValue("@Adet",(object?)d.adet??DBNull.Value);c.Parameters.AddWithValue("@Boy",(object?)d.boy??DBNull.Value);c.Parameters.AddWithValue("@En",(object?)d.en??DBNull.Value);c.Parameters.AddWithValue("@Yukseklik",(object?)d.yukseklik??DBNull.Value);await c.ExecuteNonQueryAsync();}
        }
        private static void Parametreler(SqlCommand c,BasvuruMetrajPoz x){c.Parameters.AddWithValue("@Id",x.id);c.Parameters.AddWithValue("@BasvuruId",x.basvuruId);c.Parameters.AddWithValue("@BolumId",x.bolumId);c.Parameters.AddWithValue("@PozId",x.pozId);c.Parameters.AddWithValue("@SiraNo",x.siraNo);c.Parameters.AddWithValue("@BirimFiyat",x.birimFiyat);}
        public async Task<bool> PozSilAsync(int basvuruId,int id){await using SqlCommand c=KomutOlustur("DELETE mp FROM dbo.BasvuruMetrajPoz mp INNER JOIN dbo.BasvuruMetrajBolum mb ON mb.Id=mp.BolumId INNER JOIN dbo.BasvuruBina b ON b.Id=mb.BinaId WHERE mp.Id=@Id AND b.BasvuruId=@BasvuruId;");c.Parameters.AddWithValue("@Id",id);c.Parameters.AddWithValue("@BasvuruId",basvuruId);return await c.ExecuteNonQueryAsync()>0;}
    }
}
