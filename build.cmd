@echo off
cls

.paket\paket.bootstrapper.exe
if errorlevel 1 (
	exit /b %errorlevel1%
)

.paket\paket.exe restore
if errorlevel 1 (
	exit /b %errorlevel1%
)

packages\FAKE\tools\FAKE.exe build.fsx %*