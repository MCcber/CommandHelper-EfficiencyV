using System.Windows;

namespace CBHK.Interface.Visual
{
    public interface IDragable
    {
        object DataType { get; }
        // 拖拽后的回调（可选）
        void OnDragCompleted(DragDropEffects result);
    }
}
