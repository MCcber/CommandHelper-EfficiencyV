using CBHK.CustomControl.Interfaces;
using CBHK.GeneralTool;
using CBHK.Model.Common;
using CBHK.Service.Json;
using CBHK.ViewModel.Common;
using CommunityToolkit.Mvvm.ComponentModel;
using ICSharpCode.AvalonEdit.Document;
using Prism.Ioc;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using static CBHK.Model.Common.Enums;

namespace CBHK.CustomControl.JsonTreeViewComponents
{
    public partial class BaseCompoundJsonTreeViewItem : JsonTreeViewItem
    {
        #region Field
        private IContainerProvider _container;
        [GeneratedRegex(@"(?<=\s*\s?\*+;?\s*\s?(如果|若|当)).+(?=为|是).+")]
        private static partial Regex GetEnumRawKey();
        [GeneratedRegex(@"\[\[\#?((?<1>[\u4e00-\u9fff]+)\|(?<2>[\u4e00-\u9fff]+)|(?<1>[\u4e00-\u9fff]+))\]\]")]
        private static partial Regex GetContextKey();

        [GeneratedRegex(@"^\s*\s?\:?\s*\s?(\*+)")]
        private static partial Regex GetLineStarCount();

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

        #region Property
        /// <summary>
        /// 是否被外观检查器处理过
        /// </summary>
        public bool IsProcessedByAppearanceChecker { get; set; } = false;
        /// <summary>
        /// 添加按钮悬浮文本
        /// </summary>
        [ObservableProperty]
        private string _elementButtonTip = "展开";
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
        /// 视觉上的最后一个子节点
        /// </summary>
        public JsonTreeViewItem VisualLastChild { get; set; }

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

        private TextComboBoxItem oldSelectedEnumItem;
        private TextComboBoxItem oldSelectedValueTypeItem;

        /// <summary>
        /// 已选中的数据类型
        /// </summary>
        private TextComboBoxItem _selectedValueType = null;

        public TextComboBoxItem SelectedValueType
        {
            get => _selectedValueType;
            set
            {
                SetProperty(ref _selectedValueType, value);
                if (_selectedValueType is not null)
                {
                    DataType = _selectedValueType.Text.ToLowerInvariant() switch
                    {
                        "bool" or "boolean" => DataType.Bool,
                        "byte" => DataType.Byte,
                        "short" => DataType.Short,
                        "int" => DataType.Int,
                        "float" => DataType.Float,
                        "double" => DataType.Double,
                        "long" => DataType.Long,
                        "string" => DataType.String,
                        _ => DataType.None
                    };
                    ItemType = _selectedValueType.Text.ToLowerInvariant() switch
                    {
                        "list" => ItemType.List,
                        "array" => ItemType.Array,
                        "compound" => ItemType.Compound,
                        _ => ItemType.MultiType
                    };
                }
            }
        }

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

        private DataType dataType = DataType.String;
        public new DataType DataType
        {
            get => dataType;
            set
            {
                SetProperty(ref dataType, value);
                BoolButtonVisibility = EnumBoxVisibility = InputBoxVisibility = Visibility.Collapsed;
            }
        }

        public ItemType ItemType { get; set; } = ItemType.BottomButton;

        [ObservableProperty]
        public Visibility _addOrSwitchElementButtonVisibility = Visibility.Collapsed;

        [ObservableProperty]
        public Visibility _switchBoxVisibility = Visibility.Collapsed;

        [ObservableProperty]
        public Visibility _valueTypeBoxVisibility = Visibility.Collapsed;

        /// <summary>
        /// 可切换的复合子节点源码集合
        /// </summary>
        [ObservableProperty]
        public List<string> _compoundChildrenStringList = [];

        /// <summary>
        /// 可切换的数组子节点源码集合
        /// </summary>
        [ObservableProperty]
        public List<string> _arrayChildrenStringList = [];

        /// <summary>
        /// 可切换的列表子节点源码集合
        /// </summary>
        [ObservableProperty]
        public List<string> _listChildrenStringList = [];

        /// <summary>
        /// 可切换的类型集合
        /// </summary>
        [ObservableProperty]
        public List<string> _valueTypeStringList = [];

        /// <summary>
        /// 子节点集合
        /// </summary>
        [ObservableProperty]
        public ObservableCollection<JsonTreeViewItem> _logicChildren = [];

        [ObservableProperty]
        public ObservableCollection<TextComboBoxItem> _valueTypeSource = [];
        #endregion

        #region Method
        public BaseCompoundJsonTreeViewItem(ICustomWorldUnifiedPlan plan, IJsonItemTool jsonItemTool, IContainerProvider containerProvider)
        {
            Plan = plan;
            JsonItemTool = jsonItemTool;
            _container = containerProvider;
        }

        /// <summary>
        /// 搜索视觉上的前一个与后一个节点
        /// </summary>
        public Tuple<JsonTreeViewItem,JsonTreeViewItem> SearchVisualPreviousAndNextItem(JsonTreeViewItem jsonTreeViewItem,bool isNeedSearchPrevious = true)
        {
            #region 定义字段、确定节点集合
            JsonTreeViewItem previous = null, next = null;
            int startIndex = jsonTreeViewItem.Index;
            ObservableCollection<JsonTreeViewItem> treeViewItemList = [];
            if(jsonTreeViewItem.Parent is not null)
            {
                treeViewItemList = jsonTreeViewItem.Parent.LogicChildren;
            }
            else
            if(jsonTreeViewItem is BaseCompoundJsonTreeViewItem baseCompoundJsonTreeViewItem)
            {
                treeViewItemList = baseCompoundJsonTreeViewItem.LogicChildren;
            }
            else
            if (jsonTreeViewItem.Plan is BaseCustomWorldUnifiedPlan basePlan)
            {
                treeViewItemList = basePlan.TreeViewItemList;
            }

            if(treeViewItemList.Count == 0)
            {
                return new(null,null);
            }
            #endregion

            #region 搜索视觉上的前一个节点
            if (isNeedSearchPrevious)
            {
                startIndex--;
                while (startIndex >= 0 && treeViewItemList[startIndex].StartLine is null)
                {
                    startIndex--;
                }
                if (startIndex < 0)
                {
                    startIndex = 0;
                }
                if (startIndex < treeViewItemList.Count && treeViewItemList[startIndex].StartLine is not null && treeViewItemList[startIndex] != jsonTreeViewItem && (treeViewItemList[startIndex] is not BaseCompoundJsonTreeViewItem || (treeViewItemList[startIndex] is BaseCompoundJsonTreeViewItem targetCompoundItem1 && targetCompoundItem1.ItemType is not ItemType.CustomCompound && targetCompoundItem1.ItemType is ItemType.BottomButton)))
                {
                    previous = treeViewItemList[startIndex];
                }
            }
            #endregion

            #region 搜索视觉上的后一个节点
            startIndex = treeViewItemList.Count - 1;
            while (startIndex >= 0 && (treeViewItemList[startIndex].StartLine is null || (treeViewItemList[startIndex] is BaseCompoundJsonTreeViewItem baseCompoundJsonTreeViewItem2 && baseCompoundJsonTreeViewItem2.ItemType is ItemType.BottomButton)))
            {
                startIndex--;
            }
            if (startIndex >= 0 && startIndex < treeViewItemList.Count && (treeViewItemList[startIndex] is not BaseCompoundJsonTreeViewItem || (treeViewItemList[startIndex] is BaseCompoundJsonTreeViewItem targetCompoundItem2 && targetCompoundItem2.ItemType is not ItemType.BottomButton && targetCompoundItem2.ItemType is not ItemType.CustomCompound)))
            {
                next = treeViewItemList[startIndex];
            }
            return new(previous, next);
            #endregion
        }

