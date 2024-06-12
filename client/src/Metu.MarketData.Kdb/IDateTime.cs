using System;

namespace Metu.MarketData.Kdb
{
    /// <summary>
    ///     Common interface for the q date/time types.
    /// </summary>
    public interface IDateTime
    {
        /// <summary>
        ///     Returns internal q representation.
        /// </summary>
        object GetValue();

        /// <summary>
        ///     Converts q date/time object to .NET DateTime type.
        /// </summary>
        DateTime ToDateTime();
    }
}