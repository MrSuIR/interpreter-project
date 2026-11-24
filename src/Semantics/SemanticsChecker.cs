using Lait.Ast;
using Lait.Ast.Declarations;
using Lait.Semantics.Passes;

namespace Lait.Semantics;

/// <summary>
/// Проверяет семантику программы до её выполнения.
/// Реализован как фасад над несколькими проходами, каждый из которых реализует шаблон "Посетитель".
/// </summary>
/// <exception cref="FormatException">Бросается при нарушении правил проверки программы.</exception>
public class SemanticsChecker
{
    private readonly AbstractPass[] _passes;

    public SemanticsChecker(IReadOnlyDictionary<string, BuiltinFunction> builtins)
    {
        // Типы проверяются последними: к этому моменту имена связаны с объявлениями,
        //  а число аргументов и место вызовов функций без результата уже проверены.
        _passes =
        [
            new ResolveNamesPass(builtins),
            new CheckContextSensitiveRulesPass(),
            new ResolveTypesPass(),
        ];
    }

    /// <summary>
    /// Проверяет программу и устанавливает атрибуты её узлов.
    /// </summary>
    public void Check(ProgramNode program)
    {
        try
        {
            foreach (AbstractPass pass in _passes)
            {
                program.Accept(pass);
            }
        }
        catch (InsufficientExecutionStackException)
        {
            throw new InvalidOperationException("Недостаточно стека для проверки вложенного выражения.");
        }
    }
}