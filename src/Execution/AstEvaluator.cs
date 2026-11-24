using System.Runtime.CompilerServices;

using Lait.Ast;
using Lait.Ast.Declarations;
using Lait.Ast.Expressions;
using Lait.Ast.Statements;
using Lait.Runtime;

using ValueType = Lait.Runtime.ValueType;

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
    private const string FloatOutOfRange = "Результат операции выходит за диапазон float.";

    /// <summary>
    /// В стек временно складываются результаты вычисления операндов текущей операции.
    /// </summary>
    private readonly Stack<Value> _values = [];

    /// <summary>
    /// Значения параметров и переменных выполняемого вызова функции.
    /// Объявления уникальны, поэтому перекрытые имена не смешиваются.
    /// </summary>
    private Dictionary<AbstractVariableDeclaration, Value> _frame = [];

    /// <summary>
    /// Переход, начатый инструкцией return, break или continue и ещё не завершённый.
    /// </summary>
    private Jump _jump;

    /// <summary>
    /// Значение, переданное в return.
    /// </summary>
    private Value _returnValue = Value.Void;

    /// <summary>
    /// Переходы, прерывающие последовательное выполнение инструкций.
    /// Блок прекращает выполнение, пока переход не завершён: return завершает вызов функции,
    ///  а break и continue - шаг ближайшего цикла.
    /// </summary>
    private enum Jump
    {
        None,
        Return,
        Break,
        Continue,
    }

    /// <summary>
    /// Выполняет программу, вызывая функцию main.
    /// </summary>
    public void Execute(ProgramNode program)
    {
        _values.Clear();
        _frame = [];
        _jump = Jump.None;
        _returnValue = Value.Void;

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

        // Логические операции вычисляются по короткой схеме: правый операнд может не понадобиться.
        if (expression.Operation is BinaryOperation.And or BinaryOperation.Or)
        {
            expression.Left.Accept(this);
            bool left = _values.Peek().AsBool();
            if (left == (expression.Operation == BinaryOperation.Or))
            {
                return;
            }

            _values.Pop();
            expression.Right.Accept(this);
            return;
        }

        // Левый операнд вычисляется раньше правого, поэтому правый лежит на вершине стека.
        expression.Left.Accept(this);
        expression.Right.Accept(this);
        Value rightValue = _values.Pop();
        Value leftValue = _values.Pop();

        _values.Push(expression.Operation switch
        {
            BinaryOperation.Equal => new Value(leftValue.Equals(rightValue)),
            BinaryOperation.NotEqual => new Value(!leftValue.Equals(rightValue)),
            _ when leftValue.GetValueType() == ValueType.Float =>
                ApplyFloatOperation(expression.Operation, leftValue.AsFloat(), rightValue.AsFloat()),
            _ => ApplyIntOperation(expression.Operation, leftValue.AsInt(), rightValue.AsInt()),
        });
    }

    public void Visit(UnaryMinusExpression expression)
    {
        RuntimeHelpers.EnsureSufficientExecutionStack();

        expression.Operand.Accept(this);
        Value operand = _values.Pop();
        _values.Push(operand.GetValueType() == ValueType.Float
            ? new Value(-operand.AsFloat())
            : new Value(checked(-operand.AsInt())));
    }

    public void Visit(LogicalNotExpression expression)
    {
        RuntimeHelpers.EnsureSufficientExecutionStack();

        expression.Operand.Accept(this);
        _values.Push(new Value(!_values.Pop().AsBool()));
    }

    public void Visit(FunctionCallExpression expression)
    {
        RuntimeHelpers.EnsureSufficientExecutionStack();

        // Вычисляем аргументы функции слева направо.
        List<Value> arguments = [];
        foreach (Expression argument in expression.Arguments)
        {
            argument.Accept(this);
            arguments.Add(_values.Pop());
        }

        Value result = expression.Function switch
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
            if (_jump != Jump.None)
            {
                return;
            }
        }
    }

    public void Visit(VariableDeclarationStatement statement)
    {
        statement.Declaration.Accept(this);
    }

    public void Visit(IfElseStatement statement)
    {
        RuntimeHelpers.EnsureSufficientExecutionStack();
        if (EvaluateCondition(statement.Condition))
        {
            statement.ThenBranch.Accept(this);
        }
        else
        {
            statement.ElseBranch?.Accept(this);
        }
    }

    public void Visit(WhileLoopStatement statement)
    {
        RuntimeHelpers.EnsureSufficientExecutionStack();
        while (EvaluateCondition(statement.Condition))
        {
            statement.Body.Accept(this);
            if (CompleteLoopIteration())
            {
                return;
            }
        }
    }

    public void Visit(ForLoopStatement statement)
    {
        RuntimeHelpers.EnsureSufficientExecutionStack();
        statement.Initializer.Accept(this);
        while (EvaluateCondition(statement.Condition))
        {
            statement.Body.Accept(this);
            if (CompleteLoopIteration())
            {
                return;
            }

            // После continue цикл for тоже выполняет шаг.
            statement.Step.Accept(this);
        }
    }

    public void Visit(BreakLoopStatement statement)
    {
        _jump = Jump.Break;
    }

    public void Visit(ContinueLoopStatement statement)
    {
        _jump = Jump.Continue;
    }

    public void Visit(ReturnStatement statement)
    {
        if (statement.Value is not null)
        {
            statement.Value.Accept(this);
            _returnValue = _values.Pop();
        }

        _jump = Jump.Return;
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
    /// Выполняет арифметическую операцию или сравнение двух чисел типа int.
    /// </summary>
    private static Value ApplyIntOperation(BinaryOperation operation, int left, int right)
    {
        return operation switch
        {
            BinaryOperation.Add => new Value(checked(left + right)),
            BinaryOperation.Subtract => new Value(checked(left - right)),
            BinaryOperation.Multiply => new Value(checked(left * right)),
            BinaryOperation.Divide => new Value(checked(left / right)),

            // Деление int.MinValue на -1 с остатком в .NET бросает OverflowException, как и деление.
            BinaryOperation.Remainder => new Value(checked(left % right)),
            BinaryOperation.LessThan => new Value(left < right),
            BinaryOperation.LessThanOrEqual => new Value(left <= right),
            BinaryOperation.GreaterThan => new Value(left > right),
            BinaryOperation.GreaterThanOrEqual => new Value(left >= right),
            _ => throw new InvalidOperationException($"Неизвестная операция {operation} для int."),
        };
    }

    /// <summary>
    /// Выполняет арифметическую операцию или сравнение двух чисел типа float.
    /// </summary>
    private static Value ApplyFloatOperation(BinaryOperation operation, double left, double right)
    {
        if (operation == BinaryOperation.Divide && right == 0.0)
        {
            throw new DivideByZeroException();
        }

        return operation switch
        {
            BinaryOperation.Add => FiniteFloat(left + right),
            BinaryOperation.Subtract => FiniteFloat(left - right),
            BinaryOperation.Multiply => FiniteFloat(left * right),
            BinaryOperation.Divide => FiniteFloat(left / right),
            BinaryOperation.LessThan => new Value(left < right),
            BinaryOperation.LessThanOrEqual => new Value(left <= right),
            BinaryOperation.GreaterThan => new Value(left > right),
            BinaryOperation.GreaterThanOrEqual => new Value(left >= right),
            _ => throw new InvalidOperationException($"Неизвестная операция {operation} для float."),
        };
    }

    /// <summary>
    /// Проверяет, что результат операции над float является конечным числом.
    /// </summary>
    private static Value FiniteFloat(double result)
    {
        return double.IsFinite(result) ? new Value(result) : throw new InvalidOperationException(FloatOutOfRange);
    }

    private bool EvaluateCondition(Expression condition)
    {
        condition.Accept(this);
        return _values.Pop().AsBool();
    }

    /// <summary>
    /// Завершает шаг цикла после выполнения тела и возвращает истину, если цикл нужно прекратить.
    /// </summary>
    private bool CompleteLoopIteration()
    {
        switch (_jump)
        {
            case Jump.Break:
                _jump = Jump.None;
                return true;
            case Jump.Continue:
                _jump = Jump.None;
                return false;
            default:
                // return прекращает цикл и продолжает выход из функции.
                return _jump == Jump.Return;
        }
    }

    /// <summary>
    /// Выполняет функцию программы в новом кадре вызова и возвращает её результат.
    /// </summary>
    private Value InvokeFunction(FunctionDeclaration function, IReadOnlyList<Value> arguments)
    {
        // Параметры передаются по значению.
        Dictionary<AbstractVariableDeclaration, Value> frame = [];
        for (int index = 0; index < function.Parameters.Count; index++)
        {
            frame[function.Parameters[index]] = arguments[index];
        }

        Dictionary<AbstractVariableDeclaration, Value> callerFrame = _frame;
        _frame = frame;
        try
        {
            function.Body.Accept(this);
        }
        finally
        {
            _frame = callerFrame;
        }

        Value result = _returnValue;
        _jump = Jump.None;
        _returnValue = Value.Void;
        return result;
    }
}