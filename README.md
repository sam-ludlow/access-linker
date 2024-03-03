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
__access-linker.exe link \<target.accdb\> \<database\> \<sql server\> [odbc server]__

Link Access to SQL server using Microsoft Access 16.0 Object Library (TransferDatabase acLink).

### Import
__access-linker.exe import \<target.accdb\> \<database\> \<sql server\> [odbc server]__

Import from SQL server into Access using Microsoft Access 16.0 Object Library (TransferDatabase acImport).

### Dump
__access-linker.exe dump dump \<Target.accdb\> \<database\> \<sql server\> [oledb access]__

Dump from SQL to Access using OleDb. You are normally better off using `import`.

### Backup
__access-linker.exe backup \<filename.bak\> \<database\> \<sql server\>__

Backup SQL database to `.BAK` file (BACKUP DATABASE).

### Restore
__access-linker.exe restore \<filename.bak\> \<database\> \<sql server\> [directory]__

Restore SQL databae from `.BAK` file (RESTORE DATABASE).

### Rename
__access-linker.exe rename \<source name\> \<target name\> \<sql server\> [directory]__

Rename SQL database including logical and physical data & log files (`.MDF` & `.LDF`).

### Schema
__access-linker.exe schema \<database\> \<sql server\>__

Get database schema (INFORMATION_SCHEMA) will pop up in notepad, tab delimited text.

### Encode
__access-linker.exe encode \<filename\>__

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
