using System;
using System.Collections.Generic;
using System.Text;

namespace ConsoleInteractive;

public class StringData {

    internal StringData() { }

    public StringData(string text, bool appendNewLine = false, Color? backgroundColor = null,
        Color? foregroundColor = null, Formatting formatting = Formatting.None) {
        this.Text = text;
        this.BackgroundColor = backgroundColor;
        this.ForegroundColor = foregroundColor;
        this.Formatting = formatting;
        this.AppendNewLine = appendNewLine;
    }

    public string Text { get; }
    public bool AppendNewLine { get; }
    public Color? BackgroundColor { get; }
    public Color? ForegroundColor { get; }
    public Formatting Formatting { get; } = Formatting.None;

    public string Build() {
        StringBuilder internalStringBuilder = new StringBuilder();

        // Step 1: Process formatting if available
        // Append the formatting first before the color,
        // because the None formatting type sends a reset code
        // to prevent the previous formatting and color from getting carried over.
        if (Formatting.HasFlag(Formatting.None))
            internalStringBuilder.Append($"\u001B[0m");
        if (Formatting.HasFlag(Formatting.Obfuscated)) // Square character for obfuscation.
            internalStringBuilder.Append('\u2588', Text.Length);
        if (Formatting.HasFlag(Formatting.Bold))
            internalStringBuilder.Append($"\u001B[1m");
        if (Formatting.HasFlag(Formatting.Strikethrough))
            internalStringBuilder.Append($"\u001B[9m");
        if (Formatting.HasFlag(Formatting.Underline))
            internalStringBuilder.Append($"\u001B[4m");
        if (Formatting.HasFlag(Formatting.Italic))
            internalStringBuilder.Append($"\u001B[3m");

        // Step 2: Process color if available
        if (ForegroundColor != null) {
            var color = ForegroundColor.Value;
            internalStringBuilder.Append($"\u001B[38;2;{color.R};{color.G};{color.B}m");
        }

        if (BackgroundColor != null) {
            var color = BackgroundColor.Value;
            internalStringBuilder.Append($"\u001B[48;2;{color.R};{color.G};{color.B}m");
        }

        // Step 3: Build the string
        internalStringBuilder.Append(AppendNewLine ? Text + Environment.NewLine : Text);

        // Ensure that a formatting reset happens at the end of the string.
        internalStringBuilder.Append("\u001B[0m");

        return internalStringBuilder.ToString();
    }

    public List<StringData> Expand() {
        throw new NotImplementedException();
    }
}