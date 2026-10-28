@echo off
title Tavrynexo Player - Windows Builder
echo ========================================================
echo        Tavrynexo Player - Quick Windows Build
echo ========================================================
powershell -ExecutionPolicy Bypass -File "%~dp0build_windows.ps1"
pause
