namespace BitTorrent
{
    using System;
    using System.Collections;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Text;

    public interface IBEncodeValue
    {
        byte[] Encode();

        void Parse(Stream p);
    }

    public class TorrentException : Exception
    {
        public TorrentException(string message)
            : base(message)
        {
        }
    }

    public class ValueList : IBEncodeValue, IEnumerable, IEnumerator
    {
        public Collection<IBEncodeValue> values;

        public int Position = -1;

        public IEnumerator GetEnumerator()
        {
            return this;
        }

        /* Needed since Implementing IEnumerator*/

        public bool MoveNext()
        {
            if (Position < values.Count - 1)
            {
                ++Position;
                return true;
            }

            return false;
        }

        public void Reset()
        {
            Position = -1;
        }

        public object Current
        {
            get
            {
                return values[Position];
            }
        }

        public ValueList()
        {
            values = new Collection<IBEncodeValue>();
        }

        public void Parse(Stream s)
        {
            byte current = (byte)s.ReadByte();
            while ((char)current != 'e')
            {
                IBEncodeValue value = BEncode.Parse(s, current);
                values.Add(value);
                current = (byte)s.ReadByte();
            }
        }

        public void Add(IBEncodeValue value)
        {
            values.Add(value);
        }

        public Collection<IBEncodeValue> Values
        {
            get
            {
                return values;
            }

            set
            {
                values.Clear();
                foreach (IBEncodeValue val in value)
                {
                    value.Add(val);
                }
            }
        }

        public IBEncodeValue this[int index]
        {
            get
            {
                return values[index];
            }

            set
            {
                values[index] = value;
            }
        }

        public byte[] Encode()
        {
            Collection<byte> bytes = new Collection<byte>();
            bytes.Add((byte)'l');

            foreach (IBEncodeValue member in values) foreach (byte b in member.Encode()) bytes.Add(b);

            bytes.Add((byte)'e');
            byte[] newBytes = new Byte[bytes.Count];

            for (int i = 0; i < bytes.Count; i++) newBytes[i] = bytes[i];

            return newBytes;
        }
    }

    public class ValueString : IBEncodeValue
    {
        private string v;

        private byte[] data;

        public int Length
        {
            get
            {
                return v.Length;
            }
        }

        public byte[] Bytes
        {
            get
            {
                return data;
            }
        }

        public string String
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

        public byte[] Encode()
        {
            string prefix = v.Length.ToString() + ":";
            byte[] tempBytes = Encoding.GetEncoding(1252).GetBytes(prefix);

            byte[] newBytes = new Byte[prefix.Length + data.Length];
            for (int i = 0; i < prefix.Length; i++) newBytes[i] = tempBytes[i];
            for (int i = 0; i < data.Length; i++) newBytes[i + prefix.Length] = data[i];
            return newBytes;
        }

        public ValueString(string StringValue)
        {
            String = StringValue;
        }

        public ValueString()
        {
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
            data = new Byte[length];
            s.Read(data, 0, length);
            v = Encoding.GetEncoding(1252).GetString(data); // store string also
        }
    }

    public class ValueNumber : IBEncodeValue
    {
        private string v;

        private byte[] data;

        public string String
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
                String = value.ToString();
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
            String = v;
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

            String = Int64.Parse(buffer).ToString();
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
            if (v is ValueString) return ((ValueString)v).String;
            else if (v is ValueNumber) return ((ValueNumber)v).String;
            else return null;
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
