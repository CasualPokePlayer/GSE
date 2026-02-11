@:: Note: this script is expecting a VS Developer Command Prompt environment!

if "%TARGET_RID%" == "win-x64" (
	SET EXTRA_CMAKE_ARGS=
) else if "%TARGET_RID%" == "win-arm64" (
	SET EXTRA_CMAKE_ARGS=-DCMAKE_SYSTEM_NAME=Windows -DCMAKE_SYSTEM_PROCESSOR=ARM64 -DCMAKE_C_FLAGS=--target=arm64-pc-windows-msvc -DCMAKE_CXX_FLAGS=--target=arm64-pc-windows-msvc
) else if "%TARGET_RID%" == "win-x86" (
	SET EXTRA_CMAKE_ARGS=-DCMAKE_SYSTEM_NAME=Windows -DCMAKE_SYSTEM_PROCESSOR=X86 -DCMAKE_C_FLAGS=--target=i686-pc-windows-msvc -DCMAKE_CXX_FLAGS=--target=i686-pc-windows-msvc
) else (
	echo "Invalid TARGET_RID (got %TARGET_RID%)"
	EXIT /b 1
)

call:CMakeNinjaBuild cimgui
call:CMakeNinjaBuild SDL3
call:CMakeNinjaBuild gambatte
call:CMakeNinjaBuild mgba
call:CMakeNinjaBuild mesen
call:CMakeNinjaBuild native_helper

:: Build GSE
cd ..
dotnet publish -r %TARGET_RID%
GOTO:EOF

:CMakeNinjaBuild
:: Build %~1
mkdir build_%~1_static_%TARGET_RID%
cd build_%~1_static_%TARGET_RID%
cmake ..\..\externals\%~1 ^
	-DCMAKE_BUILD_TYPE=Release ^
	-DCMAKE_C_COMPILER=clang-cl ^
	-DCMAKE_CXX_COMPILER=clang-cl ^
	%EXTRA_CMAKE_ARGS% ^
	-G Ninja ^
	-DGSE_SHARED=OFF
ninja
cd ..
GOTO:EOF
