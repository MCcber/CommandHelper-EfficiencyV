using CBHK.Common.Model;
using CBHK.Common.Utility;
using CBHK.View.Component.Datapack.EditPage;
using CBHK.ViewModel.Component.Datapack.EditPage;
using CBHK.ViewModel.Generator;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Folding;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml;

namespace CBHK.CustomControl
{
    public partial class McfunctionIntellisenseCodeEditor:TextEditor
    {
        #region Field
        /// <summary>
        /// 数据上下文
        /// </summary>
        McfunctionIntellisenseDataContext dataContext = new();
        private CancellationTokenSource communiteTokenSource = new();
        private CancellationTokenSource ShortKeyToken = new();
        private RegexService regexService;
        FoldingManager foldingManager = null;
        private Socket client = null;
        double singleQuotation;
        double doubleQuotation;
        public SolidColorBrush lightBlueBrush = new((Color)ColorConverter.ConvertFromString("#78A2F5"));
        public SolidColorBrush grayBrush = new((Color)ColorConverter.ConvertFromString("#202020"));
        public SolidColorBrush whiteBrush = new((Color)ColorConverter.ConvertFromString("#FFFFFF"));
        public SolidColorBrush transparentBrush = null;
        string DropedString = "";
        int LastLineCount = 0;
        bool Droped = false;
        bool NoLongerNeedCompleton = false;
        Dictionary<int, MacroItem> MacroVariables = [];
        /// <summary>
        /// 分析起始索引
        /// </summary>
        public int AnalyzeStartingIndex = 0;
        /// <summary>
        /// 分析末尾索引
        /// </summary>
        public int AnalyzeEndingIndex = 0;
        /// <summary>
        /// 执行了粘贴
        /// </summary>
        public bool RunningPaste = false;
        /// <summary>
        /// 拖拽起始偏移量
        /// </summary>
        public int DropStartOffset = -1;
        /// <summary>
        /// 拖拽末尾偏移量
        /// </summary>
        public int DropEndOffset = -1;
        /// <summary>
        /// 补全框与它的显示状态
        /// </summary>
        public bool isCompletionWindowShowing = true;
        public bool IsCompletionWindowShowing
        {
            get => isCompletionWindowShowing;
            set
            {
                isCompletionWindowShowing = value;
                if (isCompletionWindowShowing)
                {
                    completionWindow.StartOffset = TextArea.Caret.Offset;
                    completionWindow.Show();
                }
                else
                if(completionWindow is not null)
                {
                    completionWindow.Close();
                }
            }
        }

        /// <summary>
        /// 补全数据源
        /// </summary>
        public ObservableCollection<CompletedItemData> CompletedSource { get; set; } = [];
        #endregion

        #region 补全框与相关属性
        /// <summary>
        /// 存储需要显示的补全数据数量
        /// </summary>
        private int DisplayCount = 0;
        /// <summary>
        /// 截取当前文本时不能被匹配的字符串
        /// </summary>
        public List<string> DonotMatches = [",", "[", "{", "(", "=", " "];
        /// <summary>
        /// 截取当前文本时可被缺省的字符串
        /// </summary>
        public List<string> Matches = [".", ":", "@"];
        CompletionWindow completionWindow;
        /// <summary>
        /// 列表视图对象,用于呈现成员和过滤成员
        /// </summary>
        public CollectionViewSource CompleteViewSource { get; set; } = new();
        /// <summary>
        /// 语法字典
        /// </summary>
        public Dictionary<string, Dictionary<string, List<SyntaxTreeItem>>> SyntaxItemDicionary = [];
        private EditPageViewModel editPageDataContext = null;
        #endregion

        #region 执行延迟
        //double executeIntellisenseDelay = 0.1;
        ///// <summary>
        ///// 等待补全执行的时间
        ///// </summary>
        //public double ExecuteIntellisenseDelay
        //{
        //    get => executeIntellisenseDelay;
        //    set => SetProperty(ref executeIntellisenseDelay, value);
        //}
        //private double executeShortKeyDelay = 0.2;
        ///// <summary>
        ///// 等待快捷键的时间
        ///// </summary>
        //public double ExecuteShortKeyDelay
        //{
        //    get => executeShortKeyDelay;
        //    set => SetProperty(ref executeShortKeyDelay, value);
        //}
        //double executeShortKeyDelay = 0.1;
        #endregion

        #region 图标引用
        ImageSource ReferenceIcon = Application.Current.Resources["ReferenceIcon"] as ImageSource;
        ImageSource LiteralIcon = Application.Current.Resources["LiteralIcon"] as ImageSource;
        ImageSource RadicalIcon = Application.Current.Resources["RadicalIcon"] as ImageSource;
        ImageSource CodeBlockIcon = Application.Current.Resources["CodeBlockIcon"] as ImageSource;
        ImageSource NameSpaceIcon = Application.Current.Resources["NameSpaceIcon"] as ImageSource;
        ImageSource MacrosIcon = Application.Current.Resources["MacrosIcon"] as ImageSource;
        #endregion

