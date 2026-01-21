using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

// [2025-12-10 11:19:33] STDERR: ERROR: [youtube] uikN5mzrOrQ: Video unavailable. This video is no longer available because the YouTube account associated with this video has been terminated.
// [2025-12-10 11:48:17] STDERR: ERROR: [youtube] wpfR9qBYwfQ: Sign in to confirm you�fre not a bot. Use --cookies-from-browser or --cookies for the authentication. See  https://github.com/yt-dlp/yt-dlp/wiki/FAQ#how-do-i-pass-cookies-to-yt-dlp  for how to manually pass cookies. Also see  https://github.com/yt-dlp/yt-dlp/wiki/Extractors#exporting-youtube-cookies  for tips on effectively exporting YouTube cookies

namespace YT_DLP_WRAPPER
{
    internal class Program
    {

        private enum LogLevel
        {
            INFO,
            DEBUG,
            WARN,
            ERROR
        }

        static int Main(string[] args)
        {
            return Run();
        }

        private static int Run()
        {
            //ShowWindowsNotification("yt-dlp エラー", "ボット検出が発生しました。");

            Console.OutputEncoding = Encoding.UTF8;

            var assemblyDir = AppContext.BaseDirectory;
            var ytDlpPath = Path.Combine(assemblyDir, "yt-dlp_.exe");
            var cookiePath = Path.Combine(assemblyDir, "cookies.txt");
            var logPath = Path.Combine(assemblyDir, "wrapper.log");
            var logLock = new object();

            // ログを書き込む関数
            void Log(string message, LogLevel level = LogLevel.INFO)
            {
                var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] {message}";
                try
                {
                    File.AppendAllText(logPath, line + Environment.NewLine);
                }
                catch (Exception)
                {
                    // Failed to write log
                }
            }
            

            // コマンドライン引数を取得
            var originalArgs = Environment.GetCommandLineArgs().Skip(1).ToArray();
            Log($"Startup: args={(originalArgs.Length == 0 ? "(none)" : string.Join(" ", originalArgs.Select(QuoteArg)))}");


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
                Log(error_message, LogLevel.ERROR);
                Console.Error.WriteLine(error_message);

                return 1;
            }

            // 引数リストを元の引数から作成
            var fixedArgs = originalArgs.ToList();

            // -f または --format 引数を置換
            var newFormatValue = "(mp4/best)[protocol=https]/ba[protocol=https]/b/b*";
            fixedArgs = ReplaceOrAddArgument(fixedArgs, new[] { "-f", "--format" }, newFormatValue);
            
            Log($"Format argument replaced/added: --format {newFormatValue}");
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

            // 引数を適切にエスケープして構築
            var argumentBuilder = new StringBuilder();
            for (int i = 0; i < fixedArgs.Count; i++)
            {
                if (i > 0)
                {
                    argumentBuilder.Append(" ");
                }
                argumentBuilder.Append(QuoteArg(fixedArgs[i]));
            }
            psi.Arguments = argumentBuilder.ToString();

            Log($"Executable: {ytDlpPath}");
            Log($"Arguments : {psi.Arguments}");

            var process = Process.Start(psi);

            // あるべきプロセスが存在しない場合
            if (process is null)
            {
                Log("Failed to start yt-dlp_.exe", LogLevel.ERROR);
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
                    // "not a bot"が含まれる場合はWindows通知を表示
                    if (e.Data.ToLowerInvariant().Contains("not a bot"))
                    {
                        //ShowWindowsNotification("yt-dlp wrapper", "ボット検出によって動画の読み込みに失敗しました。VPNを無効にするか有効にしてください。");
                        Log($"エラー詳細 : ボット検出によって動画の読み込みに失敗しました。VPNを無効にするか有効にしてください。", LogLevel.WARN);

                    }

                    Console.Error.WriteLine(e.Data);
                    Log($"STDERR: {e.Data}", LogLevel.ERROR);
                   
                }
            };

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();
            Log($"Exit: code={process.ExitCode}");

            return process.ExitCode;
        }

        // 引数をエスケープ処理しながら囲みます。
        // 引数名（-f, --formatなど）の場合は引用符で囲まない
        // 値が長い場合や特殊文字を含む場合は引用符で囲む
        private static string QuoteArg(string arg)
        {
            if (string.IsNullOrEmpty(arg))
            {
                return "\"\"";
            }

            // 引数名（-で始まる）の場合は引用符で囲まない
            if (arg.StartsWith("-"))
            {
                return arg;
            }

            // URLの場合は引用符で囲む
            if (arg.StartsWith("http://") || arg.StartsWith("https://"))
            {
                var escaped = arg.Replace("\"", "\\\"");
                return $"\"{escaped}\"";
            }

            // スペース、引用符、カンマ、または長い値（200文字以上）の場合は引用符で囲む
            if (arg.Any(char.IsWhiteSpace) || arg.Contains('"') || arg.Contains(',') || arg.Length > 200)
            {
                var escaped = arg.Replace("\"", "\\\"");
                return $"\"{escaped}\"";
            }

            return arg;
        }
        

        // 既存の引数を書き換えます。
        // 引数名の配列を指定することで、複数の形式に対応します（例: ["-f", "--format"]）
        // 引数が存在する場合は値を置き換え、存在しない場合は追加します。
        // 置換時は常に --format を使用します。
        // 戻り値: 書き換え後の引数リスト
        private static List<string> ReplaceOrAddArgument(
            List<string> args,
            string[] argumentNames,
            string newValue)
        {
            if (args == null)
            {
                args = new List<string>();
            }

            if (argumentNames == null || argumentNames.Length == 0)
            {
                return args;
            }

            // 置換後の引数名（常に --format を使用、存在しない場合は配列の最後の要素を使用）
            var targetArgName = argumentNames.Contains("--format") ? "--format" : argumentNames[argumentNames.Length - 1];

            var result = new List<string>();
            bool replaced = false;

            for (int i = 0; i < args.Count; i++)
            {
                bool matched = false;

                foreach (var argName in argumentNames)
                {
                    // スペース区切りの形式: -f value または --format value
                    if (args[i] == argName && i + 1 < args.Count)
                    {
                        // 既存の値を置き換え（常に --format を使用）
                        result.Add(targetArgName);
                        result.Add(newValue);
                        i++; // 既存の値をスキップ
                        replaced = true;
                        matched = true;
                        break;
                    }
                    // 等号区切りの形式: -f=value または --format=value
                    else if (args[i].StartsWith(argName + "="))
                    {
                        // 既存の値を置き換え（常に --format を使用）
                        result.Add($"{targetArgName}={newValue}");
                        replaced = true;
                        matched = true;
                        break;
                    }
                }

                if (!matched)
                {
                    result.Add(args[i]);
                }
            }

            // 引数が存在しない場合は追加
            if (!replaced && newValue != null)
            {
                result.Add(targetArgName);
                result.Add(newValue);
            }

            return result;
        }
    }
}
