# ZIKJ Local Test Launcher
# Run this to automatically start tracking simulator

# Colors
$Green = [System.ConsoleColor]::Green
$Yellow = [System.ConsoleColor]::Yellow
$Red = [System.ConsoleColor]::Red

Write-Host "======================================" -ForegroundColor $Green
Write-Host "ZIKJ LOCAL LAPTOP TEST" -ForegroundColor $Green
Write-Host "======================================" -ForegroundColor $Green
Write-Host ""

# Check Python installed
Write-Host "[1/4] Checking Python..." -ForegroundColor $Yellow
$pythonVersion = python --version 2>&1
if ($pythonVersion -like "Python 3*") {
    Write-Host "      ✓ $pythonVersion" -ForegroundColor $Green
} else {
    Write-Host "      ✗ Python not found or wrong version" -ForegroundColor $Red
    Write-Host "      Install Python 3.8+ from https://python.org"
    exit 1
}

# Navigate to tracking folder
Write-Host "[2/4] Setting up environment..." -ForegroundColor $Yellow
cd tracking
if (Test-Path "tracking_simulator.py") {
    Write-Host "      ✓ Found tracking simulator" -ForegroundColor $Green
} else {
    Write-Host "      ✗ tracking_simulator.py not found" -ForegroundColor $Red
    exit 1
}

# Create venv if not exists
if (-not (Test-Path "../venv")) {
    Write-Host "      Creating Python virtual environment..." -ForegroundColor $Yellow
    python -m venv ../venv
    Write-Host "      ✓ Virtual environment created" -ForegroundColor $Green
}

# Activate venv
Write-Host "      Activating virtual environment..." -ForegroundColor $Yellow
& "..\venv\Scripts\Activate.ps1"
Write-Host "      ✓ Virtual environment activated" -ForegroundColor $Green

# Install NumPy if needed
Write-Host "[3/4] Installing dependencies..." -ForegroundColor $Yellow
pip install -q numpy 2>&1 | Out-Null
Write-Host "      ✓ Dependencies ready" -ForegroundColor $Green

# Start simulator
Write-Host "[4/4] Starting tracking simulator..." -ForegroundColor $Yellow
Write-Host ""
Write-Host "======================================" -ForegroundColor $Green
Write-Host "✓ TRACKING SIMULATOR RUNNING" -ForegroundColor $Green
Write-Host "======================================" -ForegroundColor $Green
Write-Host ""
Write-Host "Next steps:" -ForegroundColor $Yellow
Write-Host "1. Open Unity and press Play" -ForegroundColor $Yellow
Write-Host "2. In Unity, press T to show tracking HUD" -ForegroundColor $Yellow
Write-Host "3. Watch packets arrive (Packets counter incrementing)" -ForegroundColor $Yellow
Write-Host ""
Write-Host "To adjust:" -ForegroundColor $Yellow
Write-Host "  --karts 3      : 3 karts instead of 6" -ForegroundColor $Yellow
Write-Host "  --fps 30       : 30 Hz instead of 60 Hz" -ForegroundColor $Yellow
Write-Host ""
Write-Host "======================================" -ForegroundColor $Green
Write-Host ""

python tracking_simulator.py @args
