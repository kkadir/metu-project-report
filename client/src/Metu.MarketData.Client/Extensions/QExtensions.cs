using System;
using Metu.MarketData.Kdb;

namespace Metu.MarketData.Client.Extensions;

public static class QExtensions
{
    private const string DateTimeFormat = "yyyy.MM.dd'T'HH:mm:ss.fff";
    private const string DateFormat = "yyyy.MM.dd";
    private const string MonthFormat = "yyyy.MM'm'";
    private const string TimestampFormat = "yyyy.MM.dd'D'HH:mm:ss.fff";
    private const string NanosFormat = "{0:000000}";
    private static readonly DateTime QEpoch = new DateTime(2000, 1, 1);

    public static void QPrint(this object qObj, Action<object> writer, int connectionId, bool newLine = true)
    {
        if (connectionId > 0)
        {
            writer($"({connectionId}):\n");
        }
        var qType = (int)QTypes.GetQType(qObj);


        switch (qType)
        {
            case 10:
                writer($"{new string((char[])qObj)}");
                break;
            case < 0:
                PrintAtom(qObj, qType, writer);
                break;
            case < 20:
                PrintList(qObj, writer);
                break;
            case 98:
                PrintTable(qObj, writer);
                break;
            case 99:
                PrintDictionary(qObj, writer);
                break;
        }

        if (newLine) writer("\n");
    }

    public static void QPrint(this object qObj, Action<object> writer, bool newLine = true)
    {
        var qType = (int)QTypes.GetQType(qObj);


        switch (qType)
        {
            case 10:
                writer($"{new string((char[])qObj)}");
                break;
            case < 0:
                PrintAtom(qObj, qType, writer);
                break;
            case < 20:
                PrintList(qObj, writer);
                break;
            case 98:
                PrintTable(qObj, writer);
                break;
            case 99:
                PrintDictionary(qObj, writer);
                break;
        }

        if (newLine) writer("\n");
    }

    public static bool TryParseQMessage(this QMessage msg, out QDictionary qDic)
    {
        if (msg.Data is QDictionary data && data.Keys.Length > 0)
        {
            qDic = data;
            return true;
        }

        qDic = null;
        return false;
    }

    public static void TryParseLatencyData(QDictionary qDic, out DateTime? mdsReceived, out DateTime? mdsPublished)
    {
        mdsReceived = default;
        mdsPublished = default;
        var lastRecvIdx = Array.IndexOf(qDic.Keys, "lastRecvTime");
        var queryRecvIdx = Array.IndexOf(qDic.Keys, "queryRecvTime");
        var pubIdx = Array.IndexOf(qDic.Keys, "publishTime");
        if (lastRecvIdx == -1 && pubIdx == -1 && queryRecvIdx == -1)
            return;
        var data = qDic.Values as object[];
        try
        {
            if (lastRecvIdx > 0)
            {
                mdsReceived = ((IDateTime)data.GetValue(lastRecvIdx))?.ToDateTime();
            }
            else if (queryRecvIdx > 0)
            {
                mdsReceived = ((IDateTime)data.GetValue(queryRecvIdx))?.ToDateTime();
            }
            mdsPublished = ((IDateTime)data.GetValue(pubIdx))?.ToDateTime();
        }
        catch { }

    }

    public static (DateTime receiveTime, DateTime publishTime) QGetTime(this QMessage msg)
    {
        var fallBackDate = DateTime.UtcNow;
        DateTime? firstDate = default;
        DateTime? lastDate = default;
        if (TryParseQMessage(msg, out var parsedMessage))
        {
            TryParseLatencyData(parsedMessage, out firstDate, out lastDate);
        }

        return (firstDate ?? fallBackDate, lastDate ?? fallBackDate);
    }

    private static void PrintDictionary(object qObj, Action<object> writer)
    {
        var dict = (QDictionary)qObj;

        var keys = dict.Keys;
        var data = (object[])dict.Values;

        for (var row = 0; row < keys.Length; row++)
        {
            keys.GetValue(row).QPrint(writer, 0, false);
            writer(" | ");
            data[row].QPrint(writer, 0, true);
        }
    }

    private static void PrintTable(object qObj, Action<object> writer)
    {
        var flip = (QTable)qObj;

        var columns = flip.ColumnsCount;
        var rows = flip.RowsCount;

        writer(string.Join('\t', flip.Columns));
        writer("\n");

        for (var row = 0; row < rows; row++)
        {
            for (var column = 0; column < columns; column++)
            {
                ((Array)flip.Data.GetValue(column))?.GetValue(row).QPrint(writer, 0, false);
                writer("\t");
            }

            writer("\n");
        }
    }

