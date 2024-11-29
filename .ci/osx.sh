#!/bin/sh

# Install build tools
brew install ninja create-dmg

CMakeNinjaBuild() {
	# One time for x64
	mkdir build_$1_static_osx-x64
	cd build_$1_static_osx-x64
	cmake ../../externals/$1 \
		-DCMAKE_BUILD_TYPE=Release \
		-DCMAKE_C_COMPILER=clang \
		-DCMAKE_CXX_COMPILER=clang++ \
		-DCMAKE_OBJC_COMPILER=clang \
		-DCMAKE_OBJCXX_COMPILER=clang++ \
		-DCMAKE_OSX_ARCHITECTURES=x86_64 \
		-DCMAKE_SYSTEM_NAME=Darwin \
		-DCMAKE_SYSTEM_PROCESSOR=x86_64 \
		-G Ninja \
		-DGSE_SHARED=OFF
	ninja
	cd ..
	# Another time for arm64
	mkdir build_$1_static_osx-arm64
	cd build_$1_static_osx-arm64
	cmake ../../externals/$1 \
		-DCMAKE_BUILD_TYPE=Release \
		-DCMAKE_C_COMPILER=clang \
		-DCMAKE_CXX_COMPILER=clang++ \
		-DCMAKE_OBJC_COMPILER=clang \
		-DCMAKE_OBJCXX_COMPILER=clang++ \
		-DCMAKE_OSX_ARCHITECTURES=arm64 \
		-DCMAKE_SYSTEM_NAME=Darwin \
		-DCMAKE_SYSTEM_PROCESSOR=arm64 \
		-G Ninja \
		-DGSE_SHARED=OFF
	ninja
	cd ..
}

CMakeNinjaBuild cimgui
CMakeNinjaBuild SDL2
CMakeNinjaBuild gambatte
CMakeNinjaBuild mgba
CMakeNinjaBuild native_helper

# Build GSE
cd ..
dotnet publish -r osx-x64
dotnet publish -r osx-arm64

# Abort if the build failed for whatever reason
if [ ! -f output/osx-x64/publish/GSE ] || [ ! -f output/osx-arm64/publish/GSE ]; then
	echo "dotnet publish failed, aborting"
	exit 1
fi

# Create .app bundle structure
mkdir output/$TARGET_RID
mkdir output/$TARGET_RID/GSE.app
mkdir output/$TARGET_RID/GSE.app/Contents
mkdir output/$TARGET_RID/GSE.app/Contents/MacOS

# Merge the binaries together
lipo output/osx-x64/publish/GSE output/osx-arm64/publish/GSE -create -output output/$TARGET_RID/GSE.app/Contents/MacOS/GSE

# Add in Info.plist
cp GSE/Info.plist output/$TARGET_RID/GSE.app/Contents

# Resign the binary
codesign -s - --deep --force output/$TARGET_RID/GSE.app

# Output a dmg
mkdir output/$TARGET_RID/publish
create-dmg output/$TARGET_RID/publish/GSE.dmg output/$TARGET_RID/GSE.app
