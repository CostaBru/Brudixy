namespace Brudixy.Expressions
{

    internal enum Tokens
    {
        None = 0,
        Name = 1, /* Identifier */
        Numeric = 2,
        Decimal = 3,
        Float = 4,
        BinaryConst = 5, /* Binary Constant e.g. 0x12ef */
        StringConst = 6,
        Date = 7,
        ListSeparator = 8, /* List Tokens.ListSeparator/Comma */
        LeftParen = 9, /* '('; */
        RightParen = 10, /* ')'; */
        ZeroOp = 11, /* 0-array operator like "NULL" */
        UnaryOp = 12,
        BinaryOp = 13,
        Child = 14,
        Parent = 15,
        Dot = 16,
        Unknown = 17, /* do not understand the token */
        EOS = 18, /* End of string */
    }
}