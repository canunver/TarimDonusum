# VKN servisi

`appsettings.json` içindeki `VKNServis` boşsa/yoksa `ServisYok (1)` döner ve kayıt devam eder. Doluysa tam HTTP/HTTPS uç nokta adresi olarak kullanılır. Kontrol Firmalar ekranındaki yeni firma/düzenleme kaydı ile ön başvurudaki firma penceresinin kaydında, erişim kontrolünden sonra yapılır.

Gerçek servis henüz tanımlanmadığından hazırlanan JSON POST sözleşmesinde istek ve başarılı yanıt aynı alanları içerir:

```json
{"vkn":"1234567890","firmaAdi":"Örnek Tarım A.Ş."}
```

VKN ve firma adı eşleşirse `Dogru (2)` döner. Unvanda Türkçe büyük/küçük harf ve fazla boşluk farkları yok sayılır; noktalama veya şirket türü farklılıkları eşleşme sayılmaz. Yanıtta her iki alan da zorunludur.

Uyuşmazlıkta `Hatali (3)` ve uyuşmazlık mesajı döner. Geçersiz servis adresi, HTTP hatası (404/500 dahil), bağlantı hatası, 15 saniyelik zaman aşımı, bozuk veya eksik yanıtta `Hatali (3)` ve erişim hatası döner. Bu durumlarda kayıt engellenir.

Çağıran kullanıcı, sorgulanan VKN, firma kimliği, ekran, UTC zamanı ve çağrı sonucu mevcut `Logs/log-*.txt` günlüklerine yazılır. Sonuçlar sorgu kimliğiyle ilişkilendirilir ve başarısız firma kayıtlarında da korunur.

Gerçek servis geldiğinde sözleşme ve varsa yetkilendirme yöntemi `VKNServisi` içinde uyarlanmalıdır. Örnek VKN test amaçlıdır; bu belge gerçek servise bağlantı yapıldığını ifade etmez.
