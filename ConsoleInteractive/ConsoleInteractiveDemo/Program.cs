using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using ConsoleInteractive;
using ConsoleInteractive.Extension;

namespace ConsoleInteractiveDemo {
    class Program {
        static void Main(string[] args) {

            //var splitted = "[38;2;12;12;12mHello World!";
            var str = @"[0m[T3] ---[0m[0m[38;2;12;12;12m[T3] Black Text[0m[0m[T3] 1 ---[0m[0m[38;2;0;55;218m[T3] Dark Blue Text[0m[0m[T3] 2 ---[0m";

            FormattedStringBuilder f = new();
            //Debugger.Break();
            FormattedStringBuilder sb = new();
            FormattedStringBuilder sb2 = new();
            ConsoleWriter.Init();

            sb.AppendLine("[T3] ---")
              .AppendLine("[T3] Black Text", FormattedStringBuilder.Color.Black)
              .AppendLine("[T3] 1 ---")
              .AppendLine("[T3] Dark Blue Text", FormattedStringBuilder.Color.DarkBlue)
              .AppendLine("[T3] 2 ---")
              .AppendLine("[T3] Dark Green Text", FormattedStringBuilder.Color.DarkGreen)
              .AppendLine("[T3] 3 ---")
              .AppendLine("[T3] Dark Aqua Text", FormattedStringBuilder.Color.DarkCyan)
              .AppendLine("[T3] 4 ---")
              .AppendLine("[T3] Dark Red Text", FormattedStringBuilder.Color.DarkRed)
              .AppendLine("[T3] 5 ---")
              .AppendLine("[T3] Dark Purple Text", FormattedStringBuilder.Color.DarkMagenta)
              .AppendLine("[T3] 6 ---")
              .AppendLine("[T3] Gold Text", FormattedStringBuilder.Color.Yellow)
              .AppendLine("[T3] 7 ---")
              .AppendLine("[T3] Gray Text", FormattedStringBuilder.Color.Gray)
              .AppendLine("[T3] 8 ---")
              .AppendLine("[T3] Dark Gray Text", FormattedStringBuilder.Color.DarkGray)
              .AppendLine("[T3] 9 ---")
              .AppendLine("[T3] Blue Text", FormattedStringBuilder.Color.Blue)
              .AppendLine("[T3] a ---")
              .AppendLine("[T3] Green Text", FormattedStringBuilder.Color.Green)
              .AppendLine("[T3] b ---")
              .AppendLine("[T3] Aqua Text", FormattedStringBuilder.Color.Cyan)
              .AppendLine("[T3] c ---")
              .AppendLine("[T3] Red Text", FormattedStringBuilder.Color.Red)
              .AppendLine("[T3] d ---")
              .AppendLine("[T3] Light Purple Text", FormattedStringBuilder.Color.Magenta)
              .AppendLine("[T3] e ---")
              .AppendLine("[T3] Yellow Text", FormattedStringBuilder.Color.Yellow)
              .AppendLine("[T3] f ---")
              .AppendLine("[T3] White Text", FormattedStringBuilder.Color.White)
              .AppendLine("[T3] k ---")
              .Append("[T3] ")
              .Append("Obfuscated Text", formatting: FormattedStringBuilder.Formatting.Obfuscated)
              .AppendLine(" (Obfuscated)")
              .AppendLine("[T3] l ---")
              .Append("[T3] ")
              .AppendLine("Bold Text", formatting: FormattedStringBuilder.Formatting.Bold)
              .AppendLine("[T3] m ---")
              .Append("[T3] ")
              .AppendLine("Strikethrough Text", formatting: FormattedStringBuilder.Formatting.Strikethrough)
              .AppendLine("[T3] n ---")
              .Append("[T3] ")
              .AppendLine("Underline Text", formatting: FormattedStringBuilder.Formatting.Underline)
              .AppendLine("[T3] o ---")
              .Append("[T3] ")
              .AppendLine("Italic Text", formatting: FormattedStringBuilder.Formatting.Italic);
              //.AppendLine("This text \n has an n newline and \r\n an rn newline!");
            
            sb2.AppendMarkup("text without match aaaaaaa §aText §n§cwith §m§bMixed §1C§2o§3l§r§4o§5r§6s§a!");
            var expanded = sb.Expand();
            var built = sb2.Flatten();
            var splits = built.SplitNewLines();
            foreach (var split in splits) {
                f.AppendTerminalCodeMarkup(split);
            }
            //Debugger.Break();
            ConsoleWriter.ForceUseWindowsAPI();
            //ConsoleWriter.WriteLine(sb);
            //ConsoleWriter.WriteLine(sb);
            //Debugger.Break();
            CancellationTokenSource cts = new CancellationTokenSource();
            
            ConsoleWriter.WriteLine("Waiting for debugger...");
            //while (!Debugger.IsAttached) {
            //    Thread.Sleep(100);
            //}
            //Debugger.Break();

            ConsoleWriter.WriteLine("type cancel to exit the application.");
            var t1 = new Thread(new ThreadStart(() => {
                ConsoleWriter.WriteLine("[T1] Hello World!");
                Thread.Sleep(5000);
                ConsoleWriter.WriteLine("[T1] Hello World after 5 seconds!");
                Thread.Sleep(3000);
                ConsoleWriter.WriteLine("[T1] Hello World after 8 seconds!");
                Thread.Sleep(3000);
                ConsoleWriter.WriteLine("[T1] Hello World after 11 seconds!");
                Thread.Sleep(5000);
                ConsoleWriter.WriteLine("[T1] Hello World after 16 seconds!");
            })) {IsBackground = true};
            var t2 = new Thread(new ThreadStart(() => {
                ConsoleWriter.WriteLine("[T2] Hello from Thread 2!");
                Thread.Sleep(3000);
                ConsoleWriter.WriteLine("[T2] Hello from Thread 2 after 3 seconds!");
                Thread.Sleep(1000);
                ConsoleWriter.WriteLine("[T2] Hello from Thread 2 after 4 seconds!");
                Thread.Sleep(2000);
                ConsoleWriter.WriteLine("[T2] Hello from Thread 2 after 6 seconds!");
                Thread.Sleep(4000);
                ConsoleWriter.WriteLine("[T2] Hello from Thread 2 after 10 seconds!");
                Thread.Sleep(1000);
                ConsoleWriter.WriteLine("[T2] 1 Filler...");
                ConsoleWriter.WriteLine("[T2] 2 Filler...");
                ConsoleWriter.WriteLine("[T2] 3 Filler...");
                ConsoleWriter.WriteLine("[T2] 4 Filler...");
                ConsoleWriter.WriteLine("[T2] 5 Filler...");
                ConsoleWriter.WriteLine("[T2] 6 Filler...");
                ConsoleWriter.WriteLine("[T2] 7 Filler...");
            })) {IsBackground = true};

            var tF = new Thread(new ThreadStart(() => {
                
                ConsoleWriter.WriteLine(sb);
                //ConsoleWriter.Write("TEST --- \r\n");
            })) {IsBackground = true};
            
            //t1.Start();
            //t2.Start();
            tF.Start();

            ConsoleReader.BeginReadThread(cts);
            ConsoleReader.MessageReceived += (sender, s) => {
                if (s.Equals("cancel"))
                    ConsoleReader.StopReadThread();
                else {
                    ConsoleWriter.WriteLine(s);
                }
            };
        }
    }
}