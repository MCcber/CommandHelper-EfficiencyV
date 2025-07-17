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
            Object,
            Number,
            Bool,
            String,
            Byte,
            Decimal,
            Short,
            Int,
            Float,
            Double,
            Long
        }

        public enum ItemType
        {
            BottomButton,
            Enum,
            Compound,
            CustomCompound,
            OptionalCompound,
            MultiType,
            Array,
            List
        }
    }
}
