# Yeni kullanıcı SMS doğrulaması

`appsettings.json` içindeki `SMSServis` boşsa/yoksa yeni kullanıcı kaydında SMS doğrulaması aranmaz ve ilgili doğrulama kontrolleri gizlenir. Telefon alanı ve mevcut e-posta doğrulaması korunur. `SMSServis` doluysa SMS doğrulaması hem kayıt ekranında hem sunucuda zorunludur; eski `Sms:ServisVar` ayarı kullanılmaz.

Gerçek sağlayıcı henüz belirlenmediği için hazırlanan JSON POST sözleşmesi:

```json
{"telefon":"5551234567","mesaj":"TKDK Başvuru Portalı doğrulama kodunuz: 123456. Kod 3 dakika geçerlidir."}
```

Servis tam HTTP/HTTPS uç noktasına gönderilir. Başarılı HTTP yanıtında gönderimin kabul edildiğini bildiren şu gövde beklenir:

```json
{"basarili":true}
```

Sağlayıcının telefon formatı, yanıt biçimi ve kimlik doğrulama yöntemi belli olduğunda `SMSServisi` uyarlanmalıdır. Kod uygulamada kriptografik rastgele üreticiyle üretilir; sağlayıcı yalnızca SMS gönderir. Kod istemciye ya da loglara döndürülmez.

- Kod yalnızca başarılı gönderim onayından sonra oturuma kaydedilir, 3 dakika geçerlidir ve gönderildiği telefon numarasına bağlıdır.
- Oturumda yeniden gönderim için 60 saniye beklenir; kod başına en fazla 5 hatalı doğrulama denemesi yapılabilir.
- Başarıda kod silinir, doğrulama bilgisi kullanıcı kaydı tamamlanana/oturum bitene kadar saklanır. Kayıt sırasında doğrulanan telefon yeniden karşılaştırılır.
- Servis adresi doluyken zaman aşımı (15 saniye), HTTP hatası, olumsuz/eksik/bozuk yanıt doğrulamayı devre dışı bırakmaz; gönderim hatası gösterilir ve kayıt engellenir.
- Sabit `111111` test kodu kaldırılmıştır; eski test doğrulama durumu gerçek SMS gönderim kanıtı olmadan kabul edilmez.

Gerçek SMS gönderimi henüz denenmemiştir; gönderim sözleşmesi ve kayıt kontrolleri sahte HTTP servisiyle test edilmiştir.
