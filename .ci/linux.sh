#!/bin/sh

# This script expects to be running on Debian 11 under root

# TODO: We definitely could build under Debian 10 (although cmake would require buster-backports, so Debian 9 is probably out of the question)
# but we lose out on some packages (and Debian 11 is probably old enough to cover practically every user?)

# Install some base tools
apt-get install -y wget lsb-release software-properties-common gpg cmake ninja-build pkg-config

# Install clang 18
wget https://apt.llvm.org/llvm.sh -O $HOME/llvm.sh
chmod +x $HOME/llvm.sh
$HOME/llvm.sh 18

# Enable backports packages
echo "deb http://deb.debian.org/debian bullseye-backports main" | tee /etc/apt/sources.list.d/backports.list
apt-get update

if [ $TARGET_RID = "linux-x64" ]; then
	# Nothing special needed here
	export EXTRA_CMAKE_ARGS=""
	# Install SDL2 dependencies
	apt-get install -y libasound2-dev libpulse-dev libaudio-dev libjack-jackd2-dev libsamplerate0-dev \
		libx11-dev libxext-dev libxrandr-dev libxcursor-dev libxfixes-dev libxi-dev \
		libxss-dev libwayland-dev libxkbcommon-dev libdrm-dev libgbm-dev libgl1-mesa-dev \
		libgles2-mesa-dev libegl1-mesa-dev libdbus-1-dev libibus-1.0-dev \
		fcitx-libs-dev libudev-dev libusb-1.0-0-dev pkg-config
	# More SDL2 dependencies only under backports
	apt-get install -y libdecor-0-dev/bullseye-backports libpipewire-0.3-dev/bullseye-backports
	# Install .NET AOT dependencies
	apt-get install -y zlib1g-dev
elif [ $TARGET_RID = "linux-arm64" ]; then
	# Install aarch64 cross compiling setup
	apt-get install -y gcc-aarch64-linux-gnu g++-aarch64-linux-gnu dpkg-dev
	# Setup pkg-config for cross compiling
	ln -s /usr/bin/aarch64-linux-gnu-pkg-config /usr/share/pkg-config-crosswrapper
	chmod +x /usr/bin/aarch64-linux-gnu-pkg-config
	export PKG_CONFIG=aarch64-linux-gnu-pkg-config
	# cmake cross compiler flags
	export EXTRA_CMAKE_ARGS="-DCMAKE_SYSTEM_NAME=Linux -DCMAKE_SYSTEM_PROCESSOR=aarch64 -DCMAKE_C_FLAGS=--target=aarch64-linux-gnu -DCMAKE_CXX_FLAGS=--target=aarch64-linux-gnu"
	# Enable ARM64 packages
	dpkg --add-architecture arm64
	apt-get update
	# Install SDL2 dependencies
	apt-get install -y libasound2-dev:arm64 libpulse-dev:arm64 libaudio-dev:arm64 libjack-jackd2-dev:arm64 libsamplerate0-dev:arm64 \
		libx11-dev:arm64 libxext-dev:arm64 libxrandr-dev:arm64 libxcursor-dev:arm64 libxfixes-dev:arm64 libxi-dev:arm64 \
		libxss-dev:arm64 libwayland-dev:arm64 libxkbcommon-dev:arm64 libdrm-dev:arm64 libgbm-dev:arm64 libgl1-mesa-dev:arm64 \
		libgles2-mesa-dev:arm64 libegl1-mesa-dev:arm64 libdbus-1-dev:arm64 libibus-1.0-dev:arm64 \
		fcitx-libs-dev:arm64 libudev-dev:arm64 libusb-1.0-0-dev:arm64
	# More SDL2 dependencies only under backports
	apt-get install -y libdecor-0-dev:arm64/bullseye-backports libpipewire-0.3-dev:arm64/bullseye-backports
	# Install .NET AOT dependencies
	apt-get install -y zlib1g-dev:arm64
else
	echo "TARGET_RID must be linux-x64 or linux-arm64 (got $TARGET_RID)"
	exit 1
fi

CMakeNinjaBuild() {
	mkdir build_$1_static_$TARGET_RID
	cd build_$1_static_$TARGET_RID
	cmake ../../externals/$1 \
		-DCMAKE_BUILD_TYPE=Release \
		-DCMAKE_C_COMPILER=clang-18 \
		-DCMAKE_CXX_COMPILER=clang++-18 \
		$EXTRA_CMAKE_ARGS \
		-G Ninja \
		-DGSR_SHARED=OFF
	ninja
	cd ..
}

CMakeNinjaBuild cimgui
CMakeNinjaBuild SDL2
CMakeNinjaBuild gambatte
CMakeNinjaBuild mgba
CMakeNinjaBuild export_helper

# Install dotnet8 sdk
wget https://dot.net/v1/dotnet-install.sh -O $HOME/dotnet-install.sh
chmod +x $HOME/dotnet-install.sh
$HOME/dotnet-install.sh --channel 8.0
export PATH=$HOME/.dotnet:$PATH

# Build GSR
cd ..
dotnet publish -r $TARGET_RID -p:CppCompilerAndLinker=clang-18 -p:LinkerFlavor=lld-18 -p:ObjCopyName=llvm-objcopy-18
