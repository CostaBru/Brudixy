using System;

namespace Brudixy.Tests
{
    [Serializable]
    public class UserClass : ICloneable, IComparable
    {
        protected bool Equals(UserClass other)
        {
            return Val1 == other.Val1 && Val2 == other.Val2;
        }

        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((UserClass)obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Val1, Val2);
        }

        public UserClass(int val1, int val2)
        {
            Val1 = val1;
            Val2 = val2;
        }


        public UserClass()
        {
        }

        public int Val1 { get; set; }
        public int Val2 { get; set; }

        public object Clone()
        {
            return this.MemberwiseClone();
        }

        public override string ToString()
        {
            return (Val1, Val2) + " " + nameof(UserClass);
        }

        public int CompareTo(object obj)
        {
            var userClass = (UserClass)obj;

            return (Val1, Val2).CompareTo((userClass.Val1, userClass.Val2));
        }
    }
}