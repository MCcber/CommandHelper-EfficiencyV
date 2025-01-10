using cbhk.CustomControls.Interfaces;
using cbhk.CustomControls.JsonTreeViewComponents;
using cbhk.Interface.Json;
using cbhk.Model.Common;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using Prism.Ioc;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using static cbhk.Model.Common.Enums;

namespace cbhk.GeneralTools.TreeViewComponentsHelper
{
    public partial class JsonTreeViewItemExtension(IContainerProvider container) :IJsonItemTool
    {
        #region Field
        private IContainerProvider _container = container;

        [GeneratedRegex(@"\[\[\#(?<1>[\u4e00-\u9fff]+)\|(?<2>[\u4e00-\u9fff]+)\]\]")]
        private static partial Regex GetContextKey();
        #endregion

        public void RecursiveRemoveFlattenDescendantNodeList(CompoundJsonTreeViewItem CurrentParent, CompoundJsonTreeViewItem Target)
        {
            foreach (var item in Target.Children)
            {
                CurrentParent.FlattenDescendantNodeList.Remove(item);
                if(item is CompoundJsonTreeViewItem compoundJsonTreeViewItem)
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
        public void SetDocumentLineByLineNumber(JsonTreeViewItem item,TextEditor textEditor)
        {
            if (item.StartLineNumber > textEditor.Document.LineCount || item.StartLineNumber <= 0)
            {
                return;
            }
            item.StartLine = textEditor.Document.GetLineByNumber(item.StartLineNumber);
            item.StartLineNumber = 0;
            if(item is CompoundJsonTreeViewItem compoundJsonTreeViewItem && compoundJsonTreeViewItem.EndLineNumber > 0)
            {
                compoundJsonTreeViewItem.EndLine = textEditor.Document.GetLineByNumber(compoundJsonTreeViewItem.EndLineNumber);
                compoundJsonTreeViewItem.EndLineNumber = 0;
                if(compoundJsonTreeViewItem.Children.Count > 0)
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
        public void AddSubStructure(CompoundJsonTreeViewItem compoundJsonTreeViewItem,UIElement element)
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
            ObservableCollection<JsonTreeViewItem> subChildren = [];
            string newValue = "";
            int CurrentStartLineNumber, CurrentEndLineNumber;
            JsonTreeViewDataStructure result = new();
            ReplaceType replaceType = ReplaceType.Direct;
            CompoundJsonTreeViewItem? targetCompoundItem;
            if (currentIsArray || currentIsCompound)
            {
                targetCompoundItem = compoundJsonTreeViewItem;
            }
            else
            if(parentIsArray)
            {
                targetCompoundItem = compoundJsonTreeViewItem.Parent;
            }
            if(currentIsCompound)
            {
                replaceType = ReplaceType.Compound;
            }
            else
            {
                replaceType = ReplaceType.AddArrayElement;
            }
            #endregion

            #region 计算前导空格
            int currentLayerCount = currentIsCompound || currentIsArray ? compoundJsonTreeViewItem.LayerCount + 1 : compoundJsonTreeViewItem.Parent.LayerCount + 1;
            string space = new(' ', currentLayerCount * 2);
            #endregion

            #region 计算应该添加的内容
            string currentNewValue = (!parentIsArray ? "," : "") + "\r\n" + space + "{" + space + "}";
            #endregion

            #region 实例化复合节点子链表信息
            if (currentIsCompound && compoundJsonTreeViewItem.ChildrenStringList.Count > 0)
            {
                HtmlHelper htmlHelper = _container.Resolve<HtmlHelper>();

                #region 定位当前复合节点的起始行号
                JsonTreeViewItem previous = compoundJsonTreeViewItem.Previous;
                while (previous.StartLineNumber < 2)
                {
                    if (previous.Previous is null)
                    {
                        break;
                    }
                    previous = previous.Previous;
                }
                if (previous is not null)
                {
                    compoundJsonTreeViewItem.StartLineNumber = previous is CompoundJsonTreeViewItem previousComnpoundItem && previousComnpoundItem.EndLine is not null ? previousComnpoundItem.EndLine.LineNumber + 1 : previous.StartLine.LineNumber;
                }
                else
                {
                    compoundJsonTreeViewItem.StartLineNumber = 2;
                }
                compoundJsonTreeViewItem.StartLine = compoundJsonTreeViewItem.Plan.GetLineByNumber(compoundJsonTreeViewItem.StartLineNumber);
                #endregion

                //当只有一个子结构原始字符串成员时，分析能够提取引用
                if (compoundJsonTreeViewItem.ChildrenStringList.Count == 1)
                {
                    Match contextMatch = GetContextKey().Match(compoundJsonTreeViewItem.ChildrenStringList[0]);
                    if(contextMatch.Success)
                    {
                        string contextKey = "#" + (contextMatch.Groups[1].Value + contextMatch.Groups[2].Value).Replace("|上文","").Replace("|下文", "").Replace("[","").Replace("]", "");
                        List<string> CurrentDependencyItemList = compoundJsonTreeViewItem.Plan.CurrentDependencyItemList[contextKey];
                        ObservableCollection<string> CurrentSubDependencyItemList = (CurrentDependencyItemList[0] as CompoundJsonTreeViewItem).Children;
                        for (int i = 0; i < CurrentSubDependencyItemList.Count; i++)
                        {
                            compoundJsonTreeViewItem.Children.Add(CurrentSubDependencyItemList[i]);
                        }
                    }
                }
                else
                {
                    CompoundJsonTreeViewItem entry = new(compoundJsonTreeViewItem.Plan, compoundJsonTreeViewItem.JsonItemTool)
                    {
                        Path = currentIsCompound ? compoundJsonTreeViewItem.Path + "[" + 0 + "]" : compoundJsonTreeViewItem.Parent.Path + "[" + (compoundJsonTreeViewItem.Parent.Children.Count - 1) + "]",
                        Parent = currentIsCompound ? compoundJsonTreeViewItem : compoundJsonTreeViewItem.Parent,
                        DisplayText = "Entry",
                        DataType = DataTypes.ArrayElement,
                        LayerCount = compoundJsonTreeViewItem.LayerCount + 1,
                        RemoveElementButtonVisibility = Visibility.Visible
                    };

                    JsonTreeViewDataStructure dataStructure = htmlHelper.GetTreeViewItemResult(new(), compoundJsonTreeViewItem.ChildrenStringList, compoundJsonTreeViewItem.StartLine.LineNumber + 2, compoundJsonTreeViewItem.LayerCount, true, compoundJsonTreeViewItem, null, compoundJsonTreeViewItem.LayerCount, true);

                    for (int i = 0; i < dataStructure.Result.Count; i++)
                    {
                        dataStructure.Result[i].StartLineNumber = i + 1 + compoundJsonTreeViewItem.StartLineNumber;
                        dataStructure.Result[i].StartLine = compoundJsonTreeViewItem.Plan.GetLineByNumber(dataStructure.Result[i].StartLineNumber);
                        compoundJsonTreeViewItem.Children.Add(dataStructure.Result[i]);
                    }
                }

                #region 定位当前复合节点的末尾行号
                compoundJsonTreeViewItem.EndLineNumber = compoundJsonTreeViewItem.Children[^1] is CompoundJsonTreeViewItem compoundJsonTreeItem && compoundJsonTreeItem.EndLine is not null ? compoundJsonTreeItem.EndLine.LineNumber + 1 : compoundJsonTreeViewItem.Children[^1].StartLine.LineNumber + 1;
                compoundJsonTreeViewItem.EndLine = compoundJsonTreeViewItem.Plan.GetLineByNumber(compoundJsonTreeViewItem.EndLineNumber);
                #endregion

                return;
            }
            #endregion

            #region 实例化数组/列表节点子链表信息
            if((currentIsArray || parentIsArray) && compoundJsonTreeViewItem.ChildrenStringList.Count > 0)
            {
                CompoundJsonTreeViewItem listElement = new(compoundJsonTreeViewItem.Plan, compoundJsonTreeViewItem.JsonItemTool)
                {
                    Path = currentIsCompound ? compoundJsonTreeViewItem.Path + "[" + 0 + "]" : compoundJsonTreeViewItem.Parent.Path + "[" + (compoundJsonTreeViewItem.Parent.Children.Count - 1) + "]",
                    Parent = currentIsCompound ? compoundJsonTreeViewItem : compoundJsonTreeViewItem.Parent,
                    DisplayText = "Entry",
                    DataType = DataTypes.ArrayElement,
                    LayerCount = compoundJsonTreeViewItem.LayerCount + 1,
                    RemoveElementButtonVisibility = Visibility.Visible
                };
            }
            #endregion

            #region 更新Json视图
            compoundJsonTreeViewItem.Plan.UpdateValueBySpecifyingInterval(currentIsCompound ? compoundJsonTreeViewItem : compoundJsonTreeViewItem.Parent, replaceType, currentNewValue, currentIsCompound);
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
                if(item is CompoundJsonTreeViewItem compoundJsonTreeViewItem)
                {
                    RecursiveTraverseAndRunOperate(jsonItemList,action);
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