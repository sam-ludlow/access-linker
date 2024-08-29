# access-linker
Link Microsoft Access to SQL Server Backend and other SQL tools.

## Notes
- With `link` or `import` if tables do not have a PK you will get a pop up from Access, just click OK. Warning the pop up may end up hidden behind another window and you wouldn't know (apears to hang).
- If using trusted SQL connections you can use JUST the server name for the server paramter.
- If using connection strings provide them for both SQL & ODBC.
- DO NOT provide the database in the connection strings, they will be added.
- You may have problems running & compiling due to problems with office component versions.

## Access Commands

### ACCESS_CREATE
Create empty Access database.

`access-linker.exe COMMAND=ACCESS_CREATE FILENAME=<filename.accdb>`

### ACCESS_DELETE
Delete Access database.

`access-linker.exe COMMAND=ACCESS_DELETE FILENAME=<filename.accdb>`

### ACCESS_SCHEMA
Show Access Schema.

`access-linker.exe COMMAND=ACCESS_SCHEMA FILENAME=<filename.accdb>`

### ACCESS_LINK
Link Access to SQL server using Microsoft Access 16.0 Object Library (TransferDatabase acLink). You can optionally provide the ODBC connection string, used from Access to SQL server.

`access-linker.exe link COMMAND=ACCESS_LINK FILENAME=<filename.accdb> DATABASE=<database> SERVER_SQL=<sql server> SERVER_ODBC=[odbc server]`

### ACCESS_IMPORT
Import from SQL server into Access using Microsoft Access 16.0 Object Library (TransferDatabase acImport). You can optionally provide the ODBC connection string, used from Access to SQL server.

`access-linker.exe COMMAND=ACCESS_IMPORT FILENAME=<filename.accdb> DATABASE=<database> SERVER_SQL_=<sql server> SERVER_ODBC=[odbc server]`

### ACCESS_EXPORT
Export from Access to SQL Server using Microsoft Access 16.0 Object Library (TransferDatabase acExport). You can optionally provide the ODBC connection string, used from Access to SQL server

`access-linker.exe COMMAND=ACCESS_EXPORT FILENAME=<filename.accdb> DATABASE=<database> SERVER_SQL=<sql server> SERVER_ODBC=[odbc server]`

### ACCESS_INSERT
Insert from SQL to Access using OleDb. You are normally better off using `import`. You can optionally provide the OleDb connection string to Access.

`access-linker.exe COMMAND=ACCESS_INSERT FILENAME=<filename.accdb> DATABASE=<database> SERVER_SQL=<sql server> SERVER_OLEDB=[oledb access]`

## SQL Commands



## Other Commands

### ENCODE
Encode file into GZ compresses base64 text will pop up in notepad. Used to include an empty MS Access database in the source code.

`access-linker.exe COMMAND=ENCODE FILENAME=<filename>`

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
