using cbhk.CustomControls.Interfaces;
using cbhk.GeneralTools;
using cbhk.Interface.Json;
using cbhk.Model.Common;
using CommunityToolkit.Mvvm.ComponentModel;
using ICSharpCode.AvalonEdit.Document;
using Prism.Ioc;
using SharpNBT;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using static cbhk.Model.Common.Enums;

namespace cbhk.CustomControls.JsonTreeViewComponents
{
    public partial class CompoundJsonTreeViewItem : JsonTreeViewItem
    {
        #region Property
        public bool IsCurrentExpanded { get; set; } = false;
        public Geometry PlusIcon { get; } = Application.Current.Resources["TreeNodePlusGeometry"] as Geometry;
        public Geometry MinusIcon { get; } = Application.Current.Resources["TreeNodeMinusGeometry"] as Geometry;
        [ObservableProperty]
        public Geometry _switchButtonIcon;
        [ObservableProperty]
        public Brush _switchButtonColor = new SolidColorBrush();
        public SolidColorBrush PlusColor { get; } = new((Color)ColorConverter.ConvertFromString("#2AA515"));
        public SolidColorBrush MinusColor { get; } = new((Color)ColorConverter.ConvertFromString("#D81E06"));
        public SolidColorBrush PressedPlusColor { get; } = new((Color)ColorConverter.ConvertFromString("#2AA515"));
        public SolidColorBrush PressedMinusColor { get; } = new((Color)ColorConverter.ConvertFromString("#2AA515"));
        [ObservableProperty]
        public Brush _pressedSwitchButtonColor = new SolidColorBrush();
        public int EndLineNumber { get; set; }
        public DocumentLine EndLine { get; set; }

        /// <summary>
        /// 枚举键
        /// </summary>
        public string EnumKey { get; set; } = "";

        private bool isCanSort = false;

        public bool IsCanSort
        {
            get => isCanSort;
            set
            {
                SetProperty(ref isCanSort, value);
                if (isCanSort)
                {
                    SortButtonVisibility = Visibility.Visible;
                }
                else
                {
                    SortButtonVisibility = Visibility.Collapsed;
                }
            }
        }

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

        private DataType dataType = DataType.Compound;
        public new DataType DataType
        {
            get => dataType;
            set
            {
                SetProperty(ref dataType, value);
                SortButtonVisibility = RemoveElementButtonVisibility = BoolButtonVisibility = EnumBoxVisibility = ErrorIconVisibility = InfoIconVisibility = InputBoxVisibility = EnumBoxVisibility = Visibility.Collapsed;
            }
        }

        [ObservableProperty]
        public Visibility _addOrSwitchElementButtonVisibility = Visibility.Collapsed;

        [ObservableProperty]
        public Visibility _switchBoxVisibility = Visibility.Collapsed;

        /// <summary>
        /// 可切换的子节点集合
        /// </summary>
        [ObservableProperty]
        public List<string> _childrenStringList = [];

        /// <summary>
        /// 可切换的类型集合
        /// </summary>
        [ObservableProperty]
        public List<string> _valueTypeStringList = [];

        /// <summary>
        /// 子节点集合
        /// </summary>
        [ObservableProperty]
        public ObservableCollection<JsonTreeViewItem> _children = [];

        /// <summary>
        /// 平铺后代节点集合
        /// </summary>
        [ObservableProperty]
        public ObservableCollection<JsonTreeViewItem> _flattenDescendantNodeList = [];

        public ObservableCollection<TextComboBoxItem> ValueTypeList { get; set; } = [];
        #endregion

        #region Field
        private IContainerProvider _container;
        [GeneratedRegex(@"(?<=\s*\s?\*+;?\s*\s?(如果|若)).+(?=为|是).+")]
        private static partial Regex GetEnumRawKey();
        [GeneratedRegex(@"\[\[(?<1>[a-zA-Z_\u4e00-\u9fff]+)\]\]")]
        private static partial Regex GetEnumKey();

        private TreeViewItem currentItemReference = null;
        #endregion

