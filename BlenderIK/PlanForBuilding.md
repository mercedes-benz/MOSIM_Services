# Notes to the build-process

- clear build

- Check if blender is in resources
	- if not: ask and download from https://www.blender.org/download/Blender2.83/blender-2.83.5-windows64.zip/
	
- unzip blender to build

- rename blender... to Blender2

- call build\Blender\2.83\python\bin\python.exe -m pip install <Thrift> (sollte von setup.py erledigt werden.)

- call build\Blender\2.83\python\bin\python.exe -m pip install <ikservice>

- copy additional files
	. version.txt?
	. description.json
	. Readme.md
	. service.config
	. start.bat
	. blend-file
	
- zip it!

- deploy?
	
# Offene Fragen

Wie addressiere ich die service.config vom Package aus? [commandozeilenbefehl?]

Benötigen wir eine Buildconfig?

	- blender-version
	- thrift-version