namespace Lait.Execution;

/// <summary>
/// Средства ввода-вывода, которые использует программа.
/// </summary>
public interface IEnvironment
{
    /// <summary>
    /// Читает строку из потока ввода либо возвращает null, если достигнут конец ввода.
    /// </summary>
    public string? ReadLine();

    /// <summary>
    /// Печатает текст в поток вывода и завершает строку.
    /// </summary>
    public void WriteLine(string text);
}