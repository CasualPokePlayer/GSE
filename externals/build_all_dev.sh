#!/bin/sh

CMakeNinjaBuild() {
	cd $1
	rm -rf build
	mkdir build
	cd build
	cmake .. -DCMAKE_BUILD_TYPE=Release -G Ninja
	ninja
	cd ../..
}

CMakeNinjaBuild cimgui
CMakeNinjaBuild SDL2
CMakeNinjaBuild gambatte
CMakeNinjaBuild mgba
CMakeNinjaBuild native_helper
