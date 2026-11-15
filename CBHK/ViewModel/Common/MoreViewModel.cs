using CBHK.CustomControl.VectorButton;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace CBHK.ViewModel.Common
{
    public partial class MoreViewModel : ObservableObject
    {
        #region Field
        private List<string> LinkTargetList = ["https://space.bilibili.com/333868639", "https://space.bilibili.com/170651403/?spm_id_from=333.999.0.0", "https://space.bilibili.com/521673619/?spm_id_from=333.999.0.0", "https://space.bilibili.com/67131398/?spm_id_from=333.999.0.0", "https://space.bilibili.com/350848639/?spm_id_from=333.999.0.0", "https://space.bilibili.com/15174905/?spm_id_from=333.999.0.0", "https://space.bilibili.com/57030021/?spm_id_from=333.999.0.0", "https://space.bilibili.com/590541175?spm_id_from=333.337.0.0", "https://space.bilibili.com/413164365/?spm_id_from=333.999.0.0"];
        private string baseHeadLinkString = "/Resource/CBHK/Image/More/Thanks/";
        #endregion

        #region Event
        [RelayCommand]
        private void MemberHeadLoaded(object sender)
        {
            if (sender is VectorFlatImageButton button)
            {
                if (int.TryParse(button.Uid, out int result))
                {
                    button.Image = new BitmapImage(new Uri(baseHeadLinkString + result + ".png", UriKind.Relative));
                }
            }
        }

        [RelayCommand]
        private void MemberButtonMouseHover(object sender)
        {
            if (sender is VectorFlatImageButton button)
            {
                if (int.TryParse(button.Uid, out int result))
                {
                    button.Image = new BitmapImage(new Uri(baseHeadLinkString + result + "Hover.png", UriKind.Relative));
                }
            }
        }

        [RelayCommand]
        private void MemberButtonMouseLeave(object sender)
        {
            MemberHeadLoaded(sender);
        }

        [RelayCommand]
        public void GoToWebSite(string uid)
        {
            if (int.TryParse(uid, out int index))
            {
                var psi = new ProcessStartInfo
                {
                    FileName = LinkTargetList[index],
                    UseShellExecute = true
                };
                Process.Start(psi);
            }
        }

        public void ClickToJoinGroup(object sender, MouseButtonEventArgs e)
        {
            var psi = new ProcessStartInfo
            {
                FileName = "https://jq.qq.com/?_wv=1027&k=72W2xZYe",
                UseShellExecute = true
            };
            Process.Start(psi);
        }

        public void FeedBackBlock_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var psi = new ProcessStartInfo
            {
                FileName = "https://github.com/MCcber/CommandHelper-EfficiencyV/issues",
                UseShellExecute = true
            };
            Process.Start(psi);
        }

        #endregion
    }
}