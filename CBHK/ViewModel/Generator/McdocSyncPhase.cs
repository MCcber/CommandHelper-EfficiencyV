namespace CBHK.ViewModel.Generator
{
    /// <summary>
    /// 文本通道的阶段（只读派生自既有布尔闸门，<b>不参与判定</b>）：把"7 个布尔组合出什么状态"变成可读、可打点的一层。
    /// 顺序即优先级：消化中 &gt; 重建中 &gt; 等防抖 &gt; 回写中 &gt; 空闲。
    /// </summary>
    public enum McdocSyncPhase
    {
        Idle,
        /// <summary>正在把编辑器文本吃进树（TryApply / 树侧回写被抑制）。</summary>
        Digesting,
        /// <summary>建树 / 局部重建中（构建期赋值不许回写文本）。</summary>
        Rebuilding,
        /// <summary>用户还在打字，文本已记下、等失焦或 60ms 补跑（树暂时滞后）。</summary>
        Deferred,
        /// <summary>树 → 文本整篇回写中（期间不触发二次消化）。</summary>
        Pushing
    }
}