        /// <summary>
        /// 为指定节点集合的所有成员设置各自视觉上的前一个与后一个节点引用(二分搜索最近邻居算法)
        /// </summary>
        /// <param name="treeViewItemList"></param>
        public void SetVisualPreviousAndNextForEachItem()
        {
            if (LogicChildren == null || LogicChildren.Count == 0)
            {
                return;
            }

            //收集必选节点
            var requiredNodes = LogicChildren.Where(item => (item is not BaseCompoundJsonTreeViewItem || (item is BaseCompoundJsonTreeViewItem baseItem && baseItem.ItemType is not ItemType.BottomButton && baseItem.ItemType is not ItemType.CustomCompound)) && (!item.IsCanBeDefaulted || item.StartLine is not null)).ToList();
            int requiredCount = requiredNodes.Count;

            //没有必选节点时的处理
            if (requiredCount == 0)
            {
                foreach (var node in LogicChildren)
                {
                    node.VisualPrevious = null;
                    node.VisualNext = null;
                }
                return;
            }

            //提取索引列表
            var requiredIndices = requiredNodes.Select(r => r.Index).ToList();

            //处理每个节点
            foreach (var node in LogicChildren)
            {
                if(node is BaseCompoundJsonTreeViewItem baseCompoundItem && (baseCompoundItem.ItemType is ItemType.BottomButton || baseCompoundItem.ItemType is ItemType.CustomCompound))
                {
                    continue;
                }
                // 在必选节点索引列表中执行二分查找
                int position = requiredIndices.BinarySearch(node.Index);

                if (position >= 0)
                {
                    // 当前节点是必选节点
                    // 前一个必选节点 (索引-1)
                    node.VisualPrevious = position > 0 && requiredNodes.Count > 0 && requiredNodes[position - 1] != node
                        ? requiredNodes[position - 1]
                        : null;

                    // 后一个必选节点 (索引+1)
                    node.VisualNext = requiredNodes.Count > 0 && ((position < requiredCount - 1) || (position < requiredCount - 1 && requiredNodes[position + 1] != node))
                        ? requiredNodes[position + 1]
                        : null;
                }
                else
                {
                    // 当前节点是非必选节点
                    // 计算插入位置 (~position)
                    int insertPosition = ~position;

                    // 左侧最近的必选节点 (插入位置-1)
                    node.VisualPrevious = insertPosition > 0 && insertPosition <= requiredCount && requiredNodes.Count > 0 && requiredNodes[insertPosition - 1] != node
                        ? requiredNodes[insertPosition - 1]
                        : null;

                    // 右侧最近的必选节点 (插入位置)
                    node.VisualNext = insertPosition >= 0 && insertPosition < requiredCount && requiredNodes.Count > 0 && requiredNodes[insertPosition] != node
                        ? requiredNodes[insertPosition]
                        : null;
                }

                //递归检查子级是否需要再设置所有成员的视觉引用
                if(node is BaseCompoundJsonTreeViewItem baseCompoundJsonTreeViewItem && baseCompoundJsonTreeViewItem.LogicChildren.Count > 0)
                {
                    baseCompoundJsonTreeViewItem.SetVisualPreviousAndNextForEachItem();
                }
            }
        }

        /// <summary>
        /// 寻找列表中最后一个视觉节点
        /// </summary>
        /// <param name="list"></param>
        /// <returns></returns>
        public static JsonTreeViewItem SearchEnumGroupLastVisualItem(List<JsonTreeViewItem> list)
        {
            JsonTreeViewItem result = null;
            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (list[i].StartLine is not null && !list[i].StartLine.IsDeleted)
                {
                    result = list[i];
                    break;
                }
            }
            return result;
        }

        /// <summary>
        /// 添加子节点集合，一般用于可选复合节点的展开
        /// </summary>
        /// <param name="childrenDataList"></param>
        public void AddChildrenList(JsonTreeViewDataStructure childrenDataList)
        {
            #region 处理视觉树

            #region Field
            DocumentLine targetLine = null;
            int offset = 0, length = 0;
            string appendString = "";
            string keyString = Key.Length > 0 ? "\"" + Key + "\": " : "";
            char startBracketChar = ' ';
            char endBracketChar = ' ';
            bool isNeedStartComma = VisualPrevious is not null && VisualNext is null;
            bool isNeedEndComma = false;
            bool isNeedParentHead = false;
            #endregion

            #region 计算当前节点的Key值并拼接当前子信息最后应用于代码编辑器
            if (ItemType is ItemType.Array || ItemType is ItemType.List)
            {
                startBracketChar = '[';
                endBracketChar = ']';
            }
            else
            {
                startBracketChar = '{';
                endBracketChar = '}';
            }

            if (VisualLastChild is not null)
            {
                targetLine = VisualLastChild is BaseCompoundJsonTreeViewItem baseCompoundJsonTreeViewItem && baseCompoundJsonTreeViewItem.EndLine is not null && !baseCompoundJsonTreeViewItem.EndLine.IsDeleted ? baseCompoundJsonTreeViewItem.EndLine : VisualLastChild.StartLine;
            }
            else
            if (StartLine is not null)
            {
                string currentStartLineString = Plan.GetRangeText(StartLine.Offset, StartLine.EndOffset - StartLine.Offset);
                if (Key.Length > 0 || ItemType is ItemType.CustomCompound)
                {
                    int colonIndex = currentStartLineString.LastIndexOf(':');
                    offset = StartLine.Offset + colonIndex + 3;
                }
                else
                {
                    int bracketIndex = currentStartLineString.IndexOf('{');
                    if (bracketIndex == -1)
                    {
                        bracketIndex = currentStartLineString.IndexOf('[');
                    }
                    offset = bracketIndex + 1;
                }
            }
            else
            if (VisualPrevious is not null)
            {
                isNeedParentHead = true;
                isNeedEndComma = VisualNext is not null;
                if (VisualPrevious is BaseCompoundJsonTreeViewItem previousBaseCompoundJsonTreeViewItem && previousBaseCompoundJsonTreeViewItem.EndLine is not null && !previousBaseCompoundJsonTreeViewItem.EndLine.IsDeleted)
                {
                    targetLine = previousBaseCompoundJsonTreeViewItem.EndLine;
                }
                else
                {
                    targetLine = VisualPrevious.StartLine;
                }
            }
            else
            {
                isNeedParentHead = true;
                isNeedEndComma = childrenDataList.ResultString.Length > 0 && VisualNext is not null;
                string currentStartLineString = "{}";
                if(StartLine is null && LogicPrevious is not null && LogicPrevious.StartLine is not null)
                {
                    isNeedParentHead = true;
                    isNeedStartComma = true;
                    if (LogicPrevious is BaseCompoundJsonTreeViewItem previousBaseCompoundJsonTreeViewItem && previousBaseCompoundJsonTreeViewItem.EndLine is not null && !previousBaseCompoundJsonTreeViewItem.EndLine.IsDeleted)
                    {
                        targetLine = previousBaseCompoundJsonTreeViewItem.EndLine;
                    }
                    else
                    {
                        targetLine = LogicPrevious.StartLine;
                    }
                }
                else
                if (Parent is not null)
                {
                    currentStartLineString = Plan.GetRangeText(Parent.StartLine.Offset, Parent.StartLine.EndOffset - Parent.StartLine.Offset);
                }

                if ((Key.Length > 0 || ItemType is ItemType.CustomCompound) && currentStartLineString != "{}")
                {
                    int index = currentStartLineString.LastIndexOf(':');
                    if(index == -1)
                    {
                        index = currentStartLineString.IndexOf('{');
                        offset = Parent.StartLine.Offset + index + 1;
                    }
                    else
                    {
                        offset = Parent.StartLine.Offset + index + 3;
                    }
                    if (index == -1)
                    {
                        index = currentStartLineString.IndexOf('[');
                        offset = Parent.StartLine.Offset + index + 1;
                    }
                }
                else
                if(currentStartLineString != "{}")
                {
                    int bracketIndex = currentStartLineString.IndexOf('{');
                    if (bracketIndex == -1)
                    {
                        bracketIndex = currentStartLineString.IndexOf('[');
                    }
                    offset = bracketIndex + 1;
                }
                else
                {
                    offset = 1;
                }
            }

            if (isNeedParentHead)
            {
                appendString += (isNeedStartComma ? ',' : "") + "\r\n" + new string(' ', LayerCount * 2) + keyString + startBracketChar.ToString().Trim();
            }

            string resultString = childrenDataList.ResultString.ToString();
            resultString = resultString.Length > 1 ? resultString : resultString.Trim();

            bool isParentHaveNoVisualChild = (VisualPrevious is null && VisualNext is null) || (Parent is not null && (Parent.EndLine is null || (Parent.EndLine is not null && Parent.EndLine.IsDeleted) || Parent.StartLine == Parent.EndLine));

            appendString += (childrenDataList.ResultString.Length > 1 ? "\r\n" + resultString : "") + (childrenDataList.ResultString.Length > 1 ? "\r\n" + new string(' ', LayerCount * 2) : "") + (endBracketChar.ToString().Length > 0 ? endBracketChar.ToString().Trim() : "") + (isNeedEndComma ? ',' : "") + (!isNeedEndComma && isParentHaveNoVisualChild ? "\r\n" + (Parent is not null ? new string(' ', Parent.LayerCount * 2) : "") : "");
            //不是用目标行就是用指定偏移量执行替换
            if (targetLine is not null)
            {
                Plan.SetRangeText(targetLine.EndOffset, 0, appendString);
            }
            else
            {
                Plan.SetRangeText(offset, length, appendString);
            }
            #endregion

            #region 为当前层所有节点更新视觉引用
            if (Parent is not null)
            {
                Tuple<JsonTreeViewItem,JsonTreeViewItem> previousAndNext1 = JsonItemTool.SetLineNumbersForEachSubItem(Parent.LogicChildren, Parent,this);
                if(previousAndNext1 is not null && previousAndNext1.Item2 is not null)
                {
                    Parent.VisualLastChild = previousAndNext1.Item2;
                }
                JsonItemTool.SetParentForEachItem(Parent.LogicChildren, Parent);
            }
            else
            if(Plan is BaseCustomWorldUnifiedPlan basePlan)
            {
                int lineNumber = 2;
                if(VisualPrevious is not null && VisualPrevious is BaseCompoundJsonTreeViewItem baseCompoundJsonTreeViewItem && baseCompoundJsonTreeViewItem.EndLine is not null && !baseCompoundJsonTreeViewItem.EndLine.IsDeleted)
                {
                    lineNumber = baseCompoundJsonTreeViewItem.EndLine.LineNumber;
                }
                else
                if(VisualPrevious is not null)
                {
                    lineNumber = VisualPrevious.StartLine.LineNumber;
                }
                Tuple<JsonTreeViewItem, JsonTreeViewItem> previousAndNext3 = basePlan.SearchVisualPreviousAndNextItem(basePlan.TreeViewItemList[0], false);
                if (previousAndNext3 is not null && previousAndNext3.Item2 is not null)
                {
                    basePlan.VisualLastItem = previousAndNext3.Item2;
                }
                else
                {
                    basePlan.VisualLastItem = this;
                }
                JsonItemTool.SetParentForEachItem(basePlan.TreeViewItemList, null);
            }
            #endregion

            #endregion

            #region 处理逻辑树

            #region 添加子节点集并设置视觉引用
            LogicChildren.AddRange(childrenDataList.Result);
            JsonItemTool.SetParentForEachItem(LogicChildren, this);
            #endregion

            #region 设置当前复合节点行首引用
            if (isNeedParentHead && Parent is not null && StartLine is null)
            {
                if (VisualPrevious is not null)
                {
                    if (VisualPrevious is BaseCompoundJsonTreeViewItem previousCompoundItem && previousCompoundItem.EndLine is not null && !previousCompoundItem.EndLine.IsDeleted)
                    {
                        StartLine = previousCompoundItem.EndLine.NextLine;
                    }
                    else
                    {
                        StartLine = VisualPrevious.StartLine.NextLine;
                    }
                }
                else
                if (LogicPrevious is not null && LogicPrevious.StartLine is not null)
                {
                    if (LogicPrevious is BaseCompoundJsonTreeViewItem previousCompoundItem && previousCompoundItem.EndLine is not null && !previousCompoundItem.EndLine.IsDeleted)
                    {
                        StartLine = previousCompoundItem.EndLine.NextLine;
                    }
                    else
                    {
                        StartLine = LogicPrevious.StartLine.NextLine;
                    }
                }
                else
                if (Parent is not null)
                {
                    StartLine = Parent.StartLine.NextLine;
                }
                else
                {
                    StartLine = Plan.GetLineByNumber(2);
                }
            }
            else
            if (isNeedParentHead && StartLine is null)
            {
                if (VisualPrevious is BaseCompoundJsonTreeViewItem baseCompoundJsonTreeViewItem && baseCompoundJsonTreeViewItem.EndLine is not null && !baseCompoundJsonTreeViewItem.EndLine.IsDeleted)
                {
                    StartLine = baseCompoundJsonTreeViewItem.EndLine.NextLine;
                }
                else
                if (VisualPrevious is not null)
                {
                    StartLine = VisualPrevious.StartLine.NextLine;
                }
                else
                if(Plan is BaseCustomWorldUnifiedPlan baseCustomWorldUnifiedPlan)
                {
                    Tuple<JsonTreeViewItem, JsonTreeViewItem> previousAndNext1 = JsonItemTool.SetLineNumbersForEachSubItem(baseCustomWorldUnifiedPlan.TreeViewItemList, null, this);
                    if (previousAndNext1 is not null && previousAndNext1.Item1 != this)
                    {
                        if(previousAndNext1.Item1 is BaseCompoundJsonTreeViewItem lastCompoundItem && lastCompoundItem.EndLine is not null && !lastCompoundItem.EndLine.IsDeleted)
                        {
                            StartLine = lastCompoundItem.EndLine.NextLine;
                        }
                        else
                        if(previousAndNext1.Item1 is not null)
                        {
                            StartLine = previousAndNext1.Item1.StartLine.NextLine;
                        }
                    }
                }
                else
                {
                    StartLine = Plan.GetLineByNumber(2);
                }
            }
            #endregion

            #region 设置最后一个视觉节点的引用
            Tuple<JsonTreeViewItem, JsonTreeViewItem> previousAndNext2 = JsonItemTool.SetLineNumbersForEachSubItem(childrenDataList.Result, StartLine.LineNumber + 1, this);

            VisualLastChild = null;
            if (previousAndNext2 is not null && previousAndNext2.Item2 is not null)
            {
                VisualLastChild = previousAndNext2.Item2;
            }
            else
            {
                VisualLastChild = this;
            }
            #endregion

            #region 设置当前复合节点的行末引用
            if (isNeedParentHead)
            {
                if (VisualLastChild is BaseCompoundJsonTreeViewItem baseCompoundJsonTreeViewItem && baseCompoundJsonTreeViewItem.EndLine is not null && !baseCompoundJsonTreeViewItem.EndLine.IsDeleted)
                {
                    EndLine = baseCompoundJsonTreeViewItem.EndLine.NextLine;
                }
                else
                if (VisualLastChild is not null)
                {
                    EndLine = VisualLastChild.StartLine.NextLine;
                }
                else
                {
                    EndLine = StartLine;
                }
            }
            #endregion

            #endregion
        }

