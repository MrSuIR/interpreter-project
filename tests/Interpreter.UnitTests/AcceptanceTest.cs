using System.Globalization;

using Lait.Lexemes;
using Lait.Parsing;

using Xunit;

namespace Interpreter.UnitTests;

public class AcceptanceTest
{
    public static TheoryData<string, string, string> CompleteProgramsData()
    {
        return new()
        {
            // Пустой блок и комментарии перед программой, в теле и после неё.
            { "/* начало */ func main(): void { /* тело */ } // конец", "", "" },

            // Приоритет операций, скобки и унарный минус.
            { "func main(): void { print(10 + 7 * (2 * 3 + 4) + 5 + -5); }", "", "80\n" },

            // Все бинарные операции и их левая ассоциативность.
            { "func main(): void { print(10 - 3 - 2); print(24 / 3 / 2); print(20 / 2 * 3 + 4 - 1); }", "", "5\n4\n33\n" },

            // Рекурсивный унарный минус и деление с усечением к нулю.
            { "func main(): void { print(---5); print(-7 / 2); print(7 / -2); }", "", "-5\n-3\n-3\n" },

            // Литералы и границы диапазона вычисляемых значений.
            { "func main(): void { print(0); print(2147483647); print(-2147483647 - 1); }", "", "0\n2147483647\n-2147483648\n" },

            // Вызовы с одним и двумя аргументами, вложенные функции.
            { "func main(): void { print(abs(-8)); print(min(9, 5)); print(max(9, 5)); print(max(abs(-8), min(9, 5))); }", "", "8\n5\n9\n8\n" },

            // Вызов без аргументов, формат ввода и последняя строка без переноса.
            { "func main(): void { print(readInt()); print(readInt()); print(readInt()); }", " \t+007\t \n-2147483648\n2147483647", "7\n-2147483648\n2147483647\n" },

            // Операнды и вложенные аргументы читаются слева направо по одному разу.
            { "func main(): void { print(max(readInt() - readInt(), readInt())); print(readInt()); }", "10\n3\n2\n99\n", "7\n99\n" },

            // Инструкции-выражения вычисляются, но сами ничего не выводят.
            { "func main(): void { 1 + 2; abs(-3); min(4, 5); max(6, 7); readInt(); print(readInt() * readInt()); }", "99\n6\n7\n", "42\n" },
        };
    }

    public static TheoryData<string, string, string> InvalidProgramsData()
    {
        return new()
        {
            { "func other(): void {}", "", "" },
            { "func main(): void {} func main(): void {}", "", "" },
            { "func main(): void { print(1) }", "", "" },
            { "func main(): void { print(42); print(readInt()) readInt(); }", "7\n9\n", "42\n" },
            { "func main(): void { print(42); min(1); print(99); }", "", "42\n" },
            { "func main(): void { print(42); @ print(99); }", "", "42\n" },
        };
    }

    public static TheoryData<string, string, string> RuntimeFailuresData()
    {
        return new()
        {
            { "1 / 0", "", "Деление на ноль." },
            { "2147483647 + 1", "", "Результат операции выходит за диапазон int." },
            { "abs(-2147483647 - 1)", "", "Результат операции выходит за диапазон int." },
            { "readInt()", "", "readInt(): достигнут конец ввода." },
            { "readInt()", "\n", "readInt(): ожидалось целое число." },
            { "readInt()", "1 2\n", "readInt(): неверный формат целого числа." },
            { "readInt()", "2147483648\n", "readInt(): число вне диапазона int." },
        };
    }

    [Theory]
    [MemberData(nameof(CompleteProgramsData))]
    public void CompleteProgramProducesExpectedOutput(string source, string inputText, string expectedOutput)
    {
        using StringReader input = new(inputText);
        using StringWriter output = new(CultureInfo.InvariantCulture);
        output.NewLine = "\n";
        Parser parser = new(new Lexer(source), input, output);

        parser.ParseProgram();

        Assert.Equal(expectedOutput, output.ToString());
    }

    [Theory]
    [MemberData(nameof(InvalidProgramsData))]
    public void InvalidProgramStopsExecution(string source, string inputText, string expectedOutput)
    {
        using StringReader input = new(inputText);
        using StringWriter output = new(CultureInfo.InvariantCulture);
        output.NewLine = "\n";
        Parser parser = new(new Lexer(source), input, output);

        Exception? error = Record.Exception(() => parser.ParseProgram());

        Assert.IsType<FormatException>(error);
        Assert.Equal(expectedOutput, output.ToString());
    }

    [Theory]
    [MemberData(nameof(RuntimeFailuresData))]
    public void RuntimeFailurePreservesOutputAndStopsExecution(string expression, string inputText, string expectedMessage)
    {
        using StringReader input = new(inputText);
        using StringWriter output = new(CultureInfo.InvariantCulture);
        output.NewLine = "\n";
        string source = $"func main(): void {{ print(42); {expression}; print(99); }}";
        Parser parser = new(new Lexer(source), input, output);

        Exception? error = Record.Exception(() => parser.ParseProgram());

        InvalidOperationException runtimeError = Assert.IsType<InvalidOperationException>(error);
        Assert.Equal(expectedMessage, runtimeError.Message);
        Assert.Equal("42\n", output.ToString());
    }
}