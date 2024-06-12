namespace Metu.MarketData.Kdb
{
    /// <summary>
    ///     Common interface for the q table types.
    /// </summary>
    public interface IQTable
    {
        /// <summary>
        ///     Gets a number of rows in table.
        /// </summary>
        int RowsCount { get; }

        /// <summary>
        ///     Gets a number of columns in table.
        /// </summary>
        int ColumnsCount { get; }

        /// <summary>
        /// Checks whether table contains column with given name.
        /// </summary>
        /// <param name="column">Name of the column</param>
        /// <returns>true if table contains column with given name, false otherwise</returns>
        bool HasColumn(string column);

        /// <summary>
        ///     Gets a column index for specified name.
        /// </summary>
        /// <param name="column">Name of the column</param>
        /// <returns>0 based column index
        int GetColumnIndex(string column);

    }
}