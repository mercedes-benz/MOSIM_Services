@echo off
REM SPDX-License-Identifier: MIT
REM The content of this file has been developed in the context of the MOSIM research project.
REM Original author(s): Janis Sprenger, Bhuvaneshwaran Ilanthirayan

REM the ESC sign can be created by pressing left alt + 027 on the num-pad. 
ECHO.
ECHO _______________________________________________________
ECHO [33mdeploy_vs.bat[0m at %cd%\deploy_vs.bat Deploying the PostureBlendingService. 
ECHO _______________________________________________________
ECHO.


REM Checking environment variables
if not defined DEVENV (
  ECHO [31mDEVENV Environment variable pointing to the Visual Studio 2017 devenv.exe is missing.[0m
  ECHO    e.g. "SET DEVENV=C:\Program Files (x86)\Microsoft Visual Studio\2017\Professional\Common7\IDE\devenv.com"
  pause
  exit /b 1
) else (
  if not exist "%DEVENV%" (
    ECHO Visual Studio does not seem to be installed at "%DEVENV%" or path name in deploy_variables.bat is wrong.
    exit /b 2
  )
)

REM Build the Visual Studio Project
"%DEVENV%" .\PostureBlendingService.sln /Build Debug

REM If the build was sucessfull, copy all files to the respective build folders. 
if %ERRORLEVEL% EQU 0 (
  IF NOT EXIST .\build (
    mkdir .\build 
  ) ELSE (
    RMDIR /S/Q .\build
    mkdir .\build
  )
  REM cmd /c has to be called to prevent xcopy to destroy any coloring of outputs
  cmd /c xcopy /S .\PostureBlendingService\bin\Debug\* .\build
    
  ECHO [92mSuccessfully deployed PostureBlendingService[0m
  exit /b 0
) else (
  ECHO [31mDeployment of PostureBlendingService failed. [0m
  exit /b 1
)

pause