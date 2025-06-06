﻿using CBHK.CustomControl.JsonTreeViewComponents;
using ICSharpCode.AvalonEdit.Document;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using static CBHK.Model.Common.Enums;

namespace CBHK.CustomControl.Interfaces
{
    public interface ICustomWorldUnifiedPlan
    {
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
        public void UpdateNullValueBySpecifyingInterval(int endOffset, string newValue = "\r\n");

        public Dictionary<string, Dictionary<string, List<string>>> EnumCompoundDataDictionary { get; set; }

        public Dictionary<string, List<string>> EnumIDDictionary { get; set; }

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
    }
}