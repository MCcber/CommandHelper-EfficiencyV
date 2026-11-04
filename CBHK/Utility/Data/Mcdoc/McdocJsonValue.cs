// 最小 JSON 值模型：默认值生成 / 类型变更 / 运行节点访问器共用。
// 不给 WPF 用（WPF 侧另有树视图），只是求值与编辑回环需要的载体。
using System.Collections.Generic;

namespace CBHK.Utility.Data.Mcdoc
{
    public enum McdocJsonKind { Null, Boolean, Number, String, Array, Object }

    public sealed class McdocJsonValue
    {
        public McdocJsonKind Kind = McdocJsonKind.Null;
        public bool BooleanValue;
        public decimal NumberValue;
        /// <summary>数值是否为整数（决定写 long 还是 float）。</summary>
        public bool IsInteger = true;
        public string StringValue = "";
        public List<McdocJsonValue> Items;
        public List<KeyValuePair<string, McdocJsonValue>> Properties;

        /// <summary>在源文本中的字符区间，未知为 -1。</summary>
        public int Start = -1;
        public int End = -1;

        public static McdocJsonValue Null() => new() { Kind = McdocJsonKind.Null };
        public static McdocJsonValue Boolean(bool value) => new() { Kind = McdocJsonKind.Boolean, BooleanValue = value };
        public static McdocJsonValue String(string value) => new() { Kind = McdocJsonKind.String, StringValue = value ?? "" };
        public static McdocJsonValue Number(decimal value, bool isInteger = true)
            => new() { Kind = McdocJsonKind.Number, NumberValue = value, IsInteger = isInteger };
        public static McdocJsonValue Array() => new() { Kind = McdocJsonKind.Array, Items = [] };
        public static McdocJsonValue Object() => new() { Kind = McdocJsonKind.Object, Properties = [] };

        /// <summary>深拷贝（右键"复制"用：内存里复制一份节点数据再插入同层）。</summary>
        public McdocJsonValue Clone()
        {
            var copy = (McdocJsonValue)MemberwiseClone();
            copy.Start = -1;
            copy.End = -1;
            if (Items is not null)
            {
                copy.Items = [];
                foreach (var item in Items) copy.Items.Add(item?.Clone());
            }
            if (Properties is not null)
            {
                copy.Properties = [];
                foreach (var pair in Properties)
                {
                    copy.Properties.Add(new KeyValuePair<string, McdocJsonValue>(pair.Key, pair.Value?.Clone()));
                }
            }
            return copy;
        }

        public McdocJsonValue Add(string key, McdocJsonValue value)
        {
            McdocWriteTrace.LogKey(Start, key);
            Properties ??= [];
            Properties.Add(new KeyValuePair<string, McdocJsonValue>(key, value));
            return this;
        }

        public McdocJsonValue Add(McdocJsonValue item)
        {
            Items ??= [];
            Items.Add(item);
            return this;
        }

        public McdocJsonValue Get(string key)
        {
            if (Properties == null) return null;
            foreach (var pair in Properties)
            {
                if (pair.Key == key) return pair.Value;
            }
            return null;
        }

        /// <summary>把 JSON 值反推成 mcdoc 类型，用于 dynamic 索引 / 未知分支的类型推断。</summary>
        public McdocType InferType()
        {
            switch (Kind)
            {
                case McdocJsonKind.Boolean:
                    return new McdocType
                    {
                        Kind = McdocTypeKind.Literal,
                        LiteralKind = McdocLiteralKind.Boolean,
                        Value = BooleanValue
                    };
                case McdocJsonKind.Number:
                    return new McdocType
                    {
                        Kind = McdocTypeKind.Literal,
                        LiteralKind = IsInteger ? McdocLiteralKind.Int : McdocLiteralKind.Float,
                        Value = NumberValue
                    };
                case McdocJsonKind.String:
                    return McdocType.LiteralString(StringValue);
                case McdocJsonKind.Array:
                    return new McdocType { Kind = McdocTypeKind.List, Item = McdocType.Any() };
                case McdocJsonKind.Object:
                    return new McdocType { Kind = McdocTypeKind.Struct, Fields = [] };
                default:
                    return McdocType.Any();
            }
        }
    }
}
