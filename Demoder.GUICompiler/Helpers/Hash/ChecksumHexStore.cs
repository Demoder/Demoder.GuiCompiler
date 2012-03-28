/*
demoder.guicompiler
Copyright (c) 2010-2012 Demoder <demoder@demoder.me>

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
*/
using System;
using System.Security.Cryptography;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using System.Runtime.Serialization;

namespace Demoder.GUICompiler.Helpers.Hash
{
    /// <summary>
    /// Template used for storing byte/hexadecimal checksums.
    /// This class cannot be serialized into an attribute.
    ///	Workaround: Add [XmlIgnore] to the member of this class, and add a public accessor to access the member of this class which you want to use for serialization.
    /// </summary>
    public struct ChecksumHexStore : ICheckSum, IEquatable<ICheckSum>
    {
        #region Members
        private byte[] bytes;
        #endregion
        #region Constructors
        /// <summary>
        /// Initializes an instance using a byte representation of a checksum
        /// </summary>
        /// <param name="bytes"></param>
        public ChecksumHexStore(byte[] bytes)
        {
            this.bytes = bytes;
        }

        /// <summary>
        /// Initializes an instance using a string representation of a checksum
        /// </summary>
        /// <param name="hex"></param>
        public ChecksumHexStore(string hex)
        {
            this.bytes = null;
            this.String = hex;
        }
        #endregion

        #region Overrides
        public override bool Equals(object obj)
        {
            //If it's a string.
            if (obj.GetType() == typeof(string))
            {
                if ((string)obj == this.String)
                    return true;
                else
                    return false;
            }

            try
            {
                ChecksumHexStore template = (ChecksumHexStore)obj;
                if (template.String == this.String)
                    return true;
                else
                    return false;
            }
            catch
            {
                return false;
            }
        }
        #endregion

        #region Private methods
        private string generateString()
        {
            //Generate a hexadecimal string
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < this.bytes.Length; i++)
            {
                sb.Append(this.bytes[i].ToString("X2"));
            }
            return sb.ToString();
        }

        private byte[] generateBytes(string Hex)
        {
            int numberChars = Hex.Length;
            byte[] bytes = new byte[numberChars / 2];
            for (int i = 0; i < numberChars; i += 2)
                bytes[i / 2] = Convert.ToByte(Hex.Substring(i, 2), 16);
            return bytes;
        }
        #endregion

        #region Public Accessors (ICheckSum)
        /// <summary>
        /// Byte array representing the checksum
        /// </summary>
        [XmlIgnore]
        public byte[] Bytes
        {
            set
            {
                this.bytes = value;
            }
            get
            {
                return this.bytes;
            }
        }
        /// <summary>
        /// String representing the checksum
        /// </summary>
        [XmlAttribute("value")] 
        public string String
        {
            get { return this.generateString(); }
            set
            {
                string val = value;
                //See if it starts with 0x. If it does, remove the hexadecimal prefix.
                if (val.StartsWith("0x"))
                    val = val.Substring(2);
                try
                {
                    this.bytes = this.generateBytes(val);
                }
                catch 
                {
                    throw new ArgumentException("Provided string is not a hexadecimal string");
                }
                
            }
        }
        #endregion

        #region Operators
        #region static operators
        public static bool operator ==(ChecksumHexStore item1, ChecksumHexStore item2)
        {
            if (Object.ReferenceEquals(item1, item2)) { return true; }
            if (Object.ReferenceEquals(item1, null)) { return false; }
            if (Object.ReferenceEquals(item2, null)) { return false; }
            
            return item1.Bytes.Equals(item2.Bytes);
        }
        public static bool operator !=(ChecksumHexStore item1, ChecksumHexStore item2)
        {
            if (Object.ReferenceEquals(item1, item2)) { return false; }
            if (Object.ReferenceEquals(item1, null)) { return true; }
            if (Object.ReferenceEquals(item2, null)) { return true; }

            return !item1.Bytes.Equals(item2.Bytes);
        }
        #endregion
        #endregion

        #region IEquatable<ICheckSum> Members
        public bool Equals(ICheckSum Other)
        {
            if (this.bytes.Equals(Other.Bytes))
                return true;
            else
                return false;
        }
        #endregion
    }
}
