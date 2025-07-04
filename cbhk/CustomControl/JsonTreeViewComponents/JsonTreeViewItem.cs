using CBHK.CustomControl.Interfaces;
using CBHK.Service.Json;
using CBHK.ViewModel.Common;
using CommunityToolkit.Mvvm.ComponentModel;
using ICSharpCode.AvalonEdit.Document;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using static CBHK.Model.Common.Enums;

namespace CBHK.CustomControl.JsonTreeViewComponents
{
    public partial class JsonTreeViewItem : ObservableObject
    {
        #region Field
        private SolidColorBrush CorrectBrush = Brushes.Gray;
        private SolidColorBrush ErrorBrush = Brushes.Red;
        public ICustomWorldUnifiedPlan Plan { get; set; } = null;
        public TreeViewItem currentItemReference = null;
        #endregion

        #region Property
        /// <summary>
        /// 当前节点在父节点中的索引
        /// </summary>
        public int Index { get; set; }

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
        public string InfoTipText
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
        public DataType _dataType = DataType.None;

        /// <summary>
        /// 父节点
        /// </summary>
        public BaseCompoundJsonTreeViewItem Parent { get; set; } = null;
        /// <summary>
        /// 上一个节点(逻辑)
        /// </summary>
        public JsonTreeViewItem LogicPrevious { get; set; } = null;
        /// <summary>
        /// 下一个节点(逻辑)
        /// </summary>
        public JsonTreeViewItem LogicNext { get; set; } = null;
        /// <summary>
        /// 上一个节点(视觉)
        /// </summary>
        public JsonTreeViewItem VisualPrevious { get; set; } = null;
        /// <summary>
        /// 下一个节点(视觉)
        /// </summary>
        public JsonTreeViewItem VisualNext { get; set; } = null;

        public SolidColorBrush PressedPlusColor { get; } = new((Color)ColorConverter.ConvertFromString("#2AA515"));
        public SolidColorBrush PressedMinusColor { get; } = new((Color)ColorConverter.ConvertFromString("#2AA515"));
        [ObservableProperty]
        public Brush _pressedSwitchButtonColor = new SolidColorBrush();
        #endregion