        #region Methods
        public CompoundJsonTreeViewItem(ICustomWorldUnifiedPlan plan, IJsonItemTool jsonItemTool, IContainerProvider containerProvider)
        {
            Plan = plan;
            JsonItemTool = jsonItemTool;
            _container = containerProvider;
        }
        #endregion

        #region Event
        /// <summary>
        /// 节点成员载入
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void TreeItem_Loaded(object sender, RoutedEventArgs e)
        {
            currentItemReference ??= (sender as FrameworkElement).FindParent<TreeViewItem>();
            if (!IsCanBeDefaulted || Children.Count > 0)
            {
                currentItemReference.IsExpanded = IsCurrentExpanded = true;
                PressedSwitchButtonColor = PressedMinusColor;
            }
            else
            {
                PressedSwitchButtonColor = PressedPlusColor;
            }
        }

        /// <summary>
        /// 清空当前的分支节点
        /// </summary>
        private void ClearCurrentSubItem(CompoundJsonTreeViewItem parent)
        {
            if (parent is not null && parent.Children.Count > 1)
            {
                int length = 0;
                string firstChildItemText = parent.Plan.GetRangeText(parent.Children[0].StartLine.Offset, parent.Children[0].StartLine.Length);
                int minusOffset = firstChildItemText.TrimEnd().EndsWith(',') ? 1 : 0;
                if (parent.Children[^1] is CompoundJsonTreeViewItem lastChildItem && lastChildItem.EndLine is not null)
                {
                    length = lastChildItem.EndLine.EndOffset - (parent.Children[0].StartLine.EndOffset - minusOffset);
                }
                else
                {
                    length = parent.Children[^1].StartLine.EndOffset - (parent.Children[0].StartLine.EndOffset - minusOffset);
                }
                Plan.SetRangeText(parent.Children[0].StartLine.EndOffset - minusOffset, length, "");
                while (parent.Children.Count > 1)
                {
                    parent.Children.RemoveAt(1);
                }
            }
        }

        /// <summary>
        /// 处理数据类型的变更
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void ValueType_SelectionChanged(object sender,SelectedCellsChangedEventArgs e)
        {
            string currentValueTypeString = CurrentValueType.Text.ToLower();
            bool isRealString = EnumItemsSource.Where(item => item.Text == "compound" || item.Text == "list").Count() == 0;
            if(currentValueTypeString.Length > 0)
            {
                DataType = DataType.None;
                DataType = DataType.MultiType;
                bool isCompoundType = currentValueTypeString == "compound" || currentValueTypeString == "list" || currentValueTypeString.Contains("array");
                switch (currentValueTypeString)
                {
                    case "bool":
                    case "boolean":
                        {
                            BoolButtonVisibility = Visibility.Visible;
                            break;
                        }
                    case "byte":
                    case "short":
                    case "int":
                    case "float":
                    case "double":
                    case "long":
                    case "string":
                        {
                            InputBoxVisibility = Visibility.Visible;
                            if (currentValueTypeString == "string" && !isRealString)
                            {
                                new HtmlHelper(_container).HandlingTheTypingAppearanceOfCompositeItemList([this]);
                            }
                            break;
                        }
                }
                if(isCompoundType)
                {

                }
            }
        }

        /// <summary>
        /// 处理枚举值的变更
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void EnumType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            #region Field
            bool skipCode = false;
            HtmlHelper htmlHelper = new(_container)
            {
                plan = Plan,
                jsonTool = JsonItemTool
            };
            string currentLineText = "";
            int colonOffset = 0; 
            int offset = 0;
            if (StartLine is not null)
            {
                char locateChar = ' ';
                if(Key.Length > 0)
                {
                    locateChar = ':';
                }
                else
                {
                    locateChar = '"';
                }
                currentLineText = Plan.GetRangeText(StartLine.Offset, StartLine.Length);
                colonOffset = currentLineText.IndexOf(locateChar) + (locateChar == ':' ? 2 : 0);
                offset = colonOffset + StartLine.Offset;
            }
            int length = 0;
            string quotationMarks = "\"";
            JsonTreeViewItem previous = Previous;
            JsonTreeViewItem next = Next;
            //定位相邻的两个已有值的节点
            Tuple<JsonTreeViewItem, JsonTreeViewItem> previousAndNextItem = JsonItemTool.LocateTheNodesOfTwoAdjacentExistingValues(Previous, Next);
            previous = previousAndNextItem.Item1;
            next = previousAndNextItem.Item2;
            #endregion

