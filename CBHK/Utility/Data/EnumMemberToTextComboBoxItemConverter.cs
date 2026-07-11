using CBHK.CustomControl.VectorComboBox;
using MinecraftLanguageModelLibrary.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Data;

namespace CBHK.Utility.Data
{
    public class EnumMemberToTextComboBoxItemConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if(value is IEnumerable<EnumMember> enumMembers)
            {
                ObservableCollection<object> resultList = [];
                foreach(var enumMember in enumMembers)
                {
                    if(enumMember.Value?.LiteralValue is not null)
                    {
                        resultList.Add(MCDocumentMetaTypeDTOHelper.BuildTextComboBoxItem(enumMember.Value.LiteralValue.ToString(), enumMember.Name));
                    }
                }
                return resultList;
            }
            else if(value is EnumMember enumMember && enumMember.Value?.LiteralValue is not null)
            {
                return MCDocumentMetaTypeDTOHelper.BuildTextComboBoxItem(enumMember.Value.LiteralValue.ToString(), enumMember.Name);
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if(value is VectorTextComboBoxItem vectorTextComboBoxItem)
            {
                return new EnumMember()
                {
                    Name = vectorTextComboBoxItem.Text,
                    Value = new MetaValue() { LiteralValue = vectorTextComboBoxItem.ItemID }
                };
            }
            return null;
        }
    }
}
