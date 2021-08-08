using System;
using System.Threading;
using ConsoleInteractive;

namespace ConsoleInteractiveDemo {
    class Program {
        static void Main(string[] args) {
            CancellationTokenSource cts = new CancellationTokenSource();
            var t1 = new Thread(new ThreadStart(() => {
                ConsoleWriter.WriteLine("[T1] Hello World!");
                Thread.Sleep(5000);
                ConsoleWriter.WriteLine("[T1] Hello World after 5 seconds!");
                Thread.Sleep(3000);
                ConsoleWriter.WriteLine("[T1] Hello World after 8 seconds!");
                Thread.Sleep(3000);
                ConsoleWriter.WriteLine("[T1] Hello World after 11 seconds!");
            }));
            var t2 = new Thread(new ThreadStart(() => {
                ConsoleWriter.WriteLine("[T2] Hello from Thread 2!");
                Thread.Sleep(3000);
                ConsoleWriter.WriteLine("[T2] Hello from Thread 2 after 3 seconds!");
                Thread.Sleep(1000);
                ConsoleWriter.WriteLine("[T2] Hello from Thread 2 after 4 seconds!");
                Thread.Sleep(2000);
                ConsoleWriter.WriteLine("[T2] Hello from Thread 2 after 6 seconds!");
            }));
            ConsoleReader.BeginReadThread(cts.Token);
            
            ConsoleReader.MessageReceived += (sender, s) => {
                if (s.Equals("cancel"))
                    cts.Cancel();
            };

            t1.Start();
            t2.Start();
        }
    }
}