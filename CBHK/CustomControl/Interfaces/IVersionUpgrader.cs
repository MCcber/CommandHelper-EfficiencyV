using System.Threading.Tasks;

namespace CBHK.CustomControl.Interfaces
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
