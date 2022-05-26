using System.Collections.Concurrent;
using System.Diagnostics;
using OpenTelemetry;
using Spectre.Console;

namespace graphical_console_exporter;

public class GraphicalConsoleExporter : BaseExporter<Activity>
{
    private readonly ConcurrentDictionary<string, InternalTrace> _traces = new ConcurrentDictionary<string, InternalTrace>();
    private Timer? _trigger;

    public GraphicalConsoleExporter(
        int startupDelayInMilliseconds = 1000, 
        int pollingIntervalForWritingTraces = 3000,
        bool disableAutoWrite = false)
    {
        if (!disableAutoWrite)
            _trigger = new Timer(new TimerCallback(WriteAllTracesToConsole),
                _traces,
                startupDelayInMilliseconds,
                pollingIntervalForWritingTraces);
    }

    public void WriteTraceById(string traceId)
    {
        if (_traces == null ||
            !_traces.TryRemove(traceId, out InternalTrace? trace))
            {
                Console.WriteLine("No trace by that name");
                return;
            }

        WriteInternalTraceToConsole(trace);
    }

    private static void WriteAllTracesToConsole(object? state)
    {
        var traces = state as ConcurrentDictionary<string, InternalTrace>;
        if (traces == null)
            return;

        var keys = traces?.Keys ?? new List<string>();
        foreach (var key in keys)
        {
            if (traces == null ||
                !traces.TryRemove(key, out InternalTrace? trace))
                continue;

            WriteInternalTraceToConsole(trace);
        }
    }

    private static void WriteInternalTraceToConsole(InternalTrace trace)
    {
        var table = new Table();
        table.Title = new TableTitle($"[bold]TraceId:[/] [yellow]{trace.TraceId}[/]");
        
        table.Expand();
        table.AddColumn("Name", o => {
            o.Width = 8;
        });
        table.AddColumn("Duration (ms)");
        table.AddColumn("");

        var indentList = new Dictionary<string, int>();
        Activity? rootSpan = null; 
        int rootSpanLength = 0;
        foreach(var span in trace.Activities.OrderBy(a => a.StartTimeUtc).ToList())
        {
            var spanString = "";
            if (span.ParentId == null)
            {
                rootSpanLength = trace.Activities.Count < 50 ? 50 : trace.Activities.Count;
                rootSpan = span;
                table.AddRow(span.DisplayName.Truncate(30), span.Duration.Milliseconds.ToString(), "[yellow on yellow]" + "".PadRight(rootSpanLength, ' ') + "[/]");
                continue;
            }

            var rootSpanDuration = rootSpan?.Duration.Milliseconds ?? 0;
            double millisecondsPerBlock = rootSpanDuration / rootSpanLength;

            var spanOffsetFromRootMilliseconds = span.StartTimeUtc - rootSpan?.StartTimeUtc;
            var spanMilliseconds = spanOffsetFromRootMilliseconds?.Milliseconds ?? 0;

            var blocksToWriteForPadding = (int)Math.Round(spanMilliseconds / millisecondsPerBlock);
            var blocksToWriteForSpan = (int)Math.Round(span.Duration.Milliseconds / millisecondsPerBlock);
            var blocksToWriteForEnd = rootSpanLength - blocksToWriteForPadding - blocksToWriteForSpan;
            
            table.AddRow(span.DisplayName.Truncate(30), span.Duration.Milliseconds.ToString(),
                "".PadRight(blocksToWriteForPadding) + $"[green on green]{spanString.PadRight(blocksToWriteForSpan, ' ')}[/]" + "".PadRight(blocksToWriteForEnd));

        }
        AnsiConsole.Write(table);
    }

    public override ExportResult Export(in Batch<Activity> batch)
    {
        foreach (var activity in batch)
        {
            var traceId = activity.TraceId.ToString();

            _traces.AddOrUpdate(traceId,
                (a) =>
                {
                    return new InternalTrace
                    {
                        TraceId = a,
                        Activities = new List<Activity> { activity }
                    };
                },
                (a, t) => { 
                    t.Activities.Add(activity);
                    return t; 
                    });
        }
        return ExportResult.Success;
    }
}

public class InternalTrace
{
    public string TraceId { get; init; } = null!;
    public List<Activity> Activities { get; set; } = new List<Activity>();
}

public static class StringExtensions
{
    public static string Truncate(this string input, int maxLength)
    {
        if (input.Length > maxLength)
            return input.Substring(0, maxLength - 3) + "...";

        return input;
    }
}