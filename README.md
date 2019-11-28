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
