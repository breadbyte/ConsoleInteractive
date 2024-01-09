using System;

namespace ConsoleInteractive.Interface.Abstract; 

public class InputPrefix {
    
    //
    // PREAMBLE
    // 
    // This class represents the prefix of the input buffer.
    // The prefix is the string that is displayed before the input buffer.
    // 
    // This is the equivalent of the PS1 variable in bash.
    
    /// <summary>
    /// The prefix string.
    /// </summary>
    static string Prefix = ">";

    /// <summary>
    /// The prefix string, reversed.
    /// </summary>
    static string PrefixOpposite = "<";

    /// <summary>
    /// The amount of spaces to add after the prefix.
    /// </summary>
    static int PrefixSpaces = 1;
    
    /// <summary>
    /// The total length of the prefix, including the spaces.
    /// </summary>
    static int PrefixTotalLength = Math.Max(Prefix.Length, PrefixOpposite.Length) + PrefixSpaces;
}