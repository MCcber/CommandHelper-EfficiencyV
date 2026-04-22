using CBHK.Model.Common;
using System.Windows.Media;

namespace CBHK.Interface.TreeView
{
    // 非泛型基接口
    public interface INumberItem : IBaseItem, IBaseKeyItem
    {
        object Value { get; set; }
        NumberType ValueType { get; }
    }

    // 泛型接口继承非泛型接口，使得可以触发WPF隐性模板选择机制，既能忽略泛型在视图中的存在，又能在代码中使用强类型访问
    public interface INumberItem<T> : INumberItem where T : struct
    {
        new T Value { get; set; }
    }
}