        #region Method
        /// <summary>
        /// 移动数组元素节点
        /// </summary>
        /// <param name="direction">移动方向</param>
        private void MoveItem(MoveDirection direction)
        {
            #region Field
            int offset = 0, length = 0;
            DocumentLine targetLine = null;
            string currentString = "";
            JsonTreeViewItem previous = LogicPrevious;
            JsonTreeViewItem next = LogicNext;
            #endregion
            #region 从父级删除当前节点
            int insertIndex = Parent.LogicChildren.IndexOf(this);
            if ((direction is MoveDirection.Down && LogicNext is not null) || (direction is MoveDirection.Up && LogicPrevious is not null))
            {
                Parent.LogicChildren.Remove(this);
            }
            else
            {
                return;
            }
            #endregion
            #region 处理前后节点关系
            if (direction is MoveDirection.Down)
            {
                JsonTreeViewItem nextNext = LogicNext.LogicNext;
                if (LogicNext is BaseCompoundJsonTreeViewItem nextComoundItem && nextComoundItem.EndLine is not null && !nextComoundItem.EndLine.IsDeleted)
                {
                    targetLine = nextComoundItem.EndLine;
                }
                else
                {
                    targetLine = LogicNext.StartLine;
                }
                if (previous is not null)
                {
                    previous.LogicNext = next;
                }
                next.LogicPrevious = previous;
                next.LogicNext = this;
                LogicNext = nextNext;
                LogicPrevious = next;
                if(nextNext is not null)
                {
                    nextNext.LogicPrevious = this;
                }
                insertIndex++;
            }
            else
            if (direction is MoveDirection.Up)
            {
                JsonTreeViewItem previousPrevious = LogicPrevious.LogicPrevious;
                if (LogicPrevious.LogicPrevious is not null)
                {
                    targetLine = LogicPrevious.LogicPrevious.StartLine.PreviousLine;
                }
                else
                {
                    targetLine = Parent.StartLine;
                }
                previous.LogicPrevious = this;
                previous.LogicNext = next;
                if (next is not null)
                {
                    next.LogicPrevious = previous;
                }
                if (previousPrevious is not null)
                {
                    previousPrevious.LogicNext = this;
                }
                LogicPrevious = previousPrevious;
                LogicNext = previous;
                insertIndex--;
            }
            #endregion
            #region 保存并切除当前节点的Json文本
            offset = StartLine.PreviousLine.EndOffset;
            length = this is BaseCompoundJsonTreeViewItem compoundJsonTreeViewItem && compoundJsonTreeViewItem.EndLine is not null && !compoundJsonTreeViewItem.EndLine.IsDeleted ? compoundJsonTreeViewItem.EndLine.EndOffset - offset : StartLine.EndOffset - offset;
            currentString = Plan.GetRangeText(offset, length);
            Plan.SetRangeText(offset,length,"");
            #endregion
            #region 放置处理引用后当前节点的Json文本
            Plan.SetRangeText(targetLine.EndOffset,0,currentString);
            #endregion
            #region 处理放置后当前节点的行应用
            StartLine = targetLine.NextLine;
            if (this is BaseCompoundJsonTreeViewItem thisCompoundItem1)
            {
                JsonItemTool.SetLineNumbersForEachSubItem(thisCompoundItem1.LogicChildren, thisCompoundItem1);
                JsonTreeViewItem resultItem = thisCompoundItem1.VisualLastChild;
                if(resultItem is BaseCompoundJsonTreeViewItem resultCompoundItem && resultCompoundItem.EndLine is not null)
                {
                    thisCompoundItem1.EndLine = resultCompoundItem.EndLine.NextLine;
                }
                else
                if(resultItem is not null)
                {
                    thisCompoundItem1.EndLine = resultItem.StartLine.NextLine;
                }
            }
            #endregion
            #region 将当前节点添加回父节点的子级并处理逗号
            Parent.LogicChildren.Insert(insertIndex, this);

            if(direction is MoveDirection.Up)
            {
                Index--;
                LogicNext.Index++;
            }
            else
            {
                Index++;
                LogicPrevious.Index--;
            }

            Parent.SetVisualPreviousAndNextForEachItem();
            Tuple<JsonTreeViewItem, JsonTreeViewItem> previousAndNext = Parent.SearchVisualPreviousAndNextItem(this);
            if (previousAndNext is not null && previousAndNext.Item2 is not null)
            {
                Parent.VisualLastChild = previousAndNext.Item2;
            }

            DocumentLine currentLine = null;
            if (direction is MoveDirection.Down)
            {
                DocumentLine previousEndLine = null;
                if(LogicPrevious is BaseCompoundJsonTreeViewItem previousCompoundItem && previousCompoundItem.EndLine is not null && !previousCompoundItem.EndLine.IsDeleted)
                {
                    previousEndLine = previousCompoundItem.EndLine;
                }
                else
                if(LogicPrevious is not null)
                {
                    previousEndLine = LogicPrevious.StartLine;
                }
                string previousEndlineString = Plan.GetRangeText(previousEndLine.Offset, previousEndLine.Length);
                if(!previousEndlineString.EndsWith(','))
                {
                    Plan.SetRangeText(previousEndLine.EndOffset, 0, ",");
                }
                if (this is BaseCompoundJsonTreeViewItem thisCompoundItem2)
                {
                    JsonTreeViewItem resultItem = Parent.VisualLastChild;
                    if(this == resultItem)
                    {
                        if(thisCompoundItem2.EndLine is not null && !thisCompoundItem2.EndLine.IsDeleted)
                        {
                            currentLine = thisCompoundItem2.EndLine;
                        }
                        currentLine ??= StartLine;
                        string currentLineString = Plan.GetRangeText(currentLine.Offset, currentLine.Length);
                        if (currentLineString.EndsWith(','))
                        {
                            Plan.SetRangeText(currentLine.EndOffset - 1, 1, "");
                        }
                    }
                }
            }
            else
            if(direction is MoveDirection.Up)
            {
                if (this is BaseCompoundJsonTreeViewItem thisCompoundItem3)
                {
                    JsonTreeViewItem resultItem = Parent.VisualLastChild;
                    if(LogicNext == resultItem)
                    {
                        if (LogicNext is BaseCompoundJsonTreeViewItem nextCompoundItem && nextCompoundItem.EndLine is not null)
                        {
                            Plan.SetRangeText(nextCompoundItem.EndLine.EndOffset - 1, 1, "");
                        }
                        else
                        {
                            Plan.SetRangeText(LogicNext.StartLine.EndOffset - 1, 1, "");
                        }
                    }
                    if(thisCompoundItem3.EndLine is not null && !thisCompoundItem3.EndLine.IsDeleted)
                    {
                        currentLine = thisCompoundItem3.EndLine;
                    }
                }
                currentLine ??= StartLine;
                string currentLineString = Plan.GetRangeText(currentLine.Offset, currentLine.Length);
                if(!currentLineString.EndsWith(','))
                {
                    Plan.SetRangeText(currentLine.EndOffset, 0, ",");
                }
            }
            #endregion
        }
        #endregion

