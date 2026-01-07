@:: Note: this script is expecting a VS Developer Command Prompt environment and ANDROID_NDK_ROOT defined!

if "%ANDROID_NDK_ROOT%" == "" (
	echo "ANDROID_NDK_ROOT is not set"
	EXIT /b 1
)

call:CMakeNinjaBuild cimgui
call:CMakeNinjaBuild SDL3
call:CMakeNinjaBuild gambatte
call:CMakeNinjaBuild mgba
call:CMakeNinjaBuild native_helper
GOTO:EOF

:CMakeNinjaBuild
:: Build as ARM64
call:CMakeNinjaBuildAbi %~1 arm64 arm64-v8a
:: Build as x64
call:CMakeNinjaBuildAbi %~1 x64 x86_64
:: Build as ARM
call:CMakeNinjaBuildAbi %~1 arm armeabi-v7a
GOTO:EOF

:CMakeNinjaBuildAbi
mkdir build_%~1_%~2
cd build_%~1_%~2
cmake ..\..\%~1 ^
	-DCMAKE_TOOLCHAIN_FILE=%ANDROID_NDK_ROOT%\build\cmake\android.toolchain.cmake ^
	-DANDROID_ABI=%~3 ^
	-DANDROID_PLATFORM=android-21 ^
	-DANDROID_STL=c++_shared ^
	-DANDROID_SUPPORT_FLEXIBLE_PAGE_SIZES=ON ^
	-DCMAKE_BUILD_TYPE=Release ^
	-DGSE_SHARED=ON ^
	-G Ninja
ninja
cd ..
GOTO:EOF
