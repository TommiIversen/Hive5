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
    private const int Height = 220;
    private readonly Font _font;
    private readonly Brush _brush;
    private readonly StringFormat _format;
    private readonly ArrayPool<byte> _arrayPool;
    private const int BufferSize = 32 * 1024;

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
    }

        public byte[] GenerateImageWithNumber(int number, string extraText = "")
    {
        // Hvis vi ikke er på Windows, returner en fake bytearray
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return GenerateFakeImage();
        }

        // Beregn farver baseret på nummeret ved hjælp af modulus 255
        int r1 = number % 255;
        int g1 = (number * 2) % 255;
        int b1 = (number * 3) % 255;

        int r2 = (number + 100) % 255;
        int g2 = (number + 150) % 255;
        int b2 = (number + 200) % 255;

        using var bitmap = new Bitmap(Width, Height);
        using (var graphics = Graphics.FromImage(bitmap))
        {
            // Opret en gradient baggrund (fra blå til hvid)
            using (var gradientBrush = new LinearGradientBrush(
                       new Rectangle(0, 0, Width, Height),
                       Color.FromArgb(r1, g1, b1), // Startfarve
                       Color.FromArgb(r2, g2, b2), // Slutfarve
                       LinearGradientMode.Horizontal))
            {
                // Fyld baggrunden med gradienten
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
        byte[] buffer = _arrayPool.Rent(BufferSize);

        try
        {
            using MemoryStream memoryStream = new MemoryStream(buffer);
            bitmap.Save(memoryStream, ImageFormat.Jpeg);

            int lengthUsed = (int)memoryStream.Position;
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