            #region 替换当前枚举节点的值
            if (StartLine is null)
            {
                string connectorString = previous is not null && previous.StartLine is not null && (next is null || (next is not null && next.StartLine is null)) ? "," : "";
                string endConnectorString = next is not null && next.StartLine is not null ? "," : "";
                if (previous is not null && previous.StartLine is not null)
                {
                    Plan.UpdateNullValueBySpecifyingInterval(previous.StartLine.EndOffset, connectorString + "\r\n" + new string(' ', LayerCount * 2) + (Key.Length > 0 ? "\"" + Key + "\": " : "") + "\"" + SelectedEnumItem.Text + "\"" + endConnectorString);
                }
                else
                if (Parent is not null)
                {
                    if((Parent.DataType is DataType.List && ((Parent.StartLine == Parent.EndLine) || Parent.EndLine is null)) || ((Parent.DataType is DataType.Compound || Parent.DataType is DataType.OptionalCompound) && ((Parent.StartLine == Parent.EndLine) || Parent.EndLine is null)))
                    {
                        endConnectorString = "\r\n" + new string(' ',Parent.LayerCount * 2);
                    }
                    DocumentLine parentStartLine = Plan.GetLineByNumber(Parent.StartLine.LineNumber);
                    string parentStartLineText = Plan.GetRangeText(parentStartLine.Offset, parentStartLine.EndOffset - parentStartLine.Offset);
                    int index = parentStartLineText.IndexOf('{') + 1;
                    if (index == 0)
                    {
                        index = parentStartLineText.IndexOf('[') + 1;
                    }
                    offset = Parent.StartLine.Offset + index;
                    Plan.SetRangeText(offset, 0, "\r\n" + new string(' ', LayerCount * 2) + (Key.Length > 0 ? "\"" + Key + "\": " : "") + "\"" + SelectedEnumItem.Text + "\"" + endConnectorString);
                }
                else
                {
                    Plan.SetRangeText(1, 0, "\r\n" + new string(' ', LayerCount * 2) + "\"" + Key + "\": \"" + SelectedEnumItem.Text + "\"" + endConnectorString);
                }

                if (previous is not null && previous.StartLine is not null)
                {
                    StartLine = previous.StartLine.NextLine;
                }
                else
                if (Parent is not null && Parent.StartLine is not null)
                {
                    StartLine = Parent.StartLine.NextLine;
                }
                else
                {
                    StartLine = Plan.GetLineByNumber(2);
                }
            }
            else
            {
                //选择了成员
                if (SelectedEnumItem.Text != "- unset -")
                {
                    if (oldSelectedEnumItem is not null && oldSelectedEnumItem.Text != "- unset -")
                    {
                        length = oldSelectedEnumItem.Text.Length + 2;
                    }
                    else
                    {
                        if (currentLineText.TrimEnd().EndsWith(','))
                        {
                            length = currentLineText.LastIndexOf(',') - colonOffset;
                        }
                        else
                        {
                            length = StartLine.Length - colonOffset;
                        }

                        bool isNotStringType = bool.TryParse(SelectedEnumItem.Text, out bool boolValue) || decimal.TryParse(SelectedEnumItem.Text, out decimal decimalValue);
                        if (isNotStringType)
                        {
                            quotationMarks = "";
                        }
                    }
                    string currentNewValue = quotationMarks + SelectedEnumItem.Text + quotationMarks;
                    if (offset > 0)
                    {
                        Plan.SetRangeText(offset, length, currentNewValue);
                    }
                }
                else//未选择成员但当前为必选节点
                if(!IsCanBeDefaulted)
                {
                    string currentLineString = Plan.GetRangeText(StartLine.Offset,StartLine.Length);
                    if(colonOffset > 1)
                    {
                        Plan.SetRangeText(offset,oldSelectedEnumItem.Text.Length + 2,"\"\"");
                    }
                }
                else//未选择成员且当前为可选节点
                if(previous is not null)
                {
                    string previousLineString = "";
                    string parentStartLineText = "";
                    string parentEndLineText = "";
                    if (previous is CompoundJsonTreeViewItem previousItem && previousItem.EndLine is not null)
                    {
                        offset = previousItem.EndLine.EndOffset;
                        previousLineString = Plan.GetRangeText(previousItem.EndLine.Offset, previousItem.EndLine.Length);
                    }
                    else
                    if(previous.StartLine is not null)
                    {
                        offset = previous.StartLine.EndOffset;
                        previousLineString = Plan.GetRangeText(previous.StartLine.Offset, previous.StartLine.Length);
                    }
                    else
                    {
                        parentStartLineText = Plan.GetRangeText(Parent.StartLine.Offset, Parent.StartLine.Length);
                        parentEndLineText = Plan.GetRangeText(Parent.EndLine.Offset, Parent.EndLine.Length);
                        int parentBracketIndex = parentStartLineText.IndexOf('{') + 1;
                        if(parentBracketIndex == 0)
                        {
                            parentBracketIndex = parentStartLineText.IndexOf('[') + 1;
                        }
                        offset = Parent.StartLine.Offset + parentBracketIndex;
                    }
                    offset -= previousLineString.TrimEnd().EndsWith(',') && (next is null || (next is not null && next.StartLine is null)) ? 1 : 0;
                    if (next is not null && next.StartLine is not null)
                    {
                        length = StartLine.EndOffset - offset;
                    }
                    else
                    {
                        int parentBracketIndex = parentEndLineText.IndexOf('}');
                        if (parentBracketIndex == -1)
                        {
                            parentBracketIndex = parentEndLineText.IndexOf(']');
                        }
                        length = Parent.EndLine.Offset + parentBracketIndex - offset;
                        Parent.EndLine = Parent.StartLine;
                    }
                    Plan.SetRangeText(offset, length, "");
                    StartLine = null;
                }
                else//只有当前节点有文本值
                if(Parent is not null)
                {
                    string parentStartLineText = Plan.GetRangeText(Parent.StartLine.Offset, Parent.StartLine.Length);
                    colonOffset = parentStartLineText.IndexOf(':');
                    string parentEndLineText = "";
                    
                    if(next is null || (next is not null && next.StartLine is not null))
                    parentEndLineText = Plan.GetRangeText(Parent.EndLine.Offset, Parent.EndLine.Length);
                    int endBracketOffset = parentEndLineText.IndexOf('}');
                    if(endBracketOffset == -1)
                    {
                        endBracketOffset = parentEndLineText.IndexOf(']');
                    }
                    offset = Parent.StartLine.Offset + colonOffset + 3;
                    if (next is null || (next is not null && next.StartLine is null))
                    {
                        length = Parent.EndLine.Offset + endBracketOffset - offset;
                        Parent.EndLine = Parent.StartLine;
                    }
                    else
                    {
                        length = StartLine.EndOffset - offset;
                    }
                    Plan.SetRangeText(offset, length, "");
                    StartLine = null;
                }
            }
            #endregion

