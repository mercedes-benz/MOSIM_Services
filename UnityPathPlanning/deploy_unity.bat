@echo off
REM SPDX-License-Identifier: MIT
REM The content of this file has been developed in the context of the MOSIM research project.
REM Original author(s): Janis Sprenger, Bhuvaneshwaran Ilanthirayan

ECHO.
ECHO _______________________________________________________
ECHO [33mdeploy_unity.bat[0m at %cd%\deploy_unity.bat Deploying the Unity Path Planning project. 
ECHO _______________________________________________________
ECHO.


if not defined UNITY2019_18_1 (
  ECHO [31mUNITY2019_18_1 Environment variable pointing to the Unity.exe for Unity version 2019.18.1f1 is missing.[0m
  ECHO    e.g. SET "UNITY2019_18_1=C:\Program Files\Unity Environments\2018.4.1f1\Editor\Unity.exe\"
  ECHO UNITY2019_18_1 defined as: "%UNITY2019_18_1%"
  pause
  exit /b 1
) else (
  if not exist "%UNITY2019_18_1%" (
    ECHO Unity does not seem to be installed at "%UNITY2019_18_1%" or path name in deploy_variables.bat is wrong.
    exit /b 2
  )
)

IF EXIST UnityPathPlanningService\build (
  RD /S/Q UnityPathPlanningService\build
)

REM Build Unity Project:
ECHO Building the UnityPathPlanning project. This step may take some while, so please wait...
REM call "%UNITY%" -quit -batchmode -logFile stdout.log -projectPath . -buildWindowsPlayer "build/UnityAdapter.exe"
REM call "%UNITY2019_18_1%" -quit -batchmode -logFile build.log -projectPath .\UnityPathPlanningService -buildTarget Web
call "%UNITY2019_18_1%" -quit -batchmode -logFile build.log -projectPath ".\UnityPathPlanningService" -executeMethod BuildPathPlanning.CreateServerBuild 
if %ERRORLEVEL% EQU 0 (
  REM COPY .\configurations\avatar.mos build\
  COPY .\description.json .\UnityPathPlanningService\build\
  ECHO [92mSuccessfully deployed UnityPathPlanningService[0m
  exit /b 0
) else (
  ECHO [31mDeployment of UnityPathPlanningService failed. Please consider the build.log for more information. [0m
  exit /b 1
)