using CBHK.CustomControl.Interfaces;
using CBHK.CustomControl.JsonTreeViewComponents;
using CBHK.Model.Common;
using CBHK.Service.Json;
using CBHK.ViewModel.Common;
using Prism.Ioc;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using static CBHK.Model.Common.Enums;

namespace CBHK.GeneralTool.TreeViewComponentsHelper
{
    public partial class JsonTreeViewItemExtension(IContainerProvider container) : IJsonItemTool
    {
        #region Field
        private IContainerProvider _container = container;

        [GeneratedRegex(@"\[\[\#?((?<1>[\u4e00-\u9fff]+)\|(?<2>[\u4e00-\u9fff]+)|(?<1>[\u4e00-\u9fff]+))\]\]")]
        private static partial Regex GetContextKey();

        [GeneratedRegex(@"\s?\s*\*+\s?\s*{{[nN]bt\sinherit(?<1>[a-z_/|=*\s]+)}}\s?\s*", RegexOptions.IgnoreCase)]
        private static partial Regex GetInheritString();

        private List<string> singleLineElementType = ["string", "bool", "int", "short", "float", "double", "long"];
        #endregion

        #region Method
        public BaseCompoundJsonTreeViewItem GetAddToBottomButtonItem(BaseCompoundJsonTreeViewItem template)
        {
            return new BaseCompoundJsonTreeViewItem(template.Plan, template.JsonItemTool, _container)
            {
                CompoundChildrenStringList = template.CompoundChildrenStringList,
                ListChildrenStringList = template.ListChildrenStringList,
                ArrayChildrenStringList = template.ArrayChildrenStringList,
                ValueTypeSource = template.ValueTypeSource,
                LayerCount = template.LayerCount,
                InfoTipText = template.InfoTipText,
                ElementButtonTip = "添加到末尾",
                ItemType = ItemType.BottomButton,
                Parent = template,
                AddOrSwitchElementButtonVisibility = Visibility.Visible,
                SwitchButtonColor = template.PlusColor,
                SwitchButtonIcon = template.PlusIcon,
                PressedSwitchButtonColor = template.PressedPlusColor
            };
        }

        public Tuple<List<string>,bool> ExtractSubInformationFromPromptSourceCode(BaseCompoundJsonTreeViewItem compoundJsonTreeViewItem)
        {
            Match contextMatch = GetContextKey().Match(compoundJsonTreeViewItem.CompoundChildrenStringList.Count > 0 ? compoundJsonTreeViewItem.CompoundChildrenStringList.FirstOrDefault() : "");
            if(!contextMatch.Success)
            {
                contextMatch = GetContextKey().Match(compoundJsonTreeViewItem.InfoTipText);
            }
            string currentValueTypeString = compoundJsonTreeViewItem.SelectedValueType is not null ? compoundJsonTreeViewItem.SelectedValueType.Text.ToLower() : "";
            string contextKey = contextMatch.Groups[1].Value;
            bool isPartialData = false;
            Tuple<List<string>, bool> Result = new([],false);
            List<string> targetList = [], dependencyResultList = [];
            Dictionary<string, List<string>> targetDictionary = [];
            ICustomWorldUnifiedPlan Plan = compoundJsonTreeViewItem.Plan;

            #region 所有存储结构中依次轮询，找到当前需要的数据
            if (Plan.DependencyItemList.TryGetValue(contextKey, out dependencyResultList))
            {
            }
            else
            if (Plan.EnumIDDictionary.TryGetValue(contextKey, out targetList))
            {
                Result = new(targetList, isPartialData);
            }
            else
            if (Plan.TranslateDictionary.TryGetValue(contextKey, out string targetKey))
            {
                if(targetKey.Contains(';'))
                {
                    isPartialData = true;
                    string[] keyList = targetKey.Split(';');
                    if(compoundJsonTreeViewItem.ValueTypeSource.Count > 0)
                    {
                        int valueTypeIndex = compoundJsonTreeViewItem.ValueTypeSource.IndexOf(compoundJsonTreeViewItem.SelectedValueType);
                        if (compoundJsonTreeViewItem.ValueTypeSource[0].Text == "- unset -")
                        {
                            valueTypeIndex--;
                        }
                        if (valueTypeIndex >= 0 && valueTypeIndex < compoundJsonTreeViewItem.ValueTypeSource.Count)
                        {
                            targetKey = keyList[valueTypeIndex];
                        }
                    }
                }

                if (Plan.DependencyItemList.TryGetValue(targetKey, out targetList))
                {
                }
                else
                if (Plan.TranslateDefaultDictionary.TryGetValue(targetKey, out string defaultKey) && Plan.DependencyItemList.TryGetValue(defaultKey, out targetList))
                {
                }
                else
                if (Plan.EnumIDDictionary.TryGetValue(targetKey, out targetList))
                {
                }
                else
                if (Plan.EnumCompoundDataDictionary.TryGetValue(targetKey, out targetDictionary))
                {
                    targetList = [.. targetDictionary.Keys.Cast<string>()];
                }

                if (targetList is not null)
                {
                    Result = new(targetList, isPartialData);
                }
            }
            #endregion

            return Result;
        }

