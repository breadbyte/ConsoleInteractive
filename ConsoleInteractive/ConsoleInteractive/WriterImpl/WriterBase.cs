using System;
using System.Collections.Generic;
using System.Threading;
using ConsoleInteractive.Extensions;

namespace ConsoleInteractive.WriterImpl;

/// <summary>
/// This abstract class is the base class for all writer implementations.
/// </summary>
public abstract class WriterBase {

    #region Public write functions

    // Directly passes the string to the WriteInternal function.
    // Skips all user input checks.
    // ONLY USE IF you are absolutely certain that your string does not have any data
    // that the checks will clean up, i.e. newlines!
    public void WriteUnsafe(string data) {
        InternalWriteUnsafe(data);
    }
    
    public void Write(string data) {
        foreach (var str in data.SplitNewLines()) {
            InternalWrite(new StringData(str));
        }
    }

    public void Write(StringData data) {
        foreach (var str in data.Text.SplitNewLines()) {
            InternalWrite(new StringData(str, false, data.BackgroundColor, data.ForegroundColor, data.FormattingType));
        }
    }

    public void Write(FormattedStringBuilder data) {
        var splits = data.Expand();

        foreach (var line in splits) {
            InternalWriteStringDataChain(line);
        }
    }

    #endregion
    
    
    #region For other writers to implement.

    protected abstract void InternalWrite(StringData data);
    protected abstract void InternalWriteStringDataChain(List<StringData> data);
    protected abstract void InternalWriteUnsafe(string data);
    
    #endregion
}