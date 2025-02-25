using cbhk.CustomControls.Interfaces;
using cbhk.Interface.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using ICSharpCode.AvalonEdit.Document;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using static cbhk.Model.Common.Enums;

namespace cbhk.CustomControls.JsonTreeViewComponents
{
    public partial class JsonTreeViewItem : ObservableObject
    {
        #region Property
        [ObservableProperty]
        private JsonTreeViewItem _mutexNode;

        [ObservableProperty]
        public string _path = "";

        /// <summary>
        /// 键
        /// </summary>
        [ObservableProperty]
        public string _key = "";

        /// <summary>
        /// 所显示的键
        /// </summary>
        [ObservableProperty]
        public string _displayText = "";

        private string infoTiptext = "";

        /// <summary>
        /// 键作用提示
        /// </summary>
        public string InfoTiptext
        {
            get => infoTiptext;
            set
            {
                SetProperty(ref infoTiptext, value);
                if (infoTiptext.Trim().Length > 0)
                {
                    InfoIconVisibility = Visibility.Visible;
                }
                else
                    InfoIconVisibility = Visibility.Visible;
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
                SetProperty(ref errorTiptext, value);
                if (errorTiptext.Trim().Length > 0)
                {
                    ErrorIconVisibility = Visibility.Visible;
                }
                else
                    ErrorIconVisibility = Visibility.Visible;
            }
        }

        [ObservableProperty]
        public string _elementButtonTip = "添加在顶部";

        /// <summary>
        /// bool切换按钮可见性
        /// </summary>
        [ObservableProperty]
        public Visibility _boolButtonVisibility = Visibility.Collapsed;

        [ObservableProperty]
        public Visibility _removeElementButtonVisibility = Visibility.Collapsed;

        [ObservableProperty]
        public Visibility _sortButtonVisibility = Visibility.Collapsed;

        private bool mutuLock = false;
        private bool isFalse = true;
        public bool IsFalse
        {
            get => isFalse;
            set
            {
                SetProperty(ref isFalse, value);
                if (!mutuLock)
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
            }
        }

        private bool isTrue = false;
        public bool IsTrue
        {
            get => isTrue;
            set
            {
                SetProperty(ref isTrue, value);
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
            }
        }

        public IJsonItemTool JsonItemTool { get; set; }

        private dynamic defaultValue = null;
        public dynamic DefaultValue
        {
            get => defaultValue;
            set => SetProperty(ref defaultValue, value);
        }

        /// <summary>
        /// 可缺省
        /// </summary>
        [ObservableProperty]
        public bool _isCanBeDefaulted = false;

        [ObservableProperty]
        public Visibility _inputBoxVisibility = Visibility.Collapsed;

        [ObservableProperty]
        public TextComboBoxItem _currentValueType = null;

        private dynamic oldValue = null;
        /// <summary>
        /// 旧值
        /// </summary>
        public dynamic OldValue
        {
            get => oldValue;
            set => SetProperty(ref oldValue, value);
        }


        private dynamic currentValue = null;
        /// <summary>
        /// 当前值
        /// </summary>
        public dynamic Value
        {
            get => currentValue;
            set => SetProperty(ref currentValue, value);
        }

        /// <summary>
        /// 信息图标可见性
        /// </summary>
        [ObservableProperty]
        public Visibility _infoIconVisibility = Visibility.Collapsed;

        /// <summary>
        /// 错误图标可见性
        /// </summary>
        [ObservableProperty]
        public Visibility _errorIconVisibility = Visibility.Collapsed;

        /// <summary>
        /// 信息图标
        /// </summary>
        [ObservableProperty]
        public ImageSource _infoTipIcon = Application.Current.Resources["InfoIcon"] as DrawingImage;

        /// <summary>
        /// 错误图标
        /// </summary>
        [ObservableProperty]
        public ImageSource _errorTipIcon = Application.Current.Resources["ExclamationIcon"] as DrawingImage;

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

        [ObservableProperty]
        public SolidColorBrush _verifyBrush = Brushes.Gray;

        /// <summary>
        /// 起始行
        /// </summary>
        public DocumentLine StartLine { get; set; }

        /// <summary>
        /// 层数
        /// </summary>
        public int LayerCount { get; set; } = 1;

        [ObservableProperty]
        public DataType _dataType = Model.Common.Enums.DataType.None;

        /// <summary>
        /// 父节点
        /// </summary>
        public CompoundJsonTreeViewItem Parent { get; set; } = null;
        /// <summary>
        /// 上一个节点
        /// </summary>
        public JsonTreeViewItem Previous { get; set; } = null;
        /// <summary>
        /// 下一个节点
        /// </summary>
        public JsonTreeViewItem Next { get; set; } = null;
        #endregion

        #region Field
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
            #region Field
            ChangeType changeType = ChangeType.None;
            JsonTreeViewItem previous = Previous;
            JsonTreeViewItem next = Next;
            #endregion

            #region 判断是否需要直接返回
            if(string.IsNullOrEmpty(Value) && StartLine is null)
            {
                return;
            }
            #endregion

            #region 定位相邻的已有值的两个节点
            while (previous is not null && previous.StartLine is null)
            {
                if (previous.Previous is null)
                {
                    break;
                }
                previous = previous.Previous;
            }
            while (next is not null && next.StartLine is null)
            {
                if (next.Next is null)
                {
                    break;
                }
                next = next.Next;
            }
            #endregion

