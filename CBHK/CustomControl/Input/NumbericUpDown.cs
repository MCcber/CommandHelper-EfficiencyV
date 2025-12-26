using CommunityToolkit.Mvvm.Input;
using System.Windows;
using System.Windows.Controls;

namespace CBHK.CustomControl.Input
{
    public class NumbericUpDown : Control
    {
        public RelayCommand IncreaseCommand
        {
            get { return (RelayCommand)GetValue(IncreaseCommandProperty); }
            set { SetValue(IncreaseCommandProperty, value); }
        }

        public static readonly DependencyProperty IncreaseCommandProperty =
            DependencyProperty.Register("IncreaseCommand", typeof(RelayCommand), typeof(NumbericUpDown), new PropertyMetadata(default(RelayCommand)));

        public RelayCommand DecreaseCommand
        {
            get { return (RelayCommand)GetValue(DecreaseCommandProperty); }
            set { SetValue(DecreaseCommandProperty, value); }
        }

        public static readonly DependencyProperty DecreaseCommandProperty =
            DependencyProperty.Register("DecreaseCommand", typeof(RelayCommand), typeof(NumbericUpDown), new PropertyMetadata(default(RelayCommand)));

        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register("Text", typeof(string), typeof(NumbericUpDown), new PropertyMetadata(default(string)));
    }
}
