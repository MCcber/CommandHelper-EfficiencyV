﻿using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Input;

namespace cbhk.CustomControls
{
    /// <summary>
    /// Tutorials.xaml 的交互逻辑
    /// </summary>
    public partial class Tutorials : UserControl
    {
        public Tutorials()
        {
            InitializeComponent();
        }

        private void OpenBasicClasses(object sender, MouseButtonEventArgs e)
        {
            Process.Start("explorer.exe", "https://commandtutorials.neocities.org/");
        }

        private void OpenAdvanceClasses(object sender, MouseButtonEventArgs e)
        {
            Process.Start("explorer.exe", "https://mc-command.oschina.io/command-tutorial/output/");
        }

        private void OpenOriginalEditionModClasses(object sender, MouseButtonEventArgs e)
        {
            Process.Start("explorer.exe", "https://ruhuasiyu.github.io/VanillaModTutorial/#%E5%91%BD%E4%BB%A4%E5%9F%BA%E7%A1%80");
        }
    }
}