            #region 判断是否需要应用多分支枚举结构
            if (EnumKey.Length > 0 && !skipCode)
            {
                if (Plan.EnumCompoundDataDictionary.TryGetValue(EnumKey, out Dictionary<string, List<string>> targetDependencyDictionary) && targetDependencyDictionary.TryGetValue(SelectedEnumItem.Text, out List<string> targetRawList))
                {
                    ClearCurrentSubItem(Parent);
                    JsonTreeViewDataStructure result = htmlHelper.GetTreeViewItemResult(new(), targetRawList, LayerCount);
                    Parent.Children.AddRange(result.Result);

                    if (result.Result.Count == 1)
                    {
                        result.Result[0].Previous = this;
                    }

                    Plan.SetRangeText(StartLine.EndOffset, 0, result.ResultString.Length > 0 ? ",\r\n" + result.ResultString.ToString() : "");
                    JsonItemTool.SetLineNumbersForEachItem(result.Result, Parent, true);
                    skipCode = true;
                }
            }
            #endregion

            #region 判断是否为分支文档结构
            if (ChildrenStringList.Count > 0 && DataType is DataType.Enum && !skipCode)
            {
                List<string> FilteredRawList = [];

                ClearCurrentSubItem(Parent);

                if (SelectedEnumItem.Text != "- unset -")
                {
                    bool haveCurrentEnum = false;
                    int startIndex = 0, endIndex = 0;

                    for (int i = 0; i < ChildrenStringList.Count; i++)
                    {
                        Match targetmatch = GetEnumRawKey().Match(ChildrenStringList[i]);
                        if (targetmatch.Success && ChildrenStringList[i].Contains(SelectedEnumItem.Text) && !haveCurrentEnum)
                        {
                            haveCurrentEnum = true;
                            startIndex = i + 1;
                            continue;
                        }

                        if (haveCurrentEnum && GetEnumRawKey().Match(ChildrenStringList[i]).Success)
                        {
                            endIndex = i;
                            break;
                        }
                    }

                    if (haveCurrentEnum)
                    {
                        if (endIndex > startIndex)
                        {
                            FilteredRawList = ChildrenStringList[startIndex..endIndex];
                        }
                        else
                        {
                            FilteredRawList = ChildrenStringList[startIndex..];
                        }
                    }

                    if (FilteredRawList.Count > 0)
                    {

                        JsonTreeViewDataStructure result = htmlHelper.GetTreeViewItemResult(new(), FilteredRawList, LayerCount);
                        Parent.Children.AddRange(result.Result);

                        if (result.Result.Count > 0)
                        {
                            result.Result[0].Previous = this;
                            Next = result.Result[0];
                        }

                        Plan.SetRangeText(StartLine.EndOffset, 0, result.ResultString.Length > 0 ? ",\r\n" + result.ResultString.ToString() : "");
                        JsonItemTool.SetLineNumbersForEachItem(result.Result, Parent, true);
                    }
                    skipCode = true;
                }
            }
            #endregion

