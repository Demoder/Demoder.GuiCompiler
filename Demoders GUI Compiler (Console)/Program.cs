/*
Demoder's GUI Compiler (Console)
Copyright (c) 2010-2012 Demoder <demoder@demoder.me>

This program is free software; you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation; version 2 of the License only.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program; if not, write to the Free Software
Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307
USA
*/

using System;
using System.Collections.Generic;
using Demoder.GUICompiler.Helpers;
using System.Text;
using Demoder.GUICompiler.DataClasses;
using System.IO;

namespace Demoder.GUICompilerConsole
{
    class Program
    {
        /// <summary>
        /// Returns -1 on config error, 0 on success.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        static int Main(string[] args)
        {
            Console.Title = "Demoder's GUI Compiler (Console) v"+System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
            DateTime starttime=DateTime.Now;
            Console.WriteLine("Checking configuration parameters");
            CommandLineParameters cmdParams = new CommandLineParameters(args);
            uint batch = cmdParams.LongFlag("batch");
            //Source directory.
            DirectoryInfo srcDir = null;
            if (!String.IsNullOrEmpty(cmdParams.Argument("srcdir")))
            {
                if (Directory.Exists(cmdParams.Argument("srcdir")))
                {
                    srcDir = new DirectoryInfo(cmdParams.Argument("srcdir"));
                }
            }
            if (srcDir == null)
            {
                Console.WriteLine("You need to provide a source directory containing the GUI image files: --srcdir=\"directory\"");
                if (batch == 0)
                    Console.ReadLine();
                return -1;
            }
            else
            {
                Console.WriteLine(" Source Directory: {0} ", srcDir);
            }

            //Destination directory.
            DirectoryInfo dstDir = null;
            if (!String.IsNullOrEmpty(cmdParams.Argument("dstdir")))
            {
                dstDir = new DirectoryInfo(cmdParams.Argument("dstdir"));
            }
            if (dstDir == null)
            {
                Console.WriteLine("You need to provide a directory to store the archive to: --dstdir=\"directory\"");
                if (batch == 0)
                    Console.ReadLine();
                return -1;
            }
            else
            {
                Console.WriteLine(" Destination Directory: {0} ", dstDir);
            }

            //Destination directory.
            string archiveName = null;
            if (!String.IsNullOrEmpty(cmdParams.Argument("name")))
            {
                archiveName = cmdParams.Argument("name");
            }
            if (archiveName == null)
            {
                Console.WriteLine("You need to provide an archive name (without extension): --name=\"archivename\"");
                if (batch == 0)
                    Console.ReadLine();
                return -1;
            }
            else
            {
                Console.WriteLine(" Archive Name: {0} ", archiveName);
            }

            Console.WriteLine();
            Console.WriteLine(" Loading images");
            ImageArchive ia = new ImageArchive();
            int imgloaded = ia.Add(srcDir);
            Console.WriteLine("  {0} images loaded.", imgloaded);
            Console.WriteLine("Saving archive");
            ia.Save(dstDir, archiveName);
            Console.WriteLine("\nDone.\n Archive: {1} KiB.\n Index:   {2} KiB\n Worktime: {0} seconds.", 
                Math.Round((DateTime.Now - starttime).TotalSeconds, 3),
                Math.Round((double)(new FileInfo(dstDir.FullName + Path.DirectorySeparatorChar + archiveName+".UVGA").Length) / 1024),
                Math.Round((double)(new FileInfo(dstDir.FullName + Path.DirectorySeparatorChar + archiveName+".UVGI").Length) / 1024));
            if (batch == 0)
                Console.ReadLine();
            return 0;
        }
    }
}
