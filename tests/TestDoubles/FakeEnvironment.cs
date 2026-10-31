using System.Text;

using Lait.Execution;

namespace Lait.TestDoubles;

/// <summary>
/// Имитирует средства ввода-вывода для тестов.
/// </summary>
public class FakeEnvironment : IEnvironment
{
    private readonly StringReader _input;
    private readonly StringBuilder _output = new();

    public FakeEnvironment(string input = "")
    {
        _input = new StringReader(input);
    }

    /// <summary>
    /// Весь выведенный текст; каждая строка завершается символом '\n'.
    /// </summary>
    public string Output => _output.ToString();

    public string? ReadLine()
    {
        return _input.ReadLine();
    }

    public void WriteLine(string text)
    {
        _output.Append(text).Append('\n');
    }
}