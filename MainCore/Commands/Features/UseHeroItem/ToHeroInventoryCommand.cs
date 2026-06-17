#pragma warning disable S1172

namespace MainCore.Commands.Features.UseHeroItem
{
    [Handler]
    public static partial class ToHeroInventoryCommand
    {
        public sealed record Command : ICommand;

        private static async ValueTask<Result> HandleAsync(
            Command command,
            IChromeBrowser browser,
            CancellationToken cancellationToken)
        {
            static bool InventoryReady(IWebDriver driver)
            {
                var doc = new HtmlDocument();
                doc.LoadHtml(driver.PageSource);
                return InventoryParser.IsInventoryPage(doc) || InventoryParser.IsInventoryLoaded(doc);
            }

            // Navigate straight to the hero inventory (account-wide) rather than clicking the avatar -
            // the click sometimes does not register and the page never changes, hanging the wait.
            var host = new Uri(browser.CurrentUrl).GetLeftPart(UriPartial.Authority);
            var result = await browser.Navigate($"{host}/hero/inventory", cancellationToken);
            if (result.IsFailed) return result;

            result = await browser.Wait(InventoryReady, cancellationToken);
            if (result.IsFailed) return result;

            return Result.Ok();
        }
    }
}