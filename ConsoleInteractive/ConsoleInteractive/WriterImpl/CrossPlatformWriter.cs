using System;
using System.Collections.Generic;
using System.Text;
using ConsoleInteractive.Extension;

namespace ConsoleInteractive.WriterImpl; 

public class CrossPlatformWriter : WriterBase {

    public override void Write(FormattedStringBuilder.StringData data) {
        throw new NotImplementedException();
    }

    public override void Write(FormattedStringBuilder data) {
        int totalTextLength = 0;
        FormattedStringBuilder.StringData PreviousData = new();
        bool isPreviousDataAvailable = false;

        foreach (var text in data.strings) {
            
            // AppendNewLine means that the string is supposed to terminate.
            // So terminate the string until the next time we need to terminate it.
            // If the previous data was an append, include that in the current string.
            if (text.AppendNewLine && !PreviousData.AppendNewLine) { // If the current string is AppendNewLine and the previous line is Append only
                __WriteInternal(text.Build(), DetermineLineCount(text.Text.Length));
            } else if (text.AppendNewLine) {
                __WriteInternal(text.Build(), DetermineLineCount(text.Text.Length));
            }
            else {
                totalTextLength += text.Text.Length;
            }
        }

        __WriteInternal(data.Flatten(), DetermineLineCount(totalTextLength));
    }

    public override void WriteStringDataChain(List<FormattedStringBuilder.StringData> data) {
        int chainLength = 0;
        StringBuilder completeString = new();

        foreach (var strData in data) {
            chainLength += strData.Text.Length;
            completeString.Append(strData.Build());
        }
            
        __WriteInternal(completeString.ToString(), DetermineLineCount(chainLength));
    }
}