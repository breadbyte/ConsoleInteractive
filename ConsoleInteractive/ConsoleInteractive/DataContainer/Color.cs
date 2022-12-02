using System;
using System.Collections.Generic;

namespace ConsoleInteractive; 

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

    public string AsForegroundColorVt() {
        return $"\u001B[38;2;{R};{G};{B}m";
    }

    public string AsBackgroundColorVt() {
        return $"\u001B[48;2;{R};{G};{B}m";
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