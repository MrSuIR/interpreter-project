namespace Lait.Ast.Declarations;

/// <summary>
/// Определение встроенной функции языка.
/// </summary>
public sealed class BuiltinFunction : Declaration
{
    private readonly Func<IReadOnlyList<int>, int> _implementation;

    public BuiltinFunction(
        string name,
        IReadOnlyList<string> parameters,
        bool returnsValue,
        Func<IReadOnlyList<int>, int> implementation)
    {
        Name = name;
        Parameters = parameters;
        ReturnsValue = returnsValue;
        _implementation = implementation;
    }

    public string Name { get; }

    /// <summary>
    /// Имена параметров функции; все параметры имеют тип int.
    /// </summary>
    public IReadOnlyList<string> Parameters { get; }

    /// <summary>
    /// Истина, если функция возвращает int, и ложь, если её результат имеет тип void.
    /// </summary>
    public bool ReturnsValue { get; }

    /// <summary>
    /// Выполняет функцию. Результат функции с типом void не используется.
    /// </summary>
    public int Invoke(IReadOnlyList<int> arguments)
    {
        return _implementation(arguments);
    }

    /// <summary>
    /// Встроенная функция не входит в дерево программы, поэтому посетитель к ней не применяется.
    /// </summary>
    public override void Accept(IAstVisitor visitor)
    {
        throw new InvalidOperationException($"Посетитель не применяется к встроенной функции '{Name}'.");
    }
}