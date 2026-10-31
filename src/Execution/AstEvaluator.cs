using System.Runtime.CompilerServices;

using Lait.Ast;
using Lait.Ast.Declarations;
using Lait.Ast.Expressions;
using Lait.Ast.Statements;

namespace Lait.Execution;

/// <summary>
/// Выполняет ранее разобранную и проверенную программу, используя её AST.
/// </summary>
/// <remarks>
/// Использует атрибуты, установленные при проверке программы: объявления вызываемых функций и переменных.
/// </remarks>
/// <exception cref="InvalidOperationException">Бросается при ошибке в процессе вычислений.</exception>
public class AstEvaluator : IAstVisitor
{
    /// <summary>
    /// В стек временно складываются результаты вычисления операндов текущей операции.
    /// </summary>
    private readonly Stack<int> _values = [];

    /// <summary>
    /// Значения параметров и переменных выполняемого вызова функции.
    /// Объявления уникальны, поэтому перекрытые имена не смешиваются.
    /// </summary>
    private Dictionary<AbstractVariableDeclaration, int> _frame = [];

    /// <summary>
    /// Истина после выполнения return, пока не завершится текущий вызов функции.
    /// </summary>
    private bool _returning;

    /// <summary>
    /// Значение, переданное в return.
    /// </summary>
    private int _returnValue;

    /// <summary>
    /// Выполняет программу, вызывая функцию main.
    /// </summary>
    public void Execute(ProgramNode program)
    {
        _values.Clear();
        try
        {
            program.Accept(this);
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
            throw new InvalidOperationException("Недостаточно стека для выполнения программы.");
        }
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

        // Вычисляем аргументы функции слева направо.
        List<int> arguments = [];
        foreach (Expression argument in expression.Arguments)
        {
            argument.Accept(this);
            arguments.Add(_values.Pop());
        }

        int result = expression.Function switch
        {
            BuiltinFunction builtin => builtin.Invoke(arguments),
            FunctionDeclaration function => InvokeFunction(function, arguments),
            _ => throw new InvalidOperationException($"Неизвестный вид функции '{expression.Name}'."),
        };

        // Функция без результата не оставляет значения в стеке.
        if (expression.Function.ReturnsValue)
        {
            _values.Push(result);
        }
    }

    public void Visit(VariableAccessExpression expression)
    {
        _values.Push(_frame[expression.Variable]);
    }

    public void Visit(AssignmentExpression expression)
    {
        RuntimeHelpers.EnsureSufficientExecutionStack();
        if (expression.Left is not VariableAccessExpression variable)
        {
            throw new InvalidOperationException("Слева от присваивания должна стоять переменная.");
        }

        // Значение правой части остаётся в стеке как значение присваивания.
        expression.Right.Accept(this);
        _frame[variable.Variable] = _values.Peek();
    }

    public void Visit(ExpressionStatement statement)
    {
        int depth = _values.Count;
        statement.Expression.Accept(this);

        // Результат выражения отбрасывается.
        while (_values.Count > depth)
        {
            _values.Pop();
        }
    }

    public void Visit(BlockStatement statement)
    {
        RuntimeHelpers.EnsureSufficientExecutionStack();
        foreach (Statement nested in statement.Statements)
        {
            nested.Accept(this);
            if (_returning)
            {
                return;
            }
        }
    }

    public void Visit(VariableDeclarationStatement statement)
    {
        statement.Declaration.Accept(this);
    }

    public void Visit(ReturnStatement statement)
    {
        if (statement.Value is not null)
        {
            statement.Value.Accept(this);
            _returnValue = _values.Pop();
        }

        _returning = true;
    }

    public void Visit(FunctionDeclaration declaration)
    {
        // Объявление функции ничего не выполняет: тело выполняется при вызове.
    }

    public void Visit(ParameterDeclaration declaration)
    {
        // Параметры получают значения при вызове функции.
    }

    public void Visit(VariableDeclaration declaration)
    {
        declaration.InitialValue.Accept(this);
        _frame[declaration] = _values.Pop();
    }

    public void Visit(ProgramNode program)
    {
        FunctionDeclaration main = program.Functions.First(function => function.Name == "main");
        InvokeFunction(main, []);
    }

    /// <summary>
    /// Выполняет функцию программы в новом кадре вызова и возвращает её результат.
    /// </summary>
    private int InvokeFunction(FunctionDeclaration function, IReadOnlyList<int> arguments)
    {
        // Параметры передаются по значению.
        Dictionary<AbstractVariableDeclaration, int> frame = [];
        for (int index = 0; index < function.Parameters.Count; index++)
        {
            frame[function.Parameters[index]] = arguments[index];
        }

        Dictionary<AbstractVariableDeclaration, int> callerFrame = _frame;
        _frame = frame;
        try
        {
            function.Body.Accept(this);
        }
        finally
        {
            _frame = callerFrame;
        }

        int result = _returnValue;
        _returning = false;
        _returnValue = 0;
        return result;
    }
}