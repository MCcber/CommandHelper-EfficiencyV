using System.Linq;

namespace CBHK.Utility.Data.Mcdoc
{
    /// <summary>
    /// 把一次差量同步的结果落到投影上。VM 与探针共用这一份：门禁量的必须是界面上真正跑的那段代码，
    /// 否则"整表重投影 = 0"只是量了探针自己的另一套拼接。
    /// </summary>
    public static class McdocProjectionSync
    {
        public static void Apply(McdocRowProjector projector, McdocTreeDocument document, McdocSyncStats stats)
        {
            // 根层成员增删：顶层那一段（被重建的顶层节点 / 新插的 / 被摘掉的）按上一代清单分段重拼；
            // 其余重建节点仍然只重拼父行。两步都不做整表 Reset。
            var splicedTopLevel = stats.PreviousTopLevel is not null;
            if (splicedTopLevel)
            {
                projector.SpliceTopLevels(stats.PreviousTopLevel, document.Nodes.ToList());
            }
            foreach (var node in stats.RebuiltNodeList)
            {
                if (!projector.RefreshAround(node) && !splicedTopLevel)
                {
                    projector.Reset(document.Nodes.ToList());
                }
            }
            // 重建了节点却没拿到对应的行（局部拼接没覆盖到）：整表重置，绝不让投影停在上一代
            if (!splicedTopLevel && stats.RebuiltNodes > 0 && stats.RebuiltNodeList.Count != stats.RebuiltNodes)
            {
                projector.Reset(document.Nodes.ToList());
            }
        }
    }
}
