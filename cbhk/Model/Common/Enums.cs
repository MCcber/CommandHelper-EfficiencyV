namespace cbhk.Model.Common
{
    public class Enums
    {
        public enum ModifyType
        {
            Remove,
            Get
        }

        public enum ChangeType
        {
            None,
            NumberAndBool,
            String,
            AddCompoundObject,
            AddArrayElement,
            AddArrayElementToEnd,
            RemoveCompound,
            RemoveCompoundObject,
            RemoveArray,
            RemoveArrayElement,
        }

        public enum DataType
        {
            None,
            Number,
            Bool,
            String,
            BlockID,
            ItemID,
            BlockTag,
            ItemTag,
            Enum,
            EntityID,
            Byte,
            Decimal,
            Short,
            Int,
            Float,
            Double,
            Long,
            Compound,
            CustomCompound,
            OptionalCompound,
            NullableCompound,
            OptionalAndNullableCompound,
            EnumCompound,
            MultiType,
            ArrayElement,
            Array,
            InnerArray,
            List,
            Any
        }
    }
}
