using CBHK.CustomControl.Input;

namespace CBHK.Utility.Data.Mcdoc
{
    /// <summary>
    /// 行内的键输入框（新键占位行 / 条目行的重命名）。单独成类，是为了让行视图能按"事件目标"认出它：
    /// Preview 是隧道阶段，编辑器还没拿到键；不认出来就会把左右键当成"移动选中行"、把回车当成"展开某一行"
    /// （用户现场：重命名框里按回车，第一行 display 的子结构突然摊开）。
    /// </summary>
    public sealed class McdocKeyBox : VectorTextBox
    {
    }
}
