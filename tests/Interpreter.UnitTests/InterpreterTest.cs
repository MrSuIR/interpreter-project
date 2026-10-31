using System.Globalization;

using Lait.Lexemes;
using Lait.Parsing;

using Xunit;

namespace Lait.Interpreter.UnitTests;

public class InterpreterTest
{
    public static TheoryData<string, string> ExpressionsFollowSpecificationData()
    {
        return new()
        {
            { "10 + 7 * (2 * 3 + 4) + 5 + -5", "80" },
            { "10 - 3 - 2", "5" },
            { "24 / 3 / 2", "4" },
            { "-7 / 2", "-3" },
            { "--5", "5" },
            { "(2 + 3) * 4", "20" },
            { "max(abs(-8), min(9, 5))", "8" },
            { "-2147483647 - 1", "-2147483648" },
            { "0", "0" },
            { "2147483647", "2147483647" },
            { "1 + 2 * 3", "7" },
            { "20 / 2 * 3", "30" },
            { "20 - 6 + 2", "16" },
            { "7 / -2", "-3" },
            { "-7 / -2", "3" },
            { "---5", "-5" },
            { "2 * -3", "-6" },
            { "0 - 2147483647", "-2147483647" },
            { "abs(0)", "0" },
            { "abs(2147483647)", "2147483647" },
            { "min(-2147483647 - 1, 2147483647)", "-2147483648" },
            { "max(-2147483647 - 1, 2147483647)", "2147483647" },
            { "min(4, 4) + max(-2, -2)", "2" },
            { "abs(min(-9, max(-5, -3)))", "9" },
        };
    }

    public static TheoryData<string> InvalidStatementsAreRejectedData()
    {
        return new()
        {
            "print(1)",
            ";",
            "print();",
            "print(1, 2);",
            "readInt(1);",
            "abs();",
            "abs(1, 2);",
            "min(1);",
            "max(1, 2, 3);",
            "print(print(1));",
            "unknown();",
            "+1;",
            "1 % 2;",
            "print(01);",
            "print(2147483648);",
            "print(1); /* unfinished",
            "print(1); @",
            "max();",
            "min();",
            "min(1, 2, 3);",
            "max(1);",
            "abs(1,);",
            "min(,1);",
            "readInt(1, 2);",
            "print(1 +);",
            "print((1);",
            "print(-);",
            "print(true);",
            "print(\"text\");",
            "print(readString());",
            "print(Abs(1));",
            "print(1 == 1);",
            "int x;",
            "print(1);;",
            "-2147483648;",
            "print(1); \"unfinished",
            "(print(1));",
            "1 + print(1);",
            "abs(print(1));",
            "main();",
        };
    }

    public static TheoryData<string> ExactlyOneCompleteEntryPointIsRequiredData()
    {
        return new()
        {
            "",
            "func other(): void {}",
            "func main(x): void {}",
            "func main(): int {}",
            "func main(): void {",
            "func main(): void {} func main(): void {}",
            "func main(): void {} 1;",
            "func Main(): void {}",
            "func main() {}",
            "func main(): void",
            "func main(): void { print(1);",
            "func main(): void {} @",
            "print(1);",
        };
    }

    public static TheoryData<string, string> RuntimeErrorStopsLaterInstructionsAndPreservesOutputData()
    {
        return new()
        {
            { "1 / 0", "Деление на ноль." },
            { "2147483647 + 1", "Результат операции выходит за диапазон int." },
            { "(-2147483647 - 1) - 1", "Результат операции выходит за диапазон int." },
            { "2147483647 * 2", "Результат операции выходит за диапазон int." },
            { "-(-2147483647 - 1)", "Результат операции выходит за диапазон int." },
            { "(-2147483647 - 1) / -1", "Результат операции выходит за диапазон int." },
            { "abs(-2147483647 - 1)", "Результат операции выходит за диапазон int." },
            { "(-2147483647 - 1) * -1", "Результат операции выходит за диапазон int." },
        };
    }

