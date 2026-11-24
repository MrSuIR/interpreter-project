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

    void Visit(LogicalNotExpression expression);

    void Visit(FunctionCallExpression expression);

    void Visit(VariableAccessExpression expression);

    void Visit(AssignmentExpression expression);

    void Visit(ExpressionStatement statement);

    void Visit(BlockStatement statement);

    void Visit(VariableDeclarationStatement statement);

    void Visit(IfElseStatement statement);

    void Visit(WhileLoopStatement statement);

    void Visit(ForLoopStatement statement);

    void Visit(BreakLoopStatement statement);

    void Visit(ContinueLoopStatement statement);

    void Visit(ReturnStatement statement);

    void Visit(FunctionDeclaration declaration);

    void Visit(ParameterDeclaration declaration);

    void Visit(VariableDeclaration declaration);

    void Visit(ProgramNode program);
}