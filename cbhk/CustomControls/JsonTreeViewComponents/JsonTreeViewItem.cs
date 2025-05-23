using CBHK.CustomControls.Interfaces;
using CBHK.Service.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using ICSharpCode.AvalonEdit.Document;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using static CBHK.Model.Common.Enums;

namespace CBHK.CustomControls.JsonTreeViewComponents
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

        #region Method
        /// <summary>
        /// 移动数组元素节点
        /// </summary>
        /// <param name="direction">移动方向</param>
        private void MoveItem(MoveDirection direction)
        {
            #region 从父级删除当前节点
            if ((direction is MoveDirection.Down && Next is not null) || (direction is MoveDirection.Up && Previous is not null))
            {
                Parent.Children.Remove(this);
            }
            else
            {
                return;
            }
            #endregion

            #region Field
            int offset = 0, length = 0;
            string targetLineText = "";
            DocumentLine targetLine = null;
            string currentString = "";
            JsonTreeViewItem previous = Previous;
            JsonTreeViewItem next = Next;
            #endregion

            #region 处理移动与行引用
            if (direction is MoveDirection.Down && Next is not null)
            {
                #region 确认当前行的文本值
                if (Previous is null)
                {
                    offset = Parent.StartLine.EndOffset;
                }
                else
                {
                    if (Previous is CompoundJsonTreeViewItem previousCompoundItem2 && previousCompoundItem2.EndLine is not null)
                    {
                        offset = previousCompoundItem2.EndLine.EndOffset;
                    }
                    else
                    if(Previous is not null)
                    {
                        offset = Previous.StartLine.EndOffset;
                    }
                }

                if (this is CompoundJsonTreeViewItem thisCompoundItem && thisCompoundItem.EndLine is not null)
                {
                    currentString = Plan.GetRangeText(offset, thisCompoundItem.EndLine.EndOffset - offset);
                    Plan.SetRangeText(offset, thisCompoundItem.EndLine.EndOffset - offset, "");
                    thisCompoundItem.EndLine = null;
                }
                else
                {
                    currentString = Plan.GetRangeText(offset, StartLine.EndOffset - offset);
                    Plan.SetRangeText(offset, StartLine.EndOffset - offset, "");
                }
                if (Next is CompoundJsonTreeViewItem nextCompoundItem && nextCompoundItem.EndLine is not null)
                {
                    offset = nextCompoundItem.EndLine.EndOffset + 1;
                }
                else
                {
                    offset = Next.StartLine.EndOffset + 1;
                }
                StartLine = null;
                #endregion

                #region 处理前后的逗号
                if ((Next.Next is null || (Next.Next is not null && Next.Next is CompoundJsonTreeViewItem nextNextCompoundItem && nextNextCompoundItem.DataType is not DataType.None)) && currentString.TrimEnd().EndsWith(','))
                {
                    currentString = currentString.TrimEnd([' ', ',']);
                }

                if (Next is CompoundJsonTreeViewItem nextCompoundItem1 && nextCompoundItem1.EndLine is not null)
                {
                    targetLineText = Plan.GetRangeText(nextCompoundItem1.EndLine.Offset, nextCompoundItem1.EndLine.Length);
                    targetLine = nextCompoundItem1.EndLine;
                }
                else
                {
                    targetLineText = Plan.GetRangeText(Next.StartLine.Offset, Next.StartLine.Length);
                    targetLine = Next.StartLine;
                }
                if (!targetLineText.TrimEnd().EndsWith(','))
                {
                    Plan.SetRangeText(targetLine.EndOffset, 0, ",");
                }
                #endregion

                #region 放置文本值、重新编排前后三个节点的关系、设置首尾行的引用
                Plan.SetRangeText(offset, length, currentString);

                if (previous is not null)
                {
                    previous.Next = next;
                }
                if (next is not null)
                {
                    next.Previous = previous;
                }
                JsonTreeViewItem nextNextItem = null;
                if (next is not null)
                {
                    nextNextItem = next.Next;
                }

                Previous = next;
                next.Next = this;
                if (nextNextItem is not null)
                {
                    nextNextItem.Previous = this;
                }
                Next = nextNextItem;

                if (Previous is CompoundJsonTreeViewItem previousCompoundItem && previousCompoundItem.EndLine is not null)
                {
                    StartLine = previousCompoundItem.EndLine.NextLine;
                }
                else
                {
                    StartLine = Previous.StartLine.NextLine;
                }
                if (this is CompoundJsonTreeViewItem thisItem)
                {
                    thisItem.EndLine = Next.StartLine.PreviousLine;
                }
                int nextIndex = Parent.Children.Count - 2;
                if (Previous is not null)
                {
                    nextIndex = Parent.Children.IndexOf(Previous);
                }

                DocumentLine currentTargetLine = null;
                if(this is CompoundJsonTreeViewItem thisCompoundItem1 && thisCompoundItem1.EndLine is not null)
                {
                    currentTargetLine = thisCompoundItem1.EndLine;
                }
                else
                {
                    currentTargetLine = StartLine;
                }

                #region 删除空行
                if (Next is not null)
                {
                    int emptyLineCount = Next.StartLine.LineNumber - currentTargetLine.LineNumber - 1;
                    if (emptyLineCount > 0)
                    {
                        DocumentLine endLine = this is CompoundJsonTreeViewItem thisCompoundItem2 && thisCompoundItem2.EndLine is not null ? thisCompoundItem2.EndLine : StartLine;
                        Plan.SetRangeText(endLine.EndOffset, endLine.NextLine.Offset - endLine.EndOffset, "");
                    }
                }
                #endregion

                //插入当前节点
                Parent.Children.Insert(nextIndex + 1, this);
                #endregion
            }
            else
            if (direction is MoveDirection.Up && Previous is not null)
            {
                #region 确认当前行的文本值
                if (Previous is CompoundJsonTreeViewItem previousCompoundItem2 && previousCompoundItem2.EndLine is not null)
                {
                    offset = previousCompoundItem2.EndLine.EndOffset;
                }
                else
                {
                    offset = Previous.StartLine.EndOffset;
                }

                if (this is CompoundJsonTreeViewItem thisCompoundItem1 && thisCompoundItem1.EndLine is not null)
                {
                    currentString = Plan.GetRangeText(offset, thisCompoundItem1.EndLine.EndOffset - offset);
                    Plan.SetRangeText(offset, thisCompoundItem1.EndLine.EndOffset - offset, "");
                    thisCompoundItem1.EndLine = null;
                }
                else
                {
                    currentString = Plan.GetRangeText(offset, StartLine.EndOffset - offset);
                    Plan.SetRangeText(offset, StartLine.EndOffset - offset, "");
                }
                if (Previous.Previous is CompoundJsonTreeViewItem previousPreviousCompoundItem && previousPreviousCompoundItem.EndLine is not null)
                {
                    offset = previousPreviousCompoundItem.EndLine.EndOffset;
                }
                else
                if(Previous.Previous is not null)
                {
                    offset = Previous.Previous.StartLine.EndOffset;
                }
                else
                {
                    offset = Parent.StartLine.EndOffset;
                }
                StartLine = null;
                #endregion

                #region 处理前后的逗号
                if ((Next is null || (Next is not null && Next.StartLine is null)) && !currentString.TrimEnd().EndsWith(','))
                {
                    currentString += ',';
                }

                if (Previous is CompoundJsonTreeViewItem previousCompoundItem && previousCompoundItem.EndLine is not null)
                {
                    targetLineText = Plan.GetRangeText(previousCompoundItem.EndLine.Offset, previousCompoundItem.EndLine.Length);
                    targetLine = previousCompoundItem.EndLine;
                }
                else
                {
                    targetLineText = Plan.GetRangeText(Previous.StartLine.Offset, Previous.StartLine.Length);
                    targetLine = Previous.StartLine;
                }
                if (!targetLineText.TrimEnd().EndsWith(',') && Next is not null && Next.StartLine is not null)
                {
                    Plan.SetRangeText(targetLine.EndOffset, 0, ",");
                }
                else
                if (targetLineText.TrimEnd().EndsWith(',') && (Next is null || (Next is not null && Next.StartLine is null)))
                {
                    Plan.SetRangeText(targetLine.Offset, targetLine.Length, targetLineText.TrimEnd([' ', ',']));
                }
                #endregion

                #region 放置文本值、重新编排前后三个节点的关系、设置首尾行的引用
                Plan.SetRangeText(offset, length, currentString);

                JsonTreeViewItem previousPreviousItem = null;
                if (previous is not null)
                {
                    previous.Next = next;
                    previousPreviousItem = previous.Previous;
                    previous.Previous = this;
                }
                if (next is not null)
                {
                    next.Previous = previous;
                }
                Next = previous;
                Previous = previousPreviousItem;
                if(previousPreviousItem is not null)
                {
                    previousPreviousItem.Next = this;
                }

                if (Previous is CompoundJsonTreeViewItem previousCompoundItem1 && previousCompoundItem1.EndLine is not null)
                {
                    StartLine = previousCompoundItem1.EndLine.NextLine;
                }
                else
                if(Previous is not null)
                {
                    StartLine = Previous.StartLine.NextLine;
                }
                else
                {
                    StartLine = Parent.StartLine.NextLine;
                }

                if (this is CompoundJsonTreeViewItem thisItem)
                {
                    thisItem.EndLine = Next.StartLine.PreviousLine;
                }
                int nextIndex = 0;
                if (Next is not null)
                {
                   nextIndex = Parent.Children.IndexOf(Next);
                }
                Parent.Children.Insert(nextIndex, this);
                #endregion
            }
            #endregion
        }
        #endregion

        #region Event

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
            JsonTreeViewItem previous = Previous;
            JsonTreeViewItem next = Next;
            changeType = DataType is DataType.String || (this is CompoundJsonTreeViewItem compoundItem && compoundItem.DataType is DataType.String) ? ChangeType.String : ChangeType.NumberAndBool;
            string doubleQuotationMarks = changeType is ChangeType.String && Value is string stringValue && !stringValue.StartsWith('"') && !stringValue.EndsWith('"') ? "\"" : "";
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
            if ((string.IsNullOrEmpty(Value.ToString()) && StartLine is null) || DataType is DataType.None && this is CompoundJsonTreeViewItem compoundJsonTreeViewItem && (compoundJsonTreeViewItem.DataType is DataType.CustomCompound || compoundJsonTreeViewItem.DataType is DataType.None || (compoundJsonTreeViewItem.DataType is DataType.MultiType && compoundJsonTreeViewItem.SelectedValueType.Text == "- unset -")))
            {
                return;
            }
            #endregion

            #region 定位相邻的已有值的两个节点
            Tuple<JsonTreeViewItem, JsonTreeViewItem> previousAndNextItem = JsonItemTool.LocateTheNodesOfTwoAdjacentExistingValues(Previous, Next);
            previous = previousAndNextItem.Item1;
            next = previousAndNextItem.Item2;
            CompoundJsonTreeViewItem previousCompoundItem = previous as CompoundJsonTreeViewItem;
            CompoundJsonTreeViewItem nextCompoundItem = next as CompoundJsonTreeViewItem;
            #endregion

            #region 处理复合节点与简单节点
            if (this is CompoundJsonTreeViewItem thisCompoundJsonTreeViewItem1 && thisCompoundJsonTreeViewItem1.DataType is DataType.MultiType)
            {
                originDataType = (DataType)Enum.Parse(typeof(DataType), thisCompoundJsonTreeViewItem1.SelectedValueType.Text, true);
                changeType = originDataType is DataType.String ? ChangeType.String : ChangeType.NumberAndBool;
                if(changeType is ChangeType.String)
                {
                    doubleQuotationMarks = "\"";
                }
            }

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
                    if (previous is not null)
                    {
                        topLine = previous.StartLine;
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
                    if ((next is not null && next.StartLine is not null) || (previous is not null && previous.StartLine is not null))
                    {
                        if (next is null || (next is not null && next.StartLine is null))
                        {
                            offset--;
                        }
                        length = StartLine.EndOffset - offset;
                    }
                    else
                    {
                        string parentEndLineText = Plan.GetRangeText(Parent.EndLine.Offset, Parent.EndLine.Length);
                        char locateChar = Parent.DataType is DataType.Array || Parent.DataType is DataType.List ? ']' : '}';
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
                bool unNormalState = Parent.EndLine is null || Parent.StartLine == Parent.EndLine || (Parent.EndLine is not null && Parent.EndLine.IsDeleted);
                if (unNormalState && ((previous is null && next is null) || (previous is not null && previous.StartLine is null) || (next is not null && next.StartLine is null)))
                {
                    Parent.EndLine = Parent.StartLine;
                }
                if (IsCanBeDefaulted)
                {
                    StartLine = null;
                }
                if(this is CompoundJsonTreeViewItem thisCompoundJsonTreeViewItem2)
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
                    currentValue = (previous is not null && previous.StartLine is not null && ((next is not null && next.StartLine is null) || next is null) ? ',' : "") + "\r\n" + new string(' ', LayerCount * 2) + (Key.Length > 0 ? "\"" + Key + "\": " : "") + doubleQuotationMarks + currentValue + doubleQuotationMarks + (Parent.EndLine == Parent.StartLine || Parent.EndLine is null ? "\r\n" + new string(' ', Parent.LayerCount * 2) : "") + (next is not null && next.StartLine is not null ? ',' : "");
                }
                Plan.UpdateValueBySpecifyingInterval(this, changeType, currentValue);
                #endregion
                #region 更新父节点的末行引用
                if (Parent is not null && (Parent.EndLine is null || Parent.StartLine == Parent.EndLine || (Parent.EndLine is not null && Parent.EndLine.IsDeleted)) && ((Parent.DataType is DataType.List || (Parent.DataType is DataType.MultiType && Parent.SelectedValueType is not null && Parent.SelectedValueType.Text == "List")) && Parent.Children.Count == 2 || (((Parent.DataType is DataType.Compound || (Parent.DataType is DataType.MultiType && Parent.SelectedValueType is not null && Parent.SelectedValueType.Text == "Compound")) || Parent.DataType is DataType.OptionalCompound) && (previous is null || next is null || (previous is not null && previous.StartLine is null) || (next is not null && next.StartLine is null)))))
                {
                    Parent.EndLine = StartLine.NextLine;
                }
                #endregion
            }
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

            #region 定位相邻的已有值的两个节点
            Tuple<JsonTreeViewItem, JsonTreeViewItem> previousAndNextItem = JsonItemTool.LocateTheNodesOfTwoAdjacentExistingValues(Previous, Next);
            JsonTreeViewItem previous = previousAndNextItem.Item1;
            JsonTreeViewItem next = previousAndNextItem.Item2;
            #endregion

            #region 处理布尔值变更
            if (!IsFalse && !IsTrue)
            {
                int offset = StartLine.Offset, length = StartLine.Length;

                #region 处理偏移量和替换长度
                if (previous is not null)
                {
                    if (previous is CompoundJsonTreeViewItem PreviousCompoundItem && PreviousCompoundItem.EndLine is not null)
                    {
                        offset = PreviousCompoundItem.EndLine.EndOffset;
                    }
                    else
                    if(previous.StartLine is not null)
                    {
                        offset = previous.StartLine.EndOffset;
                    }

                    if (next is null || (next is not null && next.StartLine is null))
                    {
                        offset--;
                    }
                    length = StartLine.EndOffset - offset;
                }
                else
                if (Parent is not null)
                {
                    offset = Parent.StartLine.EndOffset;
                    if (next is not null && next.StartLine is not null)
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

                if((previous is null || (previous is not null && previous.StartLine is null)) && (next is null || (next is not null && next.StartLine is null)))
                {
                    Parent.EndLine = Parent.StartLine;
                }

                StartLine = null;
                if(this is CompoundJsonTreeViewItem compoundBoolItem)
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
                    currentValue = (previous is not null && previous.StartLine is not null && ((next is not null && next.StartLine is null) || next is null) ? ',' : "") + "\r\n" + new string(' ', LayerCount * 2) + (Key.Length > 0 ? "\"" + Key + "\": " : "") + currentValue + (Parent is not null && (Parent.EndLine == Parent.StartLine || Parent.EndLine is null) ? "\r\n" + new string(' ', Parent.LayerCount * 2) : "") + (next is not null && next.StartLine is not null ? ',' : "");
                }
                Plan.UpdateValueBySpecifyingInterval(this, ChangeType.NumberAndBool, currentValue);
                #endregion
                #region 更新父节点的末行引用
                if (Parent is not null && (Parent.StartLine == Parent.EndLine || Parent.EndLine is null || (Parent.EndLine is not null && Parent.EndLine.IsDeleted)))
                {
                    if (this is CompoundJsonTreeViewItem compoundJsonTreeViewItem && compoundJsonTreeViewItem.EndLine is not null)
                    {
                        Parent.EndLine = compoundJsonTreeViewItem.EndLine.NextLine;
                    }
                    else
                    {
                        Parent.EndLine = StartLine.NextLine;
                    }
                }
                #endregion
            }
            #endregion
        }

        /// <summary>
        /// 移除当前元素
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void RemoveCurrentItem_Click(object sender, RoutedEventArgs e) => JsonItemTool.RemoveCurrentItem(this);
        #endregion
    }
}