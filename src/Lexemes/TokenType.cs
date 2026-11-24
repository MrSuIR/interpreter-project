namespace Lait.Lexemes;

/// <summary>
/// Виды токенов языка Lait.
/// </summary>
public enum TokenType
{
    Bool,
    Break,
    Continue,
    Else,
    False,
    Float,
    For,
    Func,
    If,
    Int,
    New,
    Return,
    String,
    Struct,
    True,
    Void,
    While,

    Identifier,
    IntLiteral,
    FloatLiteral,
    StringLiteral,

    Plus,
    Minus,
    Multiply,
    Divide,
    Remainder,
    Assign,
    Equal,
    NotEqual,
    LessThan,
    LessThanOrEqual,
    GreaterThan,
    GreaterThanOrEqual,
    And,
    Or,
    Not,
    Dot,

    OpenParenthesis,
    CloseParenthesis,
    OpenBrace,
    CloseBrace,
    OpenBracket,
    CloseBracket,
    Comma,
    Colon,
    Semicolon,

    Error,
    EndOfFile,
}