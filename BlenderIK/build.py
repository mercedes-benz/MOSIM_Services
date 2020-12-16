from pathlib import Path
import requests
from zipfile import ZipFile
import os

BLENDER_FILENAME = 'blender-2.83.5-windows64.zip'
BLENDER_URL = f"https://ftp.halifax.rwth-aachen.de/blender/release/Blender2.83/{BLENDER_FILENAME}"
SERVICE_NAME = 'BlenderIKService'

RESSOURCES = Path('resources')
BUILD = Path('build')
DIST = Path('dist')

BUILD.mkdir(exist_ok=True)
DIST.mkdir(exist_ok=True)


blenderfile = RESSOURCES.joinpath(BLENDER_FILENAME)
if not blenderfile.exists() or not BUILD.joinpath("Blender").exists():
    print('blender-zip not found: Downloading')
    data = requests.get(BLENDER_URL)
    with blenderfile.open('wb') as file:
        file.write(data.content)
    print(f'Download done {blenderfile}')
else:
    print('zip found')

if not BUILD.joinpath("Blender").exists():
  print('unzip Blender to build')
  blenderzip = ZipFile(blenderfile)
  blenderzip.extractall(Path('build/'))
  Path('build/').joinpath(BLENDER_FILENAME[:-4]).rename('build/Blender')

install_cmd = f"""cmd /c .\{BUILD.joinpath('Blender/2.83/python/bin/python.exe')} -m pip install {Path.cwd()}/MMIPython/MMIStandard"""
print(install_cmd)
os.system(install_cmd)

install_cmd = f"""cmd /c .\{BUILD.joinpath('Blender/2.83/python/bin/python.exe')} -m pip install {Path.cwd()}/MMIPython/MMIPython"""
print(install_cmd)
os.system(install_cmd)

install_cmd = f"""cmd /c .\{BUILD.joinpath('Blender/2.83/python/bin/python.exe')} -m pip install {Path.cwd()}"""
print(install_cmd)
os.system(install_cmd)

servicefile = DIST.joinpath(SERVICE_NAME).with_suffix('.zip')
if servicefile.exists():
    print("removing old servicezip")
    servicefile.unlink()

print('Create Zip-File for distribution')
servicezip = ZipFile(servicefile, 'w')
servicezip.write(RESSOURCES.joinpath('IKService_dennis.blend'))
servicezip.write(RESSOURCES.joinpath('intermediate.mos'))
servicezip.write(RESSOURCES.joinpath('description.json'), arcname='description.json')
servicezip.write(RESSOURCES.joinpath('service.config'), arcname='service.config')
servicezip.write(RESSOURCES.joinpath('Start_IKService.bat'), arcname='Start_IKService.bat')
for file in BUILD.joinpath('Blender').rglob('*'):
    servicezip.write(file, arcname=file.relative_to(BUILD))

print('build completed')
