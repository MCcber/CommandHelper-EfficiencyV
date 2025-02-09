using cbhk.CustomControls.JsonTreeViewComponents;
using cbhk.Interface.Json;
using cbhk.Model.Common;
using Prism.Ioc;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using static cbhk.Model.Common.Enums;

namespace cbhk.GeneralTools.TreeViewComponentsHelper
{
    public partial class JsonTreeViewItemExtension(IContainerProvider container) : IJsonItemTool
    {
        #region Field
        private IContainerProvider _container = container;

        [GeneratedRegex(@"\[\[\#(?<1>[\u4e00-\u9fff]+)\|(?<2>[\u4e00-\u9fff]+)\]\]")]
        private static partial Regex GetContextKey();

        [GeneratedRegex(@"\r\n")]
        private static partial Regex GetLineBreakCount();
        #endregion

        public void RecursiveRemoveFlattenDescendantNodeList(CompoundJsonTreeViewItem CurrentParent, CompoundJsonTreeViewItem Target)
        {
            foreach (var item in Target.Children)
            {
                CurrentParent.FlattenDescendantNodeList.Remove(item);
                if (item is CompoundJsonTreeViewItem compoundJsonTreeViewItem)
                {
                    RecursiveRemoveFlattenDescendantNodeList(compoundJsonTreeViewItem.Parent, compoundJsonTreeViewItem);
                }
            }
        }

        public void SetLineNumbersForEachItem(ObservableCollection<JsonTreeViewItem> list,CompoundJsonTreeViewItem parent,bool withType = false)
        {
            int index = 0;

            if(parent is null)
            {
                index = 1;
            }

            if(withType)
            {
                index = 1;
                withType = false;
            }

            for (int i = 0; i < list.Count; i++)
            {
                list[i].Parent = parent;
                if (list[i].IsCanBeDefaulted)
                {
                    continue;
                }

                if (parent is not null)
                {
                    list[i].StartLine = parent.Plan.GetLineByNumber(index + 1 + parent.StartLine.LineNumber);
                }
                else
                if (list[i] is CompoundJsonTreeViewItem subCompoundItem1 && ((subCompoundItem1.DataType is not DataType.OptionalCompound) || (subCompoundItem1.DataType is DataType.OptionalCompound && subCompoundItem1.Children.Count > 0)))
                {
                    list[i].StartLine = list[i].Plan.GetLineByNumber(index + 1);
                }

                if (list[i] is CompoundJsonTreeViewItem subCompoundItem2)
                {
                    if (subCompoundItem2.Children.Count > 0)
                    {
                        SetLineNumbersForEachItem(subCompoundItem2.Children, subCompoundItem2);
                        if (subCompoundItem2.Children[^1] is CompoundJsonTreeViewItem subsubCompoundItem2)
                        {
                            subCompoundItem2.EndLine = parent.Plan.GetLineByNumber(subsubCompoundItem2.EndLine.LineNumber + 1);
                        }
                        else
                        {
                            subCompoundItem2.EndLine = parent.Plan.GetLineByNumber(subCompoundItem2.Children[^1].StartLine.LineNumber + 1);
                        }
                    }
                    else
                    {
                        subCompoundItem2.EndLine = subCompoundItem2.StartLine;
                    }
                }
                index++;
            }
        }

