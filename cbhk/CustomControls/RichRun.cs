using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace cbhk.CustomControls
{
    public class RichRun : Run
    {
        #region 字段
        public int CurrentVersion = 1202;
        /// <summary>
        /// 迭代内容序列
        /// </summary>
        private static List<char> Obfuscates = [];
        private static FontFamily commonFontFamily = new("Bitstream Vera Sans Mono");
        private FontFamily OriginFontFamily = new("Microsoft YaHei UI");

        #region 控制混淆
        private bool isObfuscated = false;
        public bool IsObfuscated
        {
            get => isObfuscated;
            set
            {
                isObfuscated = value;
                ObfuscateTimer.IsEnabled = isObfuscated;
                if (ObfuscateTimer.IsEnabled)
                {
                    ObfuscateTimer.Start();
                    FontFamily = commonFontFamily;
                }
                else
                {
                    ObfuscateTimer.Stop();
                    FontFamily = OriginFontFamily;
                }
            }
        }
        #endregion

        /// <summary>
        /// 迭代结果
        /// </summary>
        readonly StringBuilder ObfuscatesResult = new();

        /// <summary>
        /// 迭代器
        /// </summary>
        readonly Random random = new();

        #region UID
        private string uid = "";
        public string UID
        {
            get => uid;
            set => uid = value;
        }
        #endregion

        //用于判断的下划线
        readonly TextDecoration underlined_style = System.Windows.TextDecorations.Baseline.First();

        //用于判断的删除线
        readonly TextDecoration strikethrough_style = System.Windows.TextDecorations.Strikethrough.First();

        public DispatcherTimer ObfuscateTimer = new()
        {
            Interval = TimeSpan.FromMilliseconds(10),
            IsEnabled = false
        };
        #endregion

        #region 保存事件数据
        private bool hasClickEvent = false;

        public bool HasClickEvent
        {
            get => hasClickEvent;
            set => hasClickEvent = value;
        }

        private bool hasHoverEvent = false;

        public bool HasHoverEvent
        {
            get => hasHoverEvent;
            set => hasHoverEvent = value;
        }

        private bool hasInsertion = false;

        public bool HasInsertion
        {
            get => hasInsertion;
            set => hasInsertion = value;
        }

        private string clickEventActionItem = "运行命令";
        public string ClickEventActionItem
        {
            get => clickEventActionItem;
            set => clickEventActionItem = value;
        }
        private string clickEventValue = "";
        public string ClickEventValue
        {
            get => clickEventValue;
            set => clickEventValue = value;
        }
        private string hoverEventActionItem = "显示文本";
        public string HoverEventActionItem
        {
            get => hoverEventActionItem;
            set => hoverEventActionItem = value;
        }
        private string hoverEventValue = "";
        public string HoverEventValue
        {
            get => hoverEventValue;
            set => hoverEventValue = value;
        }

        private string insertionValue = "";
        public string InsertionValue
        {
            get => insertionValue;
            set => insertionValue = value;
        }
        #endregion

        #region 最终事件数据
        public string EventData
        {
            get
            {
                string result = "";
                string clickEventAction = EventDataBase.Where(item => item.Value == ClickEventActionItem.Trim()).First().Key;
                string ClickEventString = HasClickEvent ? ",\"clickEvent\":{\"action\":\"" + clickEventAction + "\",\"value\":\"" + (clickEventAction.Trim() == "open_url" ? "http://" : "") + ClickEventValue + "\"}" : "";

                string HoverEventString = HasHoverEvent ? ",\"hoverEvent\":{\"action\":\"" + (EventDataBase.Where(item => item.Value == HoverEventActionItem.Trim()).First().Key) + "\",\"value\":\"" + HoverEventValue + "\"}" : "";
                string InsertionString = HasInsertion ? ",\"insertion\":{\"action\":\"" + (EventDataBase.Where(item => item.Value == HoverEventActionItem.Trim()).First().Key) + "\",\"value\":\"" + HoverEventValue + "\"}" : "";
                result = ClickEventString + HoverEventString + InsertionString;
                return result;
            }
        }
        #endregion

        #region 当前文本数据
        public string Result
        {
            get
            {
                Paragraph CurrentParagraph = Parent as RichParagraph;
                if (CurrentParagraph != null)
                {
                    string result = "";
                    bool IsLastRun = Equals(CurrentParagraph.Inlines.LastInline, this);
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        string textString = ObfuscateTimer.IsEnabled ? UID : Text;
                        textString = textString.Replace("\\", "\\\\\\\\").Replace("\"", "\\\\\"");
                        if (textString.Length > 0)
                        {
                            string colorString = Foreground.ToString().Remove(1, 2);
                            if (CurrentVersion >= 113)
                                result = "{\"text\":\"" + textString + (IsLastRun ? "\\\\n" : "") + "\"" + (colorString != "#000000" ? ",\"color\":\"" + colorString + "\"" : "") + (FontStyle == FontStyles.Italic ? ",\"italic\":true" : "") + (FontWeight == FontWeights.Bold ? ",\"bold\":true" : "") + (TextDecorations.Contains(underlined_style) ? ",\"underlined\":true" : "") + (TextDecorations.Contains(strikethrough_style) ? ",\"strikethrough\":true" : "") + (IsObfuscated && ObfuscateTimer.IsEnabled ? ",\"obfuscated\":true" : "") + EventData + "},";
                            else
                            {
                                string colorKey = ColorPickers.ColorPickers.PresetColorList[(Foreground as SolidColorBrush).Color];
                                result = (colorKey.Length > 0 && colorKey != @"\\u00a7f" ? colorKey:"") +
                                    (FontStyle == FontStyles.Italic ? @"\\u00a7o" : "") +
                                    (FontWeight == FontWeights.Bold ? @"\\u00a7l" : "") +
                                    (TextDecorations.Contains(underlined_style) ? @"\\u00a7n" : "") +
                                    (TextDecorations.Contains(strikethrough_style) ? @"\\u00a7m" : "") +
                                    (IsObfuscated ? @"\\u00a7k" : "") + textString;
                            }
                            if (result.Length > textString.Length && CurrentVersion < 113)
                                result += @"\\u00a7r";
                        }
                    });
                    return result;
                }
                else
                    return "";
            }
        }
        #endregion

        #region 事件数据
        //点击事件数据源
        public static ObservableCollection<string> clickEventSource { get; set; } = [];
        //悬浮事件数据源
        public static ObservableCollection<string> hoverEventSource { get; set; } = [];
        //点击事件数据源文件路径
        string clickEventSourceFilePath = AppDomain.CurrentDomain.BaseDirectory + "resources\\configs\\WrittenBook\\data\\clickEventActions.ini";
        //悬浮事件数据源文件路径
        string hoverEventSourceFilePath = AppDomain.CurrentDomain.BaseDirectory + "resources\\configs\\WrittenBook\\data\\hoverEventActions.ini";
        //事件数据库
        private static Dictionary<string, string> EventDataBase = [];
        #endregion

        /// <summary>
        /// 混淆文字控件
        /// </summary>
        public RichRun()
        {
            FontFamily = OriginFontFamily;
            ObfuscateTimer.Tick += ObfuscateTick;
            MouseEnter += ObfuscateTextMouseEnter;
            MouseLeave += ObfuscateTextMouseLeave;
            MouseLeftButtonDown += ObfuscateTextMouseLeftButtonDown;
            MouseLeftButtonUp += ObfuscateTextMouseLeftButtonUp;

            #region 读取混淆文本配置
            if (File.Exists(AppDomain.CurrentDomain.BaseDirectory + @"resources\configs\WrittenBook\data\obfuscateChars.ini"))
            {
                Obfuscates =
                [
                    .. File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + @"resources\configs\WrittenBook\data\obfuscateChars.ini").ToCharArray(),
                ];
            }
            #endregion

            #region 读取点击事件
            if (File.Exists(clickEventSourceFilePath) && clickEventSource.Count == 0)
            {
                string[] source = File.ReadAllLines(clickEventSourceFilePath);
                for (int i = 0; i < source.Length; i++)
                {
                    string[] data = source[i].Split('.');
                    clickEventSource.Add(data[1]);
                    if (!EventDataBase.ContainsKey(data[0]))
                        EventDataBase.Add(data[0], data[1]);
                }
            }
            #endregion

            #region 读取悬浮事件
            if (File.Exists(hoverEventSourceFilePath) && hoverEventSource.Count == 0)
            {
                string[] source = File.ReadAllLines(hoverEventSourceFilePath);
                for (int i = 0; i < source.Length; i++)
                {
                    string[] data = source[i].Split('.');
                    hoverEventSource.Add(data[1]);
                    if (!EventDataBase.ContainsKey(data[0]))
                        EventDataBase.Add(data[0], data[1]);
                }
            }
            #endregion
        }

        /// <summary>
        /// 鼠标抬起时启用混淆效果
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ObfuscateTextMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if(IsObfuscated)
            ObfuscateTimer.IsEnabled = true;
        }

        /// <summary>
        /// 鼠标按下时关闭混淆效果
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ObfuscateTextMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (IsObfuscated)
            {
                ObfuscateTimer.IsEnabled = false;
                Text = UID;
            }
        }

        /// <summary>
        /// 鼠标移出时启用混淆效果
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ObfuscateTextMouseLeave(object sender, MouseEventArgs e)
        {
            if (IsObfuscated)
                ObfuscateTimer.IsEnabled = true;
        }

        /// <summary>
        /// 鼠标移入时关闭混淆效果
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ObfuscateTextMouseEnter(object sender, MouseEventArgs e)
        {
            if (IsObfuscated)
            {
                ObfuscateTimer.IsEnabled = false;
                Text = UID;
            }
        }

        /// <summary>
        /// 混淆效果
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void ObfuscateTick(object sender, EventArgs e)
        {
            ObfuscatesResult.Clear();
            for (int i = 0; i < UID.Length; i++)
                ObfuscatesResult.Append(Obfuscates[random.Next(0, Obfuscates.Count - 1)]);
            Text = ObfuscatesResult.ToString();
        }
    }
}