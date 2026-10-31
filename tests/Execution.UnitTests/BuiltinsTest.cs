using System.Globalization;

using Lait.Execution;

using Xunit;

namespace Lait.Execution.UnitTests;

public class BuiltinsTest
{
    public static TheoryData<string, string[], bool> FunctionsMatchSpecificationData()
    {
        return new()
        {
            { "print", ["value"], false },
            { "readInt", [], true },
            { "abs", ["value"], true },
            { "min", ["left", "right"], true },
            { "max", ["left", "right"], true },
        };
    }

    public static TheoryData<string, int[], int> NumericFunctionsReturnSpecifiedResultData()
    {
        return new()
        {
            { "abs", [-5], 5 },
            { "abs", [0], 0 },
            { "abs", [int.MaxValue], int.MaxValue },
            { "min", [3, -2], -2 },
            { "min", [4, 4], 4 },
            { "min", [int.MinValue, int.MaxValue], int.MinValue },
            { "max", [3, -2], 3 },
            { "max", [-2, -2], -2 },
            { "max", [int.MinValue, int.MaxValue], int.MaxValue },
        };
    }

    public static TheoryData<int, string> PrintWritesNumberAndEndsLineData()
    {
        return new()
        {
            { 42, "42" },
            { 0, "0" },
            { -15, "-15" },
            { int.MinValue, "-2147483648" },
            { int.MaxValue, "2147483647" },
        };
    }

    public static TheoryData<string, string> ReadIntReportsInvalidInputData()
    {
        return new()
        {
            { "", "readInt(): достигнут конец ввода." },
            { "\n", "readInt(): ожидалось целое число." },
            { "-\n", "readInt(): ожидалось целое число." },
            { "1.5\n", "readInt(): неверный формат целого числа." },
            { "1 2\n", "readInt(): неверный формат целого числа." },
            { "2147483648\n", "readInt(): число вне диапазона int." },
            { "-2147483649\n", "readInt(): число вне диапазона int." },
        };
    }

    [Fact]
    public void OnlySpecifiedFunctionsAreDefined()
    {
        Builtins builtins = new(TextReader.Null, TextWriter.Null);

        IEnumerable<string> names = builtins.Functions.Keys.Order(StringComparer.Ordinal);

        Assert.Equal(["abs", "max", "min", "print", "readInt"], names);
    }

    [Theory]
    [MemberData(nameof(FunctionsMatchSpecificationData))]
    public void FunctionsMatchSpecification(string name, string[] parameters, bool returnsValue)
    {
        Builtins builtins = new(TextReader.Null, TextWriter.Null);

        BuiltinFunction function = builtins.Functions[name];

        Assert.Equal(name, function.Name);
        Assert.Equal(parameters, function.Parameters);
        Assert.Equal(returnsValue, function.ReturnsValue);
    }

    [Theory]
    [MemberData(nameof(NumericFunctionsReturnSpecifiedResultData))]
    public void NumericFunctionsReturnSpecifiedResult(string name, int[] arguments, int expected)
    {
        Builtins builtins = new(TextReader.Null, TextWriter.Null);

        int result = builtins.Functions[name].Invoke(arguments);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void AbsOfMinimumIntOverflows()
    {
        Builtins builtins = new(TextReader.Null, TextWriter.Null);

        Assert.Throws<OverflowException>(() => builtins.Functions["abs"].Invoke([int.MinValue]));
    }

    [Theory]
    [MemberData(nameof(PrintWritesNumberAndEndsLineData))]
    public void PrintWritesNumberAndEndsLine(int value, string expected)
    {
        using StringWriter output = new(CultureInfo.InvariantCulture);
        Builtins builtins = new(TextReader.Null, output);

        builtins.Functions["print"].Invoke([value]);

        Assert.Equal(expected + Environment.NewLine, output.ToString());
    }

    [Fact]
    public void ReadIntReadsOneLinePerCall()
    {
        using StringReader input = new(" \t+007\t \n-15\n9\n");
        Builtins builtins = new(input, TextWriter.Null);
        BuiltinFunction readInt = builtins.Functions["readInt"];

        int first = readInt.Invoke([]);
        int second = readInt.Invoke([]);

        Assert.Equal(7, first);
        Assert.Equal(-15, second);
        Assert.Equal("9", input.ReadLine());
    }

    [Theory]
    [MemberData(nameof(ReadIntReportsInvalidInputData))]
    public void ReadIntReportsInvalidInput(string inputText, string expectedMessage)
    {
        using StringReader input = new(inputText);
        Builtins builtins = new(input, TextWriter.Null);

        InvalidOperationException error = Assert.Throws<InvalidOperationException>(
            () => builtins.Functions["readInt"].Invoke([]));

        Assert.Equal(expectedMessage, error.Message);
    }
}