            #region 处理父节点的尾行引用
            if(Parent is not null && (Parent.StartLine == Parent.EndLine || Parent.EndLine is null) && StartLine is not null)
            {
                Parent.EndLine = StartLine.NextLine;
            }
            #endregion

            oldSelectedEnumItem = SelectedEnumItem;
        }

        /// <summary>
        /// 切换子元素
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void SwitchCompoundState_Click(object sender, RoutedEventArgs e)
        {
            if (DataType is not DataType.None && DataType is not DataType.CustomCompound && DataType is not DataType.List)
            {
                IsCurrentExpanded = !IsCurrentExpanded;
            }

            if (IsCurrentExpanded || DataType is DataType.CustomCompound || DataType is DataType.Array || DataType is DataType.List || (Parent is not null && Parent.DataType is DataType.List))
            {
                if (currentItemReference is not null)
                {
                    currentItemReference.IsExpanded = true;
                    IsCurrentExpanded = true;
                }
                JsonItemTool.AddSubStructure(this);

                if (DataType is not DataType.CustomCompound && DataType is not DataType.None && DataType is not DataType.List)
                {
                    ElementButtonTip = "折叠";
                    PressedSwitchButtonColor = PressedMinusColor;
                    SwitchButtonIcon = MinusIcon;
                    SwitchButtonColor = MinusColor;
                }
            }
            else
            {
                JsonItemTool.CollapseCurrentItem(this);
                if (currentItemReference is not null)
                {
                    currentItemReference.IsExpanded = false;
                }
                ElementButtonTip = "展开";
                PressedSwitchButtonColor = PressedPlusColor;
                SwitchButtonIcon = PlusIcon;
                SwitchButtonColor = PlusColor;
            }
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