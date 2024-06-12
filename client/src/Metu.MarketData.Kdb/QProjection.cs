using System;

namespace Metu.MarketData.Kdb
{
    /// <summary>
    ///     Represents a q projection.
    /// </summary>
    public sealed class QProjection : QFunction
    {
        private readonly Array _parameters;

        /// <summary>
        ///     Creates new QProjection instance with given parameters.
        /// </summary>
        public QProjection(Array parameters)
            : base((byte) QType.Projection)
        {
            this._parameters = parameters;
        }

        /// <summary>
        ///     Gets parameters of a q lambda expression.
        /// </summary>
        public Array Parameters
        {
            get { return _parameters; }
        }

        /// <summary>
        ///     Determines whether the specified System.Object is equal to the current QProjection.
        /// </summary>
        /// <param name="obj">The System.Object to compare with the current QProjection.</param>
        /// <returns>true if the specified System.Object is equal to the current QProjection; otherwise, false</returns>
        public override bool Equals(object obj)
        {
            var p = obj as QProjection;
            if (p == null)
            {
                return false;
            }

            return Utils.ArrayEquals(_parameters, p._parameters);
        }

        /// <summary>
        ///     Determines whether the specified QProjection is equal to the current QProjection.
        /// </summary>
        /// <param name="l">The QProjection to compare with the current QProjection.</param>
        /// <returns>true if the specified QProjection is equal to the current QProjection; otherwise, false</returns>
        public bool Equals(QProjection p)
        {
            if (p == null)
            {
                return false;
            }

            return Utils.ArrayEquals(_parameters, p._parameters);
        }

        /// <summary>
        ///     Serves as a hash function for a QProjection type.
        /// </summary>
        /// <returns>A hash code for the current QProjection</returns>
        public override int GetHashCode()
        {
            return _parameters == null ? 0 : _parameters.Length;
        }

        /// <summary>
        ///     Returns a System.String that represents the current QLambda.
        /// </summary>
        /// <returns>A System.String that represents the current QLambda</returns>
        public override string ToString()
        {
            return "QProjection: " + (_parameters == null ? "<null>" : Utils.ArrayToString(_parameters));
        }
    }
}