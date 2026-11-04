using System;
using System.Diagnostics;

namespace CBHK.Utility.Data.Mcdoc
{
    /// <summary>
    /// 写回路径的调试开关（MCDOC_TRACE_WRITE=1）：只打印"被跳过"的写回及其断点，
    /// 平时零开销。同类问题（值被写到上层 / 删了又回来）先开这个再复现，能直接看到调用点。
    /// </summary>
    internal static class McdocWriteTrace
    {
        /// <summary>开关是否打开（MCDOC_TRACE_WRITE=1）。诊断类解析只在打开时执行，不进生产热路径。</summary>
        public static bool Enabled { get; } =
            string.Equals(Environment.GetEnvironmentVariable("MCDOC_TRACE_WRITE"), "1", StringComparison.Ordinal);

        /// <summary>往对象里新增键的原语：带上调用栈，用来点名"谁把子字段提到上层"。</summary>
        public static void LogKey(int ownerStart, string key)
        {
            if (!Enabled) return;
            Console.WriteLine("[mcdoc-write] +key " + key + " ownerStart=" + ownerStart + Caller());
        }

        /// <summary>节点没有路径时（根/分支层）的定位信息：源文本区间足以区分是哪个对象。</summary>
        public static void LogText(string tag, string text, bool withCaller = false)
        {
            if (!Enabled) return;
            var suffix = withCaller ? Caller() : "";
            if (text is null) { Console.WriteLine("[mcdoc-write] " + tag + " <null>" + suffix); return; }
            var clipped = text.Length > 120 ? text.Substring(0, 120) : text;
            Console.WriteLine("[mcdoc-write] " + tag + " len=" + text.Length + " text=" + clipped.Replace("\r", " ").Replace("\n", " ") + suffix);
        }

        /// <summary>整篇写回的物理次数：同一拍出现两次就是"两个根"的现场。</summary>
        public static int WholeWriteCount { get; private set; }

        /// <summary>
        /// 记录一次整篇写回：调用方 + 前后长度 + 落盘后文本能否解析成单个根。
        /// 只在开关打开时解析，平时只有一次自增。
        /// </summary>
        public static void LogWholeWrite(string tag, int oldLength, string documentText, bool keepRootIdentity, bool repaired = false)
        {
            WholeWriteCount++;
            if (!Enabled) return;
            var singleRoot = true;
            McdocJsonParser.SyntaxError error = null;
            if (!string.IsNullOrWhiteSpace(documentText) && !McdocJsonParser.TryParse(documentText, out _, out error))
            {
                singleRoot = false;
            }
            Console.WriteLine("[mcdoc-write] whole#" + WholeWriteCount + " " + tag
                + " old=" + oldLength + " target=" + (documentText?.Length ?? 0)
                + " keepRoot=" + keepRootIdentity + " singleRoot=" + singleRoot
                + (repaired ? " repaired" : "")
                + (singleRoot ? "" : " err=" + error?.Message) + Caller());
        }

        /// <summary>对象键集合：判断"根形状有没有被换掉"的唯一依据。</summary>
        public static string Keys(McdocJsonValue value)
        {
            if (value is null) return "<null>";
            if (value is not { Kind: McdocJsonKind.Object } || value.Properties is null) return "<非对象:" + value.Kind + ">";
            return "{" + string.Join(",", value.Properties.ConvertAll(pair => pair.Key).ToArray()) + "}";
        }

        private static string Caller()
        {
            var frames = new StackTrace(2, false).GetFrames();
            if (frames is null) return "";
            var text = "";
            for (var i = 0; i < frames.Length && i < 4; i++)
            {
                var method = frames[i].GetMethod();
                text += " <- " + (method?.DeclaringType?.Name ?? "?") + "." + (method?.Name ?? "?");
            }
            return text;
        }

        public static void Log(string action, McdocTreeNode node, string brokenAt)
        {
            if (!Enabled) return;
            Console.WriteLine("[mcdoc-write] " + action + " path=" + (node?.JsonPath ?? "?")
                + " field=" + (node?.FieldName ?? "?") + " brokenAt=" + brokenAt);
        }
    }
}
