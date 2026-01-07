call:CMakeNinjaBuild cimgui
call:CMakeNinjaBuild SDL3
call:CMakeNinjaBuild gambatte
call:CMakeNinjaBuild mgba
call:CMakeNinjaBuild native_helper
GOTO:EOF

:CMakeNinjaBuild
:: Build %~1
cd %~1
rmdir /s /q build
mkdir build
cd build
cmake .. -DCMAKE_BUILD_TYPE=Release -DCMAKE_C_COMPILER=clang-cl -DCMAKE_CXX_COMPILER=clang-cl -G Ninja
ninja
cd ..\..
GOTO:EOF
