using System.Drawing.Imaging;
using Aspose.Cells;
using System.IO;
using Aspose.Cells.Rendering;
using System.Net.NetworkInformation;

namespace TarimDonusum.Araclar

{
    public class TabloAspose : Tablo
    {
        private static int UZUNLUKCARPAN = 1;
        private Workbook XLS;
        //java.io.FileOutputStream tmpFileStream;
        //System.IO.Stream tmpFileStream;
        private string tmpFile;
        int aktifSheet;
        private string encoding;
        bool okuModu = false;
        private string dosyaSaklamaFormat;

        public void SheetToResim(int horRes, int verRes, string dosyaAd, int tur)
        {
            Worksheet sheet = XLS.Worksheets[aktifSheet];
            Aspose.Cells.Rendering.ImageOrPrintOptions options = new Aspose.Cells.Rendering.ImageOrPrintOptions();
            options.HorizontalResolution = horRes;
            options.VerticalResolution = verRes;
            options.IsCellAutoFit = false;
            if (tur == 1)
            {
                options.TiffCompression = Aspose.Cells.Rendering.TiffCompression.CompressionLZW;
                options.ImageFormat = System.Drawing.Imaging.ImageFormat.Tiff;
            }
            else
            {
                options.ImageFormat = System.Drawing.Imaging.ImageFormat.Jpeg;
            }
            options.PrintingPage = PrintingPageType.Default;
            SheetRender sr = new SheetRender(sheet, options);
            sr.ToImage(aktifSheet, dosyaAd);
        }

        private System.Drawing.Color RenkBul(TabloRenk renk)
        {
            if (renk == TabloRenk.AQUA) return System.Drawing.Color.Aqua;
            if (renk == TabloRenk.BLACK) return System.Drawing.Color.Black;
            if (renk == TabloRenk.CORAL) return System.Drawing.Color.Coral;
            if (renk == TabloRenk.GRAY_25) return System.Drawing.Color.Gray;
            if (renk == TabloRenk.ICE_BLUE) return System.Drawing.Color.FromArgb(212, 240, 255);
            if (renk == TabloRenk.IVORY) return System.Drawing.Color.Ivory;
            if (renk == TabloRenk.LIGHT_TURQUOISE2) return System.Drawing.Color.LightBlue;
            if (renk == TabloRenk.ORANGE) return System.Drawing.Color.Orange;
            if (renk == TabloRenk.PALE_BLUE) return System.Drawing.Color.PaleTurquoise;
            if (renk == TabloRenk.PERIWINKLE) return System.Drawing.Color.FromArgb(204, 204, 255);
            if (renk == TabloRenk.PINK2) return System.Drawing.Color.Pink;
            if (renk == TabloRenk.RED) return System.Drawing.Color.Red;
            if (renk == TabloRenk.ROSE) return System.Drawing.Color.MistyRose;
            if (renk == TabloRenk.VERY_LIGHT_YELLOW) return System.Drawing.Color.Yellow;
            if (renk == TabloRenk.YELLOW2) return System.Drawing.Color.YellowGreen;

            if (renk == TabloRenk.ALICEBLUE) return System.Drawing.Color.AliceBlue;
            if (renk == TabloRenk.BLUE) return System.Drawing.Color.Blue;
            if (renk == TabloRenk.GREEN) return System.Drawing.Color.Green;
            if (renk == TabloRenk.LAVENDER) return System.Drawing.Color.Lavender;
            if (renk == TabloRenk.DARKBLUE) return System.Drawing.Color.DarkBlue;
            if (renk == TabloRenk.DARKGREEN) return System.Drawing.Color.DarkGreen;
            if (renk == TabloRenk.DARKORANGE) return System.Drawing.Color.DarkOrange;
            if (renk == TabloRenk.DARKRED) return System.Drawing.Color.DarkRed;
            if (renk == TabloRenk.DARKSALMON) return System.Drawing.Color.DarkSalmon;
            if (renk == TabloRenk.DEEPPINK) return System.Drawing.Color.DeepPink;
            if (renk == TabloRenk.DEEPSKYBLUE) return System.Drawing.Color.DeepSkyBlue;
            if (renk == TabloRenk.DODGERBLUE) return System.Drawing.Color.DodgerBlue;
            if (renk == TabloRenk.FIREBRICK) return System.Drawing.Color.Firebrick;
            if (renk == TabloRenk.FORESTGREEN) return System.Drawing.Color.ForestGreen;
            if (renk == TabloRenk.GOLD) return System.Drawing.Color.Gold;
            if (renk == TabloRenk.OLIVE) return System.Drawing.Color.Olive;
            if (renk == TabloRenk.ORANGERED) return System.Drawing.Color.OrangeRed;
            if (renk == TabloRenk.WHITE) return System.Drawing.Color.White;

            return System.Drawing.Color.Black;
        }

        private CellBorderType BorderStilBul(LineStyle stil)
        {
            if (stil == LineStyle.DOUBLE) return CellBorderType.Double;
            if (stil == LineStyle.MEDIUM) return CellBorderType.Medium;
            if (stil == LineStyle.THIN) return CellBorderType.Thin;
            if (stil == LineStyle.HAIR) return CellBorderType.Hair;
            if (stil == LineStyle.MEDIUM_DASHED) return CellBorderType.MediumDashed;

            return CellBorderType.None;
        }

        public TabloAspose()
        {
            aktifSheet = 0;
            encoding = "ISO-8859-9";
        }

        public void YeniSheetEkle(string dosyaYol, string dosyaAd, int index)
        {
            Workbook kaynak = new Workbook(dosyaYol);
            Worksheet hedef;
            string ek = "";
            int say = 0;
            do
            {
                try
                {
                    hedef = XLS.Worksheets.Add(dosyaAd + ek);
                    break;
                }
                catch
                {
                    say++;
                    ek = " " + say.ToString();
                }
            } while (true);
            hedef.Copy(kaynak.Worksheets[index]);
        }

        public string SonucDosyaAd()
        {
            return tmpFile;
        }

        public void SatirAcKontrolsuz(int sheetNo, int satir, int acilacakSatirSayisi)
        {
            Worksheet sheet = XLS.Worksheets[sheetNo];
            sheet.Cells.InsertRows(satir, acilacakSatirSayisi);
            sheet.Cells.UnhideRows(satir, acilacakSatirSayisi, sheet.Cells.StandardHeightPixels);

            //SatirYukseklikAyarla(sheetNo, satir, satir + acilacakSatirSayisi - 1, 15 * UZUNLUKCARPAN);
            //for (int i = satir; i < satir + acilacakSatirSayisi; i++)
            //{
            //    if (sheet.Cells.IsRowHidden(i))
            //        sheet.Cells.UnhideRow(i, sheet.Cells.StandardHeightPixels);

            //    //sheet.Cells.SetRowHeightPixel(i, sheet.Cells.StandardHeightPixels);//AutoFitRow çalışmıyor.30.03.2014 Melih Cells 8.0.0 uncellemesinden sonra çalışmamaya başladı

            //    //sheet.AutoFitRow(i);
            //    //sheet.AutoFitRows(i, i);
            //}

        }

        public void SatirAc(int sheetNo, int satir, int acilacakSatirSayisi)
        {
            SatirAcKontrolsuz(sheetNo, satir, acilacakSatirSayisi);
        }

        public void SatirAc(int satir, int acilacakSatirSayisi)
        {
            SatirAcKontrolsuz(aktifSheet, satir, acilacakSatirSayisi);
        }

        public void SutunAcKontrolsuz(int sheetNo, int sutun, int acilacakSutunSayisi)
        {
            Worksheet sheet = XLS.Worksheets[sheetNo];
            sheet.Cells.InsertColumns(sutun, acilacakSutunSayisi);
        }

        public void SutunAc(int sheetNo, int sutun, int acilacakSutunSayisi)
        {
            SutunAcKontrolsuz(sheetNo, sutun, acilacakSutunSayisi);
        }

        public void SutunAc(int sutun, int acilacakSutunSayisi)
        {
            SutunAcKontrolsuz(aktifSheet, sutun, acilacakSutunSayisi);
        }

        private void SatirKontroluYap(int sheetNo, int satir)
        {
            //int rowSay = XLS.getSheet(sheetNo).getRows();
            //if (rowSay <= satir)
            //    SatirAcKontrolsuz(sheetNo, rowSay, satir - rowSay);
        }

