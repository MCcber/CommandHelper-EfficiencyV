using System.Collections.Generic;

namespace CBHK.Utility.Common
{
    public static class StringIDPool
    {
        private static readonly List<string> IdToValue = [""]; // 0 预留给空字符串
        private static readonly Dictionary<string, int> ValueToId = new() { { "", 0 } };

        public static int GetId(string value)
        {
            if (value == null) return 0;
            if (ValueToId.TryGetValue(value, out int id)) return id;

            lock (IdToValue) // 确保线程安全
            {
                if (ValueToId.TryGetValue(value, out id)) return id;
                id = IdToValue.Count;
                IdToValue.Add(value);
                ValueToId.Add(value, id);
                return id;
            }
        }

        public static string GetValue(int id) => (id >= 0 && id < IdToValue.Count) ? IdToValue[id] : "";
    }
}
