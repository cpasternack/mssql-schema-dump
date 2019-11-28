//
//  FileOperations.cs
//
//  Author:
//       Cpasternack <Cpasternack@users.noreply.gitlab.com>
//  Changes:
//  Ported to mono develop from original files on 28/11/2019
//
// https://github.com/https://github.com/georgekosmidis/mssql-schema-dump
//  Copyright (c) 2019 George Kosmidis
//
//  This program is free software; you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation; either version 2 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program; if not, write to the Free Software
//  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA
//

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace MSSQLDump {
    /// <summary>
    /// File operations.
    /// </summary>
    class FileOperations {
        public static string CreateFolder( string path, string folder ) {
            path = System.IO.Path.Combine( path, folder );
            if (!Directory.Exists( path ))
                System.IO.Directory.CreateDirectory( path );
            return path;
        }
        /// <summary>
        /// Writes the file.
        /// </summary>
        /// <param name="filePath">File path.</param>
        /// <param name="c">C.</param>
        /// <param name="append">If set to <c>true</c> append.</param>
        public static void writeFile( string filePath, string c, bool append ) {
            string s = ReadFile( filePath );
            TextWriter tw = new StreamWriter( filePath );
            try {
                if (append)
                    tw.WriteLine( s + c );
                else
                    tw.WriteLine( c );
            }
            finally {
                tw.Close();
            }
        }
        /// <summary>
        /// Reads the file.
        /// </summary>
        /// <returns>The file.</returns>
        /// <param name="filePath">File path.</param>
        public static string ReadFile( string filePath ) {
            byte[] buffer;
            try {
                FileStream fileStream = new FileStream( filePath, FileMode.Open, FileAccess.Read );
                try {
                    int length = (int)fileStream.Length;  // get file length
                    buffer = new byte[length];            // create buffer
                    int count;                            // actual number of bytes read
                    int sum = 0;                          // total number of bytes read

                    // read until Read method returns 0 (end of the stream has been reached)
                    while ((count = fileStream.Read( buffer, sum, length - sum )) > 0)
                        sum += count;  // sum is a buffer offset for next reading
                }
                finally {
                    fileStream.Close();
                }
                System.Text.Encoding enc = System.Text.Encoding.UTF8; // FIXME Assumption
                return enc.GetString( buffer );
            }
            catch {
                return "";
            }
        }
    }
}
