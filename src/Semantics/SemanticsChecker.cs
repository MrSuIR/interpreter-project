using System.Runtime.CompilerServices;

using Lait.Ast;
using Lait.Ast.Declarations;
using Lait.Ast.Expressions;
using Lait.Ast.Statements;

namespace Lait.Semantics;

/// <summary>
/// Проверяет корректность программы с точки зрения использования функций.
/// </summary>
/// <exception cref="FormatException">Бросается при нарушении правил вызова функций.</exception>
public class SemanticsChecker : IAstVisitor
{
    /// <summary>
    /// Словарь встроенных функций языка.
    /// </summary>
    private readonly IReadOnlyDictionary<string, BuiltinFunction> _builtins;

    public SemanticsChecker(IReadOnlyDictionary<string, BuiltinFunction> builtins)
    {
        _builtins = builtins;
    }

    /// <summary>
    /// Проверяет программу до её выполнения.
    /// </summary>
    public void Check(FunctionDeclaration program)
    {
        try
        {
            program.Accept(this);
        }
        catch (InsufficientExecutionStackException)
        {
            throw new InvalidOperationException("Недостаточно стека для проверки вложенного выражения.");
        }
    }

    public void Visit(LiteralExpression expression)
    {
    }

    public void Visit(BinaryOperationExpression expression)
    {
        RuntimeHelpers.EnsureSufficientExecutionStack();
        expression.Left.Accept(this);
        expression.Right.Accept(this);
    }

    public void Visit(UnaryMinusExpression expression)
    {
        RuntimeHelpers.EnsureSufficientExecutionStack();
        expression.Operand.Accept(this);
    }

    public void Visit(FunctionCallExpression expression)
    {
        CheckCall(expression, requiresResult: true);
    }

    public void Visit(ExpressionStatement statement)
    {
        // Вызов функции без результата допускается только как всё выражение инструкции.
        if (statement.Expression is FunctionCallExpression call)
        {
            CheckCall(call, requiresResult: false);
        }
        else
        {
            statement.Expression.Accept(this);
        }
    }

    public void Visit(FunctionDeclaration declaration)
    {
        foreach (Statement statement in declaration.Body)
        {
            statement.Accept(this);
        }
    }

    private void CheckCall(FunctionCallExpression call, bool requiresResult)
    {
        RuntimeHelpers.EnsureSufficientExecutionStack();
        if (!_builtins.TryGetValue(call.Name, out BuiltinFunction? function))
        {
            throw new FormatException($"Неизвестная функция '{call.Name}'.");
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