        public Tuple<JsonTreeViewItem, JsonTreeViewItem> SetLineNumbersForEachSubItem(ObservableCollection<JsonTreeViewItem> list, BaseCompoundJsonTreeViewItem parent,JsonTreeViewItem currentItem = null)
        {
            if (parent is not null && parent.StartLine is null)
            {
                if (parent.VisualPrevious is BaseCompoundJsonTreeViewItem baseCompoundJsonTreeViewItem && baseCompoundJsonTreeViewItem.EndLine is not null && !baseCompoundJsonTreeViewItem.EndLine.IsDeleted)
                {
                    parent.StartLine = parent.Plan.GetLineByNumber(baseCompoundJsonTreeViewItem.EndLine.LineNumber + 1);
                }
                else
                if (parent.VisualPrevious is not null && parent.VisualPrevious.StartLine is not null)
                {
                    parent.StartLine = parent.Plan.GetLineByNumber(parent.VisualPrevious.StartLine.LineNumber + 1);
                }
                else
                if (parent.Parent is not null)
                {
                    parent.StartLine = parent.Plan.GetLineByNumber(parent.Parent.StartLine.LineNumber + 1);
                }
                else
                {
                    parent.StartLine = parent.Plan.GetLineByNumber(2);
                }
            }

            int lineNumber = parent is not null && parent.StartLine is not null ? parent.StartLine.LineNumber + 1 : 2;

            JsonTreeViewItem theLastItem = null;
            JsonTreeViewItem theFirstItem = null;
            for (int i = 0; i < list.Count; i++)
            {
                BaseCompoundJsonTreeViewItem baseCompoundJsonTreeViewItem = list[i] as BaseCompoundJsonTreeViewItem;
                if (((list[i].IsCanBeDefaulted && (list[i].StartLine is null || (list[i].StartLine is not null && list[i].StartLine.IsDeleted))) || (list[i] is not BaseCompoundJsonTreeViewItem && list[i].DataType is DataType.None) || (baseCompoundJsonTreeViewItem is not null && baseCompoundJsonTreeViewItem.ValueTypeSource.Count == 0 && (baseCompoundJsonTreeViewItem.ItemType is ItemType.CustomCompound || baseCompoundJsonTreeViewItem.ItemType is ItemType.BottomButton || (baseCompoundJsonTreeViewItem.SelectedEnumItem is not null && baseCompoundJsonTreeViewItem.SelectedEnumItem.Text == "- unset -" && baseCompoundJsonTreeViewItem.IsCanBeDefaulted)))) && list[i] != currentItem)
                {
                    continue;
                }

                list[i].StartLine = list[0].Plan.GetLineByNumber(lineNumber);

                theFirstItem ??= list[i];
                theLastItem = list[i];

                if (!list[i].IsCanBeDefaulted || list[i] == currentItem || ((list[i].StartLine is not null && !list[i].StartLine.IsDeleted) && list[i].DataType is not DataType.None || (list[i] is BaseCompoundJsonTreeViewItem compoundElementItem2 && compoundElementItem2.DataType is not DataType.None && compoundElementItem2.ItemType is not ItemType.CustomCompound && compoundElementItem2.ItemType is not ItemType.OptionalCompound)))
                {
                    lineNumber++;
                }
                if (list[i] is BaseCompoundJsonTreeViewItem subCompoundItem2)
                {
                    if (subCompoundItem2.LogicChildren.Count > 0 && (subCompoundItem2.LogicChildren[0] is BaseCompoundJsonTreeViewItem subsubCompoundItem && (subsubCompoundItem.ItemType is not ItemType.CustomCompound && subsubCompoundItem.ItemType is not ItemType.BottomButton) || subCompoundItem2.LogicChildren[0] is not BaseCompoundJsonTreeViewItem))
                    {
                        Tuple<JsonTreeViewItem, JsonTreeViewItem> result = SetLineNumbersForEachSubItem(subCompoundItem2.LogicChildren, subCompoundItem2);
                        if (result.Item2 is not null)
                        {
                            subCompoundItem2.VisualLastChild = result.Item2;
                        }

                        if (result.Item2 is BaseCompoundJsonTreeViewItem subsubCompoundItem2 && subsubCompoundItem2.EndLine is not null && !subsubCompoundItem2.EndLine.IsDeleted)
                        {
                            subCompoundItem2.EndLine = subsubCompoundItem2.EndLine.NextLine;
                        }
                        else
                        if (result is not null && result.Item2 is not null && result.Item2.StartLine is not null)
                        {
                            subCompoundItem2.EndLine = result.Item2.StartLine.NextLine;
                        }
                        else
                        {
                            subCompoundItem2.EndLine = subCompoundItem2.StartLine;
                        }
                    }
                    else
                    {
                        subCompoundItem2.EndLine = subCompoundItem2.StartLine;
                    }
                    if (subCompoundItem2.StartLine != subCompoundItem2.EndLine && subCompoundItem2.EndLine is not null)
                    {
                        lineNumber = subCompoundItem2.EndLine.LineNumber + 1;
                    }
                }
            }

            return new(theFirstItem, theLastItem);
        }

