using Lait.Ast.Declarations;

namespace Lait.Ast;

/// <summary>
/// Корень AST: все функции, объявленные в программе.
/// </summary>
public sealed class ProgramNode : AstNode
{
    public ProgramNode(IReadOnlyList<FunctionDeclaration> functions)
    {
        Functions = functions;
    }

    public IReadOnlyList<FunctionDeclaration> Functions { get; }

    public override void Accept(IAstVisitor visitor)
    {
        visitor.Visit(this);
    }
}