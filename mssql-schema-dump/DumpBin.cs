//
//  Program.cs
//
//  Author:
//       Cpasternack <Cpasternack@users.noreply.gitlab.com>
//  Changes:
//  Ported to mono develop from original files on 28/11/2019
//
// https://github.com/https://github.com/georgekosmidis/mssql-schema-dump
//
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
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;

namespace mssqldump {
    class DumpBin {
        
        private static string HOST = "$HOST";
        private static string USER = "sa";
        private static string PASS = "sa";
        private static string SavePath = "/tmp/mssql-schema-dump/";
        //private static bool CleanDir = false; // TODO remove
        private static bool ExportStatistics = false;
        private static bool UseDAC = false;
        private static List<string> DBs = new List<string>();
        private static DBOperations DB = new DBOperations( HOST, USER, PASS );

        static void Main( string[] args ) {
            if ( args.Count() == 0 ) {
                WriteHelp();
                return;
            }
            if ( !ReadArguments( args ) )
                return;
            //Clean Dir // TODO remove; never cleanup in programme
            //if ( CleanDir && Directory.Exists( SavePath + Path.DirectorySeparatorChar + pathify( HOST ) ) ) {
            //    Console.WriteLine( "Cleaning Directory '" + SavePath + Path.DirectorySeparatorChar + pathify( HOST ) + "'" );
            //  var b = DeleteDirectory( SavePath + Path.DirectorySeparatorChar + pathify( HOST ) );
            //    if ( !b )
            //        return;
            //    Console.Clear(); // Don't do this in *nixland // TODO remove
            //}

            //Use DAC
            if ( UseDAC ) {
                Console.WriteLine( "Trying to enable DAC..." );
                try {
                    DB.TryEnableDAC();
                }
                catch (Exception ex){
                    //Console.WriteLine( "ERROR!" ); // Unhelpful
                    Console.WriteLine( "DAC cannot be enabled, retry without the option but encrypted objects will be omitted!" );
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(ex.StackTrace);
                    Console.WriteLine(ex.TargetSite);
                    return;
                }
                // TODO create Identity object to pass
                DB = new DBOperations( "ADMIN:" + HOST, USER, PASS );
                //Console.Clear(); // Don't do this in *nixland // TODO remove
            }
            var cn = new SqlConnection( "packet size=4096;user id=" + USER + ";Password=" + PASS +
                                       ";data source=" + HOST + ";persist security info=True;initial catalog=master;" );
            try {
                cn.Open();
                cn.Close();
            }
            catch ( Exception ex ) {
                //Console.Clear(); // Do not clear any console in *nixland // TODO remove
                //Console.WriteLine( "ERROR!" ); // Unhelpful // TODO remove
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
                Console.WriteLine(ex.TargetSite);
                Console.WriteLine( "(Server:" + HOST + ", User:" + USER + ", PASS: " +
                                  PASS.Substring( 0, 1 ) + (new String( '*', PASS.Length - 2 )) + PASS.Substring( PASS.Count() - 1, 1 ) + ")" );
                //Console.ReadKey(); // Don't do this in *nixland // TODO remove
                return;
            }
            var sc = new ServerConnection( cn );
            Server server = new Server( sc );

            //START
            SavePath = FileOperations.CreateFolder( SavePath, Pathify( HOST ) );

            //SERVER
            var filePath = PrepareSqlFile( "*", "", "SERVER", HOST, SavePath, "" );
            WriteSQLInner<Server>( "*", "", "SERVER", HOST, filePath, server, ScriptOption.DriAll );


            // TODO break up into separate methods

            foreach ( var db in server.Databases.Cast<Database>().AsQueryable().Where( o => o.IsSystemObject == false ) ) {
                if ( db.IsSystemObject )
                    continue;
                if ( DBs.Count() > 0 && !DBs.Contains( db.Name.ToLower() ) )
                    continue;
                var dbPath = FileOperations.CreateFolder( SavePath, Pathify( db.Name ) );

                Console.WriteLine( "=================================================" );
                Console.WriteLine( "DB: " + db.Name );
                Console.WriteLine( "-------------------------------------------------" );

                //var schema = "dbo";
                //var filename = "";
                //var objPath = "";
                //System.Collections.Specialized.StringCollection cs = new System.Collections.Specialized.StringCollection();
                //////////////////////////////////////////////////////////////////////////
                //DB
                var currentPath = dbPath;
                filePath = PrepareSqlFile( db.Name, "", "DB", db.Name, currentPath, "" );
                WriteSQLInner<Database>( db.Name, "", "DB", db.Name, filePath, db, ScriptOption.Default );

                //////////////////////////////////////////////////////////////////////////
                //SCHEMA
                foreach ( var schema2 in db.Schemas.Cast<Schema>().AsQueryable() ) {
                    filePath = PrepareSqlFile( db.Name, "", "Schema", schema2.Name, currentPath, "" );
                    WriteSQLInner<Schema>( db.Name, "", "Schema", schema2.Name, filePath, schema2, ScriptOption.Default );
                }

                //////////////////////////////////////////////////////////////////////////
                //DB USER TYPES
                currentPath = FileOperations.CreateFolder( dbPath, Pathify( "UTYPE" ) );
                foreach ( UserDefinedType o in db.UserDefinedTypes ) {
                    filePath = PrepareSqlFile( db.Name, o.Schema, "UTYPE", o.Name, currentPath, "" );
                    WriteSQLInner<UserDefinedType>( db.Name, o.Schema, "UTYPE", o.Name, filePath, o, ScriptOption.Default );
                }

                //////////////////////////////////////////////////////////////////////////
                //DB TRIGGERS
                currentPath = FileOperations.CreateFolder( dbPath, Pathify( "TRIGGER" ) );
                foreach ( DatabaseDdlTrigger o in db.Triggers.Cast<DatabaseDdlTrigger>().AsQueryable().Where( o => o.IsSystemObject == false ) ) {
                    filePath = PrepareSqlFile( db.Name, "dbo", "TRIGGER", o.Name, currentPath, "" );
                    WriteSQLInner<DatabaseDdlTrigger>( db.Name, "dbo", "TRIGGER", o.Name, filePath, o, ScriptOption.Default );
                }

                //////////////////////////////////////////////////////////////////////////
                //DB USER TABLE TYPES
                currentPath = FileOperations.CreateFolder( dbPath, Pathify( "TTYPES" ) );
                foreach ( UserDefinedTableType o in db.UserDefinedTableTypes ) {
                    filePath = PrepareSqlFile( db.Name, o.Schema, "TTYPES", o.Name, currentPath, "" );
                    WriteSQLInner<UserDefinedTableType>( db.Name, o.Schema, "TTYPES", o.Name, filePath, o, ScriptOption.Default );
                }

                //////////////////////////////////////////////////////////////////////////
                //DB FULLTEXT CATALOGS
                currentPath = FileOperations.CreateFolder( dbPath, Pathify( "FTC" ) );
                foreach ( FullTextCatalog o in db.FullTextCatalogs ) {
                    filePath = PrepareSqlFile( db.Name, "dbo", "FTC", o.Name, currentPath, "" );
                    WriteSQLInner<FullTextCatalog>( db.Name, "dbo", "FTC", o.Name, filePath, o, ScriptOption.Default );
                }

                //////////////////////////////////////////////////////////////////////////
                //DB FULLTEXT STOPLISTS
                currentPath = FileOperations.CreateFolder( dbPath, Pathify( "FTL" ) );
                foreach ( FullTextStopList o in db.FullTextStopLists ) {
                    filePath = PrepareSqlFile( db.Name, "dbo", "FTL", o.Name, currentPath, "" );
                    WriteSQLInner<FullTextStopList>( db.Name, "dbo", "FTL", o.Name, filePath, o, ScriptOption.Default );
                }

                //////////////////////////////////////////////////////////////////////////
                //STORED PROCEDURES
                currentPath = FileOperations.CreateFolder( dbPath, Pathify( "PROCEDURE" ) );
                foreach ( StoredProcedure o in db.StoredProcedures.Cast<StoredProcedure>().AsQueryable().Where( o => o.IsSystemObject == false ) ) {
                    filePath = PrepareSqlFile( db.Name, o.Schema, "PROCEDURE", o.Name, currentPath, "" );
                    WriteSQLInner<StoredProcedure>( db.Name, o.Schema, "PROCEDURE", o.Name, filePath, o, ScriptOption.Default );
                }

                //////////////////////////////////////////////////////////////////////////
                //FUNCTIONS
                currentPath = FileOperations.CreateFolder( dbPath, Pathify( "FUNCTION" ) );
                foreach ( UserDefinedFunction o in db.UserDefinedFunctions.Cast<UserDefinedFunction>().Where( oo => oo.IsSystemObject == false ) ) {
                    filePath = PrepareSqlFile( db.Name, o.Schema, "FUNCTION", o.Name, currentPath, "" );
                    WriteSQLInner<UserDefinedFunction>( db.Name, o.Schema, "FUNCTION", o.Name, filePath, o, ScriptOption.Default );
                }

                //////////////////////////////////////////////////////////////////////////
                //TABLE
                foreach ( Table o in db.Tables.Cast<Table>().AsQueryable().Where( o => o.IsSystemObject == false ) ) {

                    currentPath = FileOperations.CreateFolder( dbPath, Pathify( "TABLE" ) );
                    filePath = PrepareSqlFile( db.Name, o.Schema, "TABLE", o.Name, currentPath, "" );
                    WriteSQLInner<Table>( db.Name, o.Schema, "TABLE", o.Name, filePath, o, ScriptOption.Default );
                    WriteSQLInner<Table>( db.Name, o.Schema, "TABLE", o.Name, filePath, o, ScriptOption.Indexes );
                    WriteSQLInner<Table>( db.Name, o.Schema, "TABLE", o.Name, filePath, o, ScriptOption.DriAll );


                    //////////////////////////////////////////////////////////////////////////
                    //TABLE TRIGGERS
                    currentPath = FileOperations.CreateFolder( dbPath, Pathify( "TRIGGER" ) );
                    foreach ( Trigger ot in o.Triggers.Cast<Trigger>().AsQueryable().Where( oo => oo.IsSystemObject == false ) ) {
                        filePath = PrepareSqlFile( db.Name, o.Schema, "TRIGGER", ot.Name, currentPath, "TABLE_" + o.Name );
                        WriteSQLInner<Trigger>( db.Name, o.Schema, "TRIGGER", ot.Name, filePath, ot, ScriptOption.Default );
                    }

                    //////////////////////////////////////////////////////////////////////////
                    //TABLE STATISTICS
                    if ( ExportStatistics ) {
                        currentPath = FileOperations.CreateFolder( dbPath, Pathify( "STATISTIC" ) );
                        foreach ( Statistic ot in o.Statistics.Cast<Statistic>().AsQueryable() ) {
                            filePath = PrepareSqlFile( db.Name, o.Schema, "STATISTIC", ot.Name, currentPath, "TABLE_" + o.Name );
                            WriteSQLInner<Statistic>( db.Name, o.Schema, "STATISTIC", ot.Name, filePath, ot, ScriptOption.OptimizerData );
                        }
                    }
                }

                //////////////////////////////////////////////////////////////////////////
                //VIEWS
                foreach ( View o in db.Views.Cast<View>().AsQueryable().Where( o => o.IsSystemObject == false ) ) {

                    currentPath = FileOperations.CreateFolder( dbPath, Pathify( "VIEW" ) );
                    filePath = PrepareSqlFile( db.Name, o.Schema, "VIEW", o.Name, currentPath, "" );
                    WriteSQLInner<View>( db.Name, o.Schema, "VIEW", o.Name, filePath, o, ScriptOption.Default );
                    WriteSQLInner<View>( db.Name, o.Schema, "VIEW", o.Name, filePath, o, ScriptOption.Indexes );
                    WriteSQLInner<View>( db.Name, o.Schema, "VIEW", o.Name, filePath, o, ScriptOption.DriAllConstraints );

                    //////////////////////////////////////////////////////////////////////////
                    //VIEW TRIGGERS
                    currentPath = FileOperations.CreateFolder( dbPath, Pathify( "TRIGGER" ) );
                    foreach ( Trigger ot in o.Triggers.Cast<Trigger>().AsQueryable().Where( oo => oo.IsSystemObject == false ) ) {
                        filePath = PrepareSqlFile( db.Name, o.Schema, "TRIGGER", ot.Name, currentPath, "VIEW_" + o.Name );
                        WriteSQLInner<Trigger>( db.Name, o.Schema, "TRIGGER", ot.Name, filePath, ot, ScriptOption.Default );
                    }

                    //////////////////////////////////////////////////////////////////////////
                    //VIEW STATISTICS
                    if ( ExportStatistics ) {
                        currentPath = FileOperations.CreateFolder( dbPath, Pathify( "STATISTIC" ) );
                        foreach ( Statistic ot in o.Statistics.Cast<Statistic>().AsQueryable() ) {
                            filePath = PrepareSqlFile( db.Name, o.Schema, "STATISTIC", ot.Name, currentPath, "VIEW_" + o.Name );
                            WriteSQLInner<Statistic>( db.Name, o.Schema, "STATISTIC", ot.Name, filePath, ot, ScriptOption.OptimizerData );
                        }
                    }
                }

            }

            if ( UseDAC )
                DB.TryDisableDAC();

            //Console.WriteLine( Environment.NewLine );
            Console.WriteLine( "Done!" );
            //Console.ReadKey(); // Don't do this in *nixland // TODO remove

        }

