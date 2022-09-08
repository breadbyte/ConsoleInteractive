using System;
using System.Collections.Generic;
using System.Linq;

namespace ConsoleInteractive; 

public class FormattedStringBuilder {
    private List<StringData> strings = new();

    private string AppendNewLine(string text) {
        return text + Environment.NewLine;
    }

    public FormattedStringBuilder Append(string text, Color? foregroundColor = null, Color? backgroundColor = null, Formatting formatting = Formatting.None) {
        strings.Add(new StringData(text, backgroundColor, foregroundColor, formatting));
        return this;
    }
    
    public FormattedStringBuilder AppendLine(string text, Color? foregroundColor = null, Color? backgroundColor = null, Formatting formatting = Formatting.None) {
        strings.Add(new StringData(AppendNewLine(text), backgroundColor, foregroundColor, formatting));
        return this;
    }
    public FormattedStringBuilder NewLine() {
        strings.Add(new StringData(Environment.NewLine));
        return this;
    }

    private System.Text.StringBuilder internalStringBuilder = new();
    
    public string Build() {
        foreach (StringData strData in strings) {
            
            // Step 1: Process color if available
            if (strData.ForegroundColor != null) {
                var color = strData.ForegroundColor.Value;
                internalStringBuilder.Append($"\u001B[38;2;{color.R};{color.G};{color.B}m");
            }

            if (strData.BackgroundColor != null) {
                var color = strData.BackgroundColor.Value;
                internalStringBuilder.Append($"\u001B[48;2;{color.R};{color.G};{color.B}m");
            }

            // Step 2: Process formatting if available
            if (strData.Formatting.HasValue) {
                switch (strData.Formatting) {
                    case Formatting.None:
                    case null:
                        internalStringBuilder.Append($"\u001B[0m{strData.Text}");
                        break;
                    case Formatting.Obfuscated: // Square character for obfuscation.
                        internalStringBuilder.Append('\u2588', strData.Text.Length);
                        continue;
                    case Formatting.Bold:
                        internalStringBuilder.Append($"\u001B[1m");
                        break;
                    case Formatting.Strikethrough:
                        internalStringBuilder.Append($"\u001B[9m");
                        break;
                    case Formatting.Underline:
                        internalStringBuilder.Append($"\u001B[4m");
                        break;
                    case Formatting.Italic:
                        internalStringBuilder.Append($"\u001B[3m");
                        break;
                    default:
                        throw new ArgumentOutOfRangeException("Invalid formatting type");
                }
            }
        }
        var retval = internalStringBuilder + Environment.NewLine;
        internalStringBuilder.Clear();
        
        return retval;
    }

    public enum Formatting {
        None,
        Obfuscated,
        Bold,
        Strikethrough,
        Underline,
        Italic,
    }

    private struct StringData {

        public StringData(string text, Color? backgroundColor = null, Color? foregroundColor = null, Formatting? formatting = FormattedStringBuilder.Formatting.None) {
            this.Text = text;
            this.BackgroundColor = backgroundColor;
            this.ForegroundColor = foregroundColor;
        }
        
        public string Text { get; }
        public Color? BackgroundColor { get; }
        public Color? ForegroundColor { get; }
        public Formatting? Formatting { get; } = FormattedStringBuilder.Formatting.None;
    };

    public struct Color {
        public Color(byte r = 255, byte g = 255, byte b = 255) {
            this.R = r;
            this.G = g;
            this.B = b;
        }

        public byte R;
        public byte G;
        public byte B;

