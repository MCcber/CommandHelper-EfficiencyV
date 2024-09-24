using cbhk.CustomControls;
using CBHKTester.Interface;
using ICSharpCode.AvalonEdit.Document;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using static cbhk.CustomControls.JsonTreeViewComponents.Enums;

namespace CBHKTester.Model
{
    public partial class JsonTreeViewItem : ICloneable
    {
        #region Property
        
        public string Path = "";

        /// <summary>
        /// 键
        /// </summary>
        
        public string Key = "";

        /// <summary>
        /// 所显示的键
        /// </summary>
        
        public string DisplayText = "";

        private string infoTiptext = "";

        /// <summary>
        /// 键作用提示
        /// </summary>
        public string InfoTiptext
        {
            get => infoTiptext;
            set => infoTiptext = value;
        }

        private string errorTiptext = "";

        /// <summary>
        /// 值错误提示
        /// </summary>
        public string ErrorTiptext
        {
            get => errorTiptext;
            set => errorTiptext = value;
        }

        
        public string ElementButtonTip = "添加在顶部";

        /// <summary>
        /// bool切换按钮可见性
        /// </summary>
        
        public Visibility BoolButtonVisibility = Visibility.Collapsed;

        
        public Visibility RemoveElementButtonVisibility = Visibility.Collapsed;

        
        public Visibility SortButtonVisibility = Visibility.Collapsed;

        private bool isFalse = true;
        public bool IsFalse
        {
            get => isFalse;
            set => isFalse = value;
        }

        private bool isTrue = false;
        public bool IsTrue
        {
            get => isTrue;
            set => isTrue = value;
        }

        public IJsonItemTool JsonItemTool;

        private dynamic defaultValue = null;
        public dynamic DefaultValue
        {
            get => defaultValue;
            set => defaultValue = value;
        }

        
        public DataTypes DefaultType = DataTypes.Input;

        /// <summary>
        /// 可缺省
        /// </summary>
        
        public bool IsCanBeDefaulted = false;

        /// <summary>
        /// 枚举数据源
        /// </summary>
        
        public List<TextComboBoxItem> EnumItemsSource = [];

        public TextComboBoxItem oldSelectedEnumItem;

        /// <summary>
        /// 已选中的枚举成员
        /// </summary>
        
        public TextComboBoxItem SelectedEnumItem = null;

        /// <summary>
        /// 枚举下拉框可见性
        /// </summary>
        
        public Visibility EnumBoxVisibility = Visibility.Collapsed;

        
        public Visibility InputBoxVisibility = Visibility.Collapsed;

        
        public TextComboBoxItem CurrentValueType = null;

        private dynamic oldValue = "0";
        /// <summary>
        /// 旧值
        /// </summary>
        public dynamic OldValue
        {
            get => oldValue;
            set => oldValue = value;
        }


        private dynamic currentValue = null;
        /// <summary>
        /// 当前值
        /// </summary>
        public dynamic Value
        {
            get => currentValue;
            set => currentValue = value;
        }

        /// <summary>
        /// 信息图标可见性
        /// </summary>
        
        public Visibility InfoIconVisibility = Visibility.Collapsed;

        /// <summary>
        /// 错误图标可见性
        /// </summary>
        
        public Visibility ErrorIconVisibility = Visibility.Collapsed;

        /// <summary>
        /// 信息图标
        /// </summary>

        public ImageSource InfoTipIcon = null;

        /// <summary>
        /// 错误图标
        /// </summary>

        public ImageSource ErrorTipIcon = null;

        /// <summary>
        /// 最小值
        /// </summary>
        public string MinValue { get; set; }

        /// <summary>
        /// 最大值
        /// </summary>
        public string MaxValue { get; set; }

        /// <summary>
        /// 倍率模式
        /// </summary>
        public bool MultiplierMode { get; set; }

        /// <summary>
        /// 倍率
        /// </summary>
        public int Magnification { get; set; }

        /// <summary>
        /// 值区间
        /// </summary>
        public string RangeType { get; set; }

        
        public SolidColorBrush VerifyBrush = Brushes.Gray;

        /// <summary>
        /// 起始行号
        /// </summary>
        public int StartLineNumber { get; set; }

        /// <summary>
        /// 起始行
        /// </summary>
        public DocumentLine StartLine { get; set; }

        /// <summary>
        /// 层数
        /// </summary>
        public int LayerCount { get; set; } = 1;

        
        public DataTypes DataType = DataTypes.Input;

        /// <summary>
        /// 父节点
        /// </summary>
        public CompoundJsonTreeViewItem Parent { get; set; } = null;
        /// <summary>
        /// 上一个节点
        /// </summary>
        public JsonTreeViewItem Last { get; set; } = null;
        /// <summary>
        /// 下一个节点
        /// </summary>
        public JsonTreeViewItem Next { get; set; } = null;
        #endregion

