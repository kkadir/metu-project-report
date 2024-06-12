﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Metu.MarketData.Kdb
{
    /// <summary>
    ///     Represents a q keyed table type.
    /// </summary>
    public sealed class QKeyedTable : IEnumerable<QKeyedTable.KeyValuePair>, IQTable
    {
        private readonly QTable _keys;
        private readonly QTable _values;

        /// <summary>
        ///     Creates new QKeyedTable instance with given keys and values arrays.
        /// </summary>
        public QKeyedTable(QTable keys, QTable values)
        {
            if (keys == null || keys.RowsCount == 0)
            {
                throw new ArgumentException("Keys table cannot be null or 0-length");
            }

            if (values == null || values.RowsCount == 0)
            {
                throw new ArgumentException("Values table cannot be null or 0-length");
            }

            if (keys.RowsCount != values.RowsCount)
            {
                throw new ArgumentException("Keys and value tables cannot have different length");
            }

            _keys = keys;
            _values = values;
        }

        /// <summary>
        ///     Initializes a new instance of the QKeyedTable with specified column names and data matrix.
        /// </summary>
        public QKeyedTable(IList<string> columns, ICollection<string> keyColumns, Array data)
        {
            if (columns == null || columns.Count == 0)
            {
                throw new ArgumentException("Columns array cannot be null or 0-length");
            }

            if (keyColumns == null || keyColumns.Count == 0)
            {
                throw new ArgumentException("Key columns array cannot be null or 0-length");
            }

            if (data == null || data.Length == 0)
            {
                throw new ArgumentException("Data matrix cannot be null or 0-length");
            }

            if (columns.Count != data.Length)
            {
                throw new ArgumentException("Columns array and data matrix cannot have different length");
            }

            if (data.Cast<object>().Any(col => !col.GetType().IsArray))
            {
                throw new ArgumentException("Non array column found in data matrix");
            }

            if (keyColumns.Any(keyCol => !columns.Contains(keyCol)))
            {
                throw new ArgumentException("Non array column found in data matrix");
            }

            var keyIndices = new SortedSet<int>();
            for (var i = 0; i < columns.Count; i++)
            {
                if (keyColumns.Contains(columns[i]))
                {
                    keyIndices.Add(i);
                }
            }

            var keyArrays = new object[keyIndices.Count];
            var keyHeaders = new string[keyIndices.Count];
            var dataArrays = new object[data.Length - keyIndices.Count];
            var dataHeaders = new string[data.Length - keyIndices.Count];

            var ki = 0;
            var di = 0;

            for (var i = 0; i < data.Length; i++)
            {
                if (keyIndices.Contains(i))
                {
                    keyHeaders[ki] = columns[i];
                    keyArrays[ki++] = data.GetValue(i);
                }
                else
                {
                    dataHeaders[di] = columns[i];
                    dataArrays[di++] = data.GetValue(i);
                }
            }

            _keys = new QTable(keyHeaders, keyArrays);
            _values = new QTable(dataHeaders, dataArrays);
        }

        /// <summary>
        ///     Gets an array with keys.
        /// </summary>
        public QTable Keys
        {
            get { return _keys; }
        }

        /// <summary>
        ///     Gets an array with values.
        /// </summary>
        public QTable Values
        {
            get { return _values; }
        }

        /// <summary>
        ///     Returns an enumerator that iterates through a table keys and values.
        /// </summary>
        /// <returns>An QKeyedTableEnumerator object that can be used to iterate through the table</returns>
        public IEnumerator<QKeyedTable.KeyValuePair> GetEnumerator()
        {
            return new QKeyedTableEnumerator(this);
        }

        /// <summary>
        ///     Returns an enumerator that iterates through rows in a table.
        /// </summary>
        /// <returns>An QKeyedTableEnumerator object that can be used to iterate through the table</returns>
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        /// <summary>
        /// Checks whether table contains column with given name.
        /// </summary>
        /// <param name="column">Name of the column</param>
        /// <returns>true if table contains column with given name, false otherwise</returns>
        public bool HasColumn(string column)
        {
            return _keys.HasColumn(column) || _values.HasColumn(column);
        }

        /// <summary>
        ///     Gets a column index for specified name.
        /// </summary>
        /// <param name="column">Name of the column</param>
        /// <returns>0 based column index
        public int GetColumnIndex(string column)
        {
            if (_keys.HasColumn(column))
            {
                return _keys.GetColumnIndex(column);
            }
            else
            {
                return _values.GetColumnIndex(column) + _keys.ColumnsCount;
            }
        }

        /// <summary>
        ///     Determines whether the specified System.Object is equal to the current QKeyedTable.
        /// </summary>
        /// <param name="obj">The System.Object to compare with the current QKeyedTable.</param>
        /// <returns>true if the specified System.Object is equal to the current QKeyedTable; otherwise, false</returns>
        public override bool Equals(object obj)
        {
            var kt = obj as QKeyedTable;
            if (kt == null)
            {
                return false;
            }

            return Keys.Equals(kt.Keys) && Values.Equals(kt.Values);
        }

        /// <summary>
        ///     Determines whether the specified QKeyedTable is equal to the current QKeyedTable.
        /// </summary>
        /// <param name="kt">The QKeyedTable to compare with the current QKeyedTable.</param>
        /// <returns>true if the specified QKeyedTable is equal to the current QKeyedTable; otherwise, false</returns>
        public bool Equals(QKeyedTable kt)
        {
            if (kt == null)
            {
                return false;
            }

            return Keys.Equals(kt.Keys) && Values.Equals(kt.Values);
        }

        public override int GetHashCode()
        {
            return 31*Keys.GetHashCode() + Values.GetHashCode();
        }

        /// <summary>
        ///     Returns a System.String that represents the current QKeyedTable.
        /// </summary>
        /// <returns>A System.String that represents the current QKeyedTable</returns>
        public override string ToString()
        {
            return "QKeyedTable: " + Keys + "|" + Values;
        }

        /// <summary>
        ///     Defines a key/value pair that can be retrieved.
        /// </summary>
        public struct KeyValuePair
        {
            private readonly QKeyedTable _kt;
            public int Index { get; internal set; }

            /// <summary>
            ///     Initializes a new instance of the KeyValuePair.
            /// </summary>
            public KeyValuePair(QKeyedTable table, int index) : this()
            {
                _kt = table;
                Index = index;
            }

            /// <summary>
            ///     Gets the key in the key/value pair.
            /// </summary>
            public object Key
            {
                get { return _kt._keys[Index]; }
            }

            /// <summary>
            ///     Gets the value in the key/value pair.
            /// </summary>
            public object Value
            {
                get { return _kt._values[Index]; }
            }
        }

        /// <summary>
        ///     Iterator over pairs [key, value] stored in a keyed table.
        /// </summary>
        private sealed class QKeyedTableEnumerator : IEnumerator<QKeyedTable.KeyValuePair>
        {
            private readonly QKeyedTable _kt;
            private int _index = 0;
            private QKeyedTable.KeyValuePair _current;

            public QKeyedTableEnumerator(QKeyedTable table)
            {
                _kt = table;
                _current = new KeyValuePair(_kt, _index);
            }

            public QKeyedTable.KeyValuePair Current
            {
                get { return _current; }
            }

            object IEnumerator.Current
            {
                get { return Current; }
            }

            public bool MoveNext()
            {
                _current.Index = _index;
                return _index++ < _kt._keys.RowsCount;
            }

            public void Reset()
            {
                _index = -1;
            }

            public void Dispose()
            {
                _index = -1;
            }
        }

        public int RowsCount
        {
            get { return _keys.RowsCount; }
        }

        public int ColumnsCount
        {
            get { return _keys.ColumnsCount + _values.ColumnsCount; }
        }
    }
}