        #region Helpers
        /// <summary>
        /// Writes the help.
        /// </summary>
        private static void WriteHelp() {
            Console.WriteLine( "MS SQL schema dump v1.1.1 (https://github.com/cpasternack/mssql-schema-dump)" );
            Console.WriteLine( "Exports MS SQL Server database schema, that includes:" );
            Console.WriteLine( "DB" );
            Console.WriteLine( "  Schema, User Types, User Table Types, Triggers, Full Text Catalogues," );
            Console.WriteLine( "  Full Text StopLists, Stored Procedures, Functions" );
            Console.WriteLine( "DB.Tables" );
            Console.WriteLine( "  Schema, Triggers, Indexes, DRI, Statistics" );
            Console.WriteLine( "DB.Views" );
            Console.WriteLine( "  Schema, Triggers, Indexes, DRI, Statistics" );
            Console.WriteLine( "Pass a junk parameter to start with default values shown below!" );
            Console.WriteLine( "" );
            Console.WriteLine( "Usage: mssqldump -h data-source-host -u username -p password" );
            Console.WriteLine( "       mssqldump [-d path/for/files] [-c] [-s] [-a] [-b DB1[,DB2[,DB3]]]" );
            Console.WriteLine( "" );
            Console.WriteLine( "Options:" );
            Console.WriteLine( "     -h : SQL server host, defaults to (local)" );
            Console.WriteLine( "     -u : username, defaults to sa" );
            Console.WriteLine( "     -p : password, defaults to sa" );
            Console.WriteLine( "     -d : Local path for saved files, defaults to C:\\_SQL_SCHEMA_DUMP\\" );
            //Console.WriteLine( "     -c : Delete all files and folders from local path, defaults to false" ); // TODO remove
            Console.WriteLine( "     -c : inert; no action; depricated" );
            Console.WriteLine( "     -s : Also export statistics, defaults to false" );
            Console.WriteLine( "     -a : Use DAC to try decrypt encrypted objects, defaults to false" );
            Console.WriteLine( "     -b : Comma separated value of databases to export, defaults to empty string" );
            Console.WriteLine("License: GPL-2.0");
            //Console.ReadKey(); // Don't do this in *nixland // TODO remove
        }
        /// <summary>
        /// Reads the arguments.
        /// </summary>
        /// <returns><c>true</c>, if arguments was  read, <c>false</c> otherwise.</returns>
        /// <param name="args">Arguments.</param>
        private static bool ReadArguments( string[] args ) {
            try {
                for ( int i = 0; i < args.Count(); i++ ) {
                    switch ( args[i] ) {
                        case "-h":
                            if ( args[i + 1].Substring( 0, 1 ) != "-" )
                                HOST = args[i + 1];
                            i++;
                            continue;
                        case "-u":
                            if ( args[i + 1].Substring( 0, 1 ) != "-" )
                                USER = args[i + 1];
                            i++;
                            continue;
                        case "-p":
                            if ( args[i + 1].Substring( 0, 1 ) != "-" )
                                PASS = args[i + 1];
                            i++;
                            continue;
                        case "-d":
                            if ( args[i + 1].Substring( 0, 1 ) != "-" )
                                SavePath = args[i + 1];
                            i++;
                            continue;
                        case "-c":
                            //CleanDir = false; // TODO remove
                            Console.WriteLine("'-c' Option not in use");
                            continue;
                        case "-s":
                            ExportStatistics = false;
                            continue;
                        case "-a":
                            UseDAC = false;
                            continue;
                        case "-b":
                            if ( args[i + 1].Substring( 0, 1 ) != "-" ) {
                                DBs = args[i + 1].Split( ',' ).ToList<string>().ConvertAll( d => d.ToLowerInvariant());
                            }
                            continue;
                    }
                }
            }
            catch (Exception ex) {
                //Console.Clear(); // Don't do this in *nixland // TODO remove
                //Console.WriteLine( "ERROR!" ); // Unhelpful
                Console.WriteLine( "You have an error in your arguments passed." );
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
                Console.WriteLine(ex.TargetSite);
                //Console.WriteLine( "Press any key to read help" ); // TODO remove
                //Console.ReadKey(); // Don't do this in *nixland
                //Console.Clear(); // Don't do this in *nixland
                WriteHelp();
                return false;
            }
            return true;
        }
        /// <summary>
        /// Writes the SQLI nner.
        /// </summary>
        /// <returns><c>true</c>, if SQLI nner was writed, <c>false</c> otherwise.</returns>
        /// <param name="db">Db.</param>
        /// <param name="schema">Schema.</param>
        /// <param name="objType">Object type.</param>
        /// <param name="objName">Object name.</param>
        /// <param name="filePath">File path.</param>
        /// <param name="o">O.</param>
        /// <param name="so">So.</param>
        /// <typeparam name="T">The 1st type parameter.</typeparam>
        private static bool WriteSQLInner<T>( string db, string schema, string objType, string objName,
                                             string filePath, T o, ScriptingOptions so ) where T : SqlSmoObject {
            if ( schema == "" ) // FIXME string.equals
                schema = "dbo"; // FIXME string.equals
            if ( db == "*" ) // FIXME string.equals
                Console.WriteLine( objType + ": " + objName );
            else
                Console.WriteLine( objType + ": " + db + "." + schema + "." + objName + " (" + so.ToString() + ")" );


            System.Collections.Specialized.StringCollection cs = new System.Collections.Specialized.StringCollection();
            try {
                cs = (o as dynamic).Script( so );
            }
            catch ( Exception ex ) {
                if ( UseDAC ) {
                    try {
                        DB.ChangeDB( db );
                        var dt = DB.GetDecryptedObject( objName, objType );
                        cs.Clear();
                        cs.Add( dt.Rows[0]["script"].ToString() );
                    }
                    catch ( Exception ex2 ) {
                        Console.WriteLine( ex2.Message );
                        return false;
                    }
                }
                else {
                    Console.WriteLine( ex.Message );
                    return false;
                }
            }

            if ( cs != null ) {
                var ts = "";
                foreach ( var s in cs )
                    ts += s + Environment.NewLine;
                if ( !String.IsNullOrWhiteSpace( ts.Trim() ) ) {
                    if ( !File.Exists( filePath ) )
                        FileOperations.writeFile( filePath, SqlComments( db, schema, objType, objName ), true );
                    FileOperations.writeFile( filePath, ts + ";" + Environment.NewLine, true );
                }
            }

            return true;
        }
        /// <summary>
        /// Prepares the sql file.
        /// </summary>
        /// <returns>The sql file.</returns>
        /// <param name="db">Db.</param>
        /// <param name="schema">Schema.</param>
        /// <param name="objType">Object type.</param>
        /// <param name="objName">Object name.</param>
        /// <param name="objPath">Object path.</param>
        /// <param name="filePrefix">File prefix.</param>
        private static string PrepareSqlFile( string db, string schema, string objType, string objName, string objPath, string filePrefix ) {
            filePrefix = filePrefix != "" ? filePrefix + "_" : filePrefix;
            var filePath = objPath + Path.DirectorySeparatorChar + Pathify( filePrefix + objType + "_" + schema + "_" + objName ) + ".sql";

            return filePath;
        }
        /// <summary>
        /// Pathify the specified s.
        /// </summary>
        /// <returns>The pathify.</returns>
        /// <param name="s">S.</param>
        private static string Pathify( string s ) {
            foreach ( var c in System.IO.Path.GetInvalidFileNameChars() )
                s = s.Replace( c, '_' );
            return s;
        }
        /// <summary>
        /// Sqls the comments.
        /// </summary>
        /// <returns>The comments.</returns>
        /// <param name="db">Db.</param>
        /// <param name="schema">Schema.</param>
        /// <param name="type">Type.</param>
        /// <param name="name">Name.</param>
        private static string SqlComments( string db, string schema, string type, string name ) {
            var s = "--****************************************************" + Environment.NewLine;
            s += "--MS SQL schema dump v1.1.1" + Environment.NewLine;
            s += "--Mono Runtime port by Cpasternack" + Environment.NewLine;
            s += "--Latest Version on GitHub: https://github.com/cpasternack/mssql-schema-dump" + Environment.NewLine;
            s += "--Original author: George Kosmidis <www.georgekosmidis.com>" + Environment.NewLine;
            s += "-------------------------------------------------------" + Environment.NewLine;
            s += "--DB: " + db + Environment.NewLine;
            s += "--SCHEMA: " + schema + Environment.NewLine;
            s += "--" + type + ": " + name + Environment.NewLine;
            s += "--" + DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToLongTimeString() + Environment.NewLine;
            s += "--****************************************************" + Environment.NewLine + Environment.NewLine;
            return s;
        }
        // FIXME cyclical referrence
        /// <summary>
        /// Deletes the directory.
        /// </summary>
        /// <returns><c>true</c>, if directory was deleted, <c>false</c> otherwise.</returns>
        /// <param name="target_dir">Target dir.</param>
        private static bool DeleteDirectory( string target_dir ) {
            string[] files = Directory.GetFiles( target_dir );
            string[] dirs = Directory.GetDirectories( target_dir );

            foreach ( string file in files ) {
                try {
                    File.SetAttributes( file, FileAttributes.Normal );
                    File.Delete( file );
                }
                catch (Exception ex) {
                    //Console.WriteLine( "ERROR!" ); // Unhelpful // TODO remove
                    Console.WriteLine( "File '" + file + "' is locked." ); // Fail here
                    //Console.WriteLine( "R: Retry, any other key to exit" ); // TODO remove
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(ex.StackTrace);
                    Console.WriteLine(ex.TargetSite);

                    //var k = Console.ReadKey(); // TODO remove
                    //if ( k.Key.ToString().ToLower() == "r" ) // TODO remove
                    //    return DeleteDirectory( target_dir ); // TODO remove

                    return false;
                }
            }
            Thread.Sleep( 200 ); // FIXME magic number

            foreach ( string dir in dirs ) {
                var b = DeleteDirectory( dir ); // FIXME why is this recursive?
            }

            try {
                Directory.Delete( target_dir, false );
            }
            catch (Exception ex){
                //Console.WriteLine( "ERROR!" ); // Unhelpful
                Console.WriteLine( "Directory '" + target_dir + "' is locked." );
                //Console.WriteLine( "R: Retry, any other key to exit" ); // TODO remove
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
                Console.WriteLine(ex.TargetSite);

                //var k = Console.ReadKey(); // TODO remove
                //if ( k.Key.ToString().ToLower() == "r" ) // TODO remove
                //    return DeleteDirectory( target_dir ); // TODO remove

                return false;
            }
            return true;
        }

        #endregion
    }
}
