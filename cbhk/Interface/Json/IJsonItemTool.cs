﻿using cbhk.CustomControls.Interfaces;
using cbhk.CustomControls.JsonTreeViewComponents;
using ICSharpCode.AvalonEdit;
using System;
using System.Collections.ObjectModel;
using System.Text;
using System.Windows;

namespace cbhk.Interface.Json
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
        /// 添加数组元素
        /// </summary>
        /// <param name="compoundJsonTreeViewItem">数组节点</param>
        void AddSubStructure(CompoundJsonTreeViewItem compoundJsonTreeViewItem);
        /// <summary>
        /// 删除数组元素
        /// </summary>
        /// <param name="compoundJsonTreeViewItem">当前数组节点</param>
        /// <param name="customWorldUnifiedPlan">当前自定义世界生成器方案实例</param>
        void RemoveSubStructure(CompoundJsonTreeViewItem compoundJsonTreeViewItem);
        /// <summary>
        /// 更新平铺列表
        /// </summary>
        /// <param name="result">返回的列表</param>
        /// <param name="list">需要处理的集合</param>
        void UpdateFlattenDescendantNodeList(CompoundJsonTreeViewItem target,ObservableCollection<JsonTreeViewItem> list);
    }
}