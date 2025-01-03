namespace cbhk.Model.Common
{
    public class Enums
    {
        public enum ModifyType
        {
            Remove,
            Get
        }

        public enum ReplaceType
        {
            Direct,
            Input,
            String,
            AddElement,
            RemoveElement,
            AddArrayElement,
            RemoveArrayElement,
            Compound
        }

        public enum DataTypes
        {
            Input,
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