        public void InsertChild(int targetIndex,int customItemCount, JsonTreeViewDataStructure childData, bool isExternNeedStartComma = false, bool isExternNeedEndComma = false)
        {
            #region 处理视觉树

            #region Field
            BaseCompoundJsonTreeViewItem targetItem = this;
            if(ItemType is ItemType.BottomButton || ItemType is ItemType.CustomCompound)
            {
                targetItem = Parent;
            }
            if (targetIndex < 0 && targetIndex >= LogicChildren.Count)
            {
                return;
            }
            int offset = 0;
            string appendString = "";
            char startBracket = ' ';
            char endBracket = ' ';
            bool isNeedParentHead = targetItem.ItemType is not ItemType.BottomButton && StartLine is null && (targetItem.LogicChildren.Count == 0 || (targetItem.LogicChildren.Count == 1 && targetItem.LogicChildren[0] is BaseCompoundJsonTreeViewItem firstCompoundItem && firstCompoundItem.ItemType is ItemType.BottomButton));
            bool isNeedStartComma = childData.ResultString.Length > 0 && ((isNeedParentHead && VisualPrevious is not null && VisualNext is null) || (!isNeedParentHead && targetItem.VisualLastChild is not null && ((targetIndex - customItemCount > targetItem.VisualLastChild.Index && targetIndex > 0) || (targetItem.LogicChildren.Count == 1 && targetItem.LogicChildren[0] is BaseCompoundJsonTreeViewItem firstEnumItem && (firstEnumItem.EnumKey.Length > 0 || firstEnumItem.IsEnumBranch)))) || (ItemType is ItemType.BottomButton) || isExternNeedStartComma);
            bool isNeedEndComma = (targetItem.VisualLastChild is not null && (targetIndex <= targetItem.VisualLastChild.Index || targetItem.LogicChildren[0] is BaseCompoundJsonTreeViewItem customItem && customItem.ItemType is ItemType.CustomCompound)) || (isNeedParentHead && VisualNext is not null/* && Parent is not null && Parent.StartLine != Parent.EndLine*/) || isExternNeedEndComma;
            #endregion

            #region 拼接当前子信息应用于代码编辑器

            #region 分别处理尾部和头部的添加
            if(ItemType is not ItemType.BottomButton)
            {
                if(isNeedParentHead)
                {
                    if(VisualPrevious is BaseCompoundJsonTreeViewItem visualPreviousItem && visualPreviousItem.EndLine is not null && !visualPreviousItem.EndLine.IsDeleted)
                    {
                        offset = visualPreviousItem.EndLine.EndOffset;
                    }
                    else
                    if(VisualPrevious is not null && VisualPrevious.StartLine is not null)
                    {
                        offset = VisualPrevious.StartLine.EndOffset;
                    }
                    else
                    {
                        string parentStartLineString = Plan.GetRangeText(Parent.StartLine.Offset,Parent.StartLine.Length);
                        int index = 0;
                        index = parentStartLineString.IndexOf('{');
                        if (index == -1)
                        {
                            index = parentStartLineString.IndexOf('[');
                        }
                        offset = Parent.StartLine.Offset + index + 1;
                    }
                }
                else
                {
                    DocumentLine documentLine = null;

                    //树结构的插入需要按索引来，但文本的插入需要在上一个节点的末尾
                    if (targetIndex > 0 && targetIndex <= targetItem.LogicChildren.Count && targetItem.LogicChildren.Count > 0)
                    {
                        if (targetItem.LogicChildren[targetIndex - 1] is BaseCompoundJsonTreeViewItem baseCompoundJsonTreeViewItem && baseCompoundJsonTreeViewItem.EndLine is not null && !baseCompoundJsonTreeViewItem.EndLine.IsDeleted)
                        {
                            documentLine = baseCompoundJsonTreeViewItem.EndLine;
                        }
                        else
                        {
                            documentLine = targetItem.LogicChildren[targetIndex - 1].StartLine;
                        }
                    }
                    else
                    {
                        documentLine = StartLine;
                    }

                    if (documentLine is null || StartLine == EndLine)
                    {
                        string currentStartlineString = Plan.GetRangeText(StartLine.Offset,StartLine.Length);
                        if (Key.Length > 0)
                        {
                            offset = StartLine.Offset + currentStartlineString.LastIndexOf(':') + 3;
                        }
                        else
                        {
                            int index = currentStartlineString.IndexOf('[');
                            if (index == -1)
                            {
                                index = currentStartlineString.IndexOf('{');
                            }
                            offset = StartLine.Offset + index + 1;
                        }
                    }
                    else
                    {
                        offset = documentLine.EndOffset;
                    }
                }
            }
            else
            {
                if(Parent.VisualLastChild is BaseCompoundJsonTreeViewItem visualLastCompoundItem && visualLastCompoundItem.EndLine is not null && !visualLastCompoundItem.EndLine.IsDeleted)
                {
                    offset = visualLastCompoundItem.EndLine.EndOffset;
                }
                else
                if(Parent.VisualLastChild is not null && Parent.VisualLastChild.StartLine is not null)
                {
                    offset = Parent.VisualLastChild.StartLine.EndOffset;
                }
            }
            #endregion

            #region 组织最终替换的json字符串并应用
            if (isNeedParentHead)
            {
                if (ItemType is ItemType.Array || ItemType is ItemType.List)
                {
                    startBracket = '[';
                    endBracket = ']';
                }
                else
                {
                    startBracket = '{';
                    endBracket = '}';
                }
                string keyString = Key.Length > 0 ? "\"" + Key + "\": " : "";
                appendString += (isNeedStartComma ? ',' : "") + "\r\n" + new string(' ', LayerCount * 2) + keyString + startBracket + (childData.ResultString.Length > 0 ? "\r\n" + childData.ResultString.ToString() + "\r\n" + new string(' ', LayerCount * 2) : "") + endBracket + (isNeedEndComma ? ',' : "") + (!isNeedEndComma && (Parent is not null && (Parent.EndLine is null || (Parent.EndLine is not null && Parent.EndLine.IsDeleted) || Parent.StartLine == Parent.EndLine)) ? "\r\n" + new string(' ', Parent.LayerCount * 2) : "");
            }
            else
            {
                if (childData.ResultString.Length > 0)
                {
                    appendString += (isNeedStartComma ? ',' : "") + "\r\n" + childData.ResultString.ToString() + (isNeedEndComma ? ',' : "") + (targetItem.VisualLastChild is null ? "\r\n" + new string(' ', targetItem.LayerCount * 2) : "");
                }
            }

            if (offset >= 0 && appendString.Length > 0)
            {
                Plan.SetRangeText(offset, 0, appendString);
            }
            #endregion

            #endregion

            #endregion

            #region 处理逻辑树

            #region 添加子节点集
            int startIndex = targetIndex + customItemCount;
            foreach (var item in childData.Result)
            {
                targetItem.LogicChildren.Insert(startIndex, item);
                startIndex++;
            }
            #endregion

            #region 设置当前复合节点行首引用
            if (StartLine is null)
            {
                if (VisualPrevious is not null)
                {
                    if (VisualPrevious is BaseCompoundJsonTreeViewItem previousCompoundItem && previousCompoundItem.EndLine is not null && !previousCompoundItem.EndLine.IsDeleted)
                    {
                        StartLine = previousCompoundItem.EndLine.NextLine;
                    }
                    else
                    {
                        StartLine = VisualPrevious.StartLine.NextLine;
                    }
                }
                else
                if (Parent is not null && ItemType is not ItemType.BottomButton)
                {
                    StartLine = Parent.StartLine.NextLine;
                }
                else
                if (ItemType is not ItemType.BottomButton)
                {
                    StartLine = Plan.GetLineByNumber(2);
                }
            }
            #endregion

            #region 处理父子关系及当前插入成员的行引用

            #region 处理层级关系，设置定当前节点的最后一个子节点
            JsonItemTool.SetParentForEachItem(targetItem.LogicChildren, targetItem);
            targetItem.VisualLastChild = null;
            Tuple<JsonTreeViewItem, JsonTreeViewItem> previousAndNext = SearchVisualPreviousAndNextItem(childData.Result[^1],false);
            if (previousAndNext is not null && previousAndNext.Item2 is not null)
            {
                targetItem.VisualLastChild = previousAndNext.Item2;
            }
            else
            if (!childData.Result[^1].IsCanBeDefaulted)
            {
                targetItem.VisualLastChild = childData.Result[^1];
            }
            #endregion

            #region 逐层设置行引用
            DocumentLine targetLine = childData.Result[0].VisualPrevious?.StartLine;
            if (childData.Result[0].VisualPrevious is BaseCompoundJsonTreeViewItem visualPreviousCompoundItem && visualPreviousCompoundItem.EndLine is not null && !visualPreviousCompoundItem.EndLine.IsDeleted)
            {
                targetLine = visualPreviousCompoundItem.EndLine;
            }
            if (targetLine is not null)
            {
                JsonItemTool.SetLineNumbersForEachSubItem(childData.Result, targetLine.LineNumber + 1, this);
            }
            else
            {
                JsonItemTool.SetLineNumbersForEachSubItem(childData.Result,targetItem);
            }
            #endregion

            #endregion

            #region 设置当前复合节点的行末引用
            if (EndLine is null || EndLine == StartLine || (EndLine is not null && EndLine.IsDeleted))
            {
                if (VisualLastChild is BaseCompoundJsonTreeViewItem baseCompoundJsonTreeViewItem2 && baseCompoundJsonTreeViewItem2.EndLine is not null && !baseCompoundJsonTreeViewItem2.EndLine.IsDeleted)
                {
                    EndLine = baseCompoundJsonTreeViewItem2.EndLine.NextLine;
                }
                else
                if (VisualLastChild is not null && VisualLastChild.StartLine is not null)
                {
                    EndLine = VisualLastChild.StartLine.NextLine;
                }
            }
            #endregion
            
            #endregion
        }

