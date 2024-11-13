using System.Runtime.InteropServices;
using Engine.Utils;
using Xunit;

namespace Engine.Tests.Utils;

public class ImageGeneratorTests
{
    [Fact]
    public void GenerateImageWithNumber_ShouldReturnNonEmptyArray_OnWindows()
    {
        // Arrange
        var imageGenerator = new ImageGenerator();

        // Act
        var result = imageGenerator.GenerateImageWithNumber(123, "Test Text");

        // Assert
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Assert.NotNull(result);
            Assert.NotEmpty(result); // Billedet skal indeholde data
        }
        else
        {
            // Hvis ikke på Windows, forvent en fake byte array [0, 0, 0]
            Assert.Equal(new byte[] { 0, 0, 0 }, result);
        }
    }
}