namespace Lait.Lexemes;

/// <summary>
/// Предоставляет последовательный доступ к символам исходного текста.
/// </summary>
public class TextScanner
{
    private readonly string _input;
    private int _position;

    public TextScanner(string input)
    {
        _input = input;
    }

    /// <summary>
    /// Возвращает текущий символ или символ на указанное количество позиций вперёд.
    /// </summary>
    public char Peek(int offset = 0)
    {
        int position = _position + offset;
        return position >= _input.Length ? '\0' : _input[position];
    }

    /// <summary>
    /// Передвигает текущую позицию на один символ вперёд.
    /// </summary>
    public void Advance()
    {
        _position++;
    }

    /// <summary>
    /// Проверяет, достигнут ли конец исходного текста.
    /// </summary>
    public bool IsEnd()
    {
        return _position >= _input.Length;
    }
}