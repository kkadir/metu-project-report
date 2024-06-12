namespace Metu.MarketData.Kdb
{
    /// <summary>
    ///     Represents q function.
    ///     Note that the QFunction instances cannot be serialized to IPC protocol.
    /// </summary>
    public class QFunction
    {
        private readonly byte _qTypeCode;

        /// <summary>
        ///     Creates representation of q function with given q type code.
        /// </summary>
        protected QFunction(byte qTypeCode)
        {
            _qTypeCode = qTypeCode;
        }

        /// <summary>
        ///     Retrieve q type code connected with function.
        /// </summary>
        public byte QTypeCode
        {
            get { return _qTypeCode; }
        }

        /// <summary>
        ///     Returns a System.String that represents the current QFunction.
        /// </summary>
        /// <returns>A System.String that represents the current QFunction</returns>
        public override string ToString()
        {
            return "QFunction#" + _qTypeCode + "h";
        }

        /// <summary>
        ///     Creates representation of q function with given q type code.
        /// </summary>
        public static QFunction Create(byte qTypeCode)
        {
            return new QFunction(qTypeCode);
        }
    }
}