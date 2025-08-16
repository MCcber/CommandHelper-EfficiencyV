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
using Prism.Ioc;
using CBHK.View;
using System.Collections.ObjectModel;
using CBHK.Model.Common;
using CBHK.CustomControl;
using CBHK.ViewModel.Common;
using CBHK.Domain;

namespace CBHK.ViewModel.Generator
{
    public partial class DimensionTypeViewModel : BaseCustomWorldUnifiedPlan
    {
        #region Property
        public override string ConfigDirectoryPath { get; set; } = AppDomain.CurrentDomain.BaseDirectory + @"Resource\Configs\DimensionType\Data\Rule\";
        public override string CommonCompoundDataDirectoryPath { get; set; } = AppDomain.CurrentDomain.BaseDirectory + @"Resource\Configs\Common\";

        public Dictionary<string, string> PresetCustomCompoundKeyDictionary { get; set; } = [];

        private ObservableCollection<JsonTreeViewItem> _treeViewItemList = [];
        public override ObservableCollection<JsonTreeViewItem> TreeViewItemList
        {
            get => _treeViewItemList;
            set => SetProperty(ref _treeViewItemList, value);
        }

        public override Dictionary<string, JsonTreeViewItem> KeyValueContextDictionary { get; set; } = [];
        public override Dictionary<string, List<string>> DependencyItemList { get; set; } = [];
        public override Dictionary<string, Dictionary<string, List<string>>> EnumCompoundDataDictionary { get; set; } = [];
        public override Dictionary<string, List<string>> EnumIDDictionary { get; set; } = [];

        public Dictionary<string, List<string>> DefaultListSource { get; set; } = [];

        public Dictionary<string, List<string>> DefaultCompoundSource { get; set; } = [];
        public override Dictionary<string, string> TranslateDictionary { get; set; } = [];
        public override Dictionary<string, string> TranslateDefaultDictionary { get; set; } = [];
        public string RootDirectory { get; set; }
        public override List<string> DependencyFileList { get; set; } = [];
        public override List<string> DependencyDirectoryList { get; set; } = [];

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
        public Dictionary<string, string> CustomItemTranslateDictionay { get; set; } = [];
        public Dictionary<string, string> TranslateCustomKeyWordDictionary { get; set; } = [];
        #endregion

        #region Method
        public DimensionTypeViewModel(IContainerProvider container, MainView mainView,CBHKDataContext context) :base(container, mainView,context)
        {
        }
        #endregion

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
                    if (!result.IsHaveRootItem)
                    {
                        resultString = "{" + (resultString.Length > 0 ? "\r\n" + resultString + "\r\n" : "") + "}";
                    }
                    TextEditor.Text = resultString;
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
                    SetVisualPreviousAndNextForEachItem();
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
    }
}