        private void SutunKontroluYap(int sheetNo, int sutun)
        {
            //int sutSay = XLS.getSheet(sheetNo).getColumns();
            //if (sutSay <= sutun)
            //    SutunAcKontrolsuz(sheetNo, sutSay, sutun - sutSay);
        }

        public void HucreIcerikAraYaz(string bulDeger, string yazDeger)
        {
            HucreIcerikAraYaz(aktifSheet, TarimDonusum.Araclar.CellType.LABEL, bulDeger, yazDeger);
        }

        public void HucreIcerikAraYaz(string bulDeger, double yazDeger)
        {
            HucreIcerikAraYaz(aktifSheet, TarimDonusum.Araclar.CellType.LABEL, bulDeger, yazDeger);
        }


        public void HucreAdBulYaz(string hucreAd, string deger)
        {
            HucreAdBulYaz(hucreAd, TarimDonusum.Araclar.CellType.LABEL, deger);
        }

        public void HucreAdBulYaz(string hucreAd, double deger)
        {
            HucreAdBulYaz(hucreAd, TarimDonusum.Araclar.CellType.NUMBER, deger);
        }

        public void HucreAdBulYaz(string hucreAd, double deger, string paraIsareti, int kurusHane = 2)
        {
            int sheetNo = -1;
            int sutun1 = -1;
            int satir1 = -1;
            int sutun2 = -1;
            int satir2 = -1;

            HucreAdAdresCoz(hucreAd, ref sheetNo, ref satir1, ref sutun1, ref satir2, ref sutun2);

            if (sheetNo >= 0)
            {
                HucreDegerYaz(satir1, sutun1, deger, paraIsareti, kurusHane);
            }

        }

        public void HucreAdBulYaz(string hucreAd, TarimDonusum.Araclar.CellType tip, object deger)
        {
            int sheetNo = -1;
            int sutun1 = -1;
            int satir1 = -1;
            int sutun2 = -1;
            int satir2 = -1;

            HucreAdAdresCoz(hucreAd, ref sheetNo, ref satir1, ref sutun1, ref satir2, ref sutun2);

            if (sheetNo >= 0)
            {
                HucreDegerYaz(sheetNo, satir1, sutun1, tip, deger);
            }
        }

        public void HucreIcerikAraYaz(int sheetNo, TarimDonusum.Araclar.CellType tip, object bulDeger, object yazDeger)
        {
            int sutun1 = -1;
            int satir1 = -1;

            FindOptions opts = new FindOptions();
            opts.LookInType = LookInType.Values;
            opts.LookAtType = LookAtType.EntireContent;
            Cell cell = null;

            try
            {
                cell = XLS.Worksheets[sheetNo].Cells.Find(bulDeger, cell, opts);

                if (cell != null)
                {
                    satir1 = cell.Row;
                    sutun1 = cell.Column;
                }

                if (sheetNo >= 0)
                {
                    HucreDegerYaz(sheetNo, satir1, sutun1, tip, yazDeger);
                }
            }
            catch (Exception ex)
            {
                //TNS.OrtakFonksiyonlar.HataStrYaz("Aspose.XLS.Error:" + ex.Message);
            }
        }

        public void HucreDegerHtmlYaz(int sheetNo, int satir, int sutun, string HtmlString)
        {
            try
            {
                Worksheet sheet = XLS.Worksheets[sheetNo];
                Cell c = sheet.Cells[satir, sutun];
                c.HtmlString = HtmlString;
            }
            catch { }
        }

        private void HucreDegerYaz(int sheetNo, int satir, int sutun, TarimDonusum.Araclar.CellType tip, object deger)
        {
            try
            {
                bool bosalt;
                Worksheet sheet = XLS.Worksheets[sheetNo];
                Cell c = sheet.Cells[satir, sutun];
                if (deger == null)
                    bosalt = true;
                else
                    bosalt = false;

                if (!bosalt)
                {
                    c.PutValue(deger);
                }
                else
                {
                    c.PutValue(null);
                }
            }
            catch { }
        }

        private static readonly char[] FormulaBaslatanKarakterler = new[] { '=', '+', '-', '@' };

        public static string TemizleFormulaInjection(string deger)
        {
            if (string.IsNullOrEmpty(deger))
                return deger;

            int i = 0;
            bool formulBaslatanVar = false;

            // Baştaki whitespace ve '=' karakterlerini tara
            while (i < deger.Length)
            {
                char c = deger[i];

                // Baştaki whitespace'leri (space, tab, CR, LF vs.) atla
                if (char.IsWhiteSpace(c))
                {
                    i++;
                    continue;
                }
                // Formül başlatan karakterlerden biri mi?
                if (Array.IndexOf(FormulaBaslatanKarakterler, c) >= 0)
                {
                    formulBaslatanVar = true;
                    i++;
                    continue;
                }

                break;
            }

            if (!formulBaslatanVar)
                return deger;

            // Baştaki tüm whitespace ve formül başlatanları kısmını kes
            return deger.Substring(i);
        }

        public void HucreDegerYaz(int satir, int sutun, string deger)
        {
            HucreDegerYaz(aktifSheet, satir, sutun, TarimDonusum.Araclar.CellType.LABEL, TemizleFormulaInjection(deger));
        }

        public void HucreDegerYaz(int satir, int sutun, double deger)
        {
            HucreDegerYaz(aktifSheet, satir, sutun, TarimDonusum.Araclar.CellType.NUMBER, deger);
        }

        public void HucreDegerYaz(int satir, int sutun, decimal deger)
        {
            HucreDegerYaz(aktifSheet, satir, sutun, TarimDonusum.Araclar.CellType.NUMBER, Convert.ToDouble(deger));
        }

        public void HucreDegerYaz(int satir, int sutun, int deger)
        {
            HucreDegerYaz(aktifSheet, satir, sutun, TarimDonusum.Araclar.CellType.NUMBER, Convert.ToDouble(deger));
        }

        public void HucreDegerYaz(int satir, int sutun, DateTime deger)
        {
            HucreDegerYaz(aktifSheet, satir, sutun, TarimDonusum.Araclar.CellType.DATE, deger);
        }

        public void HucreDegerYaz(int satir, int sutun, double deger, string paraIsareti, int kurusHane = 2)
        {
            try
            {
                bool bosalt;
                Worksheet sheet = XLS.Worksheets[aktifSheet];
                Cell c = sheet.Cells[satir, sutun];
                if (deger == null)
                    bosalt = true;
                else
                    bosalt = false;

                if (!bosalt)
                {
                    c.PutValue(deger);
                    var currencyStyle = XLS.CreateStyle();

                    string kurus = "";
                    if (kurusHane > 0)
                    {
                        kurus = "".PadRight(kurusHane, '0');
                        kurus = "." + kurus;
                    }

                    currencyStyle.Custom = "#,##0" + kurus + " [$" + paraIsareti + "]";

                    c.SetStyle(currencyStyle, new StyleFlag { NumberFormat = true });
                }
                else
                {
                    c.PutValue(null);
                }
            }
            catch { }
        }

        public double HucreDegerAlDbl(int sheetNo, int satir, int sutun)
        {
            Worksheet sheet = XLS.Worksheets[sheetNo];
            try
            {
                Cell cell = sheet.Cells[satir, sutun];
                return cell.DoubleValue;
            }
            catch
            {
                return 0;
            }
        }

        public double HucreDegerAlDbl(int satir, int sutun)
        {
            return HucreDegerAlDbl(aktifSheet, satir, sutun);
        }

        public string HucreDegerAl(int sheetNo, int satir, int sutun, int noktaSay)
        {
            Worksheet sheet = XLS.Worksheets[sheetNo];
            Cell cell = sheet.Cells[satir, sutun];
            return cell.StringValue;
        }

        public string HucreDegerAl(int satir, int sutun)
        {
            return HucreDegerAl(aktifSheet, satir, sutun, 0);
        }

        public string HucreAdDegerAl(string hucreAd)
        {
            int sheetNo = -1;
            int sutun1 = -1;
            int satir1 = -1;
            int sutun2 = -1;
            int satir2 = -1;

            HucreAdAdresCoz(hucreAd, ref sheetNo, ref satir1, ref sutun1, ref satir2, ref sutun2);

            if (sheetNo >= 0)
            {
                return HucreDegerAl(sheetNo, satir1, sutun1, 0);
            }
            else
                return "";
        }

