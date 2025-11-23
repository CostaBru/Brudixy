using System.Diagnostics;
using Brudixy.Exceptions;
using Brudixy.Expressions.Functions;

namespace Brudixy.Expressions
{
    internal sealed class LikeNode : BinaryNode
    {
        // like kinds
        internal const int match_left = 1;      // <str>*
        internal const int match_right = 2;     // *<str> 
        internal const int match_middle = 3;    // *<str>*
        internal const int match_exact = 4;    // <str> 
        internal const int match_all = 5;      // * 

        int kind;
        string pattern;

        internal LikeNode(IExpressionDataSource table, int op, ExpressionNode left, ExpressionNode right)
            : base(table, op, left, right)
        {
        }

        internal override object Eval(int? row = null,
            IReadOnlyDictionary<string, object> testValues = null)
        {
            return EvalCore(row, testValues: testValues);
        }

        public int Kind => kind;

        private object EvalCore(int? row = null, IReadOnlyDictionary<string, object> testValues = null)
        {
            object vRight;
            // 
            object vLeft;

            vLeft = left.Eval(row, testValues);

            string substring;

            if ((vLeft == null))
            {
                return null;
            }

            if (pattern == null)
            {
                vRight = right.Eval(row, testValues);

                if (!(vRight is string))
                {
                    SetTypeMismatchError(op, vLeft, vRight);
                }

                if (vRight == null)
                {
                    return null;
                }

                string rightStr = (string) ConvertFunction.ChangeType(vRight, typeof(string));

                substring = GetPattern(rightStr);

                if (right.IsConstant())
                {
                    pattern = substring;
                }
            }
            else
            {
                substring = pattern;
            }

            if (!(vLeft is string))
            {
                SetTypeMismatchError(op, vLeft, pattern);
            }

            // WhiteSpace Chars Include : 0x9, 0xA, 0xB, 0xC, 0xD, 0x20, 0xA0, 0x2000, 0x2001, 0x2002, 0x2003, 0x2004, 0x2005, 0x2006, 0x2007, 0x2008, 0x2009, 0x200A, 0x200B, 0x3000, and 0xFEFF. 
            char[] trimChars = new char[2] {(char) 0x20, (char) 0x3000};
            string tempStr;

            tempStr = (string) vLeft;

            string s1 = (tempStr).TrimEnd(trimChars);

            switch (kind)
            {
                case match_all:
                    return true;
                case match_exact:
                    return (0 == CoreDataTable.CompareStrings(s1, substring));
                case match_middle:
                    return (0 <= CoreDataTable.IndexOfString(s1, substring));
                case match_left:
                    return (0 == CoreDataTable.IndexOfString(s1, substring));
                case match_right:
                    string s2 = substring.TrimEnd(trimChars);
                    return CoreDataTable.IsSuffixString(s1, s2);
                default:
                    Debug.Assert(false, "Unexpected LIKE kind");
                    return null;
            }
        }

        public string GetPattern(string rightStr)
        {
            string substring;
            // need to convert like pattern to a string

            // Parce the original pattern, and get the constant part of it.. 
            substring = AnalyzePattern(rightStr);
            return substring;
        }

        internal string AnalyzePattern(string pat)
        {

            int length = pat.Length;
            char[] patchars = new char[length + 1];
            pat.CopyTo(0, patchars, 0, length);
            patchars[length] = (char)0;
            string substring = null;

            char[] constchars = new char[length + 1];
            int newLength = 0;

            int stars = 0;

            int i = 0;

            while (i < length)
            {

                if (patchars[i] == '*' || patchars[i] == '%')
                {

                    // replace conseq. * or % with one..
                    while ((patchars[i] == '*' || patchars[i] == '%') && i < length)
                    {
                        i++;
                    }

                    // we allowing only *str* pattern 
                    if ((i < length && newLength > 0) || stars >= 2)
                    {
                        // we have a star inside string constant..
                        throw ExprException.InvalidPattern(pat, $"Only two * chars allowed in pattern, but {stars} occurrences found.");
                    }
                    stars++;

                }
                else if (patchars[i] == '[')
                {
                    i++;
                    if (i >= length)
                    {
                        throw ExprException.InvalidPattern(pat, "Closing ] character wasn't found.");
                    }
                    constchars[newLength++] = patchars[i++];

                    if (i >= length)
                    {
                        throw ExprException.InvalidPattern(pat, "Closing ] character wasn't found.");
                    }

                    if (patchars[i] != ']')
                    {
                        throw ExprException.InvalidPattern(pat, "Closing ] character wasn't found.");
                    }
                    i++;
                }
                else
                {
                    constchars[newLength++] = patchars[i];
                    i++;
                }
            }

            substring = new string(constchars, 0, newLength);

            if (stars == 0)
            {
                kind = match_exact;
            }
            else
            {
                if (newLength > 0)
                {
                    if (patchars[0] == '*' || patchars[0] == '%')
                    {

                        if (patchars[length - 1] == '*' || patchars[length - 1] == '%')
                        {
                            kind = match_middle;
                        }
                        else
                        {
                            kind = match_right;
                        }
                    }
                    else
                    {
                        Debug.Assert(patchars[length - 1] == '*' || patchars[length - 1] == '%', "Invalid LIKE pattern formed.. ");
                        kind = match_left;
                    }
                }
                else
                {
                    kind = match_all;
                }
            }
            return substring;
        }
    }
}