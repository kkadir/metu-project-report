using System;
using System.IO;
using System.Text;
using static Metu.MarketData.Kdb.Utils;

namespace Metu.MarketData.Kdb
{
    internal sealed class BinaryReader
    {
        private readonly byte[] _rawData;

        private ByteConverter _byteConverter;
        private Endianess _endianess;

        public BinaryReader(byte[] rawData, Endianess endianess = Endianess.LittleEndian)
        {
            _rawData = rawData;

            Position = 0;
            Endianess = endianess;
        }

        public Endianess Endianess
        {
            get
            {
                return _endianess;
            }
            set
            {
                _endianess = value;

                if (value == Endianess.LittleEndian)
                {
                    _byteConverter = LittleEndianByteConverter;
                }
                else
                {
                    _byteConverter = BigEndianByteConverter;
                }
            }
        }

        public int Position { get; private set; }

        public bool ReadBoolean()
        {
            return _rawData[Position++] == 1;
        }

        public byte ReadByte()
        {
            return _rawData[Position++];
        }

        public sbyte ReadSByte()
        {
            return unchecked((sbyte) _rawData[Position++]);
        }

        public short ReadInt16()
        {
            var result = unchecked((short) _byteConverter(_rawData, Position, 2));
            Position += 2;

            return result;
        }

        public int ReadInt32()
        {
            var result = unchecked((int) _byteConverter(_rawData, Position, 4));
            Position += 4;

            return result;
        }

        public long ReadInt64()
        {
            var result = unchecked(_byteConverter(_rawData, Position, 8));
            Position += 8;

            return result;
        }

        public float ReadSingle()
        {
            return BitConverter.ToSingle(BitConverter.GetBytes(ReadInt32()), 0);
        }

        public double ReadDouble()
        {
            return BitConverter.Int64BitsToDouble(ReadInt64());
        }

        public char ReadChar()
        {
            return (char) (ReadByte() & 0xFF);
        }

        public byte[] ReadBytes(int count)
        {
            var bytes = new byte[count];

            Array.Copy(_rawData, Position, bytes, 0, count);
            Position += count;

            return bytes;
        }

        public Guid ReadGuid()
        {
            var block = new byte[16];

            for (var i = 0; i < 16; i++)
            {
                block[Constants.GuidByteOrder[i]] = ReadByte();
            }

            return new Guid(block);
        }

        public string ReadString(int count, Encoding encoding)
        {
            Position += count;

            return encoding.GetString(_rawData, Position - count, count);
        }

        public string ReadSymbol(Encoding encoding)
        {
            var i = Position;

            for (; _rawData[i] != 0; ++i)
            {
            }

            var count = i - Position;
            var symbol = encoding.GetString(_rawData, Position, count);
            Position += count + 1;

            return symbol;
        }

        internal void Seek(int position, SeekOrigin seekOrigin)
        {
            switch (seekOrigin)
            {
                case SeekOrigin.Current:
                    Position += position;
                    break;
                case SeekOrigin.Begin:
                    Position = position;
                    break;
                case SeekOrigin.End:
                    Position = _rawData.Length - position;
                    break;
            }
        }
    }
}