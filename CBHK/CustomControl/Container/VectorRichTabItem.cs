using CommunityToolkit.Mvvm.Input;
using System.Windows;

namespace CBHK.CustomControl.Container
{
    public class VectorRichTabItem : VectorTextTabItem
    {
        #region Property
        public bool IsUnSaved
        {
            get { return (bool)GetValue(IsUnSavedProperty); }
            set { SetValue(IsUnSavedProperty, value); }
        }

        public static readonly DependencyProperty IsUnSavedProperty =
            DependencyProperty.Register("IsUnSaved", typeof(bool), typeof(VectorRichTabItem), new PropertyMetadata(default(bool)));

        public IRelayCommand SettingCommand
        {
            get { return (IRelayCommand)GetValue(SettingCommandProperty); }
            set { SetValue(SettingCommandProperty, value); }
        }

        public static readonly DependencyProperty SettingCommandProperty =
            DependencyProperty.Register("SettingCommand", typeof(IRelayCommand), typeof(VectorRichTabItem), new PropertyMetadata(default(IRelayCommand)));

        public IRelayCommand CloseCommand
        {
            get { return (IRelayCommand)GetValue(CloseCommandProperty); }
            set { SetValue(CloseCommandProperty, value); }
        }

        public static readonly DependencyProperty CloseCommandProperty =
            DependencyProperty.Register("CloseCommand", typeof(IRelayCommand), typeof(VectorRichTabItem), new PropertyMetadata(default(IRelayCommand)));
        #endregion
    }
}