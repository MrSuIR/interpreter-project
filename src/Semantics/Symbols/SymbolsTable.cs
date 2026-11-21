using Lait.Ast.Declarations;

namespace Lait.Semantics.Symbols;

/// <summary>
/// Таблица переменных и параметров одной области видимости с доступом к внешним областям.
/// </summary>
public sealed class SymbolsTable
{
    private readonly Dictionary<string, AbstractVariableDeclaration> _symbols = [];

    public SymbolsTable(SymbolsTable? parent)
    {
        Parent = parent;
    }

    public SymbolsTable? Parent { get; }

    /// <summary>
    /// Находит объявление по имени в этой или во внешних областях видимости.
    /// </summary>
    public AbstractVariableDeclaration GetSymbol(string name)
    {
        if (_symbols.TryGetValue(name, out AbstractVariableDeclaration? symbol))
        {
            return symbol;
        }

        return Parent?.GetSymbol(name) ?? throw new FormatException($"Переменная '{name}' не объявлена.");
    }

    /// <summary>
    /// Объявляет имя в этой области видимости.
    /// </summary>
    public void DefineSymbol(AbstractVariableDeclaration symbol)
    {
        if (!_symbols.TryAdd(symbol.Name, symbol))
        {
            throw new FormatException($"Имя '{symbol.Name}' уже объявлено в этой области видимости.");
        }
    }
}