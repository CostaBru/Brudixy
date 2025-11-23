namespace Brudixy.Expressions
{
    /// <devdoc>
    ///     ExpressionParser: expression node types 
    /// </devdoc>
    enum Nodes
    {
        Noop = 0,

        Unop = 1, /* Unary operator */

        UnopSpec = 2, /* Special unop: IFF does not eval args */

        Binop = 3, /* Binary operator */

        BinopSpec = 4, /* Special binop: BETWEEN, IN does not eval args */

        Zop = 5, /* "0-ary operator" - intrinsic constant. */

        Call = 6, /* Function call or rhs of IN or IFF */

        Const = 7, /* Constant value */

        Name = 8, /* Identifier */

        Paren = 9, /* Parentheses */

        Conv = 10, /* Type conversion */
    }
}