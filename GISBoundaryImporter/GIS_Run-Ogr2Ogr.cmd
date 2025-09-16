@echo off
setlocal enabledelayedexpansion

rem ---------------------------------
rem Configure Logging
rem ---------------------------------
set LOGFILE=%~dp0import_shape_%date:~10,4%-%date:~4,2%-%date:~7,2%.log
echo Starting shapefile import process... > "%LOGFILE%"
echo Log file: %LOGFILE%
echo Script started at %date% %time% >> "%LOGFILE%"

rem ---------------------------------
rem Set required GDAL environment variables
rem Adjust paths if GDAL installed elsewhere
rem ---------------------------------
echo Setting GDAL environment variables... >> "%LOGFILE%"
set GDAL_DATA=C:\Program Files\GDAL\gdal-data
set GDAL_DRIVER_PATH=C:\Program Files\GDAL\gdalplugins
set PROJ_LIB=C:\Program Files\GDAL\projlib
set PYTHONPATH=C:\Program Files\GDAL
echo GDAL_DATA=%GDAL_DATA% >> "%LOGFILE%"
echo GDAL_DRIVER_PATH=%GDAL_DRIVER_PATH% >> "%LOGFILE%"
echo PROJ_LIB=%PROJ_LIB% >> "%LOGFILE%"
echo PYTHONPATH=%PYTHONPATH% >> "%LOGFILE%"

rem ---------------------------------
rem Prompt for user inputs
rem ---------------------------------
set /p SERVERNAME=Enter your MSSQL server name (e.g. MYPC\SQLEXPRESS): 
set /p DBNAME=Enter your temporary database name (e.g. temp_import): 
set /p EPSG=Enter the EPSG value of your shapefile (e.g. 3424 or 4326): 
set /p SHPDIR=Enter the full path to the shapefiles directory (no trailing slash): 

echo User inputs: >> "%LOGFILE%"
echo SERVERNAME=%SERVERNAME% >> "%LOGFILE%"
echo DBNAME=%DBNAME% >> "%LOGFILE%"
echo EPSG=%EPSG% >> "%LOGFILE%"
echo SHPDIR=%SHPDIR% >> "%LOGFILE%"

if "%SHPDIR%"=="" (
    echo ERROR: No shapefile directory provided. >> "%LOGFILE%"
    echo No shapefile directory provided.
    pause
    exit /b 1
)

rem ---------------------------------
rem Set driver environment variable
rem ---------------------------------
echo Setting driver variable... >> "%LOGFILE%"
set driver=SQL Server Native Client 11.0
echo driver=%driver% >> "%LOGFILE%"

rem ---------------------------------
rem Optional: If .shx file is missing, try enabling this:
rem set SHAPE_RESTORE_SHX=YES

rem ---------------------------------
rem Change to GDAL install directory
rem ---------------------------------
cd /d "C:\Program Files\GDAL"
if errorlevel 1 (
    echo ERROR: Failed to change directory to GDAL folder. >> "%LOGFILE%"
    echo Failed to change to GDAL directory.
    pause
    exit /b 1
)

rem ---------------------------------
rem Construct the ogr2ogr command
rem If EPSG=4326, use -a_srs "EPSG:4326"
rem Otherwise, use -s_srs "EPSG:<EPSG>" and -t_srs "EPSG:4326"
rem ---------------------------------
if "%EPSG%"=="4326" (
    set OGR_CMD=ogr2ogr -overwrite -f MSSQLSpatial -lco "GEOM_TYPE=geography" -a_srs "EPSG:4326" "MSSQL:server=%SERVERNAME%;database=%DBNAME%;trusted_connection=yes;driver=%driver%" "%SHPDIR%" -FieldTypeToString All
) else (
    set OGR_CMD=ogr2ogr -overwrite -f MSSQLSpatial -lco "GEOM_TYPE=geography" -s_srs "EPSG:%EPSG%" -t_srs "EPSG:4326" "MSSQL:server=%SERVERNAME%;database=%DBNAME%;trusted_connection=yes;driver=%driver%" "%SHPDIR%" -FieldTypeToString All
)

echo Running ogr2ogr command: >> "%LOGFILE%"
echo %OGR_CMD% >> "%LOGFILE%"

rem ---------------------------------
rem Execute ogr2ogr command
rem ---------------------------------
%OGR_CMD% >> "%LOGFILE%" 2>&1

if errorlevel 1 (
    echo ERROR: ogr2ogr command failed. Check log for details. >> "%LOGFILE%"
    echo ogr2ogr command failed. See %LOGFILE% for details.
    pause
    exit /b 1
) else (
    echo ogr2ogr command completed successfully. >> "%LOGFILE%"
    echo ogr2ogr completed successfully!
)

rem ---------------------------------
rem Remind the user of next steps
rem ---------------------------------
echo Import completed. >> "%LOGFILE%"
echo Review your database table in %DBNAME% to verify the imported data. >> "%LOGFILE%"
echo If needed, use MakeValid() or ReorientObject() in SQL as described in the documentation. >> "%LOGFILE%"
echo Conversion to WKT can be done in your local environment or by modifying the code as described. >> "%LOGFILE%"

echo.
echo Import completed successfully!
echo Please check %LOGFILE% for details.
echo Press any key to close...
pause
endlocal
