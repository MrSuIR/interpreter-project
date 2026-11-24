using System.Runtime.CompilerServices;

using Lait.Ast;
using Lait.Ast.Declarations;
using Lait.Ast.Expressions;
using Lait.Ast.Statements;
using Lait.Runtime;

namespace Lait.Semantics.Passes;

/// <summary>
/// Проверяет соблюдение контекстно-зависимых правил языка.
/// </summary>
/// <remarks>
/// Эти правила не проверяются при синтаксическом анализе, поскольку синтаксический анализатор
///  разбирает контекстно-свободную грамматику.
/// </remarks>
public sealed class CheckContextSensitiveRulesPass : AbstractPass
{
    /// <summary>
    /// Стек контекстов инструкций; используется для проверки break и continue.
    /// </summary>
    private readonly Stack<StatementContext> _statementContextStack = [];

    /// <summary>
    /// Функция, тело которой проверяется сейчас.
    /// </summary>
    private FunctionDeclaration? _function;

    private enum StatementContext
    {
        Default,
        InsideLoop,
    }

    public override void Visit(ProgramNode program)
    {
        // Повторные имена функций уже отклонены при разрешении имён.
        FunctionDeclaration main = program.Functions.FirstOrDefault(function => function.Name == "main")
            ?? throw new FormatException("В программе нет функции 'main'.");
        if (main.Parameters.Count != 0 || main.ReturnsValue)
        {
            throw new FormatException("Функция 'main' должна быть объявлена как func main(): void.");
        }

        base.Visit(program);
    }

    public override void Visit(FunctionDeclaration declaration)
    {
        // Тело функции находится вне циклов, даже если функцию вызывают из цикла.
        _function = declaration;
        _statementContextStack.Push(StatementContext.Default);
        try
        {
            base.Visit(declaration);
        }
        finally
        {
            _statementContextStack.Pop();
        }

        if (declaration.ReturnsValue && !AlwaysReturns(declaration.Body))
        {
            throw new FormatException($"Не каждый путь выполнения функции '{declaration.Name}' заканчивается return.");
        }
    }

    public override void Visit(ReturnStatement statement)
    {
        FunctionDeclaration function = _function ?? throw new InvalidOperationException("Инструкция return вне функции.");
        if (statement.Value is not null && !function.ReturnsValue)
        {
            throw new FormatException($"Функция '{function.Name}' с результатом void не может возвращать значение.");
        }

        if (statement.Value is null && function.ReturnsValue)
        {
            throw new FormatException(
                $"Функция '{function.Name}' с результатом {function.ResultType.GetName()} должна возвращать значение.");
        }

        base.Visit(statement);
    }

    public override void Visit(WhileLoopStatement statement)
    {
        VisitInsideLoop(() => base.Visit(statement));
    }

    public override void Visit(ForLoopStatement statement)
    {
        VisitInsideLoop(() => base.Visit(statement));
    }

    public override void Visit(BreakLoopStatement statement)
    {
        CheckInsideLoop("break");
    }

    public override void Visit(ContinueLoopStatement statement)
    {
        CheckInsideLoop("continue");
    }

    public override void Visit(ExpressionStatement statement)
    {
        // Вызов функции без результата допускается только как всё выражение инструкции.
        if (statement.Expression is FunctionCallExpression call)
        {
            CheckCall(call, requiresResult: false);
        }
        else
        {
            base.Visit(statement);
        }
    }

    public override void Visit(FunctionCallExpression expression)
    {
        CheckCall(expression, requiresResult: true);
    }

    public override void Visit(AssignmentExpression expression)
    {
        if (expression.Left is not VariableAccessExpression)
        {
            throw new FormatException("Слева от присваивания должна стоять переменная.");
        }

        base.Visit(expression);
    }

    /// <summary>
    /// Возвращает истину, если каждый путь выполнения инструкции заканчивается return.
    /// Циклы не учитываются: их тело может не выполниться ни разу.
    /// </summary>
    private static bool AlwaysReturns(Statement statement)
    {
        RuntimeHelpers.EnsureSufficientExecutionStack();
        return statement switch
        {
            ReturnStatement => true,
            BlockStatement block => block.Statements.Any(AlwaysReturns),
            IfElseStatement { ElseBranch: not null } ifElse =>
                AlwaysReturns(ifElse.ThenBranch) && AlwaysReturns(ifElse.ElseBranch),
            _ => false,
        };
    }

    private void VisitInsideLoop(Action visit)
    {
        _statementContextStack.Push(StatementContext.InsideLoop);
        try
        {
            visit();
        }
        finally
        {
            _statementContextStack.Pop();
        }
    }

    /// <summary>
    /// Проверяет, что break или continue находится внутри цикла той же функции.
    /// </summary>
    private void CheckInsideLoop(string keyword)
    {
        if (_statementContextStack.Peek() != StatementContext.InsideLoop)
        {
            throw new FormatException($"Инструкция {keyword} допускается только внутри цикла.");
        }
    }

    private void CheckCall(FunctionCallExpression call, bool requiresResult)
    {
        RuntimeHelpers.EnsureSufficientExecutionStack();
        AbstractFunctionDeclaration function = call.Function;
        if (function is FunctionDeclaration { Name: "main" })
        {
            throw new FormatException("Функцию 'main' нельзя вызывать.");
        }

        if (requiresResult && !function.ReturnsValue)
        {
            throw new FormatException(
                $"Функция '{function.Name}' не возвращает значения и допускается только как инструкция.");
        }

        if (call.Arguments.Count != function.Parameters.Count)
        {
            throw new FormatException(
                $"Функция '{function.Name}' ожидает аргументов: {function.Parameters.Count}; получено: {call.Arguments.Count}.");
        }

        foreach (Expression argument in call.Arguments)
        {
            argument.Accept(this);
        }
    }
}