using CBHK.CustomControls.JsonTreeViewComponents;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace CBHK.Service.Json
{
    public interface IJsonItemTool
    {
        /// <summary>
        /// 递归删除平铺列表成员
        /// </summary>
        /// <param name="CurrentParent">目前父级节点</param>
        /// <param name="Target">已被删除的子元素</param>
        void RecursiveRemoveFlattenDescendantNodeList(CompoundJsonTreeViewItem CurrentParent,CompoundJsonTreeViewItem Target);
        /// <summary>
        /// 定位相邻两个已有值的节点
        /// </summary>
        /// <param name="previous"></param>
        /// <param name="next"></param>
        /// <returns></returns>
        public Tuple<JsonTreeViewItem, JsonTreeViewItem> LocateTheNodesOfTwoAdjacentExistingValues(JsonTreeViewItem previous, JsonTreeViewItem next);
        /// <summary>
        /// 搜索最后一个有行引用的子节点
        /// </summary>
        /// <param name="compoundJsonTreeViewItem"></param>
        /// <returns></returns>
        public JsonTreeViewItem SearchForTheLastItemWithRowReference(CompoundJsonTreeViewItem compoundJsonTreeViewItem);
        /// <summary>
        /// 从提示源码中提取当前多类型节点所需的子信息
        /// </summary>
        /// <param name="compoundJsonTreeViewItem"></param>
        /// <returns></returns>
        public List<string> ExtractSubInformationFromPromptSourceCode(CompoundJsonTreeViewItem compoundJsonTreeViewItem);
        /// <summary>
        /// 递归遍历节点树并执行代理方法
        /// </summary>
        /// <param name="jsonItemList"></param>
        /// <param name="action"></param>
        void RecursiveTraverseAndRunOperate(ObservableCollection<JsonTreeViewItem> jsonItemList,Action<JsonTreeViewItem> action);
        /// <summary>
        /// 设置每个成员的行引用
        /// </summary>
        /// <param name="list"></param>
        /// <param name="parent"></param>
        /// <param name="withType"></param>
        public void SetLineNumbersForEachItem(ObservableCollection<JsonTreeViewItem> list, CompoundJsonTreeViewItem parent,bool withType = false);
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
        public void SetParentForEachItem(ObservableCollection<JsonTreeViewItem> list, CompoundJsonTreeViewItem startParent);
        /// <summary>
        /// 添加子结构
        /// </summary>
        /// <param name="compoundJsonTreeViewItem">数组节点</param>
        void AddSubStructure(CompoundJsonTreeViewItem compoundJsonTreeViewItem);
        /// <summary>
        /// 折叠或展开当前复合节点
        /// </summary>
        /// <param name="compoundJsonTreeViewItem"></param>
        void CollapseCurrentItem(CompoundJsonTreeViewItem compoundJsonTreeViewItem);
        /// <summary>
        /// 删除当前元素
        /// </summary>
        /// <param name="compoundJsonTreeViewItem">当前节点</param>
        /// <param name="isNeedRemoveItem">是否需要删除节点</param>
        void RemoveCurrentItem(JsonTreeViewItem compoundJsonTreeViewItem,bool isNeedRemoveItem = true);
        /// <summary>
        /// 更新平铺列表
        /// </summary>
        /// <param name="result">返回的列表</param>
        /// <param name="list">需要处理的集合</param>
        void UpdateFlattenDescendantNodeList(CompoundJsonTreeViewItem target,ObservableCollection<JsonTreeViewItem> list);
    }
}