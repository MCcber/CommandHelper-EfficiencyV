namespace CBHK.Model.Common
{
    public class Enums
    {
        public enum ModifyType
        {
            Remove,
            Get
        }

        public enum MoveDirection
        {
            Up,
            Down
        }

        public enum ChangeType
        {
            None,
            NumberAndBool,
            String,
            AddCompoundObject,
            AddListElement,
            AddListElementToEnd,
            RemoveCompound,
            RemoveList,
            RemoveListElement,
        }

        public enum DataType
        {
            None,
            Number,
            Bool,
            String,
            Enum,
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
            EnumCompound,
            MultiType,
            ArrayElement,
            Array,
            List,
            ListElement
        }
    }
}
