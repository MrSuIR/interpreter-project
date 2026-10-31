using Lait.Ast;
using Lait.Ast.Expressions;
using Lait.Execution;
using Lait.Semantics;
using Lait.TestDoubles;

using Xunit;

using static Lait.TestDoubles.AstFactory;

namespace Lait.Execution.UnitTests;

public class AstEvaluatorTest
{
    private const string Overflow = "Результат операции выходит за диапазон int.";
    private const string StackShortage = "Недостаточно стека для выполнения программы.";

    public static TheoryData<Expression, string> ExpressionsAreEvaluatedData()
    {
        return new()
        {
            { Number(42), "42" },
            { Binary(Number(2), BinaryOperation.Add, Number(3)), "5" },
            { Binary(Number(10), BinaryOperation.Subtract, Number(3)), "7" },
            { Binary(Number(6), BinaryOperation.Multiply, Number(7)), "42" },
            { Binary(Minus(Number(7)), BinaryOperation.Divide, Number(2)), "-3" },
            { Binary(Number(7), BinaryOperation.Divide, Minus(Number(2))), "-3" },
            { Binary(Binary(Number(10), BinaryOperation.Subtract, Number(3)), BinaryOperation.Subtract, Number(2)), "5" },
            { Binary(Number(10), BinaryOperation.Subtract, Binary(Number(3), BinaryOperation.Subtract, Number(2))), "9" },
            { Minus(Number(5)), "-5" },
            { Minus(Minus(Number(5))), "5" },
            { Binary(Minus(Number(2147483647)), BinaryOperation.Subtract, Number(1)), "-2147483648" },
            { Call("abs", Minus(Number(8))), "8" },
            { Call("max", Call("abs", Minus(Number(8))), Call("min", Number(9), Number(5))), "8" },
        };
    }

    public static TheoryData<Expression, string> ArithmeticErrorsAreRuntimeErrorsData()
    {
        return new()
        {
            { Binary(Number(2147483647), BinaryOperation.Add, Number(1)), Overflow },
            { Binary(Number(int.MinValue), BinaryOperation.Subtract, Number(1)), Overflow },
            { Binary(Number(2147483647), BinaryOperation.Multiply, Number(2)), Overflow },
            { Binary(Number(int.MinValue), BinaryOperation.Divide, Minus(Number(1))), Overflow },
            { Minus(Number(int.MinValue)), Overflow },
            { Call("abs", Number(int.MinValue)), Overflow },
            { Binary(Number(1), BinaryOperation.Divide, Number(0)), "Деление на ноль." },
        };
    }

    public static TheoryData<ProgramNode, string, string> ProgramsProduceExpectedOutputData()
    {
        return new()
        {
            // Объявление, чтение и присваивание переменной; значение присваивания.
            {
                MainProgram(
                    Var("x", Number(1)),
                    Expr(Assign(Name("x"), Binary(Name("x"), BinaryOperation.Add, Number(2)))),
                    Expr(Call("print", Name("x"))),
                    Var("y", Assign(Name("x"), Number(5))),
                    Expr(Call("print", Name("y"))),
                    Expr(Call("print", Name("x")))),
                "",
                "3\n5\n5\n"
            },

            // Вложенное присваивание правоассоциативно.
            {
                MainProgram(
                    Var("a", Number(0)),
                    Var("b", Number(0)),
                    Expr(Assign(Name("a"), Assign(Name("b"), Number(4)))),
                    Expr(Call("print", Name("a"))),
                    Expr(Call("print", Name("b")))),
                "",
                "4\n4\n"
            },

            // Перекрытая во вложенном блоке переменная не меняет внешнюю.
            {
                MainProgram(
                    Var("x", Number(1)),
                    Block(Var("x", Binary(Name("x"), BinaryOperation.Add, Number(1))), Expr(Call("print", Name("x")))),
                    Expr(Call("print", Name("x")))),
                "",
                "2\n1\n"
            },

            // Вложенный блок изменяет внешнюю переменную.
            {
                MainProgram(
                    Var("x", Number(1)),
                    Block(Expr(Assign(Name("x"), Number(5)))),
                    Expr(Call("print", Name("x")))),
                "",
                "5\n"
            },

            // Функция с параметрами и результатом, функция без результата.
            {
                ProgramOf(
                    MainFunction(Expr(Call("show", Call("diff", Number(10), Number(3))))),
                    Function("diff", ["a", "b"], true, Return(Binary(Name("a"), BinaryOperation.Subtract, Name("b")))),
                    Function("show", ["value"], false, Expr(Call("print", Name("value"))))),
                "",
                "7\n"
            },

            // Параметры передаются по значению.
            {
                ProgramOf(
                    MainFunction(Var("x", Number(5)), Expr(Call("print", Call("inc", Name("x")))), Expr(Call("print", Name("x")))),
                    Function("inc", ["x"], true, Expr(Assign(Name("x"), Binary(Name("x"), BinaryOperation.Add, Number(1)))), Return(Name("x")))),
                "",
                "6\n5\n"
            },

            // return завершает функцию, в том числе из вложенного блока.
            {
                ProgramOf(
                    MainFunction(Expr(Call("stop")), Expr(Call("print", Call("first")))),
                    Function("stop", [], false, Expr(Call("print", Number(1))), Return(), Expr(Call("print", Number(2)))),
                    Function("first", [], true, Block(Expr(Call("print", Number(3))), Return(Number(4))), Return(Number(5)))),
                "",
                "1\n3\n4\n"
            },

            // Вызов функции внутри выражения не портит уже вычисленные операнды.
            {
                ProgramOf(
                    MainFunction(Expr(Call("print", Binary(Number(1), BinaryOperation.Add, Call("two"))))),
                    Function("two", [], true, Expr(Number(7)), Var("t", Number(2)), Return(Name("t")))),
                "",
                "3\n"
            },

            // Аргументы пользовательской функции вычисляются слева направо по одному разу.
            {
                ProgramOf(
                    MainFunction(Expr(Call("print", Call("diff", Call("readInt"), Call("readInt"))))),
                    Function("diff", ["a", "b"], true, Return(Binary(Name("a"), BinaryOperation.Subtract, Name("b"))))),
                "10\n3\n",
                "7\n"
            },
        };
    }

