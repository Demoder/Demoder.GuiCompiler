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
using Demoder.GUICompiler.Helpers.Hash;

namespace Demoder.GUICompiler.DataClasses
{
    public class ImageArchive
    {
        #region Members
        private MemoryStream archiveFile = new MemoryStream();
        private Dictionary<string, ImageArchiveEntry> archiveIndexKey = new Dictionary<string, ImageArchiveEntry>();
        private Dictionary<MD5Checksum, ImageArchiveEntry> archiveIndexMD5 = new Dictionary<MD5Checksum, ImageArchiveEntry>();
        #endregion

        #region Constructors
        /// <summary>
        /// Create an empty image archive
        /// </summary>
        public ImageArchive()
        {

        }
        /// <summary>
        /// Load an image archive
        /// </summary>
        /// <param name="IndexFile"></param>
        public ImageArchive(FileInfo IndexFile)
        {
            var index = File.ReadAllLines(IndexFile.FullName);
            FileStream archive = new FileStream(IndexFile.FullName.Substring(0, IndexFile.FullName.Length -1) + "a", FileMode.Open);
            foreach (string s in index)
            {
                var strings = s.Split(" ".ToCharArray());
                if (strings.Length != 3) { continue; }

                try
                {
                    string key = strings[0];
                    int filepos = int.Parse(strings[1]);
                    int bytes = int.Parse(strings[2]);
                    //Read to buffer
                    byte[] b = new byte[bytes];
                    archive.Seek(filepos, SeekOrigin.Begin);
                    if (archive.Read(b, 0, bytes) == bytes)
                        this.Add(key, b); //Add image slice.
                }
                catch
                {
                    continue;
                }
            }
        }
        #endregion

        #region Methods
        /// <summary>
        /// Add data to the image archive
        /// </summary>
        /// <param name="Key"></param>
        /// <param name="Bytes"></param>
        public void Add(string Key, byte[] Bytes)
        {
            lock (this)
            {
                if (this.archiveIndexKey.ContainsKey(Key))
                {
                    throw new Exception("Key already exists: " + Key);
                }
                MD5Checksum md5 = MD5Checksum.Generate(Bytes);
                ImageArchiveEntry entry;

                if (this.archiveIndexMD5.ContainsKey(md5))
                {
                    ImageArchiveEntry oldiae = this.archiveIndexMD5[md5];
                    entry = new ImageArchiveEntry
                    {
                        Key = Key,
                        BytePositionStart = oldiae.BytePositionStart,
                        Size = oldiae.Size,
                        MD5 = md5
                    };

                }
                else
                {
                    entry = new ImageArchiveEntry
                    {
                        Key = Key,
                        BytePositionStart = this.archiveFile.Position,
                        Size = Bytes.Length,
                        MD5 = md5
                    };
                    //Add the entry to the MD5 index.
                    this.archiveIndexMD5.Add(md5, entry);
                    //Add to memorystream
                    this.archiveFile.Write(Bytes, 0, Bytes.Length);
                }
                //Add to key index
                this.archiveIndexKey.Add(Key, entry);
            }
        }

        /// <summary>
        /// Will add .png files, then .jpg files, to the ImageArchive.
        /// Filename (minus extension) will be used as key.
        /// 
        /// </summary>
        /// <param name="Directory"></param>
        public int Add(DirectoryInfo Directory)
        {
            lock (this)
            {
                int numfiles = 0;
                string[] patterns = new string[] { "*.png", "*.jpg", "*.jpeg" };
                foreach (string pattern in patterns)
                {
                    foreach (FileInfo fi in Directory.GetFiles(pattern, SearchOption.TopDirectoryOnly))
                    {
                        if (this.autoaddFile(fi))
                        {
                            numfiles++;
                        }
                    }
                }
                return numfiles;
            }
        }

        private bool autoaddFile(FileInfo FileInfo)
        {
            lock (this)
            {
                string filename = FileInfo.Name;
                string key = string.Empty;
                string extension = FileInfo.Extension;
                int startext = filename.Length - extension.Length;
                string substring = filename.Substring(startext, extension.Length);
                if (substring == extension)
                {
                    key = filename.Substring(0, startext);
                }

                //Only add it if the key does not already exist.
                if (!this.Exists(key))
                {
                    try
                    {
                        this.Add(key, File.ReadAllBytes(FileInfo.FullName));
                        return true;
                    }
                    catch
                    {
                        return false;
                    }
                }
                return false;
            }
        }

