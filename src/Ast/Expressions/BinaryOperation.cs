namespace Lait.Ast.Expressions;

/// <summary>
/// Бинарная арифметическая операция.
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
    /// Целочисленное деление.
    /// </summary>
    Divide,
}