        public Tuple<JsonTreeViewItem, JsonTreeViewItem> SetLineNumbersForEachSubItem(ObservableCollection<JsonTreeViewItem> list, int lineNumber, JsonTreeViewItem currentItem = null)
        {
            JsonTreeViewItem theFirstItem = null;
            JsonTreeViewItem theLastItem = null;
            for (int i = 0; i < list.Count; i++)
            {
                if (((list[i].IsCanBeDefaulted && (list[i].StartLine is null || (list[i].StartLine is not null && list[i].StartLine.IsDeleted))) || (list[i] is not BaseCompoundJsonTreeViewItem && list[i].DataType is DataType.None) || (list[i] is BaseCompoundJsonTreeViewItem compoundElementItem1 && compoundElementItem1.ValueTypeSource.Count == 0 && (compoundElementItem1.DataType is DataType.None || compoundElementItem1.ItemType is ItemType.CustomCompound || compoundElementItem1.ItemType is ItemType.BottomButton))) && list[i] != currentItem)
                {
                    continue;
                }

                list[i].StartLine = list[0].Plan.GetLineByNumber(lineNumber);

                theFirstItem ??= list[i];
                theLastItem = list[i];

                if (!list[i].IsCanBeDefaulted || list[i] == currentItem || (list[i].StartLine is not null && !list[i].StartLine.IsDeleted && list[i].DataType is not DataType.None || (list[i] is BaseCompoundJsonTreeViewItem compoundElementItem2 && compoundElementItem2.DataType is not DataType.None && compoundElementItem2.ItemType is not ItemType.CustomCompound && compoundElementItem2.ItemType is not ItemType.OptionalCompound)))
                {
                    lineNumber++;
                }
                if (list[i] is BaseCompoundJsonTreeViewItem subCompoundItem2)
                {
                    if (subCompoundItem2.LogicChildren.Count > 0 && (subCompoundItem2.LogicChildren[0] is BaseCompoundJsonTreeViewItem subsubCompoundItem && subsubCompoundItem.ItemType is not ItemType.CustomCompound || subCompoundItem2.LogicChildren[0] is not BaseCompoundJsonTreeViewItem))
                    {
                        Tuple<JsonTreeViewItem, JsonTreeViewItem> result = SetLineNumbersForEachSubItem(subCompoundItem2.LogicChildren, subCompoundItem2);

                        if (result is not null)
                        {
                            subCompoundItem2.VisualLastChild = result.Item2;
                        }
                        if (result.Item2 is BaseCompoundJsonTreeViewItem subsubCompoundItem2 && subsubCompoundItem2.EndLine is not null && !subsubCompoundItem2.EndLine.IsDeleted)
                        {
                            subCompoundItem2.EndLine = subsubCompoundItem2.EndLine.NextLine;
                        }
                        else
                        if (result is not null && result.Item2 is not null && result.Item2.StartLine is not null)
                        {
                            subCompoundItem2.EndLine = result.Item2.StartLine.NextLine;
                        }
                        else
                        if (result is not null)
                        {
                            subCompoundItem2.EndLine = subCompoundItem2.Plan.GetLineByNumber(subCompoundItem2.StartLine.LineNumber + subCompoundItem2.LogicChildren.Count);
                        }
                    }
                    else
                    {
                        subCompoundItem2.EndLine = subCompoundItem2.StartLine;
                    }
                    if (subCompoundItem2.StartLine != subCompoundItem2.EndLine && subCompoundItem2.EndLine is not null)
                    {
                        lineNumber = subCompoundItem2.EndLine.LineNumber + 1;
                    }
                }
            }

            return new(theFirstItem, theLastItem);
        }

