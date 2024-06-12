using System;

namespace Metu.MarketData.Kdb
{
    /// <summary>
    ///     Represents q time type.
    /// </summary>
    public struct QTime : IDateTime
    {
        private const string NullRepresentation = "0Nt";
        private DateTime _datetime;

        /// <summary>
        ///     Creates new QTime instance using specified q time value.
        /// </summary>
        /// <param name="value">a count of milliseconds from midnight</param>
        public QTime(int value)
            : this()
        {
            Value = value;
        }

        /// <summary>
        ///     Creates new QTime instance using specified DateTime.
        /// </summary>
        /// <param name="datetime">DateTime to be set</param>
        public QTime(DateTime datetime)
            : this()
        {
            this._datetime = datetime;
            Value = (int) datetime.TimeOfDay.TotalMilliseconds;
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
        ///     Converts QTime object to .NET DateTime type.
        /// </summary>
        public DateTime ToDateTime()
        {
            if (_datetime == DateTime.MinValue)
            {
                _datetime = new DateTime().AddMilliseconds(Value);
            }
            return _datetime;
        }

        /// <summary>
        ///     Returns the string representation of QTime instance.
        /// </summary>
        public override string ToString()
        {
            if (Value != int.MinValue)
            {
                var millis = Math.Abs(Value);
                var seconds = millis/1000;
                var minutes = seconds/60;
                var hours = minutes/60;

                return string.Format("{0}{1:00}:{2:00}:{3:00}.{4:000}", Value < 0 ? "-" : "", hours, minutes%60,
                    seconds%60, millis%1000);
            }
            return NullRepresentation;
        }

        /// <summary>
        ///     Returns a QTime represented by a given string.
        /// </summary>
        /// <param name="date">string representation</param>
        /// <returns>a QTime instance</returns>
        public static QTime FromString(string date)
        {
            if (string.IsNullOrWhiteSpace(date) || date.Equals(NullRepresentation))
            {
                return new QTime(int.MinValue);
            }

            try
            {
                var parts = date.Split(':', '.');
                var hours = int.Parse(parts[0]);
                var minutes = int.Parse(parts[1]);
                var seconds = int.Parse(parts[2]);
                var millis = int.Parse(parts[3]);
                return new QTime((millis + 1000*seconds + 60000*minutes + 3600000*Math.Abs(hours))*('-' == date[0] ? -1 : 1));
            }
            catch (Exception e)
            {
                throw new ArgumentException("Cannot parse QTime from: " + date, e);
            }
        }

        /// <summary>
        ///     Determines whether the specified System.Object is equal to the current QTime.
        /// </summary>
        /// <param name="obj">The System.Object to compare with the current QTime.</param>
        /// <returns>true if the specified System.Object is equal to the current QTime; otherwise, false</returns>
        public override bool Equals(object obj)
        {
            if (!(obj is QTime))
            {
                return false;
            }

            return Value == ((QTime) obj).Value;
        }

        public override int GetHashCode()
        {
            return Value;
        }
    }
}