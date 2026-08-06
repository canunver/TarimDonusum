using System;
using System.Collections.Generic;
using System.Text;
using System.Net;

namespace TarimDonusum.Araclar
{
    public class DosyaIslem
    {
        public static string SablonAdOl(string raporYol, string modulYol, string sablonAd, bool imza)
        {
            string dosyaAd;
            if (imza)
            {
                dosyaAd = raporYol + "\\" + modulYol + "\\" + "eimza" + sablonAd;
                if (System.IO.File.Exists(dosyaAd))
                    return dosyaAd;
                dosyaAd = IkiEksik(raporYol) + "\\" + modulYol + "\\" + "eimza" + sablonAd;
                if (System.IO.File.Exists(dosyaAd))
                    return dosyaAd;
            }
            string dosyaOrjAd = raporYol + "\\" + modulYol + "\\" + sablonAd;
            if (System.IO.File.Exists(dosyaOrjAd))
                return dosyaOrjAd;
            dosyaAd = IkiEksik(raporYol) + "\\" + modulYol + "\\" + sablonAd;
            if (System.IO.File.Exists(dosyaAd))
                return dosyaAd;
            return dosyaOrjAd;
        }

        private static string IkiEksik(string raporYol)
        {
            if(!string.IsNullOrWhiteSpace(raporYol))
            {
                int uzunluk = raporYol.Length;
                if (uzunluk > 5)
                    return raporYol.Substring(0, uzunluk - 2);
            }
            return raporYol;
        }
        public static void DosyaGonder(string dosyaAd, string gidenAd, bool dosyaSil, string dosyaTuru)
        {
            DosyaGonder(dosyaAd, gidenAd, dosyaSil, dosyaTuru, true);
        }
        public static void DosyaGonder(string dosyaAd, string gidenAd, bool dosyaSil, string dosyaTuru, bool ekOlarakGonder)
        {
            string ext = System.IO.Path.GetExtension(dosyaAd).Replace(".", "");
            if (ext == "tmp") ext = OrtakFonksiyonlar.ExcelTur();
            
            if (string.IsNullOrWhiteSpace(dosyaTuru)) dosyaTuru = ext.ToUpper();
            dosyaTuru = dosyaTuru.Replace(".", "");

            if (dosyaTuru.ToUpper() == "PDF")
                dosyaTuru = "pdf";//Crystal report tan pdf olarak geldiği için pdf e çevrilme işlemi yapılmasın
            else if (dosyaTuru.ToUpper() == "DOCX")
                dosyaTuru = "docx";
            else if (dosyaTuru.ToUpper() == "DOC")
                dosyaTuru = "doc";
            else if (dosyaTuru.ToUpper() == "ZIP" || dosyaTuru.ToLower() == "zip")
                dosyaTuru = "zip";
            else if (dosyaTuru.ToUpper().StartsWith("HTM"))
                dosyaTuru = "html";
            else if (dosyaTuru.ToUpper() == "CSV" || dosyaTuru.ToLower() == "csv")
                dosyaTuru = "csv";
            else if (dosyaTuru.ToUpper() == "TXT" || dosyaTuru.ToLower() == "txt")
                dosyaTuru = "txt";
            else if (dosyaTuru.ToUpper() == "XML" || dosyaTuru.ToLower() == "xml")
                dosyaTuru = "xml";
            else if (dosyaTuru.ToUpper() == "XLSM")
                dosyaTuru = "xlsm";
            else if (dosyaTuru.ToUpper() == "XLSX")
                dosyaTuru = "xlsx";
            else if (dosyaTuru.ToUpper() == "XLT")
                dosyaTuru = "xlt";
            else if (dosyaTuru.ToUpper() == "XLS")
                dosyaTuru = "xls";
            else
                dosyaTuru = OrtakFonksiyonlar.ExcelTur();
            DosyaGonderX(dosyaAd, gidenAd, dosyaSil, dosyaTuru, ekOlarakGonder);
        }

        public static void DosyaGonderX(string dosyaAd, string gidenAd, bool dosyaSil, string ext)
        {
            DosyaGonderX(dosyaAd, gidenAd, dosyaSil, ext, true);
        }
        public static void DosyaGonderX(string dosyaAd, string gidenAd, bool dosyaSil, string ext, bool ekOlarakGonder)
        {
        }

        //public static void DosyaGonderPDF(string dosyaAd, string gidenAd, bool dosyaSil, bool ekOlarakGonder)
        //{
        //    string cikanDosya = "";
        //    try
        //    {
        //        cikanDosya = DosyaTipDegistir.Dosya2PDF(dosyaAd, dosyaSil);

        //        gidenAd = System.IO.Path.GetFileNameWithoutExtension(gidenAd) + ".pdf";

        //        if (cikanDosya != "Hata")
        //        {
        //            DosyaGonderX(cikanDosya, gidenAd, dosyaSil, "pdf", ekOlarakGonder);

        //            if (dosyaSil)
        //                System.IO.File.Delete(cikanDosya);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        throw ex;
        //    }
        //}
        public static string DosyaAdUret()
        {
            return DosyaAdUret("", "");
        }
        public static string DosyaAdUret(string dosyaAdOnEk, string dosyaAdArkaEk)
        {
            return System.IO.Path.GetTempPath() + dosyaAdOnEk + DosyaAdUretSade() + dosyaAdArkaEk;
        }
        public static string DosyaAdUretSade()
        {
            DateTime simdi = System.DateTime.Now;
            string dosyaAd = OrtakFonksiyonlar.MetinYap(simdi.Year, 4) + OrtakFonksiyonlar.MetinYap(simdi.Month, 2) + OrtakFonksiyonlar.MetinYap(simdi.Day, 2) + OrtakFonksiyonlar.MetinYap(simdi.Hour, 2) + OrtakFonksiyonlar.MetinYap(simdi.Minute, 2) + OrtakFonksiyonlar.MetinYap(simdi.Second, 2) + OrtakFonksiyonlar.MetinYap(System.DateTime.Now.Millisecond, 4);
            return dosyaAd;
        }
    }
}