        public void SetLayerCountForEachItem(ObservableCollection<JsonTreeViewItem> list,int startCount)
        {
            foreach (var item in list)
            {
                if(item is BaseCompoundJsonTreeViewItem compoundJsonTreeViewItem1 && compoundJsonTreeViewItem1.ItemType is ItemType.CustomCompound)
                {
                    item.LayerCount = item.Parent.LayerCount;
                }
                else
                {
                    item.LayerCount = startCount;
                }
                if (item is BaseCompoundJsonTreeViewItem compoundJsonTreeViewItem2 && compoundJsonTreeViewItem2.LogicChildren.Count > 0)
                {
                    SetLayerCountForEachItem(compoundJsonTreeViewItem2.LogicChildren, compoundJsonTreeViewItem2.LayerCount + 1);
                }
            }
        }

        public void SetParentForEachItem(ObservableCollection<JsonTreeViewItem> list, BaseCompoundJsonTreeViewItem currentParent,int startIndex = 0)
        {
            JsonTreeViewItem previous = null;
            for (int i = 0; i < list.Count; i++)
            {
                list[i].Parent = currentParent;
                list[i].LogicNext = list[i].LogicPrevious = null;
                if (list[i] is BaseCompoundJsonTreeViewItem baseCompoundJsonTreeViewItem && (baseCompoundJsonTreeViewItem.ItemType is ItemType.CustomCompound || baseCompoundJsonTreeViewItem.ItemType is ItemType.BottomButton))
                {
                    previous = list[i];
                    continue;
                }

                bool isPreviousNotItem = previous is BaseCompoundJsonTreeViewItem previousCompoundJsonTreeViewItem && (previousCompoundJsonTreeViewItem.ItemType is ItemType.CustomCompound || previousCompoundJsonTreeViewItem.ItemType is ItemType.BottomButton);

                if (!isPreviousNotItem && previous is not null && previous.StartLine is not null && !previous.StartLine.IsDeleted)
                {
                    list[i].LogicPrevious = previous;
                }
                if(!isPreviousNotItem && previous is not null)
                {
                    previous.LogicNext = list[i];
                }

                list[i].Index = startIndex;
                startIndex++;

                if (list[i] is BaseCompoundJsonTreeViewItem compoundJsonTreeViewItem && compoundJsonTreeViewItem.LogicChildren.Count > 0)
                {
                    compoundJsonTreeViewItem.SetVisualPreviousAndNextForEachItem();

                    if (currentParent is not null)
                    {
                        Tuple<JsonTreeViewItem, JsonTreeViewItem> result = compoundJsonTreeViewItem.SearchVisualPreviousAndNextItem(currentParent,false);
                        currentParent.VisualLastChild = result.Item2;
                    }
                    
                    SetParentForEachItem(compoundJsonTreeViewItem.LogicChildren, compoundJsonTreeViewItem);
                }

                previous = list[i];
            }

            currentParent?.SetVisualPreviousAndNextForEachItem();
        }