        public static Color Black		=> new Color(12,12,12);
        public static Color White		=> new Color(242,242,242);
        public static Color Gray		=> new Color(204,204,204);
        public static Color DarkRed		=> new Color(197,15,31);
        public static Color DarkGreen	=> new Color(19,161,14);
        public static Color DarkYellow	=> new Color(193,156,0);
        public static Color DarkBlue	=> new Color(0,55,218);
        public static Color DarkMagenta	=> new Color(136,23,152);
        public static Color DarkCyan	=> new Color(58,150,221);
        public static Color Red			=> new Color(231,72,86);
        public static Color Green		=> new Color(22,198,12);
        public static Color Yellow		=> new Color(249,241,165);
        public static Color Blue		=> new Color(59,120,255);
        public static Color Magenta		=> new Color(180,0,158);
        public static Color Cyan		=> new Color(97,214,214);
        public static Color DarkGray	=> new Color(118,118,118);
    }

    // Color values taken from https://en.wikipedia.org/wiki/ANSI_escape_code#3-bit_and_4-bit using the Windows 10 Console color set
    public static Dictionary<ConsoleColor, ((byte r, byte g, byte b) rgb, string code)> DefaultColors = new() {
        {ConsoleColor.Black,        ((12,12,12), "\u001B[30m")},    // §0, Black
        {ConsoleColor.White,        ((242,242,242), "\u001B[97m")}, // §f, White (Bright White)
        {ConsoleColor.Gray,         ((204,204,204), "\u001B[90m")}, // §7, Gray  (White)
        {ConsoleColor.DarkRed,      ((197,15,31), "\u001B[31m")},   // §4, Dark Red
        {ConsoleColor.DarkGreen,    ((19,161,14), "\u001B[32m")},   // §2, Dark Green
        {ConsoleColor.DarkYellow,   ((193,156,0), "\u001B[33m")},   // §6, Gold
        {ConsoleColor.DarkBlue,     ((0,55,218), "\u001B[34m")},    // §1, Dark Blue
        {ConsoleColor.DarkMagenta,  ((136,23,152), "\u001B[35m")},  // §5, Dark Purple
        {ConsoleColor.DarkCyan,     ((58,150,221), "\u001B[36m")},  // §3, Dark Aqua
        {ConsoleColor.Red,          ((231,72,86), "\u001B[91m")},   // §c, Red
        {ConsoleColor.Green,        ((22,198,12), "\u001B[92m")},   // §a, Green
        {ConsoleColor.Yellow,       ((249,241,165), "\u001B[93m")}, // §e, Yellow
        {ConsoleColor.Blue,         ((59,120,255), "\u001B[94m")},  // §9, Blue
        {ConsoleColor.Magenta,      ((180,0,158), "\u001B[95m")},   // §d, Light Purple
        {ConsoleColor.Cyan,         ((97,214,214), "\u001B[96m")},  // §b, Aqua
        {ConsoleColor.DarkGray,     ((118,118,118), "\u001B[37m")}  // §8, Dark Gray (Bright Black)
    };
    
    public static Dictionary<Formatting, string> FormattingCodes = new() {
        { Formatting.Obfuscated , "\u2588"}, // Replace the affected character(s) with this code.
        { Formatting.Bold , "\u001B[1m"},           // §l
        { Formatting.Italic , "\u001B[3m"},         // §o
        { Formatting.Strikethrough, "\u001B[9m"},   // §m
        { Formatting.Underline, "\u001B[4m"},       // §n
        { Formatting.None, "\u001B[0m"}             // §r
    };

    public static ConsoleColor GetClosestDefaultColor(byte r, byte g, byte b) {
        var colors = (r, g, b);
        
        ConsoleColor tempColor = ConsoleColor.White;
        double tempDistance = double.MaxValue;

        foreach (var defaultColor in DefaultColors) {
            double rDistance = colors.r - defaultColor.Value.rgb.r;
            double gDistance = colors.g - defaultColor.Value.rgb.g;
            double bDistance = colors.b - defaultColor.Value.rgb.b;

            double totalDistance =
                Math.Sqrt((rDistance * rDistance) + (gDistance * gDistance) + (bDistance * bDistance));

            if (totalDistance < tempDistance) {
                tempColor = defaultColor.Key;
                tempDistance = totalDistance;
            }
        }

        return tempColor;
    }
}