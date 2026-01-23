namespace CBHK.Model.Common
{
    public enum ModifyType
    {
        Remove,
        Get
    }

    public enum ModiferType
    {
        Range,
        Length
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

    public enum RecipeType
    {
        CraftingTable,
        Furnace,
        BlastFurnace,
        Campfire,
        SmithingTable,
        Smoker,
        Stonecutter
    }

    public enum PackDescriptionType
    {
        StringType,
        BoolType,
        IntType,
        ObjectType,
        ArrayType
    };
}
