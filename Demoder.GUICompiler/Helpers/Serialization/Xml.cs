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
using System.IO.Compression;
using System.Net;
using System.Xml.Serialization;
using Demoder.GUICompiler.Helpers;

namespace Demoder.GUICompiler.Helpers.Serialization
{
    public static class Xml
    {
        #region Serialization
        /// <summary>
        /// Serializes an object into an already opened stream
        /// </summary>
        /// <typeparam name="T">Class type to serialize class as</typeparam>
        /// <param name="Stream">Stream to serialize into</param>
        /// <param name="Obj">Class to serialize</param>
        public static bool Serialize<T>(Stream Stream, object Obj, bool CloseStream) where T : class
        {
            return Compat.Serialize(typeof(T), Stream, Obj, CloseStream);
        }
        /// <summary>
        /// Serialize a class to a file
        /// </summary>
        /// <typeparam name="T">Class type to serialize</typeparam>
        /// <param name="Path"></param>
        /// <param name="Obj"></param>
        /// <param name="GZip">Whether or not the saved file should be GZipped</param>
        /// <returns></returns>
        public static bool Serialize<T>(FileInfo Path, T Obj, bool GZip) where T : class
        {
            return Compat.Serialize(typeof(T), Path, Obj, GZip);
        }
        #endregion

        #region deserialization
        /// <summary>
        /// Deserialize a stream
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="Stream"></param>
        /// <param name="CloseStream"></param>
        /// <returns></returns>
        public static T Deserialize<T>(Stream Stream, bool CloseStream) where T : class
        {
            object obj = Compat.Deserialize(typeof(T), Stream, CloseStream);
            if (obj == null)
                return default(T);
            else
                return (T)obj;
        }

        /// <summary>
        /// Deserialize a file
        /// </summary>
        /// <typeparam name="T">What class type to parse file as</typeparam>
        /// <param name="Path">Path to the file</param>
        /// <param name="GZip">Whether or not the file is gzip-compressed</param>
        /// <returns></returns>
        public static T Deserialize<T>(FileInfo Path, bool GZip) where T : class
        {
            object obj = Compat.Deserialize(typeof(T), Path, GZip);
            if (obj == null)
                return default(T);
            else
                return (T)obj;
        }
        
        /// <summary>
        /// Deserialize content of UriBuilder
        /// </summary>
        /// <typeparam name="T">What class to parse file as</typeparam>
        /// <param name="Path">Path to fetch</param>
        /// <returns></returns>
        public static T Deserialize<T>(UriBuilder Path) where T : class
        {
            return Deserialize<T>(Path.Uri);
        }

        /// <summary>
        /// Deserialize content of URI
        /// </summary>
        /// <typeparam name="T">Class type to parse as</typeparam>
        /// <param name="Path">URI to deserialize</param>
        /// <returns></returns>
        public static T Deserialize<T>(Uri Path) where T : class
        {
            object obj = Compat.Deserialize(typeof(T), Path);
            if (obj == null)
                return default(T);
            else
                return (T)obj;
        }
        #endregion


        public static class Compat
        {
            #region Serialize
            /// <summary>
            /// Serialize object to stream
            /// </summary>
            /// <param name="T"></param>
            /// <param name="Stream"></param>
            /// <param name="Obj"></param>
            /// <param name="CloseStream"></param>
            /// <returns></returns>
            public static bool Serialize(Type T, Stream Stream, object Obj, bool CloseStream)
            {
                if (Stream == null) throw new ArgumentNullException("Stream");
                if (Obj == null) throw new ArgumentNullException("Obj");
                try
                {
                    XmlSerializer serializer = new XmlSerializer(T);
                    serializer.Serialize(Stream, Obj);
                    if (CloseStream) Stream.Close();
                    return true;
                }
                catch (Exception ex)
                {
                    if (CloseStream && Stream != null) Stream.Close();
                    return false;
                }
            }

