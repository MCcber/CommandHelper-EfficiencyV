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
        /// 当前选中的版本
        /// </summary>
        public TextComboBoxItem CurrentVersion { get; set; }
        /// <summary>
        /// 配置根目录
        /// </summary>
        public string RootDirectory { get; set; }
        /// <summary>
        /// 存储需要被读取并解析的文件列表
        /// </summary>
        public List<string> DependencyFileList { get; set; }
        /// <summary>
        /// 存储需要被读取并解析的文件目录
        /// </summary>
        public List<string> DependencyDirectoryList { get; set; }
        /// <summary>
        /// 将上下文中的引用转换为正确的路径
        /// </summary>
        public Dictionary<string,string> TranslateDictionary { get; set; }
        /// <summary>
        /// 用于指定当节点为复合而不是列表时需使用的默认分支结构
        /// </summary>
        public Dictionary<string, string> TranslateDefaultEnumItemDictionary { get; set; }
        /// <summary>
        /// 识别特定结构的字典
        /// </summary>
        public Dictionary<string, string> PresetCustomCompoundKeyDictionary { get; set; }
        /// <summary>
        /// 存储节点路径对应的上下文
        /// </summary>
        public Dictionary<string, JsonTreeViewItem> KeyValueContextDictionary { get; set; }
        /// <summary>
        /// 存储当前Wiki主文档的依赖字典
        /// </summary>
        public Dictionary<string,List<string>> DependencyItemList { get; set; }
        public void UpdateNullValueBySpecifyingInterval(int endOffset, string newValue = "\r\n");

        public Dictionary<string, Dictionary<string, List<string>>> EnumCompoundDataDictionary { get; set; }

        public Dictionary<string, List<string>> EnumIDDictionary { get; set; }

        /// <summary>
        /// 根据区间更新指定值
        /// </summary>
        /// <param name="item">目标节点</param>
        /// <param name="changeType">更新类型</param>
        /// <param name="newValue">新值</param>
        public void UpdateValueBySpecifyingInterval(JsonTreeViewItem item,ChangeType changeType, string newValue = "");
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