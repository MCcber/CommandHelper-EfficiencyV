using CBHK.Model.Data;
using CBHK.ViewModel.Component.Recipe;
using Prism.Ioc;
using System.Collections.Generic;

namespace CBHK.Interface.Component.Recipe
{
    /// <summary>
    /// 一种配方的策略：负责种类标识、页签标题、导入识别与编辑器实例的创建
    /// </summary>
    public interface IRecipeStrategy
    {
        RecipeType Kind { get; }

        /// <summary>
        /// 与菜单项 Uid 一致
        /// </summary>
        string Uid { get; }

        string Title { get; }

        /// <summary>
        /// 导入时识别的 minecraft 配方类型串
        /// </summary>
        IReadOnlyList<string> JsonTypes { get; }

        RecipeEditorViewModelBase CreateEditor(IContainerProvider container);
    }
}
