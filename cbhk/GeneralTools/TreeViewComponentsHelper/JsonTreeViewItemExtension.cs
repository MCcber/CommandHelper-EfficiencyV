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
using System.Threading.Tasks;
using System.Windows;
using static cbhk.Model.Common.Enums;

namespace cbhk.GeneralTools.TreeViewComponentsHelper
{
    public class JsonTreeViewItemExtension(IContainerProvider container) :IJsonItemTool
    {
        private IContainerProvider _container = container;

        public void RecursiveRemoveFlattenDescendantNodeList(CompoundJsonTreeViewItem CurrentParent, CompoundJsonTreeViewItem Target)
        {
            foreach (var item in Target.Children)
            {
                CurrentParent.FlattenDescendantNodeList.Remove(item);
                if(item is CompoundJsonTreeViewItem compoundJsonTreeViewItem)
                {
                    RecursiveRemoveFlattenDescendantNodeList(CurrentParent, compoundJsonTreeViewItem);
                }
            }
        }

        public void SetDocumentLineByNumber(ObservableCollection<JsonTreeViewItem> jsonTreeViewItems)
        {
            for (int i = 0; i < jsonTreeViewItems.Count; i++)
            {
                if (jsonTreeViewItems[i] is CompoundJsonTreeViewItem compoundItem && compoundItem.Children.Count > 0)
                {
                    SetDocumentLineByNumber(compoundItem.Children);
                }

                if(i > 0)
                {
                    jsonTreeViewItems[i].Last = jsonTreeViewItems[i - 1];
                    jsonTreeViewItems[i - 1].Next = jsonTreeViewItems[i];
                }

                if (jsonTreeViewItems[i].StartLineNumber > 0)
                {
                    DocumentLine documentLine = jsonTreeViewItems[i].Plan.GetLineByNumber(jsonTreeViewItems[i].StartLineNumber);
                    jsonTreeViewItems[i].StartLine = documentLine;
                    jsonTreeViewItems[i].StartLineNumber = 0;
                }
                if (jsonTreeViewItems[i] is CompoundJsonTreeViewItem compoundJsonTreeViewItem && compoundJsonTreeViewItem.EndLineNumber > 0)
                {
                    DocumentLine documentLine = jsonTreeViewItems[i].Plan.GetLineByNumber(compoundJsonTreeViewItem.EndLineNumber);
                    compoundJsonTreeViewItem.EndLine = documentLine;
                    compoundJsonTreeViewItem.EndLineNumber = 0;
                }

                if (jsonTreeViewItems[i] is CompoundJsonTreeViewItem compound && compound.EndLine is null)
                {
                    compound.EndLine = compound.StartLine;
                }
            }
        }

