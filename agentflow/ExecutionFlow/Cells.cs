using System.Collections.Immutable;
using Microsoft.Extensions.Logging;

namespace AgentFlow;

public abstract class Cell<T>
{
    public virtual Cell<T>? GetNext(T input)
    {
        return null;
    }

    public abstract Task<T> RunAsync(T input);
}

public class TerminateCell<T> : Cell<T>
{
    public override Cell<T>? GetNext(T input)
    {
        return null;
    }

    public override Task<T> RunAsync(T input)
    {
        return Task.FromResult(input);
    }
}

public class LambdaCell<T> : Cell<T>
{
    private readonly Func<T, T> _func;
    private readonly Cell<T>? _next;

    public LambdaCell(Func<T, T> func)
    {
        _func = func;
    }

    public LambdaCell(Func<T, T> func, Cell<T> next)
    {
        _func = func;
        _next = next;
    }

    public override Cell<T>? GetNext(T input)
    {
        return _next;
    }

    public override Task<T> RunAsync(T input)
    {
        return Task.FromResult(_func(input));
    }
}

public sealed class WhileCell<T> : Cell<T>
{
    private readonly ILogger<WhileCell<T>> _logger;

    public ICondition<T> Condition { get; init; } = new AlwaysTrueCondition<T>();

    public Cell<T> WhileTrue { get; init; } = new TerminateCell<T>();

    public WhileCell()
    {
        _logger = this.GetLogger();
    }

    public override Cell<T>? GetNext(T input)
    {
        // if (Condition.Evaluate(input))
        // {
        //     return this;
        // }

        // return AfterTrue;
        return null;
    }

    public override async Task<T> RunAsync(T input)
    {
        T result = input;

        int iteration = 0;

        var runner = new CellRunner<T>();

        while (Condition.Evaluate(result))
        {
            _logger.LogInformation("Loop condition true; starting iteration {Iter}", iteration);
            result = await runner.RunAsync(WhileTrue, result);
            iteration += 1;
        }

        _logger.LogInformation("Loop condition false; ending now, after iteration {Iter}", iteration);

        return result;
    }
}

public sealed class DiagnosticCell<T> : Cell<T>
{
    private readonly ILogger<DiagnosticCell<T>> _logger;
    private readonly string _diagnosticName;

    public DiagnosticCell(string diagnosticName)
    {
        _diagnosticName = diagnosticName;
        _logger = this.GetLogger();
    }

    public override Cell<T>? GetNext(T input)
    {
        return null;
    }

    public override Task<T> RunAsync(T input)
    {
        _logger.LogInformation("Diagnostic triggered: {Name}", _diagnosticName);

        return Task.FromResult(input);
    }
}

public sealed class IfCell<T> : Cell<T>
{
    public Cell<T> NextIfTrue { get; set; } = new TerminateCell<T>();

    public Cell<T> NextIfFalse { get; set; } = new TerminateCell<T>();

    public ICondition<T> Condition { get; set; } = new AlwaysTrueCondition<T>();

    private Cell<T>? _next;

    public override Cell<T>? GetNext(T input)
    {
        return _next;
    }

    public override Task<T> RunAsync(T input)
    {
        Cell<T>? cell = Condition.Evaluate(input) ? NextIfTrue : NextIfFalse;

        _next = cell;

        // var task = cell?.RunAsync(input) ?? throw new InvalidOperationException("Expected...");
        // return await task;
        return Task.FromResult(input);
    }
}

public sealed class StartCell<T> : Cell<T>
{
    private readonly Cell<T>? _next;
    private readonly ILogger<StartCell<T>> _logger;

    public StartCell(Cell<T>? next, ILogger<StartCell<T>> logger)
    {
        _next = next;
        _logger = logger;
    }

    public override Cell<T>? GetNext(T input)
    {
        // TODO: if we go the route of having a top-level sequence, and do not require
        // each Cell the responsibility of knowing the next Cell, then this can return null
        return _next;
    }

    public override Task<T> RunAsync(T input)
    {
        return Task.FromResult(input);
    }
}

public sealed class CellSequence<T> : Cell<T>
{
    private readonly ImmutableArray<Cell<T>> _sequence;
    private readonly Cell<T>? _next;

    /// <summary>
    /// TODO: three ways this can work...
    /// 1. GetNext() returns `this` as long as there are more in the sequence,
    ///     and RunAsync() will be invoked on this object and run the current index
    /// 2. GetNext() will return the object from the sequence that should be run, RunAsync() returns noop <-- won't work
    /// 3. GetNext() returns null, and a single call to RunAsync() will loop over the whole inner sequence 
    /// Let's start by going with option 3.
    /// </summary>
    public CellSequence(ImmutableArray<Cell<T>> sequence, Cell<T>? next)
    {
        _sequence = sequence;
        _next = next;
    }

    public CellSequence(ImmutableArray<Cell<T>> sequence)
        : this(sequence, next: null)
    {
    }

    public override Cell<T>? GetNext(T input)
    {
        return _next;
    }

    public override async Task<T> RunAsync(T input)
    {
        T nextInput = input;

        var runner = new CellRunner<T>();

        foreach (var cell in _sequence)
        {
            nextInput = await runner.RunAsync(cell, nextInput);
        }

        return nextInput;
    }
}

public sealed class LambdaCondition<T> : ICondition<T>
{
    private readonly Func<T, bool> _condition;

    public bool Evaluate(T input)
    {
        return _condition(input);
    }

    public LambdaCondition(Func<T, bool> condition)
    {
        _condition = condition;
    }
}

public interface ICondition<T>
{
    bool Evaluate(T input);
}

public class AlwaysTrueCondition<T> : ICondition<T>
{
    public bool Evaluate(T input)
    {
        return true;
    }
}

public class AlwaysFalseCondition<T> : ICondition<T>
{
    public bool Evaluate(T input)
    {
        return false;
    }
}

public sealed class ResultCell<T> : Cell<T>
{
    private readonly T _result;

    public ResultCell(T result)
    {
        _result = result;
    }

    public override Cell<T>? GetNext(T input)
    {
        return null;
    }

    public override Task<T> RunAsync(T input)
    {
        return Task.FromResult(_result);
    }
}

public interface ICellRunner<T>
{
    Task<T> RunAsync(Cell<T> rootCell, T rootInput);
}

public sealed class CellRunner<T> : ICellRunner<T>
{
    private readonly ILogger<CellRunner<T>> _logger;

    public CellRunner()
    {
        _logger = this.GetLogger();
    }

    public async Task<T> RunAsync(Cell<T> rootCell, T rootInput)
    {
        using var _ = _logger.BeginScope(new { runLoop = GetShortGuid() });

        Cell<T>? curCell = rootCell;

        T curInput = rootInput;

        do
        {
            if (curCell == null)
            {
                _logger.LogInformation("RunLoop complete");

                return curInput;
            }

            _logger.LogInformation("Executing cell: {CellType}", curCell.GetType().Name);
            string scopeName = $"{GetShortGuid()} {curCell.GetType().Name}";
            using (_logger.BeginScope(new { guid = scopeName }))
            {
                curInput = await curCell.RunAsync(curInput);
            }
            _logger.LogDebug("Cell output: {CellOutput}", curInput);

            curCell = curCell.GetNext(curInput);

            // logger.LogInformation("Executing cell: {cellType}", curCell.GetType().Name);
        } while (true);
    }

    private static string GetShortGuid() => Guid.NewGuid().ToString().Substring(0, 8);
}
