using Lait.Ast.Declarations;
using Lait.Ast.Expressions;
using Lait.Ast.Statements;
using Lait.Execution;
using Lait.TestDoubles;

using Xunit;

namespace Lait.Execution.UnitTests;

public class AstEvaluatorTest
{
    public static TheoryData<Expression, int> ExpressionsAreEvaluatedData()
    {
        return new()
        {
            { Number(42), 42 },
            { Binary(Number(2), BinaryOperation.Add, Number(3)), 5 },
            { Binary(Number(10), BinaryOperation.Subtract, Number(3)), 7 },
            { Binary(Number(6), BinaryOperation.Multiply, Number(7)), 42 },
            { Binary(Minus(Number(7)), BinaryOperation.Divide, Number(2)), -3 },
            { Binary(Number(7), BinaryOperation.Divide, Minus(Number(2))), -3 },
            { Binary(Binary(Number(10), BinaryOperation.Subtract, Number(3)), BinaryOperation.Subtract, Number(2)), 5 },
            { Binary(Number(10), BinaryOperation.Subtract, Binary(Number(3), BinaryOperation.Subtract, Number(2))), 9 },
            { Minus(Number(5)), -5 },
            { Minus(Minus(Number(5))), 5 },
            { Binary(Minus(Number(2147483647)), BinaryOperation.Subtract, Number(1)), int.MinValue },
            { Call("abs", Minus(Number(8))), 8 },
            { Call("max", Call("abs", Minus(Number(8))), Call("min", Number(9), Number(5))), 8 },
        };
    }

    public static TheoryData<Expression, string> ArithmeticErrorsAreRuntimeErrorsData()
    {
        const string overflow = "Результат операции выходит за диапазон int.";
        return new()
        {
            { Binary(Number(2147483647), BinaryOperation.Add, Number(1)), overflow },
            { Binary(Number(int.MinValue), BinaryOperation.Subtract, Number(1)), overflow },
            { Binary(Number(2147483647), BinaryOperation.Multiply, Number(2)), overflow },
            { Binary(Number(int.MinValue), BinaryOperation.Divide, Minus(Number(1))), overflow },
            { Minus(Number(int.MinValue)), overflow },
            { Call("abs", Number(int.MinValue)), overflow },
            { Binary(Number(1), BinaryOperation.Divide, Number(0)), "Деление на ноль." },
        };
    }

    [Theory]
    [MemberData(nameof(ExpressionsAreEvaluatedData))]
    public void ExpressionsAreEvaluated(Expression expression, int expected)
    {
        AstEvaluator evaluator = new(new Builtins(new FakeEnvironment()).Functions);

        int result = evaluator.Evaluate(expression);

        Assert.Equal(expected, result);
    }

    [Theory]
    [MemberData(nameof(ArithmeticErrorsAreRuntimeErrorsData))]
    public void ArithmeticErrorsAreRuntimeErrors(Expression expression, string expectedMessage)
    {
        AstEvaluator evaluator = new(new Builtins(new FakeEnvironment()).Functions);

        InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => evaluator.Evaluate(expression));

        Assert.Equal(expectedMessage, error.Message);
    }

    [Fact]
    public void OperandsAndArgumentsAreEvaluatedLeftToRight()
    {
        FakeEnvironment environment = new("10\n3\n2\n99\n");
        AstEvaluator evaluator = new(new Builtins(environment).Functions);
        Expression expression = Call(
            "max",
            Binary(Call("readInt"), BinaryOperation.Subtract, Call("readInt")),
            Call("readInt"));

        int result = evaluator.Evaluate(expression);

        Assert.Equal(7, result);
        Assert.Equal("99", environment.ReadLine());
    }

    [Fact]
    public void StatementsAreExecutedInOrderAndResultsAreDiscarded()
    {
        FakeEnvironment environment = new("5\n");
        AstEvaluator evaluator = new(new Builtins(environment).Functions);
        FunctionDeclaration main = MainFunction(
            Call("print", Number(1)),
            Binary(Number(1), BinaryOperation.Add, Number(2)),
            Call("readInt"),
            Call("print", Number(2)));

        evaluator.Execute(main);

        Assert.Equal("1\n2\n", environment.Output);
        Assert.Null(environment.ReadLine());
    }

    [Fact]
    public void RuntimeErrorStopsLaterStatementsAndPreservesOutput()
    {
        FakeEnvironment environment = new("9\n");
        AstEvaluator evaluator = new(new Builtins(environment).Functions);
        FunctionDeclaration main = MainFunction(
            Call("print", Number(42)),
            Binary(Number(1), BinaryOperation.Divide, Number(0)),
            Call("print", Call("readInt")));

        InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => evaluator.Execute(main));

        Assert.Equal("Деление на ноль.", error.Message);
        Assert.Equal("42\n", environment.Output);
        Assert.Equal("9", environment.ReadLine());
    }

    [Fact]
    public void CallWithoutResultHasNoValue()
    {
        AstEvaluator evaluator = new(new Builtins(new FakeEnvironment()).Functions);

        InvalidOperationException error = Assert.Throws<InvalidOperationException>(
            () => evaluator.Evaluate(Call("print", Number(1))));

        Assert.Equal("Выражение не имеет значения.", error.Message);
    }

    [Fact]
    public void ExcessiveNestingReportsErrorInsteadOfStackOverflow()
    {
        AstEvaluator evaluator = new(new Builtins(new FakeEnvironment()).Functions);
        Expression expression = Number(1);
        for (int depth = 0; depth < 100000; depth++)
        {
            expression = Minus(expression);
        }

        Exception? error = null;
        Thread thread = new(() => error = Record.Exception(() => evaluator.Evaluate(expression)), 256 * 1024);

        thread.Start();
        thread.Join();

        InvalidOperationException runtimeError = Assert.IsType<InvalidOperationException>(error);
        Assert.Equal("Недостаточно стека для вычисления вложенного выражения.", runtimeError.Message);
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
        return new FunctionDeclaration(
            "main",
            [],
            returnsValue: false,
            new BlockStatement([.. statements.Select(expression => new ExpressionStatement(expression))]));
    }
}