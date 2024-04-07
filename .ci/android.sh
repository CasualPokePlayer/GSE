#!/bin/sh

# Install ninja (this isn't already installed on CI)
sudo apt-get install -y ninja-build

# set ourselves to the latest NDK
export ANDROID_NDK=$ANDROID_NDK_LATEST_HOME
export ANDROID_NDK_HOME=$ANDROID_NDK_LATEST_HOME
export ANDROID_NDK_ROOT=$ANDROID_NDK_LATEST_HOME

# Build all externals
cd ../externals/android
./build_all.sh

# Set path to find NDK's clang (needed to workaround .NET bug)
export PATH=$ANDROID_NDK_ROOT/toolchains/llvm/prebuilt/linux-x86_64/bin:$PATH

# Build libGSR
cd ../..
dotnet publish -r linux-bionic-arm64 -p:DisableUnsupportedError=true -p:PublishAotUsingRuntimePack=true
dotnet publish -r linux-bionic-x64 -p:DisableUnsupportedError=true -p:PublishAotUsingRuntimePack=true

# Set our JAVA_HOME over to Java 17's
export JAVA_HOME=$JAVA_HOME_17_X64

# Build java project
cd android
echo "ndk.dir ${ANDROID_NDK_ROOT}" > local.properties
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
