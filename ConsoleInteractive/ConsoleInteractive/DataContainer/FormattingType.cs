using System;

namespace ConsoleInteractive; 

[Flags]
public enum FormattingType {
    None            =      0, // \u001B[0m -- None formatting should be treated the same as Reset.
    Obfuscated      = 1 << 0, // Replace all affected character(s) with \u2588
    Bold            = 1 << 1, // \u001B[1m
    Strikethrough   = 1 << 2, // \u001B[9m
    Underline       = 1 << 3, // \u001B[4m
    Italic          = 1 << 4  // \u001B[3m
}