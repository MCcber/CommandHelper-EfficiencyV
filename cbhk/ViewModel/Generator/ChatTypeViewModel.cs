using CBHK.CustomControl;
using CBHK.CustomControl.Interfaces;
using CBHK.CustomControl.JsonTreeViewComponents;
using CBHK.GeneralTool;
using CBHK.GeneralTool.TreeViewComponentsHelper;
using CBHK.Model.Common;
using CBHK.View;
using CommunityToolkit.Mvvm.ComponentModel;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Folding;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using Newtonsoft.Json.Linq;
using Prism.Ioc;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Xml;
using System.Threading.Tasks;
using System.Windows;
using static CBHK.Model.Common.Enums;
using ICSharpCode.AvalonEdit.Highlighting;
using Newtonsoft.Json;
using CBHK.ViewModel.Common;

namespace CBHK.ViewModel.Generator
{
    public partial class ChatTypeViewModel:BaseCustomWorldUnifiedPlan
    {
        #region Field
        private HtmlHelper _htmlHelper = null;
        private Window home = null;
        private string configDirectoryPath = AppDomain.CurrentDomain.BaseDirectory + @"Resource\Configs\ChatType\Data\Rule\";
        private string commonCompoundDataDirectoryPath = AppDomain.CurrentDomain.BaseDirectory + @"Resource\Configs\Common\";
        public TextEditor textEditor = null;
        private FoldingManager foldingManager = null;
        private IContainerProvider _container;
        JsonTreeViewItemExtension jsonTool = null;
        #endregion

        #region Property
        public string RootDirectory { get; set; } = @"ChatType\Data\Rule\";

        private TextComboBoxItem _currentVersion = new();
        public TextComboBoxItem CurrentVersion
        {
            get => _currentVersion;
            set => SetProperty(ref _currentVersion, value);
        }

        public ObservableCollection<TextComboBoxItem> VersionList { get; set; } =
            [
                new TextComboBoxItem()
                {
                    Text = "1.20.3-1.20.4"
                }
            ];

        [ObservableProperty]
        public ObservableCollection<JsonTreeViewItem> _treeViewItemList = [];

        public Dictionary<string, JsonTreeViewItem> KeyValueContextDictionary { get; set; } = [];
        public Dictionary<string, List<string>> DependencyItemList { get; set; } = [];

        public Dictionary<string, string> PresetCustomCompoundKeyDictionary { get; set; } = new()
        {
        };

        public Dictionary<string, Dictionary<string, List<string>>> EnumCompoundDataDictionary { get; set; } = [];

        public Dictionary<string, List<string>> EnumIDDictionary { get; set; } = [];
        public Dictionary<string, string> TranslateDictionary { get; set; } = new()
        {
            { "#聊天类型装饰|下文","#聊天类型装饰" },
            { "#文本组件","#Inherit/text component/main" },
            { "#文本组件内容","#Inherit/text component/content" }
        };
        public Dictionary<string, string> TranslateDefaultDictionary { get; set; } = [];
        public List<string> DependencyFileList { get; set; }
        public List<string> DependencyDirectoryList { get; set; }
        #endregion

        #region Method
        public ChatTypeViewModel(IContainerProvider container, MainView mainView)
        {
            _container = container;
            home = mainView;
            _htmlHelper = new(_container)
            {
                plan = this,
                jsonTool = jsonTool = new JsonTreeViewItemExtension(_container)
            };
        }
        #endregion

        #region Event
        /// <summary>
        /// 关闭窗体
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void CommonWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            DependencyItemList.Clear();
        }

        /// <summary>
        /// 安装大纲、应用高亮规则
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public async void TextEditor_Loaded(object sender, RoutedEventArgs e)
        {
            await Task.Run(async () =>
            {
                textEditor = sender as TextEditor;
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    JsonTreeViewDataStructure result = _htmlHelper.AnalyzeHTMLData(configDirectoryPath + CurrentVersion.Text);
                    string resultString = result.ResultString.ToString().TrimEnd([',', '\r', '\n']);
                    textEditor.Text = "{" + (resultString.Length > 0 ? "\r\n" + resultString + "\r\n" : "") + "}";
                    foreach (var item in result.Result)
                    {
                        if (item is CompoundJsonTreeViewItem compoundJsonTreeViewItem && compoundJsonTreeViewItem.Children.Count > 0)
                        {
                            jsonTool.SetParentForEachItem(compoundJsonTreeViewItem.Children, compoundJsonTreeViewItem);
                        }
                    }

                    jsonTool.SetLayerCountForEachItem(result.Result, 1);
                    jsonTool.SetLineNumbersForEachItem(result.Result, null);
                    TreeViewItemList = result.Result;

                    //为代码编辑器安装大纲管理器
                    foldingManager = FoldingManager.Install(textEditor.TextArea);
                    XshdSyntaxDefinition xshdSyntaxDefinition = new();
                    xshdSyntaxDefinition = HighlightingLoader.LoadXshd(new XmlTextReader(AppDomain.CurrentDomain.BaseDirectory + @"Resource\Configs\Common\Json.xshd"));
                    IHighlightingDefinition jsonHighlighting = HighlightingLoader.Load(xshdSyntaxDefinition, HighlightingManager.Instance);
                    textEditor.SyntaxHighlighting = jsonHighlighting;
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
            string currentText = textEditor.Text;
            Task.Run(() =>
            {
                try
                {
                    JsonTextReader jsonTextReader = new(new StringReader(currentText));
                    while (jsonTextReader.Read())
                    {
                        string path = jsonTextReader.Path;
                        switch (jsonTextReader.TokenType)
                        {
                            case JsonToken.None:
                                break;
                            case JsonToken.StartObject:
                                break;
                            case JsonToken.StartArray:
                                break;
                            case JsonToken.StartConstructor:
                                break;
                            case JsonToken.PropertyName:
                                break;
                            case JsonToken.Raw:
                                break;
                            case JsonToken.Integer:
                                break;
                            case JsonToken.Float:
                                break;
                            case JsonToken.String:
                                break;
                            case JsonToken.Boolean:
                                break;
                            case JsonToken.Null:
                                break;
                            case JsonToken.Undefined:
                                break;
                            case JsonToken.EndObject:
                                break;
                            case JsonToken.EndArray:
                                break;
                            case JsonToken.EndConstructor:
                                break;
                            case JsonToken.Date:
                                break;
                            case JsonToken.Bytes:
                                break;
                            default:
                                break;
                        }
                    }
                }
                catch (JsonReaderException exception)
                {
                    int lineNumber = exception.LineNumber;
                    int linPosition = exception.LinePosition;
                }
            });
        }
        #endregion
    }
}
