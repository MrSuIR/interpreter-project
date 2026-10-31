using Lait.Ast.Declarations;
using Lait.Execution;
using Lait.Lexemes;
using Lait.Parsing;
using Lait.Semantics;

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
    /// Разбирает, проверяет и выполняет программу.
    /// </summary>
    /// <exception cref="FormatException">Бросается при ошибке разбора или проверки программы.</exception>
    /// <exception cref="InvalidOperationException">Бросается при ошибке выполнения программы.</exception>
    public void Execute(string code)
    {
        // 1. Разбор программы.
        Parser parser = new(new Lexer(code));
        FunctionDeclaration program = parser.ParseProgram();

        // 2. Проверка вызовов функций.
        SemanticsChecker checker = new(_builtins.Functions);
        checker.Check(program);

        // 3. Исполнение программы.
        AstEvaluator evaluator = new(_builtins.Functions);
        evaluator.Execute(program);
    }
}