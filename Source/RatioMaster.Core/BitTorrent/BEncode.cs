using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;

namespace BitTorrent
{
    public interface IBEncodeValue
    {
        string StringValue { get; }
        byte[] Encode();
        void Parse(Stream p);
    }

    public class ValueCollection : Collection<IBEncodeValue>, IBEncodeValue
    {
        public string StringValue => throw new NotImplementedException();

        public byte[] Encode()
        {
            Collection<byte> bytes = new Collection<byte>();
            bytes.Add((byte)'l');

            foreach (IBEncodeValue member in Items) foreach (byte b in member.Encode()) bytes.Add(b);

            bytes.Add((byte)'e');
            byte[] newBytes = new Byte[bytes.Count];

            for (int i = 0; i < bytes.Count; i++) newBytes[i] = bytes[i];

            return newBytes;
        }

        public void Parse(Stream s)
        {
            byte current = (byte)s.ReadByte();
            while ((char)current != 'e')
            {
                IBEncodeValue value = BEncode.Parse(s, current);
                Add(value);
                current = (byte)s.ReadByte();
            }
        }
    }

    public class ValueList : Collection<IBEncodeValue>, IBEncodeValue
    {
        public void Parse(Stream s)
        {
            byte current = (byte)s.ReadByte();
            while ((char)current != 'e')
            {
                IBEncodeValue value = BEncode.Parse(s, current);
                Add(value);
                current = (byte)s.ReadByte();
            }
        }

        public string StringValue => throw new NotImplementedException();

        public byte[] Encode()
        {
            Collection<byte> bytes = new Collection<byte>();
            bytes.Add((byte)'l');

            foreach (IBEncodeValue member in Items) foreach (byte b in member.Encode()) bytes.Add(b);

            bytes.Add((byte)'e');
            byte[] newBytes = new Byte[bytes.Count];

            for (int i = 0; i < bytes.Count; i++) newBytes[i] = bytes[i];

            return newBytes;
        }
    }

    public class ValueString : IBEncodeValue
    {
        private string v;

        public int Length
        {
            get
            {
                return v.Length;
            }
        }

        public byte[] Bytes { get; private set; }

        public string StringValue
        {
            get
            {
                return v;
            }

            set
            {
                v = value;
                Bytes = Encoding.GetEncoding(1252).GetBytes(v);
            }
        }

        public byte[] Encode()
        {
            string prefix = v.Length.ToString() + ":";
            byte[] tempBytes = Encoding.GetEncoding(1252).GetBytes(prefix);

            byte[] newBytes = new Byte[prefix.Length + Bytes.Length];
            for (int i = 0; i < prefix.Length; i++) newBytes[i] = tempBytes[i];
            for (int i = 0; i < Bytes.Length; i++) newBytes[i + prefix.Length] = Bytes[i];
            return newBytes;
        }

        public ValueString(string val)
        {
            StringValue = val;
        }

        public ValueString()
        {
            StringValue = string.Empty;
        }

        public void Parse(Stream s)
        {
            throw new TorrentException(
                "Parse method not supported, the " + "first byte must be passed into the " + "string parse routine.");
        }

        public void Parse(Stream s, byte firstByte)
        {
            string q = ((char)firstByte).ToString();
            if (!Char.IsNumber(q[0])) throw new TorrentException("\"" + q + "\" is not a string length number.");

            char current = (char)s.ReadByte();
            while (current != ':')
            {
                q += current.ToString();
                current = (char)s.ReadByte();
            }

            int length = Int32.Parse(q);
            Bytes = new Byte[length];
            s.Read(Bytes, 0, length);
            v = Encoding.GetEncoding(1252).GetString(Bytes); // store string also
        }
    }

    public class ValueNumber : IBEncodeValue
    {
        private string v;

        private byte[] data;

        public string StringValue
        {
            get
            {
                return v;
            }

            set
            {
                v = value;
                data = Encoding.GetEncoding(1252).GetBytes(v);
            }
        }

        public Int64 Integer
        {
            get
            {
                return Int64.Parse(v);
            }

            set
            {
                StringValue = value.ToString();
            }
        }

        public byte[] Encode()
        {
            byte[] newByte = new Byte[data.Length + 2];
            newByte[0] = (byte)'i';
            for (int i = 0; i < data.Length; i++) newByte[i + 1] = data[i];
            newByte[data.Length + 1] = (byte)'e';
            return newByte;
        }

        public ValueNumber(Int64 number)
        {
            v = number.ToString();
            StringValue = v;
        }

        public ValueNumber()
        {
        }

        public void Parse(Stream s)
        {
            string buffer = String.Empty;
            char current = (char)s.ReadByte();
            while (current != 'e') // discard when end of integer
            {
                buffer += current.ToString();
                current = (char)s.ReadByte();
            }

            StringValue = Int64.Parse(buffer).ToString();
        }
    }

    public class BEncode
    {
        public BEncode()
        {
        }

        public static IBEncodeValue Parse(Stream d)
        {
            return Parse(d, (byte)d.ReadByte());
        }

        public static string String(IBEncodeValue v)
        {
            return v.StringValue;
        }

        public static IBEncodeValue Parse(Stream d, byte firstByte)
        {
            IBEncodeValue v;
            char first = (char)firstByte;

            // 
            if (first == 'd') v = new ValueDictionary();
            else if (first == 'l') v = new ValueList();
            else if (first == 'i') v = new ValueNumber();
            else v = new ValueString();
            if (v is ValueString) ((ValueString)v).Parse(d, (byte)first);
            else v.Parse(d);
            return v;
        }
    }
}
