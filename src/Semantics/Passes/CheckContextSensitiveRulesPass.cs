using System.Runtime.CompilerServices;

using Lait.Ast;
using Lait.Ast.Declarations;
using Lait.Ast.Expressions;
using Lait.Ast.Statements;

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
    /// Функция, тело которой проверяется сейчас.
    /// </summary>
    private FunctionDeclaration? _function;

    public override void Visit(ProgramNode program)
    {
        // Повторные имена функций уже отклонены при разрешении имён.
        FunctionDeclaration main = program.Functions.FirstOrDefault(function => function.Name == "main")
            ?? throw new FormatException("В программе нет функции 'main'.");
        if (main.ParameterCount != 0 || main.ReturnsValue)
        {
            throw new FormatException("Функция 'main' должна быть объявлена как func main(): void.");
        }

        base.Visit(program);
    }

    public override void Visit(FunctionDeclaration declaration)
    {
        _function = declaration;
        base.Visit(declaration);

        if (declaration.ReturnsValue && declaration.Body.Statements.LastOrDefault() is not ReturnStatement { Value: not null })
        {
            throw new FormatException(
                $"Функция '{declaration.Name}' с результатом int должна заканчиваться return с выражением.");
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
            throw new FormatException($"Функция '{function.Name}' с результатом int должна возвращать значение.");
        }

        base.Visit(statement);
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

        if (call.Arguments.Count != function.ParameterCount)
        {
            throw new FormatException(
                $"Функция '{function.Name}' ожидает аргументов: {function.ParameterCount}; получено: {call.Arguments.Count}.");
        }

        foreach (Expression argument in call.Arguments)
        {
            argument.Accept(this);
        }
    }
}