        public string HucreFormulAl(int satir, int sutun)
        {
            Worksheet sheet = XLS.Worksheets[aktifSheet];
            Cell cell = sheet.Cells[satir, sutun];
            string f = cell.Formula;
            if (string.IsNullOrWhiteSpace(f)) return f;
            if (f.StartsWith("=")) return f.Substring(1);
            return f;
        }

        public void HucreFormulYaz(int satir, int sutun, string formul)
        {
            Worksheet sheet = XLS.Worksheets[aktifSheet];
            Cell cell = sheet.Cells[satir, sutun];
            cell.Formula = formul;
        }

        public void KorumayaAl()
        {
            KorumayaAl("01020304");
        }

        public void KorumayaAl(string sifre)
        {
            Worksheet sheet = XLS.Worksheets[aktifSheet];
            sheet.Protect(ProtectionType.All, sifre, null);
        }

        public void HucreAdAdresCoz(string hucreAd, ref int satir, ref int sutun)
        {
            sutun = -1;
            satir = -1;
            try
            {
                Aspose.Cells.Range r = XLS.Worksheets.GetRangeByName(hucreAd);
                if (r != null)
                {
                    satir = r.FirstRow;
                    sutun = r.FirstColumn;
                }
            }
            catch { }
        }

        public void HucreAdAdresCoz(string bolgeAd, ref int sheetNo, ref int satir1, ref int sutun1, ref int satir2, ref int sutun2)
        {
            sheetNo = -1;
            sutun1 = -1;
            satir1 = -1;
            sutun2 = -1;
            satir2 = -1;
            try
            {
                Aspose.Cells.Range r = XLS.Worksheets.GetRangeByName(bolgeAd);
                if (r != null)
                {
                    sheetNo = r.Worksheet.Index;
                    satir1 = r.FirstRow;
                    sutun1 = r.FirstColumn;
                    satir2 = satir1 + r.RowCount - 1;
                    sutun2 = sutun1 + r.ColumnCount - 1;
                }
            }
            catch (Exception)
            {
                sheetNo = -1;
                sutun1 = -1;
                satir1 = -1;
                sutun2 = -1;
                satir2 = -1;
            }
        }

        public void SatirKopyalaAc(int kaynakSheet, int satir, int kopyalanacakSatirSayisi, int hedefSheet, int hedefSatir)
        {
            SatirAc(hedefSheet, hedefSatir, kopyalanacakSatirSayisi);
            SatirKopyala(kaynakSheet, satir, kopyalanacakSatirSayisi, hedefSheet, hedefSatir);
        }

        public void SatirKopyalaAc(int satir, int kopyalanacakSatirSayisi, int hedefSatir)
        {
            SatirKopyalaAc(aktifSheet, satir, kopyalanacakSatirSayisi, aktifSheet, hedefSatir);
        }

        /// <summary>
        /// Bu method duzgun calismiyor, HucreKopyala kullaniniz.
        /// </summary>
        /// <param name="satir"></param>
        /// <param name="kopyalanacakSatirSayisi"></param>
        /// <param name="hedefSatir"></param>
        [Obsolete("Bu method duzgun calismiyor, HucreKopyala kullaniniz.")]
        public void SatirKopyala(int kaynakSheet, int satir, int kopyalanacakSatirSayisi, int hedefSheet, int hedefSatir)
        {
            Worksheet sheetSrc = XLS.Worksheets[kaynakSheet];
            Worksheet sheetDst = XLS.Worksheets[hedefSheet];
            try
            {
                sheetDst.Cells.CopyRows(sheetSrc.Cells, satir, hedefSatir, kopyalanacakSatirSayisi);
            }
            catch { }
        }

        /// <summary>
        /// Bu method duzgun calismiyor, HucreKopyala kullaniniz.
        /// </summary>
        /// <param name="satir"></param>
        /// <param name="kopyalanacakSatirSayisi"></param>
        /// <param name="hedefSatir"></param>
        [Obsolete("Bu method duzgun calismiyor, HucreKopyala kullaniniz.")]
        public void SatirKopyala(int satir, int kopyalanacakSatirSayisi, int hedefSatir)
        {
            SatirKopyala(aktifSheet, satir, kopyalanacakSatirSayisi, aktifSheet, hedefSatir);
        }

        public void SutunKopyalaAc(int kaynakSheet, int sutun, int kopyalanacakSutunSayisi, int hedefSheet, int hedefSutun)
        {
            SutunAc(hedefSheet, hedefSutun, kopyalanacakSutunSayisi);
            SutunKopyala(kaynakSheet, sutun, kopyalanacakSutunSayisi, hedefSheet, hedefSutun);
        }

        public void SutunKopyalaAc(int sutun, int kopyalanacakSutunSayisi, int hedefSutun)
        {
            SutunKopyalaAc(aktifSheet, sutun, kopyalanacakSutunSayisi, aktifSheet, hedefSutun);
        }

        /// <summary>
        /// Bu method duzgun calismiyor, HucreKopyala kullaniniz.
        /// </summary>
        /// <param name="sutun"></param>
        /// <param name="kopyalanacakSutunSayisi"></param>
        /// <param name="hedefSutun"></param>
        [Obsolete("Bu method duzgun calismiyor, HucreKopyala kullaniniz.")]
        public void SutunKopyala(int kaynakSheet, int sutun, int kopyalanacakSutunSayisi, int hedefSheet, int hedefSutun)
        {
            Worksheet sheetSrc = XLS.Worksheets[kaynakSheet];
            Worksheet sheetDst = XLS.Worksheets[hedefSheet];
            sheetDst.Cells.CopyColumns(sheetSrc.Cells, sutun, hedefSutun, kopyalanacakSutunSayisi);
        }

        /// <summary>
        /// Bu method duzgun calismiyor, HucreKopyala kullaniniz.
        /// </summary>
        /// <param name="sutun"></param>
        /// <param name="kopyalanacakSutunSayisi"></param>
        /// <param name="hedefSutun"></param>
        [Obsolete("Bu method duzgun calismiyor, HucreKopyala kullaniniz.")]
        public void SutunKopyala(int sutun, int kopyalanacakSutunSayisi, int hedefSutun)
        {
            SutunKopyala(aktifSheet, sutun, kopyalanacakSutunSayisi, aktifSheet, hedefSutun);
        }

        public void HucreKopyala(int kaynakSheet, int satir1, int sutun1, int satir2, int sutun2, int hedefSheet, int hedefSatir, int hedefSutun)
        {
            try
            {
                int toplamSatir = satir2 - satir1 + 1;
                int toplamSutun = sutun2 - sutun1 + 1;
                Aspose.Cells.Range rangeSrc = XLS.Worksheets[kaynakSheet].Cells.CreateRange(satir1, sutun1, toplamSatir, toplamSutun);//toplamSatir --> 1 olarak değiştirildi. Melih 15.05.2014
                Aspose.Cells.Range rangeDst = XLS.Worksheets[hedefSheet].Cells.CreateRange(hedefSatir, hedefSutun, toplamSatir, toplamSutun);
                rangeDst.Copy(rangeSrc);
            }
            catch { }
        }

        public void HucreKopyala(int satir1, int sutun1, int satir2, int sutun2, int hedefSatir, int hedefSutun)
        {
            HucreKopyala(aktifSheet, satir1, sutun1, satir2, sutun2, aktifSheet, hedefSatir, hedefSutun);
        }


