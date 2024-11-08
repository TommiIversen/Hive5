using System.Text.RegularExpressions;
using Microsoft.Playwright;
using Microsoft.Playwright.MSTest;

namespace PlaywrightTests;

[TestClass]
public class ExampleTest : PageTest
{
   
    [TestMethod]
    public async Task OpenHomePage()
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

        // Tjek, om portinformationen indeholder forventet data
        //var portText = await Page.Locator("[data-testid='port-info']").InnerTextAsync();
        //Assert.IsFalse(string.IsNullOrEmpty(portText), "Port information should not be empty.");
        
        //var contentVisible = await Page.Locator("[data-testid='content-engineinfo']").IsVisibleAsync();
        //Assert.IsTrue(contentVisible, "Engine panel should be visible after being toggled.");
    }
}