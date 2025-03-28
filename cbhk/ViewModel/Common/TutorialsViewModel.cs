using CommunityToolkit.Mvvm.ComponentModel;
using System.Diagnostics;
using System.Windows.Input;

namespace CBHK.ViewModel.Common
{
    public class TutorialsViewModel: ObservableObject
    {
        public void OpenBasicClasses(object sender, MouseButtonEventArgs e)
        {
            Process.Start("explorer.exe", "https://commandtutorials.neocities.org/");
        }

        public void OpenAdvanceClasses(object sender, MouseButtonEventArgs e)
        {
            Process.Start("explorer.exe", "https://mc-command.oschina.io/command-tutorial/output/");
        }

        public void OpenOriginalEditionModClasses(object sender, MouseButtonEventArgs e)
        {
            Process.Start("explorer.exe", "https://ruhuasiyu.github.io/VanillaModTutorial/#%E5%91%BD%E4%BB%A4%E5%9F%BA%E7%A1%80");
        }
    }
}