        /// <summary>
        /// 搜索分支末端
        /// </summary>
        /// <returns></returns>
        private int SearchEndOfBranch(List<JsonTreeViewItem> children)
        {
            int result = children.Count - 1;
            int index = children.Count - 1;
            while (result < children.Count && index >= 0 && index < children.Count)
            {
                if (children[index] is BaseCompoundJsonTreeViewItem baseCompoundJsonTreeViewItem && baseCompoundJsonTreeViewItem.EnumKey.Length > 0)
                {
                    result = baseCompoundJsonTreeViewItem.Index + baseCompoundJsonTreeViewItem.EnumItemCount;
                    break;
                }
                index--;
            }
            return result;
        }

        /// <summary>
        /// 删除上一个选择的分支
        /// </summary>
        private void RemoveLastEnumBranch()
        {
            if (Parent is not null && Parent.DisplayText != "Root")
            {
                int startIndex = Index + 1;
                int endIndex = SearchEndOfBranch([.. Parent.LogicChildren]) + 1;

                if (startIndex < endIndex)
                {
                    Parent.RemoveChild([.. Parent.LogicChildren.ToList()[startIndex..endIndex]]);
                }
                else
                {
                    Parent.RemoveChild(Parent.LogicChildren.ToList()[1..]);
                }
            }
            else
            if (Plan is BaseCustomWorldUnifiedPlan basePlan)
            {
                int startIndex = Index + 1;
                int endIndex = SearchEndOfBranch([.. basePlan.TreeViewItemList]) + 1;

                if (startIndex < endIndex)
                {
                    basePlan.RemoveChild([.. basePlan.TreeViewItemList.ToList()[startIndex..endIndex]]);
                }
                else
                {
                    basePlan.RemoveChild(basePlan.TreeViewItemList.ToList()[1..]);
                }
            }
        }

