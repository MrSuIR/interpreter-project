using Lait.Ast.Declarations;
using Lait.Ast.Expressions;
using Lait.Ast.Statements;
using Lait.Semantics;

using Xunit;

namespace Lait.Semantics.UnitTests;

public class SemanticsCheckerTest
{
    /// <summary>
    /// Встроенные функции с сигнатурами из спецификации; реализации не используются при проверке.
    /// </summary>
    private static IReadOnlyDictionary<string, BuiltinFunction> Builtins { get; } =
        new BuiltinFunction[]
        {
            new("print", ["value"], returnsValue: false, _ => 0),
            new("readInt", [], returnsValue: true, _ => 0),
            new("abs", ["value"], returnsValue: true, _ => 0),
            new("min", ["left", "right"], returnsValue: true, _ => 0),
            new("max", ["left", "right"], returnsValue: true, _ => 0),
        }.ToDictionary(function => function.Name);

    public static TheoryData<Expression> ValidStatementsAreAcceptedData()
    {
        return new()
        {
            Number(1),
            Binary(Minus(Number(1)), BinaryOperation.Add, Number(2)),
            Call("print", Number(1)),
            Call("readInt"),
            Call("abs", Minus(Number(1))),
            Call("min", Number(1), Number(2)),
            Call("print", Call("max", Call("abs", Call("readInt")), Number(2))),
            Binary(Call("abs", Number(1)), BinaryOperation.Multiply, Minus(Call("readInt"))),
        };
    }

    public static TheoryData<Expression, string> InvalidCallsAreRejectedData()
    {
        const string printHasNoResult = "Функция 'print' не возвращает значения и допускается только как инструкция.";
        return new()
        {
            { Call("unknown"), "Неизвестная функция 'unknown'." },
            { Call("Abs", Number(1)), "Неизвестная функция 'Abs'." },
            { Call("main"), "Неизвестная функция 'main'." },
            { Call("print"), "Функция 'print' ожидает аргументов: 1; получено: 0." },
            { Call("print", Number(1), Number(2)), "Функция 'print' ожидает аргументов: 1; получено: 2." },
            { Call("readInt", Number(1)), "Функция 'readInt' ожидает аргументов: 0; получено: 1." },
            { Call("abs"), "Функция 'abs' ожидает аргументов: 1; получено: 0." },
            { Call("min", Number(1)), "Функция 'min' ожидает аргументов: 2; получено: 1." },
            { Call("print", Call("print", Number(1))), printHasNoResult },
            { Call("abs", Call("print", Number(1))), printHasNoResult },
            { Binary(Call("print", Number(1)), BinaryOperation.Add, Number(1)), printHasNoResult },
            { Binary(Number(1), BinaryOperation.Subtract, Call("print", Number(1))), printHasNoResult },
            { Minus(Call("print", Number(1))), printHasNoResult },
            { Call("print", Call("unknown")), "Неизвестная функция 'unknown'." },
            { Call("print", Binary(Number(1), BinaryOperation.Add, Call("abs"))), "Функция 'abs' ожидает аргументов: 1; получено: 0." },
        };
    }

    [Theory]
    [MemberData(nameof(ValidStatementsAreAcceptedData))]
    public void ValidStatementsAreAccepted(Expression expression)
    {
        SemanticsChecker checker = new(Builtins);

        Exception? error = Record.Exception(() => checker.Check(MainFunction(expression)));

        Assert.Null(error);
    }

    [Theory]
    [MemberData(nameof(InvalidCallsAreRejectedData))]
    public void InvalidCallsAreRejected(Expression expression, string expectedMessage)
    {
        SemanticsChecker checker = new(Builtins);

        FormatException error = Assert.Throws<FormatException>(() => checker.Check(MainFunction(expression)));

        Assert.Equal(expectedMessage, error.Message);
    }

    [Fact]
    public void ErrorInLaterStatementRejectsWholeProgram()
    {
        SemanticsChecker checker = new(Builtins);
        FunctionDeclaration program = MainFunction(Call("print", Number(1)), Call("abs"));

        Assert.Throws<FormatException>(() => checker.Check(program));
    }

    [Fact]
    public void ExcessiveNestingReportsErrorInsteadOfStackOverflow()
    {
        SemanticsChecker checker = new(Builtins);
        Expression expression = Number(1);
        for (int depth = 0; depth < 100000; depth++)
        {
            expression = Minus(expression);
        }

        Exception? error = null;
        Thread thread = new(() => error = Record.Exception(() => checker.Check(MainFunction(expression))), 256 * 1024);

        thread.Start();
        thread.Join();

        InvalidOperationException stackError = Assert.IsType<InvalidOperationException>(error);
        Assert.Equal("Недостаточно стека для проверки вложенного выражения.", stackError.Message);
    }

    private static LiteralExpression Number(int value)
    {
        return new LiteralExpression(value);
    }

    private static UnaryMinusExpression Minus(Expression operand)
    {
        return new UnaryMinusExpression(operand);
    }

    private static BinaryOperationExpression Binary(Expression left, BinaryOperation operation, Expression right)
    {
        return new BinaryOperationExpression(left, operation, right);
    }

    private static FunctionCallExpression Call(string name, params Expression[] arguments)
    {
        return new FunctionCallExpression(name, arguments);
    }

    private static FunctionDeclaration MainFunction(params Expression[] statements)
    {
        return new FunctionDeclaration("main", [.. statements.Select(expression => new ExpressionStatement(expression))]);
    }
}