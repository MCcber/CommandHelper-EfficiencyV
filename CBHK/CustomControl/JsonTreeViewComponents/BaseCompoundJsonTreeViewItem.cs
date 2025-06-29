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
using System.ComponentModel;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Windows.Security.Cryptography.Certificates;
using Windows.System.RemoteSystems;
using Windows.UI.WebUI;
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
        /// 添加按钮悬浮文本
        /// </summary>
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
                if(_selectedValueType is not null)
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
                        _ => ItemType.BottomButton
                    };
                }
                SetProperty(ref _selectedValueType, value);
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
                if (startIndex < treeViewItemList.Count && treeViewItemList[startIndex].StartLine is not null && treeViewItemList[startIndex] != jsonTreeViewItem)
                {
                    previous = treeViewItemList[startIndex];
                }
            }
            #endregion
            #region 搜索视觉上的后一个节点
            startIndex = treeViewItemList.Count - 1;
            while (startIndex >= 0 && ((treeViewItemList[startIndex] is not BaseCompoundJsonTreeViewItem && treeViewItemList[startIndex].StartLine is null) || (treeViewItemList[startIndex] is BaseCompoundJsonTreeViewItem baseCompoundJsonTreeViewItem2 && baseCompoundJsonTreeViewItem2.ItemType is ItemType.BottomButton)))
            {
                startIndex--;
            }
            next = treeViewItemList[startIndex];
            return new(previous, next);
            #endregion
        }

        /// <summary>
        /// 为指定节点集合的所有成员设置各自视觉上的前一个与后一个节点引用(二分查找最近邻居算法)
        /// </summary>
        /// <param name="treeViewItemList"></param>
        public void SetVisualPreviousAndNextForEachItem()
        {
            if (LogicChildren == null || LogicChildren.Count == 0)
            {
                return;
            }

            // 步骤1: 收集必选节点
            var requiredNodes = LogicChildren.Where(item => (item is not BaseCompoundJsonTreeViewItem || (item is BaseCompoundJsonTreeViewItem baseItem && baseItem.ItemType is not ItemType.BottomButton)) && (!item.IsCanBeDefaulted || item.StartLine is not null)).ToList();
            int requiredCount = requiredNodes.Count;

            // 没有必选节点时的处理
            if (requiredCount == 0)
            {
                foreach (var node in LogicChildren)
                {
                    node.VisualPrevious = null;
                    node.VisualNext = null;
                }
                return;
            }

            // 步骤2: 提取索引列表
            var requiredIndices = requiredNodes.Select(r => r.Index).ToList();

            // 步骤3: 处理每个节点
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
                    node.VisualPrevious = (position > 0)
                        ? requiredNodes[position - 1]
                        : null;

                    // 后一个必选节点 (索引+1)
                    node.VisualNext = (position < requiredCount - 1)
                        ? requiredNodes[position + 1]
                        : null;
                }
                else
                {
                    // 当前节点是非必选节点
                    // 计算插入位置 (~position)
                    int insertPosition = ~position;

                    // 左侧最近的必选节点 (插入位置-1)
                    node.VisualPrevious = (insertPosition > 0)
                        ? requiredNodes[insertPosition - 1]
                        : null;

                    // 右侧最近的必选节点 (插入位置)
                    node.VisualNext = (insertPosition < requiredCount)
                        ? requiredNodes[insertPosition]
                        : null;
                }
            }
        }

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
            bool isNeedStartComma = childrenDataList.ResultString.Length > 0 && VisualPrevious is not null && VisualNext is null;
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
                if (Parent is not null)
                {
                    currentStartLineString = Plan.GetRangeText(Parent.StartLine.Offset, Parent.StartLine.EndOffset - Parent.StartLine.Offset);
                }
                if ((Key.Length > 0 || ItemType is ItemType.CustomCompound) && currentStartLineString != "{}")
                {
                    int colonIndex = currentStartLineString.LastIndexOf(':');
                    offset = Parent.StartLine.Offset + colonIndex + 3;
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

            if (isNeedParentHead)
            {
                appendString += (isNeedStartComma ? ',' : "") + "\r\n" + new string(' ', LayerCount * 2) + keyString + startBracketChar.ToString().Trim();
            }
            appendString += (childrenDataList.ResultString.Length > 0 ? "\r\n" + childrenDataList.ResultString.ToString() : "") + (childrenDataList.ResultString.Length > 0 ? "\r\n" + new string(' ', LayerCount * 2) : "") + (endBracketChar.ToString().Length > 0 ? endBracketChar.ToString().Trim() : "") + (isNeedEndComma ? ',' : "");
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
                Tuple<JsonTreeViewItem,JsonTreeViewItem> previousAndNext = JsonItemTool.SetLineNumbersForEachSubItem(Parent.LogicChildren, Parent);
                if(previousAndNext is not null)
                {
                    VisualLastChild = previousAndNext.Item2;
                }
                JsonItemTool.SetParentForEachItem(Parent.LogicChildren, Parent);
                SetVisualPreviousAndNextForEachItem();
            }
            else
            if(Plan is BaseCustomWorldUnifiedPlan basePlan)
            {
                int lineNumber = 2;
                if(VisualPrevious is not null && VisualPrevious is BaseCompoundJsonTreeViewItem baseCompoundJsonTreeViewItem && baseCompoundJsonTreeViewItem.EndLine is not null && !baseCompoundJsonTreeViewItem.EndLine.IsDeleted)
                {
                    lineNumber = baseCompoundJsonTreeViewItem.EndLine.NextLine.LineNumber;
                }
                else
                if(VisualPrevious is not null)
                {
                    lineNumber = VisualPrevious.StartLine.NextLine.LineNumber;
                }
                StartLine = Plan.GetLineByNumber(lineNumber);
                JsonItemTool.SetParentForEachItem(basePlan.TreeViewItemList, null);
                basePlan.SetVisualPreviousAndNextForEachItem();
            }
            #endregion

            #endregion

            #region 处理逻辑树

            #region 添加子节点集并设置视觉引用
            LogicChildren.AddRange(childrenDataList.Result);
            JsonItemTool.SetParentForEachItem(LogicChildren, this);
            SetVisualPreviousAndNextForEachItem();
            #endregion

            #region 设置当前复合节点行首引用
            if (isNeedParentHead && Parent is not null)
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
                if (Parent is not null)
                {
                    StartLine = Parent.StartLine.NextLine;
                }
                else
                {
                    StartLine = Plan.GetLineByNumber(2);
                }
            }
            #endregion

            #region 处理父子关系及当前插入成员的行引用
            if (Parent is null)
            {
                Tuple<JsonTreeViewItem,JsonTreeViewItem> previousAndNext = JsonItemTool.SetLineNumbersForEachSubItem(childrenDataList.Result, StartLine.LineNumber + 1);
                if(previousAndNext is not null)
                {
                    VisualLastChild = previousAndNext.Item2;
                }
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

        public void InsertChild(int targetIndex, JsonTreeViewDataStructure childData)
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
            bool isNeedStartComma = childData.ResultString.Length > 0 && ((isNeedParentHead && VisualPrevious is not null && VisualNext is null) || (!isNeedParentHead && targetItem.VisualLastChild is not null && ((targetIndex < targetItem.VisualLastChild.Index && targetIndex > 0) || (targetItem.LogicChildren.Count == 1 && targetItem.LogicChildren[0] is BaseCompoundJsonTreeViewItem firstEnumItem && (firstEnumItem.EnumKey.Length > 0 || firstEnumItem.IsEnumBranch))))) || (ItemType is ItemType.BottomButton);
            bool isNeedEndComma = (targetItem.VisualLastChild is not null && (targetIndex <= targetItem.VisualLastChild.Index || targetItem.LogicChildren[0] is BaseCompoundJsonTreeViewItem customItem && customItem.ItemType is ItemType.CustomCompound)) || (isNeedParentHead && VisualNext is not null);
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
                    {
                        offset = VisualPrevious.StartLine.EndOffset;
                    }
                }
                else
                {
                    DocumentLine documentLine = null;
                    
                    if (targetIndex > 0 && targetIndex <= LogicChildren.Count && (targetItem.LogicChildren[0] is BaseCompoundJsonTreeViewItem firstItem && firstItem.ItemType is not ItemType.CustomCompound) && VisualLastChild is BaseCompoundJsonTreeViewItem baseCompoundJsonTreeViewItem && baseCompoundJsonTreeViewItem.EndLine is not null && !baseCompoundJsonTreeViewItem.EndLine.IsDeleted)
                    {
                        if (baseCompoundJsonTreeViewItem.ItemType is not ItemType.CustomCompound)
                        {
                            documentLine = baseCompoundJsonTreeViewItem.EndLine;
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
                appendString += (isNeedStartComma ? ',' : "") + "\r\n" + new string(' ', LayerCount * 2) + keyString + startBracket + "\r\n" + childData.ResultString.ToString() + "\r\n" + new string(' ', LayerCount * 2) + endBracket + (isNeedEndComma ? ',' : "");
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
            int startIndex = targetIndex;
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
            JsonItemTool.SetParentForEachItem(targetItem.LogicChildren, targetItem);
            targetItem.SetVisualPreviousAndNextForEachItem();

            DocumentLine targetLine = childData.Result[0].VisualPrevious?.StartLine;
            if (childData.Result[0].VisualPrevious is BaseCompoundJsonTreeViewItem visualPreviousCompoundItem && visualPreviousCompoundItem.EndLine is not null && !visualPreviousCompoundItem.EndLine.IsDeleted)
            {
                targetLine = visualPreviousCompoundItem.EndLine;
            }

            if (targetLine is not null)
            {
                JsonItemTool.SetLineNumbersForEachSubItem(childData.Result, targetLine.LineNumber + 1);
            }
            else
            {
                JsonItemTool.SetLineNumbersForEachSubItem(childData.Result,targetItem);
            }

            Tuple<JsonTreeViewItem, JsonTreeViewItem> previousAndNext = SearchVisualPreviousAndNextItem(childData.Result[^1]);
            if (previousAndNext is not null && previousAndNext.Item2 is not null && (previousAndNext.Item2 is not BaseCompoundJsonTreeViewItem || (previousAndNext.Item2 is BaseCompoundJsonTreeViewItem nextCompoundItem && nextCompoundItem.ItemType is not ItemType.CustomCompound && nextCompoundItem.ItemType is not ItemType.BottomButton)))
            {
                targetItem.VisualLastChild = previousAndNext.Item2;
            }
            #endregion

            #region 设置当前复合节点的行末引用
            if (EndLine is null || EndLine == StartLine || (EndLine is not null && EndLine.IsDeleted))
            {
                if (VisualLastChild is BaseCompoundJsonTreeViewItem baseCompoundJsonTreeViewItem2 && baseCompoundJsonTreeViewItem2.EndLine is not null && !baseCompoundJsonTreeViewItem2.EndLine.IsDeleted)
                {
                    EndLine = baseCompoundJsonTreeViewItem2.EndLine.NextLine;
                }
                else
                if (VisualLastChild is not null)
                {
                    EndLine = VisualLastChild.StartLine.NextLine;
                }
            }
            #endregion
            
            #endregion
        }

        /// <summary>
        /// 删除上一个选择的分支
        /// </summary>
        private void RemoveLastEnumBranch()
        {
            List<JsonTreeViewItem> targetChildrenList = [];
            if (Parent is not null && Parent.DisplayText != "Root")
            {
                int index = Parent.LogicChildren.IndexOf(this);
                if (index + 1 < Parent.LogicChildren.Count)
                {
                    targetChildrenList = Parent.LogicChildren.ToList()[(index + 1)..(index + EnumItemCount + 1)];
                }
            }
            else
            if (Plan is BaseCustomWorldUnifiedPlan basePlan)
            {
                int index = basePlan.TreeViewItemList.IndexOf(this);
                if (index + 1 < Parent.LogicChildren.Count)
                {
                    targetChildrenList = basePlan.TreeViewItemList.ToList()[(index + 1)..(index + EnumItemCount + 1)];
                }
            }
            RemoveChild(targetChildrenList);
        }

        /// <summary>
        /// 用于删除单个节点或列表元素
        /// </summary>
        /// <param name="childItem"></param>
        public void RemoveChild(List<JsonTreeViewItem> childrenList)
        {
            #region Field
            if (childrenList.Count == 0)
            {
                return;
            }
            JsonTreeViewItem groupLastItem = SearchEnumGroupLastVisualItem(childrenList);
            int offset = 0, length = 0;
            #endregion

            #region 确定替换的起始偏移
            if (childrenList[0].VisualPrevious is BaseCompoundJsonTreeViewItem visualPreviousCompoundItem && visualPreviousCompoundItem.EndLine is not null && !visualPreviousCompoundItem.EndLine.IsDeleted)
            {
                offset = visualPreviousCompoundItem.EndLine.EndOffset - (childrenList[0].VisualPrevious.VisualNext is null ? 1 : 0);
            }
            else
            if (childrenList[0].VisualPrevious is not null)
            {
                offset = childrenList[0].VisualPrevious.StartLine.EndOffset - (childrenList[0].VisualPrevious?.VisualNext is null ? 1 : 0);
            }
            else
            {
                offset = StartLine.EndOffset - (childrenList[0].VisualPrevious?.VisualNext is null ? 1 : 0);
            }
            #endregion

            #region 确定替换的长度
            if (groupLastItem is not null)
            {
                if (groupLastItem is BaseCompoundJsonTreeViewItem lastCompoundItem && lastCompoundItem.EndLine is not null && !lastCompoundItem.EndLine.IsDeleted)
                {
                    length = lastCompoundItem.EndLine.EndOffset - offset;
                }
                else
                if (groupLastItem.StartLine is not null)
                {
                    length = childrenList[^1].StartLine.EndOffset - offset;
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
                if (item.DisplayText == "Entry")
                {
                    LogicChildren.Remove(item);
                }
                else
                if (item is BaseCompoundJsonTreeViewItem baseCompoundJsonTreeViewItem)
                {
                    baseCompoundJsonTreeViewItem.EndLine = null;
                    baseCompoundJsonTreeViewItem.LogicChildren.Clear();
                    baseCompoundJsonTreeViewItem.VisualLastChild = null;
                }
            }
            SetVisualPreviousAndNextForEachItem();
            Tuple<JsonTreeViewItem, JsonTreeViewItem> previousAndNext = SearchVisualPreviousAndNextItem(LogicChildren[0]);
            if(previousAndNext is not null && previousAndNext.Item2 is not null)
            {
                VisualLastChild = previousAndNext.Item2;
            }
            #endregion
        }

        /// <summary>
        /// 用于删除复合节点的子节点集
        /// </summary>
        /// <param name="childItemList"></param>
        public void ClearChildren(List<JsonTreeViewItem> childItemList)
        {
            #region 处理视觉树
            VisualLastChild = null;
            int offset = 0,length = 0;
            string startLineString = Plan.GetRangeText(StartLine.Offset, StartLine.Length);
            string endLineString = Plan.GetRangeText(EndLine.Offset, EndLine.Length);
            if (Key.Length > 0)
            {
                offset = StartLine.Offset + startLineString.LastIndexOf(':') + 3;
            }
            else
            {
                int startBracketIndex = startLineString.IndexOf('{');
                if(startBracketIndex == -1)
                {
                    startBracketIndex = startLineString.IndexOf('[');
                }
                offset = StartLine.Offset + startBracketIndex + 1;
            }

            int endBracketIndex = endLineString.IndexOf('}');
            if(endBracketIndex == -1)
            {
                endBracketIndex = endLineString.IndexOf(']');
            }
            length = EndLine.Offset + endBracketIndex - offset;

            Plan.SetRangeText(offset, length, "");
            #endregion

            #region 处理逻辑树
            LogicChildren.Clear();
            IsCurrentExpanded = false;
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
            if(currentValueTypeString.Length > 0)
            {
                #region Field
                int customItemCount = 0;
                DataType = DataType.None;
                ItemType = ItemType.MultiType;
                IsNotValueTypeChanging = false;
                EnumItemsSource.Clear();
                IsNotValueTypeChanging = true;
                int offset = 0, length = 0, oldOffset = 0, oldLength = 0;
                string startLineText = "";
                string ResultString = "";
                string oldResultString = "";
                if (StartLine is not null && !StartLine.IsDeleted)
                {
                    startLineText = Plan.GetRangeText(StartLine.Offset, StartLine.Length);
                }
                bool isCompoundType = currentValueTypeString == "compound" || currentValueTypeString == "treeViewItemList" || currentValueTypeString.Contains("array");
                HtmlHelper htmlHelper = new(_container)
                {
                    plan = Plan,
                    jsonTool = JsonItemTool
                };
                #endregion

                #region 将当前文本值清除，处理子节点
                List<JsonTreeViewItem> cacheItemList = [];
                string cacheString = "";
                if(LogicChildren.Count > 0)
                {
                    cacheItemList = [..LogicChildren];
                }
                LogicChildren.Clear();

                if (cacheItemList.Count > 0 && cacheItemList[0].DisplayText == "Entry")
                {
                    BaseCompoundJsonTreeViewItem entryItem = cacheItemList[0] as BaseCompoundJsonTreeViewItem;
                    oldOffset = entryItem.StartLine.Offset;
                    if (entryItem.EndLine is not null)
                    {
                        oldLength = entryItem.EndLine.EndOffset - oldOffset;
                    }
                    else
                    {
                        oldLength = entryItem.StartLine.EndOffset - oldOffset;
                    }
                    oldResultString = Plan.GetRangeText(oldOffset, oldLength);
                    string spaceString = (LayerCount > 0 ? new string(' ', LayerCount * 2) : "  ");
                    oldResultString = spaceString + oldResultString.Replace("\r\n  ", "\r\n").TrimStart(['{', '\r', '\n', ' ']).TrimEnd(['}', '\r', '\n', ' ']) + spaceString;
                }

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
                else
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
                bool isExpendOptionalCompound = IsCanBeDefaulted && VisualPrevious is not null && VisualPrevious.StartLine is not null && (VisualNext is null || (VisualNext is not null && VisualNext.StartLine is null));

                bool isParentAddElement = addParentListStructure;

                bool isAddListItem = StartLine is null && EndLine is null && ItemType is ItemType.List && VisualPrevious is not null && VisualPrevious.StartLine is not null && (VisualNext is null || (VisualNext is not null && VisualNext.StartLine is null));

                if (isParentAddElement || isAddListItem || StartLine is null)
                {
                    connectorSymbol = (isExpendOptionalCompound ? ',' : "") + "\r\n" + new string(' ', LayerCount * 2);
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

                if(VisualNext is not null && VisualNext.StartLine is not null)
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
                                Value = false;
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

                    #region 管理枚举、外层括号、控件显示
                    if (CompoundChildrenStringList.Count > 0 && currentValueTypeString != "treeViewItemList")
                    {
                        targetRawStringList = [..CompoundChildrenStringList];
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
                                targetRawStringList = [.. CompoundChildrenStringList];
                            }
                        }
                        else
                        if (currentValueTypeString == "treeViewItemList" || currentValueTypeString.Contains("array"))
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
                    #endregion

                    #region 执行分析，获取结果
                    JsonTreeViewDataStructure result = new();
                    if (currentValueTypeString != "treeViewItemList")
                    {
                        //将列表状态下的第一个复合元素的子级移动到当前节点下
                        if (oldSelectedValueTypeItem is not null && oldSelectedValueTypeItem.Text.Equals("treeViewItemList", StringComparison.CurrentCultureIgnoreCase) && cacheItemList.Count > 0)
                        {
                            BaseCompoundJsonTreeViewItem entryItem = cacheItemList[0] as BaseCompoundJsonTreeViewItem;
                            foreach (var item in entryItem.LogicChildren)
                            {
                                item.Parent = this;
                            }
                            LogicChildren.AddRange(entryItem.LogicChildren);
                            result.ResultString = new(oldResultString);
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
                            htmlHelper.HandlingTheTypingAppearanceOfCompositeItemList(result.Result, this);
                            for (int i = 0; i < result.Result.Count; i++)
                            {
                                result.Result[i].RemoveElementButtonVisibility = Visibility.Collapsed;
                            }
                            LogicChildren.AddRange(result.Result);
                        }
                    }
                    else//把复合状态下的子节点移给列表在状态下
                    if(cacheItemList is not null)
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
                        JsonItemTool.SetLayerCountForEachItem(entry.LogicChildren, entry.LayerCount + 1);
                        LogicChildren.Add(entry);
                        LogicChildren.Add(addToBottom);
                        cacheString = cacheString.Replace("\r\n", "\r\n  ");
                        result.ResultString = new(new string(' ', (LayerCount + 1) * 2) + cacheString);
                    }
                    currentItemReference.IsExpanded = true;
                    #endregion

                    #region 更新代码编辑器
                    ResultString = result.ResultString.ToString();

                    ResultString = connectorSymbol + (Key.Length > 0 ? "\"" + Key + "\": " : "") + bracketPairString[0] + "\r\n" + ResultString + "\r\n" + new string(' ', LayerCount * 2) + bracketPairString[1] + endConnectorSymbol;
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
                    if (currentValueTypeString == "compound" || currentValueTypeString.Contains("array"))
                    {
                        JsonItemTool.SetParentForEachItem(LogicChildren, this);
                        JsonItemTool.SetLayerCountForEachItem(LogicChildren, LayerCount + 1);
                        JsonItemTool.SetLineNumbersForEachSubItem(LogicChildren, this);
                    }
                    
                    if (VisualLastChild is BaseCompoundJsonTreeViewItem lastCompoundItem && lastCompoundItem.EndLine is not null)
                    {
                        EndLine = lastCompoundItem.EndLine.NextLine;
                    }
                    else
                    if (VisualLastChild is not null && VisualLastChild.StartLine is not null)
                    {
                        EndLine = VisualLastChild.StartLine.NextLine;
                    }
                    EndLine ??= StartLine;
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
            #region 判断是否无需处理
            if (ItemType is ItemType.CustomCompound || EnumItemsSource.Count == 0 || !IsNotValueTypeChanging)
            {
                return;
            }
            #endregion

            #region Field
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
                string connectorString = (VisualPrevious is not null && VisualPrevious.StartLine is not null && (VisualNext is null || (VisualNext is not null && VisualNext.StartLine is null)) ? "," : "") + "\r\n" + new string(' ', LayerCount * 2);
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
                    if ((Parent.ItemType is ItemType.List && ((Parent.StartLine == Parent.EndLine) || Parent.EndLine is null)) || ((Parent.ItemType is ItemType.Compound || Parent.ItemType is ItemType.OptionalCompound) && ((Parent.StartLine == Parent.EndLine) || Parent.EndLine is null)))
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
                    offset -= previousLineString.TrimEnd().EndsWith(',') && (VisualNext is null || (VisualNext is not null && VisualNext.StartLine is null)) ? 1 : 0;
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

                    if (VisualNext is null || (VisualNext is not null && VisualNext.StartLine is null))
                    {
                        parentEndLineText = Plan.GetRangeText(Parent.EndLine.Offset, Parent.EndLine.Length);
                    }
                    int endBracketOffset = parentEndLineText.IndexOf('}');
                    if(endBracketOffset == -1)
                    {
                        endBracketOffset = parentEndLineText.IndexOf(']');
                    }
                    offset = Parent.StartLine.Offset + colonOffset;
                    if (VisualNext is null || (VisualNext is not null && VisualNext.StartLine is null))
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
                    
                    htmlHelper.HandlingTheTypingAppearanceOfCompositeItemList(result.Result,Parent);

                    ObservableCollection<JsonTreeViewItem> treeViewItemList = [];
                    index = Index;
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
                        Parent.InsertChild(Index, result);
                    }
                    else
                    {
                        basePlan1.InsertChildrenList(Index, result);
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
                    while (result.ResultString.Length > 0 && (result.ResultString[^1] == '\r' ||
                        result.ResultString[^1] == '\n' ||
                        result.ResultString[^1] == ',' ||
                        result.ResultString[^1] == ' '))
                    {
                        result.ResultString.Length--;
                    }

                    if (VisualNext is not null)
                    {
                        if (VisualNext.VisualNext is not null && VisualNext.VisualNext.StartLine is not null)
                        {
                            result.ResultString.Append(',');
                        }
                    }

                    if (result.ResultString.Length > 0)
                    {
                        Plan.SetRangeText(StartLine.EndOffset, 0, result.ResultString.Length > 0 ? ",\r\n" + result.ResultString.ToString() : "");
                        if (Parent is not null)
                        {
                            JsonItemTool.SetLineNumbersForEachSubItem(result.Result, Parent);
                        }
                        else
                        {
                            JsonItemTool.SetLineNumbersForEachSubItem(result.Result, EndLine is not null ? EndLine.LineNumber + 1 : StartLine.LineNumber + 1);
                        }
                    }
                    else
                    {
                        for (int i = 0; i < result.Result.Count; i++)
                        {
                            result.Result[i].Parent = Parent;
                            if(i > 0)
                            {
                                result.Result[i].LogicPrevious = result.Result[i - 1];
                                result.Result[i - 1].LogicNext = result.Result[i];
                            }
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
                bool isNeedResetNextItem = false;
                if (SelectedEnumItem.Text != "- unset -")
                {
                    bool haveCurrentEnum = false;
                    int startIndex = 0, endIndex = 0;
                    int currentStarCount = 0,startStarCount = 0;

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

                    if (FilteredRawList.Count > 0)
                    {
                        JsonTreeViewDataStructure result = htmlHelper.GetTreeViewItemResult(new(), FilteredRawList, LayerCount, "", this, null, 1, true);

                        index = 0;
                        if (Parent is not null)
                        {
                            index = Parent.LogicChildren.IndexOf(this) + 1;
                        }
                        else
                        if (Plan is BaseCustomWorldUnifiedPlan basePlan)
                        {
                            index = basePlan.TreeViewItemList.IndexOf(this) + 1;
                        }
                        EnumItemCount = result.Result.Count;
                        if (Parent is not null)
                        {
                            Parent.InsertChild(index, result);
                        }
                        else
                        {
                            BaseCustomWorldUnifiedPlan basePlan = Plan as BaseCustomWorldUnifiedPlan;
                            basePlan.InsertChild(index, result);
                        }

                        #region 关闭当前所有节点的删除功能
                        for (int i = 0; i < result.Result.Count; i++)
                        {
                            result.Result[i].RemoveElementButtonVisibility = Visibility.Collapsed;
                            index++;
                        }
                        #endregion
                    }
                    else
                    {
                        isNeedResetNextItem = true;
                    }
                    skipCode = true;
                }
                else
                {
                    isNeedResetNextItem = true;

                    RemoveLastEnumBranch();
                }

                #region 重新配置前后关系
                if (isNeedResetNextItem)
                {
                    ObservableCollection<JsonTreeViewItem> treeViewItemList = [];
                    if (Parent is null && Plan is BaseCustomWorldUnifiedPlan basePlan)
                    {
                        treeViewItemList = basePlan.TreeViewItemList;
                    }
                    else
                    {
                        treeViewItemList = Parent.LogicChildren;
                    }
                    if (treeViewItemList.Count > 0)
                    {
                        index = treeViewItemList.IndexOf(this) + 1;
                        if (index < treeViewItemList.Count)
                        {
                            JsonTreeViewItem nextItem = treeViewItemList[index];
                            nextItem.LogicPrevious = this;
                            LogicNext = nextItem;
                        }
                    }
                }
                #endregion
            }
            #endregion

            #region 处理父节点的尾行引用
            if (Parent is not null && (Parent.StartLine == Parent.EndLine || (Parent.EndLine is null || (Parent.EndLine is not null && Parent.EndLine.IsDeleted))) && StartLine is not null)
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