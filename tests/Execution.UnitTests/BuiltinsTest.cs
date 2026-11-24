using Lait.Ast.Declarations;
using Lait.Execution;
using Lait.Runtime;
using Lait.TestDoubles;

using Xunit;

using ValueType = Lait.Runtime.ValueType;

namespace Lait.Execution.UnitTests;

public class BuiltinsTest
{
    public static TheoryData<string, string[], ValueType[], ValueType> FunctionsMatchSpecificationData()
    {
        return new()
        {
            { "print", ["value"], [ValueType.Int], ValueType.Void },
            { "printFloat", ["value"], [ValueType.Float], ValueType.Void },
            { "readInt", [], [], ValueType.Int },
            { "readFloat", [], [], ValueType.Float },
            { "abs", ["value"], [ValueType.Int], ValueType.Int },
            { "min", ["left", "right"], [ValueType.Int, ValueType.Int], ValueType.Int },
            { "max", ["left", "right"], [ValueType.Int, ValueType.Int], ValueType.Int },
            { "toFloat", ["value"], [ValueType.Int], ValueType.Float },
            { "toInt", ["value"], [ValueType.Float], ValueType.Int },
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

    public static TheoryData<double, string> PrintFloatWritesSixFractionDigitsData()
    {
        return new()
        {
            { 2.5, "2.500000" },
            { 0.0, "0.000000" },
            { -12.5, "-12.500000" },
            { 1.0 / 3.0, "0.333333" },
            { 2.0 / 3.0, "0.666667" },
            { 0.1 + 0.2, "0.300000" },
            { 0.0078125, "0.007812" },
            { 0.0234375, "0.023438" },
            { 1e20, "100000000000000000000.000000" },
            { -0.0, "-0.000000" },
            { -1e-7, "-0.000000" },
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

    public static TheoryData<string, double> ReadFloatAcceptsSpecifiedFormatData()
    {
        return new()
        {
            { " \t+007.50\t \n", 7.5 },
            { "-0.25\n", -0.25 },
            { "42\n", 42.0 },
            { "0\n", 0.0 },
            { "3000000000.5", 3000000000.5 },
            { "0.1\n", 0.1 },
        };
    }

    public static TheoryData<string, string> ReadFloatReportsInvalidInputData()
    {
        return new()
        {
            { "", "readFloat(): достигнут конец ввода." },
            { "\n", "readFloat(): ожидалось число." },
            { "+ 2\n", "readFloat(): неверный формат числа." },
            { "-\n", "readFloat(): ожидалось число." },
            { ".5\n", "readFloat(): неверный формат числа." },
            { "5.\n", "readFloat(): неверный формат числа." },
            { "1.5.2\n", "readFloat(): неверный формат числа." },
            { "1,5\n", "readFloat(): неверный формат числа." },
            { "1e5\n", "readFloat(): неверный формат числа." },
            { "NaN\n", "readFloat(): неверный формат числа." },
            { "Infinity\n", "readFloat(): неверный формат числа." },
            { "1 2\n", "readFloat(): неверный формат числа." },
            { "1" + new string('0', 400) + "\n", "readFloat(): число вне диапазона float." },
        };
    }

    public static TheoryData<double, int> ToIntTruncatesTowardZeroData()
    {
        return new()
        {
            { 7.9, 7 },
            { -7.9, -7 },
            { 0.5, 0 },
            { -0.5, 0 },
            { 2147483647.9, int.MaxValue },
            { -2147483648.9, int.MinValue },
        };
    }

    public static TheoryData<double> ToIntReportsOutOfRangeData()
    {
        return new()
        {
            2147483648.0,
            -2147483649.0,
            1e20,
        };
    }

    [Fact]
    public void OnlySpecifiedFunctionsAreDefined()
    {
        Builtins builtins = new(new FakeEnvironment());

        IEnumerable<string> names = builtins.Functions.Keys.Order(StringComparer.Ordinal);

        Assert.Equal(["abs", "max", "min", "print", "printFloat", "readFloat", "readInt", "toFloat", "toInt"], names);
    }

    [Theory]
    [MemberData(nameof(FunctionsMatchSpecificationData))]
    public void FunctionsMatchSpecification(string name, string[] parameterNames, ValueType[] parameterTypes, ValueType resultType)
    {
        Builtins builtins = new(new FakeEnvironment());

        BuiltinFunction function = builtins.Functions[name];

        Assert.Equal(name, function.Name);
        Assert.Equal(parameterNames, function.Parameters.Select(parameter => parameter.Name));
        Assert.Equal(parameterTypes, function.Parameters.Select(parameter => parameter.Type));
        Assert.Equal(resultType, function.ResultType);
    }

    [Theory]
    [MemberData(nameof(NumericFunctionsReturnSpecifiedResultData))]
    public void NumericFunctionsReturnSpecifiedResult(string name, int[] arguments, int expected)
    {
        Builtins builtins = new(new FakeEnvironment());

        Value result = builtins.Functions[name].Invoke([.. arguments.Select(argument => new Value(argument))]);

        Assert.Equal(expected, result.AsInt());
    }

    [Fact]
    public void AbsOfMinimumIntOverflows()
    {
        Builtins builtins = new(new FakeEnvironment());

        Assert.Throws<OverflowException>(() => builtins.Functions["abs"].Invoke([new Value(int.MinValue)]));
    }

    [Theory]
    [MemberData(nameof(PrintWritesNumberAndEndsLineData))]
    public void PrintWritesNumberAndEndsLine(int value, string expected)
    {
        FakeEnvironment environment = new();
        Builtins builtins = new(environment);

        builtins.Functions["print"].Invoke([new Value(value)]);

        Assert.Equal(expected + "\n", environment.Output);
    }

    [Theory]
    [MemberData(nameof(PrintFloatWritesSixFractionDigitsData))]
    public void PrintFloatWritesSixFractionDigits(double value, string expected)
    {
        FakeEnvironment environment = new();
        Builtins builtins = new(environment);

        builtins.Functions["printFloat"].Invoke([new Value(value)]);

        Assert.Equal(expected + "\n", environment.Output);
    }

    [Fact]
    public void ReadIntReadsOneLinePerCall()
    {
        FakeEnvironment environment = new(" \t+007\t \n-15\n9\n");
        Builtins builtins = new(environment);
        BuiltinFunction readInt = builtins.Functions["readInt"];

        Value first = readInt.Invoke([]);
        Value second = readInt.Invoke([]);

        Assert.Equal(7, first.AsInt());
        Assert.Equal(-15, second.AsInt());
        Assert.Equal("9", environment.ReadLine());
    }

    [Theory]
    [MemberData(nameof(ReadIntReportsInvalidInputData))]
    public void ReadIntReportsInvalidInput(string inputText, string expectedMessage)
    {
        Builtins builtins = new(new FakeEnvironment(inputText));

        InvalidOperationException error = Assert.Throws<InvalidOperationException>(
            () => builtins.Functions["readInt"].Invoke([]));

        Assert.Equal(expectedMessage, error.Message);
    }

    [Theory]
    [MemberData(nameof(ReadFloatAcceptsSpecifiedFormatData))]
    public void ReadFloatAcceptsSpecifiedFormat(string inputText, double expected)
    {
        Builtins builtins = new(new FakeEnvironment(inputText));

        Value result = builtins.Functions["readFloat"].Invoke([]);

        Assert.Equal(expected, result.AsFloat());
    }

    [Theory]
    [MemberData(nameof(ReadFloatReportsInvalidInputData))]
    public void ReadFloatReportsInvalidInput(string inputText, string expectedMessage)
    {
        Builtins builtins = new(new FakeEnvironment(inputText));

        InvalidOperationException error = Assert.Throws<InvalidOperationException>(
            () => builtins.Functions["readFloat"].Invoke([]));

        Assert.Equal(expectedMessage, error.Message);
    }

    [Fact]
    public void ToFloatKeepsIntegerValue()
    {
        Builtins builtins = new(new FakeEnvironment());

        Value result = builtins.Functions["toFloat"].Invoke([new Value(int.MinValue)]);

        Assert.Equal(-2147483648.0, result.AsFloat());
    }

    [Theory]
    [MemberData(nameof(ToIntTruncatesTowardZeroData))]
    public void ToIntTruncatesTowardZero(double value, int expected)
    {
        Builtins builtins = new(new FakeEnvironment());

        Value result = builtins.Functions["toInt"].Invoke([new Value(value)]);

        Assert.Equal(expected, result.AsInt());
    }

    [Theory]
    [MemberData(nameof(ToIntReportsOutOfRangeData))]
    public void ToIntReportsOutOfRange(double value)
    {
        Builtins builtins = new(new FakeEnvironment());

        InvalidOperationException error = Assert.Throws<InvalidOperationException>(
            () => builtins.Functions["toInt"].Invoke([new Value(value)]));

        Assert.Equal("toInt(): число вне диапазона int.", error.Message);
    }
}