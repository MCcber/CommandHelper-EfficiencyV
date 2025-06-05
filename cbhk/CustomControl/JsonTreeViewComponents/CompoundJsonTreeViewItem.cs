using CBHK.CustomControl.Interfaces;
using CBHK.GeneralTool;
using CBHK.Service.Json;
using CBHK.Model.Common;
using CommunityToolkit.Mvvm.ComponentModel;
using ICSharpCode.AvalonEdit.Document;
using Prism.Ioc;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using static CBHK.Model.Common.Enums;

namespace CBHK.CustomControl.JsonTreeViewComponents
{
    public partial class CompoundJsonTreeViewItem : JsonTreeViewItem
    {
        #region Property
        [ObservableProperty]
        public string _elementButtonTip = "展开";
        public bool IsCurrentExpanded { get; set; } = false;
        public bool IsNotValueTypeChanging { get; set; } = true;
        public Geometry PlusIcon { get; } = Application.Current.Resources["TreeNodePlusGeometry"] as Geometry;
        public Geometry MinusIcon { get; } = Application.Current.Resources["TreeNodeMinusGeometry"] as Geometry;
        [ObservableProperty]
        public Geometry _switchButtonIcon;
        [ObservableProperty]
        public Brush _switchButtonColor = new SolidColorBrush();
        public SolidColorBrush PlusColor { get; } = new((Color)ColorConverter.ConvertFromString("#2AA515"));
        public SolidColorBrush MinusColor { get; } = new((Color)ColorConverter.ConvertFromString("#D81E06"));
        public int EndLineNumber { get; set; }
        public DocumentLine EndLine { get; set; }

        /// <summary>
        /// 枚举键
        /// </summary>
        public string EnumKey { get; set; } = "";
        /// <summary>
        /// 枚举结构附带的可缺省的成员数量
        /// </summary>
        public int EnumItemCount { get; set; }

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
        /// 是否为分支文档
        /// </summary>
        public bool IsEnumBranch { get; set; }

        /// <summary>
        /// 枚举数据源
        /// </summary>
        [ObservableProperty]
        public ObservableCollection<TextComboBoxItem> _enumItemsSource = [];

        public TextComboBoxItem oldSelectedEnumItem;

