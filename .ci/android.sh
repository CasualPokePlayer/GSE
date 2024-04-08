#!/bin/sh

# This script expects to be running on Debian 11 under root

# Install build tools
apt-get install -y wget unzip cmake ninja-build openjdk-17-jdk-headless

# Get Android Command Line Tools
wget https://dl.google.com/android/repository/commandlinetools-linux-11076708_latest.zip -O commandlinetools.zip
unzip ./commandlinetools.zip

# Set ANDROID_HOME to somewhere known
export ANDROID_HOME=$HOME/.android_sdk
mkdir $ANDROID_HOME

# Move Command Line Tools over to its expected location
mkdir $ANDROID_HOME/cmdline-tools
mv ./cmdline-tools $ANDROID_HOME/cmdline-tools/latest

# Install Android SDK and NDK
yes | $ANDROID_HOME/cmdline-tools/latest/bin/sdkmanager --licenses
$ANDROID_HOME/cmdline-tools/latest/bin/sdkmanager --install "platforms;android-34"
$ANDROID_HOME/cmdline-tools/latest/bin/sdkmanager --install "ndk;26.2.11394342"

export ANDROID_NDK_ROOT="$ANDROID_HOME/ndk/26.2.11394342"

# Build all externals
cd ../externals/android
./build_all.sh

# Install dotnet8 sdk
wget https://dot.net/v1/dotnet-install.sh -O dotnet-install.sh
chmod +x ./dotnet-install.sh
./dotnet-install.sh --channel 8.0
export PATH=$PATH:$HOME/.dotnet

# Set path to find NDK's clang (needed to workaround .NET bug)
export PATH=$ANDROID_NDK_ROOT/toolchains/llvm/prebuilt/linux-x86_64/bin:$PATH

# Build libGSR
cd ../..
dotnet publish -r linux-bionic-arm64 -p:DisableUnsupportedError=true -p:PublishAotUsingRuntimePack=true
dotnet publish -r linux-bionic-x64 -p:DisableUnsupportedError=true -p:PublishAotUsingRuntimePack=true

# Build java project
cd android
if [ -f $HOME/gsr-release-keystore.jks ]; then
	./gradlew assembleRelease -Pkeystore=$HOME/gsr-release-keystore.jks -Pstorepass=$ANDROID_RELEASE_STOREPASS -Pkeyalias=$ANDROID_RELEASE_KEYALIAS -Pkeypass=$ANDROID_RELEASE_KEYPASS
else
	./gradlew assembleRelease --info
fi

# Copy apk over to output/$TARGET_RID/publish (where our CI looks for artifacts)
cd ..
mkdir output/$TARGET_RID
mkdir output/$TARGET_RID/publish
cp -a -T android/app/build/outputs/apk/release/app-release.apk output/$TARGET_RID/publish/GSR.apk
