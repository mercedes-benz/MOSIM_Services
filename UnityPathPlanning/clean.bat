@echo off
REM SPDX-License-Identifier: MIT
REM The content of this file has been developed in the context of the MOSIM research project.
REM Original author(s): Bhuvaneshwaran Ilanthirayan

IF EXIST UnityPathPlanningService\build (
  RD /S/Q UnityPathPlanningService\build
)

if %ERRORLEVEL% EQU 0 (
  ECHO [92mSuccessfully cleaned UnityPathPlanningService[0m
  exit /b 0
) else (
  ECHO [31mCleaning of UnityPathPlanningService failed. Please consider the build.log for more information. [0m
  exit /b 1
)