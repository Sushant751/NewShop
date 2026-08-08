@echo off
REM ===========================================================================
REM  BillingSystem - Database Setup Script
REM  Executes all SQL files in the correct order
REM
REM  Usage:  database\run-all.bat [server-name]
REM  Default: localhost
REM ===========================================================================

setlocal

set SERVER=%1
if "%SERVER%"=="" set SERVER=localhost

echo.
echo ============================================================
echo   BillingSystem Database Setup
echo   Server: %SERVER%
echo ============================================================
echo.

echo [1/3] Creating schema (01_Schema.sql)...
sqlcmd -S %SERVER% -E -i "%~dp001_Schema.sql" -b
if %ERRORLEVEL% NEQ 0 (
    echo ERROR: Schema creation failed.
    exit /b 1
)
echo     Done.
echo.

echo [2/3] Creating stored procedures (02_StoredProcedures.sql)...
sqlcmd -S %SERVER% -E -i "%~dp002_StoredProcedures.sql" -b
if %ERRORLEVEL% NEQ 0 (
    echo ERROR: Stored procedure creation failed.
    exit /b 1
)
echo     Done.
echo.

echo [3/3] Loading seed data (03_SeedData.sql)...
sqlcmd -S %SERVER% -E -i "%~dp003_SeedData.sql" -b
if %ERRORLEVEL% NEQ 0 (
    echo ERROR: Seed data loading failed.
    exit /b 1
)
echo     Done.
echo.

echo ============================================================
echo   Database setup complete!
echo.
echo   Admin login:    admin@billingsystem.com
echo   Admin password: Admin@123
echo ============================================================
echo.

endlocal
