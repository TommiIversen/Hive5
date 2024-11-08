using System.Text.RegularExpressions;
using Microsoft.Playwright;
using Microsoft.Playwright.MSTest;

namespace PlaywrightTests;

[TestClass]
public class Hive5Test : PageTest
{
   
    [TestMethod]
    public async Task OpenCloseEngineCOnnectionInfoPanel()
    {
        await Page.GotoAsync("http://localhost:9000");

        // Tilføj flere assertions baseret på, hvad du vil teste
        await Expect(Page).ToHaveTitleAsync("Hive5 StreamHub");
        
        // Tjek, om knappen er til stede i DOM'en og er synlig
        var toggleButton = Page.Locator("[data-testid='toggle-engineinfo']");
        var isButtonVisible = await toggleButton.IsVisibleAsync();

        // Assert for at sikre, at knappen er fundet og synlig
        Assert.IsTrue(isButtonVisible, "Toggle button for the Engine panel should be visible.");

        // Klik på knappen, hvis du ønsker at fortsætte testen
        await toggleButton.ClickAsync();

        var portInfoVisible = await Page.Locator("[data-testid='portinfo']").IsVisibleAsync();
        Assert.IsTrue(portInfoVisible, "Port information should be visible.");
        
        // Klik på knappen igen for at lukke panelet
        await toggleButton.ClickAsync();

        // Tjek, om portinformationen er blevet skjult
        portInfoVisible = await Page.Locator("[data-testid='portinfo']").IsVisibleAsync();
        Assert.IsFalse(portInfoVisible, "Port information should not be visible after toggling the panel closed.");
    }
}