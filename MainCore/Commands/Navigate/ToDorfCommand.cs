namespace MainCore.Commands.Navigate
{
    [Handler]
    public static partial class ToDorfCommand
    {
        public sealed record Command(int Dorf) : ICommand;

        private static async ValueTask<Result> HandleAsync(
           Command command,
           IChromeBrowser browser,
           CancellationToken cancellationToken
           )
        {
            var dorf = command.Dorf;

            var currentUrl = browser.CurrentUrl;
            var currentDorf = GetCurrentDorf(currentUrl);
            if (dorf == 0)
            {
                if (currentDorf == 0) dorf = 1;
                else dorf = currentDorf;
            }

            if (currentDorf != 0 && dorf == currentDorf)
            {
                return Result.Ok();
            }

            var (_, isFailed, element, errors) = await browser.GetElement(doc => NavigationBarParser.GetDorfButton(doc, dorf), cancellationToken);
            if (isFailed) return Result.Fail(errors);

            Result result;
            result = await browser.Click(element, cancellationToken);
            if (result.IsFailed) return result;

            // Click is human-like but can miss/get blocked (e.g. on a build page). Verify quickly,
            // then self-heal with a direct URL navigation so we never hang on a stuck click.
            var quick = await browser.WaitUrl($"dorf{dorf}.php", TimeSpan.FromSeconds(10), cancellationToken);
            if (quick.IsFailed)
            {
                var root = new Uri(browser.CurrentUrl).GetLeftPart(UriPartial.Authority);
                result = await browser.Navigate($"{root}/dorf{dorf}.php", cancellationToken);
                if (result.IsFailed) return result;
            }

            result = await browser.WaitPageChanged($"dorf{dorf}.php", cancellationToken);
            if (result.IsFailed) return result;

            return Result.Ok();
        }

        private static int GetCurrentDorf(string url)
        {
            if (url.Contains("dorf1")) return 1;
            if (url.Contains("dorf2")) return 2;
            return 0;
        }
    }
}