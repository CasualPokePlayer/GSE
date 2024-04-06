#!/bin/sh

# Note: this script is expecting ANDROID_NDK_ROOT defined!

if [ -z $ANDROID_NDK_ROOT ]; then
	echo "ANDROID_NDK_ROOT is not set"
	exit 1
fi

CMakeNinjaBuild() {
	# Build as ARM64
	mkdir build_$1_arm64
	cd build_$1_arm64
	cmake ../../$1 \
		-DCMAKE_TOOLCHAIN_FILE=$ANDROID_NDK_ROOT/build/cmake/android.toolchain.cmake \
		-DANDROID_ABI=arm64-v8a \
		-DANDROID_PLATFORM=android-21 \
		-DANDROID_STL=c++_static \
		-DCMAKE_BUILD_TYPE=Release \
		-G Ninja
	ninja
	cd ..
	# Build as x64
	mkdir build_$1_x64
	cd build_$1_x64
	cmake ../../$1 ^
		-DCMAKE_TOOLCHAIN_FILE=$ANDROID_NDK_ROOT/build/cmake/android.toolchain.cmake \
		-DANDROID_ABI=x86_64 \
		-DANDROID_PLATFORM=android-21 \
		-DANDROID_STL=c++_static \
		-DCMAKE_BUILD_TYPE=Release \
		-G Ninja
	ninja
	cd ..
}

CMakeNinjaBuild cimgui
CMakeNinjaBuild SDL2
CMakeNinjaBuild gambatte
CMakeNinjaBuild mgba
CMakeNinjaBuild export_helper
