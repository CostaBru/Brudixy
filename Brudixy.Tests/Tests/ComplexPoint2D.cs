using System;
using System.Xml.Linq;
using Brudixy.Converter;
using Brudixy.Interfaces;

namespace Brudixy.Tests
{

    public class ComplexPoint2D :
        IComparable<ComplexPoint2D>,
        IEquatable<ComplexPoint2D>,
        ICloneable,
        IComparable,
        IXmlSerializable,
        IJsonSerializable,
        IReadonlySupported
    {
        public override int GetHashCode()
        {
            return HashCode.Combine(X, Y);
        }

        public int X { get; private set; }
        public int Y { get; private set; }

        public ComplexPoint2D()
        {
        }

        public ComplexPoint2D(int x, int y)
        {
            X = x;
            Y = y;
        }

        public int CompareTo(object? obj)
        {
            if (obj is ComplexPoint2D cls)
            {
                return CompareTo(cls);
            }

            return -1;
        }

        public int CompareTo(ComplexPoint2D other)
        {
            if (other is null)
            {
                return 1;
            }

            var compareTo = X.CompareTo(other.X);

            if (compareTo != 0)
            {
                return compareTo;
            }

            return Y.CompareTo(other.Y);
        }

        public bool Equals(ComplexPoint2D other)
        {
            if (other is null)
            {
                return false;
            }

            return X.Equals(other.X) && Y.Equals(other.Y);
        }

        public object Clone()
        {
            return new ComplexPoint2D() { X = this.X, Y = this.Y };
        }

        public override bool Equals(object obj)
        {
            if (obj is ComplexPoint2D ua)
            {
                return Equals(ua);
            }

            return false;
        }

        public XElement ToXml()
        {
            return new XElement("P", new XAttribute("X", X), new XAttribute("Y", Y));
        }

        public void FromXml(XElement element)
        {
            X = Tool.IntParseFast(element.Attribute("X").Value, 0, true);
            Y = Tool.IntParseFast(element.Attribute("Y").Value, 0, true);
        }

        public JElement ToJson()
        {
            var jObject = new JElement("Point2D");

            jObject.AddAttribute(new JAttribute("X", X));
            jObject.AddAttribute(new JAttribute("Y", Y));

            return jObject;
        }

        public void FromJson(JElement element)
        {
            X = Tool.IntParseFast(element.GetAttribute("X").ToString(), 0, true);
            Y = Tool.IntParseFast(element.GetAttribute("Y").ToString(), 0, true);
        }

        public bool IsReadOnly => true;
    }
}