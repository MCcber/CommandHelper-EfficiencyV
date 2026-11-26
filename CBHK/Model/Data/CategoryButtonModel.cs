using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows.Media;

namespace CBHK.Model.Data
{
    /// <summary>主页分类栏的一项。命令由 ItemsControl 的 DataContext（MainViewModel）统一提供。</summary>
    public class CategoryButtonModel : ObservableObject
    {
        private bool isChecked;

        public bool IsChecked
        {
            get => isChecked;
            set => SetProperty(ref isChecked, value);
        }

        public ImageSource Icon { get; set; }
        public string Text { get; set; }
        public string ID { get; set; }
    }
}
