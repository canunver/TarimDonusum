using System.ComponentModel.DataAnnotations;

namespace TarimDonusum.Araclar
{
    public static class OrtakFonksiyonlar
    {
        public static int Int32Yap(object? deger, int varsayilanDeger = 0)
        {
            if (deger == null || deger == DBNull.Value)
                return varsayilanDeger;

            try
            {
                return Convert.ToInt32(deger);
            }
            catch
            {
                return varsayilanDeger;
            }
        }

        public static bool TCKNGecerliMi(string? tckn)
        {
            if (string.IsNullOrWhiteSpace(tckn))
                return false;

            string deger = tckn.Trim();

            if (deger.Length != 11 || deger[0] == '0')
                return false;

            int[] rakamlar = new int[11];
            for (int i = 0; i < deger.Length; i++)
            {
                if (!char.IsDigit(deger[i]))
                    return false;

                rakamlar[i] = deger[i] - '0';
            }

            bool tumRakamlarAyni = true;
            for (int i = 1; i < rakamlar.Length; i++)
            {
                if (rakamlar[i] != rakamlar[0])
                {
                    tumRakamlarAyni = false;
                    break;
                }
            }

            if (tumRakamlarAyni)
                return false;

            int tekBasamaklarToplami =
                rakamlar[0] + rakamlar[2] + rakamlar[4] + rakamlar[6] + rakamlar[8];
            int ciftBasamaklarToplami =
                rakamlar[1] + rakamlar[3] + rakamlar[5] + rakamlar[7];

            int onuncuRakam = ((tekBasamaklarToplami * 7) - ciftBasamaklarToplami) % 10;
            if (onuncuRakam < 0)
                onuncuRakam += 10;

            int ilkOnRakamToplami = 0;
            for (int i = 0; i < 10; i++)
            {
                ilkOnRakamToplami += rakamlar[i];
            }

            int onBirinciRakam = ilkOnRakamToplami % 10;

            return rakamlar[9] == onuncuRakam && rakamlar[10] == onBirinciRakam;
        }

        public static bool EPostaGecerliMi(string? eposta)
        {
            if (string.IsNullOrWhiteSpace(eposta))
                return false;

            EmailAddressAttribute epostaKontrol = new EmailAddressAttribute();
            return epostaKontrol.IsValid(eposta.Trim());
        }

        public static bool TelefonNoGecerliMi(string? telefon)
        {
            return !string.IsNullOrWhiteSpace(TelNormalize(telefon));
        }

        public static bool ParolaGecerliMi(string? parola)
        {
            return ParolaSkoru(parola) >= 4;
        }

        public static string TelNormalize(string? telefon)
        {
            if (string.IsNullOrWhiteSpace(telefon))
                return "";

            string girilenDeger = telefon.Trim();
            string deger = "";

            for (int i = 0; i < girilenDeger.Length; i++)
            {
                if (char.IsWhiteSpace(girilenDeger[i]))
                    continue;

                if (!char.IsDigit(girilenDeger[i]))
                    return "";

                deger += girilenDeger[i];
            }

            if (deger.Length == 11 && deger[0] == '0')
                deger = deger.Substring(1);

            if (deger.Length == 10 && deger[0] != '0')
                return deger;

            return "";
        }

        private static int ParolaSkoru(string? parola)
        {
            if (string.IsNullOrWhiteSpace(parola))
                return 0;

            int skor = 0;

            if (parola.Length >= 8)
                skor++;

            if (MetinKarakterIceriyorMu(parola, BuyukHarfMi))
                skor++;

            if (MetinKarakterIceriyorMu(parola, KucukHarfMi))
                skor++;

            if (MetinKarakterIceriyorMu(parola, char.IsDigit))
                skor++;

            if (MetinKarakterIceriyorMu(parola, OzelKarakterMi))
                skor++;

            return skor;
        }

        private static bool MetinKarakterIceriyorMu(string metin, Func<char, bool> kontrol)
        {
            for (int i = 0; i < metin.Length; i++)
            {
                if (kontrol(metin[i]))
                    return true;
            }

            return false;
        }

        private static bool BuyukHarfMi(char karakter)
        {
            return char.IsUpper(karakter);
        }

        private static bool KucukHarfMi(char karakter)
        {
            return char.IsLower(karakter);
        }

        private static bool OzelKarakterMi(char karakter)
        {
            return !char.IsLetterOrDigit(karakter);
        }
    }
}
