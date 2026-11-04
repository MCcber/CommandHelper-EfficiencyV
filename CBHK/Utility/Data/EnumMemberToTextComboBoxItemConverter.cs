using CBHK.CustomControl.VectorComboBox;
using MinecraftLanguageModelLibrary.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Data;

namespace CBHK.Utility.Data
{
    /// <summary>
    /// 枚举成员 / union 分支 → 下拉项。
    /// 同一个（名字 + 字面量）必须永远映射到同一个 VectorTextComboBoxItem：
    /// VectorTextComboBox 会把 ItemsSource 包进自己的 CollectionView，SelectedItem 与集合里的元素不是同一实例时，
    /// 选中态会被清空（表现为"下拉显示空白 / 选了立刻弹回"）。
    /// ConvertBack 回的是原始 EnumMember（而不是按下标新造的壳），分支切换才能命中同一个对象。
    /// </summary>
    public class EnumMemberToTextComboBoxItemConverter : IValueConverter
    {
        private sealed record Entry(VectorTextComboBoxItem Item, EnumMember Source);

        private static readonly Dictionary<string, Entry> byKey = new(StringComparer.Ordinal);
        // VectorTextComboBoxItem 重写了 Equals（按 ItemID），这里必须按引用区分
        private static readonly Dictionary<VectorTextComboBoxItem, EnumMember> byItem = new(ReferenceEqualityComparer.Instance);

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is IEnumerable<EnumMember> enumMembers)
            {
                ObservableCollection<object> resultList = [];
                foreach (var enumMember in enumMembers)
                {
                    if (enumMember?.Value?.LiteralValue is not null)
                    {
                        resultList.Add(ItemFor(enumMember));
                    }
                }
                return resultList;
            }
            else if (value is EnumMember enumMember && enumMember.Value?.LiteralValue is not null)
            {
                return ItemFor(enumMember);
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is VectorTextComboBoxItem vectorTextComboBoxItem)
            {
                if (byItem.TryGetValue(vectorTextComboBoxItem, out var source))
                {
                    return source;
                }
                return new EnumMember()
                {
                    Name = vectorTextComboBoxItem.Text,
                    Value = new MetaValue() { LiteralValue = vectorTextComboBoxItem.ItemID }
                };
            }
            return null;
        }

        private static VectorTextComboBoxItem ItemFor(EnumMember enumMember)
        {
            var literal = enumMember.Value.LiteralValue.ToString();
            var key = literal + "\u0001" + enumMember.Name;
            if (byKey.TryGetValue(key, out var cached))
            {
                return cached.Item;
            }
            var item = MCDocumentMetaTypeDTOHelper.BuildTextComboBoxItem(literal, enumMember.Name);
            byKey[key] = new Entry(item, enumMember);
            byItem[item] = enumMember;
            return item;
        }
    }
}
