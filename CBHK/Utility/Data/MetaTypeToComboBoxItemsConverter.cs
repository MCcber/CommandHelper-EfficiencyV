using CBHK.CustomControl.VectorComboBox;
using MinecraftLanguageModelLibrary.Data;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace CBHK.Utility.Data
{
    public class MetaTypeToComboBoxItemsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isEnum = parameter is string p && p == "Enum";

            return value switch
            {
                // 枚举成员列表
                IEnumerable<EnumMember> enumMemberCollection => [.. enumMemberCollection.Select(item => new VectorTextComboBoxItem
                {
                    ItemID = item.Name,
                    Text = $"{item.Name} = {item.Value}",
                    IsSelected = false
                })],

                // 联合类型选项列表
                IEnumerable<MetaType> unionOptionCollection => [.. unionOptionCollection.Select(item => new VectorTextComboBoxItem
                {
                    ItemID = item.Name ?? item.ReferencePath,
                    Text = item.Name ?? item.ReferencePath,
                    IsSelected = false
                })],

                IEnumerable<string> stringOptionCollection => stringOptionCollection.Select(item => 
                {
                    string[] list = item.Split('=');

                    if (list.Length > 1)
                    {
                        return new VectorTextComboBoxItem
                        {
                            ItemID = list[1] ?? "",
                            Text = list[0],
                            IsSelected = false
                        };
                    }
                    else
                    {
                        return new VectorTextComboBoxItem
                        {
                            ItemID = item ?? "",
                            Text = item,
                            IsSelected = false
                        };
                    }
                }).ToList(),

                _ => null
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
