using Lait.Ast.Declarations;
using Lait.Ast.Expressions;
using Lait.Ast.Statements;

namespace Lait.Ast;

/// <summary>
/// Посетитель узлов синтаксического дерева.
/// </summary>
public interface IAstVisitor
{
    void Visit(LiteralExpression expression);

    void Visit(BinaryOperationExpression expression);

    void Visit(UnaryMinusExpression expression);

    void Visit(FunctionCallExpression expression);

    void Visit(ExpressionStatement statement);

    void Visit(FunctionDeclaration declaration);
}