using System.Collections.Generic;

namespace cbhk.CustomControls.Interfaces
{
    /// <summary>
    /// 容器版本更新协议
    /// </summary>
    public interface IContainerVersionUpgrader
    {
        public List<IVersionUpgrader> SubContent { get; set; }
    }
}
