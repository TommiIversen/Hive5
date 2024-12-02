using System.Buffers;
using SkiaSharp;


namespace Engine.Utils;


public class ImageGenerator
{
    private const int Width = 300;
    private const int Height = 168;
    private const int BufferSize = 32 * 1024;
    private readonly ArrayPool<byte> _arrayPool;
    private readonly SKColor _baseEndColor;
    private readonly SKColor _baseStartColor;

    public ImageGenerator()
    {
        _arrayPool = ArrayPool<byte>.Shared;

        // Generér tilfældige grundfarver
        var random = new Random();
        _baseStartColor = new SKColor(
            (byte)random.Next(256),
            (byte)random.Next(256),
            (byte)random.Next(256));
        _baseEndColor = new SKColor(
            (byte)random.Next(256),
            (byte)random.Next(256),
            (byte)random.Next(256));
    }

    public byte[] GenerateImageWithNumber(int number, string extraText = "")
    {
        // Beregn farver baseret på nummeret og tilføj grundfarve
        var r1 = (_baseStartColor.Red + number % 255) % 255;
        var g1 = (_baseStartColor.Green + number * 2 % 255) % 255;
        var b1 = (_baseStartColor.Blue + number * 3 % 255) % 255;

        var r2 = (_baseEndColor.Red + (number + 100) % 255) % 255;
        var g2 = (_baseEndColor.Green + (number + 150) % 255) % 255;
        var b2 = (_baseEndColor.Blue + (number + 200) % 255) % 255;

        using var surface = SKSurface.Create(new SKImageInfo(Width, Height));
        var canvas = surface.Canvas;

        // Tegn gradient baggrund
        using (var paint = new SKPaint())
        {
            paint.Shader = SKShader.CreateLinearGradient(
                new SKPoint(0, 0),
                new SKPoint(Width, 0),
                new[] { new SKColor((byte)r1, (byte)g1, (byte)b1), new SKColor((byte)r2, (byte)g2, (byte)b2) },
                null,
                SKShaderTileMode.Clamp);
            canvas.DrawRect(new SKRect(0, 0, Width, Height), paint);
        }

        // Tegn tallet i midten
        using var font = new SKFont(SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold), 40);
        using (var paint = new SKPaint())
        {
            paint.Color = SKColors.Black;
            paint.IsAntialias = true;
            canvas.DrawText(number.ToString(), Width / 2.0f, Height / 3.0f, SKTextAlign.Center, font, paint);

            // Hvis der er ekstra tekst, tegn den under tallet
            if (!string.IsNullOrEmpty(extraText))
            {
                using var smallerFont = new SKFont(SKTypeface.FromFamilyName("Arial"), 20);
                canvas.DrawText(extraText, Width / 2.0f, 2 * Height / 3.0f, SKTextAlign.Center, smallerFont, paint);
            }
        }

        canvas.Flush();

        // Konverter til bytearray
        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Jpeg, 80); // Komprimeringskvalitet
        var buffer = _arrayPool.Rent(BufferSize);

        try
        {
            using var memoryStream = new MemoryStream(buffer);
            data.SaveTo(memoryStream);

            var lengthUsed = (int)memoryStream.Position;
            var result = new byte[lengthUsed];
            Array.Copy(buffer, result, lengthUsed);

            return result;
        }
        finally
        {
            _arrayPool.Return(buffer);
        }
    }
}