            if (this is CompoundJsonTreeViewItem thisCompoundJsonTreeViewItem1 && thisCompoundJsonTreeViewItem1.DataType is DataType.MultiType)
            {
                changeType = (ChangeType)Enum.Parse(typeof(ChangeType), thisCompoundJsonTreeViewItem1.SelectedEnumItem.Text, true);
                if(changeType is ChangeType.String)
                {

                }
                else
                if(changeType is ChangeType.NumberAndBool)
                {

                }
            }
            else
            if(this is not CompoundJsonTreeViewItem)
            {
                changeType = DataType is DataType.String ? ChangeType.String : ChangeType.NumberAndBool;
                string doubleQuotationMarks = changeType is ChangeType.String && Value is string stringValue && !stringValue.StartsWith('"') && !stringValue.EndsWith('"') ? "\"" : "";
                string currentValue = Value.ToString();
                if (string.IsNullOrEmpty(currentValue))
                {
                    int offset = StartLine.Offset, length = StartLine.Length;

                    if (previous is not null && previous.StartLine is not null)
                    {
                        if (previous is CompoundJsonTreeViewItem PreviousCompoundItem && PreviousCompoundItem.EndLine is not null)
                        {
                            offset = PreviousCompoundItem.EndLine.EndOffset;
                        }
                        else
                        {
                            offset = previous.StartLine.EndOffset;
                        }
                        if(next is null || (next is not null && next.StartLine is null))
                        {
                            offset--;
                        }
                        length = StartLine.EndOffset - offset;
                    }
                    else
                    if (Parent is not null)
                    {
                        offset = Parent.StartLine.EndOffset;
                        if ((previous is null && next is null) || ((previous is not null && previous.StartLine is not null) || (next is not null && next.StartLine is not null)))
                        {
                            length = StartLine.EndOffset - offset;
                        }
                        else
                        {
                            char lastChar = Parent.DataType is not DataType.Array ? '}' : ']';
                            string parentEndlineText = Plan.GetRangeText(Parent.EndLine.Offset, Parent.EndLine.Length);
                            int lastOffset = parentEndlineText.LastIndexOf(lastChar) + Parent.EndLine.Offset;
                            length = lastOffset - offset;
                        }
                    }

                    Plan.SetRangeText(offset, length, "");
                    bool unNormalState = Parent.EndLine is null || Parent.StartLine == Parent.EndLine || (Parent.EndLine is not null && Parent.EndLine.IsDeleted);
                    if (unNormalState && ((previous is null && next is null) || ((previous is not null && previous.StartLine is null) || (next is not null && next.StartLine is null))))
                    {
                        Parent.EndLine = Parent.StartLine;
                    }
                    StartLine = null;
                }
                else
                {
                    Plan.UpdateValueBySpecifyingInterval(this, changeType, doubleQuotationMarks + currentValue + doubleQuotationMarks);
                    #region 更新父节点的末行引用
                    if (Parent is not null && (Parent.EndLine is null || Parent.StartLine == Parent.EndLine) && StartLine is not null && ((previous is null && next is null) || ((previous is not null && previous.StartLine is null) || (next is not null && next.StartLine is null))))
                    {
                        Parent.EndLine = StartLine.NextLine;
                    }
                    #endregion
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
            if (window.DataContext is ICustomWorldUnifiedPlan customWorldUnifiedPlan)
            {
                string currentValue = ((bool)Value).ToString().ToLower();
                if (!IsFalse && !IsTrue)
                {
                    int offset = StartLine.Offset, length = StartLine.Length;

                    #region 定位相邻的已有值的两个节点
                    JsonTreeViewItem previous = Previous;
                    JsonTreeViewItem next = Next;
                    while (previous is not null && previous.StartLine is null)
                    {
                        if (previous.Previous is null)
                        {
                            break;
                        }
                        previous = previous.Previous;
                    }
                    while (next is not null && next.StartLine is null)
                    {
                        if (next.Next is null)
                        {
                            break;
                        }
                        next = next.Next;
                    }
                    #endregion

                    if (previous is not null)
                    {
                        if(previous is CompoundJsonTreeViewItem PreviousCompoundItem && PreviousCompoundItem.EndLine is not null)
                        {
                            offset = PreviousCompoundItem.EndLine.EndOffset;
                        }
                        else
                        {
                            offset = previous.StartLine.EndOffset;
                        }

                        if(next is null || (next is not null && next.StartLine is null))
                        {
                            offset--;
                        }
                        length = StartLine.EndOffset - offset;
                    }
                    else
                    if(Parent is not null)
                    {
                        offset = Parent.StartLine.EndOffset;
                        if (Parent.Children.Count > 1)
                        {
                            length = StartLine.EndOffset - offset;
                        }
                        else
                        {
                            char lastChar = Parent.DataType is not DataType.Array ? '}' : ']';
                            string parentEndlineText = Plan.GetRangeText(Parent.EndLine.Offset, Parent.EndLine.Length);
                            int lastOffset = parentEndlineText.LastIndexOf(',') + 1;
                            if (lastOffset == 0)
                            {
                                lastOffset = parentEndlineText.LastIndexOf(lastChar);
                            }
                            length = lastOffset - offset;
                        }
                    }
                    
                    customWorldUnifiedPlan.SetRangeText(offset, length, "");

                    if ((previous is null && next is null) || ((previous is not null && previous.StartLine is null) || (next is not null && next.StartLine is null)))
                    {
                        Parent.EndLine = Parent.StartLine;
                    }

                    StartLine = null;
                }
                else
                {
                    customWorldUnifiedPlan.UpdateValueBySpecifyingInterval(this, ChangeType.NumberAndBool, currentValue);
                }
            }
        }
        #endregion
    }
}