        /// <summary>
        /// Hucres the kopyala.
        /// </summary>
        /// <param name="kaynakSheet">Kaynak sayfa.</param>
        /// <param name="kaynakBaslaSatir">kaynak basla satir.</param>
        /// <param name="kaynakBaslaSutun">kaynak basla sutun.</param>
        /// <param name="kaynakSatirSayisi">kaynak satir sayisi.</param>
        /// <param name="kaynakSutunSayisi">kaynak sutun sayisi.</param>
        /// <param name="hedefSheet">hedef sheet.</param>
        /// <param name="hedefBaslaSatir">hedef basla satir.</param>
        /// <param name="hedefBaslaSutun">hedef basla sutun.</param>
        /// <param name="hedefSatirSayisi">hedef satir sayisi.</param>
        /// <param name="hedefSutunSayisi">hedef sutun sayisi.</param>
        public void HucreKopyala(int kaynakSheet, int kaynakBaslaSatir, int kaynakBaslaSutun, int kaynakSatirSayisi, int kaynakSutunSayisi, int hedefSheet, int hedefBaslaSatir, int hedefBaslaSutun, int hedefSatirSayisi, int hedefSutunSayisi)
        {
            try
            {
                Aspose.Cells.Range rangeSrc = XLS.Worksheets[kaynakSheet].Cells.CreateRange(kaynakBaslaSatir, kaynakBaslaSutun, kaynakSatirSayisi, kaynakSutunSayisi);
                Aspose.Cells.Range rangeDst = XLS.Worksheets[hedefSheet].Cells.CreateRange(hedefBaslaSatir, hedefBaslaSutun, hedefSatirSayisi, hedefSutunSayisi);
                rangeDst.Copy(rangeSrc);
            }
            catch { }
        }

        /// <summary>
        /// Hucres the kopyala.
        /// </summary>
        /// <param name="kaynakSheet">Kaynak sayfa.</param>
        /// <param name="kaynakBaslaSatir">kaynak basla satir.</param>
        /// <param name="kaynakBaslaSutun">kaynak basla sutun.</param>
        /// <param name="kaynakSatirSayisi">kaynak satir sayisi.</param>
        /// <param name="kaynakSutunSayisi">kaynak sutun sayisi.</param>
        /// <param name="hedefBaslaSatir">hedef basla satir.</param>
        /// <param name="hedefBaslaSutun">hedef basla sutun.</param>
        /// <param name="hedefSatirSayisi">hedef satir sayisi.</param>
        /// <param name="hedefSutunSayisi">hedef sutun sayisi.</param>
        public void HucreKopyala(int kaynakSheet, int kaynakBaslaSatir, int kaynakBaslaSutun, int kaynakSatirSayisi, int kaynakSutunSayisi, int hedefBaslaSatir, int hedefBaslaSutun, int hedefSatirSayisi, int hedefSutunSayisi)
        {
            HucreKopyala(aktifSheet, kaynakBaslaSatir, kaynakBaslaSutun, kaynakSatirSayisi, kaynakSutunSayisi, aktifSheet, hedefBaslaSatir, hedefBaslaSutun, hedefSatirSayisi, hedefSutunSayisi);
        }

        public void HucreBirlestir(int satir1, int sutun1, int satir2, int sutun2)
        {
            Worksheet sheet = XLS.Worksheets[aktifSheet];
            try
            {
                sheet.Cells.Merge(satir1, sutun1, satir2 - satir1 + 1, sutun2 - sutun1 + 1);
            }
            catch { }
        }

        public void HucreBirlestirme(int satir1, int sutun1, int satir2, int sutun2)
        {
            for (int i = satir1; i <= satir2; i++)
            {
                for (int j = sutun1; j <= sutun2; j++)
                    HucreBirlestirme(i, j);
            }
        }

        public void HucreBirlestirme(int satir, int sutun)
        {
            Worksheet sheet = XLS.Worksheets[aktifSheet];

            //Accessing the collection of merged cells as an ArrayList
            System.Collections.ArrayList mergedCells = sheet.Cells.MergedCells;

            //Iterating through each WebCellArea object stored in merged cells
            foreach (CellArea wca in mergedCells)
            {
                //Checking if a desired range of merged cells are found
                if (wca.StartRow == satir && wca.StartColumn == sutun)
                {
                    //Removing the specific WebCellArea object and breaking the loop
                    mergedCells.Remove(wca);
                    break;
                }
            }
        }

        public void OtomatikYukseklik(int sheetNo, int satir, int sutun1, int sutun2)
        {
            Worksheet sheet = XLS.Worksheets[sheetNo];
            sheet.AutoFitRow(satir, sutun1, sutun2);
        }


        public void SatirYukseklikAyarla(int satir1, int satir2, int yukseklik, int ekle, int minYuks)
        {
            SatirYukseklikAyarla(aktifSheet, satir1, satir2, yukseklik, ekle, minYuks);
        }

        public void SatirYukseklikAyarla(int satir1, int satir2, int yukseklik)
        {
            SatirYukseklikAyarla(aktifSheet, satir1, satir2, yukseklik, 0, 0);
        }

        public void SatirYukseklikAyarla(int sheetNo, int satir1, int satir2, int yukseklik, int ekle, int minYuks)
        {
            Worksheet sheet = XLS.Worksheets[sheetNo];
            Cells cs = sheet.Cells;
            SatirKontroluYap(aktifSheet, satir2);

            if (yukseklik == -1)
            {
                for (int sat = satir1; sat <= satir2; sat++)
                {
                    sheet.AutoFitRow(sat);
                    if (ekle > 0 || minYuks > 0)
                    {
                        int tut = SatirYukseklikAl(sat) + ekle;
                        if (tut < minYuks)
                        {
                            tut = minYuks;
                        }
                        cs.SetRowHeight(sat, MaksYukseklikKontrol(tut));
                    }
                }
            }
            else
            {
                for (int sat = satir1; sat <= satir2; sat++)
                {
                    if (yukseklik > 1000) yukseklik = yukseklik / 1000;
                    cs.SetRowHeight(sat, MaksYukseklikKontrol(yukseklik));
                }
            }
        }

        private double MaksYukseklikKontrol(double tut)
        {
            if (tut > 409) return 409;
            return tut;
        }

        public void SatirGercekYukseklikAyarla(int satir1, int satir2, double yukseklik)
        {
            Worksheet sheet = XLS.Worksheets[aktifSheet];
            Cells cs = sheet.Cells;
            SatirKontroluYap(aktifSheet, satir2);

            for (int sat = satir1; sat <= satir2; sat++)
            {
                cs.SetRowHeight(sat, MaksYukseklikKontrol(yukseklik));
            }
        }

        public int SatirYukseklikAl(int sheetNo, int satir)
        {
            Worksheet sheet = XLS.Worksheets[sheetNo];
            Cells cs = sheet.Cells;
            double tut1 = cs.GetRowHeight(satir);
            if (tut1 == 0) tut1 = 15;
            return (int)(tut1);
        }

        public int SatirYukseklikAl(int satir)
        {
            return SatirYukseklikAl(aktifSheet, satir);
        }

        public double SatirGercekYukseklikAl(int satir)
        {
            Worksheet sheet = XLS.Worksheets[aktifSheet];
            Cells cs = sheet.Cells;
            return cs.GetRowHeight(satir);
        }

        public void SutunGenislikAyarla(int sutun1, int sutun2, int genislik)
        {
            try
            {
                SutunKontroluYap(aktifSheet, sutun2);
                Worksheet sheet = XLS.Worksheets[aktifSheet];
                Cells cs = sheet.Cells;

                if (genislik == -1)
                {
                    for (int sut = sutun1; sut <= sutun2; sut++)
                    {
                        sheet.AutoFitColumn(sut);
                    }
                }
                else
                {
                    for (int sut = sutun1; sut <= sutun2; sut++)
                        cs.SetColumnWidth(sut, genislik);// / UZUNLUKCARPAN);//Jexcel zamanı çalışan geliştirilen rapor (Tarim Hibe) UZUNLUKCARPAN olmadan düzgün çalışıyor Melih 15.05.2014
                }
            }
            catch { }
        }

        public void SutunGenislikAyarlaPixel(int sutun1, int sutun2, int genislik)
        {
            try
            {
                SutunKontroluYap(aktifSheet, sutun2);
                Worksheet sheet = XLS.Worksheets[aktifSheet];
                Cells cs = sheet.Cells;

                if (genislik == -1)
                {
                    for (int sut = sutun1; sut <= sutun2; sut++)
                    {
                        sheet.AutoFitColumn(sut);
                    }
                }
                else
                {
                    for (int sut = sutun1; sut <= sutun2; sut++)
                        cs.SetColumnWidthPixel(sut, genislik);
                }
            }
            catch { }
        }

