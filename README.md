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

`-c : ~~Delete all files and folders from local path, defaults to false~~`

`-s : Also export statistics, defaults to false`

`-a : Use DAC to try decrypt encrypted objects, defaults to false`

`-b : Comma separated value of databases to export, defaults to empty string`

## Updates:

This programme was written nearly 5-7 years ago now by the original author. 
I needed a quick and dirty sql dumper without using MSSQL management studio from windows, now that MS SQL Server runs on linux.
I have a fair few applications ported from .NET on MSVS and Windows to mono runtime on Linux under my belt, so this is a natural next step. 

The code looks clean, easy to read, and is a great starting point. Let's improve on that!

### Todo:

  - continue extending case insensitive databases to include all collation possibilities
    * ~~CS~~
    * AS Accented (accented latin characters are used in my work)
    * KS Kana (I work with non-latin alphabets)
    * WS Width (I work with non-latin alphabets)
    * UTF-8 encoded data (It's 2019, this was introduced in MS SQL Server 14 (2017))
  - add jenkins file for use with build environment

### License

GPL-2.0, See: LICENSE file 

### Changelog:

  - removed binaries in top level repo
  - porting to mono; making use of \*nix path referencing
  - changed README.md tags to markdown
  - incremented version to 1.1.1
  - no longer deleteing any files in any paths
  - changed local path for file saves to /tmp
  - added xmldoc comments
  - added license to file hearders
