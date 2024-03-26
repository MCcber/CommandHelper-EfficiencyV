using cbhk.CustomControls.Interfaces;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace cbhk.CustomControls.JsonTreeViewComponents
{
    public class JsonTreeViewItem(int lineNumber, int lineStartPosition): INotifyPropertyChanged
    {
        #region Property
        private string path;

        public string Path
        {
            get => path;
            set
            {
                path = value;
                OnPropertyChanged();
            }
        }


        private string key = "";

        /// <summary>
        /// 键
        /// </summary>
        public string Key
        {
            get => key;
            set
            {
                key = value;
                OnPropertyChanged();
            }
        }

        private string displayText = "";

        /// <summary>
        /// 所显示的键
        /// </summary>
        public string DisplayText
        {
            get => displayText;
            set
            {
                displayText = value;
                OnPropertyChanged();
            }
        }

        private string infoTiptext = "";

        /// <summary>
        /// 键作用提示
        /// </summary>
        public string InfoTiptext
        {
            get => infoTiptext;
            set
            {
                infoTiptext = value;
                if (infoTiptext.Trim().Length > 0)
                {
                    InfoIconVisibility = Visibility.Visible;
                    InfoTipIcon = InfoTipIcon;
                }
                else
                    InfoIconVisibility = Visibility.Visible;
                OnPropertyChanged();
            }
        }

        private string errorTiptext = "";

        /// <summary>
        /// 值错误提示
        /// </summary>
        public string ErrorTiptext
        {
            get => errorTiptext;
            set
            {
                errorTiptext = value;
                if (errorTiptext.Trim().Length > 0)
                {
                    ErrorIconVisibility = Visibility.Visible;
                    ErrorTipIcon = ErrorTipIcon;
                }
                else
                    ErrorIconVisibility = Visibility.Visible;
                OnPropertyChanged();
            }
        }

        private Visibility boolButtonVisibility = Visibility.Collapsed;

        /// <summary>
        /// bool切换按钮可见性
        /// </summary>
        public Visibility BoolButtonVisibility
        {
            get => boolButtonVisibility;
            set
            {
                boolButtonVisibility = value;
                OnPropertyChanged();
            }
        }

        private bool mutuLock = false;
        private bool isFalse = true;
        public bool IsFalse
        {
            get => isFalse;
            set
            {
                isFalse = value;
                if(!mutuLock)
                {
                    mutuLock = true;
                    if (!IsCanBeDefaulted)
                        IsTrue = !IsFalse;
                    else
                        if (IsFalse)
                        IsTrue = false;
                    mutuLock = false;
                }
                if (IsFalse)
                    Value = false;
                OnPropertyChanged();
            }
        }

        private bool isTrue = false;
        public bool IsTrue
        {
            get => isTrue;
            set
            {
                isTrue = value;
                if (!mutuLock)
                {
                    mutuLock = true;
                    if (!IsCanBeDefaulted)
                        IsFalse = !IsTrue;
                    else
                        if (IsTrue)
                        IsFalse = false;
                    mutuLock = false;
                }
                if (IsTrue)
                    Value = true;
                OnPropertyChanged();
            }
        }

        private bool isCanBeDefaulted = false;

        /// <summary>
        /// 可缺省
        /// </summary>
        public bool IsCanBeDefaulted
        {
            get => isCanBeDefaulted;
            set
            {
                isCanBeDefaulted = value;
                OnPropertyChanged();
            }
        }

        private bool isEnumType = false;

        /// <summary>
        /// 为枚举类型
        /// </summary>
        public bool IsEnumType
        {
            get => isEnumType;
            set
            {
                isEnumType = value;
                if (isEnumType)
                    EnumBoxVisibility = Visibility.Visible;
                else
                    EnumBoxVisibility = Visibility.Collapsed;
                OnPropertyChanged();
            }
        }

        private ObservableCollection<TextComboBoxItem> enumItemsSource = [];

        /// <summary>
        /// 枚举数据源
        /// </summary>
        public ObservableCollection<TextComboBoxItem> EnumItemsSource
        {
            get => enumItemsSource;
            set
            {
                enumItemsSource = value;
                OnPropertyChanged();
            }
        }

        public TextComboBoxItem oldSelectedEnumItem = null;
        private TextComboBoxItem selectedEnumItem;

        /// <summary>
        /// 已选中的枚举成员
        /// </summary>
        public TextComboBoxItem SelectedEnumItem
        {
            get => selectedEnumItem;
            set
            {
                selectedEnumItem = value;
                OnPropertyChanged();
            }
        }


        private Visibility enumBoxVisibility = Visibility.Collapsed;

        /// <summary>
        /// 枚举下拉框可见性
        /// </summary>
        public Visibility EnumBoxVisibility
        {
            get => enumBoxVisibility;
            set
            {
                enumBoxVisibility = value;
                OnPropertyChanged();
            }
        }


        private bool isBoolType = false;
        /// <summary>
        /// 是否为bool
        /// </summary>
        public bool IsBoolType
        {
            get => isBoolType;
            set
            {
                isBoolType = value;
                if (isBoolType)
                {
                    BoolButtonVisibility = Visibility.Visible;
                    InputBoxVisibility = Visibility.Collapsed;
                }
                else
                    BoolButtonVisibility = Visibility.Collapsed;
                OnPropertyChanged();
            }
        }

        private bool isNumberType = false;

        /// <summary>
        /// 是否为实数
        /// </summary>
        public bool IsNumberType
        {
            get => isNumberType;
            set
            {
                isNumberType = value;
                if (isNumberType)
                    InputBoxVisibility = Visibility.Visible;
                else
                    InputBoxVisibility = Visibility.Collapsed;
                OnPropertyChanged();
            }
        }

        private bool isStringType;

        /// <summary>
        /// 是否为字符串
        /// </summary>
        public bool IsStringType
        {
            get => isStringType;
            set
            {
                isStringType = value;
                if (isStringType)
                    InputBoxVisibility = Visibility.Visible;
                else
                    InputBoxVisibility = Visibility.Collapsed;
                OnPropertyChanged();
            }
        }


        private bool isCompoundType;

        /// <summary>
        /// 是否为复合节点
        /// </summary>
        public bool IsCompoundType
        {
            get => isCompoundType;
            set
            {
                isCompoundType = value;
                if (isCompoundType)
                    SwitchBoxVisibility = Visibility.Visible;
                else
                    SwitchBoxVisibility = Visibility.Collapsed;
                OnPropertyChanged();
            }
        }

        private Visibility inputBoxVisibility = Visibility.Collapsed;

        public Visibility InputBoxVisibility
        {
            get => inputBoxVisibility;
            set
            {
                inputBoxVisibility = value;
                OnPropertyChanged();
            }
        }


        private Visibility switchBoxVisibility = Visibility.Collapsed;

        public Visibility SwitchBoxVisibility
        {
            get => switchBoxVisibility;
            set
            {
                switchBoxVisibility = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<TextComboBoxItem> ValueTypeList { get; set; } = [];

        private TextComboBoxItem currentValueType;
        public TextComboBoxItem CurrentValueType
        {
            get => currentValueType;
            set
            {
                currentValueType = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 当前值
        /// </summary>
        public dynamic Value { get; set; }

        private Visibility infoIconVisibility = Visibility.Collapsed;
        /// <summary>
        /// 信息图标可见性
        /// </summary>
        public Visibility InfoIconVisibility
        {
            get => infoIconVisibility;
            set
            {
                infoIconVisibility = value;
                OnPropertyChanged();
            }
        }

        private Visibility errorIconVisibility = Visibility.Collapsed;
        /// <summary>
        /// 错误图标可见性
        /// </summary>
        public Visibility ErrorIconVisibility
        {
            get => errorIconVisibility;
            set
            {
                errorIconVisibility = value;
                OnPropertyChanged();
            }
        }

        private ImageSource infoTipIcon = Application.Current.Resources["InfoIcon"] as DrawingImage;
        /// <summary>
        /// 信息图标
        /// </summary>
        public ImageSource InfoTipIcon
        {
            get => infoTipIcon;
            set
            {
                infoTipIcon = value;
                OnPropertyChanged();
            }
        }

        private ImageSource errorTipIcon = Application.Current.Resources["ExclamationIcon"] as DrawingImage;
        /// <summary>
        /// 错误图标
        /// </summary>
        public ImageSource ErrorTipIcon
        {
            get => errorTipIcon;
            set
            {
                errorTipIcon = value;
                OnPropertyChanged();
            }
        }

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

        private SolidColorBrush verifyBrush = Brushes.Gray;

        public SolidColorBrush VerifyBrush
        {
            get => verifyBrush;
            set
            {
                verifyBrush = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// 行号
        /// </summary>
        public int LineNumber { get; set; } = lineNumber;

        /// <summary>
        /// 行偏移
        /// </summary>
        public int LineStartPosition { get; set; } = lineStartPosition;

        /// <summary>
        /// 层数
        /// </summary>
        public int LayerCount { get; set; } = 1;

        private Enums.DataTypes DataType { get; set; } = Enums.DataTypes.String;

        private bool isArray { get; set; } = false;
        /// <summary>
        /// 是否为Json数组
        /// </summary>
        public bool IsArray
        {
            get => isArray;
            set
            {
                isArray = value;
                if (isArray)
                    AddElementButtonVisibility = Visibility.Visible;
                else
                    AddElementButtonVisibility = Visibility.Collapsed;
                OnPropertyChanged();
            }
        }

        private Visibility addElementButtonVisibility = Visibility.Collapsed;

        public Visibility AddElementButtonVisibility
        {
            get => addElementButtonVisibility;
            set
            {
                addElementButtonVisibility = value;
                OnPropertyChanged();
            }
        }


        /// <summary>
        /// 父节点
        /// </summary>
        public JsonTreeViewItem Parent { get; set; } = null;
        /// <summary>
        /// 上一个节点
        /// </summary>
        public JsonTreeViewItem Last { get; set; } = null;

        /// <summary>
        /// 子节点集合
        /// </summary>
        public ObservableCollection<JsonTreeViewItem> Children { get; set; } = [];

        /// <summary>
        /// 可切换的子节点集合
        /// </summary>

        private ObservableCollection<JsonTreeViewItem> switchChildren = [];
        public ObservableCollection<JsonTreeViewItem> SwitchChildren
        {
            get => switchChildren;
            set
            {
                switchChildren = value;
                OnPropertyChanged();
            }
        }
        #endregion

        #region Field
        public static List<string> NumberTypes = ["TAG_Byte", "TAG_Short", "TAG_Int", "TAG_Float", "TAG_Double", "TAG_Long","TAG_Decimal"];
        private SolidColorBrush CorrectBrush = Brushes.Gray;
        private SolidColorBrush ErrorBrush = Brushes.Red;
        #endregion

        /// <summary>
        /// 属性变更事件
        /// </summary>
        /// <param name="propertyName"></param>
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            if (propertyName is not null)
            {
                switch (propertyName)
                {
                    case "CurrentValueType":
                        Children.Clear();
                        int currentindex = ValueTypeList.IndexOf(CurrentValueType);
                        if (SwitchChildren.Count > 0)
                            Children.Add(SwitchChildren[currentindex]);
                        break;
                }
            }
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// 处理字符串及数字类型的值更新
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
        }

        /// <summary>
        /// 处理枚举值和数据类型的变更
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox comboBox = sender as ComboBox;
            Window window = Window.GetWindow(comboBox);
            if (window.DataContext is ICustomWorldUnifiedPlan customWorldUnifiedPlan)
            {
                switch (comboBox.Uid)
                {
                    case "ValueTypeList":
                        break;
                    case "EnumItemsSource":
                        oldSelectedEnumItem = SelectedEnumItem;
                        break;
                }
            }
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
            if(window.DataContext is ICustomWorldUnifiedPlan customWorldUnifiedPlan)
            {
                JsonTreeViewContext context = customWorldUnifiedPlan.KeyValueOffsetDictionary[Path];
                string currentValue = (!(bool)Value).ToString();
                customWorldUnifiedPlan.UpdateValueBySpecifyingInterval(new JsonViewInterval() { StartOffset = context.ValueStartOffset, EndOffset = context.ValueEndOffset }, currentValue);
                customWorldUnifiedPlan.UpdateAllSubsequentOffsetMembers(this, toggleButton);
                context.ValueEndOffset = context.ValueStartOffset + currentValue.Length;
            }
        }

        /// <summary>
        /// 更新自身键值对偏移量
        /// </summary>
        /// <param name="plan"></param>
        public void UpdateSelfOffset(ICustomWorldUnifiedPlan plan)
        {
            JsonTreeViewContext context = plan.KeyValueOffsetDictionary[Path];
            JsonTreeViewContext lastContext = plan.KeyValueOffsetDictionary[Last.Path];
            context.KeyStartOffset = lastContext.ValueEndOffset + LayerCount * 4;
            context.KeyEndOffset = context.KeyStartOffset + context.Item.Value.Length + 2;
            context.ValueStartOffset = context.KeyEndOffset + 2;
            context.ValueEndOffset = context.ValueStartOffset + context.Item.Value.Length;
        }

        ///// <summary>
        ///// 通过偏移量获取对应的节点
        ///// </summary>
        ///// <param name="Offset">目标偏移量</param>
        ///// <returns>找到的节点</returns>
        //public static JsonTreeViewItem GetItemByOffset(Dictionary<string, JsonTreeViewItem> FlatJsonTreeViewItem, int lineNumber, int Offset)
        //{
        //    JsonTreeViewItem result = null;
        //    Parallel.ForEach(FlatJsonTreeViewItem.Keys, (key, ParallelLoopState) =>
        //    {
        //        JsonTreeViewItem CurrentTreeViewItem = FlatJsonTreeViewItem[key];
        //        int number = CurrentTreeViewItem.LineNumber;
        //        int position = CurrentTreeViewItem.LineStartPosition;
        //        //偏移量在区间内并且需要节点的Json格式合法
        //        if (number == lineNumber && position == Offset && CurrentTreeViewItem.IsItTrue())
        //        {
        //            result = CurrentTreeViewItem;
        //            ParallelLoopState.Break();
        //        }
        //    });
        //    return result;
        //}

        ///// <summary>
        ///// 通过修改区间更新树的数据
        ///// </summary>
        ///// <param name="StartOffset">起始偏移量</param>
        ///// <param name="EndOffset">末尾偏移量</param>
        //public static void UpdateTreeDataByRange(Dictionary<string, JsonTreeViewItem> FlatJsonTreeViewItem, int StartLineIndex, int EndLineIndex, int StartOffset, int EndOffset)
        //{
        //    JsonTreeViewItem startTargetItem = GetItemByOffset(FlatJsonTreeViewItem, StartLineIndex, StartOffset);
        //    JsonTreeViewItem endTargetItem = GetItemByOffset(FlatJsonTreeViewItem, EndLineIndex, EndOffset);
        //    if (startTargetItem is not null)
        //    {
        //        startTargetItem.CurrentJsonData = startTargetItem.CurrentJsonData[..StartOffset];
        //    }
        //    if (endTargetItem is not null)
        //    {
        //        endTargetItem.CurrentJsonData = endTargetItem.CurrentJsonData[..EndOffset];
        //    }
        //}

        ///// <summary>
        ///// 验证目标Json是否合法
        ///// </summary>
        ///// <returns></returns>
        //public bool IsItTrue()
        //{
        //    //string data = PresetJsonData[CurrentDataType];
        //    //try
        //    //{
        //    //    _ = new JsonTextReader(new StringReader(data));
        //    //    return true;
        //    //}
        //    //catch
        //    //{
        //    //    return false;
        //    //}
        //    return true;
        //}

        ///// <summary>
        ///// 逐层向上遍历验证合法性
        ///// </summary>
        //public JsonTreeViewItem VerifyLegalityLayerByLayerUpwards()
        //{
        //    JsonTreeViewItem ParentItem = Parent;
        //    while (ParentItem is not null)
        //    {
        //        if (!ParentItem.IsItTrue())
        //            ParentItem = ParentItem.Parent;
        //        else
        //            break;
        //    }
        //    return ParentItem;
        //}
    }
}
