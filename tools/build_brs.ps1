$ErrorActionPreference = 'Stop'

$outDir = Join-Path $PSScriptRoot '..\Artifacts'
New-Item -ItemType Directory -Path $outDir -Force | Out-Null
$outFile = Join-Path $outDir 'TarimDonusum_BRS_MevcutSistem_Analizi.rtf'

function RtfEscape([string]$text) {
    if ($null -eq $text) { return '' }
    $sb = [System.Text.StringBuilder]::new()
    foreach ($ch in $text.ToCharArray()) {
        $n = [int][char]$ch
        if ($ch -eq '\') { [void]$sb.Append('\\') }
        elseif ($ch -eq '{') { [void]$sb.Append('\{') }
        elseif ($ch -eq '}') { [void]$sb.Append('\}') }
        elseif ($n -gt 127) {
            $signed = if ($n -gt 32767) { $n - 65536 } else { $n }
            [void]$sb.Append("\u${signed}?")
        } else { [void]$sb.Append($ch) }
    }
    return $sb.ToString()
}

$b = [System.Text.StringBuilder]::new()
function Add-Raw([string]$s) { [void]$script:b.Append($s) }
function Add-P([string]$text, [string]$style='body') {
    $t = RtfEscape $text
    switch ($style) {
        'title' { Add-Raw "\pard\qc\sb1440\sa240\f0\fs56\b\cf1 $t\b0\par`n" }
        'subtitle' { Add-Raw "\pard\qc\sa180\f0\fs28\cf2 $t\par`n" }
        'meta' { Add-Raw "\pard\qc\sa100\f0\fs20\cf3 $t\par`n" }
        'h1' { Add-Raw "\pard\keepn\sb320\sa160\f0\fs32\b\cf1 $t\b0\par`n" }
        'h2' { Add-Raw "\pard\keepn\sb240\sa120\f0\fs26\b\cf2 $t\b0\par`n" }
        'h3' { Add-Raw "\pard\keepn\sb160\sa80\f0\fs23\b\cf4 $t\b0\par`n" }
        'lead' { Add-Raw "\pard\li260\ri260\sb100\sa160\brdrs\brdrw12\brdrcf5\f0\fs22\b $t\b0\par`n" }
        'note' { Add-Raw "\pard\li260\ri260\sb80\sa120\cb6\f0\fs20 $t\par`n" }
        'small' { Add-Raw "\pard\sa80\f0\fs18\cf3 $t\par`n" }
        default { Add-Raw "\pard\qj\sa120\sl264\slmult1\f0\fs22\cf0 $t\par`n" }
    }
}
function Add-Bullet([string]$text) { Add-Raw "\pard\li720\fi-360\sa80\sl264\slmult1\f0\fs22 \bullet\tab $(RtfEscape $text)\par`n" }
function Add-Num([int]$n,[string]$text) { Add-Raw "\pard\li720\fi-360\sa80\sl264\slmult1\f0\fs22 $n.\tab $(RtfEscape $text)\par`n" }
function Add-Page { Add-Raw "\page`n" }
function Add-Rule { Add-Raw "\pard\sa120\brdrb\brdrs\brdrw12\brdrcf5\par`n" }
function Add-Table([string[]]$headers, [object[]]$rows, [int[]]$widths) {
    $positions = @(); $sum=0; foreach($w in $widths){$sum += $w; $positions += $sum}
    function Row([object[]]$cells,[bool]$header) {
        Add-Raw '\trowd\trgaph120\trleft120'
        foreach($p in $positions){ Add-Raw "\cellx$p" }
        for($i=0;$i -lt $cells.Count;$i++){
            $shade = if($header){'\clcbpat7'}else{''}
            Add-Raw "${shade}\pard\intbl\sa60\f0\fs19"
            if($header){Add-Raw '\b'}
            Add-Raw (' '+(RtfEscape ([string]$cells[$i])))
            if($header){Add-Raw '\b0'}
            Add-Raw '\cell '
        }
        Add-Raw '\row' + "`n"
    }
    Row $headers $true
    foreach($r in $rows){ Row ([object[]]$r) $false }
    Add-Raw "\pard\sa120\par`n"
}

Add-Raw '{\rtf1\ansi\ansicpg1254\deff0\uc1'
Add-Raw '{\fonttbl{\f0 Calibri;}{\f1 Consolas;}}'
Add-Raw '{\colortbl;\red31\green78\blue121;\red46\green116\blue181;\red90\green99\blue108;\red11\green37\blue69;\red180\green195\blue210;\red244\green246\blue249;\red232\green238\blue245;}'
Add-Raw '\paperw12240\paperh15840\margl1440\margr1440\margt1440\margb1440\headery708\footery708'
Add-Raw '{\header\pard\qr\f0\fs17\cf3 TARIM DONUSUM | BRS - Mevcut Sistem Analizi\par}'
Add-Raw '{\footer\pard\qc\f0\fs17\cf3 Sayfa {\field{\*\fldinst PAGE}{\fldrslt 1}}\par}'

Add-P 'TARIM DÖNÜŞÜM' title
Add-P 'İş Gereksinimleri Dokümanı (BRS)' subtitle
Add-P 'Mevcut Sistem Analizi: İş Akışları, İş Kuralları ve Kavramsal Veri Modeli' subtitle
Add-Rule
Add-P 'Doküman sürümü: 1.0' meta
Add-P 'Analiz tarihi: 2 Ağustos 2026' meta
Add-P 'Kaynak: Uygulama kaynak kodu, ekran tanımları ve iş kuralı katmanı' meta
Add-P 'Durum: İş birimi doğrulamasına sunulacak taslak' meta
Add-P 'Bu doküman, sistemde uygulanmış davranışların iş diliyle ifadesidir. Mevzuat veya kurum politikası olarak yorumlanmamalıdır.' note

Add-Page
Add-P 'Doküman Kontrolü' h1
Add-Table @('Alan','Değer') @(
    @('Doküman adı','Tarım Dönüşüm - İş Gereksinimleri Dokümanı'),
    @('Belge türü','BRS / mevcut sistem analizi'),
    @('Sürüm','1.0'),
    @('Hazırlama yöntemi','Kaynak koddan tersine mühendislik ve iş kuralı analizi'),
    @('Hedef okuyucu','İş birimi, ürün sahibi, analiz, yazılım, test ve denetim ekipleri'),
    @('Onay durumu','Onay bekliyor')
) @(2200,7160)
Add-P 'Değişiklik Geçmişi' h2
Add-Table @('Sürüm','Tarih','Açıklama') @(@('1.0','02.08.2026','İlk BRS taslağı; iş akışları, kurallar ve kavramsal veri modeli eklendi.')) @(1200,1800,6360)
Add-P 'Okuma Anahtarı' h2
Add-Bullet 'Uygulanmış: Kaynak kodda açık biçimde gözlemlenen davranış.'
Add-Bullet 'Çıkarım: Birden fazla kod parçasının birlikte değerlendirilmesiyle ulaşılan iş anlamı.'
Add-Bullet 'Doğrulanmalı: İş birimi kararı veya mevzuat kaynağı koddan kesinleşmeyen konu.'

Add-Page
Add-P 'İçindekiler' h1
$toc = @('1. Yönetici Özeti','2. Amaç, Kapsam ve Yaklaşım','3. Sistem Bağlamı ve Paydaşlar','4. Roller ve Yetkilendirme','5. Uçtan Uca İş Akışları','6. Başvuru Bölümleri ve İş Gereksinimleri','7. Denetleme ve Karar Süreci','8. Tanım Yönetimi','9. İş Kuralları Kataloğu','10. Kavramsal Veri Modeli','11. Kavramsal Veri Sözlüğü','12. Durum Modeli','13. Dosya, Kayıt İzi ve Bildirimler','14. Fonksiyonel Olmayan Gereksinimler','15. İstisnalar ve Riskler','16. Doğrulama Soruları','17. İzlenebilirlik Matrisi')
foreach($x in $toc){Add-P $x 'body'}

Add-Page
Add-P '1. Yönetici Özeti' h1
Add-P 'Tarım Dönüşüm uygulaması; başvuru sahiplerinin firma ve yatırım bilgilerini adım adım topladığı, belgeleri yönettiği, ön başvuruyu incelemeye gönderdiği ve yetkili kullanıcıların denetim sonucunu kaydettiği rol tabanlı bir web sistemidir.' lead
Add-P 'Sistemin merkezindeki iş nesnesi Başvurudur. Başvuru; firma, dönem, il, yatırım, uygulama adresi, ortaklık, mali ve finansal bilgiler, uygun harcamalar, teknik proje, çevresel-sosyal anket, belgeler ve taahhüt beyanlarını bir araya getirir. Başvuru incelemeye gönderildiğinde düzenleme kısıtlanır; denetim sonucuna göre düzeltmeye dönebilir, kabul edilebilir veya reddedilebilir.'
Add-P 'Kod tabanı üç temel rol tanımlar: Sistem Yöneticisi, Başvuru Kullanıcısı ve Birim Kullanıcısı. Yönetim tanımları ağırlıklı olarak Sistem Yöneticisine; başvuru oluşturma ve düzenleme Başvuru Kullanıcısına; gösterge paneli ise Sistem Yöneticisi ve yetkili Birim Kullanıcısına yöneliktir.'
Add-P 'Kavramsal veri modeli bu BRS içinde, iş kavramlarını ve ilişkilerini teknik tablo ayrıntısına girmeden açıklamak için verilmiştir. Mantıksal ve fiziksel veri modeli ayrı teknik tasarım çıktıları olarak ele alınmalıdır.' note

Add-Page
Add-P '2. Amaç, Kapsam ve Yaklaşım' h1
Add-P '2.1 Amaç' h2
Add-P 'Bu dokümanın amacı, mevcut yazılım davranışını ortak bir iş diliyle tanımlamak; süreç, rol, kural ve veri kavramlarını paydaş doğrulamasına açmak; geliştirme ve kabul testleri için izlenebilir bir temel oluşturmaktır.'
Add-P '2.2 Kapsam Dahili' h2
foreach($x in @('Kullanıcı kaydı, giriş, parola oluşturma ve parola yenileme','Firma ve başvuran ilişkisinin yönetimi','Dönem, il/ilçe, birim ve değer zinciri tanımları','Ön başvurunun bölümler halinde hazırlanması ve kaydedilmesi','Dosya yükleme ve indirme','İncelemeye gönderme, denetim listeleri ve karar','Gösterge paneli için rol/birim bazlı erişim','İşlem ve durum değişikliği kayıtları')){Add-Bullet $x}
Add-P '2.3 Kapsam Dışı veya Koddan Kesinleşmeyen' h2
foreach($x in @('Dış kurum entegrasyonları ve servis seviye taahhütleri','Resmî mevzuat dayanakları ve destek programı limitlerinin kaynağı','Elektronik imza, KEP veya resmî evrak yönetimi','Ödeme, sözleşme ve yatırım sonrası izleme süreçleri','Felaket kurtarma ve arşiv saklama süreleri')){Add-Bullet $x}
Add-P '2.4 Analiz Yaklaşımı' h2
Add-P 'Controller, iş kuralları, model, tablo erişim sınıfları, Razor ekranları, kaynak metinleri ve menü tanımları birlikte incelenmiştir. Kaynak kodda iki kez görünen veya tarihsel kalıntı izlenimi veren davranışlar kesin gereksinim olarak değil, doğrulama konusu olarak işaretlenmiştir.'

Add-Page
Add-P '3. Sistem Bağlamı ve Paydaşlar' h1
Add-P '3.1 İş Bağlamı' h2
Add-P 'Sistem, belirli bir başvuru döneminde tarımsal değer zincirlerine yönelik yatırım taleplerinin alınması ve ön değerlendirilmesi için kullanılmaktadır. Başvurular il ve dönem bağlamında açılır; değer zinciri seçenekleri il kısıtlarına göre sunulabilir.'
Add-P '3.2 Paydaşlar' h2
Add-Table @('Paydaş','Temel beklenti','Sistem teması') @(
 @('Başvuru sahibi / firma','Başvuruyu doğru ve tamamlanabilir biçimde hazırlamak','Firma, yatırım, mali, belge ve beyan ekranları'),
 @('Başvuru kullanıcısı','Yetkili olduğu firmalar adına kayıt yürütmek','Yeni ön başvuru, taslak düzenleme, gönderim'),
 @('Denetçi / uzman','Başvuruyu salt okunur incelemek ve sonuçlandırmak','Denetleme listesi, uzman sonuçları, karar'),
 @('Birim kullanıcısı','Yetkili birim kapsamındaki görünümü izlemek','Gösterge paneli'),
 @('Sistem yöneticisi','Kullanıcı, birim ve referans verileri yönetmek','Tanımlar, kullanıcılar, denetleme'),
 @('Bilgi işlem / destek','Süreklilik, güvenlik ve hata çözümü sağlamak','Loglar, yapılandırma, veri erişimi')
) @(1900,3700,3760)
Add-P '3.3 Sistem Sınırı' h2
Add-P 'Uygulama; kimlik ve parola işlemleri, iş verisi girişi, doğrulama, veri saklama, dosya yönetimi, e-posta bağlantısı üretimi ve rol kontrolünü kendi uygulama katmanında yürütür. Harici kimlik, vergi, MERSİS veya coğrafi servis entegrasyonu kodda doğrulanmamıştır.'

Add-Page
Add-P '4. Roller ve Yetkilendirme' h1
Add-Table @('Rol','Ana yetkiler','Kısıtlar') @(
 @('Sistem Yöneticisi','Kullanıcı, birim, il, dönem ve değer zinciri tanımları; denetleme; tüm kayıt görünümü','Başvuru sahibi adına veri sahipliği iş kuralı ayrıca doğrulanmalı'),
 @('Başvuru Kullanıcısı','Firma erişimi, yeni ön başvuru, bölümleri kaydetme, dosya yükleme, incelemeye gönderme','Denetleme ekranına giremez; yalnız ilişkili başvuruları görmelidir'),
 @('Birim Kullanıcısı','Yetkili olduğu birimler için gösterge paneli','Tanım yönetimi ve başvuru düzenleme yetkisi yoktur')
) @(1900,4200,3260)
Add-P '4.1 Yetki İlkeleri' h2
Add-Bullet 'Kimliği doğrulanmamış kullanıcı korumalı işlemlere erişememelidir.'
Add-Bullet 'Başvuru kullanıcısı yalnız ilişkilendirildiği firma ve başvuruları görüntüleyip değiştirebilmelidir.'
Add-Bullet 'Birim kullanıcısının görünümü, yetki kaydındaki birimlerle sınırlandırılmalıdır.'
Add-Bullet 'Sistem yöneticisi gerektiren tanım işlemleri sunucu tarafında ayrıca doğrulanmalıdır.'
Add-Bullet 'Dosya görüntüleme ve yükleme yetkisi başvuru anahtarı ve form türü üzerinden kontrol edilmelidir.'
Add-P 'Doğrulanmalı: Denetçi için ayrı bir rol bulunmamaktadır. Mevcut uygulamada Başvuru Kullanıcısı olmayan kullanıcıların denetleme ekranına erişebilmesi, Sistem Yöneticisi ile Birim Kullanıcısı arasında beklenenden geniş bir yetkiye yol açabilir.' note

Add-Page
Add-P '5. Uçtan Uca İş Akışları' h1
Add-P '5.1 Kullanıcı Edinimi ve Giriş' h2
foreach($x in @('Kullanıcı yeni kullanıcı formunu doldurur.','Sistem kimlik, iletişim ve mükerrerlik kontrollerini uygular.','Başvuru kullanıcısı rolü oluşturulur.','Parola bağlantısı e-posta üzerinden üretilir veya kullanıcı parola belirleme ekranına yönlendirilir.','Kullanıcı giriş yapar; rol ve yetkiler oturuma yüklenir.')){Add-Num ([array]::IndexOf(@('Kullanıcı yeni kullanıcı formunu doldurur.','Sistem kimlik, iletişim ve mükerrerlik kontrollerini uygular.','Başvuru kullanıcısı rolü oluşturulur.','Parola bağlantısı e-posta üzerinden üretilir veya kullanıcı parola belirleme ekranına yönlendirilir.','Kullanıcı giriş yapar; rol ve yetkiler oturuma yüklenir.'),$x)+1) $x}
Add-P '5.2 Ön Başvuru Hazırlama' h2
Add-P 'Akış: Firma seçimi/oluşturma → dönem ve il seçimi → başvuru sahibi bilgileri → mali ve ortaklık bilgileri → uygulama adresi → yatırım → uygun harcama → finans → yatırım özeti → teknik proje → belgeler → çevresel-sosyal bilgiler → taahhüt ve beyan → özet.' lead
Add-P 'Her bölüm bağımsız kaydedilebilir. İşlem sırasında başvuru kimliği üretilir ve sonraki alt kayıtlar bu kimliğe bağlanır. Başvuru, Ön Başvuru durumundayken değiştirilebilir.'
Add-P '5.3 İncelemeye Gönderme' h2
Add-Num 1 'Başvuru kullanıcısı özet ekranından incelemeye gönderme işlemini başlatır.'
Add-Num 2 'Sistem sahiplik, durum ve bütünlük kontrollerini yapar.'
Add-Num 3 'Başvuru durumu Ön Başvurudan Başvuruya çevrilir.'
Add-Num 4 'Durum değişikliği başvuru işlem kaydına yazılır.'
Add-Num 5 'Başvuru kullanıcı düzenlemesine kapatılır ve denetleme kuyruğunda görünür.'

Add-Page
Add-P '5.4 Denetim ve Sonuçlandırma' h2
Add-Num 1 'Yetkili kullanıcı başvuru sürümleri listesinden kaydı açar.'
Add-Num 2 'Başvuru salt okunur biçimde, denetçi bölümleriyle birlikte görüntülenir.'
Add-Num 3 'Sistem sonuçları ve uzman kontrol listeleri kaydedilebilir.'
Add-Num 4 'Denetçi gerekçe ve sonucu seçer; taslak olarak kaydedebilir.'
Add-Num 5 'Sonuçlandırma talebinde sonuç zorunlu hale gelir.'
Add-Num 6 'Düzeltme için iade edilirse başvuru Ön Başvuru durumuna döner; kabul/ret seçilirse nihai duruma geçer.'
Add-P '5.5 Düzeltme Döngüsü' h2
Add-P 'Düzeltmeye iade edilen kayıt yeniden başvuru kullanıcısının düzenlemesine açılır. Kodda revizyon numarası ve başvuru ana kimliği kavramları bulunması, sürüm takibi yapıldığını göstermektedir. Revizyon üretme anı ve önceki sürümlerin değiştirilemezliği iş birimiyle doğrulanmalıdır.'
Add-P '5.6 Alternatif ve Hata Akışları' h2
foreach($x in @('Oturum süresi dolmuşsa işlem reddedilir.','Başvuru kullanıcıya ait değilse erişim veya değişiklik reddedilir.','Başvuru uygun durumda değilse gönderim ya da düzenleme yapılmaz.','Zorunlu alanlar, dosyalar veya beyanlar eksikse bölüm kaydı/gönderim başarısız olur.','Mükerrer firma, kullanıcı veya tanım kayıtları veri bütünlüğü hatasıyla reddedilir.','Dosya türü, boyutu veya anahtarı uygun değilse yükleme reddedilir.')){Add-Bullet $x}

Add-Page
Add-P '6. Başvuru Bölümleri ve İş Gereksinimleri' h1
Add-Table @('Sıra','Bölüm','İş amacı') @(
 @('10','Firma / Başvuru Sahibi','Dönem, il, firma, başvuru konusu ve hukuki niteliği belirlemek'),
 @('20','Mali','Ciro, bilanço, çalışan ve KOBİ değerlendirme verilerini toplamak'),
 @('30','Ortaklık','Ortaklar, paylar, bağlı/ortak işletme ve UBO/KYC verilerini toplamak'),
 @('40','Uygulama Adresi','Yatırım yerlerini, mülkiyet ve ruhsat durumunu kaydetmek'),
 @('50','Yatırım','Yatırım adı, türü, amacı, değer zinciri aşamaları ve harcama türleri'),
 @('60','Uygun Harcama','Uygun harcama ön listesini toplamak'),
 @('70','Finans','Yatırım, destek, katkı, oran ve vade bilgilerini toplamak'),
 @('80','Yatırım Özeti','Projenin gerekçe, çıktı, kapasite ve etki özetini almak'),
 @('90','DB/CTP Teknik Proje','Teknik proje anket ve içeriklerini almak'),
 @('100','Belgeler','Belge paketi ve zorunlu belge gruplarını yönetmek'),
 @('110','Çevresel-Sosyal','Çevresel ve sosyal uygunluk anketini almak'),
 @('120','Taahhüt/Beyan','Beyan seçimleri ve imzalı taahhüt dosyasını almak'),
 @('130','Özet','Başvuruyu bütünsel gözden geçirmek ve göndermek'),
 @('140-160','Denetçi bölümleri','Sistem sonucu, uzman sonucu ve kararı kaydetmek')
) @(900,2300,6160)

Add-Page
Add-P '6.1 Firma ve Başvuru Sahibi' h2
Add-Bullet 'Dönem, il ve firma seçimi zorunludur.'
Add-Bullet 'Son iki yıldır faal olma durumu seçilmelidir.'
Add-Bullet 'Başvuru sahibi türü ile hukuki tür/şirket türü tanımsız bırakılamaz.'
Add-Bullet 'Firma vergi kimlik numarasıyla sorgulanabilir; kullanıcı-firma ilişkisi korunmalıdır.'
Add-P '6.2 Ortaklık ve Mali Bilgiler' h2
Add-Bullet 'Ortak paylarının toplamı yüzde 100ü geçemez.'
Add-Bullet 'Aynı TCKN/VKN ile birden fazla ortak kaydedilemez.'
Add-Bullet 'Tüzel kişi ortak için hesaba dahil oran pozitif olmalıdır.'
Add-Bullet 'Bağlı/ortak işletme varsa unvan, kimlik ve iki yıla ilişkin finansal değerler istenir.'
Add-Bullet 'Yönetim kurulu veya ilgili kişiler için adli sicil kişi ve belge kayıtları tutulabilir.'
Add-P '6.3 Uygulama Adresi ve Yatırım' h2
Add-Bullet 'En az bir uygulama adresi ihtiyacı iş birimiyle doğrulanmalıdır; adres kayıtları sıra numarasıyla tutulur.'
Add-Bullet 'Yatırım yeri statüsü ve yapı ruhsatı durumu kontrollü değerlerden seçilir.'
Add-Bullet 'Yatırım adı, yatırım türü, amacı/faaliyetleri/çıktıları zorunludur.'
Add-Bullet 'En az bir değer zinciri aşaması ve en az bir harcama türü seçilmelidir.'
Add-Bullet 'Değer zinciri aşamasındaki faaliyet açıklaması 500 karakteri geçemez.'

Add-Page
Add-P '6.4 Finans ve Uygun Harcama' h2
Add-Bullet 'Toplam yatırım tutarı sıfırdan büyük olmalıdır.'
Add-Bullet 'Talep edilen finansman oranı ve vade süresi sıfırdan büyük olmalıdır.'
Add-Bullet 'Ön başvuru sahibi ve başvuru sahibi katkıları negatif olamaz.'
Add-Bullet 'Uygun harcama ön listesi JSON biçiminde saklanır ve uygulama düzeyinde uzunluk kontrolüne tabidir.'
Add-P '6.5 Belgeler' h2
Add-Bullet 'Belge paketi, taahhüt dosyası, denetim dosyası ve bölüm bazlı dosyalar ayrı iş anahtarlarıyla yönetilir.'
Add-Bullet 'Zorunlu belge grupları kaynak tanımından okunur; ekranda sunulan ve kullanıcı tarafından onaylanan grupların bütünlüğü kontrol edilir.'
Add-Bullet 'Ortaklık belgeleri, UBO/KYC belgeleri ve adli sicil belgeleri ilgili kişi/ortak bağlamında tutulur.'
Add-Bullet 'Dosya indirme, başvuru modülü ve geçerli form adı/anahtar eşleşmesi üzerinden yetkilendirilir.'
Add-P '6.6 Taahhüt, Beyan ve Anketler' h2
Add-P 'Taahhüt beyanları, çevresel-sosyal anket ve teknik proje verileri JSON tabanlı alanlarda tutulmaktadır. Bu yapı form esnekliği sağlar; ancak veri sözlüğü, sürümleme ve raporlanabilirlik için şema yönetimi gerektirir.'

Add-Page
Add-P '7. Denetleme ve Karar Süreci' h1
Add-P '7.1 Denetleme Kapsamı' h2
Add-P 'Denetleme ekranı tüm başvuru sürümlerini listeler ve seçilen sürümü salt okunur başvuru bölümleri ile denetçiye özel Sistem Sonuçları, Uzman Sonuçları ve Karar bölümleri üzerinden gösterir.'
Add-P '7.2 Denetim Sonuçları' h2
Add-Table @('Sonuç','Yeni başvuru durumu','İş etkisi') @(
 @('Düzeltme için iade','Ön Başvuru','Başvuru yeniden düzenlenebilir ve tekrar gönderilebilir.'),
 @('Kabul edildi','Kabul Edildi','Ön değerlendirme olumlu sonuçlanır; kayıt nihai görünür.'),
 @('Reddedildi','Reddedildi','Ön değerlendirme olumsuz sonuçlanır; kayıt nihai görünür.'),
 @('Taslak kayıt','Değişmez','Denetim verisi saklanır ancak süreç sonuçlandırılmaz.')
) @(2200,2200,4960)
Add-P '7.3 Karar Kuralları' h2
Add-Bullet 'Sonuçlandırma sırasında denetim sonucu Tanımsız olamaz.'
Add-Bullet 'İncelenen başvuru Başvuru durumunda olmalıdır.'
Add-Bullet 'Düzeltme iadesi Başvuru → Ön Başvuru geçişi yaratır.'
Add-Bullet 'Kabul ve ret Başvuru durumundan ilgili nihai duruma geçer.'
Add-Bullet 'Durum değişiklikleri işlem kaydında eski ve yeni durumla izlenmelidir.'
Add-P 'Doğrulanmalı: Nihai kabul/ret kararının geri alınması, ikinci göz onayı, görev ayrılığı ve karar yetkisinin birim bazında sınırlandırılması kodda açık değildir.' note

Add-Page
Add-P '8. Tanım Yönetimi' h1
Add-Table @('Tanım','İşlev','Temel kural') @(
 @('Dönem','Başvuruların çağrı/zaman bağlamı','Ad zorunlu; yıl 2000-2200; başlangıç bitişten sonra olamaz.'),
 @('İl / İlçe','Başvuru ve yatırım coğrafyası','Kod/ad mükerrerliği engellenir; ilçe ile il ilişkisi korunur.'),
 @('Birim','Merkez/taşra organizasyon yapısı','Ad ve sıra zorunlu; taşra biriminde geçerli il kodu gerekir.'),
 @('Değer Zinciri','Yatırım faaliyet sınıflaması','Ad zorunlu; aşama sıra numaraları pozitif ve benzersizdir.'),
 @('Değer Zinciri Aşaması','Zincirde yapılacak faaliyet basamağı','Ad ve sıra gereklidir; başvuru seçimiyle ilişkilidir.'),
 @('Değer Zinciri-İl','Zincirin geçerli olduğu coğrafya','Aynı il aynı zincire iki kez eklenemez.'),
 @('Kullanıcı','Kimlik, iletişim ve erişim sahibi','Rol ve rol bağlamı doğrulanır; mükerrerlik engellenir.'),
 @('Firma','Başvuru sahibi tüzel/işletme kaydı','Vergi kimliği ve kullanıcı-firma ilişkisi korunur.')
) @(2100,3200,4060)
Add-P '8.1 Yaşam Döngüsü' h2
Add-P 'Birimler pasife alınabilir. Diğer tanımlar için silme/pasife alma ve tarihsel başvurulara etkisi ayrı ayrı doğrulanmalıdır. Referans verilerin geçmiş başvurulardaki anlamı değiştirmemesi için tarihsel kayıtların ad/kod anlık görüntüsü veya sürümlü tanım yaklaşımı değerlendirilmelidir.'

Add-Page
Add-P '9. İş Kuralları Kataloğu' h1
Add-Table @('Kod','Kural','Kaynak durumu') @(
 @('BR-001','Başvuru yalnız Başvuru Kullanıcısı rolü ve firma ilişkisiyle oluşturulabilmelidir.','Uygulanmış'),
 @('BR-002','Başvuru dönem, il ve firma olmadan başlatılamaz.','Uygulanmış'),
 @('BR-003','Başvuru yalnız Ön Başvuru durumundayken düzenlenebilmelidir.','Uygulanmış'),
 @('BR-004','İncelemeye gönderme Ön Başvuru → Başvuru durum geçişi yaratmalıdır.','Uygulanmış'),
 @('BR-005','Denetim sonucu düzeltme ise durum Ön Başvuruya dönmelidir.','Uygulanmış'),
 @('BR-006','Kabul veya ret sonucu ilgili nihai durumu oluşturmalıdır.','Uygulanmış'),
 @('BR-007','Ortak pay toplamı yüzde 100ü aşmamalıdır.','Uygulanmış'),
 @('BR-008','Ortak TCKN/VKN değeri başvuru içinde benzersiz olmalıdır.','Uygulanmış'),
 @('BR-009','Yatırım için en az bir değer zinciri aşaması ve harcama türü seçilmelidir.','Uygulanmış'),
 @('BR-010','Başvuruya bağlı dosya yalnız yetkili form anahtarıyla erişilebilir olmalıdır.','Uygulanmış'),
 @('BR-011','Taşra birimi geçerli bir il koduna bağlı olmalıdır.','Uygulanmış'),
 @('BR-012','Dönem başlangıç tarihi bitiş tarihinden sonra olamaz.','Uygulanmış')
) @(1100,6300,1960)

Add-Page
Add-P '9. İş Kuralları Kataloğu (devam)' h1
Add-Table @('Kod','Kural','Kaynak durumu') @(
 @('BR-013','Değer zinciri aşama sıra numaraları pozitif ve zincir içinde benzersiz olmalıdır.','Uygulanmış'),
 @('BR-014','Başvuru kullanıcısı denetleme işlevlerine erişememelidir.','Uygulanmış'),
 @('BR-015','Birim kullanıcısı gösterge panelinde yalnız yetkili birimlerini görmelidir.','Uygulanmış'),
 @('BR-016','Sonuçlandırma sırasında geçerli denetim sonucu zorunlu olmalıdır.','Uygulanmış'),
 @('BR-017','Başvuru durum değişiklikleri kullanıcı, zaman, eski durum ve yeni durumla loglanmalıdır.','Çıkarım'),
 @('BR-018','Nihai kararlar yetkili rol ve birim kapsamıyla sınırlandırılmalıdır.','Doğrulanmalı'),
 @('BR-019','Dönem dışında yeni başvuru veya gönderim yapılamamalıdır.','Doğrulanmalı'),
 @('BR-020','Başvuruya ait JSON anket şemaları sürümlenmeli ve geriye dönük okunabilmelidir.','Önerilen'),
 @('BR-021','Dosya türü, boyutu, zararlı içerik kontrolü ve saklama süresi politikayla belirlenmelidir.','Doğrulanmalı'),
 @('BR-022','Nihai kabul/ret için gerekçe zorunluluğu ve asgari içerik kuralı tanımlanmalıdır.','Doğrulanmalı')
) @(1100,6300,1960)

Add-Page
Add-P '10. Kavramsal Veri Modeli' h1
Add-P 'Model, iş kavramlarını gösterir; fiziksel tablo veya kolon tasarımı değildir.' lead
Add-P '10.1 Ana İlişkiler' h2
Add-P 'KULLANICI → yetkilere sahiptir → KULLANICI YETKİSİ → isteğe bağlı bağlanır → BİRİM' lead
Add-P 'KULLANICI ↔ ilişkilendirilir ↔ FİRMA → bir veya daha çok → BAŞVURU' lead
Add-P 'BAŞVURU → ait olur → DÖNEM ve İL; → içerir → YATIRIM, ADRES, ORTAKLIK, MALİ, FİNANS, HARCAMA, BELGE, ANKET, BEYAN ve DENETİM' lead
Add-P 'YATIRIM → seçer → DEĞER ZİNCİRİ → içerir → DEĞER ZİNCİRİ AŞAMASI; DEĞER ZİNCİRİ ↔ geçerlidir ↔ İL' lead
Add-P 'BAŞVURU → sürümlenir → BAŞVURU SÜRÜMÜ; → olay üretir → BAŞVURU LOGU; → sonuçlanır → DENETİM KARARI' lead
Add-P '10.2 Kardinalite Özeti' h2
Add-Table @('Kaynak','İlişki','Hedef') @(
 @('Kullanıcı','1 - N','Kullanıcı Yetkisi'),
 @('Kullanıcı','N - N','Firma (Firma-Kullanıcı ilişkisi üzerinden)'),
 @('Firma','1 - N','Başvuru'),
 @('Dönem','1 - N','Başvuru'),
 @('Başvuru Ana Kaydı','1 - N','Başvuru Sürümü'),
 @('Başvuru','1 - N','Uygulama Adresi / Ortak / Belge / Log'),
 @('Başvuru','1 - 1 veya 0..1','Yatırım / Mali / Finans / Anket / Denetim'),
 @('Değer Zinciri','1 - N','Değer Zinciri Aşaması'),
 @('Değer Zinciri','N - N','İl')
) @(2700,1700,4960)

Add-Page
Add-P '11. Kavramsal Veri Sözlüğü' h1
Add-Table @('Kavram','Tanım','Kimlik / yaşam döngüsü') @(
 @('Kullanıcı','Sistemde oturum açan ve işlem yapan kişi.','Kullanıcı kimliği; aktiflik ve parola yaşam döngüsü'),
 @('Kullanıcı Yetkisi','Kullanıcının rolünü ve varsa birim kapsamını belirler.','Kullanıcıya bağlı çoklu kayıt'),
 @('Firma','Başvuru sahibi işletme veya tüzel organizasyon.','Firma kimliği; vergi kimliğiyle aranabilir'),
 @('Başvuru','Bir firma, dönem ve il bağlamındaki yatırım talebi.','Başvuru kimliği; durum ve revizyonla yaşar'),
 @('Başvuru Ana Kaydı','Revizyonları aynı iş başvurusu altında toplar.','Ana kimlik; birden çok sürüm'),
 @('Dönem','Başvurunun ait olduğu çağrı/zaman aralığı.','Yıl, ad, başlangıç ve bitiş'),
 @('Yatırım','Başvurunun yatırım amacı, türü ve faaliyet kapsamı.','Başvuruya bağlı'),
 @('Uygulama Adresi','Yatırımın uygulanacağı yer ve hukuki/ruhsat durumu.','Başvuruya bağlı çoklu kayıt'),
 @('Ortak','Firma ortak/pay sahibi ve finansal/UBO bilgileri.','Başvuruya bağlı çoklu kayıt'),
 @('Dosya','Belge içeriği ve iş bağlamı metadata kaydı.','Modül, form, anahtar ve dosya no ile bağlanır')
) @(2100,4100,3260)

Add-Page
Add-P '11. Kavramsal Veri Sözlüğü (devam)' h1
Add-Table @('Kavram','Tanım','Kimlik / yaşam döngüsü') @(
 @('Mali Bilgi','Ciro, bilanço, çalışan ve işletme ölçeği göstergeleri.','Başvuruya bağlı'),
 @('Finans Bilgisi','Yatırım, destek, katkı, oran ve vade değerleri.','Başvuruya bağlı'),
 @('Uygun Harcama','Destek kapsamına sunulan harcama ön listesi.','Başvuruya bağlı JSON içerik'),
 @('Değer Zinciri','Yatırımın faaliyet alanını sınıflayan referans kavram.','Tanım kaydı; il ve aşamalarla ilişkili'),
 @('Değer Zinciri Aşaması','Zincirin sıralı faaliyet basamağı.','Zincire bağlı; başvuruda seçilir'),
 @('Çevresel-Sosyal Anket','Çevresel ve sosyal uygunluk cevapları.','Başvuruya bağlı JSON içerik'),
 @('Teknik Proje','DB/CTP teknik değerlendirme cevapları.','Başvuruya bağlı JSON içerik'),
 @('Denetim','Kontrol listeleri, gerekçe ve ön başvuru sonucu.','Başvuru sürümüne bağlı'),
 @('Başvuru Logu','İşlem ve durum değişikliği denetim izi.','Başvuru, kullanıcı ve zaman bağlamı'),
 @('Birim','Merkez veya taşra organizasyon birimi.','Tür, il kodu, sıra ve aktiflik')
) @(2100,4100,3260)
Add-P 'Veri sınıflandırması önerisi: TCKN/VKN, iletişim, adli sicil, UBO/KYC ve finansal veriler yüksek hassasiyetli iş verisi olarak sınıflandırılmalı; erişim, maskeleme, loglama ve saklama politikaları ayrıca tanımlanmalıdır.' note

Add-Page
Add-P '12. Durum Modeli' h1
Add-Table @('Durum','Anlam','Düzenlenebilirlik','Çıkışlar') @(
 @('Tanımsız','Geçerli iş durumu oluşmamış kayıt','Hayır','Sistemsel hata/başlatma kontrolü'),
 @('Ön Başvuru','Hazırlanan veya düzeltmeye iade edilen taslak','Başvuru kullanıcısı için evet','Başvuru, İptal (doğrulanmalı)'),
 @('Başvuru','İncelemeye gönderilmiş kayıt','Başvuru kullanıcısı için hayır','Ön Başvuru, Kabul, Ret'),
 @('Kabul Edildi','Olumlu nihai ön değerlendirme','Hayır','Geri alma süreci doğrulanmalı'),
 @('Reddedildi','Olumsuz nihai ön değerlendirme','Hayır','İtiraz/geri alma süreci doğrulanmalı'),
 @('İptal','İptal edilmiş kayıt','Hayır','İptal akışı kodda doğrulanmalı')
) @(1900,3000,2100,2360)
Add-P '12.1 Geçiş Matrisi' h2
Add-Table @('Başlangıç','Olay','Hedef','Aktör') @(
 @('Yeni','İlk kayıt','Ön Başvuru','Başvuru Kullanıcısı'),
 @('Ön Başvuru','İncelemeye gönder','Başvuru','Başvuru Kullanıcısı'),
 @('Başvuru','Düzeltmeye iade','Ön Başvuru','Denetçi/Yetkili'),
 @('Başvuru','Kabul et','Kabul Edildi','Denetçi/Yetkili'),
 @('Başvuru','Reddet','Reddedildi','Denetçi/Yetkili')
) @(1800,2900,2200,2460)

Add-Page
Add-P '13. Dosya, Kayıt İzi ve Bildirimler' h1
Add-P '13.1 Dosya Yönetimi' h2
Add-P 'Dosyalar modül kodu, form adı, form anahtarı ve dosya numarasıyla iş nesnesine bağlanır. Başvuru belge paketi, taahhüt, denetim, zorunlu belge, bağlı ortaklık, adli sicil ve UBO/KYC bağlamları ayrıştırılmıştır.'
Add-Bullet 'Dosya adı ve içerik saklanmalıdır.'
Add-Bullet 'Görüntüleme/yükleme yetkisi iş bağlamıyla doğrulanmalıdır.'
Add-Bullet 'Dosya değişiklikleri Dosya Logu ile izlenebilmelidir.'
Add-Bullet 'Silme yetkisi mevcut başvuru yetki denetiminde kapalı görünmektedir.'
Add-P '13.2 İşlem Kayıtları' h2
Add-P 'Kullanıcı, firma, başvuru ve dosya için ayrı log kavramları bulunmaktadır. Asgari kayıt; işlem türü, aktör, zaman, hedef kayıt, eski/yeni değer özeti, istemci bilgisi ve başarı durumunu içermelidir.'
Add-P '13.3 Bildirimler' h2
Add-P 'Mail servisi, parola belirleme ve unutulan parola bağlantılarında kullanılmaktadır. Başvuru gönderimi, düzeltme, kabul ve ret olaylarında e-posta bildirimi beklentisi koddan kesinleşmemiştir ve iş birimi tarafından kararlaştırılmalıdır.'

Add-Page
Add-P '14. Fonksiyonel Olmayan Gereksinimler' h1
Add-Table @('Alan','Gereksinim / kabul ölçütü') @(
 @('Güvenlik','Kimlik doğrulama, rol ve nesne sahipliği kontrolleri sunucu tarafında uygulanmalıdır.'),
 @('Gizlilik','TCKN/VKN, adli sicil, iletişim ve finans verileri yetki dışı kullanıcılara gösterilmemelidir.'),
 @('Bütünlük','Durum geçişleri ve çoklu alt kayıt güncellemeleri gerektiğinde transaction içinde yürütülmelidir.'),
 @('Denetlenebilirlik','Kritik kayıt, dosya, yetki ve karar değişiklikleri değiştirilemez işlem izi üretmelidir.'),
 @('Kullanılabilirlik','Uzun başvuru, bölümler halinde kaydedilebilmeli; hata mesajı alan ve çözüm odaklı olmalıdır.'),
 @('Yerelleştirme','Türkçe ve İngilizce kaynak metinleri tutarlı anahtarlarla desteklenmelidir.'),
 @('Performans','Liste ve dosya işlemleri için hedef süreler ve eşzamanlı kullanıcı kapasitesi belirlenmelidir.'),
 @('Erişilebilirlik','Formlar klavye ile kullanılabilir, etiketli ve anlamlı hata bildirimli olmalıdır.'),
 @('Yedekleme','RPO/RTO, dosya ve veritabanı yedekleme periyotları kurum politikasıyla belirlenmelidir.'),
 @('Uyumluluk','KVKK, saklama, açık rıza ve adli sicil verisi işleme dayanakları hukuk birimince doğrulanmalıdır.')
) @(2200,7160)

Add-Page
Add-P '15. İstisnalar, Riskler ve Teknik Borç Yansımaları' h1
Add-Table @('No','Bulgu / risk','İş etkisi','Öneri') @(
 @('R-01','Denetçi için ayrı rol yok','Yetki kapsamı genişleyebilir','Denetçi rolü ve birim kapsamı tanımlansın.'),
 @('R-02','Bazı anketler JSON alanlarında','Raporlama ve sürüm uyumu zorlaşır','Şema sürümü ve doğrulama kataloğu oluşturulsun.'),
 @('R-03','İptal durumu tanımlı, akış görünür değil','Kullanıcı beklentisi belirsiz','İptal aktörü, koşulu ve geri alma kuralı kararlaştırılsın.'),
 @('R-04','İş kuralı sınıfında yinelenen yöntem izleri','Davranış tutarsızlığı riski','Kod sadeleştirme ve regresyon testi yapılsın.'),
 @('R-05','Nihai karar geri alma/itirazı belirsiz','Operasyonel hata düzeltilemeyebilir','Kontrollü geri alma ve itiraz süreci tasarlansın.'),
 @('R-06','Dosya saklama/güvenlik politikası belirsiz','Gizlilik ve kapasite riski','Tür, boyut, virüs tarama, saklama ve imha kuralları belirlensin.'),
 @('R-07','Dönem tarihinin işlem engeline etkisi belirsiz','Süre dışı başvuru alınabilir','Sunucu taraflı dönem kontrolü eklenip test edilsin.')
) @(700,2700,2600,3360)

Add-Page
Add-P '16. İş Birimi Doğrulama Soruları' h1
$qs=@(
'Denetleme işlemini hangi rol veya roller yapmalıdır; birim/il kapsamı uygulanacak mıdır?',
'Başvuru dönemi başlamadan veya bittikten sonra taslak oluşturma, kaydetme ve gönderme davranışları nedir?',
'Düzeltmeye iade yeni bir revizyon mu oluşturur, yoksa aynı kayıt mı güncellenir?',
'Kabul ve ret kararları ikinci onay gerektirir mi; geri alınabilir mi?',
'İptal işlemini kim, hangi durumlarda ve hangi gerekçeyle yapabilir?',
'Başvuruda en az bir uygulama adresi zorunlu mudur?',
'Destek oranı, tutar, vade ve uygun harcama için dönem/değer zinciri bazlı limitler nelerdir?',
'Zorunlu belge listesi hangi resmî kaynaktan ve hangi sürümle yönetilecektir?',
'Adli sicil ve UBO/KYC belgelerinin saklama süresi, erişim kapsamı ve imha yöntemi nedir?',
'Başvuru gönderimi, düzeltme, kabul ve ret olaylarında kimlere hangi kanaldan bildirim yapılmalıdır?',
'Başvuru sahibi firma ile kullanıcı ilişkisi kim tarafından onaylanır veya kaldırılır?',
'Çevresel-sosyal ve teknik proje anketlerinin puanlama/karar etkisi nedir?',
'Gösterge panelinde hangi metrikler, filtreler ve dışa aktarımlar beklenmektedir?',
'İngilizce arayüz ve belgeler operasyonel gereksinim midir?',
'Kayıt ve işlem logları kaç yıl saklanmalı, kimler tarafından görüntülenebilmelidir?')
$i=1; foreach($q in $qs){Add-Num $i $q; $i++}

Add-Page
Add-P '17. İzlenebilirlik Matrisi' h1
Add-Table @('Gereksinim grubu','İlgili süreç','Başlıca yazılım alanı','Test odağı') @(
 @('Kimlik ve erişim','Kayıt/Giriş/Parola','Home, Kullanıcı iş kuralları','Mükerrerlik, token, rol, oturum'),
 @('Firma yönetimi','Firma seçme/oluşturma','Firma controller ve iş kuralları','Sahiplik, VKN, ilişki'),
 @('Başvuru hazırlama','Bölüm bazlı kayıt','Başvuru controller/modeller','Zorunlu alan, durum, sahiplik'),
 @('Dosya yönetimi','Yükleme/indirme','Dosya yönetimi iş kuralları','Anahtar, form, yetki, bütünlük'),
 @('Gönderim','İncelemeye gönder','Başvuru iş kuralları','Tamlık ve durum geçişi'),
 @('Denetim','Kontrol ve sonuç','Denetleme controller','Salt okunur görünüm, sonuç zorunluluğu'),
 @('Tanımlar','Dönem/il/birim/zincir','Tanım iş kuralları','Yetki, benzersizlik, tarih ve sıra'),
 @('Raporlama','Gösterge paneli','Dashboard','Birim filtresi ve veri doğruluğu'),
 @('Denetim izi','Kritik olay kayıtları','Log modelleri/tabloları','Aktör, zaman, eski-yeni değer')
) @(2100,2500,2760,2000)
Add-P '17.1 Önerilen Sonraki Analiz Çıktıları' h2
Add-Bullet 'İş birimi doğrulaması sonrası onaylı BRS 1.1.'
Add-Bullet 'Ekran ve API düzeyinde fonksiyonel gereksinim/SRS.'
Add-Bullet 'Mantıksal veri modeli ve alan bazlı veri sözlüğü.'
Add-Bullet 'Rol-yetki matrisi ve kişisel veri envanteri.'
Add-Bullet 'Uçtan uca kabul senaryoları ve gereksinim-test izlenebilirliği.'

Add-Page
Add-P 'Ek A - Başvuru Kontrollü Değerleri' h1
Add-Table @('Kavram','Değerler') @(
 @('Başvuru Durumu','Tanımsız; Ön Başvuru; Başvuru; Kabul Edildi; Reddedildi; İptal'),
 @('Denetim Sonucu','Tanımsız; Reddedildi; Kabul Edildi; Düzeltme İçin İade Edildi'),
 @('Başvuru Sahibi Türü','İşletme; Üretici Örgütü; Kooperatif; Birlik; Diğer'),
 @('Hukuki Tür','Anonim; Limited; Kollektif; Komandit; Üretici Örgütü/Kooperatif/Birlik; Diğer'),
 @('Yatırım Türü','Yeni; Kapasite Artırımı; Modernizasyon; Teknoloji Yenileme'),
 @('Harcama Türü','Yapım İşleri; Makine-Ekipman; Danışmanlık; Tedarikçi Geliştirme; Yazılım/Donanım'),
 @('Yatırım Yeri Statüsü','Mülkiyet; Kira; Tahsis; İrtifak Hakkı; OSB/İhtisas Alanı Tahsisi; Diğer'),
 @('Yapı Ruhsatı','Mevcut; Başvurusu Yapıldı; Gerekmez Yazısı; Temin Edilmedi; Yapım İşi Yok'),
 @('Kullanıcı Rolü','Sistem Yöneticisi; Başvuru Kullanıcısı; Birim Kullanıcısı')
) @(2600,6760)
Add-P 'Not: Kontrollü değerlerin kullanıcıya gösterilen kesin Türkçe/İngilizce karşılıkları kaynak dosyalarıyla birlikte yönetişim altına alınmalıdır.' note

Add-Page
Add-P 'Ek B - Onay Sayfası' h1
Add-P 'Bu dokümandaki süreç, iş kuralı ve kavramsal veri tanımlarının mevcut işleyişi doğru yansıttığı aşağıdaki paydaşlarca doğrulanır.'
Add-Table @('Rol / Birim','Ad Soyad','Karar','Tarih / İmza') @(
 @('İş Birimi Sahibi','','Uygun / Revizyon',''),
 @('Ürün / Proje Sahibi','','Uygun / Revizyon',''),
 @('Bilgi Güvenliği / KVKK','','Uygun / Revizyon',''),
 @('Yazılım Ekibi','','Uygun / Revizyon',''),
 @('Test / Kalite','','Uygun / Revizyon','')
) @(2500,2500,2100,2260)
Add-P 'Onay notları:' h2
Add-P '............................................................................................................................'
Add-P '............................................................................................................................'
Add-P '............................................................................................................................'

Add-Raw '}'
[System.IO.File]::WriteAllText($outFile, $b.ToString(), [System.Text.Encoding]::ASCII)
Write-Output $outFile
