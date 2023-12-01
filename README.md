# access-linker
Link Microsoft Access to SQL Server Backend.

## Notes
- If tables do not have a PK you will get a pop up from Access, just click OK. Warning the pop up may end up hidden behind another window and you wouldn't know (apears to hang).
- You may have problems running & compiling due to problems with office component versions 

## Usage

### Link
__access-linker.exe link "C:\My Data\LINK.accdb" \<server\> \<database\>__

Link Access to SQL server using Microsoft Access 16.0 Object Library.

### Dump
__access-linker.exe dump "C:\My Data\DUMP.accdb" \<server\> \<database\>__

Dump from SQL to Access using OleDb.

### Encode
__access-linker.exe encode "C:\My Data\EMPTY.accdb"__
