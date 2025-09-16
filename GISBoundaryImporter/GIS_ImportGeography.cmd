@echo off
setlocal enabledelayedexpansion

rem ---------------------------------
rem Configure Logging
rem ---------------------------------
set LOGFILE=%~dp0process_geography_%date:~10,4%-%date:~4,2%-%date:~7,2%.log
echo Starting geography process... > "%LOGFILE%"
echo Log file: %LOGFILE%
echo Script started at %date% %time% >> "%LOGFILE%"

rem ---------------------------------
rem Prompt for user inputs
rem ---------------------------------
echo Please provide the SQL Server information and table details (no trailing spaces!).
set /p SERVERNAME=Enter your MSSQL server name (e.g. MYPC\SQLEXPRESS): 
set /p SRCDB=Enter your source (staging) database name (e.g. temp_import) no trailing spaces: 
echo Do not include dbo. prefix in the next prompt. It will be automatically added.
set /p SRCTABLE=Enter your source table name (without dbo. prefix, e.g. chathamtwpnj): 
set /p TGTDB=Enter your target database name (e.g. ParksRecClean) no trailing spaces: 
set /p TENANTID=Enter the tenant_id to update (e.g. 2): 

rem OGRFID is always 1
set OGRFID=1
rem Target table name is fixed to dbo.tenant
set TGTTABLE=dbo.tenant

echo User inputs: >> "%LOGFILE%"
echo SERVERNAME=%SERVERNAME% >> "%LOGFILE%"
echo SRCDB=%SRCDB% >> "%LOGFILE%"
echo SRCTABLE=%SRCTABLE% >> "%LOGFILE%"
echo OGRFID=%OGRFID% >> "%LOGFILE%"
echo TGTDB=%TGTDB% >> "%LOGFILE%"
echo TGTTABLE=%TGTTABLE% >> "%LOGFILE%"
echo TENANTID=%TENANTID% >> "%LOGFILE%"

if "%SERVERNAME%"=="" (
    echo ERROR: No server name provided. >> "%LOGFILE%"
    echo No server name provided.
    pause
    exit /b 1
)

if "%SRCDB%"=="" (
    echo ERROR: No source database provided. >> "%LOGFILE%"
    echo No source database provided.
    pause
    exit /b 1
)

if "%SRCTABLE%"=="" (
    echo ERROR: No source table provided. >> "%LOGFILE%"
    echo No source table provided.
    pause
    exit /b 1
)

if "%TGTDB%"=="" (
    echo ERROR: No target database provided. >> "%LOGFILE%"
    echo No target database provided.
    pause
    exit /b 1
)

if "%TENANTID%"=="" (
    echo ERROR: No tenant_id provided. >> "%LOGFILE%"
    echo No tenant_id provided.
    pause
    exit /b 1
)

rem ---------------------------------
rem Menu selection
rem ---------------------------------
echo.
echo Select an option:
echo 1) Import geography as-is
echo 2) Import with MakeValid
echo 3) Import with ReorientObject
set /p CHOICE=Enter your choice (1, 2, or 3): 

if "%CHOICE%"=="1" set ACTION=@g
if "%CHOICE%"=="2" set ACTION=@g.MakeValid()
if "%CHOICE%"=="3" set ACTION=@g.ReorientObject()

if not defined ACTION (
    echo ERROR: Invalid choice. >> "%LOGFILE%"
    echo Invalid choice.
    pause
    exit /b 1
)

echo User selected option %CHOICE%. Action=%ACTION% >> "%LOGFILE%"

rem ---------------------------------
rem Construct the SQL query
rem We'll write each line separately and carefully.
rem ---------------------------------
> "%~dp0temp_sql_query.sql" echo DECLARE @g geography;
>> "%~dp0temp_sql_query.sql" echo SET @g = (SELECT ogr_geometry FROM [%SRCDB%].dbo.[%SRCTABLE%] WHERE ogr_fid = %OGRFID%);
>> "%~dp0temp_sql_query.sql" echo UPDATE [%TGTDB%].%TGTTABLE% SET tenant_boundary = %ACTION% WHERE tenant_id = %TENANTID%;

type "%~dp0temp_sql_query.sql" >> "%LOGFILE%"

rem ---------------------------------
rem Execute the SQL using sqlcmd
rem ---------------------------------
echo Executing SQL command... >> "%LOGFILE%"
sqlcmd -S "%SERVERNAME%" -i "%~dp0temp_sql_query.sql" -b -r1 >> "%LOGFILE%" 2>&1

if errorlevel 1 (
    echo ERROR: SQL command failed. Check log for details. >> "%LOGFILE%"
    echo SQL command failed. See %LOGFILE% for details.
    del "%~dp0temp_sql_query.sql"
    pause
    exit /b 1
) else (
    echo SQL command completed successfully. >> "%LOGFILE%"
    echo SQL command completed successfully!
)

rem Delete temp SQL file
del "%~dp0temp_sql_query.sql"

rem ---------------------------------
rem End of process
rem ---------------------------------
echo Process completed successfully at %date% %time%. >> "%LOGFILE%"
echo Process completed successfully!
echo Please check %LOGFILE% for details.
echo Press any key to close...
pause
endlocal
