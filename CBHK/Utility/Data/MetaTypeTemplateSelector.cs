using MinecraftLanguageModelLibrary.Data;
using System.Windows;
using System.Windows.Controls;

namespace CBHK.Utility.Data
{
    public class MetaTypeTemplateSelector : DataTemplateSelector
    {
        #region Property
        public DataTemplate StructTemplate { get; set; }
        public DataTemplate EnumTemplate { get; set; }
        //public DataTemplate DispatchTemplate { get; set; }
        public DataTemplate UnionTemplate { get; set; }
        public DataTemplate BoolTemplate { get; set; }
        public DataTemplate ColorTemplate { get; set; }
        public DataTemplate NumberTemplate { get; set; }
        public DataTemplate StringTemplate { get; set; }
        public DataTemplate ArrayTemplate { get; set; }
        public DataTemplate ListTemplate { get; set; }
        public DataTemplate UUIDTemplate { get; set; }
        public DataTemplate AddButtonTemlate { get; set; }
        public DataTemplate RemoveTemplate { get; set; }
        #endregion

        #region Event
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item is MetaTypeEditorFieldDTO dataField)
            {
                return dataField.TypeKind switch
                {
                    MetaTypeKind.Add => AddButtonTemlate,
                    MetaTypeKind.Remove => RemoveTemplate,
                    MetaTypeKind.CompositeRGB or MetaTypeKind.CompositeARGB => ColorTemplate,
                    MetaTypeKind.DecRGB or MetaTypeKind.DecRGBA => ColorTemplate,
                    MetaTypeKind.HexRGB or MetaTypeKind.HexARGB => ColorTemplate,
                    MetaTypeKind.UUIDArray => UUIDTemplate,
                    MetaTypeKind.List => ListTemplate,
                    MetaTypeKind.Struct => StructTemplate,
                    MetaTypeKind.Enum => EnumTemplate,
                    //MetaTypeKind.Dispatch => DispatchTemplate,
                    MetaTypeKind.Byte or MetaTypeKind.Short or MetaTypeKind.Int or MetaTypeKind.Float or MetaTypeKind.Double or MetaTypeKind.Long => NumberTemplate,
                    MetaTypeKind.String => StringTemplate,
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