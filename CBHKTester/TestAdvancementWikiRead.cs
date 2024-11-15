using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using cbhk.CustomControls.JsonTreeViewComponents;
using cbhk.GeneralTools;
using cbhk.GeneralTools.TreeViewComponentsHelper;
using cbhk.Model.Common;
using cbhk.ViewModel.Generators;

namespace CBHKTester
{
    [TestClass]
    public partial class TestAdvancementWikiRead
    {
        #region Field
        private IContainerProvider _container;
        private HtmlHelper htmlHelper;
        private string configDirectoryPath = AppDomain.CurrentDomain.BaseDirectory + @"Resource\Configs\DimensionType\Data\Rules\1.20.4";

        public Dictionary<string, CompoundJsonTreeViewItem> ValueProviderContextDictionary { get; set; } = [];

        DimensionTypeViewModel plan = new(null, null);

        JsonTreeViewItemExtension jsonTool = null;

        [GeneratedRegex(@"^\s+?\:?\s+?(\*+)")]
        private static partial Regex GetLineStarCount();

        [GeneratedRegex(@"{{Nbt inherit/[a-z_\s]+}}", RegexOptions.IgnoreCase)]
        private static partial Regex GetTemplateKey();

        [GeneratedRegex(@"可选", RegexOptions.IgnoreCase)]
        private static partial Regex GetOptionalKey();

        [GeneratedRegex(@"可以为空", RegexOptions.IgnoreCase)]
        private static partial Regex GetIsCanNullableKey();

        [GeneratedRegex(@"^\:?\s+?\*+\s+?\{\{nbt\|(?<1>[a-z_]+)\}\}", RegexOptions.IgnoreCase)]
        private static partial Regex GetExplanationForHierarchicalNodes();

        [GeneratedRegex(@"^\:?\s+?\*+\s+?\{\{nbt\|(?<1>[a-z_]+)\|(?<2>[a-z_]+)\}\}", RegexOptions.IgnoreCase)]
        private static partial Regex GetNodeTypeAndKey();

        [GeneratedRegex(@"^\:?\s+?\*+\s+?(?<branch>\{\{nbt\|[a-z]+\}\})+(\{\{nbt\|(?<1>[a-z]+)\|(?<2>[a-z_]+)\}\})", RegexOptions.IgnoreCase)]
        private static partial Regex GetMultiTypeAndKeyOfNode();

        [GeneratedRegex(@"默认为\{\{cd\|[a-z_]+\}\}", RegexOptions.IgnoreCase)]
        private static partial Regex GetDefaultStringValue();

        [GeneratedRegex(@"默认为\d+", RegexOptions.IgnoreCase)]
        private static partial Regex GetDefaultNumberValue();

        [GeneratedRegex(@"默认为\{\{cd\|(true|false)\}\}", RegexOptions.IgnoreCase)]
        private static partial Regex GetDefaultBoolValue();

        [GeneratedRegex(@"\[\[方块标签\]\]", RegexOptions.IgnoreCase)]
        private static partial Regex GetBlcokTagValue();

        [GeneratedRegex(@"\[\[实体标签\]\]", RegexOptions.IgnoreCase)]
        private static partial Regex GetEntityTagValue();

        [GeneratedRegex(@"\{\{cd\|[a-z:_]+\}\}", RegexOptions.IgnoreCase)]
        private static partial Regex GetEnumValue();

        [GeneratedRegex(@"====\s(minecraft\:[a-z_]+)\s====", RegexOptions.IgnoreCase)]
        private static partial Regex GetBoldKeywords();

        private string AdvancementWikiFilePath = AppDomain.CurrentDomain.BaseDirectory + @"Resource\1.20.4.wiki";
        private Dictionary<int, string> WikiDesignateLine = [];
        private string[] ProgressClassList = ["treeview"];

        private Dictionary<string, List<JsonTreeViewItem>> CriteriaDataList = [];
        private Dictionary<string, List<JsonTreeViewItem>> AdvancementReferenceItemList = [];

        public ObservableCollection<JsonTreeViewItem> AdvancementTreeViewItemList = [];
        #endregion

        public TestAdvancementWikiRead(IContainerProvider container)
        {
            _container = container;
            htmlHelper = _container.Resolve<HtmlHelper>();
        }

        [TestMethod]
        public async Task TestHtmlRead()
        {
            JsonTreeViewDataStructure result = htmlHelper.AnalyzeHTMLData(configDirectoryPath);
            var test = result;
        }
    }
}