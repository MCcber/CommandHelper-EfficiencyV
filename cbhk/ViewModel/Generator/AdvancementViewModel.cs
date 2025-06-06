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
using CBHK.CustomControl.Interfaces;
using System.Threading;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using ICSharpCode.AvalonEdit.Document;
using static CBHK.Model.Common.Enums;
using Prism.Ioc;
using CBHK.View;
using System.Collections.ObjectModel;
using CBHK.Model.Common;
using CBHK.GeneralTool;
using CBHK.CustomControl;
using Newtonsoft.Json.Linq;
using System.Linq;
using CBHK.ViewModel.Common;

namespace CBHK.ViewModel.Generator
{
    public partial class AdvancementViewModel : BaseCustomWorldUnifiedPlan
    {
        #region Field
        private string configDirectoryPath = AppDomain.CurrentDomain.BaseDirectory + @"Resource\Configs\Advancement\Data\Rule\";
        private string commonCompoundDataDirectoryPath = AppDomain.CurrentDomain.BaseDirectory + @"Resource\Configs\Common\";
        #endregion

        #region Property
        public override string RootDirectory { get; set; } = @"Advancement\Data\Rule\";

        private TextComboBoxItem _currentVersion = new();
        public override TextComboBoxItem CurrentVersion
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

        public override Dictionary<string, JsonTreeViewItem> KeyValueContextDictionary { get; set; } = [];
        public override Dictionary<string, List<string>> DependencyItemList { get; set; } = [];

        public override Dictionary<string, Dictionary<string, List<string>>> EnumCompoundDataDictionary { get; set; } = [];

        public override Dictionary<string, List<string>> EnumIDDictionary { get; set; } = [];
        public override Dictionary<string, string> TranslateDictionary { get; set; } = new()
        {
            { "#准则|上文", "#Inherit/predicate" },
            { "#准则|下文", "#准则" },
            { "#战利品表谓词","#Inherit/predicate" },
            { "#图例|上文","#Inherit/predicate" },
            { "#文本组件","#Inherit/text component/main" },
            { "#文本组件内容","#Inherit/text component/content" },
            { "#记分板|记分项","#Inherit/common/scoreboardObject" },
            { "#流体状态|流体状态","#Inherit/common/fluidStructure"},
            { "<''物品堆叠组件''>" , "#Inherit/common/itemStackComponent" },
            { "<''准则名称''>","#准则触发器" },
            { "<''状态效果ID''>","药水#物品数据值|酿造药水的ID" },
            { "<''流体属性''>","#Inherit/common/fluidStructure"}
        };
        public override Dictionary<string, string> TranslateDefaultDictionary { get; set; } = new()
        {
            { "#Inherit/predicate","#Inherit/conditions/entity" }
        };
        public override List<string> DependencyFileList { get; set; }
        public override List<string> DependencyDirectoryList { get; set; }
        #endregion

        public AdvancementViewModel(IContainerProvider container, MainView mainView):base(container, mainView)
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
            #region 添加复合类数据
            string[] commonDirectoryFileArray = Directory.GetFiles(commonCompoundDataDirectoryPath);
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
                TextEditor = sender as TextEditor;
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    JsonTreeViewDataStructure result = htmlHelper.AnalyzeHTMLData(configDirectoryPath + CurrentVersion.Text);
                    TextEditor.Text = "{\r\n" + result.ResultString.ToString().TrimEnd([',', '\r', '\n']) + "\r\n}";
                    foreach (var item in result.Result)
                    {
                        if(item is CompoundJsonTreeViewItem compoundJsonTreeViewItem && compoundJsonTreeViewItem.Children.Count > 0)
                        {
                            JsonTool.SetParentForEachItem(compoundJsonTreeViewItem.Children, compoundJsonTreeViewItem);
                        }
                    }

                    JsonTool.SetLayerCountForEachItem(result.Result,1);
                    JsonTool.SetLineNumbersForEachItem(result.Result, null);
                    TreeViewItemList = result.Result;

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
    }
}