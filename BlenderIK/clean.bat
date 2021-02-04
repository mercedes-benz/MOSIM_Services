@echo off
REM SPDX-License-Identifier: MIT
REM The content of this file has been developed in the context of the MOSIM research project.
REM Original author(s): Bhuvaneshwaran Ilanthirayan


IF EXIST build ( 
  RMDIR /S/Q build
)

set mypath=%~dp0
cd ..\..\Core\Framework\LanguageSupport\python
call .\clean.bat
cd %mypath%

if %ERRORLEVEL% EQU 0 (
  ECHO [92mSuccessfully cleaned the blender IK Service [0m
  exit /b 0
) else (
  ECHO [31mCleaning of the blender IK Service failed. [0m
  exit /b 1
)