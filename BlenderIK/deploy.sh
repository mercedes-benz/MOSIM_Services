#!/bin/bash

BV=2.83
BD=Blender${BV}
PYCMD=python3.7m
BLENDER=blender-${BV}.10-linux64

if [ ! -d build ] ; then mkdir build ; fi

if [ ! -d build/Blender ] ; then
	curl https://ftp.halifax.rwth-aachen.de/blender/release/${BD}/${BLENDER}.tar.xz -o ${BLENDER}.tar.xz ;
	7z x ${BLENDER}.tar.xz ;
	tar xf ${BLENDER}.tar ;
	mv ${BLENDER} build/Blender ;
	rm ${BLENDER}.tar ;
	rm ${BLENDER}.tar.xz ;
	build/Blender/${BV}/python/bin/${PYCMD} -m ensurepip ;
	build/Blender/${BV}/python/bin/${PYCMD} -m pip install --upgrade pip ;
fi

MYPATH=${PWD}
cd ../../Core/Framework/LanguageSupport/python ; ./deploy.sh "${MYPATH}/build/Blender/${BV}/python/bin/${PYCMD} -m pip"

cd ${MYPATH}
cp version.txt build/
cp description.json build/
cp README.md build/
cp service.config build/
cp Start_IKService.sh build/

if [ ! -d build/src ] ; then
	cp -r src build/
fi

if [ ! -d build/resources ] ; then
	cp -r resources build/
fi
