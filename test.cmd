@echo off

if "%1"=="" (
	echo !!! Usage TEST CMD
	goto:eof
)

echo:

set exe_filename=access-linker.exe
echo exe_filename:	%exe_filename%

set access_filename=%1
echo access_filename:	%access_filename%

set sql_source_database=%2
echo sql_source_database:	%sql_source_database%

set sql_server_name=%3
echo sql_server_name:	%sql_server_name%

set sql_server_string=Data Source='%sql_server_name%';Integrated Security=True;TrustServerCertificate=True;
echo sql_server_string:	%sql_server_string%

set odbc_server_string=ODBC;Driver={ODBC Driver 17 for SQL Server};SERVER=%sql_server_name%;Trusted_Connection=Yes;
echo odbc_server_string:	%odbc_server_string%

echo:

:: ############## ACCESS ##########################

:: CREATE - from internal file

::set line=%exe_filename% COMMAND=ACCESS_CREATE FILENAME=%access_filename%
::echo %line%
::%line%

:: DELETE - simple file delete

::set line=%exe_filename% COMMAND=ACCESS_DELETE FILENAME=%access_filename%
::echo %line%
::%line%

:: LINK - create & link to SQL

:: --- SQL:NO ODBC: NO - Just SQL

%exe_filename% COMMAND=ACCESS_DELETE FILENAME=%access_filename%
%exe_filename% COMMAND=ACCESS_CREATE FILENAME=%access_filename%

set line=%exe_filename% COMMAND=ACCESS_LINK FILENAME=%access_filename% DATABASE=%sql_source_database% SERVER_SQL=%sql_server_name%
echo %line%
%line%

:: --- SQL:NO ODBC: NO - Both just server names

%exe_filename% COMMAND=ACCESS_DELETE FILENAME=%access_filename%
%exe_filename% COMMAND=ACCESS_CREATE FILENAME=%access_filename%

set line=%exe_filename% COMMAND=ACCESS_LINK FILENAME=%access_filename% DATABASE=%sql_source_database% SERVER_SQL=%sql_server_name% SERVER_ODBC=%sql_server_name%
echo %line%
%line%

:: --- SQL:YES ODBC: YES

%exe_filename% COMMAND=ACCESS_DELETE FILENAME=%access_filename%
%exe_filename% COMMAND=ACCESS_CREATE FILENAME=%access_filename%

set line=%exe_filename% COMMAND=ACCESS_LINK FILENAME=%access_filename% DATABASE=%sql_source_database% SERVER_SQL="%sql_server_string%" SERVER_ODBC="%odbc_server_string%"
echo %line%
%line%

:: --- SQL:NO ODBC: YES

%exe_filename% COMMAND=ACCESS_DELETE FILENAME=%access_filename%
%exe_filename% COMMAND=ACCESS_CREATE FILENAME=%access_filename%

set line=%exe_filename% COMMAND=ACCESS_LINK FILENAME=%access_filename% DATABASE=%sql_source_database% SERVER_SQL=%sql_server_name% SERVER_ODBC="%odbc_server_string%"
echo %line%
%line%


:: --- SQL:YES ODBC: YES

%exe_filename% COMMAND=ACCESS_DELETE FILENAME=%access_filename%
%exe_filename% COMMAND=ACCESS_CREATE FILENAME=%access_filename%

set line=%exe_filename% COMMAND=ACCESS_LINK FILENAME=%access_filename% DATABASE=%sql_source_database% SERVER_SQL="%sql_server_string%" SERVER_ODBC=%sql_server_name%
echo %line%
%line%