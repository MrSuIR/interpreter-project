using Lait.Ast;
using Lait.Ast.Declarations;
using Lait.Ast.Expressions;
using Lait.Ast.Statements;
using Lait.Semantics;
using Lait.TestDoubles;

using Xunit;

using static Lait.TestDoubles.AstFactory;

namespace Lait.Semantics.UnitTests;

public class SemanticsCheckerTest
{
    private const string AlreadyDeclaredA = "Имя 'a' уже объявлено в этой области видимости.";
    private const string NotDeclaredX = "Переменная 'x' не объявлена.";
    private const string MustEndWithReturn = "Функция 'f' с результатом int должна заканчиваться return с выражением.";
    private const string MainSignature = "Функция 'main' должна быть объявлена как func main(): void.";
    private const string AssignmentTarget = "Слева от присваивания должна стоять переменная.";

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
            { ProgramOf(MainFunction(), Function("f", [], true, Block(Return(Number(1))))), MustEndWithReturn },
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

    [Fact]
    public void VariableNamesResolveToNearestVisibleDeclaration()
    {
        SemanticsChecker checker = new(Builtins);
        VariableDeclaration outer = new("x", Number(1));
        VariableAccessExpression inInitializer = Name("x");
        VariableDeclaration inner = new("x", Binary(inInitializer, BinaryOperation.Add, Number(1)));
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
        ParameterDeclaration parameter = new("a");
        VariableAccessExpression parameterAccess = Name("a");
        FunctionDeclaration function = new("f", [parameter], returnsValue: true, new BlockStatement([Return(parameterAccess)]));
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