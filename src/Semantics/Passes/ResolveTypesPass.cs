using Lait.Ast.Declarations;
using Lait.Ast.Expressions;
using Lait.Ast.Statements;
using Lait.Runtime;

using ValueType = Lait.Runtime.ValueType;

namespace Lait.Semantics.Passes;

/// <summary>
/// Проход по AST выполняет две задачи:
///  1. Вычислить типы выражений.
///  2. Проверить корректность программы с точки зрения совместимости типов.
/// </summary>
/// <remarks>
/// Выполняется после проверки контекстно-зависимых правил, поэтому число аргументов вызовов уже проверено,
///  а вызовы функций без результата встречаются только как целые инструкции.
/// </remarks>
public sealed class ResolveTypesPass : AbstractPass
{
    /// <summary>
    /// Функция, тело которой проверяется сейчас.
    /// </summary>
    private FunctionDeclaration? _function;

    /// <summary>
    /// Литерал всегда имеет определённый тип.
    /// </summary>
    public override void Visit(LiteralExpression expression)
    {
        expression.ResultType = expression.Value.GetValueType();
    }

    /// <summary>
    /// Выполняет проверки типов для бинарных операций по таблице "Типы операндов" спецификации.
    /// </summary>
    public override void Visit(BinaryOperationExpression expression)
    {
        base.Visit(expression);

        ValueType left = expression.Left.ResultType;
        ValueType right = expression.Right.ResultType;
        ValueType? resultType = GetBinaryOperationResultType(expression.Operation, left, right);
        if (resultType is null)
        {
            throw new FormatException(
                $"Операция '{GetSymbol(expression.Operation)}' недопустима для типов {left.GetName()} и {right.GetName()}.");
        }

        expression.ResultType = resultType.Value;
    }

    /// <summary>
    /// Унарный минус применяется к числам и возвращает число того же типа.
    /// </summary>
    public override void Visit(UnaryMinusExpression expression)
    {
        base.Visit(expression);

        ValueType operandType = expression.Operand.ResultType;
        if (operandType is not (ValueType.Int or ValueType.Float))
        {
            throw new FormatException($"Операция '-' недопустима для типа {operandType.GetName()}.");
        }

        expression.ResultType = operandType;
    }

    /// <summary>
    /// Логическое отрицание применяется к булеву значению.
    /// </summary>
    public override void Visit(LogicalNotExpression expression)
    {
        base.Visit(expression);

        CheckResultType(expression.Operand, ValueType.Bool, "Операция '!' требует операнд");
        expression.ResultType = ValueType.Bool;
    }

    /// <summary>
    /// Проверяет соответствие типов параметров функции и аргументов при её вызове.
    /// </summary>
    public override void Visit(FunctionCallExpression expression)
    {
        base.Visit(expression);

        AbstractFunctionDeclaration function = expression.Function;
        for (int index = 0; index < expression.Arguments.Count; index++)
        {
            CheckResultType(
                expression.Arguments[index],
                function.Parameters[index].Type,
                $"Аргумент {index + 1} функции '{function.Name}' должен иметь тип");
        }

        expression.ResultType = function.ResultType;
    }

    public override void Visit(VariableAccessExpression expression)
    {
        expression.ResultType = expression.Variable.Type;
    }

    /// <summary>
    /// Присваивание имеет тип переменной; правая часть должна иметь тот же тип.
    /// </summary>
    public override void Visit(AssignmentExpression expression)
    {
        base.Visit(expression);

        // Левая часть - переменная, это проверено при проверке контекстно-зависимых правил.
        VariableAccessExpression variable = (VariableAccessExpression)expression.Left;
        CheckResultType(
            expression.Right,
            variable.ResultType,
            $"Значение, присваиваемое переменной '{variable.Name}', должно иметь тип");
        expression.ResultType = variable.ResultType;
    }

