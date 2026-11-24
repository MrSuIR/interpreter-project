using Lait.Ast;
using Lait.Ast.Declarations;
using Lait.Ast.Expressions;
using Lait.Ast.Statements;
using Lait.Runtime;
using Lait.Semantics;
using Lait.TestDoubles;

using Xunit;

using static Lait.TestDoubles.AstFactory;

using ValueType = Lait.Runtime.ValueType;

namespace Lait.Semantics.UnitTests;

public class SemanticsCheckerTest
{
    private const string AlreadyDeclaredA = "Имя 'a' уже объявлено в этой области видимости.";
    private const string NotDeclaredX = "Переменная 'x' не объявлена.";
    private const string MustEndWithReturn = "Не каждый путь выполнения функции 'f' заканчивается return.";
    private const string MainSignature = "Функция 'main' должна быть объявлена как func main(): void.";
    private const string AssignmentTarget = "Слева от присваивания должна стоять переменная.";

    /// <summary>
    /// Встроенные функции с сигнатурами из спецификации; реализации не используются при проверке.
    /// </summary>
    private static IReadOnlyDictionary<string, BuiltinFunction> Builtins { get; } =
        new BuiltinFunction[]
        {
            new("print", [Parameter("value", ValueType.Int)], ValueType.Void, _ => Value.Void),
            new("printFloat", [Parameter("value", ValueType.Float)], ValueType.Void, _ => Value.Void),
            new("readInt", [], ValueType.Int, _ => Value.Void),
            new("readFloat", [], ValueType.Float, _ => Value.Void),
            new("abs", [Parameter("value", ValueType.Int)], ValueType.Int, _ => Value.Void),
            new("min", [Parameter("left", ValueType.Int), Parameter("right", ValueType.Int)], ValueType.Int, _ => Value.Void),
            new("max", [Parameter("left", ValueType.Int), Parameter("right", ValueType.Int)], ValueType.Int, _ => Value.Void),
            new("toFloat", [Parameter("value", ValueType.Int)], ValueType.Float, _ => Value.Void),
            new("toInt", [Parameter("value", ValueType.Float)], ValueType.Int, _ => Value.Void),
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
            { Call("main"), "Функцию 'main' нельзя вызывать." },
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

    public static TheoryData<ProgramNode> ValidProgramsAreAcceptedData()
    {
        return new()
        {
            // Пустая функция main.
            MainProgram(),

            // Объявление, чтение и присваивание переменной.
            MainProgram(
                Var("x", Number(1)),
                Expr(Assign(Name("x"), Binary(Name("x"), BinaryOperation.Add, Number(1)))),
                Expr(Call("print", Name("x")))),

            // Вложенное присваивание и присваивание внутри аргумента.
            MainProgram(
                Var("a", Number(0)),
                Var("b", Number(0)),
                Expr(Assign(Name("a"), Assign(Name("b"), Number(1)))),
                Expr(Call("print", Assign(Name("a"), Number(2))))),

            // Функции объявлены в любом порядке и вызывают друг друга.
            ProgramOf(
                MainFunction(Expr(Call("show", Call("twice", Number(2))))),
                Function("twice", ["x"], true, Return(Binary(Name("x"), BinaryOperation.Multiply, Number(2)))),
                Function("show", ["value"], false, Expr(Call("print", Name("value"))), Return())),

            // Рекурсия и присваивание параметра.
            ProgramOf(
                MainFunction(Expr(Call("print", Call("f", Number(1))))),
                Function("f", ["n"], true, Expr(Assign(Name("n"), Number(2))), Return(Call("f", Name("n"))))),

            // Перекрытие во вложенном блоке и повторное объявление после блока.
            MainProgram(
                Var("x", Number(1)),
                Block(Var("x", Binary(Name("x"), BinaryOperation.Add, Number(1))), Expr(Call("print", Name("x")))),
                Block(Var("y", Number(1))),
                Var("y", Number(2))),

            // Параметр можно перекрыть во вложенном блоке.
            ProgramOf(
                MainFunction(Expr(Call("f", Number(1)))),
                Function("f", ["a"], false, Block(Var("a", Number(2))))),

            // Имена переменных не конфликтуют с именами функций.
            MainProgram(
                Var("max", Call("max", Number(1), Number(2))),
                Var("print", Number(3)),
                Expr(Call("print", Name("print")))),

            // Инструкции после return допустимы.
            ProgramOf(
                MainFunction(Expr(Call("print", Call("f")))),
                Function("f", [], true, Return(Number(1)), Expr(Call("print", Number(2))), Return(Number(2)))),

            // return во вложенном блоке завершает каждый путь выполнения.
            ProgramOf(MainFunction(), Function("f", [], true, Block(Return(Number(1))))),
        };
    }

    public static TheoryData<ProgramNode> ValidTypedProgramsAreAcceptedData()
    {
        return new()
        {
            // Переменные float и bool, арифметика, сравнения и логические операции.
            MainProgram(
                Var(ValueType.Float, "x", Float(1.5)),
                Var(ValueType.Bool, "b", Binary(Binary(Name("x"), BinaryOperation.LessThan, Float(2.0)), BinaryOperation.And, Not(Bool(false)))),
                Expr(Assign(Name("x"), Minus(Name("x")))),
                Expr(Assign(Name("b"), Binary(Name("b"), BinaryOperation.Or, Binary(Name("x"), BinaryOperation.Equal, Float(0.0))))),
                Expr(Call("printFloat", Binary(Name("x"), BinaryOperation.Divide, Float(2.0))))),

            // Остаток от деления int и сравнение значений bool.
            MainProgram(
                Var(ValueType.Bool, "same", Binary(Bool(true), BinaryOperation.Equal, Binary(Binary(Number(7), BinaryOperation.Remainder, Number(3)), BinaryOperation.NotEqual, Number(0))))),

            // Явные преобразования между int и float.
            MainProgram(Expr(Call("print", Call("toInt", Binary(Call("toFloat", Call("readInt")), BinaryOperation.Multiply, Call("readFloat")))))),

            // Цикл while с break и continue внутри условной инструкции.
            MainProgram(
                Var("i", Number(0)),
                While(
                    Bool(true),
                    If(Binary(Name("i"), BinaryOperation.GreaterThan, Number(3)), Block(Break())),
                    Expr(Assign(Name("i"), Binary(Name("i"), BinaryOperation.Add, Number(1)))),
                    Continue())),

            // Цикл for: инициализатор-выражение, шаг - вызов функции без результата.
            MainProgram(
                Var("i", Number(0)),
                For(
                    Expr(Assign(Name("i"), Number(1))),
                    Binary(Name("i"), BinaryOperation.LessThan, Number(3)),
                    Call("print", Name("i")),
                    Expr(Assign(Name("i"), Binary(Name("i"), BinaryOperation.Add, Number(1)))))),

            // Переменную цикла for можно перекрыть в теле и объявить снова после цикла.
            MainProgram(
                For(
                    Var("i", Number(0)),
                    Binary(Name("i"), BinaryOperation.LessThan, Number(1)),
                    Assign(Name("i"), Number(1)),
                    Var("i", Number(5)),
                    If(Bool(true), Block(Continue()), Block(Break()))),
                Var("i", Number(2))),

            // Каждый путь выполнения цепочки else if заканчивается return.
            ProgramOf(
                MainFunction(Expr(Call("print", Call("sign", Number(1))))),
                TypedFunction(
                    "sign",
                    [Parameter("x", ValueType.Int)],
                    ValueType.Int,
                    If(
                        Binary(Name("x"), BinaryOperation.GreaterThan, Number(0)),
                        Block(Return(Number(1))),
                        If(Binary(Name("x"), BinaryOperation.LessThan, Number(0)), Block(Return(Minus(Number(1)))), Block(Return(Number(0))))))),

            // Рекурсивная функция с результатом bool; return внутри цикла и после него.
            ProgramOf(
                MainFunction(If(Call("isEven", Number(4)), Block(Expr(Call("print", Call("first")))))),
                TypedFunction(
                    "isEven",
                    [Parameter("n", ValueType.Int)],
                    ValueType.Bool,
                    If(Binary(Name("n"), BinaryOperation.Equal, Number(0)), Block(Return(Bool(true)))),
                    Return(Not(Call("isEven", Binary(Name("n"), BinaryOperation.Subtract, Number(1)))))),
                Function("first", [], true, While(Bool(true), Return(Number(1))), Return(Number(0)))),
        };
    }

    public static TheoryData<Expression, string> InvalidTypesAreRejectedData()
    {
        return new()
        {
            { Call("print", Float(1.5)), "Аргумент 1 функции 'print' должен иметь тип int; получен float." },
            { Call("printFloat", Number(1)), "Аргумент 1 функции 'printFloat' должен иметь тип float; получен int." },
            { Call("toInt", Number(1)), "Аргумент 1 функции 'toInt' должен иметь тип float; получен int." },
            { Call("toFloat", Float(1.5)), "Аргумент 1 функции 'toFloat' должен иметь тип int; получен float." },
            { Call("max", Number(1), Bool(true)), "Аргумент 2 функции 'max' должен иметь тип int; получен bool." },
            { Binary(Number(1), BinaryOperation.Add, Float(1.5)), "Операция '+' недопустима для типов int и float." },
            { Binary(Float(1.5), BinaryOperation.Remainder, Float(2.0)), "Операция '%' недопустима для типов float и float." },
            { Binary(Bool(true), BinaryOperation.Multiply, Bool(false)), "Операция '*' недопустима для типов bool и bool." },
            { Binary(Bool(true), BinaryOperation.LessThan, Bool(false)), "Операция '<' недопустима для типов bool и bool." },
            { Binary(Number(1), BinaryOperation.Equal, Float(1.0)), "Операция '==' недопустима для типов int и float." },
            { Binary(Number(1), BinaryOperation.And, Bool(true)), "Операция '&&' недопустима для типов int и bool." },
            { Binary(Number(1), BinaryOperation.Or, Number(0)), "Операция '||' недопустима для типов int и int." },
            { Minus(Bool(true)), "Операция '-' недопустима для типа bool." },
            { Not(Number(1)), "Операция '!' требует операнд bool; получен int." },
            { Call("print", Binary(Number(1), BinaryOperation.LessThan, Number(2))), "Аргумент 1 функции 'print' должен иметь тип int; получен bool." },
        };
    }

    public static TheoryData<ProgramNode, string> InvalidTypedProgramsAreRejectedData()
    {
        const string breakOutsideLoop = "Инструкция break допускается только внутри цикла.";
        return new()
        {
            // Тип инициализатора и присваиваемого значения совпадает с типом переменной.
            { MainProgram(Var("x", Float(1.5))), "Инициализатор переменной 'x' должен иметь тип int; получен float." },
            { MainProgram(Var(ValueType.Float, "x", Number(1))), "Инициализатор переменной 'x' должен иметь тип float; получен int." },
            { MainProgram(Var(ValueType.Bool, "x", Number(1))), "Инициализатор переменной 'x' должен иметь тип bool; получен int." },
            {
                MainProgram(Var("x", Number(0)), Expr(Assign(Name("x"), Bool(true)))),
                "Значение, присваиваемое переменной 'x', должно иметь тип int; получен bool."
            },

            // Тип аргумента пользовательской функции совпадает с типом параметра.
            {
                ProgramOf(
                    MainFunction(Expr(Call("f", Number(1), Bool(true)))),
                    TypedFunction("f", [Parameter("a", ValueType.Int), Parameter("b", ValueType.Float)], ValueType.Void)),
                "Аргумент 2 функции 'f' должен иметь тип float; получен bool."
            },

            // Условия ветвлений и циклов имеют тип bool.
            { MainProgram(If(Number(1), Block())), "Условие инструкции if должно иметь тип bool; получен int." },
            { MainProgram(While(Number(0))), "Условие цикла while должно иметь тип bool; получен int." },
            {
                MainProgram(For(Var("i", Number(0)), Name("i"), Assign(Name("i"), Number(1)))),
                "Условие цикла for должно иметь тип bool; получен int."
            },
            {
                MainProgram(While(Call("print", Number(1)))),
                "Функция 'print' не возвращает значения и допускается только как инструкция."
            },

            // Тип возвращаемого значения совпадает с типом результата функции.
            {
                ProgramOf(MainFunction(), TypedFunction("f", [], ValueType.Bool, Return(Number(1)))),
                "Функция 'f' должна возвращать значение типа bool; получен int."
            },
            {
                ProgramOf(MainFunction(), TypedFunction("f", [], ValueType.Float, Return())),
                "Функция 'f' с результатом float должна возвращать значение."
            },

            // break и continue допускаются только внутри цикла текущей функции.
            { MainProgram(Break()), breakOutsideLoop },
            { MainProgram(If(Bool(true), Block(Continue()))), "Инструкция continue допускается только внутри цикла." },
            {
                ProgramOf(MainFunction(While(Bool(true), Expr(Call("f")))), Function("f", [], false, Break())),
                breakOutsideLoop
            },
            { MainProgram(While(Bool(true)), Break()), breakOutsideLoop },

            // Переменная цикла for не видна после цикла.
            {
                MainProgram(For(Var("i", Number(0)), Binary(Name("i"), BinaryOperation.LessThan, Number(1)), Assign(Name("i"), Number(1))), Expr(Name("i"))),
                "Переменная 'i' не объявлена."
            },

            // Каждый путь выполнения функции с результатом заканчивается return; циклы не учитываются.
            { ProgramOf(MainFunction(), Function("f", ["x"], true, If(Binary(Name("x"), BinaryOperation.GreaterThan, Number(0)), Block(Return(Name("x")))))), MustEndWithReturn },
            { ProgramOf(MainFunction(), Function("f", [], true, If(Bool(true), Block(Return(Number(1))), Block()))), MustEndWithReturn },
            { ProgramOf(MainFunction(), Function("f", [], true, While(Bool(true), Return(Number(1))))), MustEndWithReturn },
            { ProgramOf(MainFunction(), Function("f", [], true, For(Var("i", Number(0)), Bool(true), Number(0), Return(Number(1))))), MustEndWithReturn },
        };
    }

    public static TheoryData<Expression, ValueType> ExpressionTypesAreResolvedData()
    {
        return new()
        {
            { Binary(Number(1), BinaryOperation.Add, Number(2)), ValueType.Int },
            { Binary(Number(7), BinaryOperation.Remainder, Number(2)), ValueType.Int },
            { Binary(Float(1.5), BinaryOperation.Divide, Float(2.0)), ValueType.Float },
            { Minus(Float(1.5)), ValueType.Float },
            { Binary(Float(1.5), BinaryOperation.GreaterThanOrEqual, Float(2.0)), ValueType.Bool },
            { Binary(Bool(true), BinaryOperation.NotEqual, Bool(false)), ValueType.Bool },
            { Binary(Bool(true), BinaryOperation.Or, Not(Bool(false))), ValueType.Bool },
            { Call("readFloat"), ValueType.Float },
            { Call("toInt", Float(1.5)), ValueType.Int },
            { Call("print", Number(1)), ValueType.Void },
        };
    }

    public static TheoryData<ProgramNode, string> InvalidProgramsAreRejectedData()
    {
        return new()
        {
            // Функция main.
            { ProgramOf(Function("other", [], false)), "В программе нет функции 'main'." },
            { ProgramOf(Function("main", ["x"], false)), MainSignature },
            { ProgramOf(Function("main", [], true, Return(Number(0)))), MainSignature },
            { ProgramOf(MainFunction(), MainFunction()), "Функция 'main' уже объявлена." },

            // Имена функций.
            { ProgramOf(MainFunction(), Function("f", [], false), Function("f", [], false)), "Функция 'f' уже объявлена." },
            { ProgramOf(MainFunction(), Function("abs", ["x"], true, Return(Name("x")))), "Функция 'abs' совпадает со встроенной функцией." },
            { MainProgram(Var("x", Number(1)), Expr(Call("x"))), "Неизвестная функция 'x'." },

            // Повтор имени в одной области видимости.
            { ProgramOf(MainFunction(), Function("f", ["a", "a"], false)), AlreadyDeclaredA },
            { ProgramOf(MainFunction(), Function("f", ["a"], false, Var("a", Number(1)))), AlreadyDeclaredA },
            { MainProgram(Var("a", Number(1)), Var("a", Number(2))), AlreadyDeclaredA },

            // Переменная должна быть объявлена и видна.
            { MainProgram(Expr(Call("print", Name("x")))), NotDeclaredX },
            { MainProgram(Expr(Name("x")), Var("x", Number(1))), NotDeclaredX },
            { MainProgram(Var("x", Name("x"))), NotDeclaredX },
            { MainProgram(Block(Var("x", Number(1))), Expr(Name("x"))), NotDeclaredX },
            { ProgramOf(Function("f", [], false, Var("x", Number(1))), MainFunction(Expr(Name("x")))), NotDeclaredX },
            { ProgramOf(MainFunction(Var("x", Number(1))), Function("f", [], true, Return(Name("x")))), NotDeclaredX },

            // Вызов пользовательской функции.
            {
                ProgramOf(MainFunction(Expr(Call("print", Call("f")))), Function("f", ["a"], true, Return(Name("a")))),
                "Функция 'f' ожидает аргументов: 1; получено: 0."
            },
            {
                ProgramOf(MainFunction(Expr(Call("print", Call("f")))), Function("f", [], false)),
                "Функция 'f' не возвращает значения и допускается только как инструкция."
            },

            // Инструкция return.
            { ProgramOf(MainFunction(), Function("f", [], true, Expr(Call("print", Number(1))))), MustEndWithReturn },
            { ProgramOf(MainFunction(), Function("f", [], true)), MustEndWithReturn },
            { ProgramOf(MainFunction(), Function("f", [], true, Return())), "Функция 'f' с результатом int должна возвращать значение." },
            { MainProgram(Return(Number(1))), "Функция 'main' с результатом void не может возвращать значение." },

            // Левая часть присваивания.
            { MainProgram(Expr(Assign(Number(1), Number(2)))), AssignmentTarget },
            { MainProgram(Var("x", Number(1)), Expr(Assign(Call("abs", Name("x")), Number(2)))), AssignmentTarget },
        };
    }

    [Theory]
    [MemberData(nameof(ValidStatementsAreAcceptedData))]
    public void ValidStatementsAreAccepted(Expression expression)
    {
        SemanticsChecker checker = new(Builtins);

        Exception? error = Record.Exception(() => checker.Check(MainProgram(Expr(expression))));

        Assert.Null(error);
    }

    [Theory]
    [MemberData(nameof(InvalidCallsAreRejectedData))]
    public void InvalidCallsAreRejected(Expression expression, string expectedMessage)
    {
        SemanticsChecker checker = new(Builtins);

        FormatException error = Assert.Throws<FormatException>(() => checker.Check(MainProgram(Expr(expression))));

        Assert.Equal(expectedMessage, error.Message);
    }

    [Theory]
    [MemberData(nameof(ValidProgramsAreAcceptedData))]
    public void ValidProgramsAreAccepted(ProgramNode program)
    {
        SemanticsChecker checker = new(Builtins);

        Exception? error = Record.Exception(() => checker.Check(program));

        Assert.Null(error);
    }

    [Theory]
    [MemberData(nameof(InvalidProgramsAreRejectedData))]
    public void InvalidProgramsAreRejected(ProgramNode program, string expectedMessage)
    {
        SemanticsChecker checker = new(Builtins);

        FormatException error = Assert.Throws<FormatException>(() => checker.Check(program));

        Assert.Equal(expectedMessage, error.Message);
    }

    [Theory]
    [MemberData(nameof(ValidTypedProgramsAreAcceptedData))]
    public void ValidTypedProgramsAreAccepted(ProgramNode program)
    {
        SemanticsChecker checker = new(Builtins);

        Exception? error = Record.Exception(() => checker.Check(program));

        Assert.Null(error);
    }

    [Theory]
    [MemberData(nameof(InvalidTypesAreRejectedData))]
    public void InvalidTypesAreRejected(Expression expression, string expectedMessage)
    {
        SemanticsChecker checker = new(Builtins);

        FormatException error = Assert.Throws<FormatException>(() => checker.Check(MainProgram(Expr(expression))));

        Assert.Equal(expectedMessage, error.Message);
    }

    [Theory]
    [MemberData(nameof(InvalidTypedProgramsAreRejectedData))]
    public void InvalidTypedProgramsAreRejected(ProgramNode program, string expectedMessage)
    {
        SemanticsChecker checker = new(Builtins);

        FormatException error = Assert.Throws<FormatException>(() => checker.Check(program));

        Assert.Equal(expectedMessage, error.Message);
    }

    [Theory]
    [MemberData(nameof(ExpressionTypesAreResolvedData))]
    public void ExpressionTypesAreResolved(Expression expression, ValueType expectedType)
    {
        SemanticsChecker checker = new(Builtins);

        checker.Check(MainProgram(Expr(expression)));

        Assert.Equal(expectedType, expression.ResultType);
    }

    [Fact]
    public void VariableTypesAreResolvedFromDeclarations()
    {
        SemanticsChecker checker = new(Builtins);
        VariableAccessExpression floatAccess = Name("x");
        VariableAccessExpression boolAccess = Name("b");
        AssignmentExpression assignment = Assign(Name("x"), floatAccess);
        ProgramNode program = MainProgram(
            Var(ValueType.Float, "x", Float(1.5)),
            Var(ValueType.Bool, "b", Bool(true)),
            Expr(assignment),
            If(boolAccess, Block()));

        checker.Check(program);

        Assert.Equal(ValueType.Float, floatAccess.ResultType);
        Assert.Equal(ValueType.Bool, boolAccess.ResultType);
        Assert.Equal(ValueType.Float, assignment.ResultType);
    }

    [Fact]
    public void ForLoopVariableResolvesInsideLoop()
    {
        SemanticsChecker checker = new(Builtins);
        VariableDeclaration counter = new("i", ValueType.Int, Number(0));
        VariableAccessExpression inCondition = Name("i");
        VariableAccessExpression inStep = Name("i");
        VariableAccessExpression inBody = Name("i");
        ProgramNode program = MainProgram(
            For(
                new VariableDeclarationStatement(counter),
                Binary(inCondition, BinaryOperation.LessThan, Number(3)),
                Assign(Name("i"), Binary(inStep, BinaryOperation.Add, Number(1))),
                Expr(Call("print", inBody))));

        checker.Check(program);

        Assert.Same(counter, inCondition.Variable);
        Assert.Same(counter, inStep.Variable);
        Assert.Same(counter, inBody.Variable);
    }

    [Fact]
    public void VariableNamesResolveToNearestVisibleDeclaration()
    {
        SemanticsChecker checker = new(Builtins);
        VariableDeclaration outer = new("x", ValueType.Int, Number(1));
        VariableAccessExpression inInitializer = Name("x");
        VariableDeclaration inner = new("x", ValueType.Int, Binary(inInitializer, BinaryOperation.Add, Number(1)));
        VariableAccessExpression inBlock = Name("x");
        VariableAccessExpression afterBlock = Name("x");
        ProgramNode program = MainProgram(
            new VariableDeclarationStatement(outer),
            Block(new VariableDeclarationStatement(inner), Expr(Call("print", inBlock))),
            Expr(Call("print", afterBlock)));

        checker.Check(program);

        Assert.Same(outer, inInitializer.Variable);
        Assert.Same(inner, inBlock.Variable);
        Assert.Same(outer, afterBlock.Variable);
    }

    [Fact]
    public void CallsAndParametersResolveToDeclarations()
    {
        SemanticsChecker checker = new(Builtins);
        ParameterDeclaration parameter = new("a", ValueType.Int);
        VariableAccessExpression parameterAccess = Name("a");
        FunctionDeclaration function = new("f", [parameter], ValueType.Int, new BlockStatement([Return(parameterAccess)]));
        FunctionCallExpression userCall = Call("f", Number(1));
        FunctionCallExpression builtinCall = Call("abs", userCall);
        ProgramNode program = ProgramOf(MainFunction(Expr(Call("print", builtinCall))), function);

        checker.Check(program);

        Assert.Same(function, userCall.Function);
        Assert.Same(Builtins["abs"], builtinCall.Function);
        Assert.Same(parameter, parameterAccess.Variable);
    }

    [Fact]
    public void ErrorInLaterStatementRejectsWholeProgram()
    {
        SemanticsChecker checker = new(Builtins);
        ProgramNode program = MainProgram(Expr(Call("print", Number(1))), Expr(Call("abs")));

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
        Thread thread = new(() => error = Record.Exception(() => checker.Check(MainProgram(Expr(expression)))), 256 * 1024);

        thread.Start();
        thread.Join();

        InvalidOperationException stackError = Assert.IsType<InvalidOperationException>(error);
        Assert.Equal("Недостаточно стека для проверки вложенного выражения.", stackError.Message);
    }
}