        /// <summary>
        /// 切换子结构时递归整合子结构值
        /// </summary>
        public StringBuilder RecursiveIntegrationOfSubstructureValuesWhenSwitch(JsonTreeViewItem jsonItem, ObservableCollection<JsonTreeViewItem> list, JsonTreeViewItem CurrentParent, StringBuilder result = null, bool IsNotLast = false)
        {
            #region 初始化
            result ??= new();
            bool haveCompoundHead = false;
            bool needSwitchType = false;
            string space = "";
            #endregion

            foreach (var item in list)
            {
                if (result.Length == 0)
                {
                    result.Append("\r\n");
                }

                #region 添加切换Key
                if (jsonItem is CompoundJsonTreeViewItem ParentItem && ParentItem.SwitchKey.Length > 0 && !needSwitchType)
                {
                    needSwitchType = true;
                    result.Append(new string(' ', (CurrentParent is not null ? (CurrentParent.LayerCount + 1) : 2) * 2));
                    result.Append("\"" + (jsonItem as CompoundJsonTreeViewItem).SwitchKey + "\": \"" + (jsonItem as CompoundJsonTreeViewItem).Value + "\",\r\n");
                }
                #endregion

                #region 添加复合结构头Key
                if (item.Parent is not null && (jsonItem as CompoundJsonTreeViewItem).CompoundHead is not null && (jsonItem as CompoundJsonTreeViewItem).CompoundHead.Length > 0 && !haveCompoundHead)
                {
                    haveCompoundHead = true;
                    result.Append(new string(' ', (CurrentParent is not null ? (CurrentParent.LayerCount + 1) : 2) * 2));
                    result.Append("\"" + (jsonItem as CompoundJsonTreeViewItem).CompoundHead + "\": {\r\n");
                }
                #endregion

                #region 追加空格
                if (space.Length == 0)
                    space = new(' ', item.LayerCount * 2);
                #endregion

                #region 追加内容key与value，或者开启递归
                if (item is CompoundJsonTreeViewItem compoundJsonTreeViewItem)
                {
                    if (compoundJsonTreeViewItem.Children.Count == 0)
                    {
                        result.Append(space + "\"" + compoundJsonTreeViewItem.Key + "\": " + (item.DataType is DataTypes.Array ? "[]" : compoundJsonTreeViewItem.Value));
                        if (compoundJsonTreeViewItem.Next is not null)
                            result.Append(',');
                        result.Append("\r\n");
                    }
                    else
                        result = RecursiveIntegrationOfSubstructureValuesWhenSwitch(jsonItem,compoundJsonTreeViewItem.Children, compoundJsonTreeViewItem, result, IsNotLast);
                }
                else
                {
                    if(item.Value is bool boolValue)
                    {
                        item.Value = boolValue.ToString().ToLower();
                    }
                    result.Append(space + "\"" + item.Key + "\": " + (item.DataType is DataTypes.String ? "\"" : "") + item.Value + (item.DataType is DataTypes.String ? "\"" : ""));
                    if (item.Next is not null)
                        result.Append(',');
                    result.Append("\r\n");
                }
                #endregion
            }

            #region 处理一层的收尾
            if (haveCompoundHead)
            {
                result.Append(new string(' ', ((CurrentParent is not null ? CurrentParent.LayerCount : 0) + 1) * 2));
                result.Append('}');

                #region 检查是否需要末尾追加逗号
                if (IsNotLast)
                    result.Append(',');
                #endregion

                result.Append("\r\n");
            }
            #endregion

            return result;
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
            #region 定义列表和统一方案接口
            //CompoundJsonTreeViewItem subItem = compoundJsonTreeViewItem.Plan.CurrentDependencyItemList[compoundJsonTreeViewItem.Key];
            bool AddToTop = compoundJsonTreeViewItem.DataType is DataTypes.Array;
            bool AddSubStructure = 
                compoundJsonTreeViewItem.DataType is DataTypes.Compound ||
                compoundJsonTreeViewItem.DataType is DataTypes.OptionalCompound ||
                compoundJsonTreeViewItem.DataType is DataTypes.NullableCompound ||
                compoundJsonTreeViewItem.DataType is DataTypes.OptionalAndNullableCompound;
            ObservableCollection<JsonTreeViewItem> subChildren = [];
            int CurrentStartLineNumber, CurrentEndLineNumber;
            #endregion

            #region 处理可选复合型节点
            if(AddSubStructure)
            {
                ObservableCollection<JsonTreeViewItem> optionalResultList = TreeViewRuleReader.RecursivelyUpdateMemberLayer(compoundJsonTreeViewItem, compoundJsonTreeViewItem.Plan.KeyValueContextDictionary, compoundJsonTreeViewItem.Children, compoundJsonTreeViewItem.StartLine.LineNumber + 2, compoundJsonTreeViewItem.LayerCount + 1);

                compoundJsonTreeViewItem.Children = optionalResultList;

                //计算前导空格
                string optionalSpace = new(' ', compoundJsonTreeViewItem.LayerCount * 2);

                #region 计算应该添加的内容
                string optionalNewValue = (compoundJsonTreeViewItem.StartLine.LineNumber > 2 ? "," : "") + "\r\n" + optionalSpace + "\"" + compoundJsonTreeViewItem.Key + "\": " + "{" + compoundJsonTreeViewItem.JsonItemTool.RecursiveIntegrationOfSubstructureValuesWhenSwitch(compoundJsonTreeViewItem, compoundJsonTreeViewItem.Children, compoundJsonTreeViewItem.Parent) + optionalSpace + "}" + (compoundJsonTreeViewItem.Next is not null ? ',' : "");
                #endregion

                compoundJsonTreeViewItem.Plan.UpdateNullValueBySpecifyingInterval(compoundJsonTreeViewItem.StartLine.EndOffset, optionalNewValue);
                compoundJsonTreeViewItem.JsonItemTool.SetDocumentLineByNumber(compoundJsonTreeViewItem.Children);
                compoundJsonTreeViewItem.StartLine = compoundJsonTreeViewItem.Plan.GetLineByNumber(compoundJsonTreeViewItem.StartLine.LineNumber + 1);
                if (compoundJsonTreeViewItem.Children[^1] is CompoundJsonTreeViewItem subLastCompoundItem)
                {
                    compoundJsonTreeViewItem.EndLine = compoundJsonTreeViewItem.Plan.GetLineByNumber(subLastCompoundItem.EndLine.LineNumber + 1);
                }
                else
                {
                    compoundJsonTreeViewItem.EndLine = compoundJsonTreeViewItem.Plan.GetLineByNumber(compoundJsonTreeViewItem.Children[^1].StartLine.LineNumber + 1);
                }
                compoundJsonTreeViewItem.AddElementButtonVisibility = Visibility.Collapsed;
                compoundJsonTreeViewItem.RemoveElementButtonVisibility = Visibility.Visible;
                return;
            }
            #endregion

            #region 处理起始与末尾行号
            if (!AddToTop)
            {
                if (compoundJsonTreeViewItem.Parent is not null)
                {
                    CurrentStartLineNumber = (compoundJsonTreeViewItem.Parent.Children[^2] as CompoundJsonTreeViewItem).EndLine.LineNumber;
                    CurrentEndLineNumber = (compoundJsonTreeViewItem.Parent.Children[^2] as CompoundJsonTreeViewItem).EndLine.LineNumber;
                }
                else
                {
                    CurrentStartLineNumber = CurrentEndLineNumber = compoundJsonTreeViewItem.StartLine.LineNumber;
                }
            }
            else
                if (compoundJsonTreeViewItem.Children.Count - 1 - compoundJsonTreeViewItem.ChildrenStringList.Count >= 0)
            {
                CurrentStartLineNumber = compoundJsonTreeViewItem.StartLine.LineNumber + 1;
                CurrentEndLineNumber = compoundJsonTreeViewItem.StartLine.LineNumber + 1;
            }
            else
            {
                CurrentStartLineNumber = CurrentEndLineNumber = compoundJsonTreeViewItem.StartLine.LineNumber;
            }
            #endregion

            #region 创建新的数组元素
            int currentElementIndex = 0;
            if (compoundJsonTreeViewItem.Children is not null)
                currentElementIndex = AddToTop ? 0 : compoundJsonTreeViewItem.Children.Count;
            CompoundJsonTreeViewItem entry = new(compoundJsonTreeViewItem.Plan, compoundJsonTreeViewItem.JsonItemTool)
            {
                Path = AddToTop ? compoundJsonTreeViewItem.Path + "[" + currentElementIndex + "]" : compoundJsonTreeViewItem.Parent.Path + "[" + (compoundJsonTreeViewItem.Parent.Children.Count - 1) + "]",
                Parent = AddToTop ? compoundJsonTreeViewItem : compoundJsonTreeViewItem.Parent,
                DisplayText = "Entry",
                DataType = DataTypes.ArrayElement,
                LayerCount = compoundJsonTreeViewItem.LayerCount + 1,
                RemoveElementButtonVisibility = Visibility.Visible
            };

            if((!AddToTop && currentElementIndex > 0) || compoundJsonTreeViewItem.Children.Count > 1)
            {
                if (AddToTop && compoundJsonTreeViewItem.Children.Count > 1)
                {
                    entry.Next = compoundJsonTreeViewItem.Children[0];
                    compoundJsonTreeViewItem.Children[0].Last = entry;
                }
                else
                if (compoundJsonTreeViewItem.Parent is not null && compoundJsonTreeViewItem.Parent.Children.Count > 1)
                {
                    entry.Last = compoundJsonTreeViewItem.Parent.Children[^2];
                    compoundJsonTreeViewItem.Parent.Children[^2].Next = entry;
                }
            }
            #endregion

            #region 克隆模板中的子结构、设置它的数据，设置元素的末尾行号
            List<string> currentSwitchChildren = [];
            if (AddToTop)
                currentSwitchChildren = compoundJsonTreeViewItem.ChildrenStringList;
            else
                currentSwitchChildren = compoundJsonTreeViewItem.Parent.ChildrenStringList;

            //这里需要用HtmlHelper实例来取得对应的子结构
            subChildren = [];

            int currentLineNumber = AddToTop ? CurrentStartLineNumber + 2 : (compoundJsonTreeViewItem.Parent.Children[^2] as CompoundJsonTreeViewItem).EndLine.LineNumber + 2;

            ObservableCollection<JsonTreeViewItem> resultList = TreeViewRuleReader.RecursivelyUpdateMemberLayer(AddToTop ? compoundJsonTreeViewItem : compoundJsonTreeViewItem.Parent, compoundJsonTreeViewItem.Plan.KeyValueContextDictionary, subChildren, currentLineNumber, (AddToTop ? compoundJsonTreeViewItem.LayerCount : compoundJsonTreeViewItem.Parent.LayerCount) + 1);
            #endregion

            #region 计算前导空格
            int currentLayerCount = AddToTop ? compoundJsonTreeViewItem.LayerCount + 1 : compoundJsonTreeViewItem.Parent.LayerCount + 1;
            string space = new(' ', currentLayerCount * 2);
            #endregion

            #region 计算应该添加的内容
            string currentNewValue = (!AddToTop ? "," : "") + "\r\n" + space + "{" + compoundJsonTreeViewItem.JsonItemTool.RecursiveIntegrationOfSubstructureValuesWhenSwitch(compoundJsonTreeViewItem, entry.Children, compoundJsonTreeViewItem.Parent) + space + "}";
            #endregion

            #region 计算后导逗号与换行
            if (AddToTop && compoundJsonTreeViewItem.Children.Count > 0)
                currentNewValue += ",";
            currentNewValue += (AddToTop && compoundJsonTreeViewItem.Children.Count == 0) || (!AddToTop && compoundJsonTreeViewItem.Parent.Children.Count == 0) ? "\r\n" + new string(' ', compoundJsonTreeViewItem.LayerCount * 2) : "";
            #endregion

            #region 更新Json视图
            compoundJsonTreeViewItem.Plan.UpdateValueBySpecifyingInterval(AddToTop ? compoundJsonTreeViewItem : compoundJsonTreeViewItem.Parent, ReplaceType.AddArrayElement, currentNewValue, AddToTop);
            #endregion

            #region 控制排序按钮和后置添加按钮的状态、处理父子级关系

            #region 确定要处理的集合
            ObservableCollection<JsonTreeViewItem> currentChildren;
            if (AddToTop)
                currentChildren = compoundJsonTreeViewItem.Children;
            else
                currentChildren = compoundJsonTreeViewItem.Parent.Children;
            #endregion

            #region 子节点的平铺列表必须是父节点的子集
            bool NeedSetDocument = true;
            if (AddToTop)
            {
                NeedSetDocument = false;
                compoundJsonTreeViewItem.JsonItemTool.SetDocumentLineByNumber(entry.Children);
                currentChildren.Insert(0, entry);
                int count = compoundJsonTreeViewItem.FlattenDescendantNodeList.Where(item => item.StartLine.LineNumber == entry.FlattenDescendantNodeList[0].StartLine.LineNumber).Count();
                if (count == 0)
                {
                    compoundJsonTreeViewItem.FlattenDescendantNodeList.AddRange(entry.FlattenDescendantNodeList);
                    compoundJsonTreeViewItem.FlattenDescendantNodeList.Add(entry);
                }
            }
            else
            {
                currentChildren.Insert(currentChildren.Count - 1, entry);
                int count = compoundJsonTreeViewItem.FlattenDescendantNodeList.Where(item => item.StartLine.LineNumber == entry.FlattenDescendantNodeList[0].StartLine.LineNumber).Count();
                if (count == 0)
                {
                    compoundJsonTreeViewItem.Parent.FlattenDescendantNodeList.AddRange(entry.FlattenDescendantNodeList);
                    compoundJsonTreeViewItem.Parent.FlattenDescendantNodeList.Add(entry);
                }
            }
            #endregion

            #region 有新元素添加时索引后移，或者添加后置添加按钮
            if (currentChildren.Count > 1)
            {
                for (int i = 0; i < currentChildren.Count; i++)
                {
                    if (currentChildren[i] is CompoundJsonTreeViewItem compoundChild && compoundChild.AddElementButtonVisibility is Visibility.Collapsed)
                    {
                        compoundChild.IsCanSort = true;
                        if (AddToTop && i > 0 && compoundChild.AddElementButtonVisibility is Visibility.Collapsed)
                            compoundChild.Path = compoundJsonTreeViewItem.Path + "[" + (i + 1) + "]";
                    }
                }
            }
            else
            {
                if (currentChildren[0] is CompoundJsonTreeViewItem compoundChild)
                    compoundChild.IsCanSort = false;
                CompoundJsonTreeViewItem addToBottomItem = new(compoundJsonTreeViewItem.Plan, compoundJsonTreeViewItem.JsonItemTool)
                {
                    Parent = compoundJsonTreeViewItem,
                    DataType = DataTypes.ArrayElement,
                    AddElementButtonVisibility = Visibility.Visible,
                    RemoveElementButtonVisibility = Visibility.Collapsed,
                    ElementButtonTip = "添加在底部"
                };
                currentChildren.Add(addToBottomItem);
            }
            #endregion

            #endregion

            #region 根据行号更新行引用,更新数组元素的起始与末尾行引用
            if (NeedSetDocument)
                compoundJsonTreeViewItem.JsonItemTool.SetDocumentLineByNumber(entry.Children);
            entry.StartLine = compoundJsonTreeViewItem.Plan.GetLineByNumber(entry.Children[0].StartLine.LineNumber - 1);
            entry.EndLine = compoundJsonTreeViewItem.Plan.GetLineByNumber(entry.Children[^1] is CompoundJsonTreeViewItem compoundItem && compoundItem.EndLine is not null ? compoundItem.EndLine.LineNumber + 1 : entry.Children[^1].StartLine.LineNumber + 1);
            #endregion

            if(AddSubStructure)
            {
                HtmlHelper htmlHelper = _container.Resolve<HtmlHelper>();
                JsonTreeViewDataStructure result = htmlHelper.GetTreeViewItemResult(new(), [.. compoundJsonTreeViewItem.ChildrenStringList], compoundJsonTreeViewItem.StartLine.LineNumber + 1, compoundJsonTreeViewItem.LayerCount + 1, compoundJsonTreeViewItem);
                compoundJsonTreeViewItem.Children.AddRange(result.Result);
            }
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
            if (compoundJsonTreeViewItem.Last is not null)
                compoundJsonTreeViewItem.Last.Next = compoundJsonTreeViewItem.Next;
            if (compoundJsonTreeViewItem.Next is not null)
                compoundJsonTreeViewItem.Next.Last = compoundJsonTreeViewItem.Last;
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