    /// <summary>
    /// Проверяет тип переменной и тип выражения, которым она инициализируется.
    /// </summary>
    public override void Visit(VariableDeclaration declaration)
    {
        base.Visit(declaration);

        CheckResultType(
            declaration.InitialValue,
            declaration.Type,
            $"Инициализатор переменной '{declaration.Name}' должен иметь тип");
    }

    public override void Visit(FunctionDeclaration declaration)
    {
        _function = declaration;
        base.Visit(declaration);
    }

    /// <summary>
    /// Тип возвращаемого значения совпадает с типом результата функции.
    /// </summary>
    public override void Visit(ReturnStatement statement)
    {
        base.Visit(statement);

        FunctionDeclaration function = _function ?? throw new InvalidOperationException("Инструкция return вне функции.");
        if (statement.Value is not null)
        {
            CheckResultType(statement.Value, function.ResultType, $"Функция '{function.Name}' должна возвращать значение типа");
        }
    }

    public override void Visit(IfElseStatement statement)
    {
        base.Visit(statement);
        CheckResultType(statement.Condition, ValueType.Bool, "Условие инструкции if должно иметь тип");
    }

    public override void Visit(WhileLoopStatement statement)
    {
        base.Visit(statement);
        CheckResultType(statement.Condition, ValueType.Bool, "Условие цикла while должно иметь тип");
    }

    public override void Visit(ForLoopStatement statement)
    {
        base.Visit(statement);
        CheckResultType(statement.Condition, ValueType.Bool, "Условие цикла for должно иметь тип");
    }

    /// <summary>
    /// Вычисляет тип результата бинарной операции.
    /// Возвращает null, если операция не может быть выполнена с указанными типами.
    /// </summary>
    private static ValueType? GetBinaryOperationResultType(BinaryOperation operation, ValueType left, ValueType right)
    {
        if (left != right)
        {
            return null;
        }

        bool isNumber = left is ValueType.Int or ValueType.Float;
        return operation switch
        {
            BinaryOperation.Add or BinaryOperation.Subtract or BinaryOperation.Multiply or BinaryOperation.Divide
                when isNumber => left,
            BinaryOperation.Remainder when left == ValueType.Int => ValueType.Int,
            BinaryOperation.LessThan or BinaryOperation.LessThanOrEqual
                or BinaryOperation.GreaterThan or BinaryOperation.GreaterThanOrEqual
                when isNumber => ValueType.Bool,
            BinaryOperation.Equal or BinaryOperation.NotEqual when left != ValueType.Void => ValueType.Bool,
            BinaryOperation.And or BinaryOperation.Or when left == ValueType.Bool => ValueType.Bool,
            _ => null,
        };
    }

    /// <summary>
    /// Возвращает запись бинарной операции в программе.
    /// </summary>
    private static string GetSymbol(BinaryOperation operation)
    {
        return operation switch
        {
            BinaryOperation.Add => "+",
            BinaryOperation.Subtract => "-",
            BinaryOperation.Multiply => "*",
            BinaryOperation.Divide => "/",
            BinaryOperation.Remainder => "%",
            BinaryOperation.Equal => "==",
            BinaryOperation.NotEqual => "!=",
            BinaryOperation.LessThan => "<",
            BinaryOperation.LessThanOrEqual => "<=",
            BinaryOperation.GreaterThan => ">",
            BinaryOperation.GreaterThanOrEqual => ">=",
            BinaryOperation.And => "&&",
            BinaryOperation.Or => "||",
            _ => throw new ArgumentOutOfRangeException(nameof(operation), operation, "Неизвестная бинарная операция."),
        };
    }

    /// <summary>
    /// Проверяет тип выражения; сообщение об ошибке начинается с описания места.
    /// </summary>
    private static void CheckResultType(Expression expression, ValueType expectedType, string description)
    {
        if (expression.ResultType != expectedType)
        {
            throw new FormatException(
                $"{description} {expectedType.GetName()}; получен {expression.ResultType.GetName()}.");
        }
    }
}