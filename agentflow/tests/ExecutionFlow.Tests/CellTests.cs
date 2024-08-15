using System.Collections.Immutable;
using AgentFlow;
using Microsoft.Extensions.Logging.Abstractions;

namespace ExecutionFlow.Tests;

public class CellTests
{
    public CellTests()
    {
        Logging.TryRegisterLoggerFactory(NullLoggerFactory.Instance);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    [InlineData(9)]
    [InlineData(10)]
    public async Task WhileCell_TerminatesAsync(int startingNum)
    {
        var whileLoop = new WhileCell<int>()
        {
            Condition = new LambdaCondition<int>(i => i < 10),
            WhileTrue = new LambdaCell<int>(i => i + 1),
        };

        var runner = new CellRunner<int>();

        var result = await runner.RunAsync(whileLoop, rootInput: startingNum);

        Assert.Equal(expected: 10, actual: result);
    }

    [Theory]
    [InlineData(true, 100)]
    [InlineData(false, -100)]
    public async Task IfCell_TerminatesAsync(bool flag, int expected)
    {
        ICondition<int> condition = flag ?
            new AlwaysTrueCondition<int>() :
            new AlwaysFalseCondition<int>();

        var ifCell = new IfCell<int>
        {
            Condition = condition,
            NextIfTrue = new LambdaCell<int>(i => 100),
            NextIfFalse = new LambdaCell<int>(i => -100),
        };

        var runner = new CellRunner<int>();

        var result = await runner.RunAsync(ifCell, rootInput: 0);

        Assert.Equal(expected: expected, actual: result);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(9)]
    [InlineData(100)]
    public async Task CelSequence_ExecutesAllItemsAsync(int count)
    {
        var sequence = Enumerable
            .Range(0, count)
            .Select(_ => new LambdaCell<int>(i => i + 1))
            .Cast<Cell<int>>()
            .ToImmutableArray();

        var cellSequence = new CellSequence<int>(
            sequence: sequence, next: null);

        var runner = new CellRunner<int>();
        var result = await runner.RunAsync(cellSequence, rootInput: 0);

        Assert.Equal(expected: count, actual: result);
    }

    [Fact]
    public async Task CelSequence_ExecutesAllItemsInOrderAsync()
    {
        var sequence = new[]
        {
            new LambdaCell<string>(s => s + "a"),
            new LambdaCell<string>(s => s + "b"),
            new LambdaCell<string>(s => s + "c"),
            new LambdaCell<string>(s => s + "d"),
        }
        .Cast<Cell<string>>()
        .ToImmutableArray();

        var cellSequence = new CellSequence<string>(
            sequence: sequence, next: null);

        string input = string.Empty;
        var runner = new CellRunner<string>();
        string result = await runner.RunAsync(cellSequence, rootInput: input);

        Assert.Equal(expected: "abcd", actual: result);
    }

    [Fact]
    public async Task CelSequence_NextIsAStackPushAsync()
    {
        var sequence = new[]
        {
            new LambdaCell<string>(s => s + "a"),
            new LambdaCell<string>(s => s + "b"),
            new LambdaCell<string>(
                s => s + "c",
                next: new LambdaCell<string>(
                    s => s + "x",
                    next: new LambdaCell<string>(s => s + "y"))),
            new LambdaCell<string>(s => s + "d"),
        }
        .Cast<Cell<string>>()
        .ToImmutableArray();

        var cellSequence = new CellSequence<string>(
            sequence: sequence, next: null);

        string input = string.Empty;
        var runner = new CellRunner<string>();
        string result = await runner.RunAsync(cellSequence, rootInput: input);

        Assert.Equal(expected: "abcxyd", actual: result);
    }
}
