using System;
using System.Globalization;

namespace Metu.MarketData.Kdb
{
    /// <summary>
    ///     Represents q month type.
    /// </summary>
    public struct QMonth : IDateTime
    {
        private const string DateFormat = "yyyy.MM'm'";
        private const string NullRepresentation = "0Nm";
        private DateTime _datetime;

        /// <summary>
        ///     Creates new QMonth instance using specified q month value.
        /// </summary>
        /// <param name="value">a count of months since 2000.01.01</param>
        public QMonth(int value)
            : this()
        {
            Value = value;
        }

        /// <summary>
        ///     Creates new QMonth instance using specified DateTime.
        /// </summary>
        /// <param name="datetime">DateTime to be set</param>
        public QMonth(DateTime datetime)
            : this()
        {
            _datetime = datetime;
            Value = (datetime.Year - 2000)*12 + datetime.Month - 1;
        }

        public int Value { get; private set; }

        /// <summary>
        ///     Returns internal q representation.
        /// </summary>
        public object GetValue()
        {
            return Value;
        }

        /// <summary>
        ///     Converts QMonth object to .NET DateTime type.
        /// </summary>
        public DateTime ToDateTime()
        {
            if (_datetime == DateTime.MinValue)
            {
                _datetime = new DateTime(2000, 1, 1).AddMonths(Value);
            }
            return _datetime;
        }

        /// <summary>
        ///     Returns the string representation of QMonth instance.
        /// </summary>
        public override string ToString()
        {
            if (Value != int.MinValue)
            {
                return ToDateTime().ToString(DateFormat);
            }
            return NullRepresentation;
        }

        /// <summary>
        ///     Returns a QMonth represented by a given string.
        /// </summary>
        /// <param name="date">string representation</param>
        /// <returns>a QMonth instance</returns>
        public static QMonth FromString(string date)
        {
            try
            {
                return date == null || date.Length == 0 || date.Equals(NullRepresentation)
                    ? new QMonth(int.MinValue)
                    : new QMonth(DateTime.ParseExact(date, DateFormat, CultureInfo.InvariantCulture));
            }
            catch (Exception e)
            {
                throw new ArgumentException("Cannot parse QMonth from: " + date, e);
            }
        }

        /// <summary>
        ///     Determines whether the specified System.Object is equal to the current QMonth.
        /// </summary>
        /// <param name="obj">The System.Object to compare with the current QMonth.</param>
        /// <returns>true if the specified System.Object is equal to the current QMonth; otherwise, false</returns>
        public override bool Equals(object obj)
        {
            if (!(obj is QMonth))
            {
                return false;
            }

            return Value == ((QMonth) obj).Value;
        }

        public override int GetHashCode()
        {
            return Value;
        }
    }
}