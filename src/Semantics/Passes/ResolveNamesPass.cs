using Lait.Ast;
using Lait.Ast.Declarations;
using Lait.Ast.Expressions;
using Lait.Ast.Statements;
using Lait.Semantics.Symbols;

namespace Lait.Semantics.Passes;

/// <summary>
/// Проход по AST, устанавливающий соответствие имён и объявлений.
/// </summary>
/// <remarks>
/// Функции и переменные находятся в разных пространствах имён: имя вызова ищется среди функций,
///  остальные имена - среди переменных и параметров.
/// </remarks>
public sealed class ResolveNamesPass : AbstractPass
{
    /// <summary>
    /// Встроенные функции и функции программы по именам.
    /// </summary>
    private readonly Dictionary<string, AbstractFunctionDeclaration> _functions = [];

    /// <summary>
    /// Переменные и параметры, видимые в текущей области видимости.
    /// </summary>
    private SymbolsTable _variables = new(parent: null);

    public ResolveNamesPass(IReadOnlyDictionary<string, BuiltinFunction> builtins)
    {
        foreach (BuiltinFunction function in builtins.Values)
        {
            _functions.Add(function.Name, function);
        }
    }

    public override void Visit(ProgramNode program)
    {
        // Функции видны во всей программе, поэтому объявляем их до обхода тел.
        foreach (FunctionDeclaration function in program.Functions)
        {
            if (_functions.TryGetValue(function.Name, out AbstractFunctionDeclaration? existing))
            {
                throw new FormatException(existing is BuiltinFunction
                    ? $"Функция '{function.Name}' совпадает со встроенной функцией."
                    : $"Функция '{function.Name}' уже объявлена.");
            }

            _functions.Add(function.Name, function);
        }

        base.Visit(program);
    }

    public override void Visit(FunctionDeclaration declaration)
    {
        // Тело функции видит только свои параметры и переменные; параметры принадлежат области тела.
        _variables = new SymbolsTable(parent: null);
        foreach (ParameterDeclaration parameter in declaration.Parameters)
        {
            parameter.Accept(this);
        }

        foreach (Statement statement in declaration.Body.Statements)
        {
            statement.Accept(this);
        }
    }

    public override void Visit(ParameterDeclaration declaration)
    {
        _variables.DefineSymbol(declaration);
    }

    public override void Visit(BlockStatement statement)
    {
        _variables = new SymbolsTable(_variables);
        try
        {
            base.Visit(statement);
        }
        finally
        {
            _variables = _variables.Parent!;
        }
    }

    public override void Visit(VariableDeclaration declaration)
    {
        // Инициализатор не видит объявляемую переменную.
        base.Visit(declaration);
        _variables.DefineSymbol(declaration);
    }

    public override void Visit(VariableAccessExpression expression)
    {
        expression.Variable = _variables.GetSymbol(expression.Name);
    }

    public override void Visit(FunctionCallExpression expression)
    {
        base.Visit(expression);
        if (!_functions.TryGetValue(expression.Name, out AbstractFunctionDeclaration? function))
        {
            throw new FormatException($"Неизвестная функция '{expression.Name}'.");
        }

        expression.Function = function;
    }
}