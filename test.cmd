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

set sql_username=%4
echo sql_username:	%sql_username%

set sql_password=%5
echo sql_password:	%sql_password%

set sql_target_database=%sql_source_database%_TEST_TARGET
echo sql_target_database:	%sql_target_database%

set sql_server_string=Data Source='%sql_server_name%';Integrated Security=True;TrustServerCertificate=True;
echo sql_server_string:	%sql_server_string%

set odbc_server_string=ODBC;Driver={ODBC Driver 17 for SQL Server};SERVER=%sql_server_name%;Trusted_Connection=Yes;
echo odbc_server_string:	%odbc_server_string%

set sql_server_string_password=Data Source='%sql_server_name%';Integrated Security=True;User Id='%sql_username%';Password='%sql_password%';
echo sql_server_string_password:	%sql_server_string_password%

set odbc_server_string_password=ODBC;Driver={ODBC Driver 17 for SQL Server};SERVER=%sql_server_name%;UID=%sql_username%;PWD=%sql_password%;
echo odbc_server_string_password:	%odbc_server_string_password%

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



:: ########### ACCESS_LINK

goto skip_access_link

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


:: --- SQL:YES ODBC: YES - passwords

%exe_filename% COMMAND=ACCESS_DELETE FILENAME=%access_filename%
%exe_filename% COMMAND=ACCESS_CREATE FILENAME=%access_filename%

set line=%exe_filename% COMMAND=ACCESS_LINK FILENAME=%access_filename% DATABASE=%sql_source_database% SERVER_SQL="%sql_server_string_password%" SERVER_ODBC="%odbc_server_string_password%"
echo %line%
%line%

:skip_access_link



:: ########### ACCESS_IMPORT & ACCESS_EXPORT

::goto skip_access_import_export


%exe_filename% COMMAND=ACCESS_DELETE FILENAME=%access_filename%
%exe_filename% COMMAND=ACCESS_CREATE FILENAME=%access_filename%

set line=%exe_filename% COMMAND=ACCESS_IMPORT FILENAME=%access_filename% DATABASE=%sql_source_database% SERVER_SQL=%sql_server_name%
echo %line%
%line%

%exe_filename% COMMAND=SQL_DELETE DATABASE=%sql_target_database% SERVER_SQL=%sql_server_name%
%exe_filename% COMMAND=SQL_CREATE DATABASE=%sql_target_database% SERVER_SQL=%sql_server_name%

set line=%exe_filename% COMMAND=ACCESS_EXPORT FILENAME=%access_filename% DATABASE=%sql_target_database% SERVER_SQL=%sql_server_name%
echo %line%
%line%


:skip_access_import

:: ###########################