        #region Event
        /// <summary>
        /// 更新所有本层节点的视觉引用
        /// </summary>
        private void UpdateCurrentLayerVisualReference()
        {
            if (Parent is not null)
            {
                JsonItemTool.SetParentForEachItem(Parent.LogicChildren,Parent);
                Parent.SetVisualPreviousAndNextForEachItem();
            }
            else
            if(Plan is BaseCustomWorldUnifiedPlan basePlan)
            {
                JsonItemTool.SetParentForEachItem(basePlan.TreeViewItemList, null);
                Plan.SetVisualPreviousAndNextForEachItem();
            }
        }

        /// <summary>
        /// 向上移动数组元素节点
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void MoveItemUp_Click(object sender, RoutedEventArgs e) => MoveItem(MoveDirection.Up);

        /// <summary>
        /// 向下移动数组元素节点
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void MoveItemDown_Click(object sender, RoutedEventArgs e) => MoveItem(MoveDirection.Down);

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
            DataType originDataType = DataType.None;
            string doubleQuotationMarks = "";
            Value ??= "";
            string currentValue = Value.ToString();
            int offset = 0, length = 0;
            if(StartLine is not null && StartLine.IsDeleted)
            {
                StartLine = null;
            }
            if (StartLine is not null)
            {
                offset = StartLine.Offset;
                length = StartLine.Length;
            }
            #endregion

            #region 判断是否需要直接返回
            if (((string.IsNullOrEmpty(Value.ToString()) && StartLine is null) || DataType is DataType.None) && (this is BaseCompoundJsonTreeViewItem compoundJsonTreeViewItem && (compoundJsonTreeViewItem.ItemType is ItemType.CustomCompound || compoundJsonTreeViewItem.DataType is DataType.None || (compoundJsonTreeViewItem.ItemType is  ItemType.MultiType && compoundJsonTreeViewItem.SelectedValueType.Text == "- unset -"))))
            {
                return;
            }
            #endregion

            #region 定位相邻的已有值的两个节点
            BaseCompoundJsonTreeViewItem previousCompoundItem = VisualPrevious as BaseCompoundJsonTreeViewItem;
            BaseCompoundJsonTreeViewItem nextCompoundItem = VisualNext as BaseCompoundJsonTreeViewItem;
            #endregion

            #region 处理复合节点与简单节点

            #region 判断是否需要添加双引号
            if (this is BaseCompoundJsonTreeViewItem thisCompoundJsonTreeViewItem1)
            {
                originDataType = (DataType)Enum.Parse(typeof(DataType), thisCompoundJsonTreeViewItem1.SelectedValueType.Text, true);
                changeType = originDataType is DataType.String ? ChangeType.String : ChangeType.NumberAndBool;
                if (changeType is ChangeType.String)
                {
                    doubleQuotationMarks = "\"";
                }
            }
            else
            {
                changeType = DataType is DataType.String ? ChangeType.String : ChangeType.NumberAndBool;
                if (changeType is ChangeType.String)
                {
                    doubleQuotationMarks = "\"";
                }
            }
            #endregion

