using CBHK.CustomControl.JsonTreeViewComponents;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace CBHK.Service.Json
{
    public interface IJsonItemTool
    {
        /// <summary>
        /// 获取添加到底部的按钮节点
        /// </summary>
        /// <param name="template"></param>
        /// <returns></returns>
        public BaseCompoundJsonTreeViewItem GetAddToBottomButtonItem(BaseCompoundJsonTreeViewItem template);
        /// <summary>
        /// 从提示源码中提取当前多类型节点所需的子信息
        /// </summary>
        /// <param name="compoundJsonTreeViewItem"></param>
        /// <returns></returns>
        public List<string> ExtractSubInformationFromPromptSourceCode(BaseCompoundJsonTreeViewItem compoundJsonTreeViewItem);
        /// <summary>
        /// 递归遍历节点树并执行代理方法
        /// </summary>
        /// <param name="jsonItemList"></param>
        /// <param name="action"></param>
        void RecursiveTraverseAndRunOperate(ObservableCollection<JsonTreeViewItem> jsonItemList,Action<JsonTreeViewItem> action);
        /// <summary>
        /// 设置每个成员的行引用
        /// </summary>
        /// <param name="list">目标节点集合</param>
        /// <param name="parent">起始父节点</param>
        public Tuple<JsonTreeViewItem, JsonTreeViewItem> SetLineNumbersForEachSubItem(ObservableCollection<JsonTreeViewItem> list, BaseCompoundJsonTreeViewItem parent);
        /// <summary>
        /// 从指定的索引设置每个成员的行引用
        /// </summary>
        /// <param name="list"></param>
        /// <param name="lineNumber">起始行号</param>
        public Tuple<JsonTreeViewItem, JsonTreeViewItem> SetLineNumbersForEachSubItem(ObservableCollection<JsonTreeViewItem> list, int lineNumber);
        /// <summary>
        /// 设置每一个成员的层数
        /// </summary>
        /// <param name="list">节点集合</param>
        /// <param name="startCount">起始层数</param>
        public void SetLayerCountForEachItem(ObservableCollection<JsonTreeViewItem> list, int startCount);
        /// <summary>
        /// 设置每一个成员的父节点
        /// </summary>
        /// <param name="list">节点集合</param>
        /// <param name="startParent">起始父级</param>
        public void SetParentForEachItem(ObservableCollection<JsonTreeViewItem> list, BaseCompoundJsonTreeViewItem startParent,int startIndex = 0);
        /// <summary>
        /// 添加子结构
        /// </summary>
        /// <param name="compoundJsonTreeViewItem">数组节点</param>
        void AddSubStructure(BaseCompoundJsonTreeViewItem compoundJsonTreeViewItem);
        /// <summary>
        /// 折叠或展开当前复合节点
        /// </summary>
        /// <param name="compoundJsonTreeViewItem"></param>
        void CollapseCurrentItem(BaseCompoundJsonTreeViewItem compoundJsonTreeViewItem);
        /// <summary>
        /// 删除当前元素
        /// </summary>
        /// <param name="compoundJsonTreeViewItem">当前节点</param>
        /// <param name="isNeedRemoveItem">是否需要删除节点</param>
        void RemoveCurrentItem(JsonTreeViewItem compoundJsonTreeViewItem,bool isNeedRemoveItem = true);
    }
}