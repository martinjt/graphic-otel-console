using System.Collections.Concurrent;
using System.Diagnostics;
using OpenTelemetry;
using Spectre.Console;
using System.Linq;

namespace graphical_console_exporter;

public class GraphicalConsoleExporter : BaseExporter<Activity>
{
    private readonly ConcurrentDictionary<string, InternalTrace> _traces = new ConcurrentDictionary<string, InternalTrace>();
    private Timer _trigger;

    public GraphicalConsoleExporter()
    {
        _trigger = new Timer(new TimerCallback(WriteTracesToConsole),
        _traces,
        5000,
        30000);
    }

    private static void WriteTracesToConsole(object? state)
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
        table.AddColumn("Service", o => {
            o.Width = 3;
        });
        table.AddColumn("Span", o => {
            o.Width = 5;
        });
        table.AddColumn("");
        // var newList = new List<Activity>();
        // newList.AddRange(newList);

        // var orderedList = 

        var indentList = new Dictionary<string, int>();
        Activity rootSpan = null; 
        int rootSpanLength = 0;
        foreach(var span in trace.Activities.OrderBy(a => a.StartTimeUtc).ToList())
        {
            var spanString = "";
            if (span.ParentId == null)
            {
                rootSpanLength = trace.Activities.Count < 30 ? 30 : trace.Activities.Count;
                rootSpan = span;
                spanString.PadRight(rootSpanLength);
                table.AddRow(span.Source.Name, $"{span.DisplayName}", "[white on yellow]" + spanString + "[/]");
                continue;
            }

            double millisecondsPerBlock = rootSpan?.Duration.Milliseconds ?? 0 / rootSpanLength;

            var spanOffsetFromRootMilliseconds = span.StartTimeUtc - rootSpan?.StartTimeUtc;

            var blocksToWriteForPadding = (int)Math.Ceiling(spanOffsetFromRootMilliseconds?.Milliseconds ?? 0/ millisecondsPerBlock);
            var blocksToWriteForSpan = (int)Math.Round(span.Duration.Milliseconds / millisecondsPerBlock);
            var blocksToWriteForEnd = rootSpanLength - blocksToWriteForPadding - blocksToWriteForSpan;
            
            spanString.PadRight(blocksToWriteForSpan);

            table.AddRow(span.Source.Name, $"{span.DisplayName}", 
                new char[blocksToWriteForPadding] + $"[black on green]{spanString}[/]" + new char[blocksToWriteForEnd]);

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
    public string TraceId { get; init; }
    public List<Activity> Activities { get; set; } = new List<Activity>();
}