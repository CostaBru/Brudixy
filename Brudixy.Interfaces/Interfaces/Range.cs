using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using System.Xml;
using System.Xml.Linq;
using Brudixy.Converter;
using JetBrains.Annotations;

namespace Brudixy.Interfaces
{
    /// <summary>The Range class.</summary>
    /// <typeparam name="T">Generic parameter.</typeparam>
    [Serializable]
    public class Range<T>: IRange, IComparable<Range<T>>, ICloneable
        where T : IComparable<T>
    {
        protected bool Equals(Range<T> other)
        {
            return EqualityComparer<T>.Default.Equals(Minimum, other.Minimum) && EqualityComparer<T>.Default.Equals(Maximum, other.Maximum);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj.GetType() != this.GetType())
            {
                return false;
            }
            return Equals((Range<T>)obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Minimum, Maximum);
        }

        public static bool operator ==(Range<T> left, Range<T> right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(Range<T> left, Range<T> right)
        {
            return !Equals(left, right);
        }

        public Range()
        {
        }
        
        public Range(T minimum, T maximum)
        {
            if (Comparer<T>.Default.Compare(minimum, maximum) > 0)
            {
                throw new ArgumentOutOfRangeException($"Range cannot be negative: {minimum}-{maximum}.");
            }
            
            Minimum = minimum;
            Maximum = maximum;
        }
        
        /// <summary>Minimum value of the range.</summary>
        public T Minimum { get; set; }

        IComparable IRange.Maximum
        {
            get => (IComparable)Maximum;
            set => Maximum = (T)value;
        }

        IComparable IRange.Minimum
        {
            get => (IComparable)Minimum;
            set => Minimum = (T)value;
        }

        /// <summary>Maximum value of the range.</summary>
        public T Maximum { get; set; }

        /// <summary>Minimum value of the range.</summary>
        public T Begin => Minimum;

        /// <summary>Maximum value of the range.</summary>
        public T End => Maximum;
        
        /// <summary>Minimum value of the range.</summary>
        public T Start => Minimum;

        /// <summary>Maximum value of the range.</summary>
        public T Finish => Maximum;
        
        /// <summary>Minimum value of the range.</summary>
        public T Min => Minimum;

        /// <summary>Maximum value of the range.</summary>
        public T Max => Maximum;

        public T Length
        {
            get
            {
                if (IsEmpty())
                {
                    return default;
                }

                var lengthD = GetDoubleLength();

                T length = default;

                GenericConverter.ConvertTo(ref lengthD, ref length);

                return length;
            }
        }
        
        public double? GetLenghtD()
        {
            try
            {
                if (IsEmpty())
                {
                    return 0;
                }
                
                return GetDoubleLength();
            }
            catch
            {
                return null;
            }
        }

        private double GetDoubleLength()
        {
            var min = Minimum;
            var max = Maximum;

            double minD = 0;
            double maxD = 0;

            GenericConverter.ConvertTo(ref min, ref minD);
            GenericConverter.ConvertTo(ref max, ref maxD);

            var lengthD = maxD - minD;
            return lengthD;
        }

        /// <summary>Presents the Range in readable format.</summary>
        /// <returns>String representation of the Range</returns>
        public override string ToString()
        {
            if (IsEmpty())
            {
                return "EMPTY";
            }
            
            return string.Format("[{0} - {1}] ({2})", this.Minimum, this.Maximum, Length);
        }

        object ICloneable.Clone()
        {
            return this.MemberwiseClone();
        }

        public string ToString(string format, IFormatProvider formatProvider)
        {
            if (Minimum is IFormattable fmtMin && Maximum is IFormattable fmtMax && Length is IFormattable fmtLen)
            {
                return string.Format("[{0} - {1}] ({2})", fmtMin.ToString(format, formatProvider), fmtMax.ToString(format, formatProvider),  fmtLen.ToString(format, formatProvider));
            }
            
            return ToString();
        }
        
        public string ToString([NotNull] Func<T, string> customFormatter)
        {
            if (customFormatter == null)
            {
                throw new ArgumentNullException(nameof(customFormatter));
            }
            
            return string.Format("[{0} - {1}] ({2})", customFormatter(this.Minimum), customFormatter(this.Maximum), customFormatter(Length));
        }

        public int CompareTo(object obj)
        {
            var range = (Range<T>)obj;

            var compareTo = Minimum.CompareTo(range.Minimum);

            if (compareTo != 0)
            {
                return compareTo;
            }
            
            return Maximum.CompareTo(range.Maximum);
        }

        /// <summary>Determines if the range is valid.</summary>
        /// <returns>True if range is valid, else false</returns>
        public bool IsValid()
        {
            return this.Minimum.CompareTo(this.Maximum) <= 0;
        }
        
        /// <summary>Determines if the range is empty.</summary>
        /// <returns>True if range is empty, else false</returns>
        public bool IsEmpty()
        {
            return this.Minimum.CompareTo(this.Maximum) == 0;
        }

        bool IRange.ContainsValue(IComparable value)
        {
            return ContainsValue((T)value);
        }

        bool IRange.IsInsideRange(IRange range)
        {
            return IsInsideRange((Range<T>)range);
        }

        bool IRange.ContainsRange(IRange range)
        {
            return ContainsRange((Range<T>)range);
        }

        bool IRange.Intersects(IRange range)
        {
            return Intersects((Range<T>)range);
        }

        public IRange Clone()
        {
            if (Tool.IsCloneableSupported<T>())
            {
                return new Range<T>((T)((ICloneable)this.Minimum).Clone(), (T)((ICloneable)this.Maximum).Clone());
            }
            
            return (IRange)this.MemberwiseClone();
        }

        /// <summary>Determines if the provided value is inside the range.</summary>
        /// <param name="value">The value to test</param>
        /// <returns>True if the value is inside Range, else false</returns>
        public bool ContainsValue(T value)
        {
            return (this.Minimum.CompareTo(value) <= 0) && (value.CompareTo(this.Maximum) <= 0);
        }

        /// <summary>Determines if this Range is inside the bounds of another range.</summary>
        /// <param name="range">The parent range to test on</param>
        /// <returns>True if range is inclusive, else false</returns>
        public bool IsInsideRange(Range<T> range)
        {
            return this.IsValid() && range.IsValid() && range.ContainsValue(this.Minimum) && range.ContainsValue(this.Maximum);
        }

        /// <summary>Determines if another range is inside the bounds of this range.</summary>
        /// <param name="range">The child range to test</param>
        /// <returns>True if range is inside, else false</returns>
        public bool ContainsRange(Range<T> range)
        {
            return this.IsValid() && range.IsValid() && this.ContainsValue(range.Minimum) && this.ContainsValue(range.Maximum);
        }
        
        /// <summary>Determines if another range has intersection with this range or this range is inside another one..</summary>
        /// <param name="range">The child range to test</param>
        /// <returns>True if two ranges has intersection.</returns>
        public bool Intersects(Range<T> range)
        {
            return this.IsValid() && range.IsValid() && (this.ContainsValue(range.Minimum) || this.ContainsValue(range.Maximum) || range.ContainsRange(this));
        }
        
        public XElement ToXml()
        {
            var (min, max) = GetStrMinMax();
            
            return new XElement("Range", new XAttribute("Min", min), new XAttribute("Max", max));
        }

        public void FromXml(XElement element)
        {
            if (element.Name != "Range")
            {
                throw new ArgumentException($"Cannot parse Range xml element '{element.Name}'.");
            }
            
            var minStr = element.Attribute("Min")?.Value;
            var maxStr = element.Attribute("Max")?.Value;

            Minimum = (T)Convert.ChangeType(minStr, typeof(T));
            Maximum = (T)Convert.ChangeType(maxStr, typeof(T));
        }

        public JElement ToJson()
        {
            var jsonObject = new JElement("Range");
            
            var (min, max) = GetStrMinMax();
           
            jsonObject.AddAttribute(new JAttribute("Min", min));
            jsonObject.AddAttribute(new JAttribute("Max", max));

            return jsonObject;
        }

        private (object min, object max) GetStrMinMax()
        {
            object min = Minimum;
            object max = Maximum;

            if (typeof(T) == typeof(DateTime))
            {
                min = XmlConvert.ToString(((DateTime)(object)Minimum), XmlDateTimeSerializationMode.Utc);
                max = XmlConvert.ToString(((DateTime)(object)Maximum), XmlDateTimeSerializationMode.Utc);
            }

            return (min, max);
        }

        public void FromJson(JElement element)
        {
            string getProp(JElement el, string name)
            {
                return el.GetAttribute(name)?.ToString();
            }
            
            var jn = getProp(element, "Name");
            
            if (jn != "Range")
            {
                throw new ArgumentException($"Cannot parse Range json element '{jn}'.");
            }
            
            var minStr = getProp(element, "Min");
            var maxStr = getProp(element, "Max");

            Minimum = (T)Convert.ChangeType(minStr, typeof(T));
            Maximum = (T)Convert.ChangeType(maxStr, typeof(T));
        }

        public int CompareTo(Range<T> other)
        {
            if (ReferenceEquals(this, other))
            {
                return 0;
            }

            if (ReferenceEquals(null, other))
            {
                return 1;
            }
            var minimumComparison = Minimum.CompareTo(other.Minimum);
            if (minimumComparison != 0)
            {
                return minimumComparison;
            }
            return Maximum.CompareTo(other.Maximum);
        }
        
        /// <summary>
        /// Determines if two intervals overlap (i.e. if this interval starts before the other ends and it finishes after the other starts)
        /// </summary>
        /// <param name="other">The other.</param>
        /// <returns>
        ///   <c>true</c> if the specified other is overlapping; otherwise, <c>false</c>.
        /// </returns>
        public bool OverlapsWith(Range<T> other)
        {
            return this.Start.CompareTo(other.End) < 0 && this.End.CompareTo(other.Start) > 0;
        }
    }
}