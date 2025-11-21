using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace YT_DLP_WRAPPER
{
    internal class Program
    {

        static int Main(string[] args)
        {
            return Run();
        }

        private static int Run()
        {
            Console.OutputEncoding = Encoding.UTF8;
            //Console.ErrorEncoding = Encoding.UTF8;

            var assemblyDir = AppContext.BaseDirectory;
            var ytDlpPath = Path.Combine(assemblyDir, "yt-dlp_.exe");
            //var cookiePath = Path.Combine(assemblyDir, "cookies.txt");
            var logPath = Path.Combine(assemblyDir, "wrapper.log");
            var logLock = new object();

            void Log(string message)
            {
                var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}";
                try
                {
                    File.AppendAllText(logPath, line + Environment.NewLine);
                }
                catch (Exception)
                {
                    // Failed to write log
                }
            }

            var originalArgs = Environment.GetCommandLineArgs().Skip(1).ToArray();
            Log($"Startup: args={(originalArgs.Length == 0 ? "(none)" : string.Join(" ", originalArgs.Select(QuoteArg)))}");

            var lastInputArg = GetLastArgument(originalArgs);
            Log($"Last input arg: {(lastInputArg == null ? "(none)" : QuoteArg(lastInputArg))}");

            var fixedArgs = new[]
            {
                "--no-cache-dir",
                "--rm-cache-dir",
                "--format",
                "(mp4/best)[protocol=https]/(ba)[protocol=https]",
                "--get-url"

            };
            Log($"Fixed args: {string.Join(" ", fixedArgs.Select(QuoteArg))}");

            if (!File.Exists(ytDlpPath))
            {
                Log($"yt-dlp.exe not found: {ytDlpPath}");
                return 1;
            }

            //if (!File.Exists(cookiePath))
            //{
            //    Log($"Cookie file not found: {cookiePath}");
            //    return 1;
            //}

            var psi = new ProcessStartInfo
            {
                FileName = ytDlpPath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            var combinedArgs = lastInputArg == null
                ? fixedArgs
                : fixedArgs.Concat(new[] { lastInputArg }).ToArray();
            psi.Arguments = string.Join(" ", combinedArgs.Select(QuoteArg));

            Log($"Executable: {ytDlpPath}");
            Log($"Arguments: {psi.Arguments}");

            var process = Process.Start(psi);
            if (process is null)
            {
                Log("Failed to start yt-dlp.");
                return 1;
            }

            process.OutputDataReceived += (_, e) =>
            {
                if (e.Data != null)
                {
                    Console.Out.WriteLine(e.Data);
                    Log($"STDOUT: {e.Data}");
                }
            };
            process.ErrorDataReceived += (_, e) =>
            {
                if (e.Data != null)
                {
                    Console.Error.WriteLine(e.Data);
                    Log($"STDERR: {e.Data}");
                }
            };

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();
            Log($"Exit: code={process.ExitCode}");

            return process.ExitCode;
        }

        private static string QuoteArg(string arg)
        {
            if (string.IsNullOrEmpty(arg))
            {
                return "\"\"";
            }

            if (!arg.Any(char.IsWhiteSpace) && !arg.Contains('"'))
            {
                return arg;
            }

            var escaped = arg.Replace("\"", "\\\"");
            return $"\"{escaped}\"";
        }

        private static string GetLastArgument(string[] args)
        {
            if (args == null || args.Length == 0)
            {
                return null;
            }

            return args[args.Length - 1];
        }
    }
}
