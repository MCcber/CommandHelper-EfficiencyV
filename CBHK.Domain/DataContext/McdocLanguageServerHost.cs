using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

namespace CBHK.Domain.DataContext
{
    public enum McdocLanguageServerState
    {
        AlreadyRunning,
        Started,
        Unavailable,
        Timeout
    }

    public readonly record struct McdocLanguageServerStatus(McdocLanguageServerState State, string Reason)
    {
        public bool IsRunning => State is McdocLanguageServerState.AlreadyRunning or McdocLanguageServerState.Started;
    }

    /// <summary>
    /// 语言服务器的唯一启动入口：管道已经在听就不再起进程；没起就拉起应用目录里的
    /// MinecraftLanguageServer.exe 并等它挂上管道。
    /// 只回收本进程启动的实例（用户手工起的永不杀），任何失败都返回状态、不抛异常。
    /// </summary>
    public sealed class McdocLanguageServerHost : IDisposable
    {
        public const string PipeName = "MCDocumentLanguageServerPipe";
        private const string ExecutableName = "MinecraftLanguageServer.exe";

        private static readonly Lazy<McdocLanguageServerHost> SharedInstance = new(() => new McdocLanguageServerHost());

        private readonly object gate = new();
        private Process? startedProcess;
        private bool exitHookRegistered;

        public static McdocLanguageServerHost Shared => SharedInstance.Value;

        /// <summary>本进程启动的服务器进程号；没有起过（或已退出）为 null。</summary>
        public int? ProcessId { get; private set; }

        /// <summary>实际使用的 exe 路径；没找到为 null。</summary>
        public string? ExecutablePath { get; private set; }

        public McdocLanguageServerStatus EnsureRunning(string? baseDirectory = null, TimeSpan? readyTimeout = null)
        {
            lock (gate)
            {
                if (IsPipeListening())
                {
                    return new McdocLanguageServerStatus(McdocLanguageServerState.AlreadyRunning, "管道已在监听");
                }

                if (startedProcess is { HasExited: false } alive)
                {
                    // 进程活着但管道还没就绪：再等一轮，别重复起进程
                    return WaitForPipe(alive, readyTimeout ?? TimeSpan.FromSeconds(10));
                }

                string? executable = ResolveExecutable(baseDirectory);
                ExecutablePath = executable;
                if (executable is null)
                {
                    return new McdocLanguageServerStatus(McdocLanguageServerState.Unavailable,
                        "找不到语言服务器：" + (string.IsNullOrEmpty(baseDirectory) ? AppDomain.CurrentDomain.BaseDirectory : baseDirectory));
                }

                Process? process;
                try
                {
                    process = Process.Start(new ProcessStartInfo(executable)
                    {
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        // 传宿主 PID：宿主被强杀时服务器跟着退出，不残留占管道
                        Arguments = "--parent=" + Environment.ProcessId,
                        WorkingDirectory = Path.GetDirectoryName(executable) ?? AppDomain.CurrentDomain.BaseDirectory
                    });
                }
                catch (Exception error)
                {
                    return new McdocLanguageServerStatus(McdocLanguageServerState.Unavailable, "启动语言服务器失败：" + error.Message);
                }

                if (process is null)
                {
                    return new McdocLanguageServerStatus(McdocLanguageServerState.Unavailable, "启动语言服务器失败：进程未创建");
                }

                startedProcess = process;
                ProcessId = process.Id;
                RegisterExitHook();
                return WaitForPipe(process, readyTimeout ?? TimeSpan.FromSeconds(10));
            }
        }

        public bool IsPipeListening()
        {
            try
            {
                return Directory.GetFiles(@"\\.\pipe\")
                    .Any(name => string.Equals(Path.GetFileName(name), PipeName, StringComparison.OrdinalIgnoreCase));
            }
            catch (Exception)
            {
                return false;
            }
        }

        private McdocLanguageServerStatus WaitForPipe(Process process, TimeSpan timeout)
        {
            var stopwatch = Stopwatch.StartNew();
            while (stopwatch.Elapsed < timeout)
            {
                if (IsPipeListening())
                {
                    return new McdocLanguageServerStatus(McdocLanguageServerState.Started, "已启动语言服务器");
                }

                if (process.HasExited)
                {
                    startedProcess = null;
                    ProcessId = null;
                    return new McdocLanguageServerStatus(McdocLanguageServerState.Unavailable,
                        "语言服务器已退出（exit code " + process.ExitCode + "）");
                }

                Thread.Sleep(100);
            }

            KillStartedProcess();
            return new McdocLanguageServerStatus(McdocLanguageServerState.Timeout,
                "语言服务器 " + (int)timeout.TotalSeconds + " 秒内没有挂上管道");
        }

        private static string? ResolveExecutable(string? baseDirectory)
        {
            string root = string.IsNullOrEmpty(baseDirectory) ? AppDomain.CurrentDomain.BaseDirectory : baseDirectory;
            string[] candidates =
            [
                Path.Combine(root, "LanguageServer", ExecutableName),
                Path.Combine(root, ExecutableName)
            ];
            return candidates.FirstOrDefault(File.Exists);
        }

        private void RegisterExitHook()
        {
            if (exitHookRegistered)
            {
                return;
            }

            exitHookRegistered = true;
            AppDomain.CurrentDomain.ProcessExit += (_, _) => KillStartedProcess();
            AppDomain.CurrentDomain.DomainUnload += (_, _) => KillStartedProcess();
        }

        private void KillStartedProcess()
        {
            lock (gate)
            {
                Process? process = startedProcess;
                startedProcess = null;
                ProcessId = null;
                if (process is null)
                {
                    return;
                }

                try
                {
                    if (!process.HasExited)
                    {
                        process.Kill(entireProcessTree: true);
                        process.WaitForExit(3000);
                    }
                }
                catch (Exception)
                {
                    // 退出清理失败不影响调用方
                }
                finally
                {
                    process.Dispose();
                }
            }
        }

        public void Dispose() => KillStartedProcess();
    }
}
