cmake_minimum_required(VERSION 3.1...3.20 FATAL_ERROR)

# MSVC policies, introduced in CMake 3.20 / 3.15
if(POLICY CMP0117)
	cmake_policy(SET CMP0117 NEW)
	cmake_policy(SET CMP0092 NEW)
	cmake_policy(SET CMP0091 NEW)
endif()

if(POLICY CMP0069)
	cmake_policy(SET CMP0069 NEW)
endif()

# .NET 8 macOS requires at least 10.15
set(CMAKE_OSX_DEPLOYMENT_TARGET 10.15)

project(gambatte-gsr CXX)

if(MSVC)
	if(POLICY CMP0117)
	else()
		message(FATAL_ERROR "MSVC builds require at least CMake 3.20")
	endif()
endif()

option(GSR_SHARED "Build GSR lib as shared library" ON)

list(APPEND CMAKE_MODULE_PATH ${CMAKE_SOURCE_DIR}/../cmake)

include(FindRID)

set(CMAKE_CXX_STANDARD 11)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(CMAKE_CXX_EXTENSIONS OFF)

# exports don't have default visibility attributes, so can't apply hidden for a shared library
if(GSR_SHARED)
	set(CMAKE_CXX_VISIBILITY_PRESET default)
else()
	set(CMAKE_CXX_VISIBILITY_PRESET hidden)
endif()

set(CMAKE_VISIBILITY_INLINES_HIDDEN ON)

if(MSVC)
	# This kind of hack is required to disable exceptions for MSVC
	string(REPLACE "/EHsc" "" CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS}")
endif()

set(GAMBATTE_SOURCE_DIR ${CMAKE_SOURCE_DIR}/gambatte-core/libgambatte/src)
set(GAMBATTE_SOURCES
	${GAMBATTE_SOURCE_DIR}/bitmap_font.cpp
	${GAMBATTE_SOURCE_DIR}/cinterface.cpp
	${GAMBATTE_SOURCE_DIR}/cpu.cpp
	${GAMBATTE_SOURCE_DIR}/gambatte.cpp
	${GAMBATTE_SOURCE_DIR}/initstate.cpp
	${GAMBATTE_SOURCE_DIR}/interrupter.cpp
	${GAMBATTE_SOURCE_DIR}/interruptrequester.cpp
	${GAMBATTE_SOURCE_DIR}/loadres.cpp
	${GAMBATTE_SOURCE_DIR}/memory.cpp
	${GAMBATTE_SOURCE_DIR}/newstate.cpp
	${GAMBATTE_SOURCE_DIR}/sound.cpp
	${GAMBATTE_SOURCE_DIR}/state_osd_elements.cpp
	${GAMBATTE_SOURCE_DIR}/statesaver.cpp
	${GAMBATTE_SOURCE_DIR}/tima.cpp
	${GAMBATTE_SOURCE_DIR}/video.cpp
	${GAMBATTE_SOURCE_DIR}/file/file.cpp
	${GAMBATTE_SOURCE_DIR}/file/crc32.cpp
	${GAMBATTE_SOURCE_DIR}/mem/camera.cpp
	${GAMBATTE_SOURCE_DIR}/mem/cartridge.cpp
	${GAMBATTE_SOURCE_DIR}/mem/huc3_chip.cpp
	${GAMBATTE_SOURCE_DIR}/mem/infrared.cpp
	${GAMBATTE_SOURCE_DIR}/mem/memptrs.cpp
	${GAMBATTE_SOURCE_DIR}/mem/pakinfo.cpp
	${GAMBATTE_SOURCE_DIR}/mem/remote.cpp
	${GAMBATTE_SOURCE_DIR}/mem/rtc.cpp
	${GAMBATTE_SOURCE_DIR}/mem/sgb.cpp
	${GAMBATTE_SOURCE_DIR}/mem/time.cpp
	${GAMBATTE_SOURCE_DIR}/mem/mbc/huc1.cpp
	${GAMBATTE_SOURCE_DIR}/mem/mbc/huc3.cpp
	${GAMBATTE_SOURCE_DIR}/mem/mbc/m161.cpp
	${GAMBATTE_SOURCE_DIR}/mem/mbc/mbc0.cpp
	${GAMBATTE_SOURCE_DIR}/mem/mbc/mbc1.cpp
	${GAMBATTE_SOURCE_DIR}/mem/mbc/mbc2.cpp
	${GAMBATTE_SOURCE_DIR}/mem/mbc/mbc3.cpp
	${GAMBATTE_SOURCE_DIR}/mem/mbc/mbc5.cpp
	${GAMBATTE_SOURCE_DIR}/mem/mbc/mmm01.cpp
	${GAMBATTE_SOURCE_DIR}/mem/mbc/pocket_camera.cpp
	${GAMBATTE_SOURCE_DIR}/mem/mbc/wisdom_tree.cpp
	${GAMBATTE_SOURCE_DIR}/mem/snes_spc/dsp.cpp
	${GAMBATTE_SOURCE_DIR}/mem/snes_spc/SNES_SPC.cpp
	${GAMBATTE_SOURCE_DIR}/mem/snes_spc/SNES_SPC_misc.cpp
	${GAMBATTE_SOURCE_DIR}/mem/snes_spc/SNES_SPC_state.cpp
	${GAMBATTE_SOURCE_DIR}/mem/snes_spc/spc.cpp
	${GAMBATTE_SOURCE_DIR}/mem/snes_spc/SPC_DSP.cpp
	${GAMBATTE_SOURCE_DIR}/mem/snes_spc/SPC_Filter.cpp
	${GAMBATTE_SOURCE_DIR}/sound/channel1.cpp
	${GAMBATTE_SOURCE_DIR}/sound/channel2.cpp
	${GAMBATTE_SOURCE_DIR}/sound/channel3.cpp
	${GAMBATTE_SOURCE_DIR}/sound/channel4.cpp
	${GAMBATTE_SOURCE_DIR}/sound/duty_unit.cpp
	${GAMBATTE_SOURCE_DIR}/sound/envelope_unit.cpp
	${GAMBATTE_SOURCE_DIR}/sound/length_counter.cpp
	${GAMBATTE_SOURCE_DIR}/video/ly_counter.cpp
	${GAMBATTE_SOURCE_DIR}/video/lyc_irq.cpp
	${GAMBATTE_SOURCE_DIR}/video/next_m0_time.cpp
	${GAMBATTE_SOURCE_DIR}/video/ppu.cpp
	${GAMBATTE_SOURCE_DIR}/video/sprite_mapper.cpp
)

