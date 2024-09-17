using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace Engine.Utils;

public class ImageGenerator
{
    public byte[] GenerateImageWithNumber(int number)
    {
        // Definer størrelse på billedet
        const int width = 300;
        const int height = 220;
        
        // Beregn farver baseret på nummeret ved hjælp af modulus 255
        int r1 = number % 255;
        int g1 = (number * 2) % 255;
        int b1 = (number * 3) % 255;

        int r2 = (number + 100) % 255;
        int g2 = (number + 150) % 255;
        int b2 = (number + 200) % 255;

        // Opret et nyt bitmap billede
        using (Bitmap bitmap = new Bitmap(width, height))
        {
            // Opret en grafisk kontekst
            using (Graphics graphics = Graphics.FromImage(bitmap))
            {
                // Opret en gradient baggrund (fra blå til hvid)
                using (LinearGradientBrush gradientBrush = new LinearGradientBrush(
                           new Rectangle(0, 0, width, height),
                           Color.FromArgb(r1, g1, b1),   // Startfarve
                           Color.FromArgb(r2, g2, b2),   // Slutfarve
                           LinearGradientMode.Horizontal)) // Retning af gradienten
                {
                    // Fyld baggrunden med gradienten
                    graphics.FillRectangle(gradientBrush, 0, 0, width, height);
                }

                // Definer skrifttype og pensel
                Font font = new Font("Arial", 40, FontStyle.Bold);
                Brush brush = new SolidBrush(Color.Black);

                // Tegn tallet i midten af billedet
                StringFormat format = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center
                };
                graphics.DrawString(number.ToString(), font, brush, new RectangleF(0, 0, width, height), format);
            }

            // Gem billedet i en MemoryStream og konverter til byte array
            using (MemoryStream memoryStream = new MemoryStream())
            {
                bitmap.Save(memoryStream, ImageFormat.Jpeg);
                return memoryStream.ToArray();
            }
        }
    }
}