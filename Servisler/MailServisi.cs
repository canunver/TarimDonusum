using System.Net;
using System.Net.Mail;
using System.Text;
using TarimDonusum.Araclar;

namespace TarimDonusum.Servisler
{
    public class MailServisi : IMailServisi
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<MailServisi> _logger;

        public MailServisi(IConfiguration configuration, ILogger<MailServisi> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<string> MailAtAsync(
            string kimden,
            string kime,
            string konu,
            string bilgi,
            bool html,
            bool replyVar)
        {
            string smtpAdres = _configuration["Mail:SmtpAdres"] ?? "";
            int smtpPort = OrtakFonksiyonlar.Int32Yap(_configuration["Mail:SmtpPort"], 0);
            string smtpKullanici = _configuration["Mail:SmtpKullanici"] ?? "";
            string smtpParola = _configuration["Mail:SmtpParola"] ?? "";
            bool smtpGuvenli = string.Equals(_configuration["Mail:SmtpGuvenli"], "true", StringComparison.OrdinalIgnoreCase) ||
                OrtakFonksiyonlar.Int32Yap(_configuration["Mail:SmtpGuvenli"], 0) > 0;
            string varsayilanGonderen = _configuration["Mail:VarsayilanGonderen"] ?? "";

            if (string.IsNullOrWhiteSpace(smtpAdres))
                return "Mail gönderebilmek için Mail:SmtpAdres ayarı doldurulmalıdır.";

            if (smtpPort <= 0)
                smtpPort = 25;

            if (string.IsNullOrWhiteSpace(kimden))
                kimden = varsayilanGonderen;

            if (string.IsNullOrWhiteSpace(kimden))
                return "Mail gönderebilmek için gönderen adresi tanımlanmalıdır.";

            if (string.IsNullOrWhiteSpace(kime))
                return "Mail gönderebilmek için alıcı adresi tanımlanmalıdır.";

            using MailMessage mesaj = new MailMessage();
            string adresHatasi = MailAdresleriEkle(mesaj.To, kime);
            if (!string.IsNullOrWhiteSpace(adresHatasi))
                return adresHatasi;

            GonderenAta(mesaj, kimden);
            mesaj.Subject = konu;
            mesaj.Body = bilgi;
            mesaj.BodyEncoding = Encoding.UTF8;
            mesaj.SubjectEncoding = Encoding.UTF8;
            mesaj.IsBodyHtml = html;

            if (replyVar)
                mesaj.ReplyToList.Add(mesaj.From);

            using SmtpClient smtpClient = new SmtpClient(smtpAdres, smtpPort);
            smtpClient.EnableSsl = smtpGuvenli;
            smtpClient.DeliveryMethod = SmtpDeliveryMethod.Network;

            if (!string.IsNullOrWhiteSpace(smtpKullanici))
                smtpClient.Credentials = new NetworkCredential(smtpKullanici, smtpParola);

            try
            {
                await smtpClient.SendMailAsync(mesaj);
                return "";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Mail gönderilemedi. Kime: {Kime}, Konu: {Konu}", kime, konu);
                return "Mail gönderilemedi: " + ex.Message;
            }
        }

        private static void GonderenAta(MailMessage mesaj, string kimden)
        {
            string adres = kimden;
            string ad = "";

            if (kimden.Contains('|'))
            {
                string[] parcalar = kimden.Split('|');
                if (parcalar.Length > 0)
                    adres = parcalar[0];

                if (parcalar.Length > 1)
                    ad = parcalar[1];
            }

            mesaj.From = new MailAddress(adres, ad);
            mesaj.Sender = new MailAddress(adres, ad);
        }

        private static string MailAdresleriEkle(MailAddressCollection adresler, string adresMetni)
        {
            string temizAdresMetni = adresMetni.Replace(" ", "").Replace("^", ";");
            string[] mailAdresleri = temizAdresMetni.Split(';');
            int eklenenAdresSayisi = 0;

            for (int i = 0; i < mailAdresleri.Length; i++)
            {
                string mailAdresi = mailAdresleri[i];
                string mailAdi = "";

                if (string.IsNullOrWhiteSpace(mailAdresi))
                    continue;

                if (mailAdresi.Contains('|'))
                {
                    string[] parcalar = mailAdresi.Split('|');
                    if (parcalar.Length > 0)
                        mailAdresi = parcalar[0];

                    if (parcalar.Length > 1)
                        mailAdi = parcalar[1];
                }

                if (!OrtakFonksiyonlar.EPostaGecerliMi(mailAdresi))
                    return "Hatalı mail adresi: " + mailAdresi;

                adresler.Add(new MailAddress(mailAdresi, mailAdi));
                eklenenAdresSayisi++;
            }

            if (eklenenAdresSayisi == 0)
                return "Alıcısı olmayan mesaj gönderilmeye çalışılıyor.";

            return "";
        }
    }
}