        public void AddSubStructure(BaseCompoundJsonTreeViewItem compoundJsonTreeViewItem)
        {
            #region Field
            ItemType currentItemType = compoundJsonTreeViewItem.ItemType;
            ItemType parentItemType = compoundJsonTreeViewItem.Parent is not null ? compoundJsonTreeViewItem.Parent.ItemType : ItemType.Compound;
            //当可以是多种数据类型时，采用当前选择的类型
            if(currentItemType is ItemType.MultiType)
            {
                currentItemType = (ItemType)Enum.Parse(typeof(ItemType), compoundJsonTreeViewItem.SelectedValueType.Text);
            }
            if(parentItemType is ItemType.MultiType && compoundJsonTreeViewItem.Parent is not null)
            {
                parentItemType = (ItemType)Enum.Parse(typeof(ItemType), compoundJsonTreeViewItem.Parent.SelectedValueType.Text);
            }
            bool currentIsList = currentItemType is ItemType.List ||currentItemType is ItemType.Array;

            bool parentIsList = parentItemType is ItemType.Array || parentItemType is ItemType.List;

            bool currentIsCompound = currentItemType is ItemType.Compound ||
                currentItemType is ItemType.CustomCompound ||
                currentItemType is ItemType.OptionalCompound;
            JsonTreeViewDataStructure result = new();
            List<string> CurrentChildrenStringList = [];
            List<string> CurrentDependencyItemList = [];
            int customItemCount = 0;
            BaseCompoundJsonTreeViewItem targetCompoundItem = compoundJsonTreeViewItem;
            if (compoundJsonTreeViewItem.ItemType is ItemType.CustomCompound || compoundJsonTreeViewItem.ItemType is ItemType.BottomButton)
            {
                targetCompoundItem = compoundJsonTreeViewItem.Parent;
            }
            if (currentIsCompound)
            {
                if(compoundJsonTreeViewItem.ItemType is ItemType.CustomCompound && compoundJsonTreeViewItem.SelectedEnumItem is null && (compoundJsonTreeViewItem.Value is null || (compoundJsonTreeViewItem.Value is not null && (compoundJsonTreeViewItem.Value + "").Length == 0)))
                {
                    return;
                }
            }

            HtmlHelper htmlHelper = new(_container)
            {
                plan = compoundJsonTreeViewItem.Plan,
                jsonTool = compoundJsonTreeViewItem.JsonItemTool
            };
            #endregion

            #region 定位相邻的已有值的两个节点
            JsonTreeViewItem previous = compoundJsonTreeViewItem.VisualPrevious;
            BaseCompoundJsonTreeViewItem parent = compoundJsonTreeViewItem.Parent;
            JsonTreeViewItem next = compoundJsonTreeViewItem.VisualNext;
            BaseCompoundJsonTreeViewItem previousCompound = null;
            BaseCompoundJsonTreeViewItem nextCompound = null;

            if (previous is BaseCompoundJsonTreeViewItem)
            {
                previousCompound = previous as BaseCompoundJsonTreeViewItem;
            }
            if (next is BaseCompoundJsonTreeViewItem)
            {
                nextCompound = next as BaseCompoundJsonTreeViewItem;
            }
            #endregion

            #region 计算有多少个自定义子节点
            if (parent is not null && parent.LogicChildren.Count > 1 && parent.LogicChildren[1] is BaseCompoundJsonTreeViewItem subCompoundItem2 && subCompoundItem2.ItemType is ItemType.CustomCompound)
            {
                customItemCount = 2;
            }
            else
            if (parent is not null && parent.LogicChildren.Count > 0 && parent.LogicChildren[0] is BaseCompoundJsonTreeViewItem subCompoundItem1 && subCompoundItem1.ItemType is ItemType.CustomCompound)
            {
                customItemCount = 1;
            }
            #endregion

            #region 判断是在添加枚举型结构还是展开复合节点
            bool addMoreCustomStructure = currentItemType is ItemType.CustomCompound && parent is not null && customItemCount > 0;

            bool addMoreStructure = currentItemType is ItemType.OptionalCompound || currentItemType is ItemType.Compound;

            bool addListStructure = currentItemType is ItemType.List;

            bool addParentListStructure = currentItemType is not ItemType.List && parent is not null && (parent.ItemType is ItemType.List || (parent.ItemType is  ItemType.MultiType && parent.SelectedValueType is not null && parent.SelectedValueType.Text == "List"));
            #endregion

            #region 若没有子信息，则分析当前的提示源文本中是否有上下文数据
            if(targetCompoundItem.CompoundChildrenStringList.Count > 0)
            {
                CurrentChildrenStringList = compoundJsonTreeViewItem.CompoundChildrenStringList;
            }
            else
            if(targetCompoundItem.ListChildrenStringList.Count > 0)
            {
                CurrentChildrenStringList = compoundJsonTreeViewItem.ListChildrenStringList;
            }
            else
            if(targetCompoundItem.ArrayChildrenStringList.Count > 0)
            {
                CurrentChildrenStringList = compoundJsonTreeViewItem.ArrayChildrenStringList;
            }

            if (CurrentChildrenStringList.Count == 0)
            {
                if (compoundJsonTreeViewItem.CompoundChildrenStringList.Count > 0)
                {
                    CurrentChildrenStringList = compoundJsonTreeViewItem.CompoundChildrenStringList;
                }
                else
                    if (compoundJsonTreeViewItem.ArrayChildrenStringList.Count > 0)
                {
                    CurrentChildrenStringList = compoundJsonTreeViewItem.ArrayChildrenStringList;
                }
                else
                    if (compoundJsonTreeViewItem.ListChildrenStringList.Count > 0)
                {
                    CurrentChildrenStringList = compoundJsonTreeViewItem.ListChildrenStringList;
                }
            }

            if (CurrentChildrenStringList.Count == 0)
            {
                Match contextMatch = GetContextKey().Match(targetCompoundItem.InfoTipText);

                #region 分析上下文
                if (contextMatch.Success)
                {
                    string currentKey = contextMatch.Groups[1].Value;
                    ICustomWorldUnifiedPlan plan = targetCompoundItem.Plan;
                    if (plan.DependencyItemList.TryGetValue(currentKey, out List<string> targetList1))
                    {
                        CurrentChildrenStringList = targetList1;
                    }
                    else
                    if (plan.TranslateDictionary.TryGetValue(currentKey, out string targetKey))
                    {
                        if (targetKey.Contains(';'))
                        {
                            string[] keyList = targetKey.Split(';');
                            if (targetCompoundItem.ValueTypeSource.Count > 0)
                            {
                                int valueTypeIndex = targetCompoundItem.ValueTypeSource.IndexOf(targetCompoundItem.SelectedValueType);
                                if (targetCompoundItem.ValueTypeSource[0].Text == "- unset -")
                                {
                                    valueTypeIndex--;
                                }
                                if (valueTypeIndex >= 0 && valueTypeIndex < targetCompoundItem.ValueTypeSource.Count)
                                {
                                    targetKey = keyList[valueTypeIndex];
                                }
                            }
                        }

                        if (plan.DependencyItemList.TryGetValue(targetKey, out List<string> targetList2))
                        {
                            CurrentChildrenStringList = targetList2;
                        }
                        else
                        if (plan.EnumIDDictionary.TryGetValue(targetKey, out List<string> targetList3))
                        {
                            CurrentChildrenStringList = targetList3;
                        }
                        else
                        if (plan.EnumCompoundDataDictionary.TryGetValue(targetKey, out Dictionary<string, List<string>> targetDictionary))
                        {
                            bool isNeedValueType = (targetCompoundItem.SelectedValueType is not null && targetCompoundItem.SelectedValueType.Text == "List") || (targetCompoundItem.SelectedEnumItem is not null && targetCompoundItem.SelectedEnumItem.Text == "List");
                            bool isNeedTargetValue = (targetCompoundItem.SelectedValueType is not null && targetCompoundItem.SelectedValueType.Text == "Compound") || (targetCompoundItem.SelectedEnumItem is not null && targetCompoundItem.SelectedEnumItem.Text == "Compound");
                            List<string> targetList4 = [];
                            if (isNeedTargetValue && plan.TranslateDefaultDictionary.TryGetValue(targetKey, out string targetEnumKey))
                            {
                                if (targetDictionary.TryGetValue(targetEnumKey, out targetList4))
                                {
                                    CurrentChildrenStringList = targetList4[1..];
                                }
                                else
                                if (plan.DependencyItemList.TryGetValue(targetEnumKey, out targetList4))
                                {
                                    CurrentChildrenStringList = targetList4;
                                }
                            }
                            if (isNeedValueType)
                            {
                                CurrentChildrenStringList = ["*{{nbt " + targetKey.Trim('#') + "}}[[" + targetKey + "]]"];
                            }
                        }
                    }
                }
                #endregion

                #region 若无上下文关键字，则在上下文字典中搜索对应的数据集
                if (CurrentChildrenStringList.Count == 0)
                {
                    if (targetCompoundItem.EnumKey.Length > 0 && targetCompoundItem.ItemType is ItemType.CustomCompound)
                    {
                        if (targetCompoundItem.Plan.EnumCompoundDataDictionary.TryGetValue(targetCompoundItem.EnumKey, out Dictionary<string, List<string>> targetDictionary) && targetDictionary.TryGetValue(targetCompoundItem.SelectedEnumItem.Text.Trim('!'), out List<string> targetList))
                        {
                            CurrentChildrenStringList = [.. targetList];
                        }
                    }
                    else
                    if(compoundJsonTreeViewItem.EnumKey.Length > 0 && compoundJsonTreeViewItem.ItemType is ItemType.CustomCompound)
                    {
                        if (compoundJsonTreeViewItem.Plan.EnumCompoundDataDictionary.TryGetValue(compoundJsonTreeViewItem.EnumKey, out Dictionary<string, List<string>> targetDictionary) && targetDictionary.TryGetValue(compoundJsonTreeViewItem.SelectedEnumItem.Text.Trim('!'), out List<string> targetList))
                        {
                            CurrentChildrenStringList = [.. targetList];
                        }
                    }
                    else
                    if (targetCompoundItem.ItemType is ItemType.BottomButton && targetCompoundItem.Parent is not null)
                    {
                        CurrentChildrenStringList = targetCompoundItem.Parent.ListChildrenStringList;
                        if (CurrentChildrenStringList.Count == 0)
                        {
                            CurrentChildrenStringList = targetCompoundItem.Parent.ArrayChildrenStringList;
                        }
                    }
                }
                #endregion
            }
            #endregion

            #region 执行一系列添加操作
            if ((parentIsList || currentIsList || currentIsCompound) && CurrentChildrenStringList.Count > 0)
            {
                #region 提取需要分析的成员(仅提取子信息而不进行源码分析)
                if (parent is not null && parentItemType is ItemType.Compound && parent.DisplayText == "Entry" && previous is not null && previous is BaseCompoundJsonTreeViewItem previousCompoundItem && previousCompoundItem.EnumKey.Length > 0 && CurrentChildrenStringList.Count == 0)
                {
                    CurrentDependencyItemList = [..parent.Parent.CompoundChildrenStringList];
                }

                if (CurrentDependencyItemList.Count == 0)
                {
                    CurrentDependencyItemList = [..CurrentChildrenStringList];
                }
                if (compoundJsonTreeViewItem.CompoundChildrenStringList.Count == 1 && compoundJsonTreeViewItem.SelectedValueType is not null && compoundJsonTreeViewItem.SelectedValueType.Text != "List" && compoundJsonTreeViewItem.ValueTypeSource.Count > 0)
                {
                    Tuple<List<string>, bool> listOrCompoundResult = ExtractSubInformationFromPromptSourceCode(compoundJsonTreeViewItem);
                    CurrentDependencyItemList = listOrCompoundResult.Item1;
                }
                #endregion

                #region 执行解析
                string currentReferenceString = compoundJsonTreeViewItem.ItemType is ItemType.CustomCompound ? compoundJsonTreeViewItem.Value + "" : "";
                if(currentReferenceString.Length == 0 && compoundJsonTreeViewItem.SelectedEnumItem is not null && compoundJsonTreeViewItem.SelectedEnumItem.Text != "- unset -")
                {
                    currentReferenceString = compoundJsonTreeViewItem.SelectedEnumItem.Text;
                }
                result = htmlHelper.GetTreeViewItemResult(new(), CurrentDependencyItemList, compoundJsonTreeViewItem.LayerCount + 1, currentReferenceString, (currentItemType is not ItemType.BottomButton && currentItemType is not ItemType.CustomCompound) || (currentItemType is not ItemType.BottomButton && compoundJsonTreeViewItem.SelectedEnumItem is not null) ? compoundJsonTreeViewItem : parent, null, 1, true);

                htmlHelper.HandlingTheTypingAppearanceOfCompositeItemList([.. result.Result], compoundJsonTreeViewItem);

                while (result.ResultString.Length > 1 &&
                    (result.ResultString[^1] == ',' ||
                     result.ResultString[^1] == '\r' ||
                     result.ResultString[^1] == '\n' ||
                     result.ResultString[^1] == ' '))
                {
                    result.ResultString.Length--;
                }
                #endregion

                #region 处理子节点的增加
                if (currentItemType is ItemType.CustomCompound && parent is not null)
                {
                    parent.InsertChild(0, customItemCount, result);
                }
                else
                if (addListStructure || addParentListStructure)
                {
                    if (compoundJsonTreeViewItem.LogicChildren.Count == 0 && currentItemType is not ItemType.BottomButton)
                    {
                        compoundJsonTreeViewItem.LogicChildren.Add(GetAddToBottomButtonItem(compoundJsonTreeViewItem));
                    }

                    if (currentIsList || parentIsList)
                    {
                        if (currentItemType is not ItemType.BottomButton)
                        {
                            compoundJsonTreeViewItem.InsertChild(0,0, result);
                        }
                        else
                        {
                            compoundJsonTreeViewItem.Parent.InsertChild(parent.LogicChildren.Count - 1, 0, result);
                        }
                    }
                }
                else
                if (result.Result[0] is not BaseCompoundJsonTreeViewItem ||
                    (result.Result[0] is BaseCompoundJsonTreeViewItem firstCustomCompound && firstCustomCompound.ItemType is not ItemType.CustomCompound) || result.ResultString.Length > 0)
                {
                    compoundJsonTreeViewItem.AddChildrenList(result);
                }
                else
                {
                    compoundJsonTreeViewItem.LogicChildren.AddRange(result.Result);
                }
                #endregion

                #region 设置父级节点的行引用
                if(compoundJsonTreeViewItem.IsCanBeDefaulted)
                {
                    if(parent is not null)
                    {
                        parent.SetVisualPreviousAndNextForEachItem();
                        Tuple<JsonTreeViewItem,JsonTreeViewItem> previousAndNext = parent.SearchVisualPreviousAndNextItem(parent.LogicChildren[0], false);
                        if(previousAndNext is not null && previousAndNext.Item2 is not null)
                        {
                            parent.VisualLastChild = previousAndNext.Item2;
                        }
                        if(parent.VisualLastChild is not null && (parent.StartLine == parent.EndLine || (parent.EndLine is null || (parent.EndLine is not null && !parent.EndLine.IsDeleted))))
                        {
                            if(parent.VisualLastChild is BaseCompoundJsonTreeViewItem lastCompoundItem && lastCompoundItem.EndLine is not null && !lastCompoundItem.EndLine.IsDeleted)
                            {
                                parent.EndLine = lastCompoundItem.EndLine.NextLine;
                            }
                            else
                            {
                                parent.EndLine = parent.VisualLastChild.StartLine.NextLine;
                            }
                        }
                    }
                    else
                    if (compoundJsonTreeViewItem.Plan is BaseCustomWorldUnifiedPlan basePlan)
                    {
                        basePlan.SetVisualPreviousAndNextForEachItem();
                        Tuple<JsonTreeViewItem, JsonTreeViewItem> previousAndNext = basePlan.SearchVisualPreviousAndNextItem(basePlan.TreeViewItemList[0], false);
                        if (previousAndNext is not null && previousAndNext.Item2 is not null)
                        {
                            basePlan.VisualLastItem = previousAndNext.Item2;
                        }
                    }
                }
                #endregion

                #region 显示换位按钮
                if (currentIsList && compoundJsonTreeViewItem.LogicChildren.Count > 2)
                {
                    compoundJsonTreeViewItem.LogicChildren[0].SortButtonVisibility = Visibility.Visible;
                    if (compoundJsonTreeViewItem.LogicChildren.Count == 3)
                    {
                        compoundJsonTreeViewItem.LogicChildren[^2].SortButtonVisibility = Visibility.Visible;
                    }
                }
                if (parentIsList && !currentIsList && parent is not null && parent.LogicChildren.Count > 2)
                {
                    result.Result[0].SortButtonVisibility = Visibility.Visible;
                    if (parent.LogicChildren.Count == 3)
                    {
                        parent.LogicChildren[0].SortButtonVisibility = Visibility.Visible;
                    }
                }
                #endregion
            }
            #endregion
        }

