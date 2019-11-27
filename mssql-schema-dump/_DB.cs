//
//  EmptyClass.cs
//
//  Author:
//       Cpasternack <Cpasternack@users.noreply.gitlab.com>
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
using System.Text;
using System.Data.SqlClient;
using System.Data;
using System.Text.RegularExpressions;

namespace MSSQLDump {
    class _DB {
        private SqlConnection cn = new SqlConnection();
        private SqlCommand cmd = new SqlCommand();

        private string _host = "";
        private string _user = "";
        private string _password = "";

        public _DB( string host, string user, string password ) {
            _host = host;
            _user = user;
            _password = password;

            cn.ConnectionString = "packet size=4096;user id=" + _user + ";Password=" + _password + ";data source=" + _host + ";persist security info=True;initial catalog=master;";
            cmd.Connection = cn;
            cmd.CommandTimeout = 3600;
            cmd.Prepare();
        }

        public void TryEnableDAC() {

            cmd.CommandText = "exec sp_configure 'show advanced options', 1" + Environment.NewLine;
            cmd.CommandText += "RECONFIGURE WITH OVERRIDE" + Environment.NewLine;
            cmd.CommandText += "exec sp_configure 'remote admin connections', 1" + Environment.NewLine;
            cmd.CommandText += "RECONFIGURE WITH OVERRIDE" + Environment.NewLine;

            if (cmd.Connection.State == ConnectionState.Closed)
                cmd.Connection.Open();

            cmd.ExecuteNonQuery();

        }
        public void TryDisableDAC() {

            cmd.CommandText = "exec sp_configure 'show advanced options', 0" + Environment.NewLine;
            cmd.CommandText += "RECONFIGURE WITH OVERRIDE" + Environment.NewLine;
            cmd.CommandText += "exec sp_configure 'remote admin connections', 0" + Environment.NewLine;
            cmd.CommandText += "RECONFIGURE WITH OVERRIDE" + Environment.NewLine;

            if (cmd.Connection.State == ConnectionState.Closed)
                cmd.Connection.Open();

            cmd.ExecuteNonQuery();

        }
        public void ChangeDB( string db ) {
            cmd.CommandText = "USE " + db + ";";

            if (cmd.Connection.State == ConnectionState.Closed)
                cmd.Connection.Open();

            cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="objName">Name of encrypted object</param>
        /// <param name="objType">VIEW, PROCEDURE, TRIGGER</param>
        /// <returns></returns>
        public DataTable GetDecryptedObject( string objName, string objType ) {
            cmd.CommandText = @"DECLARE @encrypted NVARCHAR(MAX)
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
            return this.GetDatatable( cmd );
        }
        //        public void Test() {
        //            cmd.CommandText = @"SELECT * 
        //	            FROM sys.sysobjvalues
        //	            WHERE OBJECT_NAME(objid) = 'Network_ExecuteNonQuery' ";
        //            var dt = GetDatatable( cmd );

        //        }
        public DataTable GetObjects( string db, string type ) {
            cmd.CommandText = "SELECT * FROM [" + db + "].dbo.sysobjects WHERE xtype = '" + type + "';";
            return this.GetDatatable( cmd );
        }

        private DataTable GetDatatable( SqlCommand cmd ) {
            if (cmd.Connection.State == ConnectionState.Closed)
                cmd.Connection.Open();

            SqlDataAdapter da = new SqlDataAdapter();
            da.SelectCommand = cmd;
            DataTable dt = new DataTable();
            da.Fill( dt );

            cmd.Connection.Close();

            return dt;
        }
    }
}