        #region Field
        public static List<string> NumberTypes = ["TAG_Byte", "TAG_Short", "TAG_Int", "TAG_Float", "TAG_Double", "TAG_Long", "TAG_Decimal"];
        private SolidColorBrush CorrectBrush = Brushes.Gray;
        private SolidColorBrush ErrorBrush = Brushes.Red;
        public ICustomWorldUnifiedPlan Plan { get; set; } = null;
        #endregion

        #region Event
        /// <summary>
        /// 获取焦点时设置旧值
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void TextBox_GotFocus(object sender, RoutedEventArgs e) => OldValue = (sender as TextBox).Text;

        /// <summary>
        /// 处理字符串及数字类型的值更新
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (StartLine.IsDeleted && Last is not null)
            {
                StartLine = Plan.GetLineByNumber(Last is CompoundJsonTreeViewItem compoundJsonTreeViewItem && compoundJsonTreeViewItem.EndLine is not null ? compoundJsonTreeViewItem.EndLine.LineNumber + 1 : Last.StartLine.LineNumber + 1);
            }
            if ((Value is not null && ((string)Value).Trim().Length > 0 && (Last is null || Last.StartLine.LineNumber != StartLine.LineNumber)) || DefaultValue is not null || (DefaultValue is null && SelectedEnumItem is not null))
            {
                if (((string)Value).Trim().Length == 0)
                    Value = DefaultValue;
                Plan.UpdateValueBySpecifyingInterval(this, DataType is DataTypes.Input ? ReplaceType.Input : ReplaceType.String, Value + "", Next is null);
            }
            else
            if (Value is not null && ((string)Value).Trim().Length > 0 && DefaultValue is null)//有值
            {
                StartLine = Last is CompoundJsonTreeViewItem compoundJsonTreeViewItem && compoundJsonTreeViewItem.EndLine is not null ? compoundJsonTreeViewItem.EndLine : Last.StartLine;
                Plan.UpdateNullValueBySpecifyingInterval(StartLine.EndOffset, "\r\n" + new string(' ', LayerCount * 2) + "\"" + Key + "\": " + Value + ",");
                StartLine = Plan.GetLineByNumber(StartLine.LineNumber + 1);
            }
            else
            if (OldValue != Value && Value is not null)//无值时删除本行，回归初始状态
            {
                DocumentLine lastLine = Plan.GetLineByNumber(StartLine.LineNumber - 1);
                DocumentLine nextLine = Plan.GetLineByNumber(StartLine.LineNumber + 1);
                Plan.DeleteAllLinesInTheSpecifiedRange(StartLine.Offset, nextLine.Offset);
                StartLine = lastLine;
            }
        }

        /// <summary>
        /// 获得焦点时设置旧的枚举值
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void ComboBox_GotFocus(object sender, RoutedEventArgs e)
        {
            oldSelectedEnumItem = SelectedEnumItem;
            OldValue = Value;
        }

        /// <summary>
        /// 处理布尔值的更新
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void BoolButton_Click(object sender, RoutedEventArgs e)
        {
            FrameworkElement toggleButton = sender as FrameworkElement;
            Window window = Window.GetWindow(toggleButton);
            if (window.DataContext is ICustomWorldUnifiedPlan customWorldUnifiedPlan)
            {
                string currentValue = ((bool)Value).ToString().ToLower();
                customWorldUnifiedPlan.UpdateValueBySpecifyingInterval(this, ReplaceType.Input, currentValue, Next is null);
            }
        }

        public object Clone()
        {
            #region 处理各类属性
            JsonTreeViewItem result = new()
            {
                StartLineNumber = StartLineNumber,
                StartLine = StartLine,
                CurrentValueType = CurrentValueType,
                DataType = DataType,
                DisplayText = DisplayText,
                EnumItemsSource = EnumItemsSource,
                ErrorTipIcon = ErrorTipIcon,
                ErrorTiptext = ErrorTiptext,
                InfoTipIcon = InfoTipIcon,
                InfoTiptext = InfoTiptext,
                InputBoxVisibility = InputBoxVisibility,
                ErrorIconVisibility = ErrorIconVisibility,
                InfoIconVisibility = InfoIconVisibility,
                IsFalse = IsFalse,
                IsTrue = IsTrue,
                Key = Key,
                Last = Last,
                Next = Next,
                LayerCount = LayerCount,
                Magnification = Magnification,
                MaxValue = MaxValue,
                MinValue = MinValue,
                MultiplierMode = MultiplierMode,
                Parent = Parent,
                Path = Path,
                JsonItemTool = JsonItemTool,
                Plan = Plan,
                RangeType = RangeType,
                SelectedEnumItem = SelectedEnumItem,
                Value = Value
            };
            #endregion

            return result;
        }
        #endregion
    }
}