        /// <summary>
        /// Remove the specific key from index.
        /// </summary>
        /// <param name="Key"></param>
        public void Remove(string Key)
        {
            lock (this)
            {
                if (!this.archiveIndexKey.ContainsKey(Key))
                {
                    throw new Exception("Key not found: " + Key);
                }

                ImageArchiveEntry removedEntry = this.archiveIndexKey[Key];
                this.archiveIndexKey.Remove(Key);
                //Browse through each ImageArchiveEntry we have, to find out if we can remove this image from the stream.
                bool removeStream = true;
                foreach (KeyValuePair<string, ImageArchiveEntry> kvp in this.archiveIndexKey)
                {
                    if (kvp.Value.MD5 == removedEntry.MD5)
                    {
                        removeStream = false;
                        //Replace representation in MD5 index
                        this.archiveIndexMD5[removedEntry.MD5] = kvp.Value;
                        break;
                    }
                }

                //If we can remove the stream
                if (removeStream)
                {
                    //Adjust position mapping of entries after removed entry, but only if the removed entry isn't the last entry.
                    if ((removedEntry.BytePositionStart + removedEntry.Size) == this.archiveFile.Length)
                    {
                        foreach (KeyValuePair<string, ImageArchiveEntry> kvp in this.archiveIndexKey)
                        {
                            if (kvp.Value.BytePositionStart > removedEntry.BytePositionStart)
                            {
                                kvp.Value.BytePositionStart -= removedEntry.Size;
                            }
                        }
                    }

                    //Remove this part of the stream.
                    MemoryStream ms = new MemoryStream();
                    byte[] buffer;
                    //Part before removed entry.
                    if (removedEntry.BytePositionStart > 0)
                    {
                        buffer = new byte[removedEntry.BytePositionStart];
                        this.archiveFile.Read(buffer, 0, (int)removedEntry.BytePositionStart);
                        ms.Write(buffer, 0, buffer.Length);
                    }

                    //Part after removed entry.
                    if ((removedEntry.BytePositionStart + removedEntry.Size) <= this.archiveFile.Length)
                    {
                        int startpos = (int)removedEntry.BytePositionStart + removedEntry.Size;
                        int length = (int)this.archiveFile.Length - startpos;
                        buffer = new byte[length];
                        this.archiveFile.Read(buffer, startpos, length);
                        ms.Write(buffer, 0, buffer.Length);
                    }
                }
            }
        }

        /// <summary>
        /// Save this image archives content to a folder.
        /// </summary>
        /// <param name="Directory"></param>
        public void Save(DirectoryInfo Directory)
        {
            lock (this)
            {
                if (!Directory.Exists)
                {
                    Directory.Create();
                }

                foreach (KeyValuePair<string, ImageArchiveEntry> kvp in this.archiveIndexKey)
                {
                    var b = new byte[kvp.Value.Size];
                    this.archiveFile.Seek((int)kvp.Value.BytePositionStart, SeekOrigin.Begin);
                    this.archiveFile.Read(b, 0, kvp.Value.Size);
                    File.WriteAllBytes(String.Format("{1}{0}{2}.png", Path.DirectorySeparatorChar, Directory, kvp.Value.Key), b);
                    this.archiveFile.Seek(0, SeekOrigin.End);
                }
            }
        }

        /// <summary>
        /// Save this ImageArchive to a .UVGI/.UVGA pair
        /// </summary>
        /// <param name="File">Name of archive, without extension</param>
        public bool Save(DirectoryInfo Directory, string Name)
        {
            lock (this)
            {
                if (!Directory.Exists)
                {
                    Directory.Create();
                }

                List<string> indexFileText = new List<string>();
                indexFileText.Add(this.archiveIndexKey.Count.ToString());
                foreach (KeyValuePair<string, ImageArchiveEntry> kvp in this.archiveIndexKey)
                {
                    indexFileText.Add(kvp.Value.ToString());
                }

                //Prepare paths for index & archive file
                string baseFileName = String.Format("{1}{0}{2}", Path.DirectorySeparatorChar, Directory, Name);
                string indexFile = baseFileName + ".UVGI";
                string archiveFile = baseFileName + ".UVGA";
                FileStream fsIndex = null;
                FileStream fsArchive = null;
                //Open filestreams.
                try
                {
                    fsIndex = File.OpenWrite(indexFile);
                    fsArchive = File.OpenWrite(archiveFile);

                    byte[] buffer = this.archiveFile.ToArray();
                    fsArchive.Write(buffer, 0, buffer.Length);

                    buffer = ASCIIEncoding.ASCII.GetBytes(String.Join("\r\n", indexFileText.ToArray()));
                    fsIndex.Write(buffer, 0, buffer.Length);
                    buffer = null;
                    return true;
                }
                catch
                {
                    if (fsIndex != null)
                    {
                        fsIndex.Close();
                    }
                    if (fsArchive != null)
                    {
                        fsArchive.Close();
                    }
                    return false;
                }
            }
        }

        

        /// <summary>
        /// Check if the provided key already exist
        /// </summary>
        /// <param name="Key"></param>
        /// <returns></returns>
        public bool Exists(string Key)
        {
            lock (this)
            {
                return this.archiveIndexKey.ContainsKey(Key);
            }
        }
        /// <summary>
        /// Check if data matching the provided MD5 already exists
        /// </summary>
        /// <param name="MD5"></param>
        /// <returns></returns>
        public bool Exists(MD5Checksum MD5)
        {
            lock (this)
            {
                return this.archiveIndexMD5.ContainsKey(MD5);
            }
        }
        #endregion
    }
}
