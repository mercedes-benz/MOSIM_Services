cd /D %~dp0
Blender\blender.exe resources\IKService_dennis.blend --background --python Blender\2.83\python\lib\site-packages\blenderik\__main__.py -- run %*
PAUSE
