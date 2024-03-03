# access-linker
Link Microsoft Access to SQL Server Backend and other SQL tools.

## Notes
- With `link` or `import` if tables do not have a PK you will get a pop up from Access, just click OK. Warning the pop up may end up hidden behind another window and you wouldn't know (apears to hang).
- If using trusted SQL connections you can use JUST the server name for the server paramter.
- If using connection strings provide them for both SQL & ODBC.
- DO NOT provide the database in the connection strings, they will be added.
- You may have problems running & compiling due to problems with office component versions.
- Some commands like `rename` must be run on the server becuase of data file path names.

## Usage

### Link
`access-linker.exe link <filename.accdb> <database> <sql server> [odbc server]`

Link Access to SQL server using Microsoft Access 16.0 Object Library (TransferDatabase acLink).

### Import
`access-linker.exe import <filename.accdb> <database> <sql server> [odbc server]`

Import from SQL server into Access using Microsoft Access 16.0 Object Library (TransferDatabase acImport).

### Dump
`access-linker.exe dump <filename.accdb> <database> <sql server> [oledb access]`

Dump from SQL to Access using OleDb. You are normally better off using `import`.

### Backup
`access-linker.exe backup <filename.bak> <database> <sql server>`

Backup SQL database to `.BAK` file (BACKUP DATABASE).

### Restore
`access-linker.exe restore <filename.bak> <database> <sql server> [directory]`

Restore SQL databae from `.BAK` file (RESTORE DATABASE). You can optionally provide the server data file directory.

### Rename
`access-linker.exe rename <source database> <target database> <sql server> [directory]`

Rename SQL database including logical and physical data & log files (`.MDF` & `.LDF`). You can optionally provide the server data file directory.

### Schema
`access-linker.exe schema <database> <sql server>`

Get database schema (INFORMATION_SCHEMA) will pop up in notepad, tab delimited text.

### Encode
`access-linker.exe encode <filename>`

Encode file into GZ compresses base64 text will pop up in notepad. Used to include an empty MS Access database in the source code.

## Connection Strings
If you are using trusted connections to SQL Server you can simpily pass the server name and don't need connection strings.

If you are using credentials or have some other issue like ODBC versions or somthing.

### SQL

```
"Server=MY_SERVER;User Id='MY_USER';Password='MY_PASS';"
```

### ODBC

```
"ODBC;Driver={ODBC Driver 17 for SQL Server};SERVER=MY_SERVER;UID='MY_USER';PWD='MY_PASS';"
```

### OLEDB

```
"Provider='Microsoft.ACE.OLEDB.16.0';User ID='Admin';Password='';"
```
