using System;
using System.Collections.Generic;
using System.Text;
using ConsoleInteractive.Extensions;

namespace ConsoleInteractive;

public class StringData {

    internal StringData() { }

    public StringData(string text, bool appendNewLine = false, Color? backgroundColor = null,
        Color? foregroundColor = null, FormattingType formattingType = FormattingType.None) {
        this.Text = text;
        this.BackgroundColor = backgroundColor;
        this.ForegroundColor = foregroundColor;
        this.FormattingType = formattingType;
        this.AppendNewLine = appendNewLine;
    }

    public string Text { get; }
    public bool AppendNewLine { get; }
    public Color? BackgroundColor { get; }
    public Color? ForegroundColor { get; }
    public FormattingType FormattingType { get; } = FormattingType.None;

    public string Build(bool withNewLines = true) {
        StringBuilder internalStringBuilder = new StringBuilder();

        // Step 1: Process formatting if available
        // Append the formatting first before the color,
        // because the None formatting type sends a reset code
        // to prevent the previous formatting and color from getting carried over.
        internalStringBuilder.Append(FormattingType.BuildFormattingVtCode());
        
        if (FormattingType.HasFlag(FormattingType.Obfuscated)) // Square character for obfuscation.
            internalStringBuilder.Append('\u2588', Text.Length);

        // Step 2: Process color if available
        if (ForegroundColor != null) {
            var color = ForegroundColor.Value;
            internalStringBuilder.Append(color.BuildAsForegroundColorVtCode());
        }

        if (BackgroundColor != null) {
            var color = BackgroundColor.Value;
            internalStringBuilder.Append(color.BuildAsBackgroundColorVtCode());
        }

        // Step 3: Build the string
        if (withNewLines) {
            if (AppendNewLine) {
                internalStringBuilder.Append(Text + Environment.NewLine);
            }
        }
        else {
            internalStringBuilder.Append(Text);
        }

        // Ensure that a formatting reset happens at the end of the string.
        internalStringBuilder.Append("\u001B[0m");

        return internalStringBuilder.ToString();
    }
}