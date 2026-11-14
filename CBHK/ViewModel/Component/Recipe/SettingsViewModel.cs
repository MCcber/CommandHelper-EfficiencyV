using CBHK.Interface.Component.Recipe;
using CBHK.ViewModel.Component.Recipe.Event;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Prism.Events;
using System.Windows;

namespace CBHK.ViewModel.Component.Recipe
{
    /// <summary>
    /// 设置页：把编辑器的配方属性摊平，并按配方种类显隐字段
    /// </summary>
    public partial class SettingsViewModel : ObservableObject, IRecipePanel
    {
        private readonly IEventAggregator eventAggregator;

        public SettingsViewModel(RecipeEditorViewModelBase owner, IEventAggregator eventAggregator)
        {
            Owner = owner;
            this.eventAggregator = eventAggregator;
            CloseCommand = new RelayCommand(Close);
        }

        #region Property
        public RecipeEditorViewModelBase Owner { get; }

        /// <summary>
        /// 开关类按钮的显示状态：单向取自配方编辑器，点击由命令回落
        /// </summary>
        public bool ShapedChecked => Owner.Shaped;
        public bool MultiSelectChecked => Owner.MultiSelect;

        public Visibility ShapedVisibility => Owner.SupportsShaped ? Visibility.Visible : Visibility.Collapsed;
        public Visibility MultiSelectVisibility => Owner.SupportsMultiSelect ? Visibility.Visible : Visibility.Collapsed;
        public Visibility GroupNameVisibility => Owner.SupportsGroupName ? Visibility.Visible : Visibility.Collapsed;
        public Visibility CountVisibility => Owner.SupportsCount ? Visibility.Visible : Visibility.Collapsed;
        public Visibility ExperienceVisibility => Owner.SupportsExperience ? Visibility.Visible : Visibility.Collapsed;
        public Visibility CookingTimeVisibility => Owner.SupportsCookingTime ? Visibility.Visible : Visibility.Collapsed;
        public Visibility TrimPatternVisibility => Owner.SupportsTrimPattern ? Visibility.Visible : Visibility.Collapsed;

        public IRelayCommand CloseCommand { get; }
        #endregion

        #region Method
        [RelayCommand]
        private void ToggleShaped()
        {
            Owner.Shaped = !Owner.Shaped;
            OnPropertyChanged(nameof(ShapedChecked));
        }

        [RelayCommand]
        private void ToggleMultiSelect()
        {
            Owner.MultiSelect = !Owner.MultiSelect;
            OnPropertyChanged(nameof(MultiSelectChecked));
        }

        private void Close()
        {
            eventAggregator.GetEvent<CloseRecipePanelEvent>().Publish(this);
        }
        #endregion
    }
}
