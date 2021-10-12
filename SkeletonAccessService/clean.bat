@echo off
REM SPDX-License-Identifier: MIT
REM The content of this file has been developed in the context of the MOSIM research project.
REM Original author(s): Bhuvaneshwaran Ilanthirayan

REM the ESC sign can be created by pressing left alt + 027 on the num-pad. 

REM Checking environment variables
if not defined DEVENV (
  ECHO [31mDEVENV Environment variable pointing to the Visual Studio 2017 devenv.exe is missing.[0m
  ECHO    e.g. "SET DEVENV=C:\Program Files (x86)\Microsoft Visual Studio\2017\Professional\Common7\IDE\devenv.com"
  pause
  exit /b 1
) else (
  ECHO DEVENV defined as: "%DEVENV%"
)

REM Build the Visual Studio Project
"%MSBUILD%" .\SkeletonAccessService.sln -t:clean -flp:logfile=clean.log


REM If the cleaning is sucessfull, delete all files to the respective build folders. 
if %ERRORLEVEL% EQU 0 (
  IF EXIST .\build (
    RMDIR /S/Q .\build
  )
  
  ECHO [92mSuccessfully removed the SkeletonAccessService[0m
  exit /b 0
) else (
  ECHO [31mRemoval of SkeletonAccessService failed. [0m
  exit /b 1
)

pause