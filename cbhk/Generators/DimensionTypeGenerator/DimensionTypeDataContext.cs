using cbhk.CustomControls.JsonTreeViewComponents;
using cbhk.GeneralTools.TreeViewComponentsHelper;
using CommunityToolkit.Mvvm.ComponentModel;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Folding;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using ICSharpCode.AvalonEdit.Highlighting;
using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Xml;
using System.Threading.Tasks;
using cbhk.CustomControls.Interfaces;
using System.Threading;
using System.Windows.Controls;
using cbhk.GeneralTools;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;

namespace cbhk.Generators.DimensionTypeGenerator
{
    public class DimensionTypeDataContext:ObservableObject,ICustomWorldUnifiedPlan
    {
        #region Field
        public Window home = null;
        private string initRuleFilePath = AppDomain.CurrentDomain.BaseDirectory + @"resources\configs\DimensionType\data\Rules\1.20.4.json";
        private string configDirectoryPath = AppDomain.CurrentDomain.BaseDirectory + @"resources\configs\DimensionType\data\Rules";
        public TextEditor textEditor = null;
        private FoldingManager foldingManager = null;
        private bool IsLoaded = false;
        private bool IsFirstKeyDown = false;
        #endregion

        #region Property
        private ObservableCollection<JsonTreeViewItem> dimensionTypeItemsSource = [];
        public ObservableCollection<JsonTreeViewItem> DimensionTypeItemsSource
        {
            get => dimensionTypeItemsSource;
            set => SetProperty(ref dimensionTypeItemsSource, value);
        }
        public Dictionary<string, JsonTreeViewContext> KeyValueOffsetDictionary { get; set; } = [];
        #endregion

        /// <summary>
        /// 安装大纲、应用高亮规则
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public async void TextEditor_Loaded(object sender,RoutedEventArgs e)
        {
            //TextReader textReader = new StringReader(File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + @"\resources\configs\DimensionType\data\Templates\new dimension type.json"));
            //JsonTextReader jsonTextReader = new(textReader);

            await Task.Run(async () =>
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    textEditor = sender as TextEditor;

                    foreach (var item in Directory.GetFiles(configDirectoryPath))
                    {
                        string fileName = Path.GetFileNameWithoutExtension(item);
                        if (File.Exists(item) && fileName == "BlockTag" || fileName == "ItemTag")
                        {
                            string data = File.ReadAllText(item);
                            JArray jArray = JArray.Parse(data);
                            if (fileName == "BlockTag")
                                foreach (var tag in jArray)
                                {
                                    TreeViewRuleReader.BlockTagList.Add(tag.ToString());
                                }
                            else
                                foreach (var tag in jArray)
                                {
                                    TreeViewRuleReader.ItemTagList.Add(tag.ToString());
                                }
                        }
                    }
                    DataStructure result = TreeViewRuleReader.Read(initRuleFilePath);
                    KeyValueOffsetDictionary = result.Context;
                    DimensionTypeItemsSource = result.Result;
                    textEditor.Text = result.ResultString.ToString();

                    //为代码编辑器安装大纲管理器
                    foldingManager = FoldingManager.Install(textEditor.TextArea);
                    XshdSyntaxDefinition xshdSyntaxDefinition = new();
                    xshdSyntaxDefinition = HighlightingLoader.LoadXshd(new XmlTextReader(AppDomain.CurrentDomain.BaseDirectory + @"resources\configs\json.xshd"));
                    IHighlightingDefinition jsonHighlighting = HighlightingLoader.Load(xshdSyntaxDefinition, HighlightingManager.Instance);
                    textEditor.SyntaxHighlighting = jsonHighlighting;
                    IsLoaded = true;
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

        }

        public void UpdateValueBySpecifyingInterval(JsonViewInterval interval,string newValue)
        {
            textEditor.Select(interval.StartOffset, interval.EndOffset - interval.StartOffset);
            //textEditor.Document.Replace(interval.StartOffset,interval.EndOffset - interval.StartOffset,newValue);
        }

        public JsonTreeViewItem FindNodeBySpecifyingOffset(JsonViewInterval interval)
        {
            JsonTreeViewItem result = null;
            return result;
        }

        public bool VerifyCorrectnessLayerByLayer(JsonTreeViewItem currentItem)
        {
            bool result = false;
            return result;
        }

        public async void UpdateAllSubsequentOffsetMembers(JsonTreeViewContext context, JsonTreeViewItem currentItem,FrameworkElement frameworkElement)
        {
            int offsetDifference = (currentItem.Value + "").Length;
            JsonTreeViewItem parentItem = currentItem.Parent;
            ObservableCollection<JsonTreeViewItem> list = [];
            if (parentItem is null)
            {
                TreeView treeView = frameworkElement.FindParent<TreeView>();
                list = treeView.ItemsSource as ObservableCollection<JsonTreeViewItem>;
            }
            else
                list = parentItem.Children;
            CancellationToken cancellationToken = new();
            await Parallel.ForEachAsync(list, (item, cancellationToken) =>
            {
                if(context.KeyStartOffset > context.KeyEndOffset)
                {
                    context.KeyStartOffset += offsetDifference;
                    context.KeyEndOffset += offsetDifference;
                    context.ValueStartOffset += offsetDifference;
                    context.ValueEndOffset += offsetDifference;
                }
                return new ValueTask();
            });
        }

        public void UpdateAllSubsequentOffsetMembers(JsonTreeViewItem currentItem, FrameworkElement frameworkElement)
        {
            
        }
    }
}
