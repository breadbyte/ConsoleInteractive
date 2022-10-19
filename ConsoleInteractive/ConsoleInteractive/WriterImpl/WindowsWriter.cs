using System;
using System.Collections.Generic;
using PInvoke;

namespace ConsoleInteractive.WriterImpl;

public class WindowsWriter : WriterBase {

    private static ConsoleColor __WriterInternalForegroundColor = ConsoleColor.White;
    private static ConsoleColor __WriterInternalBackgroundColor = ConsoleColor.Black;

    // UNSAFE FUNCTION
    // Only used for WindowsAPI color writing system.
    // Should not be used otherwise, it will mess up the console positioning code
    // as it is not tracked in this function.
    private static void __WriteInternalUnsafeNoLine(string text) {

        // Assume the caller will do the housekeeping necessary, i.e.
        // stashing and restoring the buffer state,
        // and restoring the cursor position.

        lock (InternalContext.WriteLock) {
            Console.Write(text);
        }
    }

    public override void Write(StringData data) {

        // Preferably, we wouldn't have this as Windows has deprecated this API in favor of terminal VT sequences.
        // See: https://learn.microsoft.com/en-us/windows/console/classic-vs-vt
        // Due to how the console api works, we need to write it to the console at the same time as parsing the colors and formatting.
        // This function does not adjust the console state, do not use as-is!

        // Write normally to the console.
        // We setup all the color and formatting beforehand, so it applies to the output.
        // Restore the state of the colors afterward.

        // Make sure to pass the string to the regular write function,
        // so the console positioning code still works fine.
        var outA = data.AppendNewLine ? data.Text + Environment.NewLine : data.Text;
        var chainLength = data.Text.Length;

        StashColors();
        __StashBufferState();

        ApplyFormatting(data);

        // Begin writing to the console.
        __WriteInternalUnsafeNoLine(outA);

        __CalculateNextCursorTopPosition(DetermineLineCount(chainLength));

        // The regular write function uses a `WriteLine`, so we don't need to update the position
        // Here, we need to force the cursor to move.
        InternalContext.SetTopCursorPosition(InternalContext.CurrentCursorTopPos);

        RestoreColors();
        __RestoreBufferState();
    }

    public override void WriteStringDataChain(List<StringData> data) {
        var chainLength = 0;

        foreach (var strData in data) {
            // Determine the length of the entire chain.
            chainLength += strData.Text.Length;
        }

        StashColors();
        __StashBufferState();

        foreach (var strData in data) {
            // Apply the formatting set in the string data.
            ApplyFormatting(strData);

            // Begin writing to the console.
            __WriteInternalUnsafeNoLine(strData.Text);
        }

        __CalculateNextCursorTopPosition(DetermineLineCount(chainLength));

        // The regular write function uses a `WriteLine`, so we don't need to update the position
        // Here, we need to force the cursor to move.
        InternalContext.SetTopCursorPosition(InternalContext.CurrentCursorTopPos);

        RestoreColors();
        __RestoreBufferState();
    }

    #region Helper functions for the Windows API

    private static void ApplyFormatting(StringData strData) {

        // Applies the formatting assigned to a StringData.
        // Does not clear the formatting assigned afterwards.
        // The caller is responsible for restoring the console state afterwards.

        Kernel32.CONSOLE_SCREEN_BUFFER_INFO consoleAttribs = new();
        IntPtr stdoutHandle = IntPtr.Zero;

        // Step 1: Process color if available
        if (strData.ForegroundColor != null) {
            var color = strData.ForegroundColor.Value;
            Console.ForegroundColor = Color.GetClosestDefaultColor(color.R, color.G, color.B);
        }

        if (strData.BackgroundColor != null) {
            var color = strData.BackgroundColor.Value;
            Console.BackgroundColor = Color.GetClosestDefaultColor(color.R, color.G, color.B);
        }

        // All formatting options except Underline is not supported in Windows API mode.
        if (strData.Formatting.HasFlag(Formatting.Underline)) {
            stdoutHandle = Kernel32.GetStdHandle(Kernel32.StdHandle.STD_OUTPUT_HANDLE);

            // Get the current console attributes, and set the console text attribute to add an underscore to the text.
            // TODO: Doesn't seem to be working...?
            Kernel32.GetConsoleScreenBufferInfo(stdoutHandle, out consoleAttribs);
            Kernel32.SetConsoleTextAttribute(stdoutHandle,
                consoleAttribs.wAttributes | Kernel32.CharacterAttributesFlags.COMMON_LVB_UNDERSCORE);
        }
    }

    private static void StashColors() {
        // Store the current foreground and background color state
        __WriterInternalForegroundColor = Console.ForegroundColor;
        __WriterInternalBackgroundColor = Console.BackgroundColor;
    }

    private static void RestoreColors() {
        // Restore the foreground and background color state.
        Console.ForegroundColor = __WriterInternalForegroundColor;
        Console.BackgroundColor = __WriterInternalBackgroundColor;
    }

    #endregion
}