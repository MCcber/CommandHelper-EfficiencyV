using cbhk.CustomControls.JsonTreeViewComponents;
using ICSharpCode.AvalonEdit.Document;
using System.Collections.Generic;
using System.Threading.Tasks;
using static cbhk.Model.Common.Enums;

namespace cbhk.CustomControls.Interfaces
{
    public interface ICustomWorldUnifiedPlan
    {
        /// <summary>
        /// 存储节点路径对应的上下文
        /// </summary>
        public Dictionary<string, JsonTreeViewItem> KeyValueContextDictionary { get; set; }
        /// <summary>
        /// 存储当前Wiki主文档的依赖字典
        /// </summary>
        public Dictionary<string,List<string>> CurrentDependencyItemList { get; set; }
        public void UpdateNullValueBySpecifyingInterval(int endOffset, string newValue = "\r\n");
        /// <summary>
        /// 根据区间更新指定值
        /// </summary>
        /// <param name="startLineNumber">目标起始行号</param>
        /// <param name="endLineNumber">目标末尾行号</param>
        /// <param name="item">当前节点</param>
        /// <param name="newValue">新值</param>
        /// <param name="isSwitchType">正在更新类型</param>
        public void UpdateValueBySpecifyingInterval(JsonTreeViewItem item,ReplaceType replaceType, string newValue = "",bool markValue = true);
        /// <summary>
        /// 通过行号获取行引用
        /// </summary>
        /// <param name="lineNumber"></param>
        /// <returns></returns>
        public DocumentLine GetLineByNumber(int lineNumber);
        /// <summary>
        /// 通过指定范围获取文本
        /// </summary>
        /// <param name="startOffset"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public string GetRangeText(int startOffset,int length);
        /// <summary>
        /// 设置指定范围的文本
        /// </summary>
        /// <param name="startOffset"></param>
        /// <param name="length"></param>
        /// <param name="value"></param>
        public void SetRangeText(int startOffset, int length, string value);
        /// <summary>
        /// 更新CompoundHead与SwitchKey，前者用于复合子结构的根对象后者用于分支结构的标注
        /// </summary>
        /// <param name="templateItem">模板对象</param>
        /// <param name="targetItem">目标对象</param>
        public void UpdateCompoundHeadAndSwitchKey(CompoundJsonTreeViewItem templateItem, CompoundJsonTreeViewItem targetItem);
        /// <summary>
        /// 操作上下文存储节点的字典
        /// </summary>
        /// <param name="targetItem"></param>
        public JsonTreeViewItem ModifyJsonItemDictionary(JsonTreeViewItem targetItem,ModifyType modifyType);
        /// <summary>
        /// 通过路径寻找节点
        /// </summary>
        /// <param name="interval"></param>
        /// <returns></returns>
        public Task<JsonTreeViewItem> FindNodeBySpecifyingPath(string path);
        /// <summary>
        /// 逐层验证正确性
        /// </summary>
        /// <param name="currentItem"></param>
        /// <returns></returns>
        public bool VerifyCorrectnessLayerByLayer(JsonTreeViewItem currentItem);
        /// <summary>
        /// 删除指定范围内的所有行
        /// </summary>
        /// <param name="compoundJsonTreeViewItem"></param>
        public void DeleteAllLinesInTheSpecifiedRange(CompoundJsonTreeViewItem compoundJsonTreeViewItem);
        /// <summary>
        /// 删除指定范围内的所有行
        /// </summary>
        /// <param name="startOffset"></param>
        /// <param name="endOffset"></param>
        public void DeleteAllLinesInTheSpecifiedRange(int startOffset,int endOffset);
    }
}