    private static void PrintList(object qObj, Action<object> writer)
    {
        var qObjList = (Array)qObj;

        if (qObjList.Length <= 1)
        {
            writer(",");
        }

        for (var i = 0; i < qObjList.Length; i++)
        {
            qObjList.GetValue(i).QPrint(writer, 0, false);
            writer("\t");
        }
    }

    private static void PrintList(char[] qObj, Action<object> writer)
    {
        var qObjList = qObj;

        if (qObjList.Length <= 1)
        {
            writer(",");
        }

        for (var i = 0; i < qObjList.Length; i++)
        {
            qObjList[i].QPrint(writer, 0);
        }
    }

    private static void PrintAtom(object qObj, int qType, Action<object> writer)
    {
        switch (qType)
        {
            case -1:
                {
                    writer($"{(bool)qObj}");
                    break;
                }

            case -2:
                {
                    writer($"{(Guid)qObj}");
                    break;
                }

            case -4:
                {
                    writer($"{(byte)qObj:X2}");
                    break;
                }

            case -5:
                {
                    writer($"{(short)qObj:G}");
                    break;
                }

            case -6:
                {
                    writer($"{(int)qObj:G}");
                    break;
                }

            case -7:
                {
                    writer($"{(long)qObj:G}");
                    break;
                }

            case -8:
                {
                    writer($"{(float)qObj:F2}");
                    break;
                }

            case -9:
                {
                    writer($"{(double)qObj:F2}");
                    break;
                }

            case -10:
                {
                    writer($"{(char)qObj}");
                    break;
                }

            case -11:
                {
                    writer($"{(string)qObj}");
                    break;
                }

            case -12:
                {
                    var timestamp = (DateTime)qObj;//QEpoch.AddMilliseconds((double)(long)qObj / 1000000L);
                                                   //writer($"{timestamp.ToString(TimestampFormat) + string.Format(NanosFormat, (long)qObj % 1000000L)}");
                    writer($"{timestamp.ToString(TimestampFormat)}");
                    break;
                }

            case -13:
                {
                    var month = QEpoch.AddMonths((int)qObj);
                    writer($"{month.ToString(MonthFormat)}");
                    break;
                }

            case -14:
                {
                    //var date = QEpoch.AddDays((int)qObj);
                    var date = (QDate)qObj;
                    writer($"{date.ToDateTime().ToString(DateFormat)}");
                    break;
                }

            case -15:
                {
                    var datetime = QEpoch.AddDays((double)qObj);
                    writer($"{datetime.ToString(DateTimeFormat)}");
                    break;
                }

            case -16:
                {
                    writer($"{new TimeSpan((long)qObj / 100)}");
                    break;
                }

            case -17:
                {
                    writer(qObj);
                    //var u = (int)qObj;

                    //if (u == int.MinValue)
                    //{
                    //    writer("0Nu");
                    //}
                    //else
                    //{
                    //    writer($"{FormatDigit(u / 60)}:{FormatDigit(u % 60)}");
                    //}

                    break;
                }

            case -18:
                {
                    writer(qObj);
                    //var v = (int)qObj;

                    //if (v == int.MinValue)
                    //{
                    //    Console.Write("0Nv");
                    //}
                    //else
                    //{
                    //    Console.Write($"{FormatDigit(v / 3600)}:{FormatDigit(v / 60)}:{FormatDigit(v % 60)}");
                    //}

                    break;
                }

            case -19:
                {
                    writer(qObj);
                    //var t = (int)qObj;

                    //if (t == int.MinValue)
                    //{
                    //    Console.Write("0Nt");
                    //}
                    //else
                    //{
                    //    var millis = Math.Abs(t);
                    //    var seconds = millis / 1000;
                    //    var minutes = seconds / 60;
                    //    var hours = minutes / 60;

                    //    Console.Write(
                    //        "{0}{1:00}:{2:00}:{3:00}.{4:000}",
                    //        t < 0 ? "-" : "",
                    //        hours,
                    //        minutes % 60,
                    //        seconds % 60,
                    //        millis % 1000);
                    //}

                    break;
                }

            default:
                {
                    writer($"Not Supported Type: {(int)qObj:G}");
                    break;
                }
        }
    }

    public static string FormatDigit(this int i) => string.Format("{0:00}", i);
}