        public void CollapseCurrentItem(BaseCompoundJsonTreeViewItem compoundJsonTreeViewItem)
        {
            if (compoundJsonTreeViewItem.IsCanBeDefaulted)
            {
                if (compoundJsonTreeViewItem.Parent is not null)
                {
                    compoundJsonTreeViewItem.Parent.RemoveChild([compoundJsonTreeViewItem], false);
                }
                else
                if (compoundJsonTreeViewItem.Plan is BaseCustomWorldUnifiedPlan basePlan)
                {
                    basePlan.RemoveChild([compoundJsonTreeViewItem]);
                }
            }
            else
            {
                int offset = compoundJsonTreeViewItem.StartLine.EndOffset;
                string compoundEndLineText = compoundJsonTreeViewItem.Plan.GetRangeText(compoundJsonTreeViewItem.EndLine.Offset, compoundJsonTreeViewItem.EndLine.Length);
                int lastCharIndex = compoundEndLineText.LastIndexOf(']');
                if (lastCharIndex == -1)
                {
                    lastCharIndex = compoundEndLineText.LastIndexOf('}');
                }
                int length = compoundJsonTreeViewItem.EndLine.Offset + lastCharIndex - offset;
                compoundJsonTreeViewItem.Plan.SetRangeText(offset, length, "");
                compoundJsonTreeViewItem.EndLine = compoundJsonTreeViewItem.StartLine;
            }

            if(compoundJsonTreeViewItem.Parent is not null)
            {
                compoundJsonTreeViewItem.Parent.SetVisualPreviousAndNextForEachItem();
            }
            else
            if(compoundJsonTreeViewItem.Plan is BaseCustomWorldUnifiedPlan basePlan)
            {
                basePlan.SetVisualPreviousAndNextForEachItem();
            }
        }

        public void RecursiveTraverseAndRunOperate(ObservableCollection<JsonTreeViewItem> jsonItemList, Action<JsonTreeViewItem> action)
        {
            foreach (var item in jsonItemList)
            {
                action.Invoke(item);
                if (item is BaseCompoundJsonTreeViewItem compoundJsonTreeViewItem)
                {
                    RecursiveTraverseAndRunOperate(jsonItemList, action);
                }
            }
        }
        #endregion
    }
}