        public McfunctionIntellisenseCodeEditor(EditPageView editPageView,RegexService RegexService)
        {
            #region 设置属性、订阅事件
            AllowDrop = true;
            Foreground = whiteBrush;
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            LineNumbersForeground = whiteBrush;
            ShowLineNumbers = true;
            WordWrap = true;
            //为代码编辑器安装大纲管理器
            foldingManager = FoldingManager.Install(TextArea);
            XshdSyntaxDefinition xshdSyntaxDefinition = new();
            xshdSyntaxDefinition = HighlightingLoader.LoadXshd(new XmlTextReader(AppDomain.CurrentDomain.BaseDirectory + @"Resource\Configs\Common\" + "Mcfunction.xshd"));
            IHighlightingDefinition mcfunctionHighlighting = HighlightingLoader.Load(xshdSyntaxDefinition, HighlightingManager.Instance);
            SyntaxHighlighting = mcfunctionHighlighting;
            #region 初始化补全框
            CompleteViewSource.Source = CompletedSource;
            CompleteViewSource.Filter += CompletedViewSource_Filter;
            #endregion
            //安装折叠管理器，设置语法高亮文件，初始化补全框
            Loaded += TextBox_Loaded;
            //检测文本的拖拽，更新运行时变量
            PreviewDrop += TextEditor_PreviewDrop;
            //执行上下文分析
            TextChanged += TextEditor_TextChanged;
            //记录状态、Ctrl + Space触发上下文分析
            PreviewKeyDown += TextEditor_PreviewKeyDown;
            //补全快捷键
            KeyDown += TextEditor_KeyDown;
            //记录选中内容的起始和末尾偏移量
            PreviewKeyUp += TextEditor_PreviewKeyUp;
            #endregion

            regexService = regexService;

            editPageDataContext = editPageView.DataContext as EditPageViewModel;

            CompleteViewSource.Source = CompletedSource;

            Document.Changing += Document_Changing;
        }

        private void Document_Changing(object sender, DocumentChangeEventArgs e)
        {
            int offset = e.Offset; // 更改的起始位置
            int removedLength = e.RemovalLength; // 被删除的文本的长度
            string removedText = e.RemovedText.Text; // 被删除的文本
        }

        #region 与服务器交流

        /// <summary>
        /// 接收数据
        /// </summary>
        private async void ReceiveData()
        {
            // 连接到服务器
            try
            {
                communiteTokenSource = new();
                byte[] data = new byte[10240];
                //List<byte> result = [];
                int dataLength = 0;
                while (!communiteTokenSource.Token.IsCancellationRequested && client.Connected)
                {
                    // 读取服务器发送的数据
                    //while (true)
                    //{
                    dataLength = await client.ReceiveAsync(data);
                    //    if (dataLength == 0)
                    //        break;
                    //    result.AddRange(data);
                    //}
                    string message = Encoding.UTF8.GetString(data, 0, dataLength);
                    //result.Clear();
                    McfunctionIntellisenseDataContext context = JsonSerializer.Deserialize<McfunctionIntellisenseDataContext>(message);
                    dataContext = context;
                    IntellisenseService();
                    InitAndShowCompletionWindow();
                }
            }
            catch
            {
                communiteTokenSource.Cancel();
                //等待3秒后自动重连
                await Task.Delay(TimeSpan.FromSeconds(3));
                Thread thread = new(ReceiveData);
                thread.Start();
            }
        }

        /// <summary>
        /// 发送数据到服务器
        /// </summary>
        private async Task SendData()
        {
            if (client.Connected)
            {
                GetCurrentLineText();
                GetTypingText();
                Application.Current.Dispatcher.Invoke(CompletedSource.Clear);
                // 进行序列化
                string jsonString = JsonSerializer.Serialize(dataContext);
                byte[] data = Encoding.UTF8.GetBytes(jsonString);
                await client.SendAsync(data);
            }
        }
        #endregion

        #region Methods
        /// <summary>
        /// 初始化并显示补全窗体
        /// </summary>
        private void InitAndShowCompletionWindow()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                IsCompletionWindowShowing = false;
                completionWindow = new(TextArea)
                {
                    Background = grayBrush,
                    WindowStyle = WindowStyle.None,
                    Style = null,
                    ResizeMode = ResizeMode.NoResize,
                    MaxHeight = 250,
                    SizeToContent = SizeToContent.WidthAndHeight,
                    CloseWhenCaretAtBeginning = true
                };
                completionWindow.CompletionList.ListBox.ItemsSource = CompleteViewSource.View;
                completionWindow.PreviewKeyDown += CompletionWindow_PreviewKeyDown;
            });
            if (CompletedSource is not null && CompletedSource.Count > 0 && DisplayCount > 0)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    ScrollViewer.SetHorizontalScrollBarVisibility(completionWindow.CompletionList.ListBox, ScrollBarVisibility.Disabled);
                    ScrollViewer.SetVerticalScrollBarVisibility(completionWindow.CompletionList.ListBox, ScrollBarVisibility.Auto);
                    ListBox listBox = completionWindow.CompletionList.ListBox;
                    listBox.ItemsSource = CompleteViewSource.View;
                    CompleteViewSource.Source = CompletedSource;
                    listBox.Foreground = whiteBrush;
                    listBox.Background = transparentBrush;
                    listBox.SelectedIndex = 0;
                    listBox.SetValue(VirtualizingStackPanel.IsVirtualizingProperty, true);
                    listBox.SetValue(VirtualizingStackPanel.VirtualizationModeProperty, VirtualizationMode.Recycling);
                    completionWindow.StartOffset = TextArea.Caret.Offset;
                    IsCompletionWindowShowing = true;
                });
            }
            else
            {
                IsCompletionWindowShowing = false;
            }
        }

        /// <summary>
        /// 添加命令部首为补全数据
        /// </summary>
        private void AddCommandRadicalAndCodeSnippet()
        {
            Application.Current.Dispatcher.Invoke(CompletedSource.Clear);

            #region 添加大纲
            Application.Current.Dispatcher.Invoke(CompletedSource.Clear);
            CompletedItemData regionItemData = new()
            {
                Text = "#region ",
                Content = "region",
                Image = CodeBlockIcon
            };
            CompletedItemData endRegionItemData = new()
            {
                Text = "#endregion",
                Content = "endregion",
                Image = CodeBlockIcon
            };
            regionItemData.StatusUpdated += CompletedItemData_StatusUpdated;
            endRegionItemData.StatusUpdated += CompletedItemData_StatusUpdated;
            Application.Current.Dispatcher.Invoke(() =>
            {
                CompletedSource.Add(regionItemData);
                CompletedSource.Add(endRegionItemData);
            });
            DisplayCount = 2;
            #endregion
            #region 添加代码片段
            foreach (var item in editPageDataContext.CodeSnippetList)
            {
                CompletedItemData completedItemData = new()
                {
                    Text = item.Value,
                    Image = CodeBlockIcon,
                    Content = item.Key
                };
                completedItemData.CurrentContext = dataContext;
                completedItemData.StatusUpdated += CompletedItemData_StatusUpdated;
                Application.Current.Dispatcher.Invoke(() => { CompletedSource.Add(completedItemData); });
            }
            #endregion
            #region 添加命令部首
            foreach (string item in SyntaxItemDicionary.Keys)
            {
                CompletedItemData completedItemData = new()
                {
                    Text = item.Replace("Radical", ""),
                    Description = item.Replace("Radical", ""),
                    Image = RadicalIcon,
                    Content = item.Replace("Radical", "")
                };
                completedItemData.CurrentContext = dataContext;
                completedItemData.StatusUpdated += CompletedItemData_StatusUpdated;
                Application.Current.Dispatcher.Invoke(() => { CompletedSource.Add(completedItemData); });
            }
            #endregion
        }

        /// <summary>
        /// 补全节点成员去重比较器
        /// </summary>
        public class CompleteItemPropertyComparer : IEqualityComparer<SyntaxTreeItem>
        {
            public bool Equals(SyntaxTreeItem x, SyntaxTreeItem y)
            {
                if (x is not null && y is not null)
                    return x.Text == y.Text && x.Type == y.Type && x.Key == y.Key;
                else
                    return false;
            }

            public int GetHashCode([DisallowNull] SyntaxTreeItem obj)
            {
                return 0;
            }
        }

        /// <summary>
        /// 执行补全
        /// </summary>
        /// <returns></returns>
        private void IntellisenseService()
        {
            #region Macros
            if (dataContext.IsCompleteMacros)
            {
                dataContext.IsCompleteMacros = false;
                Application.Current.Dispatcher.Invoke(CompletedSource.Clear);
                foreach (var item in MacroVariables)
                {
                    CompletedItemData completedItemData = new()
                    {
                        Content = item.Value.Text,
                        Text = "$(" + item.Value.Text + ")",
                        Image = MacrosIcon,
                        Description = item.Value.Description
                    };
                    completedItemData.StatusUpdated += CompletedItemData_StatusUpdated;
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        CompletedSource.Add(completedItemData);
                    });
                }
                return;
            }
            #endregion

            #region OutLines
            if (dataContext.IsCompleteOutline)
            {
                dataContext.IsCompleteOutline = false;
                Application.Current.Dispatcher.Invoke(CompletedSource.Clear);
                CompletedItemData regionItemData = new()
                {
                    Text = "#region ",
                    Content = "region",
                    Image = CodeBlockIcon
                };
                CompletedItemData endRegionItemData = new()
                {
                    Text = "#endregion",
                    Content = "endregion",
                    Image = CodeBlockIcon
                };
                regionItemData.StatusUpdated += CompletedItemData_StatusUpdated;
                endRegionItemData.StatusUpdated += CompletedItemData_StatusUpdated;
                Application.Current.Dispatcher.Invoke(() =>
                {
                    CompletedSource.Add(regionItemData);
                    CompletedSource.Add(endRegionItemData);
                });
                DisplayCount = 2;
            }
            #endregion

            #region RuntimeVariables
            HandlingRuntimeVariables(dataContext);
            #endregion

            #region execute命令的重定向
            int lastCommandIndex = dataContext.CommandPath.LastIndexOf("commands");
            if (lastCommandIndex >= 0)
                dataContext.CommandPath = dataContext.CommandPath[lastCommandIndex..];
            if (dataContext.CommandPath.StartsWith("commands.execute") && Regex.Matches(dataContext.CommandPath, @"executeOptions").Count > 1)
                dataContext.CommandPath = "commands.execute." + dataContext.CommandPath[dataContext.CommandPath.LastIndexOf("executeOptions")..];
            #endregion

            #region 补全选择器参数
            if (dataContext.IsCompleteSelectorParameters)
            {
                dataContext.IsCompleteSelectorParameters = false;
                string inlineContext = dataContext.SelectorInlineContext;
                if (editPageDataContext.SelectorParameterValueList.TryGetValue(inlineContext, out string value))
                {
                    string[] values = value.Split('|');
                    CompletedSource.Clear();
                    for (int i = 0; i < values.Length; i++)
                    {
                        CompletedItemData completedItemData = new()
                        {
                            Text = values[i].ToString(),
                            Content = values[i],
                            Image = ReferenceIcon
                        };
                        Application.Current.Dispatcher.Invoke(() => { CompletedSource.Add(completedItemData); });
                    }
                }
                else
                {
                    CompletedSource.Clear();
                    if (!editPageDataContext.SelectorParameterValueTypes.Contains(inlineContext))
                        return;
                    switch (inlineContext)
                    {
                        case "Number":
                            {
                                CompletedItemData MinItemData = new()
                                {
                                    Content = int.MinValue.ToString(),
                                    Text = int.MinValue.ToString(),
                                    Image = LiteralIcon,
                                };
                                CompletedItemData MaxItemData = new()
                                {
                                    Content = int.MaxValue.ToString(),
                                    Text = int.MaxValue.ToString(),
                                    Image = LiteralIcon,
                                };
                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    CompletedSource.Add(MinItemData);
                                    CompletedSource.Add(MaxItemData);
                                });
                            }
                            break;
                        case "PositiveNumber":
                            {
                                CompletedItemData MinItemData = new()
                                {
                                    Content = "0",
                                    Text = "0",
                                    Image = LiteralIcon
                                };
                                CompletedItemData MaxItemData = new()
                                {
                                    Content = int.MaxValue.ToString(),
                                    Text = int.MaxValue.ToString(),
                                    Image = LiteralIcon
                                };
                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    CompletedSource.Add(MinItemData);
                                    CompletedSource.Add(MaxItemData);
                                });
                            }
                            break;
                        case "Double":
                            {
                                CompletedItemData MinItemData = new()
                                {
                                    Content = double.MinValue.ToString(),
                                    Text = double.MinValue.ToString(),
                                    Image = LiteralIcon,
                                };
                                CompletedItemData MaxItemData = new()
                                {
                                    Content = double.MaxValue.ToString(),
                                    Text = double.MaxValue.ToString(),
                                    Image = LiteralIcon,
                                };
                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    CompletedSource.Add(MinItemData);
                                    CompletedSource.Add(MaxItemData);
                                });
                            }
                            break;
                        case "DoubleInterval":
                            {
                                CompletedItemData MinItemData = new()
                                {
                                    Content = double.MinValue.ToString(),
                                    Text = double.MinValue.ToString(),
                                    Image = LiteralIcon,
                                };
                                CompletedItemData MaxItemData = new()
                                {
                                    Content = double.MaxValue.ToString(),
                                    Text = double.MaxValue.ToString(),
                                    Image = LiteralIcon,
                                };
                                CompletedItemData MoreThanMinItemData = new()
                                {
                                    Content = double.MinValue + "..",
                                    Text = double.MinValue + "..",
                                    Image = LiteralIcon,
                                };
                                CompletedItemData LessThanMaxItemData = new()
                                {
                                    Content = ".." + double.MaxValue,
                                    Text = ".." + double.MaxValue,
                                    Image = LiteralIcon,
                                };
                                CompletedItemData RangeItemData = new()
                                {
                                    Content = double.MinValue + ".." + double.MaxValue,
                                    Text = double.MinValue + ".." + double.MaxValue,
                                    Image = LiteralIcon,
                                };
                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    CompletedSource.Add(MinItemData);
                                    CompletedSource.Add(MaxItemData);
                                    CompletedSource.Add(MoreThanMinItemData);
                                    CompletedSource.Add(LessThanMaxItemData);
                                    CompletedSource.Add(RangeItemData);
                                });
                            }
                            break;
                        case "PositiveDouble":
                            {
                                CompletedItemData MinItemData = new()
                                {
                                    Content = "0",
                                    Text = "0",
                                    Image = LiteralIcon,
                                };
                                CompletedItemData MaxItemData = new()
                                {
                                    Content = double.MaxValue.ToString(),
                                    Text = double.MaxValue.ToString(),
                                    Image = LiteralIcon,
                                };
                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    CompletedSource.Add(MinItemData);
                                    CompletedSource.Add(MaxItemData);
                                });
                            }
                            break;
                        case "PositiveDoubleInterval":
                            {
                                CompletedItemData MinItemData = new()
                                {
                                    Content = "0",
                                    Text = "0",
                                    Image = LiteralIcon,
                                };
                                CompletedItemData MaxItemData = new()
                                {
                                    Content = double.MaxValue.ToString(),
                                    Text = double.MaxValue.ToString(),
                                    Image = LiteralIcon,
                                };
                                CompletedItemData MoreThanMinItemData = new()
                                {
                                    Content = "0..",
                                    Text = "0..",
                                    Image = LiteralIcon,
                                };
                                CompletedItemData LessThanMaxItemData = new()
                                {
                                    Content = ".." + double.MaxValue,
                                    Text = ".." + double.MaxValue,
                                    Image = LiteralIcon,
                                };
                                CompletedItemData RangeItemData = new()
                                {
                                    Content = "0.." + double.MaxValue,
                                    Text = "0.." + double.MaxValue,
                                    Image = LiteralIcon,
                                };
                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    CompletedSource.Add(MinItemData);
                                    CompletedSource.Add(MaxItemData);
                                    CompletedSource.Add(MoreThanMinItemData);
                                    CompletedSource.Add(LessThanMaxItemData);
                                    CompletedSource.Add(RangeItemData);
                                });
                            }
                            break;
                        case "Int":
                            {
                                CompletedItemData intMinItemData = new()
                                {
                                    Content = int.MinValue.ToString(),
                                    Text = int.MinValue.ToString(),
                                    Image = LiteralIcon,
                                };
                                CompletedItemData intMaxItemData = new()
                                {
                                    Content = int.MaxValue.ToString(),
                                    Text = int.MaxValue.ToString(),
                                    Image = LiteralIcon,
                                };
                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    CompletedSource.Add(intMinItemData);
                                    CompletedSource.Add(intMaxItemData);
                                });
                            }
                            break;

                        case "AdvancementsValue":
                        case "PredicateValue":
                            {

                                foreach (string item in editPageDataContext.ResourceFilePathes[inlineContext])
                                {
                                    CompletedItemData completedItemData = new()
                                    {
                                        Content = item,
                                        Text = item,
                                        Image = ReferenceIcon,
                                    };
                                    Application.Current.Dispatcher.Invoke(() => { CompletedSource.Add(completedItemData); });
                                }
                            }
                            break;
                        case "EntityId":
                            foreach (string item in editPageDataContext.EntityIDList)
                            {
                                CompletedItemData completedItemData = new()
                                {
                                    Content = item,
                                    Text = item,
                                    Image = ReferenceIcon,
                                };
                                Application.Current.Dispatcher.Invoke(() => { CompletedSource.Add(completedItemData); });
                            }
                            break;
                        case "NameValue":
                            CompletedItemData steveItemData = new()
                            {
                                Content = int.MinValue.ToString(),
                                Text = int.MinValue.ToString(),
                                Image = ReferenceIcon,
                            };
                            CompletedItemData alexItemData = new()
                            {
                                Content = int.MaxValue.ToString(),
                                Text = int.MaxValue.ToString(),
                                Image = ReferenceIcon,
                            };
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                CompletedSource.Add(steveItemData);
                                CompletedSource.Add(alexItemData);
                            });
                            break;

                        case "TargetObjective":
                        case "TagValue":
                        case "TeamValue":
                            foreach (var item in editPageDataContext.RuntimeVariables[inlineContext].Values)
                            {
                                CompletedItemData completedItemData = new()
                                {
                                    Content = item,
                                    Text = item,
                                    Image = ReferenceIcon,
                                };
                                Application.Current.Dispatcher.Invoke(() => { CompletedSource.Add(completedItemData); });
                            }
                            break;
                    }
                }
                return;
            }
            else
            if (dataContext.IsCompleteSelectorParameterValues)
            {
                dataContext.IsCompleteSelectorParameterValues = false;
                CompletedSource.Clear();
                foreach (string item in editPageDataContext.SelectorParameterList)
                {
                    CompletedItemData completedItemData = new()
                    {
                        Text = item + "=",
                        Content = item,
                        Image = ReferenceIcon
                    };
                    if (editPageDataContext.CompoundSelectorParameters.Contains(item))
                    {
                        completedItemData.Text += "{}";
                        completedItemData.IsCompoundSelectorParameter = true;
                    }

                    Application.Current.Dispatcher.Invoke(() => { CompletedSource.Add(completedItemData); });
                }
                return;
            }
            #endregion

            #region 处理残缺的坐标
            if (dataContext.IsCompletePos)
            {
                dataContext.IsCompletePos = false;
                CompletedSource.Clear();
                for (int i = 0; i < dataContext.CoordinateCount; i++)
                {
                    CompletedItemData completedItemData1 = new()
                    {
                        Image = LiteralIcon
                    };
                    CompletedItemData completedItemData2 = new()
                    {
                        Image = LiteralIcon,
                        Content = i > 0 ? "^ ^" : "^",
                        Text = i > 0 ? "^ ^" : "^"
                    };
                    if (dataContext.CoordinateType == "Relative")
                    {
                        completedItemData1.Content = i > 0 ? "~ ~" : "~";
                        completedItemData1.Text = i > 0 ? "~ ~" : "~";
                    }
                    else
                    if (dataContext.CoordinateType == "Local")
                    {
                        completedItemData1.Content = i > 0 ? "^ ^" : "^";
                        completedItemData1.Text = i > 0 ? "^ ^" : "^";
                    }
                    else
                    {
                        completedItemData1.Content = i > 0 ? "~ ~" : "~";
                        completedItemData1.Text = i > 0 ? "~ ~" : "~";
                        completedItemData2.Content = i > 0 ? "^ ^" : "^";
                        completedItemData2.Text = i > 0 ? "^ ^" : "^";

                        CompletedSource.Add(completedItemData2);
                    }

                    CompletedSource.Add(completedItemData1);
                }
                dataContext.CoordinateCount = 0;
                return;
            }
            #endregion

            #region 处理坐标与关键字同层的情况
            dataContext.CommandPath = dataContext.CommandPath.Replace("cloneFrom.pos3D", "cloneFrom")
                .Replace("cloneTo.pos3D", "cloneTo");
            #endregion

            #region 计算补全数据
            if (dataContext.CommandPath != "commands." && dataContext.CommandPath.Replace("commands.", "").Trim().Length > 0)
            {
                #region 搜索命令路径下对应的节点集合并给出补全数据
                Application.Current.Dispatcher.Invoke(CompletedSource.Clear);
                string commandRadical = dataContext.CommandPath[9..(dataContext.CommandPath[9..].IndexOf('.') + 9)] + "Radical";
                //处理补全数据
                if (dataContext.CommandPath.Contains('.') && SyntaxItemDicionary.TryGetValue(commandRadical, out Dictionary<string, List<SyntaxTreeItem>> radical) && radical.TryGetValue(dataContext.CommandPath, out List<SyntaxTreeItem> targetItems))
                {
                    #region 判断是否已完成语法树的补全
                    List<bool> everyItemMatches = [];
                    dataContext.IsCompletionOver = false;
                    bool haveChildren = false;
                    int childrenCount = 0;
                    for (int i = 0; i < targetItems.Count; i++)
                        childrenCount += targetItems[i].Children.Count;
                    haveChildren = childrenCount > 0;
                    #endregion
                    //显示数归零
                    DisplayCount = 0;
                    if (dataContext.IsNeedCalculate)
                    {
                        if (targetItems.Count > 1)
                            targetItems = targetItems.Distinct(new CompleteItemPropertyComparer()).ToList();

                        foreach (SyntaxTreeItem targetItem in targetItems)
                        {
                            #region 字面节点直接添加即可
                            if (targetItem.Type == SyntaxTreeItem.SyntaxTreeItemType.Literal)
                            {
                                List<string> literals = [.. targetItem.Text.Split('|')];
                                foreach (string literal in literals)
                                {
                                    CompletedItemData completedItemData = new()
                                    {
                                        Text = literal,
                                        Content = literal,
                                        Description = targetItem.Description,
                                        Image = LiteralIcon
                                    };

                                    Application.Current.Dispatcher.Invoke(() => { CompletedSource.Add(completedItemData); });
                                }
                                bool isLiteral = targetItem.Text.StartsWith(dataContext.TypingContent);
                                if (isLiteral)
                                    everyItemMatches.Add(isLiteral);
                            }
                            #endregion
                            #region 引用型节点需要添加对应的数据源
                            if (targetItem.Type == SyntaxTreeItem.SyntaxTreeItemType.Reference)
                            {
                                switch (targetItem.Text)
                                {
                                    case "commandList":
                                        AddCommandRadicalAndCodeSnippet();
                                        break;
                                    case "particleId":
                                        foreach (string particleId in editPageDataContext.ParticleIDList)
                                        {
                                            CompletedItemData completedItemData = new()
                                            {
                                                Image = ReferenceIcon,
                                                Text = particleId,
                                                Content = particleId,
                                                Description = targetItem.Description
                                            };


                                            Application.Current.Dispatcher.Invoke(() => { Application.Current.Dispatcher.Invoke(() => { CompletedSource.Add(completedItemData); }); });
                                        }
                                        break;
                                    case "effectID":
                                        foreach (string effect in editPageDataContext.EffectIDList)
                                        {
                                            CompletedItemData completedItemData = new()
                                            {
                                                Text = effect,
                                                Content = effect,
                                                Description = targetItem.Description,
                                                Image = ReferenceIcon
                                            };


                                            Application.Current.Dispatcher.Invoke(() => { CompletedSource.Add(completedItemData); });
                                        }
                                        break;
                                    case "lootTool":
                                        foreach (string lootTool in editPageDataContext.LootToolList)
                                        {
                                            CompletedItemData completedItemData = new()
                                            {
                                                Text = lootTool,
                                                Content = lootTool,
                                                Description = targetItem.Description,
                                                Image = ReferenceIcon
                                            };


                                            Application.Current.Dispatcher.Invoke(() => { CompletedSource.Add(completedItemData); });
                                        }
                                        break;
                                    case "itemSlot":
                                        foreach (string itemSlot in editPageDataContext.ItemSlotList)
                                        {
                                            CompletedItemData completedItemData = new()
                                            {
                                                Text = itemSlot,
                                                Content = itemSlot,
                                                Image = ReferenceIcon,
                                                Description = targetItem.Description
                                            };


                                            Application.Current.Dispatcher.Invoke(() => { CompletedSource.Add(completedItemData); });
                                        }
                                        break;
                                    case "enchantID":
                                        {
                                            List<string> enchantIDList = [.. editPageDataContext.EnchantmentIDAndNameGroupByVersionMap.SelectMany(item => item.Value).Select(item => item.Key)];
                                            foreach (var enchantment in enchantIDList)
                                            {
                                                CompletedItemData completedItemData = new()
                                                {
                                                    Text = enchantment,
                                                    Content = enchantment,
                                                    Image = ReferenceIcon,
                                                    Description = targetItem.Description
                                                };


                                                Application.Current.Dispatcher.Invoke(() => { CompletedSource.Add(completedItemData); });
                                            }
                                        }
                                        break;
                                    case "damageType":
                                        foreach (string DamageType in editPageDataContext.DamageTypeList)
                                        {
                                            CompletedItemData completedItemData = new()
                                            {
                                                Text = DamageType,
                                                Content = DamageType,
                                                Image = ReferenceIcon,
                                                Description = targetItem.Description
                                            };


                                            Application.Current.Dispatcher.Invoke(() => { CompletedSource.Add(completedItemData); });
                                        }
                                        break;
                                    case "bossbarColor":
                                        foreach (string bossbarColor in editPageDataContext.BossbarColorList)
                                        {
                                            CompletedItemData completedItemData = new()
                                            {
                                                Text = bossbarColor,
                                                Content = bossbarColor,
                                                Image = LiteralIcon,
                                                Description = targetItem.Description
                                            };


                                            Application.Current.Dispatcher.Invoke(() => { CompletedSource.Add(completedItemData); });
                                        }
                                        break;
                                    case "bossbarStyle":
                                        foreach (string bossbarStyle in editPageDataContext.BossbarStyles)
                                        {
                                            CompletedItemData completedItemData = new()
                                            {
                                                Text = bossbarStyle,
                                                Content = bossbarStyle,
                                                Image = LiteralIcon,
                                                Description = targetItem.Description
                                            };


                                            Application.Current.Dispatcher.Invoke(() => { CompletedSource.Add(completedItemData); });
                                        }
                                        break;
                                    case "TeamColorList":
                                        foreach (string teamColor in editPageDataContext.TeamColorList)
                                        {
                                            CompletedItemData completedItemData = new()
                                            {
                                                Text = teamColor,
                                                Content = teamColor,
                                                Image = LiteralIcon,
                                                Description = targetItem.Description
                                            };


                                            Application.Current.Dispatcher.Invoke(() => { CompletedSource.Add(completedItemData); });
                                        }
                                        break;
                                    case "singleSelector":
                                        foreach (string singleSelector in editPageDataContext.singleSelectors)
                                        {
                                            CompletedItemData completedItemData = new()
                                            {
                                                Text = singleSelector,
                                                Content = singleSelector,
                                                Image = ReferenceIcon,
                                                Description = targetItem.Description
                                            };


                                            Application.Current.Dispatcher.Invoke(() => { CompletedSource.Add(completedItemData); });
                                        }
                                        break;

                                    case "optionalSelector":
                                    case "selector":
                                        foreach (string selector in editPageDataContext.selectors)
                                        {
                                            CompletedItemData completedItemData = new()
                                            {
                                                Text = selector,
                                                Content = selector,
                                                Image = ReferenceIcon,
                                            };


                                            Application.Current.Dispatcher.Invoke(() => { CompletedSource.Add(completedItemData); });
                                        }
                                        break;

                                    case "itemId":
                                        foreach (string itemId in editPageDataContext.ItemIDList)
                                        {
                                            CompletedItemData completedItemData = new()
                                            {
                                                Text = itemId,
                                                Content = itemId,
                                                Image = ReferenceIcon,
                                            };


                                            Application.Current.Dispatcher.Invoke(() => { CompletedSource.Add(completedItemData); });
                                        }
                                        break;
                                    case "blockId":
                                        foreach (string blockId in editPageDataContext.BlockIDList)
                                        {
                                            CompletedItemData completedItemData = new()
                                            {
                                                Text = blockId,
                                                Content = blockId,
                                                Image = ReferenceIcon,
                                            };


                                            Application.Current.Dispatcher.Invoke(() => { CompletedSource.Add(completedItemData); });
                                        }
                                        break;
                                    case "entityId":
                                        foreach (string entity in editPageDataContext.EntityIDList)
                                        {
                                            CompletedItemData completedItemData = new()
                                            {
                                                Text = entity,
                                                Content = entity,
                                                Image = ReferenceIcon,
                                            };


                                            Application.Current.Dispatcher.Invoke(() => { CompletedSource.Add(completedItemData); });
                                        }
                                        break;
                                    case "soundId":
                                        List<string> soundIDList = [..editPageDataContext.SoundIDAndNameMap.Select(item=>item.Value)];
                                        foreach (var sound in soundIDList)
                                        {
                                            CompletedItemData completedItemData = new()
                                            {
                                                Text = sound,
                                                Content = sound,
                                                Image = ReferenceIcon,
                                            };


                                            Application.Current.Dispatcher.Invoke(() => { CompletedSource.Add(completedItemData); });
                                        }
                                        break;

                                    case "predicateValue":
                                    case "advancementValue":
                                        foreach (string filePath in editPageDataContext.ResourceFilePathes[targetItem.Text])
                                        {
                                            CompletedItemData completedItemData = new()
                                            {
                                                Text = filePath,
                                                Content = filePath,
                                                Image = ReferenceIcon,
                                            };


                                            Application.Current.Dispatcher.Invoke(() => { CompletedSource.Add(completedItemData); });
                                        }
                                        break;
                                    case "mobAttribute":
                                        foreach (string mobAttribute in editPageDataContext.MobAttributeIDList)
                                        {
                                            CompletedItemData completedItemData = new()
                                            {
                                                Text = mobAttribute,
                                                Content = mobAttribute,
                                                Image = ReferenceIcon,
                                            };

                                            Application.Current.Dispatcher.Invoke(() => { CompletedSource.Add(completedItemData); });
                                        }
                                        break;
                                    case "scoreboardType":
                                        string ignoreNameSpace = "";
                                        ignoreNameSpace = dataContext.TypingContent.Replace(".minecraft", "").Replace("minecraft.", "");
                                        int lastDotIndex = ignoreNameSpace.LastIndexOf('.');
                                        if (lastDotIndex != -1)
                                            ignoreNameSpace = ignoreNameSpace[..lastDotIndex];
                                        for (int i = 0; i < editPageDataContext.ScoreboardTypeList.Count; i++)
                                        {
                                            string type = editPageDataContext.ScoreboardTypeList[i].Replace("minecraft", "").Replace(".", "").Replace(":", "");
                                            int bracketIndex = type.IndexOf('{');
                                            string inlineType = Regex.Match(type, @"(?<={)[a-zA-Z]+(?=})").ToString();
                                            if (bracketIndex != -1)
                                                type = type[..bracketIndex];
                                            if (type.StartsWith(ignoreNameSpace) && type.Length > ignoreNameSpace.Length)
                                            {
                                                int startIndex = editPageDataContext.ScoreboardTypeList[i].IndexOf('.') + 1;
                                                int endIndex = editPageDataContext.ScoreboardTypeList[i].LastIndexOf(':');
                                                if (editPageDataContext.ScoreboardTypeList[i].Contains('{') && endIndex == -1)
                                                {
                                                    startIndex = 0;
                                                    endIndex = editPageDataContext.ScoreboardTypeList[i].LastIndexOf('{') - 1;
                                                }
                                                if (endIndex <= 0)
                                                    endIndex = editPageDataContext.ScoreboardTypeList[i].Length;
                                                CompletedItemData completedItemData = new()
                                                {
                                                    Content = editPageDataContext.ScoreboardTypeList[i][startIndex..endIndex],
                                                    Text = editPageDataContext.ScoreboardTypeList[i][startIndex..endIndex],
                                                    Image = ReferenceIcon,
                                                };
                                                Application.Current.Dispatcher.Invoke(() => { CompletedSource.Add(completedItemData); });
                                            }
                                            else
                                                if (type.StartsWith(ignoreNameSpace) && type.Length <= ignoreNameSpace.Length)
                                            {
                                                switch (inlineType)
                                                {
                                                    case "teamColor":
                                                        foreach (string TeamColor in editPageDataContext.TeamColorList)
                                                        {
                                                            if (dataContext.TypingContent.TrimEnd('.').EndsWith(TeamColor))
                                                            {
                                                                CompletedSource.Clear();
                                                                break;
                                                            }
                                                            int dotIndex = dataContext.TypingContent.LastIndexOf('.') + 1;
                                                            string ignoreItem = "";
                                                            if (TeamColor.StartsWith(dataContext.TypingContent[dotIndex..]))
                                                                ignoreItem = dataContext.TypingContent + TeamColor[dataContext.TypingContent[dotIndex..].Length..];
                                                            if (ignoreItem == "")
                                                                ignoreItem = TeamColor;
                                                            CompletedItemData completedItemData = new()
                                                            {
                                                                Content = ignoreItem,
                                                                Text = ignoreItem,
                                                                Image = LiteralIcon,
                                                            };
                                                            Application.Current.Dispatcher.Invoke(() => { CompletedSource.Add(completedItemData); });
                                                        }
                                                        break;
                                                    case "itemId":
                                                        foreach (string ItemId in editPageDataContext.ItemIDList)
                                                        {
                                                            if (dataContext.TypingContent.TrimEnd('.').EndsWith(ItemId))
                                                            {
                                                                CompletedSource.Clear();
                                                                break;
                                                            }
                                                            int dotIndex = dataContext.TypingContent.LastIndexOf('.') + 1;
                                                            string ignoreItem = "";
                                                            if (ItemId.StartsWith(dataContext.TypingContent[dotIndex..]))
                                                                ignoreItem = dataContext.TypingContent + ItemId[dataContext.TypingContent[dotIndex..].Length..];
                                                            if (ignoreItem == "")
                                                                ignoreItem = ItemId;
                                                            CompletedItemData completedItemData = new()
                                                            {
                                                                Content = ignoreItem,
                                                                Text = ignoreItem,
                                                                Image = ReferenceIcon,
                                                            };
                                                            Application.Current.Dispatcher.Invoke(() => { CompletedSource.Add(completedItemData); });
                                                        }
                                                        break;
                                                    case "entityId":
                                                        foreach (string EntityId in editPageDataContext.EntityIDList)
                                                        {
                                                            if (dataContext.TypingContent.TrimEnd('.').EndsWith(EntityId))
                                                            {
                                                                CompletedSource.Clear();
                                                                break;
                                                            }
                                                            int dotIndex = dataContext.TypingContent.LastIndexOf('.') + 1;
                                                            string ignoreItem = "";
                                                            if (EntityId.StartsWith(dataContext.TypingContent[dotIndex..]))
                                                                ignoreItem = dataContext.TypingContent + EntityId[dataContext.TypingContent[dotIndex..].Length..];
                                                            if (ignoreItem == "")
                                                                ignoreItem = EntityId;
                                                            CompletedItemData completedItemData = new()
                                                            {
                                                                Content = ignoreItem,
                                                                Text = ignoreItem,
                                                                Image = ReferenceIcon,
                                                            };

                                                            Application.Current.Dispatcher.Invoke(() => { CompletedSource.Add(completedItemData); });
                                                        }
                                                        break;
                                                    case "customId":
                                                        foreach (string ScoreboardCustomId in editPageDataContext.ScoreboardCustomIDList)
                                                        {
                                                            if (dataContext.TypingContent.TrimEnd('.').EndsWith(ScoreboardCustomId))
                                                            {
                                                                CompletedSource.Clear();
                                                                break;
                                                            }
                                                            int dotIndex = dataContext.TypingContent.LastIndexOf('.') + 1;
                                                            string ignoreItem = "";
                                                            if (ScoreboardCustomId.StartsWith(dataContext.TypingContent[dotIndex..]))
                                                                ignoreItem = dataContext.TypingContent + ScoreboardCustomId[dataContext.TypingContent[dotIndex..].Length..];
                                                            if (ignoreItem == "")
                                                                ignoreItem = ScoreboardCustomId;
                                                            CompletedItemData completedItemData = new()
                                                            {
                                                                Content = ignoreItem,
                                                                Text = ignoreItem,
                                                                Image = ReferenceIcon,
                                                            };

                                                            Application.Current.Dispatcher.Invoke(() => { CompletedSource.Add(completedItemData); });
                                                        }
                                                        break;
                                                }
                                            }
                                        }

                                        if (!dataContext.TypingContent.EndsWith("minecraft.") || dataContext.TypingContent == "")
                                        {
                                            CompletedItemData nameSpaceItemData = new()
                                            {
                                                Text = "minecraft.",
                                                Content = "minecraft",
                                                Image = NameSpaceIcon,
                                            };
                                            Application.Current.Dispatcher.Invoke(() => { CompletedSource.Add(nameSpaceItemData); });

                                        }
                                        break;
                                    case "dimensionId":
                                        foreach (string dimension in editPageDataContext.DimensionIDList)
                                        {
                                            CompletedItemData completedItemData = new()
                                            {
                                                Text = dimension,
                                                Content = dimension,
                                                Image = ReferenceIcon,
                                            };

                                            Application.Current.Dispatcher.Invoke(() => { CompletedSource.Add(completedItemData); });
                                        }
                                        break;
                                    case "gameruleName":
                                        foreach (var gamerule in editPageDataContext.GameRuleMap)
                                        {
                                            CompletedItemData completedItemData = new()
                                            {
                                                Text = gamerule.Key,
                                                Content = gamerule.Key,
                                                Image = ReferenceIcon,
                                            };
                                            Application.Current.Dispatcher.Invoke(() => { CompletedSource.Add(completedItemData); });
                                        }
                                        break;

                                    case "bossbarID":
                                    case "targetObjective":
                                    case "storageID":
                                    case "tagValue":
                                    case "triggerObjective":
                                        foreach (var value in editPageDataContext.RuntimeVariables[targetItem.Text].Values)
                                        {
                                            CompletedItemData completedItemData = new()
                                            {
                                                Text = value,
                                                Content = value,
                                                Image = ReferenceIcon,
                                            };
                                            Application.Current.Dispatcher.Invoke(() => { CompletedSource.Add(completedItemData); });
                                        }
                                        break;
                                }
                            }
                            #endregion
                            #region 数据型节点给出数据的常用枚举
                            if (targetItem.Type == SyntaxTreeItem.SyntaxTreeItemType.DataType)
                            {
                                switch (targetItem.Text)
                                {
                                    #region 基本数据类型
                                    case "bool":
                                        {
                                            CompletedItemData trueItemData = new()
                                            {
                                                Text = "true",
                                                Content = "true",
                                                Description = targetItem.Description,
                                                Image = LiteralIcon,
                                            };
                                            CompletedItemData falseItemData = new()
                                            {
                                                Text = "false",
                                                Content = "false",
                                                Description = targetItem.Description,
                                                Image = LiteralIcon,
                                            };
                                            Application.Current.Dispatcher.Invoke(() => { CompletedSource.Add(trueItemData); CompletedSource.Add(falseItemData); });
                                            break;
                                        }
                                    case "byte":
                                        foreach (byte data in editPageDataContext.bytes)
                                        {
                                            CompletedItemData completedItemData = new()
                                            {
                                                Image = LiteralIcon,
                                                Text = data.ToString(),
                                                Content = data.ToString()
                                            };

                                            Application.Current.Dispatcher.Invoke(() => { CompletedSource.Add(completedItemData); });
                                        }
                                        break;
                                    case "short":
                                        foreach (short data in editPageDataContext.shorts)
                                        {
                                            CompletedItemData completedItemData = new()
                                            {
                                                Image = LiteralIcon,
                                                Text = data.ToString(),
                                                Content = data.ToString()
                                            };

                                            Application.Current.Dispatcher.Invoke(() => { CompletedSource.Add(completedItemData); });
                                        }
                                        break;
                                    case "int":
                                        foreach (int data in editPageDataContext.ints)
                                        {
                                            CompletedItemData completedItemData = new()
                                            {
                                                Image = LiteralIcon,
                                                Text = data.ToString(),
                                                Content = data.ToString()
                                            };

                                            Application.Current.Dispatcher.Invoke(() => { CompletedSource.Add(completedItemData); });
                                        }
                                        break;
                                    case "intRange":
                                        {
                                            CompletedItemData completedItemData = new()
                                            {
                                                Image = LiteralIcon,
                                                Text = "-2147483648..2147483647",
                                                Content = "-2147483648..2147483647"
                                            };

                                            Application.Current.Dispatcher.Invoke(() => { CompletedSource.Add(completedItemData); });
                                            break;
                                        }
                                    case "long":
                                        foreach (long data in editPageDataContext.longs)
                                        {
                                            CompletedItemData completedItemData = new()
                                            {
                                                Image = LiteralIcon,
                                                Text = data.ToString(),
                                                Content = data.ToString()
                                            };

                                            Application.Current.Dispatcher.Invoke(() => { CompletedSource.Add(completedItemData); });
                                        }
                                        break;
                                    case "float":
                                        foreach (float data in editPageDataContext.floats)
                                        {
                                            CompletedItemData completedItemData = new()
                                            {
                                                Image = LiteralIcon,
                                                Text = data.ToString(),
                                                Content = data.ToString()
                                            };

                                            Application.Current.Dispatcher.Invoke(() => { CompletedSource.Add(completedItemData); });
                                        }
                                        break;
                                    case "double":
                                        foreach (double data in editPageDataContext.doubles)
                                        {
                                            CompletedItemData completedItemData = new()
                                            {
                                                Image = LiteralIcon,
                                                Text = data.ToString(),
                                                Content = data.ToString()
                                            };

                                            Application.Current.Dispatcher.Invoke(() => { CompletedSource.Add(completedItemData); });
                                        }
                                        break;
                                    #endregion
                                    #region 特殊数据类型
                                    case "axes":
                                        foreach (string axe in editPageDataContext.axesTypes)
                                        {
                                            CompletedItemData completedItemData = new()
                                            {
                                                Image = ReferenceIcon,
                                                Text = axe,
                                                Content = axe,
                                                Description = targetItem.Description
                                            };

                                            Application.Current.Dispatcher.Invoke(() => { CompletedSource.Add(completedItemData); });
                                        }
                                        break;
                                    case "pos3D":
                                        foreach (string pos3D in editPageDataContext.pos3DTypes)
                                        {
                                            CompletedItemData completedItemData = new()
                                            {
                                                Image = ReferenceIcon,
                                                Text = pos3D,
                                                Content = pos3D,
                                                Description = targetItem.Description
                                            };

                                            Application.Current.Dispatcher.Invoke(() => { CompletedSource.Add(completedItemData); });
                                        }
                                        break;
                                    case "pos2D":
                                        foreach (string pos2D in editPageDataContext.pos2DTypes)
                                        {
                                            CompletedItemData completedItemData = new()
                                            {
                                                Image = ReferenceIcon,
                                                Text = pos2D,
                                                Content = pos2D,
                                                Description = targetItem.Description
                                            };

                                            Application.Current.Dispatcher.Invoke(() => { CompletedSource.Add(completedItemData); });
                                        }
                                        break;
                                    case "gameruleValue":
                                        {
                                            GameRuleItem.DataType dataType = GameRuleItem.DataType.Bool;
                                            GameRuleItem currentGameRule = new();
                                            foreach (var gamerule in editPageDataContext.GameRuleMap)
                                            {
                                                if (gamerule.Key == dataContext.CurrentGameRuleName)
                                                {
                                                    currentGameRule = gamerule.Value;
                                                    dataType = gamerule.Value.ItemType;
                                                    break;
                                                }
                                            }
                                            if (dataType == GameRuleItem.DataType.Int)
                                            {
                                                CompletedItemData completedItemData = new()
                                                {
                                                    Text = currentGameRule.Value,
                                                    Content = currentGameRule.Value,
                                                    Image = LiteralIcon,
                                                    Description = targetItem.Description
                                                };

                                                Application.Current.Dispatcher.Invoke(() => { CompletedSource.Add(completedItemData); });
                                            }
                                            else
                                            {
                                                CompletedItemData trueItemData = new()
                                                {
                                                    Text = "true",
                                                    Content = "true",
                                                    Image = LiteralIcon,
                                                    Description = targetItem.Description
                                                };
                                                CompletedItemData falseItemData = new()
                                                {
                                                    Text = "false",
                                                    Content = "false",
                                                    Image = LiteralIcon,
                                                    Description = targetItem.Description
                                                };
                                                Application.Current.Dispatcher.Invoke(() => { CompletedSource.Add(trueItemData); CompletedSource.Add(falseItemData); });
                                            }
                                            break;
                                        }
                                        #endregion
                                }
                            }
                            #endregion
                        }
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            foreach (var item in CompletedSource)
                            {
                                item.CurrentContext = dataContext;
                                item.StatusUpdated += CompletedItemData_StatusUpdated;
                            }
                        });
                    }
                }
                #endregion
            }
            else
                AddCommandRadicalAndCodeSnippet();
            #endregion
        }
        #endregion

        #region Events
        /// <summary>
        /// 执行快捷键
        /// </summary>
        /// <param name="key">需要补全的字符</param>
        /// <param name="e">按下的键</param>
        private void ExecuteShortKey(string key, KeyEventArgs e)
        {
            if (!ShortKeyToken.IsCancellationRequested)
            {
                e.Handled = true;
                Document.Insert(TextArea.Caret.Offset, key);
                TextArea.Caret.Offset--;
            }
        }

        /// <summary>
        /// 处理运行时变量的增删
        /// </summary>
        /// <param name="dataContext"></param>
        private void HandlingRuntimeVariables(McfunctionIntellisenseDataContext dataContext)
        {
            #region Macros
            for (int i = 0; i < dataContext.RemoveMacroIndexes.Count; i++)
            {
                MacroVariables.Remove(dataContext.RemoveMacroIndexes[i]);
            }
            dataContext.RemoveMacroIndexes.Clear();
            #endregion
            #region RuntimeVariables
            if (dataContext.TypingBossbarIdString)
            {
                editPageDataContext.RuntimeVariables["bossbarID"][dataContext.CurrentLineIndex] = dataContext.currentBossbarIdString;
                dataContext.TypingBossbarIdString = false;
                dataContext.currentBossbarIdString = "";
            }
            if (dataContext.TypingScoreboardVariable)
            {
                editPageDataContext.RuntimeVariables["targetObjective"][dataContext.CurrentLineIndex] = dataContext.currentScoreboardVariable;
                dataContext.TypingScoreboardVariable = false;
                dataContext.currentScoreboardVariable = "";
            }
            if (dataContext.TypingStorageIdString)
            {
                editPageDataContext.RuntimeVariables["storageID"][dataContext.CurrentLineIndex] = dataContext.currentStorageVariable;
                dataContext.TypingStorageIdString = false;
                dataContext.currentStorageVariable = "";
            }
            if (dataContext.TypingTagVariable)
            {
                editPageDataContext.RuntimeVariables["tagValue"][dataContext.CurrentLineIndex] = dataContext.currentTagVariable;
                dataContext.TypingTagVariable = false;
                dataContext.currentTagVariable = "";
            }
            if (dataContext.TypingTeamNameString)
            {
                editPageDataContext.RuntimeVariables["teamID"][dataContext.CurrentLineIndex] = dataContext.currentTeamVariable;
                dataContext.TypingTeamNameString = false;
                dataContext.currentTeamVariable = "";
            }
            if (dataContext.TypingTriggerVariable)
            {
                editPageDataContext.RuntimeVariables["triggerObjective"][dataContext.CurrentLineIndex] = dataContext.currentTriggerVariable;
                dataContext.TypingTriggerVariable = false;
                dataContext.currentTriggerVariable = "";
            }
            #endregion
        }

        /// <summary>
        /// 向服务器发送当前上下文代码文本，索要补全数据
        /// </summary>
        /// <param name="context"></param>
        /// <param name="NeedCalculate"></param>
        /// <returns></returns>
        private async Task PushContextToServer(bool NeedCalculate = true)
        {
            dataContext.IsNeedCalculate = NeedCalculate;
            try
            {
                communiteTokenSource.Cancel();
            }
            catch { }

            // 创建新的延迟任务
            communiteTokenSource = new CancellationTokenSource();

            try
            {
                await Task.Delay(250, communiteTokenSource.Token);
                await SendData();
            }
            catch { }
        }

        /// <summary>
        /// 处理拖拽后所影响行的运行时变量
        /// </summary>
        /// <param name="RowIndexes"></param>
        private async Task DropRowsUpdate(int startLineIndex, int endLineIndex)
        {
            for (int i = startLineIndex; i < endLineIndex; i++)
            {
                //如果被影响的行还存在，那么执行语法分析
                if (i < Document.LineCount)
                {
                    #region 获取完整的一行后执行语法分析、更新运行时变量
                    int currentStartLineIndex = dataContext.CurrentLineIndex;
                    int currentEndLineIndex = dataContext.CurrentLineIndex;
                    int EndLineIndex = i;
                    string wholeString = "";
                    StringBuilder LeadingString = new();
                    StringBuilder TrailingString = new();
                    if (dataContext.CurrentLineIndex > 0)
                        wholeString = Document.GetText(Document.Lines[currentStartLineIndex - 1]);
                    while (wholeString.EndsWith('\\'))
                    {
                        LeadingString.Insert(0, wholeString);
                        currentStartLineIndex--;
                        if (currentStartLineIndex >= 0)
                            wholeString = Document.GetText(Document.Lines[currentStartLineIndex]);
                    }
                    if (dataContext.CurrentLineIndex < Document.LineCount && Document.LineCount > 1)
                        wholeString = Document.GetText(Document.Lines[currentStartLineIndex + 1]);
                    while (wholeString.EndsWith('\\'))
                    {
                        EndLineIndex = currentEndLineIndex;
                        TrailingString.Append(wholeString);
                        currentEndLineIndex++;
                        if (currentEndLineIndex < Document.LineCount)
                            wholeString = Document.GetText(Document.Lines[currentEndLineIndex]);
                    }
                    #endregion
                    //同步循环索引，避免重复分析
                    i = EndLineIndex;
                    //通过上述的末尾反斜杠判断循环后拼接成完整的命令
                    dataContext.CurrentCode = LeadingString.ToString().TrimStart() + Document.GetText(Document.Lines[dataContext.CurrentLineIndex]).Trim() + TrailingString.ToString().TrimEnd();
                    dataContext.CurrentCaretIndex = dataContext.CurrentCode.Length;
                    if (dataContext.CurrentCode.StartsWith("bossbar add ") ||
                       dataContext.CurrentCode.StartsWith("data modify storage ") ||
                       dataContext.CurrentCode.StartsWith("data merge storage ") ||
                       dataContext.CurrentCode.StartsWith("scoreboard objectives add ") ||
                       dataContext.CurrentCode.StartsWith("tag @a add ") ||
                       dataContext.CurrentCode.StartsWith("team add "))
                        await PushContextToServer(false);
                }
                HandlingRuntimeVariables(dataContext);
            }
        }

        /// <summary>
        /// 表示已触发补全
        /// </summary>
        private void CompletedItemData_StatusUpdated(int CompletionLength)
        {
            communiteTokenSource.Cancel();
            Application.Current.Dispatcher.Invoke(() =>
            {
                completionWindow = new(TextArea);
                IsCompletionWindowShowing = false;
            });
            IsCompletionWindowShowing = false;
            if (dataContext.CurrentCode.TrimStart().StartsWith('$') && CompletionLength > 0)
            {
                Select(CompletionLength, 0);
                DropStartOffset = DropEndOffset = -1;
            }
        }

        /// <summary>
        /// 处理拖拽文本
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void TextEditor_PreviewDrop(object sender, DragEventArgs e)
        {
            if (e.Data is not null && e.Data.GetDataPresent(DataFormats.StringFormat))
            {
                DropedString = e.Data.GetData(DataFormats.StringFormat) as string;
                DropStartOffset = DropEndOffset = -1;
                Droped = DropedString is not null && DropedString.Trim().Length > 0;
                if (Droped)
                {
                    LastLineCount = Document.LineCount;
                    if (SelectionStart > TextArea.Caret.Offset || (SelectionStart + SelectionLength) < TextArea.Caret.Offset)
                    {
                        DropStartOffset = SelectionStart;
                        DropEndOffset = SelectionStart + SelectionLength;
                    }
                }
            }
        }

        /// <summary>
        /// 检测并执行快捷键
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public async void TextEditor_KeyDown(object sender, KeyEventArgs e)
        {
            #region 处理快捷键
            singleQuotation += e.Key == Key.OemQuotes && e.KeyboardDevice.Modifiers == ModifierKeys.None ? (byte)1 : (byte)0;
            doubleQuotation += e.Key == Key.OemQuotes && e.KeyboardDevice.Modifiers == ModifierKeys.Shift ? (byte)1 : (byte)0;

            bool IsKeyDown = (e.Key == Key.OemCloseBrackets && e.KeyboardDevice.Modifiers == ModifierKeys.None) ||
                             (e.Key == Key.OemCloseBrackets && e.KeyboardDevice.Modifiers == ModifierKeys.Shift) ||
                             (e.Key == Key.D0 && e.KeyboardDevice.Modifiers == ModifierKeys.Shift) ||
                             (e.Key == Key.OemPeriod && e.KeyboardDevice.Modifiers == ModifierKeys.Shift) ||
                             singleQuotation > 1 || doubleQuotation > 1;

            if (IsKeyDown)
            {
                ShortKeyToken.Cancel();
                singleQuotation = doubleQuotation = 0;
            }

            ShortKeyToken = new CancellationTokenSource();

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(/*ExecuteShortKeyDelay*/0.1), ShortKeyToken.Token);

                // 检查是否任务在延迟期间被取消
                if (ShortKeyToken.IsCancellationRequested)
                {
                    singleQuotation = doubleQuotation = 0;
                    return; // 如果在延迟等待期间任务已经被取消，则直接返回
                }

                await Task.Run(async () =>
                {
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        #region 括号类
                        if (e.Key == Key.OemOpenBrackets && e.KeyboardDevice.Modifiers == ModifierKeys.None)
                            ExecuteShortKey("]", e);
                        if (e.Key == Key.OemOpenBrackets && e.KeyboardDevice.Modifiers == ModifierKeys.Shift)
                            ExecuteShortKey("}", e);
                        if (e.Key == Key.D9 && e.KeyboardDevice.Modifiers == ModifierKeys.Shift)
                            ExecuteShortKey(")", e);
                        if (e.Key == Key.OemComma && e.KeyboardDevice.Modifiers == ModifierKeys.Shift)
                            ExecuteShortKey(">", e);
                        #endregion
                        #region 引号类
                        if (singleQuotation % 2 != 0)
                        {
                            ExecuteShortKey("'", e);
                            singleQuotation = 0;
                        }
                        if (doubleQuotation % 2 != 0)
                        {
                            ExecuteShortKey("\"", e);
                            doubleQuotation = 0;
                        }
                        #endregion
                    });
                }, ShortKeyToken.Token);
            }
            catch (Exception) { }
            #endregion

            #region 处理函数签名
            int startIndex = TextArea.Caret.Offset - 3;
            if (startIndex >= 0 && TextArea.Caret.Line - 1 == 0)
            {
                string startString = Document.GetText(startIndex, 3);
                if (startString == "###")
                {
                    Document.Insert(TextArea.Caret.Offset, " <summary>\r\n### \r\n###</summary>\r\n### <param name=\"sender\"></param>\r\n### <param name=\"e\"></param>");
                    MacroVariables.Add(TextArea.Caret.Line - 2, new MacroItem()
                    {
                        Text = "sender",
                        Description = ""
                    });
                    MacroVariables.Add(TextArea.Caret.Line - 1, new MacroItem()
                    {
                        Text = "e",
                        Description = ""
                    });
                }
            }
            #endregion
        }

        /// <summary>
        /// 记录上一次编辑的文档行数并记录选中偏移量
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void TextEditor_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            LastLineCount = Document.LineCount;
            if (SelectionLength > 0)
            {
                DropStartOffset = SelectionStart;
                DropEndOffset = SelectionStart + SelectionLength;
            }
            else
            {
                DropStartOffset = DropEndOffset = -1;
            }
        }

        /// <summary>
        /// 处理补全成员上下反转
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CompletionWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.Up) && completionWindow.CompletionList.ListBox.SelectedIndex == 0)
            {
                completionWindow.CompletionList.ListBox.SelectedIndex = completionWindow.CompletionList.ListBox.Items.Count - 1;
                Task.Run(() =>
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        completionWindow.CompletionList.ListBox.SelectedIndex++;
                        completionWindow.CompletionList.ScrollViewer.ScrollToVerticalOffset(completionWindow.CompletionList.ScrollViewer.ScrollableHeight);
                    });
                });
            }
            if (Keyboard.IsKeyDown(Key.Down) && completionWindow.CompletionList.ListBox.SelectedIndex == completionWindow.CompletionList.ListBox.Items.Count - 1)
            {
                completionWindow.CompletionList.ListBox.SelectedIndex = 0;
                Task.Run(() =>
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        if(completionWindow.CompletionList.ListBox.SelectedIndex > 0)
                        completionWindow.CompletionList.ListBox.SelectedIndex--;
                        completionWindow.CompletionList.ScrollViewer.ScrollToVerticalOffset(0);
                    });
                });
            }
        }

        /// <summary>
        /// 载入代码编辑器引用
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void TextBox_Loaded(object sender, RoutedEventArgs e)
        {
            DatapackViewModel datapackDataContext = Window.GetWindow(this).DataContext as DatapackViewModel;
            editPageDataContext = datapackDataContext.EditPage.DataContext as EditPageViewModel;
            client = editPageDataContext.client;
            SyntaxItemDicionary = editPageDataContext.SyntaxItemDicionary;
            Thread thread = new(ReceiveData);
            thread.Start();
            Focus();
        }

        //TextArea.TextView.BackgroundRenderers.Add(new WaveUnderlineBackgroundRenderer(23, 30, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#BF322D"))));
        //// 获取当前行号
        //int line = TextArea.Caret.Line;
        //// 获取当前行的文本长度
        //int lineLength = Document.Lines[line - 1].Length;
        //// 将光标移动到行末
        //TextArea.Caret.Offset = Document.GetOffset(line, lineLength - 2);
        //TextArea.TextView.BackgroundRenderers.Add(new WaterMarkRenderer("ace", TextArea.Caret.Location, new(window.FontFamily, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal), window.FontSize, new((Color)ColorConverter.ConvertFromString("#A1A1A1"))));
        //TextArea.TextView.BackgroundRenderers.Add(new DotUnderlineBackgroundRenderer(new TextLocation(1,15),10, new((Color)ColorConverter.ConvertFromString("#A1A1A1"))));

        /// <summary>
        /// 根据按键行为执行自动补全
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public async void TextEditor_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            #region 记录行数与粘贴状态
            if (e.Key == Key.LWin || e.Key == Key.RWin)
            {
                LastLineCount = Document.LineCount;
            }
            RunningPaste = e.Key == Key.V && e.KeyboardDevice.Modifiers == ModifierKeys.Control;
            #endregion

            #region 23w31a起命令未编写完毕时按下回车键则让当前行以反斜杠结尾
            NoLongerNeedCompleton = e.Key == Key.Enter;
            //if (e.Key == Key.Enter && !IsCompletionOver)
            //{
            //    if (!IsCompletionOver && Document.GetText(TextArea.Caret.Offset - 1,1) != "\\")
            //        Document.Insert(TextArea.Caret.Offset, "\\");
            //    else
            //        IsCompletionWindowShowing = false;
            //}
            #endregion

            #region 处理运行时变量的增删
            //处理光标位置更新后运行时变量的更新
            if ((e.Key == Key.Enter || e.Key == Key.Up || e.Key == Key.Down || e.Key == Key.Left || e.Key == Key.Right) && !RunningPaste && !Droped)
            {
                HandlingRuntimeVariables(dataContext);
            }

            //处理跨行选择删除或剪切后运行时变量与宏变量的更新
            if ((e.Key == Key.Back || e.Key == Key.Delete || (e.Key == Key.X && e.KeyboardDevice.Modifiers == ModifierKeys.Control)) && SelectedText.Trim().Length > 0 && SelectedText.Contains("\r\n"))
            {
                string CurrentLineText = Document.GetText(Document.Lines[TextArea.Caret.Line - 1]);
                int selectedEndLineIndex = Document.GetLineByOffset(SelectionStart + SelectionLength).LineNumber - 1;
                int differenceLineCount = Document.LineCount - LastLineCount;
                int CurrentLineIndex = TextArea.Caret.Line - 1;
                int lineCount = Document.LineCount;
                //删除指定行的宏定义
                if (CurrentLineText.TrimStart().StartsWith('#'))
                {
                    if (differenceLineCount != 0)
                    {
                        await Task.Run(() =>
                        {
                            for (int i = CurrentLineIndex; i < lineCount; i++)
                            {
                                if (MacroVariables.TryGetValue(i, out MacroItem macroItem))
                                {
                                    MacroItem currentMacroItem = macroItem;
                                    MacroVariables.Remove(i);
                                    if (i > selectedEndLineIndex)
                                        MacroVariables.Add(i - differenceLineCount, currentMacroItem);
                                }
                            }
                        });
                    }
                }
                else
                if (differenceLineCount != 0)//删除指定行的运行时变量
                {
                    await Task.Run(() =>
                    {
                        foreach (var key in editPageDataContext.RuntimeVariables.Keys)
                        {
                            for (int i = dataContext.CurrentLineIndex; i < lineCount; i++)
                            {
                                if (editPageDataContext.RuntimeVariables[key].TryGetValue(i, out string variable))
                                {
                                    string currentVariable = variable;
                                    editPageDataContext.RuntimeVariables[key].Remove(i);
                                    if (i > selectedEndLineIndex)
                                        editPageDataContext.RuntimeVariables[key].Add(i - differenceLineCount, currentVariable);
                                }
                            }
                        }
                    });
                }
            }
            #endregion

            #region Ctrl+Space或光标超出当前上下文范围则执行自动补全
            e.Handled = e.KeyboardDevice.Modifiers == ModifierKeys.Control && e.Key == Key.Space;
            if (e.Handled)
            {
                communiteTokenSource.Cancel();
                GetCurrentLineText();
                GetTypingText();
                dataContext.CurrentLineIndex = TextArea.Caret.Line - 1;
                if (dataContext.CurrentCode.Trim() == "")
                    await PushContextToServer(false);
                else
                    await PushContextToServer();
            }
            #endregion
        }

        /// <summary>
        /// 文本更新时更新语法树和命令路径
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public async void TextEditor_TextChanged(object sender, EventArgs e)
        {
            if (!communiteTokenSource.IsCancellationRequested)
                communiteTokenSource.Cancel();
            communiteTokenSource = new CancellationTokenSource();

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(/*ExecuteIntellisenseDelay*/0.2)/*, communiteTokenSource.Token*/);

                // 检查是否任务在延迟期间被取消
                if (communiteTokenSource.IsCancellationRequested)
                    return;

                GetCurrentLineText();
                GetTypingText();
                await Task.Run(async () =>
                {
                    bool IsExplanatoryNote = false;
                    int differenceLineCount = 0;
                    string pastedText = "";
                    int LastLineIndex = 0;
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        #region 计算行数之差
                        differenceLineCount = Document.LineCount - LastLineCount;
                        RunningPaste = differenceLineCount > 0;
                        LastLineIndex = dataContext.CurrentLineIndex - differenceLineCount;
                        if (LastLineIndex == 0)
                            LastLineIndex = dataContext.CurrentLineIndex;
                        #endregion
                        #region 添加大纲
                        DocumentLine startLine;
                        DocumentLine endLine;
                        int currentLineTextLength = dataContext.CurrentCode.Trim().Length;
                        if (dataContext.CurrentCode.Trim() == "#endregion")
                        {
                            //向当前行的上方搜索#region
                            if (Document.LineCount > 1)
                            {
                                int lineIndex = TextArea.Caret.Line - 2;
                                endLine = Document.Lines[dataContext.CurrentLineIndex];
                                startLine = Document.Lines[lineIndex];
                                while (lineIndex >= 0 && !Document.GetText(startLine).Trim().StartsWith("#region"))
                                {
                                    lineIndex--;
                                    if (lineIndex >= 0)
                                        startLine = Document.Lines[lineIndex];
                                }
                                //已找到大纲头部标记
                                if (lineIndex >= 0)
                                    LineFoldingStrategy.AddFolding(foldingManager, Document, startLine, endLine);
                            }
                        }
                        #endregion
                        #region 大纲的删除与更新
                        if (dataContext.CurrentLineIndex < Document.LineCount)
                        {
                            #region 删除大纲
                            DocumentLine currentLine = Document.GetLineByNumber(dataContext.CurrentLineIndex + 1);
                            FoldingSection currentFoldingSection;
                            ReadOnlyCollection<FoldingSection> foldingSections = foldingManager.GetFoldingsAt(currentLine.Offset);
                            foreach (FoldingSection item in foldingManager.AllFoldings)
                            {
                                if ((item.StartOffset == currentLine.Offset || item.EndOffset == currentLine.EndOffset) && ("#region".StartsWith(dataContext.CurrentCode) && dataContext.CurrentCode.Trim().Length < 7 || "#endregion".StartsWith(dataContext.CurrentCode) && dataContext.CurrentCode.Trim().Length < 10))
                                {
                                    foldingManager.RemoveFolding(item);
                                    break;
                                }
                            }
                            #endregion
                            #region 更新大纲标题
                            if (currentLineTextLength > 0 && foldingSections.Count > 0)
                            {
                                currentFoldingSection = foldingManager.GetFoldingsAt(currentLine.Offset).First();
                                if (currentFoldingSection is not null && dataContext.CurrentCode.Length > 7 && currentFoldingSection.StartOffset == currentLine.Offset)
                                    currentFoldingSection.Title = dataContext.CurrentCode.Length == 8 ? "#region" : dataContext.CurrentCode[8..];
                            }
                            #endregion
                        }
                        #endregion
                        #region 更新宏变量
                        if (dataContext.CurrentCode.TrimStart().StartsWith('#'))
                        {
                            Match nameAndDescription = Regex.Match(dataContext.CurrentCode, @"(\s+)?###(\s+)?<(\s+)?param(\s+)?name(\s+)?=(\s+)?""([a-zA-Z0-9_]+)?""(\s+)?>(.*)?</param>");
                            MacroItem macroItem = new();
                            if (nameAndDescription.Groups.Count > 6 && nameAndDescription.Groups[7].Success)
                            {
                                if (!MacroVariables.ContainsKey(dataContext.CurrentLineIndex))
                                {
                                    macroItem.Text = nameAndDescription.Groups[7].Value;
                                    MacroVariables.Add(dataContext.CurrentLineIndex, macroItem);
                                }
                                else
                                    MacroVariables[dataContext.CurrentLineIndex].Text = nameAndDescription.Groups[7].Value;
                            }
                            if (nameAndDescription.Groups.Count > 8 && nameAndDescription.Groups[9].Success)
                            {
                                if (!MacroVariables.ContainsKey(dataContext.CurrentLineIndex))
                                {
                                    macroItem.Description = nameAndDescription.Groups[9].Value;
                                    MacroVariables.Add(dataContext.CurrentLineIndex, macroItem);
                                }
                                else
                                    MacroVariables[dataContext.CurrentLineIndex].Description = nameAndDescription.Groups[9].Value;
                            }

                            if (nameAndDescription.Groups.Count <= 6)
                                MacroVariables.Remove(dataContext.CurrentLineIndex);
                        }
                        #endregion
                        NoLongerNeedCompleton |= Document.Text.Trim() == "";
                        IsExplanatoryNote = ("#region".StartsWith(dataContext.CurrentCode.TrimStart()) && dataContext.CurrentCode.TrimStart().Length < 7) || ("#endregion".StartsWith(dataContext.CurrentCode.TrimStart()) && dataContext.CurrentCode.TrimStart().Length < 10) || (Regex.IsMatch(dataContext.CurrentCode.TrimStart(), @"^(###(\s+)?<(\s+)?param(\s+)?)") && dataContext.CurrentCode.Replace(" ", "").Length < 8);
                    });

                    if (NoLongerNeedCompleton)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            IsCompletionWindowShowing = false;
                            completionWindow = new(TextArea);
                        });
                        dataContext.CommandPath = "";
                    }
                    else//处理多行粘贴后运行时变量和宏定义的更新
                    if (RunningPaste)
                    {
                        for (int i = LastLineIndex; i <= dataContext.CurrentLineIndex; i++)
                        {
                            if (i < 0)
                                continue;
                            //取一行文本
                            await Application.Current.Dispatcher.InvokeAsync(() =>
                            {
                                pastedText = Document.GetText(Document.Lines[i]);
                            });
                            #region 处理宏定义
                            if (pastedText.TrimStart().StartsWith("###"))
                            {
                                Match nameAndDescription = Regex.Match(pastedText, @"(\s+)?###(\s+)?<(\s+)?param(\s+)?name(\s+)?=(\s+)?""([a-zA-Z0-9_]+)?""(\s+)?>(.*)?</param>");
                                if (nameAndDescription.Success && nameAndDescription.Groups.Count > 6)
                                {
                                    string macroName = nameAndDescription.Groups[7].Value;
                                    MacroItem macroItem = new()
                                    {
                                        Text = macroName
                                    };
                                    MacroVariables.Add(i, macroItem);
                                    if (nameAndDescription.Groups.Count > 8)
                                        macroItem.Description = nameAndDescription.Groups[9].Value;
                                }
                            }
                            #endregion
                            #region 处理bossbar
                            if (pastedText.TrimStart().StartsWith("bossbar "))
                            {
                                Match matchBossbarId = Regex.Match(pastedText, @"bossbar add ([a-z0-9_\.\-/]+) ");
                                if (matchBossbarId.Success && matchBossbarId.Value == pastedText)
                                {
                                    editPageDataContext.RuntimeVariables["bossbarID"].Add(i, matchBossbarId.Groups[1].Value);
                                    continue;
                                }
                            }
                            #endregion
                            #region 处理data
                            if (pastedText.TrimStart().StartsWith("data "))
                            {
                                Match matchStorageId = Regex.Match(pastedText, @"data (modify|merge) storage ([a-z0-9_\.\-/]+) ");
                                if (matchStorageId.Success && matchStorageId.Value == pastedText)
                                {
                                    editPageDataContext.RuntimeVariables["storageID"].Add(i, matchStorageId.Groups[2].Value);
                                    continue;
                                }
                            }
                            #endregion
                            #region 处理scoreboard
                            if (pastedText.TrimStart().StartsWith("scoreboard "))
                            {
                                Match matchScoreboardVariable = Regex.Match(pastedText, @"scoreboard objectives add ([A-z0-9_\.\-+]+) ");
                                if (matchScoreboardVariable.Success && matchScoreboardVariable.Value == pastedText)
                                {
                                    editPageDataContext.RuntimeVariables["targetObjective"].Add(i, matchScoreboardVariable.Groups[1].Value);
                                    continue;
                                }
                            }
                            #endregion
                            #region 处理tag
                            if (pastedText.TrimStart().StartsWith("tag "))
                            {
                                dataContext.CurrentCaretIndex = pastedText.Trim().Length;
                                dataContext.CurrentCode = pastedText.Trim();
                                //交给分析器判断是否需要添加tag变量
                                await PushContextToServer();
                            }
                            #endregion
                            #region 处理team
                            if (pastedText.TrimStart().StartsWith("team "))
                            {
                                Match matchTeamId = Regex.Match(pastedText, @"team add ([a-zA-Z0-9_\.\-+]+)");
                                if (matchTeamId.Success && matchTeamId.Value == pastedText)
                                {
                                    editPageDataContext.RuntimeVariables["teamID"].Add(i, matchTeamId.Groups[1].Value);
                                }
                            }
                            #endregion
                        }
                        Application.Current.Dispatcher.Invoke(CompletedSource.Clear);
                        RunningPaste = false;
                    }
                    else//处理拖拽后运行时变量与宏定义的更新
                    if (Droped)
                    {
                        int DropStartLineIndex = 0;
                        int DropEndLineIndex = 0;
                        Droped = false;
                        #region 处理拖拽前被选中行
                        if (DropStartOffset > -1 && DropEndOffset > -1)
                        {
                            DropStartLineIndex = Document.GetLineByOffset(DropStartOffset).LineNumber - 1;
                            DropEndLineIndex = Document.GetLineByOffset(DropEndOffset).LineNumber - 1;
                            await DropRowsUpdate(DropStartLineIndex, DropEndLineIndex);
                        }
                        #endregion
                        #region 处理拖拽后影响的行
                        DropStartLineIndex = TextArea.Caret.Line - 1;
                        DropEndLineIndex = DropedString.Split("\r\n").Length + DropStartLineIndex;
                        await DropRowsUpdate(DropStartLineIndex, DropEndLineIndex);
                        #endregion
                    }
                    else
                    if (!IsExplanatoryNote)
                    {
                        await PushContextToServer();
                        HandlingRuntimeVariables(dataContext);
                    }
                    else
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            IsCompletionWindowShowing = false;
                            completionWindow = new(TextArea);
                        });
                    }
                }, communiteTokenSource.Token);
            }
            catch (Exception) { }
        }

        /// <summary>
        /// 抓取光标所在上下文的文本
        /// </summary>
        private void GetTypingText()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                TextDocument textDocument = Document;
                dataContext.TypingContent = "";
                string CurrentLineText = textDocument.GetText(textDocument.Lines[dataContext.CurrentLineIndex]);
                if (dataContext.CurrentCode.Trim().Length > 0)
                {
                    //光标位置
                    int CurrentIndex = TextArea.Caret.Offset;
                    int CurrentLineIndex = dataContext.CurrentLineIndex;

                    //左侧索引
                    int LeftIndex = CurrentIndex;
                    //是否需要替换已存在的光标左侧单词
                    bool NeedReplaceLeftWord = false;
                    #region 由光标位置作起始点,向当前行左右延伸直到分别遇到第一个空格,最后识别截取范围内的文本
                    string LeftString = Document.GetText(LeftIndex - 1, 1);
                    if (LeftIndex > 0 && LeftString != " ")
                    {
                        while (LeftIndex > 0 &&
                               !DonotMatches.Contains(LeftString) &&
                              (regexService.IsWord().IsMatch(LeftString) ||
                              Matches.Contains(LeftString) ||
                              Regex.IsMatch(Document.GetText(LeftIndex - 1, 1), @"~|\^") || ("#region".StartsWith(dataContext.CurrentCode.Trim()) && dataContext.CurrentCode.Trim().Length < 8)))
                        {
                            LeftString = Document.GetText(LeftIndex - 1, 1);
                            if (LeftIndex == Document.Lines[CurrentLineIndex].Offset && Document.GetText(Document.Lines[CurrentLineIndex - 1]).EndsWith('\\'))
                            {
                                LeftIndex -= 4;
                                CurrentLineIndex--;
                            }
                            else
                                if (LeftIndex == Document.Lines[dataContext.CurrentLineIndex].Offset && !Document.GetText(Document.Lines[CurrentLineIndex - 1]).EndsWith('\\'))
                                break;
                            else//循环向左
                                LeftIndex--;
                        }
                        NeedReplaceLeftWord = LeftIndex != CurrentIndex || TextArea.Caret.Offset == Document.Lines[CurrentLineIndex].EndOffset;
                        if (DonotMatches.Contains(LeftString))
                        {
                            LeftIndex++;
                        }
                    }
                    int length = CurrentIndex - LeftIndex;
                    if (length < 0)
                        length = 0;
                    if (LeftIndex > TextArea.Document.Lines[CurrentLineIndex].EndOffset)
                        LeftIndex = TextArea.Document.Lines[CurrentLineIndex].EndOffset;
                    if (NeedReplaceLeftWord)
                        dataContext.TypingContent = Document.GetText(LeftIndex, length);
                    #endregion
                }
            });
        }

        /// <summary>
        /// 抓取光标所在位置所需被分析的所有文本
        /// </summary>
        private void GetCurrentLineText()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                int lastLineIndex = TextArea.Caret.Line - 2;
                int nextLineindex = TextArea.Caret.Line;
                dataContext.CurrentCode = Document.GetText(Document.Lines[lastLineIndex + 1]);
                dataContext.CurrentLineIndex = TextArea.Caret.Line - 1;
                if (dataContext.CurrentCode.TrimStart().StartsWith('#'))
                {
                    dataContext.CurrentCaretIndex = 0;
                    return;
                }
                int OrgianSpaceCount = dataContext.CurrentCode.Length;
                int StartSpaceCount = OrgianSpaceCount - dataContext.CurrentCode.TrimStart().Length;
                List<string> BackContent = [];
                List<string> FrontContent = [];
                int BackSlashOffset = 0;
                if (lastLineIndex >= 0)
                {
                    string lastLineText = Document.GetText(Document.Lines[lastLineIndex]);
                    while (lastLineText.EndsWith('\\'))
                    {
                        if (lastLineIndex >= 0)
                        {
                            BackSlashOffset += 3;
                            AnalyzeStartingIndex = Document.Lines[lastLineIndex].Offset;
                            lastLineText = Document.GetText(Document.Lines[lastLineIndex]);
                            FrontContent.Add(lastLineText.TrimEnd('\\'));
                        }
                        else
                            break;
                        lastLineIndex--;
                    }
                }
                else
                    AnalyzeStartingIndex = Document.Lines[TextArea.Caret.Line - 1].Offset;

                if (nextLineindex < Document.LineCount)
                {
                    string nextLineText = Document.GetText(Document.Lines[nextLineindex]);
                    while (nextLineText.EndsWith('\\'))
                    {
                        nextLineindex++;
                        if (nextLineindex < Document.LineCount)
                        {
                            AnalyzeEndingIndex = Document.Lines[nextLineindex].EndOffset;
                            nextLineText = Document.GetText(Document.Lines[nextLineindex]);
                            BackContent.Add(nextLineText.TrimEnd('\\'));
                        }
                        else
                            break;
                    }
                }
                else
                    AnalyzeEndingIndex = Document.Lines[TextArea.Caret.Line - 1].EndOffset;

                if (FrontContent.Count > 0 || BackContent.Count > 0)
                    dataContext.CurrentCode = (string.Join("", FrontContent) + dataContext.CurrentCode + string.Join("", BackContent)).TrimStart();
                else
                    dataContext.CurrentCode = Document.GetText(Document.Lines[TextArea.Caret.Line - 1]).TrimStart();
                dataContext.CurrentCaretIndex = TextArea.Caret.Offset - Document.Lines[lastLineIndex + 1].Offset - (StartSpaceCount + BackSlashOffset);
            });
        }

        /// <summary>
        /// 补全数据过滤
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CompletedViewSource_Filter(object sender, FilterEventArgs e)
        {
            CompletedItemData completedItemData = (e.Item as CompletedItemData)!;
            bool IsContentString = false;
            string contentString = "";
            string currentCompletionItem = completedItemData!.Text.ToLower();
            string currentTypingString = Regex.Replace(dataContext.TypingContent.ToLower(), @"([a-z]+:)|([a-z]+\.)", "");
            if (completedItemData.Content is string content && content.ToLower().StartsWith(currentTypingString))
            {
                contentString = content;
                IsContentString = true;
            }
            e.Accepted = currentCompletionItem.StartsWith("minecraft") || (IsContentString && currentCompletionItem.StartsWith(currentTypingString) && currentTypingString.Length < currentCompletionItem.Length);
            if (e.Accepted)
            {
                DisplayCount++;
                if (dataContext.TypingContent != "" && currentCompletionItem.StartsWith(currentTypingString))
                {
                    string rightPartString = !IsContentString ? completedItemData!.Text[currentTypingString.Length..] : contentString[currentTypingString.Length..];
                    string leftPartString = !IsContentString ? completedItemData!.Text[..currentTypingString.Length] : contentString[..currentTypingString.Length];
                    DockPanel dockPanel = new()
                    {
                        LastChildFill = false
                    };
                    TextBlock leftPart = new()
                    {
                        Text = leftPartString,
                        FontWeight = FontWeights.Bold,
                        Foreground = lightBlueBrush
                    };
                    TextBlock rightPart = new()
                    {
                        Text = rightPartString
                    };
                    dockPanel.Children.Add(leftPart);
                    dockPanel.Children.Add(rightPart);
                    completedItemData.Content = dockPanel;
                }
            }
            else
            if (completedItemData.Content.ToString() == "minecraft")
                e.Accepted = true;
        }
        #endregion
    }
}