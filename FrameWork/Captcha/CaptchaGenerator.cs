using static System.Net.Mime.MediaTypeNames;
using System.Drawing;
using SkiaSharp;

namespace TarimDonusum.FrameWork.Captcha
{
    public class CaptchaGenerator
    {
        public const string SessionKey = "guvenlikKodu";
        private const string SessionImageKey = "guvenlikKoduResim";

        private static readonly char[] Karakterler = "23456789ABCDEFGHJKLMNPQRSTUVWXYZ".ToCharArray();

        private static char KarakterBul(int charNo)
        {
            return Karakterler[charNo % Karakterler.Length];
        }

        public byte[] Create(HttpContext httpContext)
        {
            byte[]? mevcutResim = httpContext.Session.Get(SessionImageKey);
            if (mevcutResim != null && mevcutResim.Length > 0)
                return mevcutResim;

            int width = 180;
            int height = 58;

            using var bitmap = new SKBitmap(width, height);
            using var canvas = new SKCanvas(bitmap);
            canvas.Clear(SKColors.White);

            var rnd = Random.Shared;

            using var typeface = SKTypeface.FromFamilyName(
                "Arial",
                SKFontStyleWeight.Bold,
                SKFontStyleWidth.Normal,
                SKFontStyleSlant.Upright);

            using var font = new SKFont(typeface, 30);

            using var paint = new SKPaint
            {
                IsAntialias = true,
                Color = SKColors.Black
            };

            string guvenlikKodu = "";

            for (int i = 0; i < 4; i++)
            {
                char karakter = KarakterBul(rnd.Next(0, Karakterler.Length));
                guvenlikKodu += karakter;

                float x = 22 + i * 38;
                float y = 40 + rnd.Next(-4, 4);

                canvas.Save();
                canvas.Translate(x, y);
                canvas.RotateDegrees(rnd.Next(-14, 14));
                canvas.DrawText(karakter.ToString(), 0f, 0f, SKTextAlign.Left, font, paint);
                canvas.Restore();
            }

            httpContext.Session.SetString(SessionKey, guvenlikKodu);

            using var noisePaint = new SKPaint { IsAntialias = true };

            for (int i = 0; i < 12; i++)
            {
                noisePaint.Color = new SKColor(
                    (byte)rnd.Next(50, 200),
                    (byte)rnd.Next(50, 200),
                    (byte)rnd.Next(50, 200),
                    90);

                canvas.DrawCircle(
                    rnd.Next(0, width),
                    rnd.Next(0, height),
                rnd.Next(2, 6),
                    noisePaint);
            }

            using var image = SKImage.FromBitmap(bitmap);
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);

            byte[] resim = data.ToArray();
            httpContext.Session.Set(SessionImageKey, resim);

            return resim;
        }

        public void Yenile(HttpContext httpContext)
        {
            httpContext.Session.Remove(SessionKey);
            httpContext.Session.Remove(SessionImageKey);
        }

        public bool Validate(HttpContext httpContext, string? girilenKod)
        {
            var sessionKod = httpContext.Session.GetString(SessionKey);

            if (string.IsNullOrWhiteSpace(sessionKod))
                return false;

            return string.Equals(
                sessionKod,
                girilenKod?.Trim(),
                StringComparison.OrdinalIgnoreCase);
        }
    }
}
