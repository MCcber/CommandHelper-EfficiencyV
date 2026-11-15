using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace CBHK.Model.Generator.Tag
{
    /// <summary>
    /// 载入成员的属性模板
    /// </summary>
    public class TagItemTemplate : ObservableObject
    {
        public Uri Icon { get; set; }
        public string DataType { get; set; }
        public string DisplayId { get; set; }
        public string DisplayName { get; set; }

        /// <summary>选中态：行首的勾与导出时是否收进 values 都看这一个标记。</summary>
        private bool? beChecked = false;
        public bool? BeChecked
        {
            get => beChecked;
            set => SetProperty(ref beChecked, value);
        }
    }
}