    [Theory]
    [MemberData(nameof(ExpressionsAreEvaluatedData))]
    public void ExpressionsAreEvaluated(Expression expression, string expected)
    {
        FakeEnvironment environment = new();

        Run(MainProgram(Expr(Call("print", expression))), environment);

        Assert.Equal(expected + "\n", environment.Output);
    }

    [Theory]
    [MemberData(nameof(ArithmeticErrorsAreRuntimeErrorsData))]
    public void ArithmeticErrorsAreRuntimeErrors(Expression expression, string expectedMessage)
    {
        FakeEnvironment environment = new();

        InvalidOperationException error = Assert.Throws<InvalidOperationException>(
            () => Run(MainProgram(Expr(Call("print", expression))), environment));

        Assert.Equal(expectedMessage, error.Message);
    }

    [Theory]
    [MemberData(nameof(ProgramsProduceExpectedOutputData))]
    public void ProgramsProduceExpectedOutput(ProgramNode program, string input, string expectedOutput)
    {
        FakeEnvironment environment = new(input);

        Run(program, environment);

        Assert.Equal(expectedOutput, environment.Output);
    }

    [Fact]
    public void OperandsAndArgumentsAreEvaluatedLeftToRight()
    {
        FakeEnvironment environment = new("10\n3\n2\n99\n");
        ProgramNode program = MainProgram(Expr(Call(
            "print",
            Call("max", Binary(Call("readInt"), BinaryOperation.Subtract, Call("readInt")), Call("readInt")))));

        Run(program, environment);

        Assert.Equal("7\n", environment.Output);
        Assert.Equal("99", environment.ReadLine());
    }

    [Fact]
    public void StatementsAreExecutedInOrderAndResultsAreDiscarded()
    {
        FakeEnvironment environment = new("5\n");
        ProgramNode program = MainProgram(
            Expr(Call("print", Number(1))),
            Expr(Binary(Number(1), BinaryOperation.Add, Number(2))),
            Expr(Call("readInt")),
            Expr(Call("print", Number(2))));

        Run(program, environment);

        Assert.Equal("1\n2\n", environment.Output);
        Assert.Null(environment.ReadLine());
    }

    [Fact]
    public void RuntimeErrorStopsLaterStatementsAndPreservesOutput()
    {
        FakeEnvironment environment = new("9\n");
        ProgramNode program = MainProgram(
            Expr(Call("print", Number(42))),
            Expr(Binary(Number(1), BinaryOperation.Divide, Number(0))),
            Expr(Call("print", Call("readInt"))));

        InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => Run(program, environment));

        Assert.Equal("Деление на ноль.", error.Message);
        Assert.Equal("42\n", environment.Output);
        Assert.Equal("9", environment.ReadLine());
    }

    [Fact]
    public void ExcessiveNestingReportsErrorInsteadOfStackOverflow()
    {
        Expression expression = Number(1);
        for (int depth = 0; depth < 100000; depth++)
        {
            expression = Minus(expression);
        }

        // Глубокое выражение без имён не требует проверки программы перед выполнением.
        AstEvaluator evaluator = new();
        Exception? error = null;
        Thread thread = new(() => error = Record.Exception(() => evaluator.Execute(MainProgram(Expr(expression)))), 256 * 1024);

        thread.Start();
        thread.Join();

        InvalidOperationException runtimeError = Assert.IsType<InvalidOperationException>(error);
        Assert.Equal(StackShortage, runtimeError.Message);
    }

    [Fact]
    public void InfiniteRecursionReportsErrorInsteadOfStackOverflow()
    {
        FakeEnvironment environment = new();
        ProgramNode program = ProgramOf(
            MainFunction(Expr(Call("print", Call("loop")))),
            Function("loop", [], true, Return(Call("loop"))));
        Exception? error = null;
        Thread thread = new(() => error = Record.Exception(() => Run(program, environment)), 256 * 1024);

        thread.Start();
        thread.Join();

        InvalidOperationException runtimeError = Assert.IsType<InvalidOperationException>(error);
        Assert.Equal(StackShortage, runtimeError.Message);
    }

    /// <summary>
    /// Проверяет программу, чтобы установить атрибуты узлов, и выполняет её.
    /// </summary>
    private static void Run(ProgramNode program, FakeEnvironment environment)
    {
        Builtins builtins = new(environment);
        new SemanticsChecker(builtins.Functions).Check(program);
        new AstEvaluator().Execute(program);
    }
}