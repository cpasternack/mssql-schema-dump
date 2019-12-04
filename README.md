## MS SQL schema dump v1.1.1

Exports MS SQL Server database schema, that includes:

1. DB
  - Schema
  - User Types
  - User Table Types
  - Triggers
  - Full Text Catalogues
  - Full Text StopLists 
  - Stored Procedures
  - Functions
2. DB.Tables
  - Schema
  - Triggers
  - Indexes
  - DRI
  - Statistics
3. DB.Views
  - Schema
  - Triggers
  - Indexes
  - DRI
  - Statistics

## Usage:

Pass a junk parameter to start with default values shown below!

`mssqldump -h data-source-host -u username -p password [-d path/for/files] [-c] [-s] [-a] [-b DB1[,DB2[,DB3]]]`

Options:

`-h : SQL server host, defaults to $HOST`

`-u : username, defaults to sa`

`-p : password, defaults to sa`

`-d : Local path for saved files, defaults to /tmp/sql_schema_dump`

~~-c : Delete all files and folders from local path, defaults to false~~

`-s : Also export statistics, defaults to false`

`-a : Use DAC to try decrypt encrypted objects, defaults to false`

`-b : Comma separated value of databases to export, defaults to empty string`

## Updates:

12/2019:
And we are into mono and .net framework/CORE compatibility issues with the latest MSSQL server! Key server management methods are not
implemented in either System.Data.SqlClient, Microsoft.Data.SqlClient, or Microsoft.SqlServer.Management


11/2019:
This programme was written nearly 5-7 years ago now by the original author. 
I needed a quick and dirty sql dumper without using MSSQL management studio from windows, now that MS SQL Server runs on linux.
Azure Data Studio is also a bit too heavy-handed for testing out development database layer nonsense in a pipeline as well.
I have a fair few applications ported from .NET on MSVS and Windows to mono runtime on Linux under my belt, so this is a natural next step. 

The code looks clean, easy to read, and is a great starting point. Let's improve on that!

There are some rather big changes with MSSQL 2019 
https://docs.microsoft.com/en-us/sql/relational-databases/server-management-objects-smo/installing-smo?view=sql-server-ver15

### Todo:

See TODO.md for list

### License

GPL-2.0, See: LICENSE file 

### Changelog:

12/2019
  - Changed references to Microsoft.SqlServer.Management, Microsoft.Data.SqlClient in NuGet
  - Moving development from Mono 5.10 to Mono 6.4
  - Moving development tool from flatpak Monodevelop 7.3 to Monodevelop 7.6
  - Additionally using VS Code on Linux with .NET CORE 3.x SDK for testing Management code

11/2019
  - removed binaries in top level repo
  - porting to mono; making use of \*nix path referencing
  - changed README.md tags to markdown
  - incremented version to 1.1.1
  - no longer deleteing any files in any paths
  - changed local path for file saves to /tmp
  - added xmldoc comments
  - added license to file hearders
  - re-created solution files with mono Develop latest
  - Removed System.Data.SqlClient
  - Added Microsoft.SqlServer.SqlManagement
