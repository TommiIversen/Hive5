using System.Buffers;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Engine.Utils;

public class ImageGenerator
{
    private const int Width = 300;
    private const int Height = 168;
    private const int BufferSize = 32 * 1024;
    private readonly ArrayPool<byte> _arrayPool;
    private readonly Color _baseEndColor;
    private readonly Color _baseStartColor;
    private readonly Brush _brush;
    private readonly Color _endColor;
    private readonly Font _font;
    private readonly StringFormat _format;
    private readonly Color _startColor;

    [SupportedOSPlatform("windows")]
    public ImageGenerator()
    {
        // Initialisér skrifttype, pensel og format én gang i konstruktøren
        _font = new Font("Arial", 40, FontStyle.Bold);
        _brush = new SolidBrush(Color.Black);
        _format = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center
        };
        _arrayPool = ArrayPool<byte>.Shared; // Genbrug array pool

        // Generér tilfældige grundfarver
        var random = new Random();
        _baseStartColor = Color.FromArgb(random.Next(256), random.Next(256), random.Next(256));
        _baseEndColor = Color.FromArgb(random.Next(256), random.Next(256), random.Next(256));
    }

    public byte[] GenerateImageWithNumber(int number, string extraText = "")
    {
        // Hvis vi ikke er på Windows, returner en fake bytearray
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return GenerateFakeImage();

        // Beregn farver baseret på nummeret og tilføj grundfarve
        var r1 = (_baseStartColor.R + number % 255) % 255;
        var g1 = (_baseStartColor.G + number * 2 % 255) % 255;
        var b1 = (_baseStartColor.B + number * 3 % 255) % 255;

        var r2 = (_baseEndColor.R + (number + 100) % 255) % 255;
        var g2 = (_baseEndColor.G + (number + 150) % 255) % 255;
        var b2 = (_baseEndColor.B + (number + 200) % 255) % 255;

        using var bitmap = new Bitmap(Width, Height);
        using (var graphics = Graphics.FromImage(bitmap))
        {
            // Opret en gradient baggrund med farver baseret på input
            using (var gradientBrush = new LinearGradientBrush(
                       new Rectangle(0, 0, Width, Height),
                       Color.FromArgb(r1, g1, b1), // Startfarve
                       Color.FromArgb(r2, g2, b2), // Slutfarve
                       LinearGradientMode.Horizontal))
            {
                graphics.FillRectangle(gradientBrush, 0, 0, Width, Height);
            }

            // Tegn tallet i midten af billedet
            graphics.DrawString(number.ToString(), _font, _brush, new RectangleF(0, 0, Width, Height / 2.0f), _format);

            // Hvis der er ekstra tekst, tegn den under tallet
            if (!string.IsNullOrEmpty(extraText))
            {
                var smallerFont = new Font("Arial", 20, FontStyle.Regular);
                var textRectangle = new RectangleF(0, Height / 2.0f, Width, Height / 2.0f);
                graphics.DrawString(extraText, smallerFont, _brush, textRectangle, _format);
                smallerFont.Dispose();
            }
        }

        // Brug en buffer fra poolen til at undgå gentagne allokeringer
        var buffer = _arrayPool.Rent(BufferSize);
        try
        {
            using var memoryStream = new MemoryStream(buffer);
            bitmap.Save(memoryStream, ImageFormat.Jpeg);

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

    private byte[] GenerateFakeImage()
    {
        // Returner en simpel fake bytearray som placeholder på ikke-Windows platforme
        return new byte[] { 0, 0, 0 };
    }
}