        public void SutunGenislikAyarla(int sutun1, int sutun2, int genislik, int ekle, int minGenislik)
        {
            SutunGenislikAyarla(aktifSheet, sutun1, sutun2, genislik, ekle, minGenislik);
        }

        public void SutunGenislikAyarla(int sheetNo, int sutun1, int sutun2, int genislik, int ekle, int minGenislik)
        {
            Worksheet sheet = XLS.Worksheets[sheetNo];
            Cells cs = sheet.Cells;
            SutunKontroluYap(sheetNo, sutun2);

            if (genislik == -1)
            {
                for (int sut = sutun1; sut <= sutun2; sut++)
                {
                    sheet.AutoFitColumn(sut);
                    if (ekle > 0 || minGenislik > 0)
                    {
                        int tut = SutunGenislikAl(sut) + ekle;
                        if (tut < minGenislik)
                        {
                            tut = minGenislik;
                        }
                        cs.SetColumnWidthPixel(sut, tut);
                    }
                }
            }
            else
            {
                for (int sut = sutun1; sut <= sutun2; sut++)
                {
                    cs.SetColumnWidthPixel(sut, genislik);
                }
            }
        }

        public int SutunGenislikAl(int sheetNo, int sutun)
        {
            Worksheet sheet = XLS.Worksheets[sheetNo];
            Cells cs = sheet.Cells;
            return cs.GetViewColumnWidthPixel(sutun);
        }

        public int SutunGenislikAl(int sutun)
        {
            return SutunGenislikAl(aktifSheet, sutun);
        }

        public void SutunGizle(int sutun1, int sutun2, bool gizle)
        {
            SutunKontroluYap(aktifSheet, sutun2);
            Worksheet sheet = XLS.Worksheets[aktifSheet];
            Cells cs = sheet.Cells;
            for (int sut = sutun1; sut <= sutun2; sut++)
                if (gizle)
                    cs.HideColumn(sut);
                else
                    cs.UnhideColumn(sut, 14.86);
        }

        public void SatirGizle(int satir1, int satir2, bool gizle)
        {
            SatirGizle(aktifSheet, satir1, satir2, gizle);
        }

        public void SatirGizle(int sheetNo, int satir1, int satir2, bool gizle)
        {
            SatirKontroluYap(sheetNo, satir2);
            Worksheet sheet = XLS.Worksheets[sheetNo];
            Cells cs = sheet.Cells;
            for (int sat = satir1; sat <= satir2; sat++)
                if (gizle)
                    cs.HideRow(sat);
                else
                    cs.UnhideRow(sat, 15);
        }

        public void SatirSil(int satir1, int satir2)
        {
            SatirKontroluYap(aktifSheet, satir2);
            Worksheet sheet = XLS.Worksheets[aktifSheet];
            Cells cs = sheet.Cells;
            cs.DeleteRows(satir1, satir2 - satir1 + 1);
        }

        public void SutunSil(int sutun1, int sutun2)
        {
            SutunKontroluYap(aktifSheet, sutun2);
            Worksheet sheet = XLS.Worksheets[aktifSheet];
            Cells cs = sheet.Cells;
            cs.DeleteColumns(sutun1, sutun2 - sutun1 + 1, true);
        }

        //		public void DuseyHizala(int satir, int sutun, int deger)
        //		{
        //			//0-sol
        //			//1-sağ
        //			//2-orta
        //			//3-iki yana yasla
        //			CellFormatsNET.TCellHorizAlignment hiza=CellFormatsNET.TCellHorizAlignment.chaLeft;
        //
        //			if (deger==2)
        //				hiza=CellFormatsNET.TCellHorizAlignment.chaCenter;
        //
        //			XLS.Sheets[0].GetCell(sutun,satir).SetHorizAlignment(hiza);
        //		}

        public void HucreRakamFormatla(int satir, int sutun, string deger)
        {
            HucreRakamFormatla(satir, sutun, satir, sutun, deger);
        }

        public void HucreRakamFormatla(int satir1, int sutun1, int satir2, int sutun2, string deger)
        {
            HucreFormatla(satir1, sutun1, satir2, sutun2, deger);
        }

        public void HucreFormatla(int satir1, int sutun1, int satir2, int sutun2, string deger)
        {
            Aspose.Cells.Style style = XLS.Styles[XLS.Styles.Add()];
            style.Custom = deger;

            Worksheet sheet = XLS.Worksheets[aktifSheet];
            Aspose.Cells.Range r = sheet.Cells.CreateRange(satir1, sutun1, satir2 - satir1 + 1, sutun2 - sutun1 + 1);

            StyleFlag sf = new StyleFlag();
            sf.NumberFormat = true;
            r.ApplyStyle(style, sf);
        }

        public void DuseyCizgiCiz(int satir1, int satir2, int sutun, TarimDonusum.Araclar.LineStyle stil, TabloRenk renk, bool solMu)
        {
            try
            {
                Worksheet sheet = XLS.Worksheets[aktifSheet];
                Aspose.Cells.Range r = sheet.Cells.CreateRange(satir1, sutun, satir2 - satir1 + 1, 1);

                BorderType b;
                if (solMu)
                    b = BorderType.LeftBorder;
                else
                    b = BorderType.RightBorder;
                r.SetOutlineBorder(b, BorderStilBul(stil), RenkBul(renk));

            }
            catch { }
        }

        public void DuseyCizgiCiz(int satir1, int satir2, int sutun, TarimDonusum.Araclar.LineStyle stil, TabloRenk renk)
        {
            DuseyCizgiCiz(satir1, satir2, sutun, stil, renk, true);
        }

        public void YatayCizgiCiz(int satir, int sutun1, int sutun2, TarimDonusum.Araclar.LineStyle stil, TabloRenk renk, bool ustMu)
        {
            try
            {
                Worksheet sheet = XLS.Worksheets[aktifSheet];
                Aspose.Cells.Range r = sheet.Cells.CreateRange(satir, sutun1, 1, sutun2 - sutun1 + 1);

                BorderType b;
                if (ustMu)
                    b = BorderType.TopBorder;
                else
                    b = BorderType.BottomBorder;
                r.SetOutlineBorder(b, BorderStilBul(stil), RenkBul(renk));
            }
            catch { }
        }

        public void YatayCizgiCiz(int satir, int sutun1, int sutun2, TarimDonusum.Araclar.LineStyle stil, TabloRenk renk)
        {
            YatayCizgiCiz(satir, sutun1, sutun2, stil, renk, true);
        }

        public void CerceveCizgiCiz(int satir1, int satir2, int sutun1, int sutun2, TarimDonusum.Araclar.LineStyle stil, TabloRenk renk)
        {
            for (int i = satir1; i <= satir2 + 1; i++)
                YatayCizgiCiz(i, sutun1, sutun2, stil, renk, true);

            for (int i = sutun1; i <= sutun2 + 1; i++)
                DuseyCizgiCiz(satir1, satir2, i, stil, renk, true);
        }

        public void CerceveCiz(int satir1, int satir2, int sutun1, int sutun2, TarimDonusum.Araclar.LineStyle stil, TabloRenk renk)
        {
            DuseyCizgiCiz(satir1, satir2 - 1, sutun1, stil, renk, true);
            DuseyCizgiCiz(satir1, satir2 - 1, sutun2 - 1, stil, renk, false);
            YatayCizgiCiz(satir1, sutun1, sutun2 - 1, stil, renk, true);
            YatayCizgiCiz(satir2 - 1, sutun1, sutun2 - 1, stil, renk, false);
        }

        public void ZoomSheet(int zf)
        {
            Worksheet sheet = XLS.Worksheets[aktifSheet];
            sheet.Zoom = zf;
        }

        public void ZoomYazici(int zf)
        {
            Worksheet sheet = XLS.Worksheets[aktifSheet];
            sheet.PageSetup.Zoom = zf;
        }

        public void HucreMetniKaydir(int satir1, int sutun1, int satir2, int sutun2, bool deger)
        {
            Aspose.Cells.Style style = XLS.Styles[XLS.Styles.Add()];
            if (deger)
                style.IsTextWrapped = true;
            else
                style.IsTextWrapped = false;

            Worksheet sheet = XLS.Worksheets[aktifSheet];
            Aspose.Cells.Range r = sheet.Cells.CreateRange(satir1, sutun1, satir2 - satir1 + 1, sutun2 - sutun1 + 1);

            StyleFlag sf = new StyleFlag();
            sf.WrapText = true;
            r.ApplyStyle(style, sf);
        }

