using cbhk.CustomControls.Interfaces;
using cbhk.CustomControls.JsonTreeViewComponents;
using cbhk.Interface.Json;
using cbhk.Model.Common;
using DryIoc;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using Prism.Ioc;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

        /// <summary>
        /// 根据当前行号获取节点自身所在的起始和末尾行对象
        /// </summary>
        /// <param name="item"></param>
        /// <param name="textEditor"></param>
        public void SetDocumentLineByLineNumber(JsonTreeViewItem item, TextEditor textEditor)
        {
            if (item.StartLineNumber > textEditor.Document.LineCount || item.StartLineNumber <= 0)
            {
                return;
            }
            item.StartLine = textEditor.Document.GetLineByNumber(item.StartLineNumber);
            item.StartLineNumber = 0;
            if (item is CompoundJsonTreeViewItem compoundJsonTreeViewItem && compoundJsonTreeViewItem.EndLineNumber > 0)
            {
                compoundJsonTreeViewItem.EndLine = textEditor.Document.GetLineByNumber(compoundJsonTreeViewItem.EndLineNumber);
                compoundJsonTreeViewItem.EndLineNumber = 0;
                if (compoundJsonTreeViewItem.Children.Count > 0)
                {
                    foreach (var subItem in compoundJsonTreeViewItem.Children)
                    {
                        SetDocumentLineByLineNumber(subItem, textEditor);
                    }
                }
            }
        }

        /// <summary>
        /// 添加数组元素
        /// </summary>
        /// <param name="compoundJsonTreeViewItem">数组节点</param>
        public void AddSubStructure(CompoundJsonTreeViewItem compoundJsonTreeViewItem, UIElement element)
        {
            #region 字段
            bool currentIsArray = compoundJsonTreeViewItem.DataType is DataTypes.Array ||
                compoundJsonTreeViewItem.DataType is DataTypes.InnerArray;
            bool parentIsArray = compoundJsonTreeViewItem.Parent is not null && (compoundJsonTreeViewItem.Parent.DataType is DataTypes.Array || compoundJsonTreeViewItem.Parent.DataType is DataTypes.InnerArray ||
                compoundJsonTreeViewItem.Parent.DataType is DataTypes.List);

            bool currentIsCompound = compoundJsonTreeViewItem.DataType is DataTypes.Compound ||
                compoundJsonTreeViewItem.DataType is DataTypes.CustomCompound ||
                compoundJsonTreeViewItem.DataType is DataTypes.NullableCompound ||
                compoundJsonTreeViewItem.DataType is DataTypes.OptionalCompound;
            bool IsArrayOrList = currentIsArray || parentIsArray;
            JsonTreeViewDataStructure result = new();
            ReplaceType replaceType = ReplaceType.Direct;
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
                replaceType = ReplaceType.Compound;
            }
            else
            {
                replaceType = ReplaceType.AddArrayElement;
            }
            #endregion

            #region 定位上一个已有值的节点
            JsonTreeViewItem previous = compoundJsonTreeViewItem.Previous;
            while (previous.StartLineNumber == 0)
            {
                if (previous.Previous is null)
                {
                    break;
                }
                previous = previous.Previous;
            }
            #endregion

            #region 实例化复合节点子链表信息
            if (currentIsCompound && compoundJsonTreeViewItem.ChildrenStringList.Count > 0)
            {
                HtmlHelper htmlHelper = _container.Resolve<HtmlHelper>();

                //当只有一个子结构原始字符串成员时，分析能够提取引用
                if (compoundJsonTreeViewItem.ChildrenStringList.Count == 1)
                {
                    Match contextMatch = GetContextKey().Match(compoundJsonTreeViewItem.ChildrenStringList[0]);
                    if (contextMatch.Success)
                    {
                        string contextKey = ("#" + contextMatch.Groups[1].Value + "|" + contextMatch.Groups[2].Value).Replace("|上文", "").Replace("|下文", "");
                        List<string> CurrentDependencyItemList = compoundJsonTreeViewItem.Plan.CurrentDependencyItemList[contextKey];
                        int subLineNumber = previous is CompoundJsonTreeViewItem previousItem && previousItem.EndLine is not null ? previousItem.EndLine.LineNumber + 1 : previous.StartLine.LineNumber + 1;
                        result = htmlHelper.GetTreeViewItemResult(new(), CurrentDependencyItemList, subLineNumber, compoundJsonTreeViewItem.LayerCount + 1, "", compoundJsonTreeViewItem, null, 1, true);

                        #region 添加当前节点的子节点集
                        if (result.Result[0] is CompoundJsonTreeViewItem CurrentSubDependencyItem && CurrentSubDependencyItem.Children.Count > 0)
                        {
                            compoundJsonTreeViewItem.Children.AddRange(CurrentSubDependencyItem.Children);
                        }
                        #endregion

                        #region 更新代码编辑器
                        if (result.ResultString.Length > 0)
                        {
                            int endOffset;
                            string currentNewString = result.ResultString.ToString();

                            if (previous is CompoundJsonTreeViewItem previousCompoundItem)
                            {
                                endOffset = previousCompoundItem.EndLine is not null ? previousCompoundItem.EndLine.EndOffset : previousCompoundItem.StartLine.EndOffset;
                                compoundJsonTreeViewItem.Plan.UpdateNullValueBySpecifyingInterval(endOffset, ",\r\n" + currentNewString);
                            }
                            else
                            {
                                if (previous is not null)
                                {
                                    endOffset = previous.StartLine.EndOffset;
                                    compoundJsonTreeViewItem.Plan.UpdateNullValueBySpecifyingInterval(endOffset, ",\r\n" + currentNewString);
                                }
                                else
                                {
                                    endOffset = compoundJsonTreeViewItem.Plan.GetLineByNumber(2).EndOffset;
                                    compoundJsonTreeViewItem.Plan.UpdateNullValueBySpecifyingInterval(endOffset, ",\r\n" + currentNewString);
                                }
                            }
                            int newLineCount = GetLineBreakCount().Matches(currentNewString).Count;
                            #region 确定起始行号
                            if (compoundJsonTreeViewItem.StartLine is null)
                            {
                                if (previous is not null)
                                {
                                    if (previous is CompoundJsonTreeViewItem item && item.EndLine is not null)
                                    {
                                        compoundJsonTreeViewItem.StartLine = compoundJsonTreeViewItem.Plan.GetLineByNumber(item.EndLine.LineNumber + 1);
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
                            #region 确定末尾行号
                            compoundJsonTreeViewItem.EndLine = compoundJsonTreeViewItem.Plan.GetLineByNumber(compoundJsonTreeViewItem.StartLine.LineNumber + newLineCount + 1);
                            #endregion
                        }
                        #endregion
                    }
                }
                else
                if(compoundJsonTreeViewItem.ChildrenStringList.Count > 1)
                {
                    result = htmlHelper.GetTreeViewItemResult(new(), compoundJsonTreeViewItem.ChildrenStringList, compoundJsonTreeViewItem.StartLine.LineNumber + 1, compoundJsonTreeViewItem.LayerCount + 1, compoundJsonTreeViewItem.Key, compoundJsonTreeViewItem, null, 1, true);
                }

                #region 将子结构行号与代码编辑器对齐
                ObservableCollection<JsonTreeViewItem> subDependencyItemList = (result.Result[0] as CompoundJsonTreeViewItem).Children;
                int index = 0;
                for (int i = 0; i < subDependencyItemList.Count; i++)
                {
                    if (subDependencyItemList[i].IsCanBeDefaulted)
                    {
                        continue;
                    }
                    subDependencyItemList[i].StartLine = compoundJsonTreeViewItem.Plan.GetLineByNumber(index + 1 + compoundJsonTreeViewItem.StartLine.LineNumber);
                    index++;
                }
                #endregion

                compoundJsonTreeViewItem.AddElementButtonVisibility = Visibility.Collapsed;
                compoundJsonTreeViewItem.RemoveElementButtonVisibility = Visibility.Visible;

                return;
            }
            #endregion

            #region 实例化数组/列表节点子链表信息
            if ((currentIsArray && compoundJsonTreeViewItem.ChildrenStringList.Count > 0) || 
                (parentIsArray && compoundJsonTreeViewItem.Parent is not null && compoundJsonTreeViewItem.Parent.ChildrenStringList.Count > 0))
            {
                string currentNewString = result.ResultString.ToString();
                CompoundJsonTreeViewItem entry = new(compoundJsonTreeViewItem.Plan, compoundJsonTreeViewItem.JsonItemTool)
                {
                    Path = currentIsCompound ? compoundJsonTreeViewItem.Path + "[" + 0 + "]" : compoundJsonTreeViewItem.Parent.Path + "[" + (compoundJsonTreeViewItem.Parent.Children.Count - 1) + "]",
                    Parent = currentIsCompound ? compoundJsonTreeViewItem : compoundJsonTreeViewItem.Parent,
                    DisplayText = "Entry",
                    DataType = DataTypes.ArrayElement,
                    LayerCount = compoundJsonTreeViewItem.LayerCount + 1,
                    RemoveElementButtonVisibility = Visibility.Visible
                };
                entry.Children.AddRange(result.Result);

                #region 更新代码编辑器
                int endOffset;
                if (currentIsArray)
                {
                    endOffset = compoundJsonTreeViewItem.StartLine.EndOffset;
                    compoundJsonTreeViewItem.Plan.SetRangeText(endOffset, 0, "\r\n" + new string(' ', compoundJsonTreeViewItem.LayerCount + 1) + "{\r\n" + new string(' ', compoundJsonTreeViewItem.LayerCount + 2) + currentNewString + "\r\n},");
                }
                else
                if(parentIsArray)
                {
                    endOffset = compoundJsonTreeViewItem.Plan.GetLineByNumber(compoundJsonTreeViewItem.EndLine.LineNumber - 1).EndOffset;
                    compoundJsonTreeViewItem.Plan.SetRangeText(endOffset, 0, ",\r\n" + new string(' ', compoundJsonTreeViewItem.LayerCount + 1) + "{\r\n" + new string(' ', compoundJsonTreeViewItem.LayerCount + 2) + currentNewString + "\r\n}");
                }
                #endregion

                #region 将子结构行号与代码编辑器对齐
                if (currentIsArray)
                {
                    entry.StartLine = compoundJsonTreeViewItem.Plan.GetLineByNumber(compoundJsonTreeViewItem.StartLine.LineNumber + 1);
                    entry.EndLine = compoundJsonTreeViewItem.Plan.GetLineByNumber(compoundJsonTreeViewItem.Children[0].StartLine.LineNumber - 1);
                }
                else
                if (parentIsArray)
                {
                    entry.StartLine = compoundJsonTreeViewItem.Plan.GetLineByNumber((compoundJsonTreeViewItem.Children[^1] as CompoundJsonTreeViewItem).EndLine.LineNumber + 1);
                    entry.EndLine = compoundJsonTreeViewItem.Plan.GetLineByNumber(compoundJsonTreeViewItem.Parent.EndLine.LineNumber - 1);
                }
                for (int i = 0; i < result.Result.Count; i++)
                {
                    result.Result[i].StartLineNumber = i + 1 + entry.StartLine.LineNumber;
                    result.Result[i].StartLine = compoundJsonTreeViewItem.Plan.GetLineByNumber(result.Result[i].StartLineNumber);
                    compoundJsonTreeViewItem.Children.Add(result.Result[i]);
                }
                #endregion
            }
            #endregion
        }

        /// <summary>
        /// 删除数组元素
        /// </summary>
        /// <param name="compoundJsonTreeViewItem">当前数组节点</param>
        /// <param name="customWorldUnifiedPlan">当前自定义世界生成器方案实例</param>
        public void RemoveSubStructure(CompoundJsonTreeViewItem compoundJsonTreeViewItem, ICustomWorldUnifiedPlan customWorldUnifiedPlan)
        {
            #region 确定删除的数据类型
            ReplaceType replaceType = ReplaceType.Input;
            if (compoundJsonTreeViewItem.DataType is DataTypes.ArrayElement)
                replaceType = ReplaceType.RemoveArrayElement;
            else
                if (compoundJsonTreeViewItem.DataType is DataTypes.Compound)
                replaceType = ReplaceType.Compound;
            #endregion

            #region 更新TreeView与JsonView
            Parallel.ForEach(customWorldUnifiedPlan.KeyValueContextDictionary.Keys, item =>
            {
                if (item.Contains(compoundJsonTreeViewItem.Path))
                {
                    customWorldUnifiedPlan.KeyValueContextDictionary.Remove(item);
                }
            });
            customWorldUnifiedPlan.UpdateValueBySpecifyingInterval(compoundJsonTreeViewItem, replaceType);
            if (replaceType is ReplaceType.Compound)
                compoundJsonTreeViewItem.Children.Clear();
            if (replaceType is ReplaceType.RemoveArrayElement)
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
                //if (item is CompoundJsonTreeViewItem compoundJsonTreeViewItem)
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