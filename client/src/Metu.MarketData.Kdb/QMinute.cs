﻿using System;

namespace Metu.MarketData.Kdb
{
    /// <summary>
    ///     Represents q minute type.
    /// </summary>
    public struct QMinute : IDateTime
    {
        private const string NullRepresentation = "0Nu";
        private DateTime _datetime;

        /// <summary>
        ///     Creates new QMinute instance using specified q minute value.
        /// </summary>
        /// <param name="value">a count of minutes from midnight</param>
        public QMinute(int value)
            : this()
        {
            Value = value;
        }

        /// <summary>
        ///     Creates new QMinute instance using specified q minute value.
        /// </summary>
        /// <param name="datetime">DateTime to be set</param>
        public QMinute(DateTime datetime)
            : this()
        {
            this._datetime = datetime;
            Value = (int) datetime.TimeOfDay.TotalMinutes;
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
        ///     Converts QMinute object to .NET DateTime type.
        /// </summary>
        public DateTime ToDateTime()
        {
            if (_datetime == DateTime.MinValue)
            {
                _datetime = new DateTime().AddMinutes(Value);
            }
            return _datetime;
        }

        /// <summary>
        ///     Returns the string representation of QMinute instance.
        /// </summary>
        public override string ToString()
        {
            if (Value != int.MinValue)
            {
                var minutes = Math.Abs(Value);
                var hours = minutes/60;

                return string.Format("{0}{1:00}:{2:00}", Value < 0 ? "-" : "", hours, minutes%60);
            }
            return NullRepresentation;
        }

        /// <summary>
        ///     Returns a QMinute represented by a given string.
        /// </summary>
        /// <param name="date">string representation</param>
        /// <returns>a QMinute instance</returns>
        public static QMinute FromString(string date)
        {
            if (string.IsNullOrWhiteSpace(date) || date.Equals(NullRepresentation))
            {
                return new QMinute(int.MinValue);
            }

            try
            {
                var parts = date.Split(':');
                var hours = int.Parse(parts[0]);
                var minutes = int.Parse(parts[1]);

                return new QMinute((minutes + 60*Math.Abs(hours))*('-' == date[0] ? -1 : 1));
            }
            catch (Exception e)
            {
                throw new ArgumentException("Cannot parse QMinute from: " + date, e);
            }
        }

        /// <summary>
        ///     Determines whether the specified System.Object is equal to the current QMinute.
        /// </summary>
        /// <param name="obj">The System.Object to compare with the current QMinute.</param>
        /// <returns>true if the specified System.Object is equal to the current QMinute; otherwise, false</returns>
        public override bool Equals(object obj)
        {
            if (!(obj is QMinute))
            {
                return false;
            }

            return Value == ((QMinute) obj).Value;
        }

        public override int GetHashCode()
        {
            return Value;
        }
    }
}