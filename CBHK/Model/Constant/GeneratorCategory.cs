using System;

namespace CBHK.Model.Constant
{
    /// <summary>
    /// 主页分类按钮对应的功能/产出类别，枚举名就是按钮的稳定 Id（按钮文本只用于显示）。
    /// 位标志：一个入口可以同时属于多个类别，过滤时按位相与判断，命中的任一类都会列出它。
    /// </summary>
    [Flags]
    public enum GeneratorCategory
    {
        None = 0,
        Entity = 1 << 0,
        Item = 1 << 1,
        Resource = 1 << 2,
        World = 1 << 3,
        Datapack = 1 << 4,
        Command = 1 << 5,
        JsonFile = 1 << 6,
        NbtFile = 1 << 7,
    }
}