        public void HucreMetniKaydir(int satir, int sutun, bool deger)
        {
            HucreMetniKaydir(satir, sutun, satir, sutun, deger);
        }

        public void HucreMetniSigdir(int satir1, int sutun1, int satir2, int sutun2, bool deger)
        {
            Aspose.Cells.Style style = XLS.Styles[XLS.Styles.Add()];
            if (deger)
                style.ShrinkToFit = true;
            else
                style.ShrinkToFit = false;

            Worksheet sheet = XLS.Worksheets[aktifSheet];
            Aspose.Cells.Range r = sheet.Cells.CreateRange(satir1, sutun1, satir2 - satir1 + 1, sutun2 - sutun1 + 1);

            StyleFlag sf = new StyleFlag();
            sf.ShrinkToFit = true;
            r.ApplyStyle(style, sf);
        }

        public void HucreMetniSigdir(int satir, int sutun, bool deger)
        {
            HucreMetniSigdir(satir, sutun, satir, sutun, deger);
        }

        public void KoyuYap(int satir1, int sutun1, int satir2, int sutun2, bool deger)
        {
            Aspose.Cells.Style style = XLS.Styles[XLS.Styles.Add()];
            Font f = style.Font;
            if (deger)
                f.IsBold = true;
            else
                f.IsBold = false;

            Worksheet sheet = XLS.Worksheets[aktifSheet];
            Aspose.Cells.Range r = sheet.Cells.CreateRange(satir1, sutun1, satir2 - satir1 + 1, sutun2 - sutun1 + 1);

            StyleFlag sf = new StyleFlag();
            sf.FontBold = true;
            r.ApplyStyle(style, sf);
        }

        public void KoyuYap(int satir, int sutun, bool deger)
        {
            KoyuYap(satir, sutun, satir, sutun, deger);
        }

        public void YaziTipiAta(int satir1, int sutun1, int satir2, int sutun2, string fontAd)
        {
            Aspose.Cells.Style style = XLS.Styles[XLS.Styles.Add()];
            Font f = style.Font;
            f.Name = fontAd;

            Worksheet sheet = XLS.Worksheets[aktifSheet];
            Aspose.Cells.Range r = sheet.Cells.CreateRange(satir1, sutun1, satir2 - satir1 + 1, sutun2 - sutun1 + 1);

            StyleFlag sf = new StyleFlag();
            sf.Font = true;
            r.ApplyStyle(style, sf);
        }

        public void YaziTipBuyuklugu(int satir, int sutun, int deger)
        {
            YaziTipBuyuklugu(satir, sutun, satir, sutun, deger);
        }

        public void YaziTipBuyuklugu(int satir1, int sutun1, int satir2, int sutun2, int deger)
        {
            Aspose.Cells.Style style = XLS.Styles[XLS.Styles.Add()];
            Font f = style.Font;
            f.Size = deger;

            Worksheet sheet = XLS.Worksheets[aktifSheet];
            Aspose.Cells.Range r = sheet.Cells.CreateRange(satir1, sutun1, satir2 - satir1 + 1, sutun2 - sutun1 + 1);

            StyleFlag sf = new StyleFlag();
            sf.FontSize = true;
            r.ApplyStyle(style, sf);
        }

        public void YatayHizala(int satir, int sutun, int deger)
        {
            YatayHizala(satir, sutun, satir, sutun, deger);
        }

        public void DuseyHizala(int satir1, int sutun1, int satir2, int sutun2, int deger)
        {
            //0-sol
            //1-sağ
            //2-orta
            //3-iki yana yasla

            Aspose.Cells.Style style = XLS.Styles[XLS.Styles.Add()];

            if (deger == 0)
                style.HorizontalAlignment = TextAlignmentType.Left;
            else if (deger == 1)
                style.HorizontalAlignment = TextAlignmentType.Right;
            else if (deger == 2)
                style.HorizontalAlignment = TextAlignmentType.Center;
            else
                style.HorizontalAlignment = TextAlignmentType.Distributed;

            Worksheet sheet = XLS.Worksheets[aktifSheet];
            Aspose.Cells.Range r = sheet.Cells.CreateRange(satir1, sutun1, satir2 - satir1 + 1, sutun2 - sutun1 + 1);

            StyleFlag sf = new StyleFlag();
            sf.HorizontalAlignment = true;
            r.ApplyStyle(style, sf);
        }

        public void DuseyHizala(int satir, int sutun, int deger)
        {
            DuseyHizala(satir, sutun, satir, sutun, deger);
        }

        public void YatayHizala(int satir1, int sutun1, int satir2, int sutun2, int deger)
        {
            //0-alt
            //1-üst
            //2-orta
            //3-iki yana yasla
            Aspose.Cells.Style style = XLS.Styles[XLS.Styles.Add()];

            if (deger == 0)
                style.VerticalAlignment = TextAlignmentType.Bottom;
            else if (deger == 1)
                style.VerticalAlignment = TextAlignmentType.Top;
            else if (deger == 2)
                style.VerticalAlignment = TextAlignmentType.Center;
            else
                style.VerticalAlignment = TextAlignmentType.Distributed;

            Worksheet sheet = XLS.Worksheets[aktifSheet];
            Aspose.Cells.Range r = sheet.Cells.CreateRange(satir1, sutun1, satir2 - satir1 + 1, sutun2 - sutun1 + 1);

            StyleFlag sf = new StyleFlag();
            sf.VerticalAlignment = true;
            r.ApplyStyle(style, sf);
        }

        public void ArkaPlanRenk(int satir1, int sutun1, int satir2, int sutun2, System.Drawing.Color renk)
        {
            //0-alt
            //1-üst
            //2-orta
            //3-iki yana yasla
            Aspose.Cells.Style style = XLS.Styles[XLS.Styles.Add()];

            style.ForegroundColor = renk;
            style.Pattern = BackgroundType.Solid;

            Worksheet sheet = XLS.Worksheets[aktifSheet];
            Aspose.Cells.Range r = sheet.Cells.CreateRange(satir1, sutun1, satir2 - satir1 + 1, sutun2 - sutun1 + 1);

            StyleFlag sf = new StyleFlag();
            sf.CellShading = true;
            r.ApplyStyle(style, sf);
        }

        public void ArkaPlanRenk(int satir, int sutun, System.Drawing.Color renk)
        {
            ArkaPlanRenk(satir, sutun, satir, sutun, renk);
        }

        public void ArkaPlanRenk(int satir1, int sutun1, int satir2, int sutun2, TabloRenk renk)
        {
            ArkaPlanRenk(satir1, sutun1, satir2, sutun2, RenkBul(renk));
        }

        public void ArkaPlanRenk(int satir, int sutun, TabloRenk renk)
        {
            ArkaPlanRenk(satir, sutun, satir, sutun, renk);
        }

        public void YaziRenk(int satir1, int sutun1, int satir2, int sutun2, TabloRenk renk)
        {
            //0-alt
            //1-üst
            //2-orta
            //3-iki yana yasla
            Aspose.Cells.Style style = XLS.Styles[XLS.Styles.Add()];

            style.Font.Color = RenkBul(renk);
            Worksheet sheet = XLS.Worksheets[aktifSheet];
            Aspose.Cells.Range r = sheet.Cells.CreateRange(satir1, sutun1, satir2 - satir1 + 1, sutun2 - sutun1 + 1);

            StyleFlag sf = new StyleFlag();
            sf.FontColor = true;
            r.ApplyStyle(style, sf);
        }

        public void YaziRenk(int satir, int sutun, TabloRenk renk)
        {
            YaziRenk(satir, sutun, satir, sutun, renk);
        }

        public void YaziRenk(int satir1, int sutun1, int satir2, int sutun2, System.Drawing.Color renk)
        {
            //0-alt
            //1-üst
            //2-orta
            //3-iki yana yasla
            Aspose.Cells.Style style = XLS.Styles[XLS.Styles.Add()];

            style.Font.Color = renk;
            style.Pattern = BackgroundType.Solid;

            Worksheet sheet = XLS.Worksheets[aktifSheet];
            Aspose.Cells.Range r = sheet.Cells.CreateRange(satir1, sutun1, satir2 - satir1 + 1, sutun2 - sutun1 + 1);

            StyleFlag sf = new StyleFlag();
            sf.FontColor = true;
            r.ApplyStyle(style, sf);
        }

