using System;
using System.Collections.Generic;
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
            var cookiePath = Path.Combine(assemblyDir, "cookies.txt");
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


            if (originalArgs.Length == 0) {
                Console.WriteLine("このまま実行はできません。install.bat を実行してください。");
                Console.WriteLine("詳細な情報は: https://ajisaiflow.booth.pm/items/7673438");
                Console.WriteLine("\nエンターキーで終了します...");
                Console.ReadLine();
            }

            // オリジナルの yt-dlp がない場合はエラーにします。
            if (!File.Exists(ytDlpPath))
            {
                var error_message = $"yt-dlp_.exe (Original yt-dlp) not found: {ytDlpPath}";
                Log(error_message);
                Console.Error.WriteLine(error_message);

                return 1;
            }

            // 引数リストを定義
            var fixedArgs = new List<string>();

            // Cookieファイルがある場合は引数に追加する。
            //if (File.Exists(cookiePath))
            //{
            //    fixedArgs.Add("--cookies");
            //    fixedArgs.Add("cookies.txt");
            //    Log($"Cookies file found: {cookiePath}");
            //}

            // 置換後の引数
            fixedArgs.AddRange(new[]
            {
                "--no-cache-dir",
                "--rm-cache-dir",
                "-f",
                "(mp4/best)[protocol=https]/ba[protocol=https]/b/b*",
                "--get-url"
            });

            Log($"Fixed args: {string.Join(" ", fixedArgs.Select(QuoteArg))}");

            // プロセス開始情報を定義
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
                ? fixedArgs.ToArray()
                : fixedArgs.Concat(new[] { lastInputArg }).ToArray();
            psi.Arguments = string.Join(" ", combinedArgs.Select(QuoteArg));

            Log($"Executable: {ytDlpPath}");
            Log($"Arguments: {psi.Arguments}");

            var process = Process.Start(psi);

            // あるべきプロセスが存在しない場合
            if (process is null)
            {
                Log("Failed to start yt-dlp_.exe");
                Console.Error.WriteLine($"Failed to start yt-dlp_.exe ({ytDlpPath})");

                return 1;
            }

            // yt-dlp_.exe からの結果受け取りコールバック
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

        // 引数をエスケープ処理しながら囲みます。
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
        
        // 最後の引数を取得します。
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