        /// <summary>
        /// 用于删除单个节点
        /// </summary>
        /// <param name="childrenList"></param>
        public void RemoveChild(List<JsonTreeViewItem> childrenList,bool isNeedRemove = true)
        {
            #region Field
            if (childrenList.Count == 0)
            {
                return;
            }

            JsonTreeViewItem groupLastItem = SearchEnumGroupLastVisualItem([..LogicChildren]);
            int offset = 0, length = 0;
            #endregion

            #region 确定替换的起始偏移
            if (childrenList[0].VisualPrevious is BaseCompoundJsonTreeViewItem visualPreviousCompoundItem && visualPreviousCompoundItem.EndLine is not null && !visualPreviousCompoundItem.EndLine.IsDeleted)
            {
                offset = visualPreviousCompoundItem.EndLine.EndOffset - (childrenList[0].VisualNext is null && (!childrenList[0].IsCanBeDefaulted || childrenList[0].VisualPrevious is not null) ? 1 : 0);
            }
            else
            if (childrenList[0].VisualPrevious is not null)
            {
                offset = childrenList[0].VisualPrevious.StartLine.EndOffset - (childrenList[0].VisualNext is null && (!childrenList[0].IsCanBeDefaulted || childrenList[0].VisualPrevious is not null) ? 1 : 0);
            }
            else
            {
                offset = StartLine.EndOffset;
            }
            #endregion

            #region 确定替换的长度
            if (groupLastItem is not null)
            {
                if (groupLastItem == childrenList[^1] && childrenList[^1].VisualPrevious is null)
                {
                    string currentEndLineString = Plan.GetRangeText(EndLine.Offset, EndLine.Length);
                    int index = currentEndLineString.IndexOf('}');
                    if (index == -1)
                    {
                        index = currentEndLineString.IndexOf(']');
                    }
                    length = EndLine.Offset + index - offset;
                }
                else
                {
                    if(groupLastItem is BaseCompoundJsonTreeViewItem baseCompoundJsonTreeViewItem && baseCompoundJsonTreeViewItem.EndLine is not null && !baseCompoundJsonTreeViewItem.EndLine.IsDeleted)
                    {
                        length = baseCompoundJsonTreeViewItem.EndLine.EndOffset - offset;
                    }
                    else
                    {
                        length = groupLastItem.StartLine.EndOffset - offset;
                    }
                }
            }
            #endregion

            #region 执行替换并删除节点
            if (groupLastItem is not null)
            {
                Plan.SetRangeText(offset, length, "");
            }
            foreach (var item in childrenList)
            {
                if(item.IsCanBeDefaulted)
                {
                    item.StartLine = null;
                }

                if (isNeedRemove)
                {
                    LogicChildren.Remove(item);
                }

                if (item is BaseCompoundJsonTreeViewItem baseCompoundJsonTreeViewItem)
                {
                    baseCompoundJsonTreeViewItem.EndLine = null;
                    baseCompoundJsonTreeViewItem.LogicChildren.Clear();
                    baseCompoundJsonTreeViewItem.VisualLastChild = null;
                }
            }

            if(LogicChildren.Count == 1 && LogicChildren[0] is BaseCompoundJsonTreeViewItem lastItem && (lastItem.ItemType is ItemType.BottomButton || lastItem.ItemType is ItemType.CustomCompound))
            {
                if(lastItem.ItemType is ItemType.BottomButton)
                {
                    LogicChildren.RemoveAt(0);
                }
                EndLine = StartLine;
            }

            VisualLastChild = null;
            if (LogicChildren.Count > 0)
            {
                JsonItemTool.SetParentForEachItem(LogicChildren,this);
                Tuple<JsonTreeViewItem, JsonTreeViewItem> previousAndNext = JsonItemTool.SetLineNumbersForEachSubItem(LogicChildren,this);
                if (previousAndNext is not null && previousAndNext.Item2 is not null && (previousAndNext.Item2 is not BaseCompoundJsonTreeViewItem || (previousAndNext.Item2 is BaseCompoundJsonTreeViewItem nextItem && nextItem.ItemType is not ItemType.BottomButton && nextItem.ItemType is not ItemType.CustomCompound)))
                {
                    VisualLastChild = previousAndNext.Item2;
                }
                else
                {
                    EndLine = StartLine;
                }
            }
            #endregion
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
            if (this is BaseCompoundJsonTreeViewItem compoundJsonTreeViewItem)
            {
                if (!IsCanBeDefaulted || compoundJsonTreeViewItem.LogicChildren.Count > 0)
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
        /// 处理数据类型的变更
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void ValueType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string currentValueTypeString = SelectedValueType.Text.ToLower();
            if (currentValueTypeString.Length > 0)
            {
                #region Field
                bool isSubItemSameDataType = false;
                int customItemCount = 0;
                IsNotValueTypeChanging = false;
                EnumItemsSource.Clear();
                IsNotValueTypeChanging = true;
                int offset = 0, length = 0;
                string startLineText = "";
                string ResultString = "";
                string oldResultString = "";
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

                #region 将当前文本值清除，处理子节点

                #region 当Compound切换为List时，转移已有的子节点
                List<JsonTreeViewItem> cacheItemList = [];
                string cacheString = "";
                //if(ListChildrenStringList.Count > 0 && (ItemType is ItemType.List || (SelectedValueType is not null && SelectedValueType.Text == "List")))
                //{
                //    List<string> NBTFeatureList = htmlHelper.GetHeadTypeAndKeyList(ListChildrenStringList[0]);
                //    NBTFeatureList = HtmlHelper.RemoveUIMarker(NBTFeatureList);
                //    isSubItemSameDataType = NBTFeatureList.Contains("list");
                //}

                //if (isSubItemSameDataType)
                //{
                //    if (LogicChildren[0] is BaseCompoundJsonTreeViewItem firstListItem && firstListItem.LogicChildren.Count > 0)
                //    {
                //        cacheItemList = [.. firstListItem.LogicChildren];
                //    }
                //}
                //else
                //if (LogicChildren.Count > 0)
                //{
                //    cacheItemList = [.. LogicChildren];
                //}

                LogicChildren.Clear();

                //if (cacheItemList.Count > 0)
                //{
                //    if (cacheItemList[0].DisplayText == "Entry")
                //    {
                //        BaseCompoundJsonTreeViewItem entryItem = cacheItemList[0] as BaseCompoundJsonTreeViewItem;
                //        oldOffset = entryItem.StartLine.Offset;
                //        if (entryItem.EndLine is not null)
                //        {
                //            oldLength = entryItem.EndLine.EndOffset - oldOffset;
                //        }
                //        else
                //        {
                //            oldLength = entryItem.StartLine.EndOffset - oldOffset;
                //        }
                //        oldResultString = Plan.GetRangeText(oldOffset, oldLength);
                //        string spaceString = (LayerCount > 0 ? new string(' ', LayerCount * 2) : "  ");
                //        oldResultString = spaceString + oldResultString.Replace("\r\n  ", "\r\n").TrimStart(['{', '\r', '\n', ' ']).TrimEnd(['}', '\r', '\n', ' ']) + spaceString;
                //    }
                //    else
                //    if(isSubItemSameDataType)
                //    {

                //    }
                //}
                #endregion

                #region 计算偏移
                int minusOffset = currentValueTypeString == "- unset -" && (VisualNext is null || (VisualNext is not null && VisualNext.StartLine is null)) ? 1 : 0;

                if (currentValueTypeString == "- unset -" || StartLine is null)
                {
                    if (VisualPrevious is BaseCompoundJsonTreeViewItem previousCompoundItem && previousCompoundItem.EndLine is not null)
                    {
                        offset = previousCompoundItem.EndLine.EndOffset - minusOffset;
                    }
                    else
                    if (VisualPrevious is not null && VisualPrevious.StartLine is not null)
                    {
                        offset = VisualPrevious.StartLine.EndOffset - minusOffset;
                    }
                    else
                    if (Parent is not null)
                    {
                        startLineText = Plan.GetRangeText(Parent.StartLine.Offset, Parent.StartLine.Length);
                        if (Parent.Key.Length > 0)
                        {
                            offset = Parent.StartLine.Offset + (Parent.Key.Contains(':') ? startLineText.LastIndexOf(':') : startLineText.IndexOf(':')) + 3;
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
                #endregion

                #region 计算长度
                if (Parent is not null && VisualPrevious is null && VisualNext is null && currentValueTypeString == "- unset -")
                {
                    string parentEndLineString = Plan.GetRangeText(Parent.EndLine.Offset, Parent.EndLine.Length);
                    int index = parentEndLineString.IndexOf('}');
                    if (index == -1)
                    {
                        index = parentEndLineString.IndexOf(']');
                    }
                    length = Parent.EndLine.Offset + index - offset;
                }
                else
                if (EndLine is not null && !EndLine.IsDeleted)
                {
                    length = EndLine.EndOffset - offset;
                }
                else
                if (StartLine is not null && !StartLine.IsDeleted)
                {
                    length = StartLine.EndOffset - offset;
                }
                #endregion

                #region 执行替换
                if (StartLine is not null)
                {
                    cacheString = Plan.GetRangeText(offset, length);
                    Plan.SetRangeText(offset, length, "");
                }
                if (currentValueTypeString == "- unset -")
                {
                    DataType = DataType.None;
                    ItemType = ItemType.MultiType;
                    ValueTypeBoxVisibility = Visibility.Visible;
                    StartLine = EndLine = null;
                }
                length = 0;
                #endregion

                #endregion

                #region 判断是在添加枚举型结构还是展开复合节点
                bool addMoreCustomStructure = ItemType is ItemType.CustomCompound && Parent is not null && customItemCount > 0;

                bool addMoreStructure = ItemType is ItemType.OptionalCompound || ItemType is ItemType.Compound;

                bool addListStructure = ItemType is ItemType.List;

                bool addParentListStructure = ItemType is not ItemType.List && Parent is not null && Parent.ItemType is ItemType.List;
                #endregion

                #region 计算有多少个自定义子节点
                if (Parent is not null && Parent.LogicChildren.Count > 0 && Parent.LogicChildren[0] is BaseCompoundJsonTreeViewItem subCompoundItem1 && subCompoundItem1.ItemType is ItemType.CustomCompound)
                {
                    customItemCount = 1;
                }

                if (Parent is not null && Parent.LogicChildren.Count > 1 && Parent.LogicChildren[1] is BaseCompoundJsonTreeViewItem subCompoundItem2 && subCompoundItem2.ItemType is ItemType.CustomCompound)
                {
                    customItemCount = 2;
                }
                #endregion

                #region 定义前置与后置连接符
                string connectorSymbol = "";
                string endConnectorSymbol = "";
                #endregion

                #region 计算前置衔接符
                if (VisualPrevious is not null && VisualNext is null && StartLine is null)
                {
                    connectorSymbol += ",";
                }

                if ((VisualPrevious is not null && VisualNext is null && StartLine is null) || (Parent is not null && (Parent.EndLine == Parent.StartLine || Parent.EndLine is null || (Parent.EndLine is not null && Parent.EndLine.IsDeleted))) || StartLine is null)
                {
                    connectorSymbol += "\r\n";
                }
                if (StartLine is null)
                {
                    connectorSymbol += new string(' ', LayerCount * 2);
                }
                #endregion

                #region 计算后置衔接符
                BaseCompoundJsonTreeViewItem endConnectorSymbolItem = null;
                if (addMoreCustomStructure || addParentListStructure)
                {
                    endConnectorSymbolItem = Parent;
                }
                else
                if (addMoreStructure || addListStructure)
                {
                    endConnectorSymbolItem = this;
                }

                if (endConnectorSymbolItem is not null && ((addMoreCustomStructure && endConnectorSymbolItem.LogicChildren.Count - customItemCount > 0) || (addMoreStructure && VisualNext is not null && VisualNext.StartLine is not null)))
                {
                    endConnectorSymbol = ",";
                }

                if (addListStructure && IsCanBeDefaulted && ((VisualNext is not null && VisualNext.StartLine is not null && ((StartLine is null && EndLine is null) || StartLine == EndLine)) || LogicChildren.Count > 1))
                {
                    endConnectorSymbol = ",";
                }

                if (VisualNext is not null || (LogicNext is not null && LogicNext.StartLine is not null))
                {
                    endConnectorSymbol = ",";
                }
                if (Parent is not null && Parent.StartLine is not null && (Parent.EndLine is null || (Parent.EndLine is not null && Parent.EndLine.IsDeleted) || Parent.EndLine == Parent.StartLine || (VisualPrevious is null && VisualNext is null && StartLine is null)))
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
                                Value = false;
                            }
                            DataType = DataType.Bool;
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
                            string currentValue = "\"\"";

                            if (currentValueTypeString == "string")
                            {
                                DataType = DataType.String;
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
                                if (contextMatch.Success)
                                {
                                    EnumItemsSource.Add(new TextComboBoxItem()
                                    {
                                        Text = "- unset -"
                                    });
                                    if (Plan.EnumIDDictionary.TryGetValue(contextMatch.Groups[1].Value, out List<string> targetEnumIDList))
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
                                DataType = DataType.Number;
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
                            InputBoxVisibility = Visibility.Visible;
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
                    bool isPartialData = false;
                    string bracketPairString = "{}";
                    List<string> targetRawStringList = [];
                    List<string> targetList = [];
                    AddOrSwitchElementButtonVisibility = Visibility.Collapsed;
                    #endregion

                    #region 管理枚举、外层括号、控件显示
                    if (CompoundChildrenStringList.Count > 0 && currentValueTypeString != "list")
                    {
                        targetRawStringList = [.. CompoundChildrenStringList];
                        if (currentValueTypeString.Contains("array"))
                        {
                            bracketPairString = "[]";
                        }
                    }
                    else
                    {
                        Dictionary<string, List<string>> targetDictionary = [];
                        Tuple<List<string>, bool> subDataAndPartialInfo = JsonItemTool.ExtractSubInformationFromPromptSourceCode(this);
                        targetRawStringList = [..subDataAndPartialInfo.Item1];
                        isPartialData = subDataAndPartialInfo.Item2;

                        #region 根据当前数据类型执行对应的操作
                        if (currentValueTypeString == "compound")
                        {
                            bracketPairString = "{}";
                            AddOrSwitchElementButtonVisibility = Visibility.Visible;
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

                        if (targetRawStringList.Count == 0)
                        {
                            if (ListChildrenStringList.Count > 0)
                            {
                                targetRawStringList = [.. ListChildrenStringList];
                            }
                            else
                            if (ArrayChildrenStringList.Count > 0)
                            {
                                targetRawStringList = [.. ArrayChildrenStringList];
                            }
                        }
                        #endregion
                    }
                    #endregion

                    #region 执行分析，获取结果
                    JsonTreeViewDataStructure result = new();
                    if (currentValueTypeString != "list")
                    {
                        //将列表状态下的第一个复合元素的子级移动到当前节点下
                        if (oldSelectedValueTypeItem is not null && oldSelectedValueTypeItem.Text.Equals("list", StringComparison.CurrentCultureIgnoreCase) && cacheItemList.Count > 0)
                        {
                            BaseCompoundJsonTreeViewItem entryItem = cacheItemList[0] as BaseCompoundJsonTreeViewItem;
                            AddChildrenList(new() { Result = [entryItem], ResultString = new(oldResultString) });
                        }
                        else
                        {
                            result = htmlHelper.GetTreeViewItemResult(new(), targetRawStringList, LayerCount + 1, "", this, null, 1, true);
                            while (result.ResultString.Length > 0 && (result.ResultString[^1] == ',' ||
                                result.ResultString[^1] == '\r' ||
                                result.ResultString[^1] == '\n'))
                            {
                                result.ResultString.Length--;
                            }

                            htmlHelper.HandlingTheTypingAppearanceOfCompositeItemList([..result.Result], this);
                            for (int i = 0; i < result.Result.Count; i++)
                            {
                                result.Result[i].RemoveElementButtonVisibility = Visibility.Collapsed;
                            }
                            LogicChildren.AddRange(result.Result);
                        }
                    }
                    else//把复合状态下的子节点移给列表在状态下
                    if (cacheItemList is not null && cacheItemList.Count > 0 && !(CompoundChildrenStringList.Count > 0 && ListChildrenStringList.Count > 0) && !isPartialData && !isSubItemSameDataType)
                    {
                        BaseCompoundJsonTreeViewItem addToBottom = new(Plan, JsonItemTool, _container)
                        {
                            DataType = DataType.None,
                            RemoveElementButtonVisibility = Visibility.Collapsed,
                            AddOrSwitchElementButtonVisibility = Visibility.Visible,
                            ElementButtonTip = "添加到尾部",
                            Parent = this
                        };
                        BaseCompoundJsonTreeViewItem entry = new(Plan, JsonItemTool, _container)
                        {
                            ItemType = ItemType.Compound,
                            RemoveElementButtonVisibility = Visibility.Visible,
                            DisplayText = "Entry",
                            Parent = this
                        };

                        addToBottom.SwitchButtonIcon = PlusIcon;
                        addToBottom.SwitchButtonColor = PlusColor;
                        addToBottom.PressedSwitchButtonColor = PressedPlusColor;

                        if (Parent is not null)
                        {
                            addToBottom.LayerCount = entry.LayerCount = Parent.LayerCount + 1;
                        }
                        else
                        {
                            addToBottom.LayerCount = entry.LayerCount = 1;
                        }
                        entry.LogicChildren.AddRange(cacheItemList);
                        foreach (var item in entry.LogicChildren)
                        {
                            item.Parent = entry;
                        }

                        if (entry.LogicChildren.Count > 0)
                        {
                            JsonItemTool.SetLayerCountForEachItem(entry.LogicChildren, entry.LayerCount + 1);
                            LogicChildren.Add(entry);
                            LogicChildren.Add(addToBottom);
                        }
                        cacheString = cacheString.Replace("\r\n", "\r\n  ");
                        result.ResultString = new(new string(' ', (LayerCount + 1) * 2) + cacheString);
                    }
                    currentItemReference.IsExpanded = true;
                    #endregion

                    #region 更新代码编辑器
                    ResultString = result.ResultString.ToString();

                    ResultString = connectorSymbol + (Key.Length > 0 ? "\"" + Key + "\": " : "") + bracketPairString[0] + (ResultString.Length > 0 ? "\r\n" + ResultString + "\r\n" + new string(' ', LayerCount * 2) : "") + bracketPairString[1] + endConnectorSymbol;
                    if ((offset > 0 && ResultString.Length > 0) || DisplayText == "Root")
                    {
                        Plan.SetRangeText(offset, 0, ResultString);
                    }
                    if (LogicChildren.Count > 0 && DisplayText == "Root")
                    {
                        LogicChildren[0].StartLine = StartLine.NextLine;
                        JsonTreeViewItem lastItem = null;
                        if (LogicChildren.Count > 0 && LogicChildren[0].DisplayText == "Entry")
                        {
                            BaseCompoundJsonTreeViewItem compoundJsonTreeViewItem = LogicChildren[0] as BaseCompoundJsonTreeViewItem;
                            JsonItemTool.SetLineNumbersForEachSubItem(compoundJsonTreeViewItem.LogicChildren, compoundJsonTreeViewItem);
                            BaseCompoundJsonTreeViewItem entryItem = LogicChildren[0] as BaseCompoundJsonTreeViewItem;
                            lastItem = entryItem.VisualLastChild;
                            if (lastItem is BaseCompoundJsonTreeViewItem lastCompoundItem && lastCompoundItem.EndLine is not null)
                            {
                                entryItem.EndLine = lastCompoundItem.EndLine.NextLine;
                            }
                            else
                            {
                                entryItem.EndLine = lastItem.StartLine.NextLine;
                            }
                        }
                    }
                    #endregion
                }
                #endregion

                #endregion

                #region 为每个子节点设置所需字段值
                EndLine = null;
                if (currentValueTypeString != "- unset -")
                {
                    if (VisualPrevious is BaseCompoundJsonTreeViewItem previousItem1 && previousItem1.EndLine is not null)
                    {
                        StartLine = previousItem1.EndLine.NextLine;
                    }
                    else
                    if (VisualPrevious is not null && VisualPrevious.StartLine is not null)
                    {
                        StartLine = VisualPrevious.StartLine.NextLine;
                    }
                    else
                    if (Parent is not null)
                    {
                        StartLine = Parent.StartLine.NextLine;
                    }

                    VisualLastChild = null;
                    if (currentValueTypeString == "compound" || currentValueTypeString.Contains("array"))
                    {
                        JsonItemTool.SetParentForEachItem(LogicChildren, this);
                        JsonItemTool.SetLayerCountForEachItem(LogicChildren, LayerCount + 1);
                        JsonItemTool.SetLineNumbersForEachSubItem(LogicChildren, this);
                        Tuple<JsonTreeViewItem, JsonTreeViewItem> previousAndNext = SearchVisualPreviousAndNextItem(LogicChildren[^1], false);
                        if (previousAndNext is not null && previousAndNext.Item2 is not null)
                        {
                            VisualLastChild = previousAndNext.Item2;
                        }
                    }

                    if (VisualLastChild is BaseCompoundJsonTreeViewItem lastCompoundItem && lastCompoundItem.EndLine is not null && !lastCompoundItem.EndLine.IsDeleted)
                    {
                        EndLine = lastCompoundItem.EndLine.NextLine;
                    }
                    else
                    if (VisualLastChild is not null && VisualLastChild.StartLine is not null && !VisualLastChild.StartLine.IsDeleted)
                    {
                        EndLine = VisualLastChild.StartLine.NextLine;
                    }
                    EndLine ??= StartLine;

                    if (Parent is not null)
                    {
                        JsonItemTool.SetParentForEachItem(Parent.LogicChildren, Parent);
                        JsonItemTool.SetLineNumbersForEachSubItem(Parent.LogicChildren, Parent);
                        Parent.SetVisualPreviousAndNextForEachItem();
                        Tuple<JsonTreeViewItem, JsonTreeViewItem> previousAndNext = Parent.SearchVisualPreviousAndNextItem(this);
                        if (previousAndNext is not null && previousAndNext.Item2 is not null && Parent.LogicChildren.Contains(previousAndNext.Item2))
                        {
                            Parent.VisualLastChild = previousAndNext.Item2;
                        }
                        else
                        if (SelectedValueType.Text != "- unset -")
                        {
                            Parent.VisualLastChild = this;
                        }
                        else
                        {
                            Parent.VisualLastChild = null;
                        }
                    }
                }

                if (Parent is not null && (Parent.StartLine == Parent.EndLine || Parent.EndLine is null || (Parent.EndLine is not null && Parent.EndLine.IsDeleted)))
                {
                    JsonTreeViewItem parentLastItem = Parent.VisualLastChild;
                    if (parentLastItem is BaseCompoundJsonTreeViewItem parentLastCompoundItem && parentLastCompoundItem.EndLine is not null)
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
                    (VisualPrevious is null || (VisualPrevious is not null && VisualPrevious.StartLine is null)) &&
                    (VisualNext is null || (VisualNext is not null && VisualNext.StartLine is null)))
                {
                    offset = Parent.StartLine.EndOffset;
                    if (Parent.EndLine is not null && !Parent.EndLine.IsDeleted)
                    {
                        string endLineText = Plan.GetRangeText(Parent.EndLine.Offset, Parent.EndLine.Length);
                        int lastCharIndex = endLineText.LastIndexOf('}');
                        if (lastCharIndex == -1)
                        {
                            lastCharIndex = endLineText.LastIndexOf(']');
                        }
                        length = Parent.EndLine.Offset + lastCharIndex - offset;
                        Plan.SetRangeText(offset, length, "");
                    }
                    Parent.EndLine = Parent.StartLine;
                }
                #endregion

                #region 复位并存储类型
                if (StartLine is not null && StartLine.IsDeleted)
                {
                    StartLine = null;
                }
                if (EndLine is not null && EndLine.IsDeleted)
                {
                    EndLine = null;
                }
                oldSelectedValueTypeItem = SelectedValueType;
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
            #region Field
            if (ItemType is ItemType.CustomCompound || EnumItemsSource.Count == 0 || !IsNotValueTypeChanging)
            {
                return;
            }
            bool skipCode = false;
            int index = 0;
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
            #endregion

            #region 替换当前枚举节点的值
            if (StartLine is null)
            {
                string connectorString = (VisualPrevious is not null && VisualNext is null ? "," : "") + "\r\n" + new string(' ', LayerCount * 2);
                string endConnectorString = VisualNext is not null && VisualNext.StartLine is not null ? "," : "";
                if (VisualPrevious is BaseCompoundJsonTreeViewItem previousCompoundItem1 && previousCompoundItem1.EndLine is not null)
                {
                    Plan.SetRangeText(previousCompoundItem1.EndLine.EndOffset,0, connectorString + (Key.Length > 0 ? "\"" + Key + "\": " : "") + "\"" + SelectedEnumItem.Text + "\"" + endConnectorString);
                }
                else
                if (VisualPrevious is not null && VisualPrevious.StartLine is not null)
                {
                    Plan.SetRangeText(VisualPrevious.StartLine.EndOffset,0, connectorString + (Key.Length > 0 ? "\"" + Key + "\": " : "") + "\"" + SelectedEnumItem.Text + "\"" + endConnectorString);
                }
                else
                if (Parent is not null)
                {
                    if ((Parent.StartLine == Parent.EndLine) || Parent.EndLine is null || (Parent.EndLine is not null && Parent.EndLine.IsDeleted))
                    {
                        endConnectorString = "\r\n" + new string(' ', Parent.LayerCount * 2);
                    }
                    DocumentLine parentStartLine = Plan.GetLineByNumber(Parent.StartLine.LineNumber);
                    string parentStartLineText = Plan.GetRangeText(parentStartLine.Offset, parentStartLine.Length);
                    offset = Parent.StartLine.Offset + (Parent.Key.Contains(':') && StartLine is not null ? parentStartLineText.LastIndexOf(':') : parentStartLineText.IndexOf(':')) + 3;
                    Plan.SetRangeText(offset, 0, connectorString + (Key.Length > 0 ? "\"" + Key + "\": " : "") + "\"" + SelectedEnumItem.Text + "\"" + endConnectorString);
                }
                else
                {
                    Plan.SetRangeText(1, 0, connectorString + "\"" + Key + "\": \"" + SelectedEnumItem.Text + "\"" + endConnectorString);
                }

                if(VisualPrevious is BaseCompoundJsonTreeViewItem previousCompoundItem2 && previousCompoundItem2 is not null && previousCompoundItem2.EndLine is not null)
                {
                    StartLine = previousCompoundItem2.EndLine.NextLine;
                }
                else
                if (VisualPrevious is not null && VisualPrevious.StartLine is not null)
                {
                    StartLine = VisualPrevious.StartLine.NextLine;
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
                if(VisualPrevious is not null && VisualPrevious.StartLine is not null)
                {
                    string previousLineString = "";
                    string parentStartLineText = "";
                    string parentEndLineText = "";
                    if (VisualPrevious is BaseCompoundJsonTreeViewItem previousItem && previousItem.EndLine is not null)
                    {
                        offset = previousItem.EndLine.EndOffset;
                        previousLineString = Plan.GetRangeText(previousItem.EndLine.Offset, previousItem.EndLine.Length);
                    }
                    else
                    if(VisualPrevious.StartLine is not null)
                    {
                        offset = VisualPrevious.StartLine.EndOffset;
                        previousLineString = Plan.GetRangeText(VisualPrevious.StartLine.Offset, VisualPrevious.StartLine.Length);
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

                    offset -= VisualNext is null ? 1 : 0;
                    if ((VisualNext is not null && VisualNext.StartLine is not null) || (VisualPrevious is not null && VisualPrevious.StartLine is not null))
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

                    if (VisualNext is null)
                    {
                        parentEndLineText = Plan.GetRangeText(Parent.EndLine.Offset, Parent.EndLine.Length);
                    }
                    int endBracketOffset = parentEndLineText.IndexOf('}');
                    if(endBracketOffset == -1)
                    {
                        endBracketOffset = parentEndLineText.IndexOf(']');
                    }
                    offset = Parent.StartLine.Offset + colonOffset;
                    if (VisualNext is null)
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
                else
                {
                    offset = 1;
                    if(EndLine is not null && !EndLine.IsDeleted)
                    {
                        length = EndLine.EndOffset - offset;
                    }
                    else
                    {
                        length = StartLine.EndOffset - offset;
                    }
                    Plan.SetRangeText(offset, length, "");
                    EndLine = null;
                    if(IsCanBeDefaulted)
                    {
                        StartLine = null;
                    }
                }
            }
            #endregion

            #region 判断是否需要应用枚举结构
            if (EnumItemsSource[0].Text == "- unset -" && !IsCanBeDefaulted)
            {
                EnumItemsSource.RemoveAt(0);
            }
            if (EnumKey.Length > 0 && !skipCode)
            {
                if (Plan.EnumCompoundDataDictionary.TryGetValue(EnumKey, out Dictionary<string, List<string>> targetDependencyDictionary) && targetDependencyDictionary.TryGetValue(SelectedEnumItem.Text, out List<string> targetRawList))
                {
                    RemoveLastEnumBranch();
                    
                    Match firstKeyWordMatch = GetEnumValueMode1().Match(targetRawList[0]);
                    List<string> targetRawListTemp = [.. targetRawList];
                    if(firstKeyWordMatch.Success && firstKeyWordMatch.Groups[1].Value == SelectedEnumItem.Text)
                    {
                        targetRawListTemp.RemoveAt(0);
                    }

                    JsonTreeViewDataStructure result = htmlHelper.GetTreeViewItemResult(new(), targetRawListTemp, LayerCount, "", this, null, 1, true);

                    while (result.ResultString.Length > 1 && 
                        (result.ResultString[^1] == ' ' || 
                        result.ResultString[^1] == ',' || 
                        result.ResultString[^1] == '\r' ||
                        result.ResultString[^1] == '\n'))
                    {
                        result.ResultString.Length--;
                    }

                    htmlHelper.HandlingTheTypingAppearanceOfCompositeItemList([.. result.Result], Parent);

                    ObservableCollection<JsonTreeViewItem> treeViewItemList = [];
                    index = Index + 1;
                    BaseCustomWorldUnifiedPlan basePlan1 = null;
                    if(Plan is BaseCustomWorldUnifiedPlan baseCustomWorldUnifiedPlan)
                    {
                        basePlan1 = baseCustomWorldUnifiedPlan;
                    }
                    if (Parent is not null)
                    {
                        treeViewItemList = Parent.LogicChildren;
                    }
                    else
                    if(basePlan1 is not null)
                    {
                        treeViewItemList = basePlan1.TreeViewItemList;
                    }

                    EnumItemCount = result.Result.Count;

                    for (int i = 0; i < result.Result.Count; i++)
                    {
                        result.Result[i].RemoveElementButtonVisibility = Visibility.Collapsed;
                    }
                    if (Parent is not null)
                    {
                        Parent.InsertChild(index, 0, result, VisualNext is null, VisualNext is not null);
                    }
                    else
                    {
                        basePlan1.InsertChildren(index, result);
                    }

                    if (result.Result.Count > 0)
                    {
                        result.Result[0].LogicPrevious = this;
                        LogicNext = result.Result[0];
                        int insertIndex = result.Result.Count + 1;
                        if (insertIndex < treeViewItemList.Count)
                        {
                            treeViewItemList[insertIndex].LogicPrevious = result.Result[^1];
                            result.Result[^1].LogicNext = treeViewItemList[insertIndex];
                        }
                    }

                    skipCode = true;
                }
            }
            #endregion

            #region 判断是否为分支文档结构
            if (CompoundChildrenStringList.Count > 0 && IsEnumBranch && ItemType is ItemType.Enum && !skipCode)
            {
                List<string> FilteredRawList = [];
                if (SelectedEnumItem.Text != "- unset -")
                {
                    #region Field
                    bool haveCurrentEnum = false;
                    int startIndex = 0, endIndex = 0;
                    int currentStarCount = 0,startStarCount = 0;
                    #endregion

                    #region 提取目标分支源码
                    for (int i = 0; i < CompoundChildrenStringList.Count; i++)
                    {
                        Match targetmatch = GetEnumRawKey().Match(CompoundChildrenStringList[i]);
                        Match starMatch = GetLineStarCount().Match(CompoundChildrenStringList[i]);
                        startStarCount = starMatch.Value.Trim().Length;
                        if (targetmatch.Success && CompoundChildrenStringList[i].Contains(SelectedEnumItem.Text) && !haveCurrentEnum)
                        {
                            haveCurrentEnum = true;
                            startIndex = i + 1;
                            currentStarCount = startStarCount;
                            continue;
                        }

                        if (haveCurrentEnum && GetEnumRawKey().Match(CompoundChildrenStringList[i]).Success && startStarCount == currentStarCount)
                        {
                            endIndex = i;
                            break;
                        }
                        else
                        if(i == CompoundChildrenStringList.Count - 1)
                        {
                            endIndex = i + 1;
                        }
                    }

                    RemoveLastEnumBranch();

                    if (haveCurrentEnum)
                    {
                        if (endIndex > startIndex)
                        {
                            FilteredRawList = CompoundChildrenStringList[startIndex..endIndex];
                        }
                    }
                    #endregion

                    #region 分析提取出的源码并应用结果
                    if (FilteredRawList.Count > 0)
                    {
                        JsonTreeViewDataStructure result = htmlHelper.GetTreeViewItemResult(new(), FilteredRawList, LayerCount, "", this, null, 1, true);

                        index = 0;
                        if (Parent is not null)
                        {
                            index = Parent.LogicChildren.IndexOf(this) + 1;
                        }
                        else
                        if (Plan is BaseCustomWorldUnifiedPlan basePlan1)
                        {
                            index = basePlan1.TreeViewItemList.IndexOf(this) + 1;
                        }
                        EnumItemCount = result.Result.Count;
                        if (Parent is not null)
                        {
                            Parent.InsertChild(index,0, result);
                        }
                        else
                        {
                            BaseCustomWorldUnifiedPlan basePlan1 = Plan as BaseCustomWorldUnifiedPlan;
                            basePlan1.InsertChild(index, result);
                        }

                        #region 关闭当前所有节点的删除功能
                        for (int i = 0; i < result.Result.Count; i++)
                        {
                            result.Result[i].RemoveElementButtonVisibility = Visibility.Collapsed;
                            index++;
                        }
                        #endregion
                    }
                    skipCode = true;
                    #endregion
                }
                else
                {
                    RemoveLastEnumBranch();
                }
            }
            #endregion

            #region 重新配置前后关系
            if (SelectedEnumItem is not null && SelectedEnumItem.Text == "- unset -")
            {
                StartLine = EndLine = null;
            }
            if (Parent is not null)
            {
                JsonItemTool.SetParentForEachItem(Parent.LogicChildren, Parent);
                Parent.SetVisualPreviousAndNextForEachItem();
                Tuple<JsonTreeViewItem, JsonTreeViewItem> previousAndNext = JsonItemTool.SetLineNumbersForEachSubItem(Parent.LogicChildren, Parent, StartLine is not null ? this : null);
                Parent.VisualLastChild = null;
                if(previousAndNext is not null && previousAndNext.Item2 is not null)
                {
                    Parent.VisualLastChild = previousAndNext.Item2;
                    if (Parent.StartLine == Parent.EndLine || Parent.EndLine is null || (Parent.EndLine is not null && Parent.EndLine.IsDeleted))
                    {
                        if (previousAndNext.Item2 is BaseCompoundJsonTreeViewItem baseCompoundJsonTreeViewItem && baseCompoundJsonTreeViewItem.EndLine is not null && !baseCompoundJsonTreeViewItem.EndLine.IsDeleted)
                        {
                            Parent.EndLine = baseCompoundJsonTreeViewItem.EndLine.NextLine;
                        }
                        else
                        if (previousAndNext.Item2 is not null)
                        {
                            Parent.EndLine = previousAndNext.Item2.StartLine.NextLine;
                        }
                    }
                }
            }
            else
            if (Plan is BaseCustomWorldUnifiedPlan basePlan)
            {
                basePlan.SetVisualPreviousAndNextForEachItem();
                JsonItemTool.SetLineNumbersForEachSubItem(basePlan.TreeViewItemList, 2);
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
            if (DataType is not DataType.None && ItemType is not ItemType.CustomCompound && ItemType is not ItemType.List && (ItemType is not ItemType.MultiType || (ItemType is ItemType.MultiType && SelectedValueType is not null && SelectedValueType.Text != "List")))
            {
                IsCurrentExpanded = !IsCurrentExpanded;
            }

            if (IsCurrentExpanded || ItemType is ItemType.CustomCompound || ItemType is  ItemType.Array || ItemType is ItemType.List || (Parent is not null && Parent.ItemType is ItemType.List) || (ItemType is  ItemType.MultiType && SelectedValueType is not null && SelectedValueType.Text == "List"))
            {
                if (currentItemReference is not null)
                {
                    currentItemReference.IsExpanded = true;
                    IsCurrentExpanded = true;
                }
                JsonItemTool.AddSubStructure(this);

                if(ItemType is ItemType.CustomCompound && InputBoxVisibility is Visibility.Visible)
                {
                    Value = "";
                }
                if (ItemType is ItemType.OptionalCompound)
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