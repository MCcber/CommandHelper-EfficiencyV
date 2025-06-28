using CBHK.CustomControl.JsonTreeViewComponents;
using CBHK.GeneralTool.TreeViewComponentsHelper;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Folding;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using ICSharpCode.AvalonEdit.Highlighting;
using System;
using System.Windows;
using System.Xml;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;
using Prism.Ioc;
using CBHK.View;
using System.Collections.ObjectModel;
using CBHK.Model.Common;
using CBHK.CustomControl;
using Newtonsoft.Json.Linq;
using System.Linq;
using CBHK.ViewModel.Common;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CBHK.ViewModel.Generator
{
    public partial class CustomWorldGeneratorViewModel:BaseCustomWorldUnifiedPlan
    {
        #region Property
        private List<string> ConfigDirectoryPathList { get; set; } = [];
        public override string ConfigDirectoryPath { get; set; } = AppDomain.CurrentDomain.BaseDirectory + @"Resource\Configs\CustomWorldGenerator\Data\Rule\";
        public override string CommonCompoundDataDirectoryPath { get; set; } = AppDomain.CurrentDomain.BaseDirectory + @"Resource\Configs\Common\";

        private TextComboBoxItem _currentVersion = new();
        public override TextComboBoxItem CurrentVersion
        {
            get => _currentVersion;
            set => SetProperty(ref _currentVersion, value);
        }

        public override ObservableCollection<TextComboBoxItem> VersionList { get; set; } =
            [
                new TextComboBoxItem()
                {
                    Text = "1.20.3-1.20.4"
                }
            ];

        [ObservableProperty]
        private TextComboBoxItem _currentGenerator = new();

        public ObservableCollection<TextComboBoxItem> GeneratorList { get; set; } = [];

        private ObservableCollection<JsonTreeViewItem> _treeViewItemList;
        public override ObservableCollection<JsonTreeViewItem> TreeViewItemList
        {
            get => _treeViewItemList;
            set => SetProperty(ref _treeViewItemList, value);
        }

        protected override TextEditor TextEditor { get; set; }

        public override Dictionary<string, JsonTreeViewItem> KeyValueContextDictionary { get; set; } = [];
        public override Dictionary<string, List<string>> DependencyItemList { get; set; } = [];

        public override Dictionary<string, Dictionary<string, List<string>>> EnumCompoundDataDictionary { get; set; } = [];

        public override Dictionary<string, List<string>> EnumIDDictionary { get; set; } = [];
        public override Dictionary<string, string> TranslateDictionary { get; set; } = [];
        public override Dictionary<string, string> TranslateDefaultDictionary { get; set; } = [];
        public override List<string> DependencyFileList { get; set; }
        public override List<string> DependencyDirectoryList { get; set; }
        public override JsonTreeViewItem VisualLastItem { get; set; }
        #endregion

        #region Method
        public CustomWorldGeneratorViewModel(IContainerProvider container, MainView mainView) : base(container, mainView)
        {
            Container = container;
            Home = mainView;
            htmlHelper = new(Container)
            {
                plan = this,
                jsonTool = JsonTool = new JsonTreeViewItemExtension(Container)
            };

            #region 添加数据上下文所需的枚举集合与转换字典数据
            EnumIDDictionary.Add("流体ID", ["minecraft:water", "minecraft:lava"]);
            EnumIDDictionary.Add("方块ID", ["minecraft:acacia_button", "minecraft:acacia_door"]);
            EnumIDDictionary.Add("物品ID", ["minecraft:acacia_button", "minecraft:acacia_door"]);
            EnumIDDictionary.Add("战利品表", ["minecraft:a", "minecraft:b", "minecraft:c"]);
            EnumIDDictionary.Add("药水#物品数据值|酿造药水的ID", ["minecraft:potion_a", "minecraft:potion_b"]);
            EnumIDDictionary.Add("染料颜色", ["red", "green", "blue"]);
            #endregion

            #region 添加复合类数据、调用上下文初始化方法
            string[] commonDirectoryFileArray = Directory.GetFiles(CommonCompoundDataDirectoryPath);
            foreach (var item in commonDirectoryFileArray)
            {
                string fileName = Path.GetFileNameWithoutExtension(item);
                string data = File.ReadAllText(item);
                switch (fileName)
                {
                    case "BlockStateProperty":
                        {
                            JObject blockStatePropertyObject = JObject.Parse(data);
                            List<JProperty> blockIDList = [.. blockStatePropertyObject.Properties()];
                            Dictionary<string, List<string>> blockStateCompound = [];
                            foreach (var blockID in blockIDList)
                            {
                                blockStateCompound.TryAdd(blockID.Name, []);
                                if (blockStatePropertyObject[blockID.Name][0] is JObject propertObject)
                                {
                                    blockStateCompound[blockID.Name].AddRange(propertObject.Properties().Select(item => '{' + item.ToString() + '}'));
                                }
                            }
                            EnumCompoundDataDictionary.Add(fileName, blockStateCompound);
                            break;
                        }
                    default:
                        {
                            break;
                        }
                }
            }
            #endregion
        }
        #endregion

        #region Event
        /// <summary>
        /// 安装大纲、应用高亮规则
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public async void TextEditor_Loaded(object sender, RoutedEventArgs e)
        {
            await Task.Run(async () =>
            {
                TextEditor = sender as TextEditor;
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    #region 分析Wiki源码文档并应用于View
                    JsonTreeViewDataStructure result = htmlHelper.AnalyzeHTMLData(ConfigDirectoryPath + CurrentVersion.Text);
                    string resultString = result.ResultString.ToString().TrimEnd([',', '\r', '\n']);
                    TextEditor.Text = "{" + (resultString.Length > 0 ? "\r\n" + resultString + "\r\n" : "") + "}";
                    TreeViewItemList = result.Result;
                    #endregion

                    #region 设置父级与行引用
                    foreach (var item in result.Result)
                    {
                        if (item is BaseCompoundJsonTreeViewItem compoundJsonTreeViewItem && compoundJsonTreeViewItem.LogicChildren.Count > 0)
                        {
                            JsonTool.SetParentForEachItem(compoundJsonTreeViewItem.LogicChildren, compoundJsonTreeViewItem);
                        }
                    }
                    Tuple<JsonTreeViewItem, JsonTreeViewItem> previousAndNext1 = JsonTool.SetLineNumbersForEachSubItem(result.Result, !result.IsHaveRootItem ? 2 : 1);
                    if (previousAndNext1.Item2 is not null)
                    {
                        VisualLastItem = previousAndNext1.Item2;
                    }
                    #endregion

                    #region 处理视觉引用
                    for (int i = 0; i < result.Result.Count; i++)
                    {
                        Tuple<JsonTreeViewItem, JsonTreeViewItem> currentPreviousAndNext = SearchVisualPreviousAndNextItem(result.Result[i]);
                        if (currentPreviousAndNext.Item1 is not null)
                        {
                            result.Result[i].VisualPrevious = currentPreviousAndNext.Item1;
                        }
                        if (currentPreviousAndNext.Item2 is not null)
                        {
                            result.Result[i].VisualNext = currentPreviousAndNext.Item2;
                        }
                        if (result.Result[i] is BaseCompoundJsonTreeViewItem baseCompoundJsonTreeViewItem)
                        {
                            foreach (var item in baseCompoundJsonTreeViewItem.LogicChildren)
                            {
                                Tuple<JsonTreeViewItem, JsonTreeViewItem> previousAndNext2 = baseCompoundJsonTreeViewItem.SearchVisualPreviousAndNextItem(item);
                                if (previousAndNext2.Item1 is not null)
                                {
                                    item.VisualPrevious = previousAndNext2.Item1;
                                }
                                if (previousAndNext2.Item2 is not null)
                                {
                                    item.VisualNext = previousAndNext2.Item2;
                                }
                            }
                        }
                    }
                    #endregion

                    #region 为代码编辑器安装大纲管理器并应用文档着色规则
                    FoldingManager = FoldingManager.Install(TextEditor.TextArea);
                    XshdSyntaxDefinition xshdSyntaxDefinition = new();
                    xshdSyntaxDefinition = HighlightingLoader.LoadXshd(new XmlTextReader(AppDomain.CurrentDomain.BaseDirectory + @"Resource\Configs\Common\Json.xshd"));
                    IHighlightingDefinition jsonHighlighting = HighlightingLoader.Load(xshdSyntaxDefinition, HighlightingManager.Instance);
                    TextEditor.SyntaxHighlighting = jsonHighlighting;
                    #endregion
                });
            });
        }

        /// <summary>
        /// 失焦后执行数据同步
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void TextEditor_LostFocus(object sender, RoutedEventArgs e)
        {
            string currentText = TextEditor.Text;
            Task.Run(() =>
            {
                _ = htmlHelper.GenerateTreeViewItemListFromJson(currentText);
            });
        }
        #endregion
    }
}
