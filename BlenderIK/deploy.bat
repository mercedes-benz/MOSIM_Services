REM @echo off

IF NOT EXIST blender-2.83.10-windows64.zip (
  powershell -Command "Invoke-WebRequest https://ftp.halifax.rwth-aachen.de/blender/release/Blender2.83/blender-2.83.10-windows64.zip -OutFile blender-2.83.10-windows64.zip"
)
IF NOT EXIST build ( 
  md build
)
REM setting up blender
IF NOT EXIST build/Blender (
  powershell -Command "Expand-Archive -Path blender-2.83.10-windows64.zip -DestinationPath build/"
  rename build\blender-2.83.10-windows64 Blender
)

set mypath=%~dp0
build\Blender\2.83\python\bin\python.exe -m ensurepip
REM build\Blender\2.83\python\bin\python.exe -m pip install --upgrade pip setuptools wheel

cd ..\..\Core\Framework\LanguageSupport\python
call .\deploy.bat "%mypath%\build\Blender\2.83\python\bin\python.exe -m pip"
cd %mypath%

COPY version.txt build\
COPY description.json build\
COPY Readme.md build\
COPY service.config build\
COPY Start_IKService.bat build\
md build\resources
md build\src
cmd /c xcopy /S/E/Y .\resources .\build\resources
cmd /c xcopy /S/E/Y .\src .\build\src


if %ERRORLEVEL% EQU 0 (
  REM COPY .\configurations\avatar.mos build\
  ECHO [92mSuccessfully deployed the blender IK Service [0m
  exit /b 0
) else (
  ECHO [31mDeployment of the blender IK Service failed. [0m
  exit /b 1
)