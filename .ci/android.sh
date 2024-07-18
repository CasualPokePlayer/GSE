#!/bin/sh

# This script expects to be running on Debian 11 under root

# Install build tools
apt-get install -y wget unzip openjdk-17-jdk-headless

# Get Android Command Line Tools
wget https://dl.google.com/android/repository/commandlinetools-linux-11076708_latest.zip -O $HOME/cmdline-tools.zip
unzip $HOME/cmdline-tools.zip -d $HOME

# Set ANDROID_HOME to somewhere known
export ANDROID_HOME=$HOME/.android_sdk
mkdir $ANDROID_HOME

# Move Command Line Tools over to its expected location
mkdir $ANDROID_HOME/cmdline-tools
mv $HOME/cmdline-tools $ANDROID_HOME/cmdline-tools/latest

# Install Android SDK and NDK
yes | $ANDROID_HOME/cmdline-tools/latest/bin/sdkmanager --licenses
$ANDROID_HOME/cmdline-tools/latest/bin/sdkmanager --install "platforms;android-34"
$ANDROID_HOME/cmdline-tools/latest/bin/sdkmanager --install "ndk;26.2.11394342"
$ANDROID_HOME/cmdline-tools/latest/bin/sdkmanager --install "cmake;3.22.1"

export ANDROID_NDK_ROOT="$ANDROID_HOME/ndk/26.2.11394342"
export PATH=$ANDROID_HOME/cmake/3.22.1/bin:$PATH

# Build all externals
cd ../externals/android
./build_all.sh

# Install dotnet8 sdk
wget https://dot.net/v1/dotnet-install.sh -O $HOME/dotnet-install.sh
chmod +x $HOME/dotnet-install.sh
$HOME/dotnet-install.sh --channel 8.0
export PATH=$HOME/.dotnet:$PATH

# Set path to find NDK's clang (needed to workaround .NET bug)
export PATH=$ANDROID_NDK_ROOT/toolchains/llvm/prebuilt/linux-x86_64/bin:$PATH

# Build libGSE
cd ../..
dotnet publish -r linux-bionic-arm64 -p:DisableUnsupportedError=true -p:PublishAotUsingRuntimePack=true
dotnet publish -r linux-bionic-x64 -p:DisableUnsupportedError=true -p:PublishAotUsingRuntimePack=true

# Gradle won't understand if libraries being missing means the build should fail, so check against failure here
if [ ! -f output/linux-bionic-arm64/publish/libGSE.so ] || [ ! -f output/linux-bionic-x64/publish/libGSE.so ]; then
	echo "dotnet publish failed, aborting"
	exit 1
fi

# Build java project
cd android
if [ -f $HOME/gse-release-keystore.jks ]; then
	./gradlew assembleRelease -Pkeystore="$HOME/gse-release-keystore.jks" -Pstorepass="$ANDROID_RELEASE_STOREPASS" -Pkeyalias="$ANDROID_RELEASE_KEYALIAS" -Pkeypass="$ANDROID_RELEASE_KEYPASS"
else
	./gradlew assembleRelease
fi

# Copy apk over to output/$TARGET_RID/publish (where our CI looks for artifacts)
cd ..
mkdir output/$TARGET_RID
mkdir output/$TARGET_RID/publish
cp -a -T android/app/build/outputs/apk/release/app-release.apk output/$TARGET_RID/publish/GSE.apk

# Also possibly build an app bundle (for Play Store submission)
if [ -f $HOME/gse-upload-keystore.jks ]; then
	cd android
	./gradlew bundleRelease -Pkeystore="$HOME/gse-upload-keystore.jks" -Pstorepass="$ANDROID_UPLOAD_STOREPASS" -Pkeyalias="$ANDROID_UPLOAD_KEYALIAS" -Pkeypass="$ANDROID_UPLOAD_KEYPASS"
	cd ..
	cp -a -T android/app/build/outputs/bundle/release/app-release.aab output/$TARGET_RID/publish/GSE.aab
fi
