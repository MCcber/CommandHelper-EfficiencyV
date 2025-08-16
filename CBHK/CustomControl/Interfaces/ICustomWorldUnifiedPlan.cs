using CBHK.CustomControl.JsonTreeViewComponents;
using CBHK.Model.Common;
using ICSharpCode.AvalonEdit.Document;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using static CBHK.Model.Common.Enums;

namespace CBHK.CustomControl.Interfaces
{
    public interface ICustomWorldUnifiedPlan
    {
        #region Property
        /// <summary>
        /// 当前选中的版本
        /// </summary>
        public TextComboBoxItem CurrentVersion { get; set; }

        public ObservableCollection<TextComboBoxItem> VersionList { get; set; }
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
        public Dictionary<string, string> TranslateDefaultDictionary { get; set; }
        /// <summary>
        /// 存储节点路径对应的上下文
        /// </summary>
        public Dictionary<string, JsonTreeViewItem> KeyValueContextDictionary { get; set; }
        /// <summary>
        /// 存储当前Wiki主文档的依赖字典
        /// </summary>
        public Dictionary<string,List<string>> DependencyItemList { get; set; }

        public Dictionary<string, Dictionary<string, List<string>>> EnumCompoundDataDictionary { get; set; }

        public Dictionary<string, List<string>> EnumIDDictionary { get; set; }

        /// <summary>
        /// 当前子节点层视觉树上最后一个节点
        /// </summary>
        public JsonTreeViewItem VisualLastItem { get; set; }
        #endregion

        #region Method
        /// <summary>
        /// 搜索视觉上的前一个与后一个节点
        /// </summary>
        public Tuple<JsonTreeViewItem, JsonTreeViewItem> SearchVisualPreviousAndNextItem(JsonTreeViewItem jsonTreeViewItem, bool isNeedSearchPrevious = true);

        /// <summary>
        /// 为指定节点集合的所有成员设置各自视觉上的前一个与后一个节点引用(二分查找最近邻居算法)
        /// </summary>
        /// <param name="treeViewItemList"></param>
        public void SetVisualPreviousAndNextForEachItem();

        /// <summary>
        /// 插入子节点
        /// </summary>
        /// <param name="targetIndex"></param>
        /// <param name="childData"></param>
        public void InsertChild(int targetIndex, JsonTreeViewDataStructure childData);

        /// <summary>
        /// 插入子节点集
        /// </summary>
        /// <param name="targetIndex"></param>
        /// <param name="childDataList"></param>
        public void InsertChildren(int targetIndex, JsonTreeViewDataStructure childDataList);

        public int GetDocumentLineCount();

        public void LoadDictionaryFromFile();

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
        /// 通过路径寻找节点
        /// </summary>
        /// <param name="interval"></param>
        /// <returns></returns>
        public Task<JsonTreeViewItem> FindNodeBySpecifyingPath(string path);
        #endregion
    }
}