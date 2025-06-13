using CBHK.CustomControl.Interfaces;
using CBHK.Service.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using DryIoc.FastExpressionCompiler.LightExpression;
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
            #region 从父级删除当前节点
            int insertIndex = Parent.Children.IndexOf(this);
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
            DocumentLine targetLine = null;
            string currentString = "";
            JsonTreeViewItem previous = Previous;
            JsonTreeViewItem next = Next;
            #endregion
            #region 处理前后节点关系
            if (direction is MoveDirection.Down)
            {
                JsonTreeViewItem nextNext = Next.Next;
                if (Next is CompoundJsonTreeViewItem nextComoundItem && nextComoundItem.EndLine is not null && !nextComoundItem.EndLine.IsDeleted)
                {
                    targetLine = nextComoundItem.EndLine;
                }
                else
                {
                    targetLine = Next.StartLine;
                }
                if (previous is not null)
                {
                    previous.Next = next;
                }
                next.Previous = previous;
                next.Next = this;
                Next = nextNext;
                Previous = next;
                if(nextNext is not null)
                {
                    nextNext.Previous = this;
                }
                insertIndex++;
            }
            else
            if (direction is MoveDirection.Up)
            {
                JsonTreeViewItem previousPrevious = Previous.Previous;
                if (Previous.Previous is not null)
                {
                    targetLine = Previous.Previous.StartLine.PreviousLine;
                }
                else
                {
                    targetLine = Parent.StartLine;
                }
                previous.Previous = this;
                previous.Next = next;
                if (next is not null)
                {
                    next.Previous = previous;
                }
                if (previousPrevious is not null)
                {
                    previousPrevious.Next = this;
                }
                Previous = previousPrevious;
                Next = previous;
                insertIndex--;
            }
            #endregion
            #region 保存并切除当前节点的Json文本
            offset = StartLine.PreviousLine.EndOffset;
            length = this is CompoundJsonTreeViewItem compoundJsonTreeViewItem && compoundJsonTreeViewItem.EndLine is not null && !compoundJsonTreeViewItem.EndLine.IsDeleted ? compoundJsonTreeViewItem.EndLine.EndOffset - offset : StartLine.EndOffset - offset;
            currentString = Plan.GetRangeText(offset, length);
            Plan.SetRangeText(offset,length,"");
            #endregion
            #region 放置处理引用后当前节点的Json文本
            Plan.SetRangeText(targetLine.EndOffset,0,currentString);
            #endregion
            #region 处理放置后当前节点的行应用
            StartLine = targetLine.NextLine;
            if (this is CompoundJsonTreeViewItem thisCompoundItem1)
            {
                JsonItemTool.SetLineNumbersForEachSubItem(thisCompoundItem1.Children, thisCompoundItem1, thisCompoundItem1.EnumKey.Length > 0 || thisCompoundItem1.IsEnumBranch);
                JsonTreeViewItem resultItem = JsonItemTool.SearchForTheLastItemWithRowReference(thisCompoundItem1);
                if(resultItem is CompoundJsonTreeViewItem resultCompoundItem && resultCompoundItem.EndLine is not null)
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
            Parent.Children.Insert(insertIndex, this);
            DocumentLine currentLine = null;
            if (direction is MoveDirection.Down)
            {
                DocumentLine previousEndLine = null;
                if(Previous is CompoundJsonTreeViewItem previousCompoundItem && previousCompoundItem.EndLine is not null && !previousCompoundItem.EndLine.IsDeleted)
                {
                    previousEndLine = previousCompoundItem.EndLine;
                }
                else
                if(Previous is not null)
                {
                    previousEndLine = Previous.StartLine;
                }
                string previousEndlineString = Plan.GetRangeText(previousEndLine.Offset, previousEndLine.Length);
                if(!previousEndlineString.EndsWith(','))
                {
                    Plan.SetRangeText(previousEndLine.EndOffset, 0, ",");
                }
                if (this is CompoundJsonTreeViewItem thisCompoundItem2)
                {
                    JsonTreeViewItem resultItem = JsonItemTool.SearchForTheLastItemWithRowReference(Parent);
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
                if (this is CompoundJsonTreeViewItem thisCompoundItem3)
                {
                    JsonTreeViewItem resultItem = JsonItemTool.SearchForTheLastItemWithRowReference(Parent);
                    if(Next == resultItem)
                    {
                        if (Next is CompoundJsonTreeViewItem nextCompoundItem && nextCompoundItem.EndLine is not null)
                        {
                            Plan.SetRangeText(nextCompoundItem.EndLine.EndOffset - 1, 1, "");
                        }
                        else
                        {
                            Plan.SetRangeText(Next.StartLine.EndOffset - 1, 1, "");
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
            CompoundJsonTreeViewItem previousCompoundItem1 = previous as CompoundJsonTreeViewItem;
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
                    if (previousCompoundItem1 is not null && previousCompoundItem1.EndLine is not null)
                    {
                        topLine = previousCompoundItem1.EndLine;
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
                    currentValue = (previous is not null && previous.StartLine is not null && ((next is not null && next.StartLine is null) || next is null) ? ',' : "") + "\r\n" + new string(' ', LayerCount * 2) + (Key.Length > 0 ? "\"" + Key + "\": " : "") + doubleQuotationMarks + currentValue + doubleQuotationMarks + (Parent is not null && (Parent.EndLine == Parent.StartLine || Parent.EndLine is null) ? "\r\n" + new string(' ', Parent.LayerCount * 2) : "") + (next is not null && next.StartLine is not null ? ',' : "");
                }
                if (Parent is not null || StartLine is not null)
                {
                    Plan.UpdateValueBySpecifyingInterval(this, changeType, currentValue);
                }
                else
                {
                    int lineNumber = 0;
                    if(previous is CompoundJsonTreeViewItem previousCompoundItem2 && previousCompoundItem2.EndLine is not null)
                    {
                        offset = previousCompoundItem2.EndLine.EndOffset;
                        lineNumber = previousCompoundItem2.EndLine.LineNumber + 1;
                    }
                    else
                    if(previous is not null && previous.StartLine is not null)
                    {
                        offset = previous.StartLine.EndOffset;
                        lineNumber = previous.StartLine.LineNumber + 1;
                    }
                    else
                    if(Parent is not null)
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
                    }
                    Plan.SetRangeText(offset, length, currentValue);

                    StartLine = Plan.GetLineByNumber(lineNumber);
                }
                #endregion
                #region 更新父节点的末行引用
                if (Parent is not null && (Parent.EndLine is null || Parent.StartLine == Parent.EndLine || (Parent.EndLine is not null && Parent.EndLine.IsDeleted)) && ((Parent.DataType is DataType.List || (Parent.DataType is DataType.MultiType && Parent.SelectedValueType is not null && Parent.SelectedValueType.Text == "List") && (Parent.Children.Count == 2 || IsCanBeDefaulted)) || ((Parent.DataType is DataType.Compound || (Parent.DataType is DataType.MultiType && Parent.SelectedValueType is not null && Parent.SelectedValueType.Text == "Compound") || Parent.DataType is DataType.OptionalCompound) && (previous is null || next is null || (previous is not null && previous.StartLine is null) || (next is not null && next.StartLine is null)))))
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