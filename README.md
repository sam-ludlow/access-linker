# access-linker
Link Microsoft Access to SQL Server Backend.

## Notes
- If tables do not have a PK you will get a pop up from Access, just click OK. Warning the pop up may end up hidden behind another window and you wouldn't know (apears to hang).
- You may have problems running & compiling due to problems with office component versions 

## Usage

- If using trusted SQL connections you can use JUST the server name for the server paramter.
- If using connection strings provide them for both SQL & ODBC ( `""`).
- DO NOT provide the database in the connection strings, they will be added.

### Link
__access-linker.exe link "C:\My Data\LINK.accdb" \<database\> \<sql server\> [odbc server]__

Link Access to SQL server using Microsoft Access 16.0 Object Library.

### Import
__access-linker.exe import "C:\My Data\LINK.accdb" \<database\> \<sql server\> [odbc server]__

Import from SQL server into Access using Microsoft Access 16.0 Object Library.

### Dump
__access-linker.exe dump "C:\My Data\DUMP.accdb" \<database\> \<sql server\> [oledb access]__

Dump from SQL to Access using OleDb. This is just lolz you are better off using `import`.

### Encode
__access-linker.exe encode "C:\My Data\EMPTY.accdb"__

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
