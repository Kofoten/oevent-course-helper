using Microsoft.Extensions.Logging;

namespace OEventCourseHelper.Logging;

internal sealed class TerminalWriter(
    string traceLabel,
    string debugLabel,
    string infoLabel,
    string warningLabel,
    string errorLabel,
    string criticalLabel,
    string noneLabel,
    string exceptionLabel,
    string eventIdColor,
    string parameterHighlightingColor,
    string exceptionTypeColor,
    string innerExceptionColor)
{
    private const string resetColor = "\x1b[0m";

    public void WriteTraceLabel(TextWriter writer) => writer.Write(traceLabel);
    public void WriteDebugLabel(TextWriter writer) => writer.Write(debugLabel);
    public void WriteInfoLabel(TextWriter writer) => writer.Write(infoLabel);
    public void WriteWarningLabel(TextWriter writer) => writer.Write(warningLabel);
    public void WriteErrorLabel(TextWriter writer) => writer.Write(errorLabel);
    public void WriteCriticalLabel(TextWriter writer) => writer.Write(criticalLabel);
    public void WriteNoneLabel(TextWriter writer) => writer.Write(noneLabel);
    public void WriteExceptionLabel(TextWriter writer) => writer.Write(exceptionLabel);

    public void WriteEventId(TextWriter writer, EventId eventId)
    {
        writer.Write(eventIdColor);
        writer.Write(eventId.Id);
        writer.Write(resetColor);

        if (!string.IsNullOrWhiteSpace(eventId.Name))
        {
            writer.Write("|");
            writer.Write(eventIdColor);
            writer.Write(eventId.Name);
            writer.Write(resetColor);
        }
    }

    public void WriteParameter(TextWriter writer, string text)
    {
        writer.Write(parameterHighlightingColor);
        writer.Write(text);
        writer.Write(resetColor);
    }

    public void WriteException(TextWriter textWriter, Exception? exception)
    {
        if (exception is null)
        {
            return;
        }

        textWriter.Write(exceptionTypeColor);
        textWriter.Write(exception.GetType().FullName);
        textWriter.Write(": ");
        textWriter.Write(resetColor);

        textWriter.WriteLine(exception.Message);

        if (!string.IsNullOrWhiteSpace(exception.StackTrace))
        {
            textWriter.Write("\x1b[90m");
            textWriter.WriteLine(exception.StackTrace);
            textWriter.Write("\x1b[0m");
        }

        textWriter.WriteLine();

        if (exception.InnerException is not null)
        {
            textWriter.Write(innerExceptionColor);
            textWriter.Write("---> Inner Exception: ");
            textWriter.Write(resetColor);

            WriteException(textWriter, exception.InnerException);
        }
    }

    public static TerminalWriter Create()
    {
        var support = TerminalCapabilities.GetSupportedPalette();

        return support switch
        {
            ColorSupportLevel.None => new TerminalWriter(
                "TRACE",
                "DEBUG",
                "INFO",
                "WARNING",
                "ERROR",
                "CRITICAL",
                "NONE",
                "EXCEPTION",
                "",
                "",
                "",
                ""),
            ColorSupportLevel.TrueColor => new TerminalWriter(
                "\x1b[38;2;192;192;192mTRACE\x1b[0m",
                "\x1b[38;2;0;255;255mDEBUG\x1b[0m",
                "\x1b[38;2;0;175;0mINFO\x1b[0m",
                "\x1b[38;2;175;175;0mWARNING\x1b[0m",
                "\x1b[38;2;175;0;0mERROR\x1b[0m",
                "\x1b[38;2;255;0;255mCRITICAL\x1b[0m",
                "\x1b[38;2;255;255;255mNONE\x1b[0m",
                "\x1b[38;2;0;0;0;48;2;255;0;0mEXCEPTION\x1b[0m",
                "\x1b[38;2;135;215;175m",
                "\x1b[38;2;175;175;255m",
                "\x1b[38;2;255;95;95m",
                "\x1b[38;2;255;255;135m"),
            ColorSupportLevel.Palette256 => new TerminalWriter(
                "\x1b[38;5;250mTRACE\x1b[0m",
                "\x1b[38;5;51mDEBUG\x1b[0m",
                "\x1b[38;5;34mINFO\x1b[0m",
                "\x1b[38;5;142mWARNING\x1b[0m",
                "\x1b[38;5;124mERROR\x1b[0m",
                "\x1b[38;5;201mCRITICAL\x1b[0m",
                "\x1b[38;5;231mNONE\x1b[0m",
                "\x1b[38;5;16;48;5;196mEXCEPTION\x1b[0m",
                "\x1b[38;5;115m",
                "\x1b[38;5;147m",
                "\x1b[38;5;203m",
                "\x1b[38;5;228m"),
            _ => new TerminalWriter(
                "\x1b[37mTRACE\x1b[0m",
                "\x1b[96mDEBUG\x1b[0m",
                "\x1b[32mINFO\x1b[0m",
                "\x1b[33mWARNING\x1b[0m",
                "\x1b[31mERROR\x1b[0m",
                "\x1b[95mCRITICAL\x1b[0m",
                "\x1b[97mNONE\x1b[0m",
                "\x1b[30;41mEXCEPTION\x1b[0m",
                "\x1b[92m",
                "\x1b[94m",
                "\x1b[91m",
                "\x1b[93m"),
        };
    }
}
