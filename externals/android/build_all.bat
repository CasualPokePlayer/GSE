@:: Note: this script is expecting a VS Developer Command Prompt environment and ANDROID_NDK_ROOT defined!

if "%ANDROID_NDK_ROOT%" == "" (
	echo "ANDROID_NDK_ROOT is not set"
	EXIT /b 1
)

call:CMakeNinjaBuild cimgui
call:CMakeNinjaBuild SDL2
call:CMakeNinjaBuild gambatte
call:CMakeNinjaBuild mgba
call:CMakeNinjaBuild export_helper
GOTO:EOF

:CMakeNinjaBuild
:: Build %~1 as ARM64
mkdir build_%~1_arm64
cd build_%~1_arm64
cmake ..\..\%~1 ^
	-DCMAKE_TOOLCHAIN_FILE=%ANDROID_NDK_ROOT%\build\cmake\android.toolchain.cmake ^
	-DANDROID_ABI=arm64-v8a ^
	-DANDROID_PLATFORM=android-21 ^
	-DANDROID_STL=c++_static ^
	-DCMAKE_BUILD_TYPE=Release ^
	-G Ninja
ninja
cd ..
:: Build %~1 as x64
mkdir build_%~1_x64
cd build_%~1_x64
cmake ..\..\%~1 ^
	-DCMAKE_TOOLCHAIN_FILE=%ANDROID_NDK_ROOT%\build\cmake\android.toolchain.cmake ^
	-DANDROID_ABI=x86_64 ^
	-DANDROID_PLATFORM=android-21 ^
	-DANDROID_STL=c++_static ^
	-DCMAKE_BUILD_TYPE=Release ^
	-G Ninja
ninja
cd ..
GOTO:EOF
