using System;
using System.Xml.Linq;
using Brudixy.Converter;
using Brudixy.Interfaces;

namespace Brudixy.Tests
{
    public class ComplexUserClass :
        IComparable<ComplexUserClass>,
        IEquatable<ComplexUserClass>,
        ICloneable,
        IComparable,
        IXmlSerializable,
        IJsonSerializable
    {
        public int ID { get; set; }

        public int CompareTo(ComplexUserClass other)
        {
            if (other is null)
            {
                return 1;
            }

            return ID.CompareTo(other.ID);
        }

        public bool Equals(ComplexUserClass other)
        {
            if (other is null)
            {
                return false;
            }

            return ID.Equals(other.ID);
        }

        public object Clone()
        {
            return new ComplexUserClass() { ID = this.ID };
        }

        public override bool Equals(object obj)
        {
            if (obj is ComplexUserClass ua)
            {
                return Equals(ua);
            }

            return false;
        }

        public int CompareTo(object? obj)
        {
            if (obj is ComplexUserClass cls)
            {
                return CompareTo(cls);
            }

            return -1;
        }

        public XElement ToXml()
        {
            return new XElement("ID", ID);
        }

        public static ComplexUserClass LoadFromXml(XElement x)
        {
            var userClass = new ComplexUserClass();
            userClass.FromXml(x);
            return userClass;
        }

        public void FromXml(XElement element)
        {
            ID = Tool.IntParseFast(element.Value, 0, true);
        }

        public JElement ToJson()
        {
            return new JElement("ID", ID);
        }

        public void FromJson(JElement element)
        {
            ID = Tool.IntParseFast(element.Value?.ToString(), 0);
        }

        public override int GetHashCode()
        {
            return ID.GetHashCode();
        }
    }
}