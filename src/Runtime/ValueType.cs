namespace Lait.Runtime;

/// <summary>
/// Тип значения в языке Lait.
/// </summary>
public enum ValueType
{
    /// <summary>
    /// Значение отсутствует: результат функции с типом void.
    /// </summary>
    Void,

    /// <summary>
    /// Целое число типа int.
    /// </summary>
    Int,

    /// <summary>
    /// Число с плавающей точкой типа float.
    /// </summary>
    Float,

    /// <summary>
    /// Булево значение типа bool.
    /// </summary>
    Bool,
}