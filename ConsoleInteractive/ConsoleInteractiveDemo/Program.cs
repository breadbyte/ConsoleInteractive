using System;
using System.Diagnostics;
using System.Threading;
using ConsoleInteractive;

namespace ConsoleInteractiveDemo {
    class Program {
        static void Main(string[] args) {
            CancellationTokenSource cts = new CancellationTokenSource();
            ConsoleWriter.Init();
            
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
                ConsoleInteractive.FormattedStringBuilder sb = new ConsoleInteractive.FormattedStringBuilder();
                sb.AppendLine("[T3] ---")
                  .AppendLine("[T3] §0Black Text", FormattedStringBuilder.Color.Black)
                  .AppendLine("[T3] 1 ---")
                  .AppendLine("[T3] §1Dark Blue Text", FormattedStringBuilder.Color.DarkBlue)
                  .AppendLine("[T3] 2 ---");
                var built = sb.Build();
                Debug.WriteLine(built);
                ConsoleWriter.WriteLine(built);
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