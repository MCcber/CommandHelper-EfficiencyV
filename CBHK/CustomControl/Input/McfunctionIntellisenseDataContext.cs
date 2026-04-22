using System;
using System.Collections.Generic;

namespace CBHK.CustomControl.Input
{
    /// <summary>
    /// 自动补全数据上下文
    /// </summary>
    [Serializable]
    public class McfunctionIntellisenseDataContext
    {
        /// <summary>
        /// 当前上下文的唯一标记码
        /// </summary>
        public string Uid { get; set; } = Guid.NewGuid().ToString();
        /// <summary>
        /// 命令路径
        /// </summary>
        public string CommandPath { get; set; } = "";
        /// <summary>
        /// 需要补全宏变量
        /// </summary>
        public bool IsCompleteMacros { get; set; } = false;
        /// <summary>
        /// 需要补全坐标
        /// </summary>
        public bool IsCompletePos { get; set; } = false;
        /// <summary>
        /// 需要补全选择器参数
        /// </summary>
        public bool IsCompleteSelectorParameters { get; set; } = false;
        /// <summary>
        /// 需要补全选择器参数值
        /// </summary>
        public bool IsCompleteSelectorParameterValues { get; set; } = false;
        /// <summary>
        /// 需要补全大纲
        /// </summary>
        public bool IsCompleteOutline { get; set; } = false;
        /// <summary>
        /// 需要补全运行时变量
        /// </summary>
        public bool IsCompleteRuntimeVariable { get; set; } = false;

        #region 坐标数据
        public string CoordinateType { get; set; } = "";
        public int CoordinateCount { get; set; } = 0;
        #endregion

        /// <summary>
        /// 选择器内联上下文
        /// </summary>
        public string SelectorInlineContext { get; set; } = "";
        /// <summary>
        /// 保存正在键入的内容
        /// </summary>
        public string TypingContent { get; set; } = "";
        /// <summary>
        /// 抓取当前光标所在的文本
        /// </summary>
        public string CurrentCode { get; set; } = "";
        /// <summary>
        /// 当前游戏规则名
        /// </summary>
        public string CurrentGameRuleName { get; set; } = "";
        /// <summary>
        /// 计算光标对于当前命令上下文的偏移量
        /// </summary>
        public int CurrentCaretIndex { get; set; } = 0;
        /// <summary>
        /// 标记语法树是否已结束
        /// </summary>
        public bool IsCompletionOver { get; set; } = false;
        /// <summary>
        /// 记录当前行的索引
        /// </summary>
        public int CurrentLineIndex { get; set; } = 0;
        /// <summary>
        /// 执行了粘贴
        /// </summary>
        public bool RunningPaste { get; set; } = false;

        /// <summary>
        /// 是否需要计算数据
        /// </summary>
        public bool IsNeedCalculate { get; set; } = true;

        /// <summary>
        /// 需要被移除的宏定义行索引
        /// </summary>
        public List<int> RemoveMacroIndexes { get; set; } = [];

        /// <summary>
        /// 补全框与它的显示状态
        /// </summary>
        public bool IsCompletionWindowShowing { get; set; } = true;

        #region 编码变量及标记
        public bool TypingBossbarIdString { get; set; } = false;
        public string currentBossbarIdString { get; set; } = "";

        public bool TypingScoreboardVariable { get; set; } = false;
        public string currentScoreboardVariable { get; set; } = "";

        public bool TypingTriggerVariable { get; set; } = false;
        public string currentTriggerVariable { get; set; } = "";

        public bool TypingStorageIdString { get; set; } = false;
        public string currentStorageVariable { get; set; } = "";

        public bool TypingTagVariable { get; set; } = false;
        public string currentTagVariable { get; set; } = "";

        public bool TypingTeamNameString { get; set; } = false;
        public string currentTeamVariable { get; set; } = "";
        #endregion
    }
}
