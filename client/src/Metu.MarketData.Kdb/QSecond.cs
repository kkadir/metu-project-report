﻿using System;

namespace Metu.MarketData.Kdb
{
    /// <summary>
    ///     Represents q second type.
    /// </summary>
    public struct QSecond : IDateTime
    {
        private const string NullRepresentation = "0Nv";
        private DateTime _datetime;

        /// <summary>
        ///     Creates new QSecond instance using specified q second value.
        /// </summary>
        /// <param name="value">a count of seconds from midnight</param>
        public QSecond(int value)
            : this()
        {
            Value = value;
        }

        /// <summary>
        ///     Creates new QSecond instance using specified DateTime.
        /// </summary>
        /// <param name="datetime">DateTime to be set</param>
        public QSecond(DateTime datetime)
            : this()
        {
            _datetime = datetime;
            Value = (int) datetime.TimeOfDay.TotalSeconds;
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
        ///     Converts QSecond object to .NET DateTime type.
        /// </summary>
        public DateTime ToDateTime()
        {
            if (_datetime == DateTime.MinValue)
            {
                _datetime = new DateTime().AddSeconds(Value);
            }
            return _datetime;
        }

        /// <summary>
        ///     Returns the string representation of QSecond instance.
        /// </summary>
        public override string ToString()
        {
            if (Value == int.MinValue) return NullRepresentation;
            var seconds = Math.Abs(Value);
            var minutes = seconds/60;
            var hours = minutes/60;

            return string.Format("{0}{1:00}:{2:00}:{3:00}", Value < 0 ? "-" : "", hours, minutes%60, seconds%60);
        }

        /// <summary>
        ///     Returns a QSecond represented by a given string.
        /// </summary>
        /// <param name="date">string representation</param>
        /// <returns>a QSecond instance</returns>
        public static QSecond FromString(string date)
        {
            if (string.IsNullOrWhiteSpace(date) || date.Equals(NullRepresentation))
            {
                return new QSecond(int.MinValue);
            }

            try
            {
                var parts = date.Split(':');
                var hours = int.Parse(parts[0]);
                var minutes = int.Parse(parts[1]);
                var seconds = int.Parse(parts[2]);

                return new QSecond((seconds + 60*minutes + 3600*Math.Abs(hours))*('-' == date[0] ? -1 : 1));
            }
            catch (Exception e)
            {
                throw new ArgumentException("Cannot parse QSecond from: " + date, e);
            }
        }

        /// <summary>
        ///     Determines whether the specified System.Object is equal to the current QSecond.
        /// </summary>
        /// <param name="obj">The System.Object to compare with the current QSecond.</param>
        /// <returns>true if the specified System.Object is equal to the current QSecond; otherwise, false</returns>
        public override bool Equals(object obj)
        {
            if (!(obj is QSecond))
            {
                return false;
            }

            return Value == ((QSecond) obj).Value;
        }

        public override int GetHashCode()
        {
            return Value;
        }
    }
}