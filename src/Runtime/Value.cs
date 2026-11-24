using System.Globalization;

namespace Lait.Runtime;

/// <summary>
/// Значение одного из типов языка Lait.
/// </summary>
public sealed class Value : IEquatable<Value>
{
    /// <summary>
    /// Отсутствие значения: результат функции с типом void.
    /// </summary>
    public static readonly Value Void = new(new VoidType());

    private readonly object _value;

    public Value(int value)
    {
        _value = value;
    }

    public Value(double value)
    {
        _value = value;
    }

    public Value(bool value)
    {
        _value = value;
    }

    private Value(VoidType value)
    {
        _value = value;
    }

    /// <summary>
    /// Возвращает тип значения.
    /// </summary>
    public ValueType GetValueType()
    {
        return _value switch
        {
            int => ValueType.Int,
            double => ValueType.Float,
            bool => ValueType.Bool,
            VoidType => ValueType.Void,
            _ => throw new InvalidOperationException($"Неизвестный вид значения {_value.GetType()}."),
        };
    }

    /// <summary>
    /// Возвращает значение как целое число либо бросает исключение.
    /// </summary>
    public int AsInt()
    {
        return _value is int intValue
            ? intValue
            : throw new InvalidOperationException($"Значение {this} не является числом типа int.");
    }

    /// <summary>
    /// Возвращает значение как число с плавающей точкой либо бросает исключение.
    /// </summary>
    public double AsFloat()
    {
        return _value is double floatValue
            ? floatValue
            : throw new InvalidOperationException($"Значение {this} не является числом типа float.");
    }

    /// <summary>
    /// Возвращает значение как булево значение либо бросает исключение.
    /// </summary>
    public bool AsBool()
    {
        return _value is bool boolValue
            ? boolValue
            : throw new InvalidOperationException($"Значение {this} не является значением типа bool.");
    }

    /// <summary>
    /// Печатает значение для отладки.
    /// </summary>
    public override string ToString()
    {
        return _value switch
        {
            int intValue => intValue.ToString(CultureInfo.InvariantCulture),
            double floatValue => floatValue.ToString("R", CultureInfo.InvariantCulture),
            bool boolValue => boolValue ? "true" : "false",
            VoidType => "void",
            _ => throw new InvalidOperationException($"Неизвестный вид значения {_value.GetType()}."),
        };
    }

    /// <summary>
    /// Сравнивает значения одного типа по правилам операции ==.
    /// </summary>
    public bool Equals(Value? other)
    {
        if (other is null || GetValueType() != other.GetValueType())
        {
            return false;
        }

        return _value switch
        {
            int intValue => intValue == other.AsInt(),

            // Числа сравниваются по значению, поэтому 0.0 и -0.0 равны.
            double floatValue => floatValue == other.AsFloat(),
            bool boolValue => boolValue == other.AsBool(),
            VoidType => true,
            _ => throw new InvalidOperationException($"Неизвестный вид значения {_value.GetType()}."),
        };
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as Value);
    }

    public override int GetHashCode()
    {
        // Хеш-код double одинаков для 0.0 и -0.0, что согласуется с Equals.
        return _value is VoidType ? 0 : _value.GetHashCode();
    }

    /// <summary>
    /// Маркер отсутствия значения.
    /// </summary>
    private sealed class VoidType
    {
    }
}