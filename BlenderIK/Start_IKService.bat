cd ..\Services\BlenderIK
Blender\blender.exe resources\IKService_dennis.blend --background --python src\blenderik\__main__.py -- run %*
PAUSE