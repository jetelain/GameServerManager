// https://github.com/Braini01/bis-file-formats/
// Copyright (c) 2017 Braini01

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.IO;

namespace BIS.Core.Streams
{
    public class BinaryReaderEx : BinaryReader
    {
        //used to store file format versions (e.g. ODOL v60)
        public int Version { get; set; }

        public long Position
        {
            get
            {
                return BaseStream.Position;
            }
            set
            {
                BaseStream.Position = value;
            }
        }

        public BinaryReaderEx(Stream stream): base(stream)
        {
        }

        public uint ReadUInt24()
        {
            return (uint)(ReadByte() + (ReadByte() << 8) + (ReadByte() << 16));
        }

        public string ReadAscii(int count)
        {
            string str = "";
            for (int index = 0; index < count; ++index)
                str = str + (char)ReadByte();
            return str;
        }

        public string ReadAscii()
        {
            var n = ReadUInt16();
            return ReadAscii(n);
        }

        public string ReadAsciiz()
        {
            string str = "";
            char ch;
            while ((ch = (char)ReadByte()) != 0)
                str = str + ch;
            return str;
        }

        #region SimpleArray
        public T[] ReadArrayBase<T>(Func<BinaryReaderEx, T> readElement, int size)
        {
            var array = new T[size];
            for (int i = 0; i < size; i++)
                array[i] = readElement(this);

            return array;
        }

        public T[] ReadArray<T>(Func<BinaryReaderEx, T> readElement) => ReadArrayBase(readElement, ReadInt32());
        public float[] ReadFloatArray() => ReadArray(i => i.ReadSingle());
        public int[] ReadIntArray() => ReadArray(i => i.ReadInt32());
        public string[] ReadStringArray() => ReadArray(i => i.ReadAsciiz());

        #endregion


        public int ReadCompactInteger()
        {
            int val = ReadByte();
            if ((val & 0x80) != 0)
            {
                int extra = ReadByte();
                val += (extra - 1) * 0x80;
            }
            return val;
        }

        public byte[] ReadCompressedIndices(int bytesToRead, uint expectedSize)
        {
            var result = new byte[expectedSize];
            int outputI = 0;
            for(int i=0;i<bytesToRead;i++)
            {
                var b = ReadByte();
                if( (b & 128) != 0 )
                {
                    byte n = (byte)(b - 127);
                    byte value = ReadByte();
                    for (int j = 0; j < n; j++)
                        result[outputI++] = value;
                }
                else
                {
                    for (int j = 0; j < b + 1; j++)
                        result[outputI++] = ReadByte();
                }
            }

            Debug.Assert(outputI == expectedSize);

            return result;
        }
    }
}
