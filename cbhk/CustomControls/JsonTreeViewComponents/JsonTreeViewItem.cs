using cbhk.CustomControls.Interfaces;
using cbhk.Interface.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using ICSharpCode.AvalonEdit.Document;
using System.Collections.ObjectModel;
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

        public IJsonItemTool JsonItemTool;

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

        /// <summary>
        /// 枚举数据源
        /// </summary>
        [ObservableProperty]
        public ObservableCollection<TextComboBoxItem> _enumItemsSource = [];

        public TextComboBoxItem oldSelectedEnumItem;

        /// <summary>
        /// 已选中的枚举成员
        /// </summary>
        [ObservableProperty]
        public TextComboBoxItem _selectedEnumItem = null;

        /// <summary>
        /// 枚举下拉框可见性
        /// </summary>
        [ObservableProperty]
        public Visibility _enumBoxVisibility = Visibility.Collapsed;

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
            if(this is CompoundJsonTreeViewItem)
            {
                return;
            }

            string currentValue = "\"" + Value.ToString() + "\"";
            if (string.IsNullOrEmpty(currentValue))
            {
                int offset = 1, length = StartLine.Length;

                #region 定位上一个已有值的节点
                JsonTreeViewItem previous = Previous;
                while (previous is not null && previous.StartLine is null)
                {
                    if (previous.Previous is null)
                    {
                        break;
                    }
                    previous = previous.Previous;
                }
                #endregion

                if (previous is not null)
                {
                    if (previous is CompoundJsonTreeViewItem PreviousCompoundItem && PreviousCompoundItem.EndLine is not null)
                    {
                        offset = PreviousCompoundItem.EndLine.EndOffset - 1;
                    }
                    else
                    {
                        offset = previous.StartLine.EndOffset;
                    }
                    length = StartLine.EndOffset - offset;
                }

                Plan.SetRangeText(offset, length, "");
                StartLine = null;
            }
            else
            {
                Plan.UpdateValueBySpecifyingInterval(this, ChangeType.String, currentValue);
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
                    int offset = 1, length = StartLine.Length;

                    #region 定位上一个已有值的节点
                    JsonTreeViewItem previous = Previous;
                    while (previous is not null && previous.StartLine is null)
                    {
                        if (previous.Previous is null)
                        {
                            break;
                        }
                        previous = previous.Previous;
                    }
                    #endregion

                    if (previous is not null)
                    {
                        if(previous is CompoundJsonTreeViewItem PreviousCompoundItem && PreviousCompoundItem.EndLine is not null)
                        {
                            offset = PreviousCompoundItem.EndLine.EndOffset - 1;
                        }
                        else
                        {
                            offset = previous.StartLine.EndOffset;
                        }
                        length = StartLine.EndOffset - offset;
                    }
                    
                    customWorldUnifiedPlan.SetRangeText(offset, length, "");
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