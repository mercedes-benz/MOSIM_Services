@echo off
if not defined UNITY2018_4_1 (
  ECHO [31mUNITY2018_4_1 Environment variable pointing to the Unity.exe for Unity version 2018.4.1f1 is missing.[0m
  ECHO    e.g. SET "UNITY2018_4_1=C:\Program Files\Unity Environments\2018.4.1f1\Editor\Unity.exe\"
  pause
  exit /b 1
) else (
  ECHO UNITY2018_4_1 defined as: "%UNITY2018_4_1%"
)

IF EXIST UnityPathPlanningService\build (
  RD /S/Q UnityPathPlanningService\build
)

REM Build Unity Project:

REM call "%UNITY%" -quit -batchmode -logFile stdout.log -projectPath . -buildWindowsPlayer "build/UnityAdapter.exe"
call "%UNITY2018_4_1%" -quit -batchmode -logFile build.log -projectPath .\UnityPathPlanningService -buildWindowsPlayer "build\UnityPathPlanningService.exe"

if %ERRORLEVEL% EQU 0 (
  REM COPY .\configurations\avatar.mos build\
  COPY .\description.json .\UnityPathPlanningService\build\
  ECHO [92mSuccessfully deployed UnityPathPlanningService[0m
  exit /b 0
) else (
  ECHO [31mDeployment of UnityPathPlanningService failed. Please consider the build.log for more information. [0m
  exit /b 1
)