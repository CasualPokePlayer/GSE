cmake_minimum_required(VERSION 3.1...3.20 FATAL_ERROR)

if(POLICY CMP0069)
	cmake_policy(SET CMP0069 NEW)
endif()

# MSVC policies, introduced in CMake 3.15
if(POLICY CMP0092)
	cmake_policy(SET CMP0091 NEW)
	cmake_policy(SET CMP0092 NEW)
endif()

# .NET 8 macOS requires at least 10.15
set(CMAKE_OSX_DEPLOYMENT_TARGET 10.15)

project(gse_native_helper C)

if(MSVC)
	if(POLICY CMP0091)
	else()
		# We need this policy present for MSVC
		message(FATAL_ERROR "MSVC builds require at least CMake 3.15")
	endif()
endif()

if(APPLE)
	enable_language(OBJC)
endif()

option(GSE_SHARED "Build GSE lib as shared library" ON)

list(APPEND CMAKE_MODULE_PATH ${CMAKE_SOURCE_DIR}/../cmake)

include(FindRID)

set(CMAKE_C_STANDARD 99)
set(CMAKE_C_STANDARD_REQUIRED ON)
set(CMAKE_C_EXTENSIONS OFF)

if(APPLE)
	set(CMAKE_OBJC_STANDARD 99)
	set(CMAKE_OBJC_STANDARD_REQUIRED ON)
	set(CMAKE_OBJC_EXTENSIONS OFF)
endif()

if(GSE_SHARED)
	add_library(native_helper SHARED export_helper.c)
	target_compile_definitions(native_helper PRIVATE GSE_SHARED)
	set_target_properties(native_helper PROPERTIES POSITION_INDEPENDENT_CODE ON)
	set_target_properties(native_helper PROPERTIES INTERPROCEDURAL_OPTIMIZATION $<$<CONFIG:Debug>:OFF,ON>)
else()
	add_library(native_helper STATIC export_helper.c)
	set_target_properties(native_helper PROPERTIES INTERPROCEDURAL_OPTIMIZATION OFF)
	if(MSVC)
		set_target_properties(native_helper PROPERTIES MSVC_RUNTIME_LIBRARY MultiThreaded$<$<CONFIG:Debug>:Debug>)
	endif()
endif()

if(APPLE)
	target_sources(native_helper PRIVATE cocoa_helper.m)
endif()

# Copy output to our runtime folders
add_custom_command(
	TARGET native_helper
	POST_BUILD
	COMMAND ${CMAKE_COMMAND}
	ARGS -E copy $<TARGET_FILE:native_helper> ${CMAKE_SOURCE_DIR}/../runtimes/${GSE_RID}/
)
