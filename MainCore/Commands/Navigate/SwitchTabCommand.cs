namespace MainCore.Commands.Navigate
{
    [Handler]
    public static partial class SwitchTabCommand
    {
        public sealed record Command(int TabIndex) : ICommand;

        private static async ValueTask<Result> HandleAsync(
           Command command,
           IChromeBrowser browser,
           CancellationToken cancellationToken
           )
        {
            return await SwitchTab(browser, command.TabIndex, cancellationToken);
        }

        public static async ValueTask<Result> SwitchTab(
            IChromeBrowser browser,
            int tabIndex,
            CancellationToken cancellationToken)
        {
            var count = BuildingTabParser.CountTab(browser.Html);
            if (tabIndex >= count) return Retry.Error.WithError($"Found {count} tabs but need tab #{tabIndex + 1} active");

            var tab = BuildingTabParser.GetTab(browser.Html, tabIndex);
            if (BuildingTabParser.IsTabActive(tab)) return Result.Ok();

            var (_, isFailed, element, errors) = await browser.GetElement(By.XPath(tab.XPath), cancellationToken);
            if (isFailed) return Result.Fail(errors).WithError($"Failed to find tab element [{tab.XPath}]");

            Result result;
            result = await browser.Click(element, cancellationToken);
            if (result.IsFailed) return result;

            bool tabActived(IWebDriver driver)
            {
                var doc = new HtmlDocument();
                doc.LoadHtml(driver.PageSource);
                var tab = BuildingTabParser.GetTab(doc, tabIndex);
                if (!BuildingTabParser.IsTabActive(tab)) return false;
                return true;
            }

            // Verify quickly; if the tab click did not activate, self-heal by navigating its href.
            var quick = await browser.Wait(tabActived, TimeSpan.FromSeconds(10), cancellationToken);
            if (quick.IsFailed)
            {
                var href = tab.GetAttributeValue("href", "");
                if (!string.IsNullOrEmpty(href))
                {
                    href = System.Net.WebUtility.HtmlDecode(href);
                    var host = new Uri(browser.CurrentUrl).GetLeftPart(UriPartial.Authority);
                    var url = href.StartsWith("http") ? href : $"{host}/{href.TrimStart('/')}";
                    result = await browser.Navigate(url, cancellationToken);
                    if (result.IsFailed) return result;
                }
            }

            result = await browser.Wait(tabActived, cancellationToken);
            if (result.IsFailed) return result;

            return Result.Ok();
        }
    }
}