        public void AddSubStructure(CompoundJsonTreeViewItem compoundJsonTreeViewItem)
        {
            #region 字段
            bool currentIsArray = compoundJsonTreeViewItem.DataType is DataType.List ||
                compoundJsonTreeViewItem.DataType is DataType.Array ||
                compoundJsonTreeViewItem.DataType is DataType.InnerArray;
            bool parentIsArray = compoundJsonTreeViewItem.Parent is not null && (
                compoundJsonTreeViewItem.Parent.DataType is DataType.List || 
                compoundJsonTreeViewItem.Parent.DataType is DataType.Array || 
                compoundJsonTreeViewItem.Parent.DataType is DataType.InnerArray ||
                compoundJsonTreeViewItem.Parent.DataType is DataType.List);

            bool currentIsCompound = compoundJsonTreeViewItem.DataType is DataType.Compound ||
                compoundJsonTreeViewItem.DataType is DataType.CustomCompound ||
                compoundJsonTreeViewItem.DataType is DataType.NullableCompound ||
                compoundJsonTreeViewItem.DataType is DataType.OptionalCompound;
            bool IsArrayOrList = currentIsArray || parentIsArray;
            JsonTreeViewDataStructure result = new();
            List<string> CurrentDependencyItemList = [];
            List<string> NBTFeatureList = [];
            ChangeType changeType = ChangeType.String;
            CompoundJsonTreeViewItem? targetCompoundItem;
            if (currentIsArray || currentIsCompound)
            {
                targetCompoundItem = compoundJsonTreeViewItem;
            }
            else
            if (parentIsArray)
            {
                targetCompoundItem = compoundJsonTreeViewItem.Parent;
            }
            if (currentIsCompound)
            {
                changeType = ChangeType.AddCompoundObject;
            }
            else
            {
                changeType = ChangeType.AddArrayElement;
            }

            HtmlHelper htmlHelper = new(_container)
            {
                plan = compoundJsonTreeViewItem.Plan,
                jsonTool = compoundJsonTreeViewItem.JsonItemTool
            };
            #endregion

            #region 定位相邻的已有值的两个节点
            JsonTreeViewItem previous = compoundJsonTreeViewItem.Previous;
            CompoundJsonTreeViewItem parent = compoundJsonTreeViewItem.Parent;
            JsonTreeViewItem next = compoundJsonTreeViewItem.Next;
            CompoundJsonTreeViewItem previousCompound = null;
            CompoundJsonTreeViewItem nextCompound = null;
            while (previous is not null && previous.StartLine is null)
            {
                if (previous.Previous is null)
                {
                    break;
                }
                previous = previous.Previous;
            }

            if (previous is not null && previous.StartLine is null)
            {
                while (parent is not null && parent.StartLine is null)
                {
                    if (parent.Parent is null)
                    {
                        break;
                    }
                    parent = parent.Parent;
                }
            }

            while (next is not null && next.StartLine is null)
            {
                if (next.Next is null)
                {
                    break;
                }
                next = next.Next;
            }

            if (previous is CompoundJsonTreeViewItem)
            {
                previousCompound = previous as CompoundJsonTreeViewItem;
            }
            if (next is CompoundJsonTreeViewItem)
            {
                nextCompound = next as CompoundJsonTreeViewItem;
            }
            #endregion

            #region 实例化复合节点子链表信息
            if (currentIsCompound && compoundJsonTreeViewItem.ChildrenStringList.Count > 0)
            {
                #region 定义前置衔接符
                string connectorSymbol = "";
                if(previous is not null && previous.StartLine is not null && (next is null || (next is not null && next.StartLine is null)))
                {
                    connectorSymbol = ",\r\n";
                }
                else
                if(compoundJsonTreeViewItem.StartLine == compoundJsonTreeViewItem.EndLine)
                {
                    connectorSymbol = "\r\n";
                }
                #endregion

                #region 提取需要分析的成员
                if (compoundJsonTreeViewItem.ChildrenStringList.Count == 1)
                {
                    Match contextMatch = GetContextKey().Match(compoundJsonTreeViewItem.ChildrenStringList[0]);
                    if (contextMatch.Success)
                    {
                        string contextKey = ("#" + contextMatch.Groups[1].Value + "|" + contextMatch.Groups[2].Value);
                        if (compoundJsonTreeViewItem.Plan.CurrentDependencyItemList.TryGetValue(contextKey, out List<string> targetList1))
                        {
                            CurrentDependencyItemList = targetList1;
                        }
                        else
                        {
                            string targetKey = compoundJsonTreeViewItem.Plan.TranslateDictionary[contextKey];
                            if (compoundJsonTreeViewItem.Plan.CurrentDependencyItemList.TryGetValue(targetKey, out List<string> targetList2))
                            {
                                CurrentDependencyItemList = targetList2;
                            }
                        }
                    }
                }
                else
                if(compoundJsonTreeViewItem.ChildrenStringList.Count > 1)
                {
                    CurrentDependencyItemList = compoundJsonTreeViewItem.ChildrenStringList;
                }

                if (CurrentDependencyItemList.Count == 0)
                {
                    CurrentDependencyItemList = compoundJsonTreeViewItem.ChildrenStringList;
                }
                #endregion

                #region 提取NBT类型与Key并去除用于Wiki页面显示的一些UI标记
                NBTFeatureList = htmlHelper.GetHeadTypeAndKeyList(CurrentDependencyItemList[0]);
                if (NBTFeatureList is not null && NBTFeatureList.Count > 0)
                {
                    for (int j = NBTFeatureList.Count - 1; j > 0; j--)
                    {
                        if (NBTFeatureList[j].Contains('='))
                        {
                            NBTFeatureList.RemoveAt(j);
                        }
                    }
                }
                string currentNodeKey = NBTFeatureList[^1];
                #endregion

                #region 执行分析
                result = htmlHelper.GetTreeViewItemResult(new(), CurrentDependencyItemList, compoundJsonTreeViewItem.LayerCount + (compoundJsonTreeViewItem.ChildrenStringList.Count == 1 && currentNodeKey == compoundJsonTreeViewItem.Key ? 0 : 1), (string)compoundJsonTreeViewItem.Value, compoundJsonTreeViewItem, null, 1, true);

                if (result.Result.Count == 1 && result.Result[0] is CompoundJsonTreeViewItem firstCompoundItem1 && firstCompoundItem1.DataType is DataType.CustomCompound)
                {
                    firstCompoundItem1.AddElementButtonVisibility = Visibility.Collapsed;
                    firstCompoundItem1.RemoveElementButtonVisibility = Visibility.Visible;
                }
                #endregion

                #region 添加当前节点的子节点集
                if (result.Result.Count == 1 && result.Result[0] is CompoundJsonTreeViewItem CurrentSubDependencyItem && CurrentSubDependencyItem.Children.Count > 0)
                {
                    if (CurrentSubDependencyItem.DataType is not DataType.CustomCompound)
                    {
                        compoundJsonTreeViewItem.Children.AddRange(CurrentSubDependencyItem.Children);
                    }
                    else
                    {
                        compoundJsonTreeViewItem.Children.Insert(0,CurrentSubDependencyItem);
                    }
                }
                else
                {
                    compoundJsonTreeViewItem.Children.AddRange(result.Result);
                }
                #endregion

                #region 更新代码编辑器
                if (result.ResultString.Length > 0)
                {
                    int endOffset;
                    //替换前分离首末行的引用避免执行替换时首行产生变动
                    if(compoundJsonTreeViewItem.StartLine is null && previousCompound is not null && previousCompound.EndLine is not null)
                    {
                        compoundJsonTreeViewItem.StartLine = compoundJsonTreeViewItem.Plan.GetLineByNumber(previousCompound.EndLine.LineNumber + 1);
                    }
                    else
                    if(compoundJsonTreeViewItem.StartLine is null && previous is not null && previous.StartLine is not null)
                    {
                        compoundJsonTreeViewItem.StartLine = compoundJsonTreeViewItem.Plan.GetLineByNumber(previous.StartLine.LineNumber + 1);
                    }

                    compoundJsonTreeViewItem.StartLine ??= compoundJsonTreeViewItem.Plan.GetLineByNumber(2);

                    if(compoundJsonTreeViewItem.StartLine is null)
                    {
                        return;
                    }

                    int startLineNumber = compoundJsonTreeViewItem.StartLine is not null ? compoundJsonTreeViewItem.StartLine.LineNumber : 2;
                    int endlineNumber = compoundJsonTreeViewItem.EndLine is not null ? compoundJsonTreeViewItem.EndLine.LineNumber : 2;
                    compoundJsonTreeViewItem.EndLine = null;

                    #region 整理最终拼接字符串
                    string currentNewString;
                    bool endWithComma = result.ResultString.ToString().TrimEnd(' ').EndsWith("\r\n");
                    if (startLineNumber != endlineNumber)
                    {
                        //if (result.Result[0] is CompoundJsonTreeViewItem addedChildItem && addedChildItem.DataType is DataType.CustomCompound)
                        //{
                        //    currentNewString = "\r\n" + result.ResultString.ToString() + ",";
                        //}
                        //else
                        //{
                            currentNewString = connectorSymbol + result.ResultString.ToString() + (next is not null && next.StartLine is not null ? ',' : "");
                        //}
                    }
                    else
                    {
                        currentNewString = connectorSymbol + result.ResultString.ToString() + (compoundJsonTreeViewItem.DataType is not DataType.OptionalCompound && !endWithComma ? "\r\n" : "") + new string(' ', compoundJsonTreeViewItem.LayerCount * 2) + (next is not null && next.StartLine is not null ? ',' : "");
                    }
                    #endregion

                    #region 执行替换
                    if (previous is CompoundJsonTreeViewItem previousCompoundItem && compoundJsonTreeViewItem.DataType is DataType.OptionalCompound)
                    {
                        endOffset = previousCompoundItem.EndLine is not null ? previousCompoundItem.EndLine.EndOffset : previousCompoundItem.StartLine.EndOffset;
                        compoundJsonTreeViewItem.StartLine = null;
                        compoundJsonTreeViewItem.Plan.UpdateNullValueBySpecifyingInterval(endOffset, currentNewString);
                    }
                    else
                    {
                        compoundJsonTreeViewItem.Plan.UpdateValueBySpecifyingInterval(compoundJsonTreeViewItem, changeType, currentNewString);
                    }
                    #endregion

                    #region 确定起始行号
                    int newLineCount = 0;
                    if (compoundJsonTreeViewItem.Children.Count == 1)
                    {
                        newLineCount = GetLineBreakCount().Matches(currentNewString).Count + (!string.IsNullOrEmpty(connectorSymbol) ? -1 : 0) + (result.Result[^1] is CompoundJsonTreeViewItem lastCompoundChildItem && (lastCompoundChildItem.DataType is not DataType.OptionalCompound ||
                            lastCompoundChildItem.ValueTypeList.Select(item => item.Text.Contains("compound")).Count() > 0) && result.Result[^1].Key != compoundJsonTreeViewItem.Key ? 1 : 0);
                    }
                    if (compoundJsonTreeViewItem.StartLine is null)
                    {
                        if (previous is not null)
                        {
                            if (previousCompound.EndLine is not null)
                            {
                                compoundJsonTreeViewItem.StartLine = compoundJsonTreeViewItem.Plan.GetLineByNumber(previousCompound.EndLine.LineNumber + 1);
                            }
                            else
                            {
                                compoundJsonTreeViewItem.StartLine = compoundJsonTreeViewItem.Plan.GetLineByNumber(previous.StartLine.LineNumber + 1);
                            }
                        }
                        else
                        {
                            compoundJsonTreeViewItem.StartLine = compoundJsonTreeViewItem.Plan.GetLineByNumber(2);
                        }
                    }
                    #endregion

                    #region 确定首次添加子级时的末尾行号
                    if (compoundJsonTreeViewItem.Children.Count == 1)
                    {
                        compoundJsonTreeViewItem.EndLine = compoundJsonTreeViewItem.Plan.GetLineByNumber(compoundJsonTreeViewItem.StartLine.LineNumber + newLineCount);
                    }
                    #endregion
                }
                else
                {
                    //执行替换
                    if (previousCompound.EndLine is not null)
                    {
                        compoundJsonTreeViewItem.Plan.UpdateNullValueBySpecifyingInterval(previousCompound.EndLine.EndOffset, ",\r\n" + new string(' ',compoundJsonTreeViewItem.LayerCount * 2) + "\"" + compoundJsonTreeViewItem.Key + "\": {}");
                        compoundJsonTreeViewItem.StartLine = compoundJsonTreeViewItem.Plan.GetLineByNumber(previousCompound.EndLine.LineNumber + 1);
                    }
                    else
                    {
                        compoundJsonTreeViewItem.Plan.UpdateNullValueBySpecifyingInterval(previousCompound.StartLine.EndOffset, ",\r\n" + new string(' ', compoundJsonTreeViewItem.LayerCount * 2) + "\"" + compoundJsonTreeViewItem.Key + "\": {}");
                        compoundJsonTreeViewItem.StartLine = compoundJsonTreeViewItem.Plan.GetLineByNumber(previousCompound.StartLine.LineNumber + 1);
                    }
                }
                #endregion

                #region 将子结构行号与代码编辑器对齐
                ObservableCollection<JsonTreeViewItem> subDependencyItemList = [];
                if (result.Result[0] is CompoundJsonTreeViewItem firstCompoundItem2 && firstCompoundItem2.DataType is not DataType.CustomCompound)
                {
                    subDependencyItemList = firstCompoundItem2.Children;
                }

                if (subDependencyItemList.Count == 0)
                {
                    subDependencyItemList = result.Result;
                }
                SetLineNumbersForEachItem(subDependencyItemList, compoundJsonTreeViewItem);
                #endregion

                #region 确认已有子级时的末尾行号
                if (compoundJsonTreeViewItem.Children.Count > 1)
                {
                    if (compoundJsonTreeViewItem.Children[^1] is CompoundJsonTreeViewItem lastChildItem1 && lastChildItem1.EndLine is not null)
                    {
                        compoundJsonTreeViewItem.EndLine = compoundJsonTreeViewItem.Plan.GetLineByNumber(lastChildItem1.EndLine.LineNumber + 1);
                    }
                    else
                    if (compoundJsonTreeViewItem.Children[^1] is CompoundJsonTreeViewItem lastChildItem2 && lastChildItem2.EndLine is not null)
                    {
                        compoundJsonTreeViewItem.EndLine = compoundJsonTreeViewItem.Plan.GetLineByNumber(lastChildItem2.StartLine.LineNumber + 1);
                    }

                    compoundJsonTreeViewItem.EndLine ??= compoundJsonTreeViewItem.StartLine;
                }
                #endregion

                #region 根据当前节点类型判断是否切换功能按钮
                if ((compoundJsonTreeViewItem.Children[0] is CompoundJsonTreeViewItem childItem && childItem.DataType is not DataType.CustomCompound) || compoundJsonTreeViewItem.Children[0] is not CompoundJsonTreeViewItem)
                {
                    compoundJsonTreeViewItem.AddElementButtonVisibility = Visibility.Collapsed;
                    compoundJsonTreeViewItem.RemoveElementButtonVisibility = Visibility.Visible;
                }
                #endregion

            }
            #endregion

            #region 实例化数组/列表节点子链表信息
            if ((currentIsArray && compoundJsonTreeViewItem.ChildrenStringList.Count > 0) || 
                (parentIsArray && compoundJsonTreeViewItem.Parent is not null && compoundJsonTreeViewItem.Parent.ChildrenStringList.Count > 0))
            {
                string connectorSymbol = (currentIsArray && previous is not null) || (parentIsArray && parent is not null) ? ",\r\n" : "";
                result = htmlHelper.GetTreeViewItemResult(new(), compoundJsonTreeViewItem.ChildrenStringList, compoundJsonTreeViewItem.LayerCount + 1, "", compoundJsonTreeViewItem, null, 1, true);
                string currentNewString = result.ResultString.ToString();
                CompoundJsonTreeViewItem entry = new(compoundJsonTreeViewItem.Plan, compoundJsonTreeViewItem.JsonItemTool,_container)
                {
                    Path = currentIsArray ? compoundJsonTreeViewItem.Path + "[" + 0 + "]" : compoundJsonTreeViewItem.Parent.Path + "[" + (compoundJsonTreeViewItem.Parent.Children.Count - 1) + "]",
                    Parent = currentIsCompound ? compoundJsonTreeViewItem : compoundJsonTreeViewItem.Parent,
                    DisplayText = "Entry",
                    DataType = DataType.ArrayElement,
                    LayerCount = compoundJsonTreeViewItem.LayerCount + 1,
                    RemoveElementButtonVisibility = Visibility.Visible
                };
                entry.Children.AddRange(result.Result);
                if(compoundJsonTreeViewItem.DataType is DataType.Array || compoundJsonTreeViewItem.DataType is DataType.InnerArray)
                {
                    compoundJsonTreeViewItem.Children.Insert(0, entry);
                }
                else
                {
                    compoundJsonTreeViewItem.Parent.Children.Add(entry);
                }

                #region 更新代码编辑器
                int endOffset = 0;
                if (currentIsArray)
                {
                    endOffset = compoundJsonTreeViewItem.StartLine.EndOffset;
                }
                else
                if(parentIsArray)
                {
                    endOffset = compoundJsonTreeViewItem.Plan.GetLineByNumber(compoundJsonTreeViewItem.EndLine.LineNumber - 1).EndOffset;
                }
                compoundJsonTreeViewItem.Plan.SetRangeText(endOffset, 0, connectorSymbol + new string(' ', compoundJsonTreeViewItem.LayerCount + 1) + "{\r\n" + new string(' ', compoundJsonTreeViewItem.LayerCount + 2) + currentNewString + "\r\n" + new string(' ', compoundJsonTreeViewItem.LayerCount + 1) + "}");
                #endregion

                #region 将子结构行号与代码编辑器对齐
                if (currentIsArray)
                {
                    entry.StartLine = compoundJsonTreeViewItem.Plan.GetLineByNumber(compoundJsonTreeViewItem.StartLine.LineNumber + 1);
                    entry.EndLine = compoundJsonTreeViewItem.Plan.GetLineByNumber(compoundJsonTreeViewItem.Children[1].StartLine.LineNumber - 1);
                }
                else
                if (parentIsArray)
                {
                    entry.StartLine = compoundJsonTreeViewItem.Plan.GetLineByNumber((compoundJsonTreeViewItem.Children[^1] as CompoundJsonTreeViewItem).EndLine.LineNumber + 1);
                    entry.EndLine = compoundJsonTreeViewItem.Plan.GetLineByNumber(compoundJsonTreeViewItem.Parent.EndLine.LineNumber - 1);
                }
                SetLineNumbersForEachItem(entry.Children, entry);
                #endregion
            }
            #endregion

            #region 更新父节点的末行引用
            if (compoundJsonTreeViewItem.Parent is not null && compoundJsonTreeViewItem.Parent.Children.Count == 1)
            {
                compoundJsonTreeViewItem.Parent.EndLine = compoundJsonTreeViewItem.Plan.GetLineByNumber(compoundJsonTreeViewItem.EndLine.LineNumber + 1);
            }
            #endregion
        }

