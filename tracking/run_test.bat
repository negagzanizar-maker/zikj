@echo off
REM ZIKJ Local Test Launcher (Windows Batch)
REM Double-click this file to start the test

setlocal enabledelayedexpansion

echo.
echo ======================================
echo ZIKJ LOCAL LAPTOP TEST
echo ======================================
echo.

REM Check Python
echo [1/4] Checking Python...
python --version >nul 2>&1
if errorlevel 1 (
    echo.      X Python not found
    echo.      Install from https://python.org
    pause
    exit /b 1
)
for /f "tokens=*" %%i in ('python --version') do set PYVER=%%i
echo.      OK %PYVER%

REM Go to tracking folder
echo [2/4] Setting up environment...
cd tracking
if not exist "tracking_simulator.py" (
    echo.      X tracking_simulator.py not found
    pause
    exit /b 1
)
echo.      OK Found simulator

REM Create venv if needed
if not exist "..\venv" (
    echo.      Creating virtual environment...
    python -m venv ..\venv
    echo.      OK Created
)

REM Activate venv
echo.      Activating virtual environment...
call ..\venv\Scripts\activate.bat
echo.      OK Activated

REM Install NumPy
echo [3/4] Installing dependencies...
pip install -q numpy >nul 2>&1
echo.      OK Dependencies ready

REM Run simulator
echo [4/4] Starting tracking simulator...
echo.
echo ======================================
echo OK TRACKING SIMULATOR RUNNING
echo ======================================
echo.
echo Next steps:
echo 1. Open Unity and press Play
echo 2. In Unity, press T to show tracking HUD
echo 3. Watch packets arrive (Packets counter incrementing)
echo.
echo To adjust (edit command below):
echo   --karts 3      : 3 karts instead of 6
echo   --fps 30       : 30 Hz instead of 60 Hz
echo.
echo ======================================
echo.

python tracking_simulator.py %*

pause
