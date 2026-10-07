using System.Globalization;

using Lait.Lexemes;
using Lait.Parsing;
using Lait.Runtime;

using Xunit;

namespace Lait.Interpreter.UnitTests;

public class InterpreterTest
{
    [Theory]
    [InlineData("10 + 7 * (2 * 3 + 4) + 5 + -5", "80")]
    [InlineData("10 - 3 - 2", "5")]
    [InlineData("24 / 3 / 2", "4")]
    [InlineData("-7 / 2", "-3")]
    [InlineData("--5", "5")]
    [InlineData("(2 + 3) * 4", "20")]
    [InlineData("max(abs(-8), min(9, 5))", "8")]
    [InlineData("-2147483647 - 1", "-2147483648")]
    public void Expressions_follow_specification(string expression, string expected)
    {
        string output = Run($"print({expression});");

        Assert.Equal(expected + Environment.NewLine, output);
    }

    [Fact]
    public void Statements_and_operands_execute_in_source_order()
    {
        string output = Run(
            "readInt(); print(readInt() - readInt()); print(min(readInt(), readInt()));",
            "99\n10\n3\n8\n2\n");

        Assert.Equal($"7{Environment.NewLine}2{Environment.NewLine}", output);
    }

    [Fact]
    public void Comments_and_empty_body_are_accepted()
    {
        Parser parser = new(new Lexer("/* header */ func // comment\nmain(): void { /* body */ } // end"));

        Assert.Empty(parser.ParseProgram());
    }

    [Theory]
    [InlineData("print(1)")]
    [InlineData(";")]
    [InlineData("print();")]
    [InlineData("print(1, 2);")]
    [InlineData("readInt(1);")]
    [InlineData("abs();")]
    [InlineData("abs(1, 2);")]
    [InlineData("min(1);")]
    [InlineData("max(1, 2, 3);")]
    [InlineData("print(print(1));")]
    [InlineData("unknown();")]
    [InlineData("+1;")]
    [InlineData("1 % 2;")]
    [InlineData("print(01);")]
    [InlineData("print(2147483648);")]
    [InlineData("print(1); /* unfinished")]
    [InlineData("print(1); @")]
    public void Invalid_statements_are_rejected(string body)
    {
        Parser parser = new(new Lexer($"func main(): void {{ {body} }}"));

        Assert.Throws<FormatException>(() => parser.ParseProgram());
    }

    [Theory]
    [InlineData("")]
    [InlineData("func other(): void {}")]
    [InlineData("func main(x): void {}")]
    [InlineData("func main(): int {}")]
    [InlineData("func main(): void {")]
    [InlineData("func main(): void {} func main(): void {}")]
    [InlineData("func main(): void {} 1;")]
    public void Exactly_one_complete_entry_point_is_required(string source)
    {
        Parser parser = new(new Lexer(source));

        Assert.Throws<FormatException>(() => parser.ParseProgram());
    }

    [Fact]
    public void Syntax_errors_prevent_input_and_output()
    {
        using StringReader input = new("7\n");
        using StringWriter output = new(CultureInfo.InvariantCulture);
        Parser parser = new(new Lexer("func main(): void { print(readInt()); print(1) }"));
        VirtualMachine machine = new();

        Assert.Throws<FormatException>(() => machine.Execute(parser.ParseProgram(), input, output));
        Assert.Equal(string.Empty, output.ToString());
        Assert.Equal("7", input.ReadLine());
    }

    [Theory]
    [InlineData("1 / 0")]
    [InlineData("2147483647 + 1")]
    [InlineData("(-2147483647 - 1) - 1")]
    [InlineData("2147483647 * 2")]
    [InlineData("-(-2147483647 - 1)")]
    [InlineData("(-2147483647 - 1) / -1")]
    [InlineData("abs(-2147483647 - 1)")]
    public void Runtime_error_stops_later_instructions_and_preserves_output(string expression)
    {
        using StringReader input = new(string.Empty);
        using StringWriter output = new(CultureInfo.InvariantCulture);
        Parser parser = new(new Lexer($"func main(): void {{ print(42); {expression}; print(99); }}"));
        VirtualMachine machine = new();

        Assert.Throws<InvalidOperationException>(() => machine.Execute(parser.ParseProgram(), input, output));
        Assert.Equal("42" + Environment.NewLine, output.ToString());
    }

    [Theory]
    [InlineData(" \t+007\t \n", "7")]
    [InlineData("-2147483648\n", "-2147483648")]
    [InlineData("2147483647\n", "2147483647")]
    public void Read_integer_accepts_specified_format(string input, string expected)
    {
        Assert.Equal(expected + Environment.NewLine, Run("print(readInt());", input));
    }

    [Theory]
    [InlineData("")]
    [InlineData("\n")]
    [InlineData("+\n")]
    [InlineData("1 2\n")]
    [InlineData("+ 2\n")]
    [InlineData("1.5\n")]
    [InlineData("2147483648\n")]
    [InlineData("-2147483649\n")]
    [InlineData("\u00a01\n")]
    [InlineData("１２\n")]
    public void Invalid_input_is_a_runtime_error(string input)
    {
        Assert.Throws<InvalidOperationException>(() => Run("print(readInt());", input));
    }

    [Fact]
    public void Excessive_nesting_reports_error_instead_of_stack_overflow()
    {
        Parser parser = new(new Lexer($"func main(): void {{ {new string('-', 300)}1; }}"));

        Assert.Throws<FormatException>(() => parser.ParseProgram());
    }

    private static string Run(string body, string inputText = "")
    {
        using StringReader input = new(inputText);
        using StringWriter output = new(CultureInfo.InvariantCulture);
        Parser parser = new(new Lexer($"func main(): void {{ {body} }}"));
        VirtualMachine machine = new();
        machine.Execute(parser.ParseProgram(), input, output);
        return output.ToString();
    }
}