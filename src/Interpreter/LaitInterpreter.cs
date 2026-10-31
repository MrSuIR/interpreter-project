using Lait.Execution;
using Lait.Lexemes;
using Lait.Parsing;

namespace Lait.Interpreter;

/// <summary>
/// Выполняет программы на языке Lait.
/// </summary>
public class LaitInterpreter
{
    private readonly Builtins _builtins;

    public LaitInterpreter(IEnvironment environment)
    {
        _builtins = new Builtins(environment);
    }

    /// <summary>
    /// Разбирает и выполняет программу.
    /// </summary>
    /// <exception cref="FormatException">Бросается при ошибке разбора программы.</exception>
    /// <exception cref="InvalidOperationException">Бросается при ошибке выполнения программы.</exception>
    public void Execute(string code)
    {
        Parser parser = new(new Lexer(code), _builtins);
        parser.ParseProgram();
    }
}