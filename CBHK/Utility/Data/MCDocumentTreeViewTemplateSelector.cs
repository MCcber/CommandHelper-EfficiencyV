using MinecraftLanguageModelLibrary.Data;
using System.Windows;
using System.Windows.Controls;

namespace CBHK.Utility.Data
{
    public class MCDocumentTreeViewTemplateSelector : DataTemplateSelector
    {
        #region Field
        public DataTemplate StructTemplate { get; set; }
        public DataTemplate EnumTemplate { get; set; }
        public DataTemplate UnionTemplate { get; set; }
        public DataTemplate BoolTemplate { get; set; }
        public DataTemplate ColorTemplate { get; set; }
        public DataTemplate NumberTemplate { get; set; }
        public DataTemplate StringTemplate { get; set; }
        public DataTemplate ArrayTemplate { get; set; }
        public DataTemplate ListTemplate { get; set; }
        public DataTemplate UUIDTemplate { get; set; }
        public DataTemplate CompositeTemplate { get; set; }
        public DataTemplate AddButtonTemlate { get; set; }
        public DataTemplate RemoveTemplate { get; set; }
        public DataTemplate EntryTemplate { get; set; }
        public DataTemplate DefinitionTemplate { get; set; }
        #endregion

        #region Method
        #endregion

        #region Event
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item is MetaTypeEditorFieldDTO dto)
            {
                return dto.TypeKind switch
                {
                    MetaTypeKind.Entry => EntryTemplate,
                    MetaTypeKind.Definition => DefinitionTemplate,
                    MetaTypeKind.Add => AddButtonTemlate,
                    MetaTypeKind.Remove => RemoveTemplate,
                    MetaTypeKind.CompositeRGB or MetaTypeKind.CompositeARGB => ColorTemplate,
                    MetaTypeKind.HexRGB or MetaTypeKind.HexARGB => ColorTemplate,
                    MetaTypeKind.List => ListTemplate,
                    MetaTypeKind.DecRGB or MetaTypeKind.DecRGBA => ColorTemplate,
                    MetaTypeKind.UUIDArray => UUIDTemplate,
                    MetaTypeKind.Composite => CompositeTemplate,
                    MetaTypeKind.Struct => StructTemplate,
                    MetaTypeKind.Enum => EnumTemplate,
                    MetaTypeKind.Byte or MetaTypeKind.Short or MetaTypeKind.Int or MetaTypeKind.Float or MetaTypeKind.Double or MetaTypeKind.Long => NumberTemplate,
                    MetaTypeKind.String or MetaTypeKind.Any => StringTemplate,
                    MetaTypeKind.Boolean => BoolTemplate,
                    MetaTypeKind.Union => UnionTemplate,
                    MetaTypeKind.IntArray or MetaTypeKind.ByteArray or MetaTypeKind.LongArray => ArrayTemplate,
                    MetaTypeKind.Reference => new(),
                    _ => StringTemplate
                };
            }
            return base.SelectTemplate(item, container);
        }
        #endregion
    }
}