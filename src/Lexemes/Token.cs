using System.Text;

namespace Lait.Lexemes;

/// <summary>
/// Представляет один токен исходного кода Lait.
/// </summary>
public class Token
{
    public Token(TokenType type)
    {
        Type = type;
    }

    public Token(TokenType type, string value)
    {
        Type = type;
        Value = new TokenValue(value);
    }

    public Token(TokenType type, int value)
    {
        Type = type;
        Value = new TokenValue(value);
    }

    public TokenType Type { get; }

    public TokenValue? Value { get; }

    /// <summary>
    /// Сравнивает токены по типу и значению.
    /// </summary>
    public override bool Equals(object? obj)
    {
        return obj is Token other
            && Type == other.Type
            && Equals(Value, other.Value);
    }

    /// <summary>
    /// Возвращает хеш-код типа и значения токена.
    /// </summary>
    public override int GetHashCode()
    {
        return HashCode.Combine(Type, Value);
    }

    /// <summary>
    /// Возвращает токен в формате Type или Type (Value).
    /// </summary>
    public override string ToString()
    {
        StringBuilder stringBuilder = new();
        stringBuilder.Append(Type);

        if (Value is not null)
        {
            stringBuilder.Append(" (");
            stringBuilder.Append(Value);
            stringBuilder.Append(')');
        }

        return stringBuilder.ToString();
    }
}