            #region 处理有值和空值替换并收尾
            if (string.IsNullOrEmpty(currentValue))
            {
                DocumentLine topLine = null;

                if (Key.Length > 0 && IsCanBeDefaulted)
                {
                    if (previousCompoundItem is not null && previousCompoundItem.EndLine is not null)
                    {
                        topLine = previousCompoundItem.EndLine;
                    }
                    else
                    if (VisualPrevious is not null)
                    {
                        topLine = VisualPrevious.StartLine;
                    }
                    if (topLine is null && Parent is not null)
                    {
                        topLine = Parent.StartLine;
                    }
                    else
                    {
                        topLine ??= Plan.GetLineByNumber(2);
                    }
                }

                if (topLine is null)
                {
                    string currentLineText = Plan.GetRangeText(StartLine.Offset, StartLine.Length);
                    if (currentLineText.Contains(':'))
                    {
                        offset = StartLine.Offset + currentLineText.IndexOf(':') + 2;
                    }
                    else
                    {
                        offset = StartLine.Offset + currentLineText.Length - currentLineText.TrimStart().Length;
                    }
                    length = StartLine.EndOffset - (IsCanBeDefaulted || currentLineText.TrimEnd().EndsWith(',') ? 1 : 0) - offset;
                }
                else
                {
                    offset = topLine.EndOffset;
                    if (VisualPrevious is null && VisualNext is not null)
                    {
                        length = StartLine.EndOffset - offset;
                    }
                    else
                    {
                        string parentEndLineText = Plan.GetRangeText(Parent.EndLine.Offset, Parent.EndLine.Length);
                        char locateChar = Parent.ItemType is ItemType.Array || Parent.ItemType is ItemType.List ? ']' : '}';
                        length = Parent.EndLine.Offset + parentEndLineText.IndexOf(locateChar) - offset;
                    }
                }
                string newValue = "";
                if ((changeType is ChangeType.String && !IsCanBeDefaulted) || originDataType is DataType.String || (Parent is not null && Parent.DisplayText == "Entry"))
                {
                    newValue = "\"\"";
                }
                else
                if (originDataType is DataType.Number)
                {
                    newValue = "0";
                }
                Plan.SetRangeText(offset, length, newValue);
                if (Parent is not null && Parent.VisualLastChild is null)
                {
                    Parent.EndLine = Parent.StartLine;
                }
                if (IsCanBeDefaulted)
                {
                    StartLine = null;
                }
                if (this is BaseCompoundJsonTreeViewItem thisCompoundJsonTreeViewItem2)
                {
                    thisCompoundJsonTreeViewItem2.EndLine = null;
                }
            }
            else
            {
                #region 整合当前节点的值并执行更新
                if (StartLine is not null)
                {
                    currentValue = doubleQuotationMarks + currentValue + doubleQuotationMarks;
                }
                else
                {
                    currentValue = (VisualPrevious is not null && VisualNext is null ? ',' : "") + "\r\n" + new string(' ', LayerCount * 2) + (Key.Length > 0 ? "\"" + Key + "\": " : "") + doubleQuotationMarks + currentValue + doubleQuotationMarks + (Parent.VisualLastChild is null ? "\r\n" + new string(' ', Parent.LayerCount * 2) : "") + (VisualNext is not null ? ',' : "");
                }

                if ((!IsCanBeDefaulted && Parent is not null) || StartLine is not null)
                {
                    Plan.UpdateValueBySpecifyingInterval(this, changeType, currentValue);
                }
                else
                {
                    int lineNumber = 0;
                    if (previousCompoundItem is not null && previousCompoundItem.EndLine is not null)
                    {
                        offset = previousCompoundItem.EndLine.EndOffset;
                        lineNumber = previousCompoundItem.EndLine.LineNumber + 1;
                    }
                    else
                    if (VisualPrevious is not null)
                    {
                        offset = VisualPrevious.StartLine.EndOffset;
                        lineNumber = VisualPrevious.StartLine.LineNumber + 1;
                    }
                    else
                    if (Parent is not null)
                    {
                        lineNumber = Parent.StartLine.NextLine.LineNumber;
                        string parentStartLineString = Plan.GetRangeText(Parent.StartLine.Offset, Parent.StartLine.Length);
                        if (Parent.Key.Length > 0)
                        {
                            offset = Parent.StartLine.Offset + parentStartLineString.IndexOf(':') + 3;
                        }
                        else
                        {
                            if (offset == -1)
                            {
                                offset = Parent.StartLine.Offset + parentStartLineString.IndexOf('{');
                            }
                            if (offset == -1)
                            {
                                offset = Parent.StartLine.Offset + parentStartLineString.IndexOf('[');
                            }
                        }
                    }
                    else
                    {
                        offset = 1;
                        lineNumber = 2;
                    }
                    Plan.SetRangeText(offset, length, currentValue);

                    StartLine = Plan.GetLineByNumber(lineNumber);
                }
                #endregion

                #region 更新父节点的末行引用
                if (Parent is not null)
                {
                    if (Parent.VisualLastChild is null)
                    {
                        Parent.EndLine = StartLine.NextLine;
                        Parent.VisualLastChild = this;
                    }
                    else
                    {
                        Tuple<JsonTreeViewItem, JsonTreeViewItem> previousAndNext = Parent.SearchVisualPreviousAndNextItem(this);
                        if (previousAndNext is not null && previousAndNext.Item2 is not null)
                        {
                            Parent.VisualLastChild = previousAndNext.Item2;
                        }
                    }
                }
                #endregion
            }

