#!/bin/sh

# Note: this script is expecting ANDROID_NDK_ROOT defined!

if [ -z $ANDROID_NDK_ROOT ]; then
	echo "ANDROID_NDK_ROOT is not set"
	exit 1
fi

CMakeNinjaBuildAbi() {
	mkdir build_$1_$2
	cd build_$1_$2
	cmake ../../$1 \
		-DCMAKE_TOOLCHAIN_FILE=$ANDROID_NDK_ROOT/build/cmake/android.toolchain.cmake \
		-DANDROID_ABI=$3 \
		-DANDROID_PLATFORM=android-21 \
		-DANDROID_STL=c++_shared \
		-DCMAKE_BUILD_TYPE=Release \
		-DGSE_SHARED=ON \
		-G Ninja
	ninja
	cd ..
}

CMakeNinjaBuild() {
	# Build as ARM64
	CMakeNinjaBuildAbi $1 arm64 arm64-v8a
	# Build as x64
	CMakeNinjaBuildAbi $1 x64 x86_64
	# Build as ARM
	CMakeNinjaBuildAbi $1 arm armeabi-v7a
}

CMakeNinjaBuild cimgui
CMakeNinjaBuild SDL2
CMakeNinjaBuild gambatte
CMakeNinjaBuild mgba
CMakeNinjaBuild export_helper
