namespace Lait.Ast.Declarations;

/// <summary>
/// Определение встроенной функции языка.
/// </summary>
public sealed class BuiltinFunction : AbstractFunctionDeclaration
{
    private readonly Func<IReadOnlyList<int>, int> _implementation;

    public BuiltinFunction(
        string name,
        IReadOnlyList<string> parameters,
        bool returnsValue,
        Func<IReadOnlyList<int>, int> implementation)
        : base(name, returnsValue)
    {
        Parameters = parameters;
        _implementation = implementation;
    }

    /// <summary>
    /// Имена параметров функции; все параметры имеют тип int.
    /// </summary>
    public IReadOnlyList<string> Parameters { get; }

    public override int ParameterCount => Parameters.Count;

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