namespace MainCore.Commands.Features.UseHeroItem
{
    [Handler]
    public static partial class UseHeroItemCommand
    {
        public sealed record Command(HeroItemEnums Item, long Amount) : ICommand;

        private static async ValueTask<Result> HandleAsync(
            Command command,
            IChromeBrowser browser,
            ILogger logger,
            IDelayService delayService,
            CancellationToken cancellationToken)
        {
            var (item, _) = command;
            logger.Information("Transfer max {Item} from hero inventory", item);

            var result = await ClickItem(browser, item, cancellationToken);
            if (result.IsFailed) return result;
            await delayService.DelayClick(cancellationToken);

            result = await ConfirmMax(browser, cancellationToken);
            if (result.IsFailed) return result;
            await delayService.DelayClick(cancellationToken);

            return Result.Ok();
        }

        private static async Task<Result> ClickItem(
            IChromeBrowser browser,
            HeroItemEnums item,
            CancellationToken cancellationToken)
        {
            var (_, isFailed, element, errors) = await browser.GetElement(doc => InventoryParser.GetItemSlot(doc, item), cancellationToken);
            if (isFailed) return Result.Fail(errors);

            Result result;
            result = await browser.Click(element, cancellationToken);
            if (result.IsFailed) return result;

            // Clicking a resource bag opens the "Transfer resources" dialog.
            static bool dialogShown(IWebDriver driver)
            {
                var doc = new HtmlDocument();
                doc.LoadHtml(driver.PageSource);
                return InventoryParser.IsResourceTransferDialog(doc);
            }

            result = await browser.Wait(dialogShown, cancellationToken);
            if (result.IsFailed) return result;
            return Result.Ok();
        }

        private static async Task<Result> ConfirmMax(
            IChromeBrowser browser,
            CancellationToken cancellationToken)
        {
            var (_, isFailed, element, errors) = await browser.GetElement(doc => InventoryParser.GetTransferMaxButton(doc), cancellationToken);
            if (isFailed) return Result.Fail(errors);

            Result result;
            result = await browser.Click(element, cancellationToken);
            if (result.IsFailed) return result;

            // Dialog closes once the transfer completes.
            static bool dialogClosed(IWebDriver driver)
            {
                var doc = new HtmlDocument();
                doc.LoadHtml(driver.PageSource);
                return !InventoryParser.IsResourceTransferDialog(doc);
            }

            result = await browser.Wait(dialogClosed, cancellationToken);
            if (result.IsFailed) return result;

            return Result.Ok();
        }
    }
}