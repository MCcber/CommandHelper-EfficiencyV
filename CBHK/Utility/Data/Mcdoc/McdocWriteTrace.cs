using System;
using System.Diagnostics;

namespace CBHK.Utility.Data.Mcdoc
{
    /// <summary>
    /// 写回路径的调试开关（MCDOC_TRACE_WRITE=1）：只打印"被跳过"的写回及其断点，
    /// 平时零开销。同类问题（值被写到上层 / 删了又回来）先开这个再复现，能直接看到调用点。
    /// </summary>
    public static class McdocWriteTrace
    {
        /// <summary>开关是否打开（MCDOC_TRACE_WRITE=1）。诊断类解析只在打开时执行，不进生产热路径。</summary>
        public static bool Enabled { get; } =
            string.Equals(Environment.GetEnvironmentVariable("MCDOC_TRACE_WRITE"), "1", StringComparison.Ordinal)
            // 从 VS 里 F5 启动时没法带环境变量：在 exe 旁边放一个空文件 mcdoc-trace.on 同样能开
            || System.IO.File.Exists(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "mcdoc-trace.on"));

        /// <summary>宿主是 WinExe，没有控制台：开关打开时同步落一份文件，复现完直接把文件拿走。</summary>
        public static string TracePath { get; } =
            Environment.GetEnvironmentVariable("MCDOC_TRACE_FILE")
            ?? System.IO.Path.Combine(System.IO.Path.GetTempPath(), "mcdoc-trace.log");

        private static void Sink(string line)
        {
            Console.WriteLine(line);
            try
            {
                System.IO.File.AppendAllText(TracePath,
                    DateTime.Now.ToString("HH:mm:ss.fff") + " " + line + Environment.NewLine);
            }
            catch
            {
                // 落盘失败不影响主流程：诊断永远不能把编辑器搞崩
            }
        }

        /// <summary>结构化现场：一条动作 + 关键状态，复现日志靠它定位「哪一步把行/按钮吃掉了」。</summary>
        public static void Trace(string tag, string detail)
        {
            if (!Enabled) return;
            Sink("[mcdoc-trace] " + tag + " " + detail);
        }

        /// <summary>往对象里新增键的原语：带上调用栈，用来点名"谁把子字段提到上层"。</summary>
        public static void LogKey(int ownerStart, string key)
        {
            if (!Enabled) return;
            Sink("[mcdoc-write] +key " + key + " ownerStart=" + ownerStart + Caller());
        }

        /// <summary>节点没有路径时（根/分支层）的定位信息：源文本区间足以区分是哪个对象。</summary>
        public static void LogText(string tag, string text, bool withCaller = false)
        {
            if (!Enabled) return;
            var suffix = withCaller ? Caller() : "";
            if (text is null) { Sink("[mcdoc-write] " + tag + " <null>" + suffix); return; }
            var clipped = text.Length > 120 ? text.Substring(0, 120) : text;
            Sink("[mcdoc-write] " + tag + " len=" + text.Length + " text=" + clipped.Replace("\r", " ").Replace("\n", " ") + suffix);
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
