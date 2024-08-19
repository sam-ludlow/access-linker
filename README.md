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
`access-linker.exe link FILENAME=<filename.accdb> DATABASE=<database> SERVER=<sql server> SERVER_ODBC=[odbc server]`

Link Access to SQL server using Microsoft Access 16.0 Object Library (TransferDatabase acLink). You can optionally provide the ODBC connection string, used from Access to SQL server.

### Import
`access-linker.exe import FILENAME=<filename.accdb> DATABASE=<database> SERVER=<sql server> SERVER_ODBC=[odbc server]`

Import from SQL server into Access using Microsoft Access 16.0 Object Library (TransferDatabase acImport). You can optionally provide the ODBC connection string, used from Access to SQL server.

### Export
`access-linker.exe export FILENAME=<filename.accdb> DATABASE=<database> SERVER=<sql server> SERVER_ODBC=[odbc server]`

Export from Access to SQL Server using Microsoft Access 16.0 Object Library (TransferDatabase acExport). You can optionally provide the ODBC connection string, used from Access to SQL server.

### Dump
`access-linker.exe dump FILENAME=<filename.accdb> DATABASE=<database> SERVER=<sql server> ACCESS_OLEDB=[oledb access]`

Dump from SQL to Access using OleDb. You are normally better off using `import`. You can optionally provide the OleDb connection string to Access.

### Empty
`access-linker.exe empty FILENAME=<filename.accdb>`

Create an empty Access database.

### Backup
`access-linker.exe backup FILENAME=<filename.bak> DATABASE=<database> SERVER=<sql server> WITH=[WITH Options]`

Backup SQL database to `.BAK` file (BACKUP DATABASE).

### Verify
`access-linker.exe verify FILENAME=<filename.bak> SERVER=<sql server>`

Verify the `.BAK` file (RESTORE VERIFYONLY).

### List
`access-linker.exe list FILENAME=<filename.bak> SERVER=<sql server>`

List the data files within the `.BAK` file (RESTORE FILELISTONLY), will pop up in notepad.

### Restore
`access-linker.exe restore FILENAME=<filename.bak> DATABASE=<database> SERVER=<sql server> WITH=[WITH Options] DIRECTORY=[.MDF directory] LOG_DIRECTORY=[.LDF directory]`

Restore SQL databae from `.BAK` file (RESTORE DATABASE). You can optionally provide the server data file directories.

### Rename
`access-linker.exe rename DATABASE=<database> NEW_DATABASE=<database> SERVER=<sql server> DIRECTORY=[.MDF directory] LOG_DIRECTORY=[.LDF directory]`

Rename SQL database including logical and physical data & log files (`.MDF` & `.LDF`). You can optionally provide the server data file directory.

### Create
`access-linker.exe create <database> <sql server>`

Create a new SQL database.

### Delete





### Shrink

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