        public void YaziRenk(int satir, int sutun, System.Drawing.Color renk)
        {
            YaziRenk(satir, sutun, satir, sutun, renk);
        }

        public void CokluSatirdaYaz(int satir, int sutun, bool deger)
        {
            HucreMetniKaydir(satir, sutun, deger);
        }

        public void AktifSheetDegistir(int sheetNo)
        {
            aktifSheet = sheetNo;
            XLS.Worksheets.ActiveSheetIndex = aktifSheet;
        }

        public int AktifSheet()
        {
            return aktifSheet;
        }

        public int YeniSheetEkle()
        {
            int tut = aktifSheet;
            aktifSheet = XLS.Worksheets.Count;
            XLS.Worksheets.ActiveSheetIndex = aktifSheet;
            XLS.Worksheets.AddCopy(aktifSheet);
            return tut;
        }

        public int SheetSayisi()
        {
            return XLS.Worksheets.Count;
        }

        public int YeniSheetEkle(int kaynakSheetNo)
        {
            int tut = aktifSheet;
            aktifSheet = XLS.Worksheets.Count;
            XLS.Worksheets.ActiveSheetIndex = aktifSheet;
            XLS.Worksheets.AddCopy(kaynakSheetNo);
            return tut;
        }

        public void SheetSil(int sheetNo)
        {
            XLS.Worksheets.RemoveAt(sheetNo);
        }

        public void SheetAdiVer(int sheetNo, string adi)
        {
            XLS.Worksheets[sheetNo].Name = adi;
        }

        public string SheetAdiAl()
        {
            return SheetAdiAl(AktifSheet());
        }

        public string SheetAdiAl(int sheetNo)
        {
            return XLS.Worksheets[sheetNo].Name;
        }

        public void SheetBilgileriKopyalax(int sheetNo, int hedefSheet)
        {
            Worksheet sheetSrc = XLS.Worksheets[sheetNo];
            Worksheet sheetDst = XLS.Worksheets[hedefSheet];

            sheetDst.Copy(sheetSrc);
        }

        public void SayfaSonuKoy(int satir)
        {
            Worksheet sheet = XLS.Worksheets[aktifSheet];
            sheet.HorizontalPageBreaks.Add(satir);
        }

        public void SayfaSonuKoyHucresel(int satir)
        {
            Worksheet sheet = XLS.Worksheets[aktifSheet];
            sheet.HorizontalPageBreaks.Add("A" + satir);
        }

        public void SayfaSonuKoySutun(int sutun)
        {
            Worksheet sheet = XLS.Worksheets[aktifSheet];
            sheet.VerticalPageBreaks.Add(sutun);
        }

        //public void SayfaSonuKoySutunHucresel(int satir)
        //{
        //    Worksheet sheet = XLS.Worksheets[aktifSheet];
        //    sheet.HorizontalPageBreaks.Add("A" + satir);
        //}

        public void AdTanimla(string ad, int sheetNo, int sol, int ust, int sag, int alt)
        {
            Worksheet sheet = XLS.Worksheets[sheetNo];
            Aspose.Cells.Range r = sheet.Cells.CreateRange(ust, sol, ust - alt + 1, sag - sol + 1);
            r.Name = ad;
        }

