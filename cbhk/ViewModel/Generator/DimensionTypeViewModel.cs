using CBHK.CustomControl.JsonTreeViewComponents;
using CBHK.GeneralTool.TreeViewComponentsHelper;
using CommunityToolkit.Mvvm.ComponentModel;
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
        public DimensionTypeViewModel(IContainerProvider container, MainView mainView) :base(container, mainView)
        {
            Container = container;
            Home = mainView;
            htmlHelper = new(Container)
            {
                plan = this,
                jsonTool = JsonTool = new JsonTreeViewItemExtension(Container)
            };
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
                    JsonTreeViewDataStructure result = htmlHelper.AnalyzeHTMLData(ConfigDirectoryPath + CurrentVersion.Text);
                    TreeViewItemList = result.Result;
                    string resultString = result.ResultString.ToString().TrimEnd([',', '\r', '\n']);
                    TextEditor.Text = "{" + (resultString.Length > 0 ? "\r\n" + resultString + "\r\n" : "") + "}";

                    JsonTool.SetLineNumbersForEachItem(result.Result, null);

                    //为代码编辑器安装大纲管理器
                    FoldingManager = FoldingManager.Install(TextEditor.TextArea);
                    XshdSyntaxDefinition xshdSyntaxDefinition = new();
                    xshdSyntaxDefinition = HighlightingLoader.LoadXshd(new XmlTextReader(AppDomain.CurrentDomain.BaseDirectory + @"Resource\Configs\Common\Json.xshd"));
                    IHighlightingDefinition jsonHighlighting = HighlightingLoader.Load(xshdSyntaxDefinition, HighlightingManager.Instance);
                    TextEditor.SyntaxHighlighting = jsonHighlighting;
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
                _ = htmlHelper.ReceiveJsonContentAndGenerateTreeViewItemList(currentText);
            });
        }
    }
}