    public static TheoryData<string, string> ReadIntegerAcceptsSpecifiedFormatData()
    {
        return new()
        {
            { " \t+007\t \n", "7" },
            { "-2147483648\n", "-2147483648" },
            { "2147483647\n", "2147483647" },
            { "0\n", "0" },
            { "-000\n", "0" },
            { "00042\n", "42" },
            { "42", "42" },
            { "\t -15 \t\r\n", "-15" },
        };
    }

    public static TheoryData<string> InvalidInputIsARuntimeErrorData()
    {
        return new()
        {
            "",
            "\n",
            "+\n",
            "1 2\n",
            "+ 2\n",
            "1.5\n",
            "2147483648\n",
            "-2147483649\n",
            "\u00a01\n",
            "１２\n",
            "-\n",
            " \t\n",
            "1\t2\n",
            "1e2\n",
            "0x10\n",
            "--1\n",
            "+-1\n",
            "1\u00a0\n",
            "\v1\n",
            "999999999999999999999999\n",
        };
    }

    [Theory]
    [MemberData(nameof(ExpressionsFollowSpecificationData))]
    public void ExpressionsFollowSpecification(string expression, string expected)
    {
        string output = Run($"print({expression});");

        Assert.Equal(expected + Environment.NewLine, output);
    }

    [Fact]
    public void StatementsAndOperandsExecuteInSourceOrder()
    {
        string output = Run(
            "readInt(); print(readInt() - readInt()); print(min(readInt(), readInt()));",
            "99\n10\n3\n8\n2\n");

        Assert.Equal($"7{Environment.NewLine}2{Environment.NewLine}", output);
    }

    [Fact]
    public void CommentsAndEmptyBodyAreAccepted()
    {
        using StringWriter output = new(CultureInfo.InvariantCulture);
        Parser parser = new(new Lexer("/* header */ func // comment\nmain(): void { /* body */ } // end"), TextReader.Null, output);

        parser.ParseProgram();

        Assert.Equal(string.Empty, output.ToString());
    }

    [Theory]
    [MemberData(nameof(InvalidStatementsAreRejectedData))]
    public void InvalidStatementsAreRejected(string body)
    {
        Parser parser = new(new Lexer($"func main(): void {{ {body} }}"), TextReader.Null, TextWriter.Null);

        Assert.Throws<FormatException>(() => parser.ParseProgram());
    }

    [Theory]
    [MemberData(nameof(ExactlyOneCompleteEntryPointIsRequiredData))]
    public void ExactlyOneCompleteEntryPointIsRequired(string source)
    {
        Parser parser = new(new Lexer(source), TextReader.Null, TextWriter.Null);

        Assert.Throws<FormatException>(() => parser.ParseProgram());
    }

    [Fact]
    public void SyntaxErrorPreservesPriorInputAndOutputAndStopsExecution()
    {
        using StringReader input = new("7\n9\n");
        using StringWriter output = new(CultureInfo.InvariantCulture);
        Parser parser = new(new Lexer("func main(): void { print(readInt()); print(1) readInt(); }"), input, output);

        Assert.Throws<FormatException>(() => parser.ParseProgram());
        Assert.Equal("7" + Environment.NewLine, output.ToString());
        Assert.Equal("9", input.ReadLine());
    }

    [Theory]
    [MemberData(nameof(RuntimeErrorStopsLaterInstructionsAndPreservesOutputData))]
    public void RuntimeErrorStopsLaterInstructionsAndPreservesOutput(string expression, string expectedMessage)
    {
        using StringReader input = new(string.Empty);
        using StringWriter output = new(CultureInfo.InvariantCulture);
        Parser parser = new(new Lexer($"func main(): void {{ print(42); {expression}; print(99); }}"), input, output);

        InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => parser.ParseProgram());

