@echo off
REM SPDX-License-Identifier: MIT
REM The content of this file has been developed in the context of the MOSIM research project.
REM Original author(s): Bhuvaneshwaran Ilanthirayan

REM the ESC sign can be created by pressing left alt + 027 on the num-pad. 

REM Checking environment variables
if not defined DEVENV (
  ECHO [31mDEVENV Environment variable pointing to the Visual Studio 2017 devenv.exe is missing.[0m
  ECHO    e.g. "SET DEVENV=C:\Program Files (x86)\Microsoft Visual Studio\2017\Professional\Common7\IDE\devenv.com"
  exit /b 1
) else (
  ECHO DEVENV defined as: "%DEVENV%"
)

if not defined MSBUILD (
  ECHO [31mMSBUILD Environment variable pointing to the Visual Studio 2017 MSBuild.exe is missing.[0m
  ECHO    e.g. "C:\Program Files (x86)\Microsoft Visual Studio\2019\Professional\MSBuild\Current\Bin\MSBuild.exe"
  pause
  exit /b 1
) else (
  if not exist "%MSBUILD%" (
    ECHO    MSBUILD: [31mMISSING[0m at "%MSBUILD%"
    ECHO [31mPlease update the deploy_variables.bat script with a valid path![0m
	exit /b 2
  )
)
)

REM Build the Visual Studio Project
REM "%DEVENV%" .\PostureBlendingService.sln /Clean
"%MSBUILD%" -t:clean -flp:logfile=clean.log


REM If the cleaning is sucessfull, deleting all files from the respective build folders. 
if %ERRORLEVEL% EQU 0 (
  IF EXIST .\build (
    RMDIR /S/Q .\build
  )

  ECHO [92mSuccessfully cleaned PostureBlendingService[0m
  exit /b 0
) else (
  ECHO [31mCleaning of PostureBlendingService failed. [0m
  exit /b 1
)