            /// <summary>
            /// Serialize object to file
            /// </summary>
            /// <param name="T"></param>
            /// <param name="Path"></param>
            /// <param name="Obj"></param>
            /// <param name="GZip"></param>
            /// <returns></returns>
            public static bool Serialize(Type T, FileInfo Path, object Obj, bool GZip)
            {
                if (Path == null) throw new ArgumentNullException("Path");
                if (Obj == null) throw new ArgumentNullException("Obj");
                if (GZip)
                {
                    using (FileStream fs = Path.Create())
                    {
                        using (GZipStream gzs = new GZipStream(fs, CompressionMode.Compress))
                        {
                            Serialize(T, gzs, Obj, true);
                        }
                    }
                    return true;
                }
                else
                { //don't gzip the output
                    MemoryStream ms = new MemoryStream();
                    FileStream fs = null;
                    try
                    {
                        XmlSerializer serializer = new XmlSerializer(T);
                        serializer.Serialize(ms, Obj); //Serialize into memory

                        fs = Path.Create();
                        ms.WriteTo(fs);
                        if (fs != null) fs.Close();
                        if (ms != null) ms.Close();
                        return true;
                    }
                    catch (Exception ex)
                    {
                        if (fs != null) fs.Close();
                        if (ms != null) ms.Close();
                        return false;
                    }
                }
            }
            #endregion
            #region Deserialize
            /// <summary>
            /// Deserialize a stream
            /// </summary>
            /// <param name="T"></param>
            /// <param name="Stream"></param>
            /// <param name="CloseStream"></param>
            /// <returns></returns>
            public static object Deserialize(Type T, Stream Stream, bool CloseStream)
            {
                if (Stream == null) throw new ArgumentNullException("Stream");
                try
                {
                    XmlSerializer serializer = new XmlSerializer(T);
                    object obj = serializer.Deserialize(Stream);
                    if (Stream != null && CloseStream) Stream.Close();
                    return obj;
                }
                catch (Exception ex)
                {
                    if (Stream != null && CloseStream) 
                        Stream.Close();
                    return null;
                }
            }

            /// <summary>
            /// Deserialize a file
            /// </summary>
            /// <param name="T"></param>
            /// <param name="Path"></param>
            /// <param name="GZip"></param>
            /// <returns></returns>
            public static object Deserialize(Type T, FileInfo Path, bool GZip)
            {
                if (Path == null) throw new ArgumentNullException("Path");

                if (GZip)
                {
                    using (FileStream fs = Path.OpenRead())
                    {
                        using (GZipStream gzs = new System.IO.Compression.GZipStream(fs, CompressionMode.Decompress, true))
                        {
                            return Deserialize(T, gzs, true);
                        }
                    }
                }
                else
                {
                    FileStream stream = null;
                    try
                    {
                        stream = Path.OpenRead();
                        XmlSerializer serializer = new XmlSerializer(T);
                        Object obj = serializer.Deserialize(stream);
                        if (stream != null) stream.Close();
                        return obj;
                    }
                    catch (Exception ex)
                    {
                        if (stream != null) stream.Close();
                        return null;
                    }
                }
            }

            /// <summary>
            /// Deserialize an URI
            /// </summary>
            /// <param name="T"></param>
            /// <param name="Path"></param>
            /// <returns></returns>
            public static object Deserialize(Type T, UriBuilder Path)
            {
                return Deserialize(T, Path.Uri);
            }

            /// <summary>
            /// Deserialize an URI
            /// </summary>
            /// <param name="T"></param>
            /// <param name="Path"></param>
            /// <returns></returns>
            public static object Deserialize(Type T, Uri Path)
            {
                if (Path == null) throw new ArgumentNullException("Path");
                try
                {
                    var webClient = new WebClient();
                    var data = webClient.DownloadData(Path.ToString());
                    return Deserialize(T, new MemoryStream(data), true);
                }
                catch { return null; }
            }


            #endregion deserialize
        }
    }
}