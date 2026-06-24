namespace MainCore.Parsers
{
    public static class InventoryParser
    {
        public static bool IsInventoryPage(HtmlDocument doc)
        {
            var heroDiv = doc.GetElementbyId("heroV2");
            if (heroDiv is null) return false;
            var aNode = heroDiv.Descendants("a")
                .FirstOrDefault(x => x.GetAttributeValue("data-tab", 0) == 1);
            if (aNode is null) return false;
            return aNode.HasClass("active");
        }

        public static HtmlNode GetHeroAvatar(HtmlDocument doc)
        {
            return doc.GetElementbyId("heroImageButton");
        }

        public static bool IsInventoryLoaded(HtmlDocument doc)
        {
            var inventoryPageWrapper = doc.DocumentNode
                .Descendants("div")
                .FirstOrDefault(x => x.HasClass("inventoryPageWrapper"));
            if (inventoryPageWrapper is null) return false;
            return !inventoryPageWrapper.HasClass("loading");
        }

        public static HtmlNode? GetItemSlot(HtmlDocument doc, HeroItemEnums type)
        {
            var heroItemsDiv = doc.DocumentNode
                .Descendants("div")
                .FirstOrDefault(x => x.HasClass("heroItems"));

            if (heroItemsDiv is null) return null;

            var heroItemDivs = heroItemsDiv
                .Descendants("div")
                .Where(x => x.HasClass("heroItem") && !x.HasClass("empty"));

            if (!heroItemDivs.Any()) return null;

            foreach (var itemSlot in heroItemDivs)
            {
                if (itemSlot.ChildNodes.Count < 2) continue;
                var itemNode = itemSlot.ChildNodes[1];
                var classes = itemNode.GetClasses();
                if (classes.Count() < 2) continue;

                var itemValue = classes.ElementAt(1);

                if (itemValue.ParseInt() == (int)type) return itemSlot;
            }

            return null;
        }

        // Newer "Transfer resources" dialog (hero -> active village): one dialog with named inputs
        // (lumber/clay/iron/crop) plus a "Transfer" button. Replaces the old consumableHeroItem dialog.
        public static bool IsResourceTransferDialog(HtmlDocument doc)
        {
            return doc.DocumentNode
                .Descendants("div")
                .Any(x => x.HasClass("resourceTransferDialog"));
        }

        public static HtmlNode? GetResourceTransferInput(HtmlDocument doc, string name)
        {
            return doc.DocumentNode
                .Descendants("input")
                .FirstOrDefault(x => x.GetAttributeValue("name", "") == name);
        }

        public static HtmlNode? GetTransferButton(HtmlDocument doc)
        {
            var actionButton = doc.DocumentNode
                .Descendants("div")
                .FirstOrDefault(x => x.HasClass("actionButton"));
            if (actionButton is null) return null;

            // Two buttons: "Transfer maximum" and "Transfer". We want the exact-amount "Transfer".
            return actionButton
                .Descendants("button")
                .FirstOrDefault(x => x.InnerText.Trim() == "Transfer");
        }

        // "Transfer maximum" - always enabled (unlike "Transfer", which needs a manually-entered
        // amount and is disabled until the input is touched). Simpler/more reliable than driving the
        // per-resource amount inputs.
        public static HtmlNode? GetTransferMaxButton(HtmlDocument doc)
        {
            var actionButton = doc.DocumentNode
                .Descendants("div")
                .FirstOrDefault(x => x.HasClass("actionButton"));
            if (actionButton is null) return null;

            return actionButton
                .Descendants("button")
                .FirstOrDefault(x => x.InnerText.Trim() == "Transfer maximum");
        }
    }
}