        /// <summary>
        /// 删除数组元素
        /// </summary>
        /// <param name="compoundJsonTreeViewItem">当前数组节点</param>
        public void RemoveSubStructure(CompoundJsonTreeViewItem compoundJsonTreeViewItem)
        {
            #region 确定删除的数据类型
            ChangeType changeType = ChangeType.String;
            if (compoundJsonTreeViewItem.DataType is DataType.ArrayElement)
                changeType = ChangeType.RemoveArrayElement;
            else
                if (compoundJsonTreeViewItem.DataType is DataType.Compound || compoundJsonTreeViewItem.DataType is DataType.CustomCompound)
                changeType = ChangeType.RemoveCompound;
            #endregion

            #region 更新TreeView与JsonView
            Parallel.ForEach(compoundJsonTreeViewItem.Plan.KeyValueContextDictionary.Keys, item =>
            {
                if (item.Contains(compoundJsonTreeViewItem.Path))
                {
                    compoundJsonTreeViewItem.Plan.KeyValueContextDictionary.Remove(item);
                }
            });
            compoundJsonTreeViewItem.Plan.UpdateValueBySpecifyingInterval(compoundJsonTreeViewItem, changeType);
            if (changeType is ChangeType.RemoveCompound)
                compoundJsonTreeViewItem.Children.Clear();
            if (changeType is ChangeType.RemoveArrayElement)
            {
                compoundJsonTreeViewItem.Parent.Children.Remove(compoundJsonTreeViewItem);
                compoundJsonTreeViewItem.Parent.FlattenDescendantNodeList.Remove(compoundJsonTreeViewItem);
                if (compoundJsonTreeViewItem.Parent.Children.Count == 1)
                    compoundJsonTreeViewItem.Parent.Children.Clear();
            }
            #endregion

            #region 更新数组元素前后关系
            if (compoundJsonTreeViewItem.Previous is not null)
                compoundJsonTreeViewItem.Previous.Next = compoundJsonTreeViewItem.Next;
            if (compoundJsonTreeViewItem.Next is not null)
                compoundJsonTreeViewItem.Next.Previous = compoundJsonTreeViewItem.Previous;
            #endregion

            RecursiveRemoveFlattenDescendantNodeList(compoundJsonTreeViewItem.Parent, compoundJsonTreeViewItem);
        }

        public void RecursiveTraverseAndRunOperate(ObservableCollection<JsonTreeViewItem> jsonItemList, Action<JsonTreeViewItem> action)
        {
            foreach (var item in jsonItemList)
            {
                action.Invoke(item);
                if (item is CompoundJsonTreeViewItem compoundJsonTreeViewItem)
                {
                    RecursiveTraverseAndRunOperate(jsonItemList, action);
                }
            }
        }

        public void UpdateFlattenDescendantNodeList(CompoundJsonTreeViewItem target, ObservableCollection<JsonTreeViewItem> list)
        {
            foreach (JsonTreeViewItem item in list)
            {
                target.FlattenDescendantNodeList.Add(item);
                //if (previousItem is CompoundJsonTreeViewItem compoundJsonTreeViewItem)
                //{
                //    target.FlattenDescendantNodeList.AddRange(compoundJsonTreeViewItem.FlattenDescendantNodeList);
                //}
            }
            if (target.Parent is not null)
            {
                UpdateFlattenDescendantNodeList(target.Parent, list);
            }
        }
    }
}