if(GSR_SHARED)
	add_library(gambatte SHARED ${GAMBATTE_SOURCES})
	set_target_properties(gambatte PROPERTIES POSITION_INDEPENDENT_CODE ON)
	set_target_properties(gambatte PROPERTIES INTERPROCEDURAL_OPTIMIZATION $<$<CONFIG:Debug>:OFF,ON>)
else()
	add_library(gambatte STATIC ${GAMBATTE_SOURCES})
	set_target_properties(gambatte PROPERTIES INTERPROCEDURAL_OPTIMIZATION OFF)
	set_target_properties(gambatte PROPERTIES MSVC_RUNTIME_LIBRARY MultiThreaded$<$<CONFIG:Debug>:Debug>)
endif()

target_compile_definitions(gambatte PRIVATE HAVE_STDINT_H REVISION=-1)
target_include_directories(gambatte PRIVATE
	${CMAKE_SOURCE_DIR}/gambatte-core/common
	${CMAKE_SOURCE_DIR}/gambatte-core/libgambatte/include
	${CMAKE_SOURCE_DIR}/gambatte-core/libgambatte/src
)

# Set compiler specific options
if(MSVC)
	# shut up the compiler
	target_compile_definitions(gambatte PRIVATE _CRT_SECURE_NO_WARNINGS)
	# Source files are UTF-8
	target_compile_options(gambatte PRIVATE /utf-8)
	# Enforce standard conformance
	target_compile_options(gambatte PRIVATE /permissive- /volatile:iso /fp:precise)
	# No exceptions
	target_compile_options(gambatte PRIVATE /EHa-s-)
	target_compile_definitions(gambatte PRIVATE _HAS_EXCEPTIONS=0)
	# No RTTI
	target_compile_options(gambatte PRIVATE /GR-)
else()
	target_compile_options(gambatte PRIVATE -fno-exceptions -fno-rtti)
	if(GSR_SHARED)
		target_compile_options(gambatte PRIVATE $<$<CONFIG:RELEASE>:-s>)
		if(CMAKE_C_COMPILER_ID MATCHES "Clang")
			target_compile_options(gambatte PRIVATE -fuse-ld=lld)
		endif()
	endif()
	target_compile_options(gambatte PRIVATE $<$<CONFIG:DEBUG>:-ggdb>)
endif()

# Copy output to our runtime folders
add_custom_command(
	TARGET gambatte
	POST_BUILD
	COMMAND ${CMAKE_COMMAND}
	ARGS -E copy $<TARGET_FILE:gambatte> ${CMAKE_SOURCE_DIR}/../runtimes/${GSR_RID}/
)
