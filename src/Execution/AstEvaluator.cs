using System.Runtime.CompilerServices;

using Lait.Ast;
using Lait.Ast.Declarations;
using Lait.Ast.Expressions;
using Lait.Ast.Statements;

namespace Lait.Execution;

/// <summary>
/// Выполняет ранее разобранную программу, используя её AST.
/// </summary>
/// <exception cref="InvalidOperationException">Бросается при ошибке в процессе вычислений.</exception>
public class AstEvaluator : IAstVisitor
{
    /// <summary>
    /// Словарь встроенных функций языка.
    /// </summary>
    private readonly IReadOnlyDictionary<string, BuiltinFunction> _builtins;

    /// <summary>
    /// В стек временно складываются результаты вычисления операндов текущей операции.
    /// </summary>
    private readonly Stack<int> _values = [];

    public AstEvaluator(IReadOnlyDictionary<string, BuiltinFunction> builtins)
    {
        _builtins = builtins;
    }

    /// <summary>
    /// Выполняет инструкции тела функции по порядку.
    /// </summary>
    public void Execute(FunctionDeclaration function)
    {
        _values.Clear();
        RunWithRuntimeErrors(() => function.Accept(this));
    }

    /// <summary>
    /// Вычисляет выражение и возвращает его значение.
    /// </summary>
    public int Evaluate(Expression expression)
    {
        _values.Clear();
        RunWithRuntimeErrors(() => expression.Accept(this));

        return _values.Count switch
        {
            1 => _values.Pop(),
            0 => throw new InvalidOperationException("Выражение не имеет значения."),
            _ => throw new InvalidOperationException($"После вычисления в стеке осталось значений: {_values.Count}."),
        };
    }

    public void Visit(LiteralExpression expression)
    {
        _values.Push(expression.Value);
    }

    public void Visit(BinaryOperationExpression expression)
    {
        RuntimeHelpers.EnsureSufficientExecutionStack();

        // Левый операнд вычисляется раньше правого, поэтому правый лежит на вершине стека.
        expression.Left.Accept(this);
        expression.Right.Accept(this);
        int right = _values.Pop();
        int left = _values.Pop();

        _values.Push(expression.Operation switch
        {
            BinaryOperation.Add => checked(left + right),
            BinaryOperation.Subtract => checked(left - right),
            BinaryOperation.Multiply => checked(left * right),
            BinaryOperation.Divide => checked(left / right),
            _ => throw new InvalidOperationException($"Неизвестная бинарная операция {expression.Operation}."),
        });
    }

    public void Visit(UnaryMinusExpression expression)
    {
        RuntimeHelpers.EnsureSufficientExecutionStack();

        expression.Operand.Accept(this);
        _values.Push(checked(-_values.Pop()));
    }

    public void Visit(FunctionCallExpression expression)
    {
        RuntimeHelpers.EnsureSufficientExecutionStack();
        if (!_builtins.TryGetValue(expression.Name, out BuiltinFunction? function))
        {
            throw new InvalidOperationException($"Неизвестная функция '{expression.Name}'.");
        }

        // Вычисляем аргументы функции слева направо.
        List<int> arguments = [];
        foreach (Expression argument in expression.Arguments)
        {
            argument.Accept(this);
            arguments.Add(_values.Pop());
        }

        // Функция без результата не оставляет значения в стеке.
        int result = function.Invoke(arguments);
        if (function.ReturnsValue)
        {
            _values.Push(result);
        }
    }

    public void Visit(ExpressionStatement statement)
    {
        statement.Expression.Accept(this);

        // Результат выражения отбрасывается.
        _values.Clear();
    }

    public void Visit(FunctionDeclaration declaration)
    {
        foreach (Statement statement in declaration.Body)
        {
            statement.Accept(this);
        }
    }

    /// <summary>
    /// Выполняет действие, превращая арифметические ошибки и нехватку стека в ошибки выполнения.
    /// </summary>
    private static void RunWithRuntimeErrors(Action action)
    {
        try
        {
            action();
        }
        catch (OverflowException)
        {
            throw new InvalidOperationException("Результат операции выходит за диапазон int.");
        }
        catch (DivideByZeroException)
        {
            throw new InvalidOperationException("Деление на ноль.");
        }
        catch (InsufficientExecutionStackException)
        {
            throw new InvalidOperationException("Недостаточно стека для вычисления вложенного выражения.");
        }
    }
}