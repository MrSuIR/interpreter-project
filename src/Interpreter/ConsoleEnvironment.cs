using Lait.Execution;

namespace Lait.Interpreter;

/// <summary>
/// Ввод из стандартного потока ввода и вывод в стандартный поток вывода.
/// </summary>
public class ConsoleEnvironment : IEnvironment
{
    public string? ReadLine()
    {
        return Console.In.ReadLine();
    }

    public void WriteLine(string text)
    {
        Console.Out.WriteLine(text);
        Console.Out.Flush();
    }
}