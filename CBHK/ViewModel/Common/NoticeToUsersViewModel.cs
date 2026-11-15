using CBHK.Domain;
using CBHK.Domain.Model;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Windows;
using System.Windows.Threading;

namespace CBHK.ViewModel.Common
{
    public partial class NoticeToUsersViewModel: ObservableObject
    {
        #region Field
        private EnvironmentSettingsService settingsService = null;
        private EnvironmentSettings config = null;
        private int browseTime = 5;
        DispatcherTimer timer = new()
        {
            IsEnabled = true,
            Interval = TimeSpan.FromSeconds(1)
        };
        #endregion

        #region Property
        private bool donotShowNextTime = false;
        public bool DonotShowNextTime
        {
            get => donotShowNextTime;
            set
            {
                SetProperty(ref donotShowNextTime, value);
                config.ShowNotice = (!donotShowNextTime).ToString();
            }
        }

        [ObservableProperty]
        private Visibility countDownVisibility = Visibility.Visible;
        [ObservableProperty]
        private bool understandButtonEnable = false;

        [ObservableProperty]
        private string browseTimeBlockText = "";

        #endregion

        #region Method
        public NoticeToUsersViewModel(EnvironmentSettingsService settingsService)
        {
            this.settingsService = settingsService;
            config = settingsService.Current;
            timer.Tick += Readed_Tick;
        }
        #endregion

        #region Event
        private void Readed_Tick(object sender, EventArgs e)
        {
            BrowseTimeBlockText = browseTime + "s";
            if (browseTime == 0)
            {
                CountDownVisibility = Visibility.Hidden;
                timer.IsEnabled = false;
                UnderstandButtonEnable = true;
            }
            browseTime--;
        }

        [RelayCommand]
        public void UnderStand(Window window) => window.Close();
        #endregion
    }
}