            UpdateCurrentLayerVisualReference();
            OldValue = (sender as TextBox).Text;
            #endregion

            #endregion
        }

        /// <summary>
        /// 处理布尔值的更新
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void BoolButton_Click(object sender, RoutedEventArgs e)
        {
            #region Field
            string currentValue = ((bool)Value).ToString().ToLower();
            if (StartLine is not null && StartLine.IsDeleted)
            {
                StartLine = null;
            }
            #endregion

            #region 处理布尔值变更
            if (!IsFalse && !IsTrue)
            {
                int offset = StartLine.Offset, length = StartLine.Length;

                #region 处理偏移量和替换长度
                if (VisualPrevious is not null)
                {
                    if (VisualPrevious is BaseCompoundJsonTreeViewItem PreviousCompoundItem && PreviousCompoundItem.EndLine is not null)
                    {
                        offset = PreviousCompoundItem.EndLine.EndOffset;
                    }
                    else
                    {
                        offset = VisualPrevious.StartLine.EndOffset;
                    }

                    if (VisualNext is null)
                    {
                        offset--;
                    }
                    length = StartLine.EndOffset - offset;
                }
                else
                if (Parent is not null)
                {
                    offset = Parent.StartLine.EndOffset;
                    if (VisualNext is not null)
                    {
                        length = StartLine.EndOffset - offset;
                    }
                    else
                    {
                        string parentEndlineText = Plan.GetRangeText(Parent.EndLine.Offset, Parent.EndLine.Length);
                        int lastOffset = parentEndlineText.LastIndexOf('}');
                        if (lastOffset == 0)
                        {
                            lastOffset = parentEndlineText.LastIndexOf(']');
                        }
                        length = Parent.EndLine.Offset + lastOffset - offset;
                    }
                }
                #endregion

                #region 更新编辑器、设置父节点行引用
                Plan.SetRangeText(offset, length, "");

                if(Parent is not null && (Parent.VisualLastChild is null || Parent.VisualLastChild == this))
                {
                    Parent.EndLine = Parent.StartLine;
                    Parent.VisualLastChild = null;
                }

                StartLine = null;
                if(this is BaseCompoundJsonTreeViewItem compoundBoolItem)
                {
                    compoundBoolItem.EndLine = null;
                }
                #endregion
            }
            else
            {
                #region 整合当前节点的值并执行更新
                if (StartLine is null)
                {
                    currentValue = (VisualPrevious is not null && VisualNext is null ? ',' : "") + "\r\n" + new string(' ', LayerCount * 2) + (Key.Length > 0 ? "\"" + Key + "\": " : "") + currentValue + (Parent is not null && Parent.VisualLastChild is null ? "\r\n" + new string(' ', Parent.LayerCount * 2) : "") + (VisualNext is not null ? ',' : "");
                }
                Plan.UpdateValueBySpecifyingInterval(this, ChangeType.NumberAndBool, currentValue);
                #endregion

                #region 更新父节点的末行引用
                if (Parent is not null)
                {
                    if (Parent.VisualLastChild is null)
                    {
                        Parent.EndLine = StartLine.NextLine;
                        Parent.VisualLastChild = this;
                    }
                    else
                    {
                        Tuple<JsonTreeViewItem, JsonTreeViewItem> previousAndNext = Parent.SearchVisualPreviousAndNextItem(this);
                        if (previousAndNext is not null && previousAndNext.Item2 is not null)
                        {
                            Parent.VisualLastChild = previousAndNext.Item2;
                        }
                    }
                }
                #endregion
            }

            UpdateCurrentLayerVisualReference();
            #endregion
        }

        /// <summary>
        /// 移除当前元素
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void RemoveCurrentItem_Click(object sender, RoutedEventArgs e)
        {
            if(Parent is not null)
            {
                Parent.RemoveChild([this]);
            }
            else
            if(Plan is BaseCustomWorldUnifiedPlan basePlan)
            {
                basePlan.RemoveChild([this]);
            }
        }
        #endregion
    }
}