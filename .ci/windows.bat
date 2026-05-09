:: Find the VS install path (so the correct VS build environment is set)
FOR /F "delims=" %%I IN (
	'CALL "%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe" -latest -property installationPath'
) DO (
	SET VS_INSTALL_PATH=%%I
)

if "%TARGET_RID%" == "win-x64" (
	CALL "%VS_INSTALL_PATH%\VC\Auxiliary\Build\vcvars64.bat"
	SET EXTRA_CMAKE_ARGS=
) else if "%TARGET_RID%" == "win-arm64" (
	CALL "%VS_INSTALL_PATH%\VC\Auxiliary\Build\vcvarsamd64_arm64.bat"
	SET EXTRA_CMAKE_ARGS=-DCMAKE_SYSTEM_NAME=Windows -DCMAKE_SYSTEM_PROCESSOR=ARM64 -DCMAKE_C_FLAGS=--target=arm64-pc-windows-msvc -DCMAKE_CXX_FLAGS=--target=arm64-pc-windows-msvc
) else if "%TARGET_RID%" == "win-x86" (
	CALL "%VS_INSTALL_PATH%\VC\Auxiliary\Build\vcvarsamd64_x86.bat"
	SET EXTRA_CMAKE_ARGS=-DCMAKE_SYSTEM_NAME=Windows -DCMAKE_SYSTEM_PROCESSOR=X86 -DCMAKE_C_FLAGS=--target=i686-pc-windows-msvc -DCMAKE_CXX_FLAGS=--target=i686-pc-windows-msvc
) else (
	echo "Invalid TARGET_RID (got %TARGET_RID%)"
	EXIT /b 1
)

call:CMakeNinjaBuild cimgui
call:CMakeNinjaBuild SDL3
call:CMakeNinjaBuild gambatte
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
