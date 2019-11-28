## To do:

- continue extending case insensitive databases to include all collation possibilities
  * ~~CS~~
  * AS Accented (accented latin characters are used in my work)
  * KS Kana (I work with non-latin alphabets)
  * WS Width (I work with non-latin alphabets)
  * UTF-8 encoded data (It's 2019, this was introduced in MS SQL Server 14 (2017))
- add jenkins file for use with build environment
- create library in addition to binary so we can call our tool from another programme
- dump out a nuget package for our internal nuget repo
- create a branch for mono-porting
- create additional options to:
  * dump list of stored procedures to file or output
  * dump just stored procedures to file or output
  * dump list of triggers to file or output
  * dump just triggers to file or output
  * dump list of tables to file or output
  * dump just tables to file or output
  * dump list of table schema to file or output
  * dump just table schema to file or output
  * dump list of views to file or output
  * dump just views to file or output
  * dump list of view schema to file or output
  * dump just view schema to file or output
- take username and password from stdin both interactively, or as parameter, for all argument patterns
- support ODBC driver instead of supplied connection details
