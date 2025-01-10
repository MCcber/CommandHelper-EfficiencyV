using cbhk.CustomControls.JsonTreeViewComponents;
using cbhk.Model.Common;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace cbhk.GeneralTools.TreeViewComponentsHelper
{
    /// <summary>
    /// 将Json规则转化为TreeView
    /// </summary>
    public class TreeViewRuleReader
    {
        /// <summary>
        /// 递归整理子结构上下文
        /// </summary>
        /// <param name="children">当前节点集合</param>
        /// <param name="Parent">当前父级节点</param>
        /// <param name="context">当前上下文字典</param>
        /// <returns>整理后的节点集合</returns>
        public static ObservableCollection<JsonTreeViewItem> RecursivelyUpdateMemberLayer(CompoundJsonTreeViewItem Parent, Dictionary<string, JsonTreeViewItem> context, ObservableCollection<JsonTreeViewItem> children, int lineNumber, int layer = 2)
        {
            bool haveCompoundHead = false;
            bool haveSwitchKey = false;
            string ParentCompoundHead = "";
            //string ParentSwitchKey = "";
            for (int i = 0; i < children.Count; i++)
            {
                //CompoundHead和SwitchKey都会占用一行，所以遇到需要让行号加1

                #region 处理CompoundHead
                if (children[i].Parent.CompoundHead is not null && children[i].Parent.CompoundHead.Length > 0 && !haveCompoundHead)
                {
                    ParentCompoundHead = children[i].Parent.CompoundHead + ".";
                    haveCompoundHead = true;
                    lineNumber++;
                }
                #endregion

                #region 处理SwitchKey
                if (children[i].Parent.SwitchKey is not null && children[i].Parent.SwitchKey.Length > 0 && !haveSwitchKey)
                {
                    //ParentSwitchKey = children[i].Parent.SwitchKey + ".";
                    haveSwitchKey = true;
                    lineNumber++;
                }
                #endregion

                #region 处理首行与尾行、拼接节点路径
                if (children[i].Parent.SwitchKey.Length > 0)
                    children[i].Parent = Parent;
                children[i].Path = children[i].Parent.Path + "." + ParentCompoundHead + children[i].Key;
                children[i].StartLineNumber = lineNumber;
                //判断是否为复合数据类型
                if (children[i] is CompoundJsonTreeViewItem compoundJsonTreeViewItem && (compoundJsonTreeViewItem.DataType is Enums.DataTypes.Array || compoundJsonTreeViewItem.DataType is Enums.DataTypes.Compound || compoundJsonTreeViewItem.DataType is Enums.DataTypes.EnumCompound || compoundJsonTreeViewItem.DataType is Enums.DataTypes.MultiType))
                    compoundJsonTreeViewItem.EndLineNumber = lineNumber;
                #endregion

                #region 绑定前后关系
                if (i > 0)
                {
                    children[i].Previous = children[i - 1];
                    children[i - 1].Next = children[i];
                }
                #endregion

                #region 处理节点层级
                int currentLayerCount = layer;
                if (ParentCompoundHead.Length > 0 && children[i].Previous is null)
                    currentLayerCount += 1;
                if ((children[i].Parent.DataType is Enums.DataTypes.Array || children[i].Parent.DataType is Enums.DataTypes.ArrayElement) && children[i].Previous is null)
                    currentLayerCount += 1;
                children[i].LayerCount = layer = currentLayerCount;
                #endregion

                #region 将处理完的节点添加到当前上下文字典中、递归处理
                if (!context.ContainsKey(children[i].Path))
                    context.Add(children[i].Path, children[i]);
                if (children[i] is CompoundJsonTreeViewItem compound && compound.Children.Count > 0)
                {
                    RecursivelyUpdateMemberLayer(compound, context, children, lineNumber + 1, layer + 1);
                }
                #endregion

                #region 复合标头复位、行号自增1
                ParentCompoundHead /*= ParentSwitchKey */= "";
                lineNumber++;
                #endregion
            }
            return children;
        }
    }
}