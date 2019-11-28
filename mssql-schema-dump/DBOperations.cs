//
//  DBOperations.cs
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
using System.Data.SqlClient;
using System.Data;
using System.Text.RegularExpressions;

namespace mssqldump {
    /// <summary>
    /// Db.
    /// </summary>
    class DBOperations {
        private SqlConnection cn = new SqlConnection();
        private SqlCommand sqlCMD = new SqlCommand();

        private string _host = "";
        private string _user = "";
        private string _password = "";
        /// <summary>
        /// Initializes a new instance of the <see cref="T:MSSQLDump._DB"/> class.
        /// </summary>
        /// <param name="host">Host.</param>
        /// <param name="user">User.</param>
        /// <param name="password">Password.</param>
        public DBOperations( string host, string user, string password ) {
            _host = host;
            _user = user;
            _password = password;

            cn.ConnectionString = "packet size=4096;user id=" + _user + ";Password=" + _password + ";data source=" + _host + ";persist security info=True;initial catalog=master;";
            sqlCMD.Connection = cn;
            sqlCMD.CommandTimeout = 3600;
            sqlCMD.Prepare();
        }
        /// <summary>
        /// Tries the enable dac.
        /// </summary>
        public void TryEnableDAC() {

            sqlCMD.CommandText = "exec sp_configure 'show advanced options', 1" + Environment.NewLine;
            sqlCMD.CommandText += "RECONFIGURE WITH OVERRIDE" + Environment.NewLine;
            sqlCMD.CommandText += "exec sp_configure 'remote admin connections', 1" + Environment.NewLine;
            sqlCMD.CommandText += "RECONFIGURE WITH OVERRIDE" + Environment.NewLine;

            if (sqlCMD.Connection.State == ConnectionState.Closed)
                sqlCMD.Connection.Open();

            sqlCMD.ExecuteNonQuery();

        }
        /// <summary>
        /// Tries the disable dac.
        /// </summary>
        public void TryDisableDAC() {

            sqlCMD.CommandText = "exec sp_configure 'show advanced options', 0" + Environment.NewLine;
            sqlCMD.CommandText += "RECONFIGURE WITH OVERRIDE" + Environment.NewLine;
            sqlCMD.CommandText += "exec sp_configure 'remote admin connections', 0" + Environment.NewLine;
            sqlCMD.CommandText += "RECONFIGURE WITH OVERRIDE" + Environment.NewLine;

            if (sqlCMD.Connection.State == ConnectionState.Closed)
                sqlCMD.Connection.Open();

            sqlCMD.ExecuteNonQuery();

        }
        /// <summary>
        /// Changes the db.
        /// </summary>
        /// <param name="db">Db.</param>
        public void ChangeDB( string db ) {
            sqlCMD.CommandText = "USE " + db + ";";

            if (sqlCMD.Connection.State == ConnectionState.Closed)
                sqlCMD.Connection.Open();

            sqlCMD.ExecuteNonQuery();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="objName">Name of encrypted object</param>
        /// <param name="objType">VIEW, PROCEDURE, TRIGGER</param>
        /// <returns></returns>
        public DataTable GetDecryptedObject( string objName, string objType ) {
            sqlCMD.CommandText = @"DECLARE @encrypted NVARCHAR(MAX)
                                SET @encrypted = ( 
	                                SELECT TOP 1 imageval 
	                                FROM sys.sysobjvalues
	                                WHERE OBJECT_NAME(objid) = '" + objName + @"' 
                                )
                                DECLARE @encryptedLength INT
                                SET @encryptedLength = DATALENGTH(@encrypted) / 2

                                DECLARE @procedureHeader NVARCHAR(MAX)
                                SET @procedureHeader = N'ALTER  " + objType.ToUpper() + @" dbo." + objName + @" WITH ENCRYPTION AS '
                                SET @procedureHeader = @procedureHeader + REPLICATE(N'-',(@encryptedLength - LEN(@procedureHeader)))

                                EXEC sp_executesql @procedureHeader
                                DECLARE @blankEncrypted NVARCHAR(MAX)
                                SET @blankEncrypted = ( 
	                                SELECT TOP 1 imageval 
	                                FROM sys.sysobjvalues
	                                WHERE OBJECT_NAME(objid) = '" + objName + @"' 
                                )

                                SET @procedureHeader = N'CREATE " + objType.ToUpper() + @" dbo." + objName + @" WITH ENCRYPTION AS '
                                SET @procedureHeader = @procedureHeader + REPLICATE(N'-',(@encryptedLength - LEN(@procedureHeader)))

                                DECLARE @cnt SMALLINT
                                DECLARE @decryptedChar NCHAR(1)
                                DECLARE @decryptedMessage NVARCHAR(MAX)
                                SET @decryptedMessage = ''
                                SET @cnt = 1
                                WHILE @cnt <> @encryptedLength BEGIN
                                  SET @decryptedChar = 
                                      NCHAR(
                                        UNICODE(SUBSTRING(
                                           @encrypted, @cnt, 1)) ^
                                        UNICODE(SUBSTRING(
                                           @procedureHeader, @cnt, 1)) ^
                                        UNICODE(SUBSTRING(
                                           @blankEncrypted, @cnt, 1))
                                     )
                                  SET @decryptedMessage = @decryptedMessage + @decryptedChar
                                 SET @cnt = @cnt + 1
                                END
                                SELECT @decryptedMessage AS [script]";
            return this.GetDatatable( sqlCMD );
        }
        //        public void Test() {
        //            cmd.CommandText = @"SELECT * 
        //	            FROM sys.sysobjvalues
        //	            WHERE OBJECT_NAME(objid) = 'Network_ExecuteNonQuery' ";
        //            var dt = GetDatatable( cmd );

        //        }
        /// <summary>
        /// Gets the objects.
        /// </summary>
        /// <returns>The objects.</returns>
        /// <param name="db">Db.</param>
        /// <param name="type">Type.</param>
        public DataTable GetObjects( string db, string type ) {
            sqlCMD.CommandText = "SELECT * FROM [" + db + "].dbo.sysobjects WHERE xtype = '" + type + "';";
            return this.GetDatatable( sqlCMD );
        }
        /// <summary>
        /// Gets the datatable.
        /// </summary>
        /// <returns>The datatable.</returns>
        /// <param name="scmd">Cmd.</param>
        private DataTable GetDatatable( SqlCommand scmd ) {
            if (scmd.Connection.State == ConnectionState.Closed)
                scmd.Connection.Open();

            SqlDataAdapter da = new SqlDataAdapter();
            da.SelectCommand = sqlCMD;
            DataTable dt = new DataTable();
            da.Fill( dt );

            sqlCMD.Connection.Close();

            return dt;
        }
    }
}

