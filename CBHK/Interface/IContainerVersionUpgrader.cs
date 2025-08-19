using System.Collections.Generic;

namespace CBHK.Interface
{
    /// <summary>
    /// 容器版本更新协议
    /// </summary>
    public interface IContainerVersionUpgrader
    {
        public List<IVersionUpgrader> SubContent { get; set; }
    }
}
