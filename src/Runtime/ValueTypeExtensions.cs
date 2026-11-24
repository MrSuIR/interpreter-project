namespace Lait.Runtime;

/// <summary>
/// Вспомогательные методы для типов значений.
/// </summary>
public static class ValueTypeExtensions
{
    /// <summary>
    /// Возвращает имя типа так, как оно записывается в программе на Lait.
    /// </summary>
    public static string GetName(this ValueType type)
    {
        return type switch
        {
            ValueType.Void => "void",
            ValueType.Int => "int",
            ValueType.Float => "float",
            ValueType.Bool => "bool",
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Неизвестный тип значения."),
        };
    }
}