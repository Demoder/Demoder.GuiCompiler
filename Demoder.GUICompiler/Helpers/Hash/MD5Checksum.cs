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
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Security.Cryptography;
using System.Xml;
using System.Xml.Serialization;

namespace Demoder.GUICompiler.Helpers.Hash
{
    /// <summary>
    /// Represents a single MD5 Checksum
    /// </summary>
    public class MD5Checksum : ICheckSum
    {
        #region Members
        private ICheckSum _checkSumStore;
        #endregion
        #region Constructors
        public MD5Checksum(byte[] Bytes) : this() { this._checkSumStore = new ChecksumHexStore(Bytes); }
        public MD5Checksum(string Hex) : this() { this._checkSumStore = new ChecksumHexStore(Hex); }
        public MD5Checksum() { this._checkSumStore = null; }
        #endregion
        #region Interfaces
        #region ICheckSum Members
        /// <summary>
        /// Set or retrieve a byte representation of this class
        /// </summary>
        public byte[] Bytes
        {
            get
            {
                if (this._checkSumStore == null)
                    return null;
                return this._checkSumStore.Bytes;
            }
            set
            {
                if (this._checkSumStore == null)
                    this._checkSumStore = new ChecksumHexStore(value);
                else
                    this._checkSumStore.Bytes = value;
            }
        }
        /// <summary>
        /// Set or retrieve a string representation of this class
        /// </summary>
        public string String
        {
            get
            {
                if (this._checkSumStore == null)
                    return String.Empty;
                return this._checkSumStore.String;
            }
            set
            {
                if (this._checkSumStore == null)
                    this._checkSumStore = new ChecksumHexStore(value);
                else
                    this._checkSumStore.String = value;
            }
        }
        #endregion
        #region IEquatable<ICheckSum> Members
        public override bool Equals(object Other)
        {
            ICheckSum other;
            try
            {
                other = (ICheckSum)Other;
            }
            catch { return false; }
            if (this.String == other.String)
                return true;
            else
                return false;
        }
        public override int GetHashCode()
        {
            return this.String.GetHashCode();
        }
        #endregion
        #endregion Interfaces

        public override string ToString()
        {
            return this.String;
        }

        #region static operators
        public static bool operator ==(MD5Checksum item1, MD5Checksum item2)
        {

            if (Object.ReferenceEquals(item1, item2)) { return true; }
            if (Object.ReferenceEquals(item1, null)) { return false; }
            if (Object.ReferenceEquals(item2, null)) { return false; }

            if (item1.String == item2.String)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public static bool operator !=(MD5Checksum item1, MD5Checksum item2)
        {
            if (Object.ReferenceEquals(item1, item2)) { return false; }
            if (Object.ReferenceEquals(item1, null)) { return true; }
            if (Object.ReferenceEquals(item2, null)) { return true; }

            return item1.String != item2.String;
        }
        #endregion

        #region Static Generate
        /// <summary>
        /// Generates a hexadecimal string representing the MD5 hash of the provided data.
        /// </summary>
        /// <param name="Input">byte[] array representing data</param>
        /// <returns>a hexadecimal string of 32 characters representing the MD5 hash of the provided data</returns>
        public static MD5Checksum Generate(byte[] Input)
        {
            MD5 _md5 = System.Security.Cryptography.MD5.Create();
            byte[] hash = _md5.ComputeHash(Input);
            return new MD5Checksum(hash);
        }

        /// <summary>
        /// Generates a hexadecimal string representing the MD5 hash of the provided data.
        /// </summary>
        /// <param name="Input">stream input</param>
        /// <returns>a hexadecimal string of 32 characters representing the MD5 hash of the provided data</returns>
        public static MD5Checksum Generate(Stream Input)
        {
            MD5 _md5 = System.Security.Cryptography.MD5.Create();
            byte[] hash = _md5.ComputeHash(Input);
            return new MD5Checksum(hash);
        }
        /// <summary>
        /// Generates a hexadecimal string representing the MD5 hash of the provided data
        /// </summary>
        /// <param name="Input">MemoryStream input</param>
        /// <returns>a hexadecimal string of 32 characters representing the MD5 hash of the provided data</returns>
        public static MD5Checksum Generate(MemoryStream Input)
        {
            return Generate(Input.ToArray());
        }

        /// <summary>
        /// Generates a hexadecimal string representing the MD5 hash of the provided data.
        /// </summary>
        /// <param name="input">char[] array representing data</param>
        /// <returns>a hexadecimal string of 32 characters representing the MD5 hash of the provided data</returns>
        public static MD5Checksum Generate(char[] Input)
        {
            //Convert the char array to a byte array
            byte[] b = new byte[Input.Length];
            for (int i = 0; i < Input.Length; i++)
            {
                b[i] = byte.Parse(Input[i].ToString());
            }
            return Generate(b);
        }

        /// <summary>
        /// Generates a hexadecimal string representing the MD5 hash of the provided data
        /// </summary>
        /// <param name="Input">string input representing data</param>
        /// <returns>a hexadecimal string of 32 characters representing the MD5 hash of the provided data</returns>
        public static MD5Checksum Generate(string Input) { return Generate(Encoding.Default.GetBytes(Input)); }

        public static MD5Checksum Generate(List<byte> Input) { return Generate(Input.ToArray()); }

        /// <summary>
        /// Generates a hexadecimal string representing the MD5 hash of the file located at path
        /// </summary>
        /// <param name="FilePath">Full path to the file we should generate a MD5 hash of</param>
        /// <exception cref="FileNotFoundException">File does not exist</exception>
        /// <returns>a hexadecimal string of 32 characters representing the MD5 hash of the provided file</returns>
        public static MD5Checksum Generate(FileInfo FilePath)
        {
            if (!FilePath.Exists) throw new FileNotFoundException("File does not exist");
            return Generate(File.ReadAllBytes(FilePath.FullName));
        }
        #endregion
    }
}
