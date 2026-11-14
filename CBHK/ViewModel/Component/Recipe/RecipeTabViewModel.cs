using CBHK.Interface.Component.Recipe;
using CBHK.ViewModel.Component.Recipe.Event;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Prism.Events;
using System;

namespace CBHK.ViewModel.Component.Recipe
{
    /// <summary>
    /// 一个配方页签：持有编辑器，并在配方页/设置页/搜索页之间切换内容
    /// </summary>
    public partial class RecipeTabViewModel : ObservableObject, IDisposable
    {
        private readonly IEventAggregator eventAggregator;
        private readonly SubscriptionToken requestToken;
        private readonly SubscriptionToken closePanelToken;

        public RecipeTabViewModel(RecipeEditorViewModelBase editor, IRecipeStrategy strategy, IEventAggregator eventAggregator)
        {
            Editor = editor;
            Strategy = strategy;
            this.eventAggregator = eventAggregator;

            requestToken = eventAggregator.GetEvent<RequestRecipePanelEvent>()
                .Subscribe(RequestPanel, ThreadOption.UIThread, false, panel => panel.Owner == Editor);
            closePanelToken = eventAggregator.GetEvent<CloseRecipePanelEvent>()
                .Subscribe(ClosePanel, ThreadOption.UIThread, false, panel => panel.Owner == Editor);

            _activeContent = editor;
            OpenSettingsCommand = new RelayCommand(ToggleSettings);
            CloseCommand = new RelayCommand(Close);
        }

        #region Property
        public IRecipeStrategy Strategy { get; }
        public RecipeEditorViewModelBase Editor { get; }
        public string Title => Strategy.Title;

        /// <summary>
        /// 页签内容：配方页 / 设置页 / 搜索页
        /// </summary>
        [ObservableProperty]
        private object _activeContent;

        [ObservableProperty]
        private bool _isUnSaved;

        public IRelayCommand OpenSettingsCommand { get; }
        public IRelayCommand CloseCommand { get; }
        #endregion

        #region Method
        private void RequestPanel(IRecipePanel panel)
        {
            ApplyPendingPanel();
            ActiveContent = panel;
        }

        /// <summary>
        /// 搜索页被顶替前把 Key/Tag 落盘，避免静默丢编辑
        /// </summary>
        private void ApplyPendingPanel()
        {
            if (ActiveContent is SearchViewModel searchPanel)
                searchPanel.Apply();
        }

        private void ClosePanel(IRecipePanel panel)
        {
            if (!ReferenceEquals(ActiveContent, panel))
                return;
            ActiveContent = Editor;
        }

        /// <summary>
        /// 齿轮按钮：在设置页与配方页之间切换
        /// </summary>
        private void ToggleSettings()
        {
            ApplyPendingPanel();
            ActiveContent = ActiveContent is SettingsViewModel ? Editor : new SettingsViewModel(Editor, eventAggregator);
        }

        private void Close()
        {
            eventAggregator.GetEvent<CloseRecipeTabEvent>().Publish(this);
        }

        public void Dispose()
        {
            eventAggregator.GetEvent<RequestRecipePanelEvent>().Unsubscribe(requestToken);
            eventAggregator.GetEvent<CloseRecipePanelEvent>().Unsubscribe(closePanelToken);
        }
        #endregion
    }
}