        Assert.Equal(expectedMessage, error.Message);
        Assert.Equal("42" + Environment.NewLine, output.ToString());
    }

    [Theory]
    [MemberData(nameof(ReadIntegerAcceptsSpecifiedFormatData))]
    public void ReadIntegerAcceptsSpecifiedFormat(string input, string expected)
    {
        string output = Run("print(readInt());", input);

        Assert.Equal(expected + Environment.NewLine, output);
    }

    [Theory]
    [MemberData(nameof(InvalidInputIsARuntimeErrorData))]
    public void InvalidInputIsARuntimeError(string input)
    {
        Assert.Throws<InvalidOperationException>(() => Run("print(readInt());", input));
    }

    [Fact]
    public void ExcessiveNestingReportsErrorInsteadOfStackOverflow()
    {
        string expression = new string('(', 100000) + "1" + new string(')', 100000);
        Parser parser = new(new Lexer($"func main(): void {{ {expression}; }}"), TextReader.Null, TextWriter.Null);
        Exception? error = null;
        Thread thread = new(() => error = Record.Exception(() => parser.ParseProgram()), 256 * 1024);

        thread.Start();
        thread.Join();

        InvalidOperationException runtimeError = Assert.IsType<InvalidOperationException>(error);
        Assert.Equal("Недостаточно стека для разбора вложенного выражения.", runtimeError.Message);
    }

    public static TheoryData<string> DeepExpressionsData()
    {
        return new()
        {
            new string('-', 300) + "1",
            new string('(', 300) + "1" + new string(')', 300),
            string.Concat(Enumerable.Repeat("abs(", 300)) + "1" + new string(')', 300),
        };
    }

    [Theory]
    [MemberData(nameof(DeepExpressionsData))]
    public void DeepExpressionsAreAccepted(string expression)
    {
        string output = Run($"print({expression});");

        Assert.Equal("1" + Environment.NewLine, output);
    }

    [Fact]
    public void NestedArgumentsAreEvaluatedOnceInSourceOrder()
    {
        using StringReader input = new("10\n3\n2\n99\n");
        using StringWriter output = new(CultureInfo.InvariantCulture);
        Parser parser = new(new Lexer("func main(): void { print(max(readInt() - readInt(), readInt())); }"), input, output);

        parser.ParseProgram();

        Assert.Equal("7" + Environment.NewLine, output.ToString());
        Assert.Equal("99", input.ReadLine());
    }

    public static TheoryData<string> InterruptedPrintData()
    {
        return new()
        {
            "print(1)",
            "print(1) readInt();",
            "print(1, 2);",
            "print(readInt())",
            "print(1) + 1;",
            "abs(print(1));",
        };
    }

    [Theory]
    [MemberData(nameof(InterruptedPrintData))]
    public void IncompletePrintDoesNotProduceOutput(string body)
    {
        using StringReader input = new("7\n");
        using StringWriter output = new(CultureInfo.InvariantCulture);
        Parser parser = new(new Lexer($"func main(): void {{ {body} }}"), input, output);

        Assert.Throws<FormatException>(() => parser.ParseProgram());

        Assert.Equal(string.Empty, output.ToString());
    }

    [Fact]
    public void RuntimeErrorDoesNotConsumeLaterInput()
    {
        using StringReader input = new("9\n");
        using StringWriter output = new(CultureInfo.InvariantCulture);
        Parser parser = new(new Lexer("func main(): void { print(42); 1 / 0; readInt(); }"), input, output);

        Assert.Throws<InvalidOperationException>(() => parser.ParseProgram());

        Assert.Equal("42" + Environment.NewLine, output.ToString());
        Assert.Equal("9", input.ReadLine());
    }

    [Fact]
    public void StandaloneExpressionsDoNotProduceOutput()
    {
        string output = Run("1 + 2; abs(-3); min(4, 5); readInt();", "7\n");

        Assert.Equal(string.Empty, output);
    }

    [Fact]
    public void ParserRejectsRepeatedExecution()
    {
        Parser parser = new(new Lexer("func main(): void {}"), TextReader.Null, TextWriter.Null);
        parser.ParseProgram();

        InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => parser.ParseProgram());

        Assert.Equal("Парсер допускает только один разбор программы.", error.Message);
    }

    private static string Run(string body, string inputText = "")
    {
        using StringReader input = new(inputText);
        using StringWriter output = new(CultureInfo.InvariantCulture);
        Parser parser = new(new Lexer($"func main(): void {{ {body} }}"), input, output);
        parser.ParseProgram();
        return output.ToString();
    }
}