        public void IlkSayfaNumarasi(int sayfaNo)
        {
            Worksheet sheet = XLS.Worksheets[aktifSheet];
            sheet.PageSetup.FirstPageNumber = sayfaNo;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="yer">1 ise alt, değil ise üst</param>
        /// <param name="yanasik">1 ise sol, 2 ise sağ, değil ise orta</param>
        /// <param name="deger"></param>
        public void HFDegerAta(int yer, int yanasik, string deger)
        {
            Worksheet sheet = XLS.Worksheets[aktifSheet];
            int sect = 0;
            if (yanasik == 1) sect = 0;
            else if (yanasik == 2) sect = 2;
            else sect = 1;

            if (yer == 1)
                sheet.PageSetup.SetFooter(sect, deger);
            else
                sheet.PageSetup.SetHeader(sect, deger);
        }

        public void SelectSheet(int sheetNo)
        {
            Worksheet sheet = XLS.Worksheets[sheetNo];
            sheet.IsSelected = true;
            XLS.Settings.FirstVisibleTab = sheetNo;
        }

        public void SelectSheet()
        {
            SelectSheet(aktifSheet);
        }

        public void ResimEkle(double x, double y, double width, double height, string dosya)
        {
            ResimEkle(x, y, width, height, new System.IO.FileStream(dosya, System.IO.FileMode.Open, System.IO.FileAccess.Read), 0, 0, 2, 2);
        }

        public void ResimEkle(double x, double y, double width, double height, Stream stream)
        {
            ResimEkle(x, y, width, height, stream, 0, 0, 2, 2);
        }

        public object ResimEkle(double x, double y, double width, double height, Stream stream, double left, double top, int enBoyOran, int yanasiklik)
        {
            Worksheet sheet = XLS.Worksheets[aktifSheet];

            int pictureIndex = sheet.Pictures.Add((int)x, (int)y, stream);

            //Accessing the newly added picture
            Aspose.Cells.Drawing.Picture picture = sheet.Pictures[pictureIndex];

            double aspRatioW = width / picture.WidthCM;
            double aspRatioH = height / picture.HeightCM;
            double aspRatio = Math.Min(aspRatioW, aspRatioH);

            if (enBoyOran == 2)
            {
                height = picture.HeightCM * aspRatio;
                width = picture.WidthCM * aspRatio;
            }

            if (enBoyOran != 0)
            {
                picture.HeightCM = height;
                picture.WidthCM = width;
            }

            Cells cs = sheet.Cells;
            if (yanasiklik == 1 || yanasiklik == 3)
            {
                double colWidth = cs.GetColumnWidthInch((int)y) * 2.54;
                left = (colWidth - picture.WidthCM) / 2;
            }
            if (yanasiklik == 2 || yanasiklik == 3)
            {
                double rowHeight = cs.GetRowHeightInch((int)x) * 2.54;
                top = (rowHeight - picture.HeightCM) / 2;
            }
            picture.LeftCM = left;
            picture.TopCM = top;
            return picture;
        }
        ///////////////////////////// DOSYA İŞLEMLERİ
        ///

        public void BosDosyaAc(string sonucDosya)
        {
            tmpFile = sonucDosya;
            XLS = new Workbook();
            //XLS.Worksheets.Add();
            aktifSheet = 0;
            XLS.Settings.Encoding = System.Text.Encoding.Default; //.GetEncoding(encoding);
        }

        public void DosyaOkuAc(string dosyaAd)
        {
            DosyaAcGenel(dosyaAd, null, DosyaIslem.DosyaAdUret(), true);
        }

        public void DosyaOkuAc(System.IO.Stream dosyaStream)
        {
            DosyaAcGenel(null, dosyaStream, DosyaIslem.DosyaAdUret(), true);
        }

        public void DosyaAc(string dosyaAd)
        {
            DosyaAcGenel(dosyaAd, null, DosyaIslem.DosyaAdUret(), false);
        }

        public void DosyaAc(string dosyaAd, string sonucDosya)
        {
            DosyaAcGenel(dosyaAd, null, sonucDosya, false);
        }

        private void DosyaAcGenel(string dosyaAd, Stream dosyaStream, string sonucDosya, bool okuModuMu)
        {
            okuModu = okuModuMu;
            if (dosyaStream != null)
                XLS = new Workbook(dosyaStream);
            else
                XLS = new Workbook(dosyaAd);
            tmpFile = sonucDosya;
            //jxl.WorkbookSettings wbs = new WorkbookSettings();
            //wbs.setFormulaAdjust(false);
            //wbs.setExcelRegionalSettings("tr-TR");
            //wbs.setEncoding(encoding);
            //Workbook inputWorkBook = jxl.Workbook.getWorkbook(new java.io.File(dosyaAd)); //Xls dosyasını açma/workbooku oluşturma

            //System.IO.FileStream f = System.IO.File.Open(dosyaAd, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.ReadWrite);
            //Workbook inputWorkBook = jxl.Workbook.getWorkbook(f, wbs); //Xls dosyasını açma/workbooku oluşturma
            //f.Close();

            //tmpFileStream = new java.io.FileOutputStream(tmpFile);
            //tmpFileStream = System.IO.File.Create(tmpFile);
            //XLS = Workbook.createWorkbook(tmpFileStream, inputWorkBook, wbs);
        }

        public void DosyaSaklaTamYol()
        {
            if (dosyaSaklamaFormat == "html" || dosyaSaklamaFormat == "pdf")
            {
                XLS.CalculateFormula(true);
                if (dosyaSaklamaFormat == "html")
                    XLS.Save(tmpFile, SaveFormat.Html);    // Html
                else
                    XLS.Save(tmpFile, SaveFormat.Pdf);     // Pdf
            }
            else if (dosyaSaklamaFormat == "xlsm")
                XLS.Save(tmpFile, SaveFormat.Xlsm);
            else
                XLS.Save(tmpFile, SaveFormat.Xlsx);    // Geri kalanı Xlsx
        }

        public void DosyaKapat()
        {
        }

        public string UzantiBul()
        {
            return "xlsx";
        }
        //public void DosyaGonder(string dosyaAd)
        //{
        //    DosyaSaklaTamYol();
        //    DosyaIslem.DosyaGonder(tmpFile, dosyaAd, true, "xls");
        //}

        //public void DosyaGonder(string dosyaAd, string dosyaTuru)
        //{
        //    DosyaSaklaTamYol();
        //    DosyaIslem.DosyaGonder(tmpFile, dosyaAd, true, dosyaTuru);
        //}

        //public static void DosyaGonder(string dosyaAd, string gidenAd, bool dosyaSil, string dosyaTuru)
        //{
        //    string ext = System.IO.Path.GetExtension(dosyaAd).Replace(".", "");
        //    dosyaTuru = dosyaTuru.Replace(".", "");
        //    if (ext == "tmp") ext = "xls";
        //    if (dosyaTuru == null)
        //        dosyaTuru = "XLS";
        //    if (dosyaTuru.ToUpper() == "PDF")
        //        DosyaGonderPDF(dosyaAd, gidenAd, dosyaSil, true);
        //    else if (dosyaTuru.ToUpper() == "DOC")
        //        DosyaGonderX(dosyaAd, gidenAd, dosyaSil, "doc");
        //    else if (dosyaTuru.ToUpper() == "ZIP" || dosyaTuru.ToLower() == "zip")
        //        DosyaGonderX(dosyaAd, gidenAd, dosyaSil, "zip");
        //    else
        //        DosyaGonderX(dosyaAd, gidenAd, dosyaSil, "xls");
        //}

        //public static void DosyaGonderX(string dosyaAd, string gidenAd, bool dosyaSil, string ext)
        //{
        //    DosyaGonderX(dosyaAd, gidenAd, dosyaSil, ext, true);
        //}

        //public static void DosyaGonderX(string dosyaAd, string gidenAd, bool dosyaSil, string ext, bool ekOlarakGonder)
        //{
        //    OrtakClass.GenelIslemler.ResponseBasla(gidenAd, ext, ekOlarakGonder);

        //    //System.Web.HttpContext.Current.Response.WriteFile(dosyaAd);
        //    System.Web.HttpContext.Current.Response.TransmitFile(dosyaAd);
        //    System.Web.HttpContext.Current.Response.Flush();
        //    if (dosyaSil)
        //        System.IO.File.Delete(dosyaAd);

        //    System.Web.HttpContext.Current.Response.End();
        //}

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

        //public static string DosyaAdUret()
        //{
        //    return DosyaAdUret("", "");
        //}

        //public static string DosyaAdUret(string dosyaAdOnEk, string dosyaAdArkaEk)
        //{
        //    return System.IO.Path.GetTempPath() + dosyaAdOnEk + DosyaAdUretSade() + dosyaAdArkaEk + ".xls";
        //}

        //public static string DosyaAdUretSade()
        //{
        //    string dosyaAd = System.DateTime.Now.ToString() + System.DateTime.Now.Millisecond.ToString();
        //    dosyaAd = dosyaAd.Replace("/", "");
        //    dosyaAd = dosyaAd.Replace(".", "");
        //    dosyaAd = dosyaAd.Replace("-", "");
        //    dosyaAd = dosyaAd.Replace(":", "");
        //    dosyaAd = dosyaAd.Replace(" ", "");
        //    return dosyaAd;
        //}

        public void YazdirmaYineleme(int sheetNo, string yineleSatir, string yineleSutun)
        {
            PageSetup ps = XLS.Worksheets[0].PageSetup;
            if (!string.IsNullOrWhiteSpace(yineleSatir))
                ps.PrintTitleRows = yineleSatir;
            if (!string.IsNullOrWhiteSpace(yineleSutun))
                ps.PrintTitleColumns = yineleSutun;
        }

        public void DosyaSaklamaFormatAta(string uzanti)
        {
            dosyaSaklamaFormat = uzanti.ToLower();
        }

        public void CalculateFormula()
        {
            XLS.CalculateFormula(true);
        }

        public void HucreAdAdresOl(string bolgeAd, int sheetNo, int satir1, int sutun1, int satir2, int sutun2)
        {
            try
            {
                Aspose.Cells.Range r = XLS.Worksheets[sheetNo].Cells.CreateRange(satir1, sutun1, satir2 - satir1 + 1, sutun2 - sutun1 + 1);
                r.Name = bolgeAd;

            }
            catch (Exception)
            {
            }
        }

        public string CellIndexToName(int row, int column)
        {
            return Aspose.Cells.CellsHelper.CellIndexToName(row, column);
        }

        public void CellNameToIndex(string name, out int row, out int column)
        {
            Aspose.Cells.CellsHelper.CellNameToIndex(name, out row, out column);
        }

        public void SayfaYonuAta(SayfaYonu sayfaYonu)
        {
            PageOrientationType pt;
            if (sayfaYonu == SayfaYonu.DUSEY)
                pt = PageOrientationType.Portrait;
            else
                pt = PageOrientationType.Landscape;
            XLS.Worksheets[aktifSheet].PageSetup.Orientation = pt;
        }

        public void TekrarlanacakSatirlar(int satir1, int satir2)
        {
            XLS.Worksheets[aktifSheet].PageSetup.PrintTitleRows = "$" + (satir1 + 1).ToString() + ":$" + (satir2 + 1).ToString();
        }

        public void TekrarlanacakSutunlar(int sutun1, int sutun2)
        {
            XLS.Worksheets[aktifSheet].PageSetup.PrintTitleColumns = "$" + Aspose.Cells.CellsHelper.ColumnIndexToName(sutun1 + 1) + ":$" + Aspose.Cells.CellsHelper.ColumnIndexToName(sutun2 + 1);
        }

        public void AltaltaSayfaSayisi(int sayfaSayisi)
        {
            XLS.Worksheets[aktifSheet].PageSetup.FitToPagesTall = sayfaSayisi;
        }

        public void YanyanaSayfaSayisi(int sayfaSayisi)
        {
            XLS.Worksheets[aktifSheet].PageSetup.FitToPagesWide = sayfaSayisi;
        }

        public void FormulleriSil()
        {
            XLS.CalculateFormula(true);
            XLS.Worksheets[aktifSheet].Cells.RemoveFormulas();
        }

        public void Sirala(int order1, int key1, int satir1, int sutun1, int satir2, int sutun2)
        {
            DataSorter sorter = XLS.DataSorter;

            if (order1 == 0)
                sorter.Order1 = Aspose.Cells.SortOrder.Ascending;
            else
                sorter.Order1 = Aspose.Cells.SortOrder.Descending;

            // Define the first key.
            sorter.Key1 = key1;

            // Create a cells area (range).
            CellArea ca = new CellArea();

            // Specify the start row index.
            ca.StartRow = satir1;

            // Specify the start column index.
            ca.StartColumn = sutun1;

            // Specify the last row index.
            ca.EndRow = satir2;

            // Specify the last column index.
            ca.EndColumn = sutun2;

            // Sort data in the specified data range (A1:B14)
            sorter.Sort(XLS.Worksheets[0].Cells, ca);
        }
    }
}