        /// <summary>
        /// 已选中的数据类型
        /// </summary>
        [ObservableProperty]
        public TextComboBoxItem _selectedValueType = null;

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
                BoolButtonVisibility = EnumBoxVisibility = InputBoxVisibility = Visibility.Collapsed;
            }
        }

        [ObservableProperty]
        public Visibility _addOrSwitchElementButtonVisibility = Visibility.Collapsed;

        [ObservableProperty]
        public Visibility _switchBoxVisibility = Visibility.Collapsed;

        [ObservableProperty]
        public Visibility _valueTypeBoxVisibility = Visibility.Collapsed;

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

        [ObservableProperty]
        public ObservableCollection<TextComboBoxItem> _valueTypeSource = [];
        #endregion

        #region Field
        private IContainerProvider _container;
        [GeneratedRegex(@"(?<=\s*\s?\*+;?\s*\s?(如果|若|当)).+(?=为|是).+")]
        private static partial Regex GetEnumRawKey();
        [GeneratedRegex(@"\[\[\#?((?<1>[\u4e00-\u9fff]+)\|(?<2>[\u4e00-\u9fff]+)|(?<1>[\u4e00-\u9fff]+))\]\]")]
        private static partial Regex GetContextKey();

        [GeneratedRegex(@"(?<=<code>)(?<1>[a-z_]+)(?=</code>)")]
        private static partial Regex GetEnumValueMode1();

        [GeneratedRegex(@"\{\{cd\|(?<1>[a-z:_]+)\}\}", RegexOptions.IgnoreCase)]
        private static partial Regex GetEnumValueMode2();

        [GeneratedRegex(@"默认为\{\{cd\|(?<1>[a-z_]+)\}\}", RegexOptions.IgnoreCase)]
        private static partial Regex GetDefaultStringValue();

        [GeneratedRegex(@"默认为(?<1>\d)+", RegexOptions.IgnoreCase)]
        private static partial Regex GetDefaultNumberValue();

        [GeneratedRegex(@"默认为\{\{cd\|(?<1>true|false)\}\}", RegexOptions.IgnoreCase)]
        private static partial Regex GetDefaultBoolValue();
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
            currentItemReference = (sender as FrameworkElement).FindParent<TreeViewItem>();
            if (this is CompoundJsonTreeViewItem compoundJsonTreeViewItem)
            {
                if (!IsCanBeDefaulted || compoundJsonTreeViewItem.Children.Count > 0)
                {
                    currentItemReference.IsExpanded = compoundJsonTreeViewItem.IsCurrentExpanded = true;
                    PressedSwitchButtonColor = PressedMinusColor;
                }
                else
                {
                    PressedSwitchButtonColor = PressedPlusColor;
                }
            }
        }

        /// <summary>
        /// 清空当前的分支节点
        /// </summary>
        private void ClearCurrentSubItem(CompoundJsonTreeViewItem parent)
        {
            #region 移除切换前的节点
            JsonTreeViewItem lastItem = null;
            if(EnumItemCount > 0 && EnumItemCount < parent.Children.Count)
            {
                lastItem = parent.Children[EnumItemCount];
                if(lastItem.StartLine is null)
                {
                    for (int i = EnumItemCount; i >= 0; i--)
                    {
                        if (parent.Children[i].StartLine is not null)
                        {
                            lastItem = parent.Children[i];
                            break;
                        }
                    }
                }
                lastItem ??= parent.Children.FirstOrDefault();
            }
            else
            {
                for (int i = parent.Children.Count - 1; i >= 0; i--)
                {
                    if (parent.Children[i].StartLine is not null)
                    {
                        lastItem = parent.Children[i];
                        break;
                    }
                }
                lastItem ??= parent.Children.FirstOrDefault();
            }
            int index = parent.Children.IndexOf(this) + 1;
            if (EnumItemCount > 0)
            {
                for (int i = index; i <= EnumItemCount && parent.Children.Count > 1; i++)
                {
                    parent.Children.RemoveAt(1);
                }
            }
            else
            {
                while (parent.Children.Count > 1 && index < parent.Children.Count)
                {
                    parent.Children.RemoveAt(index);
                }
            }
            #endregion

            #region 清空子结构的代码
            int length = 0;
            string lastChildItemText = parent.Plan.GetRangeText(parent.Children[0].StartLine.Offset, parent.Children[0].StartLine.Length);
            int minusOffset = lastChildItemText.TrimEnd().EndsWith(',') ? 1 : 0;
            int offset = parent.Children[0].StartLine.EndOffset - minusOffset;
            if (lastItem != parent.Children[0])
            {
                if (lastItem is CompoundJsonTreeViewItem lastCompoundItem && lastCompoundItem.EndLine is not null)
                {
                    length = lastCompoundItem.EndLine.EndOffset - offset;
                }
                else
                {
                    length = lastItem.StartLine.EndOffset - offset;
                }
            }
            else
            if(parent.EndLine is not null)
            {
                length = parent.EndLine.PreviousLine.EndOffset - offset;
            }
            Plan.SetRangeText(offset, length, "");
            #endregion
        }

        /// <summary>
        /// 处理数据类型的变更
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void ValueType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string currentValueTypeString = SelectedValueType.Text.ToLower();
            if(currentValueTypeString.Length > 0)
            {
                #region Field
                int customItemCount = 0;
                DataType = DataType.None;
                DataType = DataType.MultiType;
                IsNotValueTypeChanging = false;
                EnumItemsSource.Clear();
                IsNotValueTypeChanging = true;
                int offset = 0, length = 0;
                string startLineText = "";
                string ResultString = "";
                if (StartLine is not null && !StartLine.IsDeleted)
                {
                    startLineText = Plan.GetRangeText(StartLine.Offset, StartLine.Length);
                }
                bool isCompoundType = currentValueTypeString == "compound" || currentValueTypeString == "list" || currentValueTypeString.Contains("array");
                HtmlHelper htmlHelper = new(_container)
                {
                    plan = Plan,
                    jsonTool = JsonItemTool
                };
                #endregion

                #region 定位相邻的已有值的两个节点
                Tuple<JsonTreeViewItem, JsonTreeViewItem> previousAndNextItem = JsonItemTool.LocateTheNodesOfTwoAdjacentExistingValues(Previous, Next);
                JsonTreeViewItem previous = previousAndNextItem.Item1;
                JsonTreeViewItem next = previousAndNextItem.Item2;
                #endregion

                #region 将当前文本值清除，处理子节点
                Children.Clear();

                int minusOffset = currentValueTypeString == "- unset -" && (next is null || (next is not null && next.StartLine is null)) ? 1 : 0;

                if (currentValueTypeString == "- unset -" || StartLine is null)
                {
                    if (previous is CompoundJsonTreeViewItem previousCompoundItem && previousCompoundItem.EndLine is not null)
                    {
                        offset = previousCompoundItem.EndLine.EndOffset - minusOffset;
                    }
                    else
                    if (previous is not null && previous.StartLine is not null)
                    {
                        offset = previous.StartLine.EndOffset - minusOffset;
                    }
                    else
                    if (Parent is not null)
                    {
                        startLineText = Plan.GetRangeText(Parent.StartLine.Offset, Parent.StartLine.Length);
                        if (Parent.Key.Length > 0)
                        {
                            offset = Parent.StartLine.Offset + (Key.Contains(':') ? startLineText.LastIndexOf(':') : startLineText.IndexOf(':')) + 3;
                        }
                        else
                        if (startLineText.Contains('['))
                        {
                            offset = Parent.StartLine.Offset + startLineText.LastIndexOf('[') + 3;
                        }
                        else
                        {
                            offset = Parent.StartLine.Offset + startLineText.LastIndexOf('{') + 3;
                        }
                    }
                }
                else
                if (StartLine is not null && !StartLine.IsDeleted)
                {
                    int index = startLineText.IndexOf('"');
                    if (index == -1)
                    {
                        index = startLineText.IndexOf('{');
                    }
                    if (index == -1)
                    {
                        index = startLineText.IndexOf('[');
                    }
                    offset = StartLine.Offset + index;
                }
                else
                if (Parent is not null)
                {
                    startLineText = Plan.GetRangeText(Parent.StartLine.Offset, Parent.StartLine.Length);
                    offset = Parent.StartLine.Offset + (Key.Contains(':') ? startLineText.LastIndexOf(':') : startLineText.IndexOf(':')) + 3;
                }

                if (EndLine is not null && !EndLine.IsDeleted)
                {
                    length = EndLine.EndOffset - offset;
                }
                else
                if (StartLine is not null && !StartLine.IsDeleted)
                {
                    length = StartLine.EndOffset - offset;
                }
                if (StartLine is not null)
                {
                    Plan.SetRangeText(offset, length, "");
                }
                if (currentValueTypeString == "- unset -")
                {
                    DataType = DataType.None;
                    DataType = DataType.MultiType;
                    ValueTypeBoxVisibility = Visibility.Visible;
                    StartLine = EndLine = null;
                }
                length = 0;
                #endregion

                #region 判断是在添加枚举型结构还是展开复合节点
                bool addMoreCustomStructure = DataType is DataType.CustomCompound && Parent is not null && customItemCount > 0;

                bool addMoreStructure = DataType is DataType.OptionalCompound || DataType is DataType.Compound;

                bool addListStructure = DataType is DataType.List;

                bool addParentListStructure = DataType is not DataType.List && Parent is not null && Parent.DataType is DataType.List;
                #endregion

                #region 计算有多少个自定义子节点
                if (Parent is not null && Parent.Children.Count > 0 && Parent.Children[0] is CompoundJsonTreeViewItem subCompoundItem1 && subCompoundItem1.DataType is DataType.CustomCompound)
                {
                    customItemCount = 1;
                }
                else
                if (Parent is not null && Parent.Children.Count > 1 && Parent.Children[1] is CompoundJsonTreeViewItem subCompoundItem2 && subCompoundItem2.DataType is DataType.CustomCompound)
                {
                    customItemCount = 2;
                }
                #endregion

                #region 定义前置与后置连接符
                string connectorSymbol = "";
                string endConnectorSymbol = "";
                #endregion

                #region 计算前置衔接符
                bool isExpendOptionalCompound = IsCanBeDefaulted && previous is not null && previous.StartLine is not null && (next is null || (next is not null && next.StartLine is null));

                bool isParentAddElement = addParentListStructure;

                bool isAddListItem = StartLine is null && EndLine is null && DataType is DataType.List && previous is not null && previous.StartLine is not null && (next is null || (next is not null && next.StartLine is null));

                if (isParentAddElement || isAddListItem || StartLine is null)
                {
                    connectorSymbol = (isExpendOptionalCompound ? ',' : "") + "\r\n" + new string(' ', LayerCount * 2);
                }
                #endregion

                #region 计算后置衔接符
                CompoundJsonTreeViewItem endConnectorSymbolItem = null;
                if (addMoreCustomStructure || addParentListStructure)
                {
                    endConnectorSymbolItem = Parent;
                }
                else
                if (addMoreStructure || addListStructure)
                {
                    endConnectorSymbolItem = this;
                }

                if (endConnectorSymbolItem is not null && ((addMoreCustomStructure && endConnectorSymbolItem.Children.Count - customItemCount > 0) || (addMoreStructure && next is not null && next.StartLine is not null)))
                {
                    endConnectorSymbol = ",";
                }

                if (addListStructure && IsCanBeDefaulted && ((next is not null && next.StartLine is not null && ((StartLine is null && EndLine is null) || StartLine == EndLine)) || Children.Count > 1))
                {
                    endConnectorSymbol = ",";
                }

                if(next is not null && next.StartLine is not null)
                {
                    endConnectorSymbol = ",";
                }
                if(Parent is not null && Parent.StartLine is not null && (Parent.EndLine is null || (Parent.EndLine is not null && Parent.EndLine.IsDeleted) || Parent.EndLine == Parent.StartLine))
                {
                    endConnectorSymbol = "\r\n" + new string(' ', Parent.LayerCount * 2);
                }
                #endregion

                #region 处理不同的数据类型所需的值和交互控件

                #region 处理简单节点
                if (!isCompoundType)
                {
                    ResultString = connectorSymbol + "\"" + Key + "\": ";
                    AddOrSwitchElementButtonVisibility = Visibility.Collapsed;
                }
                switch (currentValueTypeString)
                {
                    case "bool":
                    case "boolean":
                        {
                            BoolButtonVisibility = Visibility.Visible;
                            Match defaultBoolMatch = GetDefaultBoolValue().Match(InfoTipText);
                            if (defaultBoolMatch.Success)
                            {
                                Value = bool.Parse(defaultBoolMatch.Groups[1].Value);
                                ResultString += Value;
                                Plan.SetRangeText(offset, length, Value + endConnectorSymbol);
                            }
                            else
                            {
                                Plan.SetRangeText(offset, length, ResultString + "false" + endConnectorSymbol);
                            }
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
                            string currentValue = "\"\"";

                            if (currentValueTypeString == "string")
                            {
                                MatchCollection enumModeMatch1 = GetEnumValueMode1().Matches(InfoTipText);
                                MatchCollection enumModeMatch2 = GetEnumValueMode2().Matches(InfoTipText);
                                Match contextMatch = GetContextKey().Match(InfoTipText);
                                if (enumModeMatch1.Count > 0)
                                {
                                    EnumItemsSource.AddRange(enumModeMatch1.Select(item => new TextComboBoxItem() { Text = item.Groups[1].Value }));
                                }
                                else
                                if (enumModeMatch2.Count > 0)
                                {
                                    EnumItemsSource.AddRange(enumModeMatch2.Select(item => new TextComboBoxItem() { Text = item.Groups[1].Value }));
                                }
                                else
                                if(contextMatch.Success)
                                {
                                    EnumItemsSource.Add(new TextComboBoxItem()
                                    {
                                        Text = "- unset -"
                                    });
                                    if (Plan.EnumIDDictionary.TryGetValue(contextMatch.Groups[1].Value,out List<string> targetEnumIDList))
                                    {
                                        EnumItemsSource.AddRange(targetEnumIDList.Select(item =>
                                        {
                                            return new TextComboBoxItem()
                                            {
                                                Text = item
                                            };
                                        }));
                                    }
                                    IsNotValueTypeChanging = false;
                                    SelectedEnumItem = EnumItemsSource[0];
                                    IsNotValueTypeChanging = true;
                                }

                                if (enumModeMatch1.Count > 0 || enumModeMatch2.Count > 0 || contextMatch.Success && EnumItemsSource.Count > 1)
                                {
                                    InputBoxVisibility = Visibility.Collapsed;
                                    EnumBoxVisibility = Visibility.Visible;
                                    Value = EnumItemsSource[0].Text;
                                    if (EnumItemsSource[0].Text != "- unset -")
                                    {
                                        currentValue = "\"" + EnumItemsSource[0].Text + "\"";
                                    }
                                }
                                else
                                {
                                    InputBoxVisibility = Visibility.Visible;
                                    EnumBoxVisibility = Visibility.Collapsed;
                                    Value = "";
                                }
                            }
                            else
                            {
                                Match defaultNumberMatch = GetDefaultNumberValue().Match(InfoTipText);
                                if (defaultNumberMatch.Success)
                                {
                                    Value = decimal.Parse(defaultNumberMatch.Groups[1].Value);
                                    currentValue = Value + "";
                                }
                                else
                                {
                                    Value = 0;
                                    currentValue = "0";
                                }
                            }
                            if (Key.Length > 0)
                            {
                                currentValue = connectorSymbol + "\"" + Key + "\": " + currentValue + endConnectorSymbol;
                            }
                            Plan.SetRangeText(offset, length, currentValue);
                            break;
                        }
                }
                #endregion

                #region 处理复合节点
                if (isCompoundType && currentValueTypeString != "- unset -")
                {
                    #region Field
                    string bracketPairString = "{}";
                    List<string> targetRawStringList = [];
                    List<string> targetList = [];
                    AddOrSwitchElementButtonVisibility = Visibility.Collapsed;
                    #endregion

                    if (ChildrenStringList.Count > 0 && currentValueTypeString != "list")
                    {
                        targetRawStringList = [..ChildrenStringList];
                        if(currentValueTypeString.Contains("array"))
                        {
                            bracketPairString = "[]";
                        }
                    }
                    else
                    {
                        Dictionary<string, List<string>> targetDictionary = [];
                        targetRawStringList = JsonItemTool.ExtractSubInformationFromPromptSourceCode(this);

                        #region 根据当前数据类型执行对应的操作
                        if (currentValueTypeString == "compound")
                        {
                            bracketPairString = "{}";
                            if (targetRawStringList.Count == 0)
                            {
                                targetRawStringList = [.. ChildrenStringList];
                            }
                        }
                        else
                        if (currentValueTypeString == "list" || currentValueTypeString.Contains("array"))
                        {
                            bracketPairString = "[]";
                            AddOrSwitchElementButtonVisibility = Visibility.Visible;
                        }
                        else
                        if (currentValueTypeString == "string" || currentValueTypeString == "enum")
                        {
                            EnumItemsSource.AddRange(targetDictionary.Keys.Select(item =>
                            {
                                return new TextComboBoxItem() { Text = item };
                            }));
                        }
                        #endregion
                    }

                    #region 执行分析，获取结果
                    JsonTreeViewDataStructure result = new();
                    if (currentValueTypeString != "list")
                    {
                        result = htmlHelper.GetTreeViewItemResult(new(), targetRawStringList, LayerCount + 1, "", this, null, 1, true);
                        while (result.ResultString.Length > 0 && (result.ResultString[^1] == ',' ||
                            result.ResultString[^1] == '\r' ||
                            result.ResultString[^1] == '\n'))
                        {
                            result.ResultString.Length--;
                        }
                        htmlHelper.HandlingTheTypingAppearanceOfCompositeItemList(result.Result, this);
                        for (int i = 0; i < result.Result.Count; i++)
                        {
                            result.Result[i].RemoveElementButtonVisibility = Visibility.Collapsed;
                        }
                        Children.AddRange(result.Result);
                    }
                    currentItemReference.IsExpanded = true;
                    #endregion

                    #region 更新代码编辑器
                    ResultString = result.ResultString.ToString();

                    if (currentValueTypeString != "list" && ResultString.Length > 0)
                    {
                        ResultString = connectorSymbol + (Key.Length > 0 ? "\"" + Key + "\": " : "") + bracketPairString[0] + "\r\n" + ResultString + "\r\n" + new string(' ',LayerCount * 2) + bracketPairString[1] + endConnectorSymbol;
                    }
                    else
                    {
                        ResultString = connectorSymbol + (Key.Length > 0 ? "\"" + Key + "\": " : "") + bracketPairString + endConnectorSymbol;
                    }
                    if (offset > 0 && ResultString.Length > 0)
                    {
                        Plan.SetRangeText(offset, 0, ResultString);
                    }
                    #endregion
                }
                #endregion

                #endregion

                #region 为每个子节点设置所需字段值
                EndLine = null;
                if (currentValueTypeString != "- unset -")
                {
                    if (previous is CompoundJsonTreeViewItem previousItem1 && previousItem1.EndLine is not null)
                    {
                        StartLine = previousItem1.EndLine.NextLine;
                    }
                    else
                    if (previous is not null && previous.StartLine is not null)
                    {
                        StartLine = previous.StartLine.NextLine;
                    }
                    else
                    if (Parent is not null)
                    {
                        StartLine = Parent.StartLine.NextLine;
                    }
                    if (currentValueTypeString == "compound" || currentValueTypeString.Contains("array"))
                    {
                        JsonItemTool.SetParentForEachItem(Children, this);
                        JsonItemTool.SetLayerCountForEachItem(Children, LayerCount + 1);
                        JsonItemTool.SetLineNumbersForEachItem(Children, this);
                    }
                    JsonTreeViewItem lastItem = JsonItemTool.SearchForTheLastItemWithRowReference(this);
                    if (lastItem is CompoundJsonTreeViewItem lastCompoundItem && lastCompoundItem.EndLine is not null)
                    {
                        EndLine = lastCompoundItem.EndLine.NextLine;
                    }
                    else
                    if (lastItem is not null && lastItem.StartLine is not null)
                    {
                        EndLine = lastItem.StartLine.NextLine;
                    }
                    EndLine ??= StartLine;
                }

                if (Parent is not null && (Parent.StartLine == Parent.EndLine || Parent.EndLine is null || (Parent.EndLine is not null && Parent.EndLine.IsDeleted)))
                {
                    JsonTreeViewItem parentLastItem = JsonItemTool.SearchForTheLastItemWithRowReference(Parent);
                    if (parentLastItem is CompoundJsonTreeViewItem parentLastCompoundItem && parentLastCompoundItem.EndLine is not null)
                    {
                        Parent.EndLine = parentLastCompoundItem.EndLine.NextLine;
                    }
                    else
                    if (parentLastItem is not null && parentLastItem.StartLine is not null)
                    {
                        Parent.EndLine = parentLastItem.StartLine.NextLine;
                    }
                }
                if (SelectedValueType is not null && SelectedValueType.Text == "- unset -" && IsCanBeDefaulted &&
                    (previous is null || (previous is not null && previous.StartLine is null)) &&
                    (next is null || (next is not null && next.StartLine is null)))
                {
                    offset = Parent.StartLine.EndOffset;
                    string endLineText = Plan.GetRangeText(Parent.EndLine.Offset,Parent.EndLine.Length);
                    int lastCharIndex = endLineText.LastIndexOf('}');
                    if(lastCharIndex == -1)
                    {
                        lastCharIndex = endLineText.LastIndexOf(']');
                    }
                    length = Parent.EndLine.Offset + lastCharIndex - offset;
                    Plan.SetRangeText(offset, length, "");
                    Parent.EndLine = Parent.StartLine;
                }
                #endregion

                #region 复位
                if (StartLine is not null && StartLine.IsDeleted)
                {
                    StartLine = null;
                }
                if (EndLine is not null && EndLine.IsDeleted)
                {
                    EndLine = null;
                }
                #endregion
            }
        }

        /// <summary>
        /// 处理枚举值的变更
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void EnumType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            #region 判断是否无需处理
            if (DataType is DataType.CustomCompound || EnumItemsSource.Count == 0 || !IsNotValueTypeChanging)
            {
                return;
            }
            #endregion

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
            if (StartLine is not null && !StartLine.IsDeleted)
            {
                currentLineText = Plan.GetRangeText(StartLine.Offset, StartLine.Length);
                if(Key.Length > 0)
                {
                    if (Key.Contains(':'))
                    {
                        colonOffset = (Key.Contains(':') ? currentLineText.LastIndexOf(':') : currentLineText.IndexOf(':')) + 2;
                    }
                    else
                    {
                        colonOffset = currentLineText.IndexOf(':') + 2;
                    }
                }
                else
                {
                    colonOffset = currentLineText.IndexOf('"');
                }
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
                string connectorString = (previous is not null && previous.StartLine is not null && (next is null || (next is not null && next.StartLine is null)) ? "," : "") + "\r\n" + new string(' ', LayerCount * 2);
                string endConnectorString = next is not null && next.StartLine is not null ? "," : "";
                if (previous is CompoundJsonTreeViewItem previousCompoundItem1 && previousCompoundItem1.EndLine is not null)
                {
                    Plan.UpdateNullValueBySpecifyingInterval(previousCompoundItem1.EndLine.EndOffset, connectorString + (Key.Length > 0 ? "\"" + Key + "\": " : "") + "\"" + SelectedEnumItem.Text + "\"" + endConnectorString);
                }
                else
                if (previous is not null && previous.StartLine is not null)
                {
                    Plan.UpdateNullValueBySpecifyingInterval(previous.StartLine.EndOffset, connectorString + (Key.Length > 0 ? "\"" + Key + "\": " : "") + "\"" + SelectedEnumItem.Text + "\"" + endConnectorString);
                }
                else
                if (Parent is not null)
                {
                    if ((Parent.DataType is DataType.List && ((Parent.StartLine == Parent.EndLine) || Parent.EndLine is null)) || ((Parent.DataType is DataType.Compound || Parent.DataType is DataType.OptionalCompound) && ((Parent.StartLine == Parent.EndLine) || Parent.EndLine is null)))
                    {
                        endConnectorString = "\r\n" + new string(' ', Parent.LayerCount * 2);
                    }
                    DocumentLine parentStartLine = Plan.GetLineByNumber(Parent.StartLine.LineNumber);
                    string parentStartLineText = Plan.GetRangeText(parentStartLine.Offset, parentStartLine.EndOffset - parentStartLine.Offset);
                    offset = Parent.StartLine.Offset + (Key.Contains(':') && StartLine is not null ? parentStartLineText.LastIndexOf(':') : parentStartLineText.IndexOf(':')) + 3;
                    Plan.SetRangeText(offset, 0, connectorString + (Key.Length > 0 ? "\"" + Key + "\": " : "") + "\"" + SelectedEnumItem.Text + "\"" + endConnectorString);
                }
                else
                {
                    Plan.SetRangeText(1, 0, connectorString + "\"" + Key + "\": \"" + SelectedEnumItem.Text + "\"" + endConnectorString);
                }

                if(previous is CompoundJsonTreeViewItem previousCompoundItem2 && previousCompoundItem2 is not null && previousCompoundItem2.EndLine is not null)
                {
                    StartLine = previousCompoundItem2.EndLine.NextLine;
                }
                else
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
                if(!IsCanBeDefaulted && oldSelectedEnumItem is not null)
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
                    if ((next is not null && next.StartLine is not null) || (previous is not null && previous.StartLine is not null))
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
                    colonOffset = (Key.Contains(':') ? parentStartLineText.LastIndexOf(':') : parentStartLineText.IndexOf(':')) + 3;
                    string parentEndLineText = "";

                    if (next is null || (next is not null && next.StartLine is null))
                    {
                        parentEndLineText = Plan.GetRangeText(Parent.EndLine.Offset, Parent.EndLine.Length);
                    }
                    int endBracketOffset = parentEndLineText.IndexOf('}');
                    if(endBracketOffset == -1)
                    {
                        endBracketOffset = parentEndLineText.IndexOf(']');
                    }
                    offset = Parent.StartLine.Offset + colonOffset;
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

            #region 判断是否需要应用枚举结构
            if (EnumKey.Length > 0 && !skipCode)
            {
                if (Plan.EnumCompoundDataDictionary.TryGetValue(EnumKey, out Dictionary<string, List<string>> targetDependencyDictionary) && targetDependencyDictionary.TryGetValue(SelectedEnumItem.Text, out List<string> targetRawList))
                {
                    ClearCurrentSubItem(Parent);
                    Match firstKeyWordMatch = GetEnumValueMode1().Match(targetRawList[0]);
                    List<string> targetRawListTemp = [.. targetRawList];
                    if(firstKeyWordMatch.Success && firstKeyWordMatch.Groups[1].Value == SelectedEnumItem.Text)
                    {
                        targetRawListTemp.RemoveAt(0);
                    }
                    JsonTreeViewDataStructure result = htmlHelper.GetTreeViewItemResult(new(), targetRawListTemp, LayerCount, "", this, null, 1, true);
                    
                    htmlHelper.HandlingTheTypingAppearanceOfCompositeItemList(result.Result,Parent);

                    int index = 1;
                    EnumItemCount = result.Result.Count;
                    for (int i = 0; i < result.Result.Count; i++)
                    {
                        Parent.Children.Insert(index, result.Result[i]);
                        result.Result[i].RemoveElementButtonVisibility = Visibility.Collapsed;
                        index++;
                    }

                    if (result.Result.Count > 0)
                    {
                        result.Result[0].Previous = this;
                        Next = result.Result[0];
                        int insertIndex = result.Result.Count + 1;
                        if (insertIndex < Parent.Children.Count)
                        {
                            Parent.Children[insertIndex].Previous = result.Result[^1];
                            result.Result[^1].Next = Parent.Children[insertIndex];
                        }
                    }
                    while (result.ResultString.Length > 0 && (result.ResultString[^1] == '\r' ||
                        result.ResultString[^1] == '\n' ||
                        result.ResultString[^1] == ',' ||
                        result.ResultString[^1] == ' '))
                    {
                        result.ResultString.Length--;
                    }

                    if (next is not null)
                    {
                        Tuple<JsonTreeViewItem, JsonTreeViewItem> NextNextItem = JsonItemTool.LocateTheNodesOfTwoAdjacentExistingValues(next.Previous, next.Next);

                        if (NextNextItem.Item2 is not null && NextNextItem.Item2.StartLine is not null)
                        {
                            result.ResultString.Append(',');
                        }
                    }

                    if (result.ResultString.Length > 0)
                    {
                        Plan.SetRangeText(StartLine.EndOffset, 0, result.ResultString.Length > 0 ? ",\r\n" + result.ResultString.ToString() : "");
                        JsonItemTool.SetLineNumbersForEachItem(result.Result, Parent, true);
                    }
                    else
                    {
                        for (int i = 0; i < result.Result.Count; i++)
                        {
                            result.Result[i].Parent = Parent;
                            if(i > 0)
                            {
                                result.Result[i].Previous = result.Result[i - 1];
                                result.Result[i - 1].Next = result.Result[i];
                            }
                        }
                    }
                    skipCode = true;
                }
            }
            #endregion

            #region 判断是否为分支文档结构
            if (ChildrenStringList.Count > 0 && IsEnumBranch && DataType is DataType.Enum && !skipCode)
            {
                List<string> FilteredRawList = [];

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
                        ClearCurrentSubItem(Parent);
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

                        JsonTreeViewDataStructure result = htmlHelper.GetTreeViewItemResult(new(), FilteredRawList, LayerCount, "", this, null, 1, true);
                        Parent.Children.AddRange(result.Result);

                        if (result.Result.Count > 0)
                        {
                            result.Result[0].Previous = this;
                            Next = result.Result[0];
                        }

                        Plan.SetRangeText(StartLine.EndOffset, 0, result.ResultString.Length > 0 ? ",\r\n" + result.ResultString.ToString() : "");
                        JsonItemTool.SetLineNumbersForEachItem(result.Result, Parent, true);
                        JsonItemTool.SetParentForEachItem(result.Result, Parent);
                        JsonItemTool.SetLayerCountForEachItem(result.Result, Parent.LayerCount + 1);
                    }
                    skipCode = true;
                }
                else
                {
                    ClearCurrentSubItem(Parent);
                }
            }
            #endregion

            #region 处理父节点的尾行引用
            if(Parent is not null && (Parent.StartLine == Parent.EndLine || (Parent.EndLine is null || (Parent.EndLine is not null && Parent.EndLine.IsDeleted))) && StartLine is not null)
            {
                Parent.EndLine = StartLine.NextLine;
            }
            #endregion

            #region 复位与存储
            if (StartLine is not null && StartLine.IsDeleted)
            {
                StartLine = null;
            }
            if (EndLine is not null && EndLine.IsDeleted)
            {
                EndLine = null;
            }

            oldSelectedEnumItem = SelectedEnumItem;
            #endregion
        }

        /// <summary>
        /// 切换子元素
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void SwitchCompoundState_Click(object sender, RoutedEventArgs e)
        {
            if (DataType is not DataType.None && DataType is not DataType.CustomCompound && DataType is not DataType.List && (DataType is not DataType.MultiType || (DataType is DataType.MultiType && SelectedValueType is not null && SelectedValueType.Text != "List")))
            {
                IsCurrentExpanded = !IsCurrentExpanded;
            }

            if (IsCurrentExpanded || DataType is DataType.CustomCompound || DataType is DataType.Array || DataType is DataType.List || (Parent is not null && Parent.DataType is DataType.List) || (DataType is DataType.MultiType && SelectedValueType is not null && SelectedValueType.Text == "List"))
            {
                if (currentItemReference is not null)
                {
                    currentItemReference.IsExpanded = true;
                    IsCurrentExpanded = true;
                }
                JsonItemTool.AddSubStructure(this);

                if(DataType is DataType.CustomCompound && InputBoxVisibility is Visibility.Visible)
                {
                    Value = "";
                }
                if (DataType is not DataType.CustomCompound && DataType is not DataType.None && DataType is not DataType.List && (DataType is not DataType.MultiType || (DataType is DataType.MultiType && SelectedValueType is not null && SelectedValueType.Text != "List")))
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
        #endregion
    }
}