namespace TarimDonusum.IsKurallari
{
    public static class CevreselSosyalAnketTanimlari
    {
        public static IReadOnlyList<CevreselSosyalSoruGrubu> Tum { get; } =
        [
            new(
                Id: "1",
                Title: "Genel Bilgiler",
                Questions:
                [
                    new(Id: "1.1", Title: "", Text: "Başvuru sahibi tüzel kişiliğin unvanı nedir?", AnswerType: "text", Scope: "global", Placeholder: "Başvuru sahibi ticaret unvanı"),
                    new(Id: "1.2", Title: "", Text: "Yatırımınızın adı nedir?", AnswerType: "text", Scope: "global", Placeholder: "Yatırım adı"),
                    new(Id: "1.3", Title: "", Text: "Mevcut tesisinizin ve/veya planlanan yatırımınızın adresi nedir?", AnswerType: "textarea", Contexts: ["existing", "planned"], Note: "Adres ve konum bilgisini yazınız."),
                    new(Id: "1.4", Title: "", Text: "Başvurunuza konu yatırım türü nedir?", AnswerType: "select", Scope: "global", Options: ["Yeni yatırım", "Modernizasyon", "Kapasite artırımı"]),
                    new(Id: "1.5", Title: "", Text: "Kredi desteği kapsamında finanse edilmesini talep ettiğiniz yatırımın kısa özeti ve gerekçesi nedir?", AnswerType: "textarea", Scope: "global")
                ]),
            new(
                Id: "2",
                Title: "Çevresel ve Sosyal Risklerin Değerlendirilmesi",
                Descriptions:
                [
                    new(Type: "info", Text: "Kapsam Dışı Faaliyetler Listesi bu bölümde bilgi amaçlı gösterilir. Başvuru sahibi 2.1 sorusunda yatırımın bu listede yer alıp almadığını beyan eder."),
                    new(
                        Type: "details",
                        Title: "Kapsam Dışı Faaliyetler Listesi - Bilgilendirme",
                        Text: "Aşağıdaki faaliyetler proje kapsamında desteklenmeyecek faaliyetlerdir.",
                        Items:
                        [
                            "Mayınlar, ateşli silahlar, mühimmat ve patlayıcılar dahil olmak üzere her türlü silahın üretimi, ticareti veya kullanımına ilişkin faaliyetler.",
                            "Alkol, tütün ürünleri ve kontrol altındaki maddeler dahil olmak üzere tehlikeli malların üretimine yönelik faaliyetler.",
                            "Ulusal mevzuatta biyolojik çeşitliliğin korunması açısından korunan alan veya öncelikli koruma alanı olarak tanımlanan bölgelerde gerçekleştirilecek inşaat faaliyetleri.",
                            "Kritik doğal habitatlarda önemli ölçüde kayıp veya bozulmaya neden olabilecek ya da doğal habitatlar üzerinde önemli olumsuz etkiler oluşturabilecek faaliyetler.",
                            "Zorla çalıştırma, çocuk istismarı, çocuk işçiliği, insan ticareti veya 14-18 yaş arasındaki çocukların sağlık, güvenlik, eğitim veya gelişimlerini olumsuz etkileyebilecek işlerde çalıştırıldığı faaliyetler.",
                            "Türkiye Cumhuriyeti mevzuatı veya Türkiye'nin taraf olduğu uluslararası hukuki düzenlemeler kapsamında yasaklanmış faaliyetler.",
                            "Zorla tahliyeye neden olan faaliyetler.",
                            "Siyasi veya dini amaç taşıyan yatırımlar; siyasi partilere, sendikalara veya dini kurumlara yönelik idari hizmetler, altyapılar ve tesisler."
                        ])
                ],
                Questions:
                [
                    new(Id: "2.1", Title: "", Text: "Projeniz, Çevresel ve Sosyal Yönetim Sistemi (ÇSYS) Kapsam Dışı Faaliyetler Listesinde veya TKDK ile Uluslararası Finans Kuruluşları arasında imzalanan Hukuki Anlaşmadaki hariç tutulan faaliyetler listesinde yer alıyor mu?", AnswerType: "yesno", Scope: "global", Exclusion: true, ExplainOn: ["Evet"]),
                    new(Id: "2.2", Title: "", Text: "Yatırım kapsamında talep edilen harcama türleri nelerdir?", AnswerType: "select", Scope: "global", Options: ["Yapım işi", "Makine Ekipman", "Her ikisi"], Note: "Harcama kalemlerini kısaca açıklayınız.", AlwaysExplain: true),
                    new(Id: "2.3", Title: "", Text: "İşletmeniz ve yatırımınız Çevresel Etki Değerlendirme (ÇED) Yönetmeliği kapsamında değerlendiriliyor mu?", AnswerType: "select", Contexts: ["existing", "planned"], Options: ["ÇED kapsamına tabi değil", "ÇED Olumlu Kararı alındı", "ÇED Gerekli Değildir Kararı alındı", "ÇED Muafiyet Yazısı mevcut", "Başvuru süreci devam ediyor"], Note: "ÇED Kararı / ÇED Gerekli Değildir Kararı / Muafiyet Yazısını yükleyiniz. Başvuru süreci devam ediyorsa açıklayınız.", ExplainOn: ["Başvuru süreci devam ediyor"], DocOn: ["ÇED Olumlu Kararı alındı", "ÇED Gerekli Değildir Kararı alındı", "ÇED Muafiyet Yazısı mevcut"]),
                    new(Id: "2.4", Title: "", Text: "İşletmeniz ve yatırımınız için ulusal mevzuat kapsamında Çevre İzin Belgesi veya Çevre İzin ve Lisans Belgesi var mı?", AnswerType: "select", Contexts: ["existing", "planned"], Options: ["Var", "Yok"], ExplainOn: ["Var", "Yok"], DocOn: ["Var"]),
                    new(Id: "2.5", Title: "", Text: "Yatırımınızın uygulanabilmesi için, proje ile doğrudan ilişkili, yatırım ile eş zamanlı yapılması gereken ve yatırımınız olmasaydı ihtiyaç olmayan bağlantılı tesisler bulunuyor mu?", AnswerType: "yesno", Contexts: ["planned"], Note: "Erişim yolu, enerji nakil hattı, su hattı, kanalizasyon hattı, gübre/atık çukuru vb. bağlantılı tesisleri açıklayınız.", Info: "Bu bağlantılı tesisler için alt proje kapsamında kredi talep edilmese dahi, yatırımın faaliyete geçebilmesi için eş zamanlı olarak yapılması zorunlu olan tüm tesisler eksiksiz beyan edilmelidir.", ExplainOn: ["Evet"], DocOn: ["Evet"]),
                    new(Id: "2.6", Title: "", Text: "İşletmenizde ve yatırımınızda ISO 14001, ISO 45001, OHSAS 18001 benzeri yönetim sistemleri uygulanıyor mu/uygulanacak mı?", AnswerType: "yesno", Contexts: ["existing", "planned"], ExplainOn: ["Evet"], DocOn: ["Evet"]),
                    new(Id: "2.7", Title: "", Text: "İşletmenizin ve yatırım faaliyetlerinizin iklim değişikliğinden etkilenme potansiyeli bulunuyor mu?", AnswerType: "yesno", Contexts: ["existing", "planned"], Note: "Oluşabilecek riskleri ve bu risklerin yönetilmesi için uygulanacak önlemleri açıklayınız.", ExplainOn: ["Evet"], DocOn: ["Evet"]),
                    new(Id: "2.8", Title: "", Text: "İşletmenizin ve yatırım faaliyetlerinizin iklim değişikliğine bağlı çevresel risk oluşturma potansiyeli bulunuyor mu?", AnswerType: "yesno", Contexts: ["existing", "planned"], Note: "Oluşabilecek riskleri ve bu risklerin yönetilmesi için uygulanacak önlemleri açıklayınız.", ExplainOn: ["Evet"], DocOn: ["Evet"])
                ]),
            new(Id: "3", Title: "İşgücü ve Çalışma Koşulları", Questions:
                [
                    new(Id: "3.1", Title: "", Text: "Mevcut işletmenizde veya planlanan yatırımınızda işgücü, çalışma koşulları, iş sağlığı ve güvenliği ya da çalışan haklarına ilişkin dava, ceza, şikayet veya ciddi olay yaşandı mı?", AnswerType: "yesno", Contexts: ["existing", "planned"], Note: "Toplumsal cinsiyete dayalı şiddet, cinsel sömürü, istismar veya cinsel taciz dahil. Mevcut işletme için son 5 yılda dava, ceza vb. vaka varsa açıklayınız.", ExplainOn: ["Evet"]),
                    new(Id: "3.2", Title: "", Text: "Mevcut işletmenizde ve planlanan yatırımınızda çalışacak kişi sayısını çalışan kategorilerine göre belirtiniz.", AnswerType: "staff", Contexts: ["existing", "planned"], Info: "Göçmen çalışanlar Dünya Bankası Ç&S ESS2 kapsamında ayrı bir çalışan kategorisi olmayıp, doğrudan çalışanlar, yüklenici çalışanları veya birincil tedarikçi çalışanları arasında yer alabilir. Proje kapsamındaki göçmen çalışanlar diğer çalışanlarla eşit çalışma koşulları ve haklardan yararlanmalı, ayrımcılığa maruz bırakılmamalıdır."),
                    new(Id: "3.3", Title: "", Text: "Kadın çalışan oranı nedir / ne olacaktır?", AnswerType: "percent", Contexts: ["existing", "planned"], AlwaysExplain: true),
                    new(Id: "3.4", Title: "", Text: "İş sağlığı ve güvenliği risk değerlendirmesi, acil durum planı ve çalışan eğitimleri mevcut mu / hazırlanacak mı?", AnswerType: "yesno", Contexts: ["existing", "planned"], ExplainOn: ["Evet"], DocOn: ["Evet"])
                ]),
            new(Id: "4", Title: "Kaynak Verimliliği ve Kirliliğin Önlenmesi", Questions:
                [
                    new(Id: "4.1", Title: "", Text: "İşletmeniz ve yatırım faaliyetleri toprak, bitki örtüsü, yüzey suları veya yeraltı sularını olumsuz etkileyebilecek katı veya sıvı atık oluşumuna neden olacak mı?", AnswerType: "yesno", Contexts: ["existing", "planned"], ExplainOn: ["Evet"]),
                    new(Id: "4.2", Title: "", Text: "Yatırım faaliyetleri kapsamında tehlikeli atık oluşacak mı?", AnswerType: "yesno", Contexts: ["planned"], Note: "Evet ise bertaraf yöntemini açıklayınız. Var ise ilgili plan veya lisanslı firma bilgilerini yükleyiniz.", ExplainOn: ["Evet"], DocOn: ["Evet"]),
                    new(Id: "4.3", Title: "", Text: "İşletmenizin ve yatırım faaliyetlerinin su kalitesi üzerinde olumsuz etkisi olma ihtimali bulunuyor mu?", AnswerType: "yesno", Contexts: ["existing", "planned"], ExplainOn: ["Evet"]),
                    new(Id: "4.4", Title: "", Text: "Atıksu yönetimi altyapınızın durumu nedir?", AnswerType: "select", Contexts: ["existing", "planned"], Options: ["Mevcut ve yeterli", "Mevcut ancak yetersiz", "Mevcut değil"], AlwaysExplain: true),
                    new(Id: "4.5", Title: "", Text: "Su kullanım izinleri veya abonelik belgeleri var mı?", AnswerType: "select", Contexts: ["existing", "planned"], Options: ["Var", "Yok"], ExplainOn: ["Var", "Yok"], DocOn: ["Var"]),
                    new(Id: "4.6", Title: "", Text: "Enerji verimliliği, yenilenebilir enerji veya kaynak verimliliği tedbirleri uygulanıyor mu / uygulanacak mı?", AnswerType: "select", Contexts: ["existing", "planned"], Options: ["Var", "Yok"], ExplainOn: ["Var", "Yok"], DocOn: ["Var"]),
                    new(Id: "4.7", Title: "", Text: "İşletme veya yatırım faaliyetleri sera gazı emisyonu ya da önemli hava emisyonu oluşturacak mı?", AnswerType: "yesno", Contexts: ["existing", "planned"], Note: "Metan, siyah karbon, karbondioksit, diazot monoksit, perflorokarbonlar vb. iklim kirleticileri dahil.", Info: "Yıllık tahmini sera gazı emisyonu 25.000 ton CO₂ eşdeğerini aşan büyük ölçekli tarımsal sanayi yatırımlarında bağımsız bir sera gazı emisyon tahmin raporu başvuru kapsamında sunulmalıdır.", ExplainOn: ["Evet"], DocOn: ["Evet"]),
                    new(Id: "4.8", Title: "", Text: "Pestisit, veteriner ilacı, dezenfektan veya tehlikeli kimyasal kullanılacak mı?", AnswerType: "yesno", Contexts: ["existing", "planned"], Info: "Dünya Sağlık Örgütü (WHO) Sınıf IA ve IB pestisitleri ile Türkiye'de kullanımı yasaklı aktif maddelerin proje kapsamında kullanımı kesinlikle yasaktır.", ExplainOn: ["Evet"])
                ]),
            new(Id: "5", Title: "Toplum Sağlığı ve Güvenliği", Questions:
                [
                    new(Id: "5.1", Title: "", Text: "Yatırım alanı en yakın yerleşim yerine ne kadar uzaklıktadır?", AnswerType: "textarea", Contexts: ["existing", "planned"], Note: "Köy, mahalle vb. yerleşime uzaklık ve konumu açıklayınız."),
                    new(Id: "5.2", Title: "", Text: "Komşu yerleşimler, hassas alıcılar veya kamu kullanım alanları üzerinde olası etki var mı?", AnswerType: "select", Contexts: ["existing", "planned"], Options: ["Var", "Yok"], ExplainOn: ["Var", "Yok"], DocOn: ["Var"]),
                    new(Id: "5.3", Title: "", Text: "Yapım işleri sırasında toplum sağlığı ve güvenliği açısından risk oluşacak mı?", AnswerType: "yesno", Contexts: ["planned"], ExplainOn: ["Evet"]),
                    new(Id: "5.4", Title: "", Text: "Yatırım kapsamında geçici işçi kampı veya konaklama alanı kurulacak mı?", AnswerType: "yesno", Contexts: ["planned"], Note: "Evet ise yaklaşık çalışan sayısını belirtiniz.", ExplainOn: ["Evet"]),
                    new(Id: "5.5", Title: "", Text: "Yatırım faaliyetleri trafik yoğunluğunu veya trafik güvenliği riskini artıracak mı?", AnswerType: "yesno", Contexts: ["planned"], ExplainOn: ["Evet"]),
                    new(Id: "5.6", Title: "", Text: "Yatırım kapsamında tehlikeli madde taşınması, depolanması veya kullanımı olacak mı?", AnswerType: "yesno", Contexts: ["planned"], ExplainOn: ["Evet"]),
                    new(Id: "5.7", Title: "", Text: "Yatırım çevrede gürültü, koku, toz veya titreşim etkisi oluşturacak mı?", AnswerType: "yesno", Contexts: ["planned"], ExplainOn: ["Evet"]),
                    new(Id: "5.8", Title: "", Text: "Yapım ve işletme döneminde trafik yönetimi ve güvenlik tedbirleri planlandı mı?", AnswerType: "yesno", Contexts: ["planned"], Note: "Ulaşım güzergahlarını, beklenen trafik yoğunluğunu ve trafik güvenliği tedbirlerini açıklayınız.", ExplainOn: ["Evet"])
                ]),
            new(Id: "6", Title: "Arazi Edinimi, Arazi Kullanım Kısıtlamaları ve Gönülsüz Yeniden Yerleşim", Questions:
                [
                    new(Id: "6.1", Title: "", Text: "Yatırım yapılacak arazi veya tesis için mülkiyet, kira, tahsis veya kullanım hakkı belgesi var mı?", AnswerType: "select", Contexts: ["existing", "planned"], Options: ["Var", "Yok"], Note: "Tapu, kira sözleşmesi, tahsis yazısı vb. belgeleri yükleyiniz.", Info: "En az bir arazi kullanım/kullanma hakkı belgesi yüklenmeden başvuru tamamlanamaz.", ExplainOn: ["Var", "Yok"], DocOn: ["Var"]),
                    new(Id: "6.2", Title: "", Text: "Yatırım alanının arazi statüsü nedir?", AnswerType: "select", Contexts: ["existing", "planned"], Options: ["Kamu Arazisi", "Özel Mülkiyet", "Ortak/Kolektif Kullanım", "Diğer"], ExplainOn: ["Diğer"]),
                    new(Id: "6.3", Title: "", Text: "Yatırım nedeniyle arazi edinimi, kamulaştırma veya arazi kullanımında kısıtlama olacak mı?", AnswerType: "yesno", Contexts: ["planned"], ExplainOn: ["Evet"], DocOn: ["Evet"]),
                    new(Id: "6.4", Title: "", Text: "Yatırım nedeniyle herhangi bir kişi veya işletmenin fiziksel olarak taşınması gerekecek mi?", AnswerType: "yesno", Contexts: ["planned"], ExplainOn: ["Evet"], DocOn: ["Evet"]),
                    new(Id: "6.5", Title: "", Text: "Yatırım nedeniyle geçim kaynakları veya gelir getirici faaliyetler etkilenebilir mi?", AnswerType: "yesno", Contexts: ["planned"], ExplainOn: ["Evet"]),
                    new(Id: "6.6", Title: "", Text: "Yatırım alanını tapu veya resmi kira sözleşmesi olmadan fiilen kullanan kişi veya gruplar var mı?", AnswerType: "yesno", Contexts: ["planned"], Note: "Kullanım şeklini; kiracılık, hayvan otlatma, tarımsal üretim vb. olarak belirtiniz.", Info: "Tapu veya yasal kira sözleşmesine sahip olmamakla birlikte araziyi fiilen hayvan otlatma, tarımsal üretim veya benzeri amaçlarla kullanan kişilerin hakları da bu kapsamda değerlendirilmelidir.", ExplainOn: ["Evet"], DocOn: ["Evet"]),
                    new(Id: "6.7", Title: "", Text: "Arazi kullanımı nedeniyle hassas gruplar veya kırılgan kişiler etkilenebilir mi?", AnswerType: "yesno", Contexts: ["planned"], Info: "Tapu veya yasal kira sözleşmesine sahip olmamakla birlikte araziyi fiilen kullanan kişilerin geçim kaynakları üzerindeki olası etkiler de dikkate alınmalıdır.", ExplainOn: ["Evet"])
                ]),
            new(Id: "7", Title: "Biyoçeşitliliğin Korunması ve Canlı Doğal Kaynakların Sürdürülebilir Yönetimi", Questions:
                [
                    new(Id: "7.1", Title: "", Text: "Yatırım alanı korunan alan, hassas habitat, sulak alan, orman veya doğal yaşam alanı içinde ya da yakınında mı?", AnswerType: "yesno", Contexts: ["existing", "planned"], ExplainOn: ["Evet"], DocOn: ["Evet"]),
                    new(Id: "7.2", Title: "", Text: "Yatırım faaliyetleri doğal habitatlarda kayıp, bozulma veya parçalanmaya neden olacak mı?", AnswerType: "yesno", Contexts: ["existing", "planned"], ExplainOn: ["Evet"]),
                    new(Id: "7.3", Title: "", Text: "Yatırım alanında nesli tehlike altında olan türler veya hassas türler bulunuyor mu?", AnswerType: "yesno", Contexts: ["planned"], ExplainOn: ["Evet"], DocOn: ["Evet"]),
                    new(Id: "7.4", Title: "", Text: "Yatırım kapsamında ağaç kesimi, bitki örtüsü temizliği veya peyzaj değişikliği yapılacak mı?", AnswerType: "yesno", Contexts: ["planned"], ExplainOn: ["Evet"]),
                    new(Id: "7.5", Title: "", Text: "Yatırım faaliyetleri sucul ekosistemleri veya balıkçılık kaynaklarını etkileyebilir mi?", AnswerType: "yesno", Contexts: ["planned"], ExplainOn: ["Evet"]),
                    new(Id: "7.6", Title: "", Text: "Yatırım kapsamında canlı hayvan, bitki, tohum veya biyolojik materyal kullanılacak mı?", AnswerType: "yesno", Contexts: ["planned"], ExplainOn: ["Evet"]),
                    new(Id: "7.7", Title: "", Text: "Yatırım faaliyetleri istilacı türlerin yayılımına neden olabilir mi?", AnswerType: "yesno", Contexts: ["planned"], ExplainOn: ["Evet"]),
                    new(Id: "7.8", Title: "", Text: "Biyoçeşitlilik etkileri için izin, görüş veya uzman değerlendirmesi mevcut mu?", AnswerType: "yesno", Contexts: ["planned"], ExplainOn: ["Evet"], DocOn: ["Evet"])
                ]),
            new(Id: "8", Title: "Kültürel Miras", Questions:
                [
                    new(Id: "8.1", Title: "", Text: "Yatırım alanı kültürel miras, arkeolojik sit, tarihi yapı veya koruma alanı içinde ya da yakınında mı?", AnswerType: "yesno", Contexts: ["existing", "planned"], ExplainOn: ["Evet"]),
                    new(Id: "8.2", Title: "", Text: "Yapım işleri sırasında tesadüfi buluntu ihtimali var mı?", AnswerType: "yesno", Contexts: ["planned"], ExplainOn: ["Evet"]),
                    new(Id: "8.3", Title: "", Text: "Kültürel mirasla ilgili izin, kurum görüşü veya koruma kurulu kararı mevcut mu?", AnswerType: "yesno", Contexts: ["existing", "planned"], ExplainOn: ["Evet"], DocOn: ["Evet"]),
                    new(Id: "8.4", Title: "", Text: "Yatırım yerel topluluklar için manevi, geleneksel veya kültürel öneme sahip alanları etkileyebilir mi?", AnswerType: "yesno", Contexts: ["existing", "planned"], ExplainOn: ["Evet"])
                ]),
            new(Id: "9", Title: "Paydaş Katılımı ve Bilgilendirme", Questions:
                [
                    new(Id: "9.1", Title: "", Text: "Yatırımınızdan etkilenebilecek paydaşlar belirlendi mi?", AnswerType: "yesno", Contexts: ["planned"], ExplainOn: ["Evet"]),
                    new(Id: "9.2", Title: "", Text: "Yerel topluluklar veya komşu işletmeler yatırım hakkında bilgilendirildi mi?", AnswerType: "yesno", Contexts: ["planned"], ExplainOn: ["Evet"]),
                    new(Id: "9.3", Title: "", Text: "Yatırım ile ilgili şikayet ve önerilerin alınacağı bir mekanizma oluşturuldu mu?", AnswerType: "yesno", Contexts: ["planned"], ExplainOn: ["Evet"]),
                    new(Id: "9.4", Title: "", Text: "Paydaşlardan yatırım hakkında herhangi bir itiraz, şikayet veya talep geldi mi?", AnswerType: "yesno", Contexts: ["planned"], ExplainOn: ["Evet"]),
                    new(Id: "9.5", Title: "", Text: "Yatırım hakkında kadınlar, gençler, yaşlılar, engelliler veya hassas gruplarla özel bilgilendirme yapıldı mı?", AnswerType: "yesno", Contexts: ["planned"], ExplainOn: ["Evet"]),
                    new(Id: "9.6", Title: "", Text: "Paydaş katılımı veya bilgilendirme toplantılarına ilişkin belge, tutanak veya fotoğraf mevcut mu?", AnswerType: "yesno", Contexts: ["planned"], ExplainOn: ["Evet"], DocOn: ["Evet"]),
                    new(Id: "9.7", Title: "", Text: "Yatırımın uygulanması sırasında paydaşlarla iletişimden sorumlu kişi belirlendi mi?", AnswerType: "yesno", Contexts: ["planned"], ExplainOn: ["Evet"]),
                    new(Id: "9.8", Title: "", Text: "Mevcut işletme veya planlanan yatırım hakkında çevresel/sosyal şikayet kaydı var mı?", AnswerType: "yesno", Contexts: ["existing", "planned"], ExplainOn: ["Evet"], DocOn: ["Evet"])
                ])
        ];
    }

    public sealed record CevreselSosyalSoruGrubu(
        string Id,
        string Title,
        IReadOnlyList<CevreselSosyalSoru> Questions,
        IReadOnlyList<CevreselSosyalAciklama>? Descriptions = null,
        IReadOnlyList<string>? Scopes = null);

    public sealed record CevreselSosyalSoru(
        string Id,
        string Title,
        string Text,
        string AnswerType,
        IReadOnlyList<string>? Options = null,
        bool Explanation = false,
        bool Required = false,
        int? MaxLength = null,
        string? Scope = null,
        IReadOnlyList<string>? Contexts = null,
        string? Note = null,
        string? Info = null,
        string? Placeholder = null,
        bool Exclusion = false,
        IReadOnlyList<string>? ExplainOn = null,
        IReadOnlyList<string>? DocOn = null,
        bool AlwaysExplain = false);

    public sealed record CevreselSosyalAciklama(
        string Type,
        string? Text = null,
        string? Title = null,
        IReadOnlyList<string>? Items = null);
}
