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



[TestMethod]
public async Task CreateAndRemoveWorkerTest()
{
    // Naviger til startsiden
    await Page.GotoAsync("http://localhost:9000");

    // Tjek, at siden har den forventede titel
    await Expect(Page).ToHaveTitleAsync("Hive5 StreamHub");

    // Vent på, at "Tilføj Worker"-knappen bliver synlig, og klik på den
    await Page.WaitForSelectorAsync("[data-testid='toggle-addworker']", new PageWaitForSelectorOptions { State = WaitForSelectorState.Visible });
    var toggleButton = Page.Locator("[data-testid='toggle-addworker']");
    await toggleButton.ClickAsync();

    // Vent på, at "Create Worker"-formularen bliver synlig
    await Page.WaitForSelectorAsync("[data-testid='create-worker-form']", new PageWaitForSelectorOptions { State = WaitForSelectorState.Visible });

    // Udfyld formularfelterne for at oprette en ny worker
    string workerId = "PlaywrightWorker123"; // Unikt Worker ID for testen
    await Page.Locator("[data-testid='worker-id-input']").FillAsync(workerId);
    await Page.Locator("[data-testid='worker-name-input']").FillAsync("Playwright Test Worker");
    await Page.Locator("[data-testid='worker-description-input']").FillAsync("Playwright Test description");
    await Page.Locator("[data-testid='worker-command-input']").FillAsync("echo 'Hello World'");

    // Klik på 'Opret Worker'-knappen for at oprette en ny worker
    var createButton = Page.Locator("[data-testid='create-worker-button']");
    await createButton.ClickAsync();

    // Vent på, at resultatbeskeden bliver synlig, og bekræft, at den indikerer succes
    await Page.WaitForSelectorAsync("[data-testid='command-result-message']", new PageWaitForSelectorOptions { State = WaitForSelectorState.Visible });
    var resultMessage = await Page.Locator("[data-testid='command-result-message']").InnerTextAsync();
    Assert.IsTrue(!string.IsNullOrEmpty(resultMessage), "Result message should be displayed after creating a worker.");
    Assert.IsTrue(resultMessage.Contains("successfully", StringComparison.OrdinalIgnoreCase), "Result message should indicate success.");

    // Vent på, at "Control"-fanen for den specifikke worker bliver synlig, og klik på den
    await Page.WaitForSelectorAsync($"[data-testid='toggle-tab-control-{workerId}']", new PageWaitForSelectorOptions { State = WaitForSelectorState.Visible });
    var controlTabButton = Page.Locator($"[data-testid='toggle-tab-control-{workerId}']");
    await controlTabButton.ClickAsync();

    // Vent på, at fjern-knappen bliver synlig, og klik på den for at fjerne worker
    await Page.WaitForSelectorAsync($"[data-testid='remove-worker-{workerId}']", new PageWaitForSelectorOptions { State = WaitForSelectorState.Visible });
    var removeButton = Page.Locator($"[data-testid='remove-worker-{workerId}']");
    await removeButton.ClickAsync();
}



}