# Kimlik servisi bağlantısı

`appsettings.json` içindeki `TCKNServis` boşsa veya yoksa doğrulama `ServisYok (1)` döner ve kayıt devam eder. Doluysa bu değer servis uç noktasının tam HTTP/HTTPS adresidir. Gerçek servis henüz tanımlanmadığı için aşağıdaki JSON POST sözleşmesi hazırlanmıştır; gerçek MERNİS servisinin sözleşmesi ve kimlik doğrulama yöntemi geldiğinde `TCKNServisi` uyarlanmalıdır.

İstek (ortaklarda ad/unvan tek alan olduğundan ad ve soyad birlikte gönderilir):

```json
{"tckn":"10000000146","adSoyad":"Test Kişi","dogumTarihi":"1990-05-12"}
```

Başarılı HTTP yanıtının gövdesi:

```json
{"tckn":"10000000146","ad":"Test","soyad":"Kişi","dogumTarihi":"1990-05-12","cinsiyet":"Kadın"}
```

TCKN, tam ad ve doğum tarihi karşılaştırılır. Adlarda Türkçe büyük/küçük harf ve fazla boşluk farkları dikkate alınmaz. Cinsiyet `Kadın` veya `Erkek` olmalıdır; karşılaştırmaya dahil edilmez, eşleşmede servisten döner. Asenkron C# metotları `out` desteklemediği için `TCKNDogrulaAsync` cinsiyeti `TCKNDogrulamaSonucu.Cinsiyet` alanında döndürür.

- `Dogru (2)`: bilgiler eşleşti. Gerçek kişi ortağın cinsiyeti ve buna bağlı sahiplik niteliği güncellenir.
- `Hatali (3)`: bilgiler eşleşmedi; kayıt engellenir ve uyuşmazlık mesajı gösterilir.
- Ayar doluyken geçersiz adres, başarısız HTTP durumu (404 dahil), bağlantı hatası, 15 saniyelik zaman aşımı veya eksik/bozuk yanıt da `Hatali (3)` döndürür; `Hata` alanında erişim hatası bulunur. Servis yokmuş gibi devam edilmez.

Çağrı başlangıcı ve sonucu mevcut Serilog günlük dosyalarına (`Logs/log-*.txt`) ortak sorgu kimliğiyle yazılır. Başlangıç kaydı çağıran kullanıcı kimliğini, sorgulanan TCKN'yi, başvuru ve bölüm bilgisini, UTC zamanı içerir. Kayıt işlemi reddedilse bile loglar korunur. Yanıt gövdesi loglanmaz. Çağrılar başvuru yetkisi kontrol edildikten sonra, veritabanı kayıt işleminden önce yapılır.
