using cbhk.CustomControls.Interfaces;
using cbhk.CustomControls.JsonTreeViewComponents;
using ICSharpCode.AvalonEdit;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Windows;

namespace cbhk.Interface.Json
{
    public interface IJsonItemTool
    {
        /// <summary>
        /// 通过行号寻找行引用
        /// </summary>
        /// <param name="jsonTreeViewItems"></param>
        void SetDocumentLineByNumber(ObservableCollection<JsonTreeViewItem> jsonTreeViewItems);
        /// <summary>
        /// 设置默认子结构
        /// </summary>
        /// <param name="item"></param>
        /// <param name="switchChildren"></param>
        /// <returns></returns>
        int SetDefaultStructureJsonItem(CompoundJsonTreeViewItem item, ObservableCollection<JsonTreeViewItem> switchChildren);
        void SetEnumData(ObservableCollection<JsonTreeViewItem> Children, CompoundJsonTreeViewItem entry, CompoundJsonTreeViewItem TempateItem);
        /// <summary>
        /// 递归删除平铺列表成员
        /// </summary>
        /// <param name="CurrentParent">目前父级节点</param>
        /// <param name="Target">已被删除的子元素</param>
        void RecursiveRemoveFlattenDescendantNodeList(CompoundJsonTreeViewItem CurrentParent,CompoundJsonTreeViewItem Target);
        /// <summary>
        /// 切换子结构时递归整合子结构值
        /// </summary>
        /// <param name="jsonTreeViewItem">当前节点</param>
        /// <param name="list">需要被遍历的节点集合</param>
        /// <param name="CurrentParent">当前父节点</param>
        /// <param name="result">拼接结果</param>
        /// <param name="IsNotLast">不为最后一个</param>
        /// <returns></returns>
        StringBuilder RecursiveIntegrationOfSubstructureValuesWhenSwitch(JsonTreeViewItem jsonTreeViewItem, ObservableCollection<JsonTreeViewItem> list, JsonTreeViewItem CurrentParent, StringBuilder result = null, bool IsNotLast = false);
        /// <summary>
        /// 递归遍历节点树并执行代理方法
        /// </summary>
        /// <param name="jsonItemList"></param>
        /// <param name="action"></param>
        void RecursiveTraverseAndRunOperate(ObservableCollection<JsonTreeViewItem> jsonItemList,Action<JsonTreeViewItem> action);
        /// <summary>
        /// 递归克隆节点
        /// </summary>
        /// <param name="list"></param>
        /// <param name="currentItem"></param>
        /// <returns></returns>
        ObservableCollection<JsonTreeViewItem> RecursiveCloneItem(ObservableCollection<JsonTreeViewItem> list, CompoundJsonTreeViewItem currentItem);
        /// <summary>
        /// 通过行号设置DocumentLine实例
        /// </summary>
        /// <param name="jsonTreeViewItem">当前节点</param>
        /// <param name="textEditor">节点所在的代码编辑器</param>
        void SetDocumentLineByLineNumber(JsonTreeViewItem jsonTreeViewItem, TextEditor textEditor);
        /// <summary>
        /// 设置分支的子节点
        /// </summary>
        /// <param name="switchChildren"></param>
        /// <param name="compoundJsonTreeViewItem"></param>
        /// <param name="entry"></param>
        /// <returns></returns>
        ObservableCollection<JsonTreeViewItem> SetSwitchChildrenOfSubItem(ObservableCollection<JsonTreeViewItem> switchChildren, CompoundJsonTreeViewItem compoundJsonTreeViewItem, CompoundJsonTreeViewItem entry);
        /// <summary>
        /// 添加数组元素
        /// </summary>
        /// <param name="compoundJsonTreeViewItem">数组节点</param>
        void AddSubStructure(CompoundJsonTreeViewItem compoundJsonTreeViewItem,UIElement element);
        /// <summary>
        /// 删除数组元素
        /// </summary>
        /// <param name="compoundJsonTreeViewItem">当前数组节点</param>
        /// <param name="customWorldUnifiedPlan">当前自定义世界生成器方案实例</param>
        void RemoveSubStructure(CompoundJsonTreeViewItem compoundJsonTreeViewItem,ICustomWorldUnifiedPlan customWorldUnifiedPlan);
        /// <summary>
        /// 更新平铺列表
        /// </summary>
        /// <param name="result">返回的列表</param>
        /// <param name="list">需要处理的集合</param>
        void UpdateFlattenDescendantNodeList(CompoundJsonTreeViewItem target,ObservableCollection<JsonTreeViewItem> list);
    }
}