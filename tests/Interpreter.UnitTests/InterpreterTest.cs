using Lait.Execution;
using Lait.Lexemes;
using Lait.Parsing;
using Lait.TestDoubles;

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

        Assert.Equal(expected + "\n", output);
    }

    [Fact]
    public void StatementsAndOperandsExecuteInSourceOrder()
    {
        string output = Run(
            "readInt(); print(readInt() - readInt()); print(min(readInt(), readInt()));",
            "99\n10\n3\n8\n2\n");

        Assert.Equal("7\n2\n", output);
    }

    [Fact]
    public void CommentsAndEmptyBodyAreAccepted()
    {
        FakeEnvironment environment = new();
        LaitInterpreter interpreter = new(environment);

        interpreter.Execute("/* header */ func // comment\nmain(): void { /* body */ } // end");

        Assert.Equal(string.Empty, environment.Output);
    }

    [Theory]
    [MemberData(nameof(InvalidStatementsAreRejectedData))]
    public void InvalidStatementsAreRejected(string body)
    {
        LaitInterpreter interpreter = new(new FakeEnvironment());

        Assert.Throws<FormatException>(() => interpreter.Execute($"func main(): void {{ {body} }}"));
    }

    [Theory]
    [MemberData(nameof(ExactlyOneCompleteEntryPointIsRequiredData))]
    public void ExactlyOneCompleteEntryPointIsRequired(string source)
    {
        LaitInterpreter interpreter = new(new FakeEnvironment());

        Assert.Throws<FormatException>(() => interpreter.Execute(source));
    }

    [Fact]
    public void SyntaxErrorPreservesPriorInputAndOutputAndStopsExecution()
    {
        FakeEnvironment environment = new("7\n9\n");
        LaitInterpreter interpreter = new(environment);

        Assert.Throws<FormatException>(
            () => interpreter.Execute("func main(): void { print(readInt()); print(1) readInt(); }"));
        Assert.Equal("7\n", environment.Output);
        Assert.Equal("9", environment.ReadLine());
    }

    [Theory]
    [MemberData(nameof(RuntimeErrorStopsLaterInstructionsAndPreservesOutputData))]
    public void RuntimeErrorStopsLaterInstructionsAndPreservesOutput(string expression, string expectedMessage)
    {
        FakeEnvironment environment = new();
        LaitInterpreter interpreter = new(environment);

        InvalidOperationException error = Assert.Throws<InvalidOperationException>(
            () => interpreter.Execute($"func main(): void {{ print(42); {expression}; print(99); }}"));

        Assert.Equal(expectedMessage, error.Message);
        Assert.Equal("42\n", environment.Output);
    }

    [Theory]
    [MemberData(nameof(ReadIntegerAcceptsSpecifiedFormatData))]
    public void ReadIntegerAcceptsSpecifiedFormat(string input, string expected)
    {
        string output = Run("print(readInt());", input);

        Assert.Equal(expected + "\n", output);
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
        LaitInterpreter interpreter = new(new FakeEnvironment());
        Exception? error = null;
        Thread thread = new(
            () => error = Record.Exception(() => interpreter.Execute($"func main(): void {{ {expression}; }}")),
            256 * 1024);

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

        Assert.Equal("1\n", output);
    }

    [Fact]
    public void NestedArgumentsAreEvaluatedOnceInSourceOrder()
    {
        FakeEnvironment environment = new("10\n3\n2\n99\n");
        LaitInterpreter interpreter = new(environment);

        interpreter.Execute("func main(): void { print(max(readInt() - readInt(), readInt())); }");

        Assert.Equal("7\n", environment.Output);
        Assert.Equal("99", environment.ReadLine());
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
        FakeEnvironment environment = new("7\n");
        LaitInterpreter interpreter = new(environment);

        Assert.Throws<FormatException>(() => interpreter.Execute($"func main(): void {{ {body} }}"));

        Assert.Equal(string.Empty, environment.Output);
    }

    [Fact]
    public void RuntimeErrorDoesNotConsumeLaterInput()
    {
        FakeEnvironment environment = new("9\n");
        LaitInterpreter interpreter = new(environment);

        Assert.Throws<InvalidOperationException>(
            () => interpreter.Execute("func main(): void { print(42); 1 / 0; readInt(); }"));

        Assert.Equal("42\n", environment.Output);
        Assert.Equal("9", environment.ReadLine());
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
        Parser parser = new(new Lexer("func main(): void {}"), new Builtins(new FakeEnvironment()));
        parser.ParseProgram();

        InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => parser.ParseProgram());

        Assert.Equal("Парсер допускает только один разбор программы.", error.Message);
    }

    private static string Run(string body, string inputText = "")
    {
        FakeEnvironment environment = new(inputText);
        LaitInterpreter interpreter = new(environment);
        interpreter.Execute($"func main(): void {{ {body} }}");
        return environment.Output;
    }
}