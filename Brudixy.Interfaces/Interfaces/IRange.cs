using System;

namespace Brudixy.Interfaces
{
    public interface IRange : IXmlSerializable, IJsonSerializable, IComparable, IFormattable
    {
        /// <summary>Minimum value of the range.</summary>
        public IComparable Minimum { get; set; }

        /// <summary>Maximum value of the range.</summary>
        public IComparable Maximum { get; set; }

        /// <summary>Minimum value of the range.</summary>
        public IComparable Begin => Minimum;

        /// <summary>Maximum value of the range.</summary>
        public IComparable End => Maximum;
        
        /// <summary>Minimum value of the range.</summary>
        public IComparable Start => Minimum;

        /// <summary>Maximum value of the range.</summary>
        public IComparable Finish => Maximum;
        
        /// <summary>Minimum value of the range.</summary>
        public IComparable Min => Minimum;

        /// <summary>Maximum value of the range.</summary>
        public IComparable Max => Maximum;

        /// <summary>Determines if the range is valid.</summary>
        /// <returns>True if range is valid, else false</returns>
        bool IsValid();

        /// <summary>Determines if the range is empty.</summary>
        /// <returns>True if range is empty, else false</returns>
        bool IsEmpty();

        /// <summary>Determines if the provided value is inside the range.</summary>
        /// <param name="value">The value to test</param>
        /// <returns>True if the value is inside Range, else false</returns>
        bool ContainsValue(IComparable value);

        /// <summary>Determines if this Range is inside the bounds of another range.</summary>
        /// <param name="range">The parent range to test on</param>
        /// <returns>True if range is inclusive, else false</returns>
        bool IsInsideRange(IRange range);

        /// <summary>Determines if another range is inside the bounds of this range.</summary>
        /// <param name="range">The child range to test</param>
        /// <returns>True if range is inside, else false</returns>
        bool ContainsRange(IRange range);

        /// <summary>Determines if another range has intersection with this range or this range is inside another one..</summary>
        /// <param name="range">The child range to test</param>
        /// <returns>True if two ranges has intersection.</returns>
        bool Intersects(IRange range);

        /// <summary>
        /// Clones current instance.
        /// </summary>
        /// <returns></returns>
        IRange Clone();

        /// <summary>
        /// Gets length as a double type value.
        /// </summary>
        /// <returns></returns>
        double? GetLenghtD();
    }
}