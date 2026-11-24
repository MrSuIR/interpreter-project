namespace Lait.Ast.Expressions;

/// <summary>
/// Бинарная операция.
/// </summary>
public enum BinaryOperation
{
    /// <summary>
    /// Сложение чисел.
    /// </summary>
    Add,

    /// <summary>
    /// Вычитание чисел.
    /// </summary>
    Subtract,

    /// <summary>
    /// Умножение чисел.
    /// </summary>
    Multiply,

    /// <summary>
    /// Деление чисел; для int - целочисленное.
    /// </summary>
    Divide,

    /// <summary>
    /// Остаток от целочисленного деления.
    /// </summary>
    Remainder,

    /// <summary>
    /// Проверка равенства.
    /// </summary>
    Equal,

    /// <summary>
    /// Проверка неравенства.
    /// </summary>
    NotEqual,

    /// <summary>
    /// Сравнение "меньше".
    /// </summary>
    LessThan,

    /// <summary>
    /// Сравнение "меньше или равно".
    /// </summary>
    LessThanOrEqual,

    /// <summary>
    /// Сравнение "больше".
    /// </summary>
    GreaterThan,

    /// <summary>
    /// Сравнение "больше или равно".
    /// </summary>
    GreaterThanOrEqual,

    /// <summary>
    /// Логическое "и" с вычислением по короткой схеме.
    /// </summary>
    And,

    /// <summary>
    /// Логическое "или" с вычислением по короткой схеме.
    /// </summary>
    Or,
}