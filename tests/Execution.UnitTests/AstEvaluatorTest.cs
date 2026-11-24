using Lait.Ast;
using Lait.Ast.Expressions;
using Lait.Execution;
using Lait.Semantics;
using Lait.TestDoubles;

using Xunit;

using static Lait.TestDoubles.AstFactory;

using ValueType = Lait.Runtime.ValueType;

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
            { Binary(Number(7), BinaryOperation.Remainder, Number(3)), "1" },
            { Binary(Minus(Number(7)), BinaryOperation.Remainder, Number(3)), "-1" },
            { Binary(Number(7), BinaryOperation.Remainder, Minus(Number(3))), "1" },
            { Call("toInt", Binary(Float(7.5), BinaryOperation.Multiply, Float(2.0))), "15" },
        };
    }

    public static TheoryData<Expression, string> FloatExpressionsAreEvaluatedData()
    {
        return new()
        {
            { Float(2.5), "2.500000" },
            { Binary(Float(1.5), BinaryOperation.Add, Float(2.25)), "3.750000" },
            { Binary(Float(1.5), BinaryOperation.Subtract, Float(2.25)), "-0.750000" },
            { Binary(Float(1.5), BinaryOperation.Multiply, Float(2.0)), "3.000000" },
            { Binary(Float(7.0), BinaryOperation.Divide, Float(2.0)), "3.500000" },
            { Binary(Float(1.0), BinaryOperation.Divide, Float(3.0)), "0.333333" },
            { Minus(Float(2.5)), "-2.500000" },
            { Minus(Float(0.0)), "-0.000000" },
            { Call("toFloat", Number(7)), "7.000000" },
        };
    }

    public static TheoryData<Expression, bool> BoolExpressionsAreEvaluatedData()
    {
        return new()
        {
            { Bool(true), true },
            { Binary(Number(1), BinaryOperation.LessThan, Number(2)), true },
            { Binary(Number(2), BinaryOperation.LessThanOrEqual, Number(2)), true },
            { Binary(Number(1), BinaryOperation.GreaterThan, Number(2)), false },
            { Binary(Number(1), BinaryOperation.GreaterThanOrEqual, Number(2)), false },
            { Binary(Number(3), BinaryOperation.Equal, Number(3)), true },
            { Binary(Number(3), BinaryOperation.NotEqual, Number(3)), false },
            { Binary(Float(0.5), BinaryOperation.LessThan, Float(0.25)), false },
            { Binary(Float(0.5), BinaryOperation.GreaterThanOrEqual, Float(0.5)), true },
            { Binary(Float(0.0), BinaryOperation.Equal, Minus(Float(0.0))), true },
            { Binary(Bool(true), BinaryOperation.NotEqual, Bool(false)), true },
            { Binary(Bool(false), BinaryOperation.Equal, Bool(false)), true },
            { Not(Bool(true)), false },
            { Not(Not(Bool(true))), true },
            { Binary(Bool(true), BinaryOperation.And, Bool(false)), false },
            { Binary(Bool(true), BinaryOperation.And, Bool(true)), true },
            { Binary(Bool(false), BinaryOperation.Or, Bool(true)), true },
            { Binary(Bool(false), BinaryOperation.Or, Bool(false)), false },
        };
    }

    public static TheoryData<Expression, string> StatementRuntimeErrorsData()
    {
        const string floatOverflow = "Результат операции выходит за диапазон float.";
        return new()
        {
            { Binary(Number(1), BinaryOperation.Remainder, Number(0)), "Деление на ноль." },
            { Binary(Number(int.MinValue), BinaryOperation.Remainder, Minus(Number(1))), Overflow },
            { Binary(Float(1.0), BinaryOperation.Divide, Float(0.0)), "Деление на ноль." },
            { Binary(Float(0.0), BinaryOperation.Divide, Minus(Float(0.0))), "Деление на ноль." },
            { Binary(Float(1e308), BinaryOperation.Multiply, Float(10.0)), floatOverflow },
            { Binary(Float(1e308), BinaryOperation.Add, Float(1e308)), floatOverflow },
            { Binary(Minus(Float(1e308)), BinaryOperation.Subtract, Float(1e308)), floatOverflow },
            { Binary(Float(1e308), BinaryOperation.Divide, Float(0.1)), floatOverflow },
            { Call("toInt", Float(3e9)), "toInt(): число вне диапазона int." },
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

            // Ветви условной инструкции и цепочка else if.
            {
                ProgramOf(
                    MainFunction(
                        Expr(Call("print", Call("sign", Number(5)))),
                        Expr(Call("print", Call("sign", Minus(Number(5))))),
                        Expr(Call("print", Call("sign", Number(0)))),
                        If(Bool(false), Block(Expr(Call("print", Number(9)))))),
                    Function(
                        "sign",
                        ["x"],
                        true,
                        If(
                            Binary(Name("x"), BinaryOperation.GreaterThan, Number(0)),
                            Block(Return(Number(1))),
                            If(Binary(Name("x"), BinaryOperation.LessThan, Number(0)), Block(Return(Minus(Number(1)))), Block(Return(Number(0))))))),
                "",
                "1\n-1\n0\n"
            },

            // Цикл while с continue и break.
            {
                MainProgram(
                    Var("i", Number(0)),
                    While(
                        Bool(true),
                        Expr(Assign(Name("i"), Binary(Name("i"), BinaryOperation.Add, Number(1)))),
                        If(Binary(Name("i"), BinaryOperation.Equal, Number(2)), Block(Continue())),
                        If(Binary(Name("i"), BinaryOperation.GreaterThan, Number(4)), Block(Break())),
                        Expr(Call("print", Name("i")))),
                    Expr(Call("print", Name("i")))),
                "",
                "1\n3\n4\n5\n"
            },

            // Цикл while с ложным условием не выполняет тело.
            {
                MainProgram(While(Bool(false), Expr(Call("print", Number(1)))), Expr(Call("print", Number(2)))),
                "",
                "2\n"
            },

            // После continue цикл for выполняет шаг; переменная тела создаётся заново на каждом шаге.
            {
                MainProgram(
                    For(
                        Var("i", Number(0)),
                        Binary(Name("i"), BinaryOperation.LessThan, Number(5)),
                        Assign(Name("i"), Binary(Name("i"), BinaryOperation.Add, Number(1))),
                        Var("square", Binary(Name("i"), BinaryOperation.Multiply, Name("i"))),
                        If(Binary(Binary(Name("i"), BinaryOperation.Remainder, Number(2)), BinaryOperation.Equal, Number(0)), Block(Continue())),
                        Expr(Call("print", Name("square"))))),
                "",
                "1\n9\n"
            },

            // Цикл for с инициализатором-выражением и шагом - вызовом функции без результата.
            {
                MainProgram(
                    Var("i", Number(0)),
                    For(
                        Expr(Assign(Name("i"), Number(1))),
                        Binary(Name("i"), BinaryOperation.LessThanOrEqual, Number(3)),
                        Call("print", Name("i")),
                        Expr(Assign(Name("i"), Binary(Name("i"), BinaryOperation.Add, Number(1))))),
                    Expr(Call("print", Name("i")))),
                "",
                "2\n3\n4\n4\n"
            },

            // break завершает только ближайший цикл.
            {
                MainProgram(
                    For(
                        Var("i", Number(1)),
                        Binary(Name("i"), BinaryOperation.LessThanOrEqual, Number(2)),
                        Assign(Name("i"), Binary(Name("i"), BinaryOperation.Add, Number(1))),
                        While(Bool(true), Expr(Call("print", Name("i"))), Break()),
                        Expr(Call("print", Binary(Name("i"), BinaryOperation.Multiply, Number(10)))))),
                "",
                "1\n10\n2\n20\n"
            },

            // return внутри цикла завершает функцию.
            {
                ProgramOf(
                    MainFunction(Expr(Call("print", Call("firstDivisor", Number(91))))),
                    Function(
                        "firstDivisor",
                        ["n"],
                        true,
                        For(
                            Var("d", Number(2)),
                            Binary(Name("d"), BinaryOperation.LessThan, Name("n")),
                            Assign(Name("d"), Binary(Name("d"), BinaryOperation.Add, Number(1))),
                            If(Binary(Binary(Name("n"), BinaryOperation.Remainder, Name("d")), BinaryOperation.Equal, Number(0)), Block(Return(Name("d"))))),
                        Return(Name("n")))),
                "",
                "7\n"
            },

            // Логические операции вычисляются по короткой схеме.
            {
                ProgramOf(
                    MainFunction(
                        If(Binary(Bool(false), BinaryOperation.And, Call("touch", Bool(true))), Block(Expr(Call("print", Number(2))))),
                        If(Binary(Bool(true), BinaryOperation.Or, Call("touch", Bool(false))), Block(Expr(Call("print", Number(3))))),
                        If(Binary(Bool(true), BinaryOperation.And, Call("touch", Bool(true))), Block(Expr(Call("print", Number(4))))),
                        If(Binary(Bool(false), BinaryOperation.Or, Call("touch", Bool(false))), Block(), Block(Expr(Call("print", Number(5)))))),
                    TypedFunction(
                        "touch",
                        [Parameter("result", ValueType.Bool)],
                        ValueType.Bool,
                        Expr(Call("print", Number(1))),
                        Return(Name("result")))),
                "",
                "3\n1\n4\n1\n5\n"
            },

            // Переменные float и bool, ввод и вывод float.
            {
                MainProgram(
                    Var(ValueType.Float, "x", Call("readFloat")),
                    Var(ValueType.Bool, "negative", Binary(Name("x"), BinaryOperation.LessThan, Float(0.0))),
                    If(Name("negative"), Block(Expr(Assign(Name("x"), Minus(Name("x")))))),
                    Expr(Call("printFloat", Name("x")))),
                "-2.5\n",
                "2.500000\n"
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
    [MemberData(nameof(FloatExpressionsAreEvaluatedData))]
    public void FloatExpressionsAreEvaluated(Expression expression, string expected)
    {
        FakeEnvironment environment = new();

        Run(MainProgram(Expr(Call("printFloat", expression))), environment);

        Assert.Equal(expected + "\n", environment.Output);
    }

    [Theory]
    [MemberData(nameof(BoolExpressionsAreEvaluatedData))]
    public void BoolExpressionsAreEvaluated(Expression expression, bool expected)
    {
        FakeEnvironment environment = new();
        ProgramNode program = MainProgram(If(expression, Block(Expr(Call("print", Number(1)))), Block(Expr(Call("print", Number(0))))));

        Run(program, environment);

        Assert.Equal(expected ? "1\n" : "0\n", environment.Output);
    }

    [Theory]
    [MemberData(nameof(StatementRuntimeErrorsData))]
    public void StatementRuntimeErrorsAreReported(Expression expression, string expectedMessage)
    {
        FakeEnvironment environment = new();

        InvalidOperationException error = Assert.Throws<InvalidOperationException>(
            () => Run(MainProgram(Expr(expression)), environment));

        Assert.Equal(expectedMessage, error.Message);
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
    public void ExecuteCanBeRepeatedAfterSuccessAndRuntimeError()
    {
        FakeEnvironment environment = new("2\n0\n4\n");
        ProgramNode program = ProgramOf(
            MainFunction(
                Var("divisor", Call("getDivisor")),
                Expr(Call("print", Binary(Number(10), BinaryOperation.Divide, Name("divisor")))),
                Expr(Call("print", Number(20)))),
            Function("getDivisor", [], true, Return(Call("readInt"))));
        Builtins builtins = new(environment);
        new SemanticsChecker(builtins.Functions).Check(program);
        AstEvaluator evaluator = new();

        evaluator.Execute(program);
        Exception? error = Record.Exception(() => evaluator.Execute(program));
        evaluator.Execute(program);

        InvalidOperationException runtimeError = Assert.IsType<InvalidOperationException>(error);
        Assert.Equal("Деление на ноль.", runtimeError.Message);
        Assert.Equal("5\n20\n2\n20\n", environment.Output);
        Assert.Null(environment.ReadLine());
    }

    [Fact]
    public void ExecuteClearsStateLeftByDirectAstVisit()
    {
        FakeEnvironment environment = new();
        ProgramNode program = MainProgram(
            Expr(Call("print", Number(1))),
            Expr(Call("print", Number(2))));
        Builtins builtins = new(environment);
        new SemanticsChecker(builtins.Functions).Check(program);
        AstEvaluator evaluator = new();
        Return(Number(42)).Accept(evaluator);

        evaluator.Execute(program);

        Assert.Equal("1\n2\n", environment.Output);
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