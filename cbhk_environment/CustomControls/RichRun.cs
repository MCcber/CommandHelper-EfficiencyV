using cbhk_environment.ControlsDataContexts;
using cbhk_environment.Generators.WrittenBookGenerator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;

namespace cbhk_environment.CustomControls
{
    public class RichRun:Run
    {
        /// <summary>
        /// 迭代内容序列
        /// </summary>
        public List<char> Obfuscates;
        //开启混淆
        public bool IsObfuscated = false;

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
            get { return uid; }
            set
            {
                uid = value;
            }
        }
        #endregion

        //用于判断的下划线
        readonly TextDecoration underlined_style = System.Windows.TextDecorations.Baseline.First();

        //用于判断的删除线
        readonly TextDecoration strikethrough_style = System.Windows.TextDecorations.Strikethrough.First();

        /// <summary>
        /// 当前混淆最长长度
        /// </summary>
        double MaxContentLength;

        public System.Windows.Forms.Timer ObfuscateTimer = new()
        {
            Interval = 10,
            Enabled = false
        };

        #region 保存事件数据
        private bool hasClickEvent = false;

        public bool HasClickEvent
        {
            get { return hasClickEvent; }
            set
            {
                hasClickEvent = value;
            }
        }

        private bool hasHoverEvent = false;

        public bool HasHoverEvent
        {
            get { return hasHoverEvent; }
            set
            {
                hasHoverEvent = value;
            }
        }

        private bool hasInsertion = false;

        public bool HasInsertion
        {
            get { return hasInsertion; }
            set
            {
                hasInsertion = value;
            }
        }

        private string clickEventActionItem = "运行命令";
        public string ClickEventActionItem
        {
            get { return clickEventActionItem; }
            set
            {
                clickEventActionItem = value;
            }
        }
        private string clickEventValue = "";
        public string ClickEventValue
        {
            get { return clickEventValue; }
            set
            {
                clickEventValue = value;
            }
        }
        private string hoverEventActionItem = "显示文本";
        public string HoverEventActionItem
        {
            get { return hoverEventActionItem; }
            set
            {
                hoverEventActionItem = value;
            }
        }
        private string hoverEventValue = "";
        public string HoverEventValue
        {
            get { return hoverEventValue; }
            set
            {
                hoverEventValue = value;
            }
        }

        private string insertionValue = "";
        public string InsertionValue
        {
            get { return insertionValue; }
            set
            {
                insertionValue = value;
            }
        }

        #region 最终事件数据
        public string EventData
        {
            get
            {
                string result = "";
                string clickEventAction = written_book_datacontext.EventDataBase.Where(item => item.Value == ClickEventActionItem.Trim()).First().Key;
                string ClickEventString = HasClickEvent ? ",\"clickEvent\":{\"action\":\"" + clickEventAction + "\",\"value\":\"" + (clickEventAction.Trim() == "open_url"? "http://" : "") + ClickEventValue + "\"}" : "";

                string HoverEventString = HasHoverEvent ? ",\"hoverEvent\":{\"action\":\"" + (written_book_datacontext.EventDataBase.Where(item => item.Value == HoverEventActionItem.Trim()).First().Key) + "\",\"value\":\"" + HoverEventValue + "\"}" : "";
                string InsertionString = HasInsertion ? ",\"insertion\":{\"action\":\"" + (written_book_datacontext.EventDataBase.Where(item => item.Value == HoverEventActionItem.Trim()).First().Key) + "\",\"value\":\"" + HoverEventValue + "\"}" : "";
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
                    string textString = ObfuscateTimer.Enabled ? UID : Text;
                    textString = textString.Replace("\\", "\\\\\\\\").Replace("\"", "\\\\\"");
                    if (textString.Length > 0)
                    {
                        string colorString = Foreground.ToString().Remove(1, 2);
                        result = "{\"text\":\"" + textString + (IsLastRun ? "\\\\n" : "") + "\"" + (colorString != "#000000" ? ",\"color\":\"" + colorString + "\"" : "") + (FontStyle == FontStyles.Italic ? ",\"italic\":true" : "") + (FontWeight == FontWeights.Bold ? ",\"bold\":true" : "") + (TextDecorations.Contains(underlined_style) ? ",\"underlined\":true" : "") + (TextDecorations.Contains(strikethrough_style) ? ",\"strikethrough\":true" : "") + (IsObfuscated && ObfuscateTimer.Enabled ? ",\"obfuscated\":true" : "") + EventData + "},";
                    }
                    return result;
                }
                else
                    return "";
            }
        }
        #endregion

        #endregion

        /// <summary>
        /// 混淆文字控件
        /// </summary>
        public RichRun()
        {
            Obfuscates = written_book_datacontext.obfuscates;
            ObfuscateTimer.Tick += ObfuscateTick;
            MouseEnter += ObfuscateTextMouseEnter;
            MouseLeave += ObfuscateTextMouseLeave;
            MouseLeftButtonDown += ObfuscateTextMouseLeftButtonDown;
            MouseLeftButtonUp += ObfuscateTextMouseLeftButtonUp;
        }

        /// <summary>
        /// 鼠标抬起时启用混淆效果
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ObfuscateTextMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if(IsObfuscated)
            ObfuscateTimer.Enabled = true;
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
                ObfuscateTimer.Enabled = false;
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
                ObfuscateTimer.Enabled = true;
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
                ObfuscateTimer.Enabled = false;
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
            MaxContentLength = GeneralTools.GetTextWidth.Get(new Run(UID));
            ObfuscatesResult.Clear();
            for (int i = 0; i < UID.Length; i++)
                ObfuscatesResult.Append(Obfuscates[random.Next(0, Obfuscates.Count - 1)]);
            Text = ObfuscatesResult.ToString();
        }
    }
}
