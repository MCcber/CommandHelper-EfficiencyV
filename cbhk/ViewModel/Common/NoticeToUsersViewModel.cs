using cbhk.Model;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Windows;
using System.Windows.Threading;

namespace cbhk.ViewModel.Common
{
    public partial class NoticeToUsersViewModel: ObservableObject
    {
        #region Field
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
                MainWindowProperties.ShowNotice = !donotShowNextTime;
            }
        }

        [ObservableProperty]
        private bool _understandButtonEnable = false;

        [ObservableProperty]
        private string _browseTimeBlockText = "";

        #endregion

        #region Method
        public NoticeToUsersViewModel()
        {
            DonotShowNextTime = !MainWindowProperties.ShowNotice;
            timer.Tick += Readed_Tick;
        }
        #endregion

        #region Event
        private void Readed_Tick(object sender, EventArgs e)
        {
            BrowseTimeBlockText = browseTime + "s";
            if (browseTime == 0)
            {
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
