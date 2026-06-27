using static System.Net.Mime.MediaTypeNames;
using System.Drawing;
using SkiaSharp;

namespace TarimDonusum.FrameWork.Captcha
{
    public class CaptchaGenerator
    {
        public const string SessionKey = "guvenlikKodu";

        private static char KarakterBul(int charNo)
        {
            if (charNo < 9)
                return (char)('1' + charNo);

            return (char)('A' + (charNo - 9));
        }

        public byte[] Create(HttpContext httpContext)
        {
            int width = 150;
            int height = 50;

            using var bitmap = new SKBitmap(width, height);
            using var canvas = new SKCanvas(bitmap);
            canvas.Clear(SKColors.Beige);

            var rnd = Random.Shared;

            using var typeface = SKTypeface.FromFamilyName(
                "Arial",
                SKFontStyleWeight.Bold,
                SKFontStyleWidth.Normal,
                SKFontStyleSlant.Upright);

            using var font = new SKFont(typeface, 24);

            using var paint = new SKPaint
            {
                IsAntialias = true,
                Color = SKColors.Black
            };

            string guvenlikKodu = "";

            for (int i = 0; i < 4; i++)
            {
                char karakter = KarakterBul(rnd.Next(0, 35));
                guvenlikKodu += karakter;

                float x = 18 + i * 32;
                float y = 34 + rnd.Next(-5, 5);

                canvas.Save();
                canvas.Translate(x, y);
                canvas.RotateDegrees(rnd.Next(-25, 25));
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

            return data.ToArray();
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