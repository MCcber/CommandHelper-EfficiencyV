using System.Threading.Tasks;

namespace cbhk.CustomControls.Interfaces
{
    /// <summary>
    /// 版本更新协议
    /// </summary>
    public interface IVersionUpgrader
    {
        public Task Upgrade(int version);

        public Task<string> Result();
    }
}
