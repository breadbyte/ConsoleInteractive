# ConsoleInteractive
A C# library that allows you to write and read from the console simultaneously

## Usage
See [ConsoleInteractiveDemo](https://github.com/breadbyte/ConsoleInteractive/blob/main/ConsoleInteractive/ConsoleInteractiveDemo) for a sample, but the gist of it is
```cs
// Create a CancellationToken so we can cancel the reader later.
CancellationTokenSource cts = new CancellationTokenSource();

// Create a new Thread that will print for us.
Thread PrintingThread = new Thread(new ThreadStart(() => {
    ConsoleWriter.WriteLine("Hello World!");
    Thread.Sleep(5000);
    ConsoleWriter.WriteLine("Hello World after 5 seconds!");
}));

// Create a new Reader Thread. This thread will start listening for console input
ConsoleReader.BeginReadThread(cts.Token);

// Handle incoming messages from the user (Enter key pressed)
ConsoleReader.MessageReceived += (sender, s) => {
    // We got a cancellation command! Let's cancel the CancellationTokenSource.
    if (s.Equals("cancel"))
        cts.Cancel();
};

// Start the printing thread.
PrintingThread.Start();
```
## Note
It is important that you use the `ConsoleInteractive.ConsoleWriter` and `ConsoleInteractive.ConsoleReader` classes. 

Mixing and matching with `System.Console` is not supported nor recommended.

## Demo
Check out an asciicast demo here!
[![asciicast](https://asciinema.org/a/T1G1OWROPIpWB5rViZ0UQiaOJ.png)](https://asciinema.org/a/T1G1OWROPIpWB5rViZ0UQiaOJ)
