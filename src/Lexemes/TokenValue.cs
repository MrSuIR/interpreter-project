using System.Globalization;

namespace Lait.Lexemes;

/// <summary>
/// Представляет строковое или целочисленное значение токена.
/// </summary>
public class TokenValue
{
    private readonly object _value;

    public TokenValue(string value)
    {
        _value = value;
    }

    public TokenValue(int value)
    {
        _value = value;
    }

    /// <summary>
    /// Возвращает значение токена в виде строки.
    /// </summary>
    public override string ToString()
    {
        return _value switch
        {
            string stringValue => stringValue,
            int intValue => intValue.ToString(CultureInfo.InvariantCulture),
            _ => throw new InvalidOperationException("Неизвестный тип значения токена."),
        };
    }

    /// <summary>
    /// Возвращает целочисленное значение токена.
    /// </summary>
    public int ToInt()
    {
        return _value switch
        {
            int intValue => intValue,
            _ => throw new InvalidOperationException("Значение токена не является целым числом."),
        };
    }

    /// <summary>
    /// Сравнивает значения с учётом их типов.
    /// </summary>
    public override bool Equals(object? obj)
    {
        return obj is TokenValue other && _value.Equals(other._value);
    }

    /// <summary>
    /// Возвращает хеш-код значения.
    /// </summary>
    public override int GetHashCode()
    {
        return _value.GetHashCode();
    }
}