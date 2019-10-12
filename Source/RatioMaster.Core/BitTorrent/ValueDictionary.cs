using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;

namespace BitTorrent
{
    public class ValueDictionary : Dictionary<string, IBEncodeValue>, IBEncodeValue
    {
        public string StringValue => throw new System.NotImplementedException();

        public byte[] Encode()
        {
            Collection<byte> collection1 = new Collection<byte>();
            collection1.Add(100);
            ArrayList list1 = new ArrayList();
            foreach (string text1 in Keys)
            {
                list1.Add(text1);
            }

            foreach (string text2 in list1)
            {
                ValueString text3 = new ValueString(text2);
                foreach (byte num1 in text3.Encode())
                {
                    collection1.Add(num1);
                }

                foreach (byte num2 in this[text2].Encode())
                {
                    collection1.Add(num2);
                }
            }

            collection1.Add(0x65);
            byte[] buffer1 = new byte[collection1.Count];
            collection1.CopyTo(buffer1, 0);
            return buffer1;
        }

        public void Parse(Stream s)
        {
            for (byte num1 = (byte)s.ReadByte(); num1 != 0x65; num1 = (byte)s.ReadByte())
            {
                if (!char.IsNumber((char)num1))
                {
                    throw new TorrentException("Key expected to be a string.");
                }

                ValueString text1 = new ValueString();
                text1.Parse(s, num1);
                IBEncodeValue value1 = BEncode.Parse(s);
                if (ContainsKey(text1.StringValue))
                {
                    this[text1.StringValue] = value1;
                }
                else
                {
                    Add(text1.StringValue, value1);
                }
            }
        }

        public IBEncodeValue GetFromKey(string key)
        {
            if (!ContainsKey(key))
            {
                Add(key, new ValueString());
            }
            return this[key];
        }

        public void SetStringValue(string key, string value)
        {
            if (ContainsKey(value))
            {
                ((ValueString)this[key]).StringValue = value;
            }
            else
            {
                this[key] = new ValueString(value);
            }
        }

        public new IBEncodeValue this[string key]
        {
            get
            {
                if (!ContainsKey(key))
                {
                    Add(key, new ValueString(""));
                }

                return base[key];
            }

            set
            {
                if (ContainsKey(key))
                {
                    Remove(key);
                }

                Add(key, value);
            }
        }
    }
}