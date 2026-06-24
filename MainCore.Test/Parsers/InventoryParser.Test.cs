using MainCore.Enums;

namespace MainCore.Test.Parsers
{
    public class InventoryParser : BaseParser
    {
        private const string HeroInventory = "Parsers/Inventory/HeroInventory.html";
        private const string Buildings = "Parsers/Inventory/Buildings.html";
        private const string AmountDialog = "Parsers/Inventory/AmountDialog.html";

        [Theory]
        [InlineData(HeroInventory, true)]
        [InlineData(Buildings, false)]
        public void IsInventoryPage(string file, bool expected)
        {
            _html.Load(file);
            var actual = MainCore.Parsers.InventoryParser.IsInventoryPage(_html);
            actual.ShouldBe(expected);
        }

        [Fact]
        public void GetHeroAvatar()
        {
            _html.Load(HeroInventory);
            var actual = MainCore.Parsers.InventoryParser.GetHeroAvatar(_html);
            actual.ShouldNotBeNull();
        }

        [Theory]
        [InlineData(HeroInventory, true)]
        [InlineData(Buildings, false)]
        public void IsInventoryLoaded(string file, bool expected)
        {
            _html.Load(file);
            var actual = MainCore.Parsers.InventoryParser.IsInventoryLoaded(_html);
            actual.ShouldBe(expected);
        }

        [Theory]
        [InlineData(HeroInventory, HeroItemEnums.Crop)]
        [InlineData(HeroInventory, HeroItemEnums.Wood)]
        [InlineData(HeroInventory, HeroItemEnums.Iron)]
        [InlineData(HeroInventory, HeroItemEnums.Clay)]
        public void GetItemSlot(string file, HeroItemEnums type)
        {
            _html.Load(file);
            var actual = MainCore.Parsers.InventoryParser.GetItemSlot(_html, type);
            actual.ShouldNotBeNull();
        }

        [Fact]
        public void IsResourceTransferDialog()
        {
            _html.Load(AmountDialog);
            var actual = MainCore.Parsers.InventoryParser.IsResourceTransferDialog(_html);
            actual.ShouldBeTrue();
        }

        [Theory]
        [InlineData("lumber")]
        [InlineData("clay")]
        [InlineData("iron")]
        public void GetResourceTransferInput(string name)
        {
            _html.Load(AmountDialog);
            var actual = MainCore.Parsers.InventoryParser.GetResourceTransferInput(_html, name);
            actual.ShouldNotBeNull();
        }

        [Fact]
        public void GetTransferMaxButton()
        {
            _html.Load(AmountDialog);
            var actual = MainCore.Parsers.InventoryParser.GetTransferMaxButton(_html);
            actual.ShouldNotBeNull();
        }

        [Fact]
        public void GetTransferButton()
        {
            _html.Load(AmountDialog);
            var actual = MainCore.Parsers.InventoryParser.GetTransferButton(_html);
            actual.ShouldNotBeNull();
        }
    }
}