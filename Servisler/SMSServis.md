# Yeni kullanıcı SMS doğrulaması

`appsettings.json` içindeki `SMSServis` boşsa/yoksa telefon doğrulaması test modunda çalışır. Alanlar görünür kalır; kullanıcı önce kod ister, ardından `111111` koduyla telefonunu doğrular. Gerçek SMS gönderilmez ve ekranda test modu açıklaması gösterilir. `SMSServis` doluysa gerçek SMS koduyla doğrulama zorunludur. Her iki modda da doğrulama tamamlanmadan kullanıcı kaydı yapılmaz; mevcut e-posta doğrulaması korunur. Eski `Sms:ServisVar` ayarı kullanılmaz.

Gerçek sağlayıcı henüz belirlenmediği için hazırlanan JSON POST sözleşmesi:

```json
{"telefon":"5551234567","mesaj":"TKDK Başvuru Portalı doğrulama kodunuz: 123456. Kod 3 dakika geçerlidir."}
```

Servis tam HTTP/HTTPS uç noktasına gönderilir. Başarılı HTTP yanıtında gönderimin kabul edildiğini bildiren şu gövde beklenir:

```json
{"basarili":true}
```

Sağlayıcının telefon formatı, yanıt biçimi ve kimlik doğrulama yöntemi belli olduğunda `SMSServisi` uyarlanmalıdır. Kod uygulamada kriptografik rastgele üreticiyle üretilir; sağlayıcı yalnızca SMS gönderir. Kod istemciye ya da loglara döndürülmez.

- Gerçek modda kod yalnızca başarılı gönderim onayından sonra oturuma kaydedilir; test modunda gönderim yapılmadan `111111` oluşturulur. Kod 3 dakika geçerlidir ve gönderildiği telefon numarasına bağlıdır.
- Oturumda yeniden gönderim için 60 saniye beklenir; kod başına en fazla 5 hatalı doğrulama denemesi yapılabilir.
- Başarıda kod silinir, doğrulama bilgisi kullanıcı kaydı tamamlanana/oturum bitene kadar saklanır. Kayıt sırasında doğrulanan telefon yeniden karşılaştırılır.
- Servis adresi doluyken zaman aşımı (15 saniye), HTTP hatası, olumsuz/eksik/bozuk yanıt doğrulamayı devre dışı bırakmaz; gönderim hatası gösterilir ve kayıt engellenir.
- `111111` yalnızca test modunda geçerlidir. Test ve gerçek SMS doğrulama durumları ayrı tutulur; servis etkinleştirildiğinde test modunda alınan kod veya tamamlanan doğrulama kabul edilmez. Gerçek kod üretimi `111111` değerini dışlar.

Gerçek SMS gönderimi henüz denenmemiştir; gönderim sözleşmesi ve kayıt kontrolleri sahte HTTP servisiyle test edilmiştir.
