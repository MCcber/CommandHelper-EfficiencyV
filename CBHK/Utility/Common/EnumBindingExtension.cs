using CBHK.CustomControl.VectorComboBox;
using System;
using System.Linq;
using System.Windows.Markup;
using System.Windows.Media;

namespace CBHK.Utility.Common
{
    public class EnumBindingExtension(Type enumType) : MarkupExtension
    {
        #region Property
        public Brush DisplayPanelBrushKey { get; set; }
        public Brush MemberBrushKey { get; set; }
        #endregion

        #region Method
        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            if (!enumType.IsEnum || enumType is null) throw new ArgumentException("Type must be an Enum");

            // 直接在扩展内完成从 Enum 到 VectorComboBoxItem 的映射
            return Enum.GetValues(enumType)
                .Cast<Enum>()
                .Select(e => new VectorTextComboBoxItem()
                {
                    Text = e.ToString(),
                    DisplayPanelBrush = DisplayPanelBrushKey,
                    MemberBrush = MemberBrushKey
                }).ToList();
        }
        #endregion
    }
}