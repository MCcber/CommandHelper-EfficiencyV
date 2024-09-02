using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Windows.Media;

namespace cbhk.Model.Generator.Tag
{
    /// <summary>
    /// 载入成员的属性模板
    /// </summary
    public class TagItemTemplate: ObservableObject
    {
        private SolidColorBrush background = null;
        public SolidColorBrush Background
        {
            get => background;
            set => SetProperty(ref background, value);
        }

        public Uri Icon { get; set; }
        public string DataType { get; set; }
        public string DisplayId { get; set; }
        public string DisplayName { get; set; }

        private bool? beChecked = false;
        public bool? BeChecked
        {
            get => beChecked;
            set => SetProperty(ref beChecked, value);
        }
    }
}
