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

project(gsr_export_helper C)

if(MSVC)
	if(POLICY CMP0091)
	else()
		# We need this policy present for MSVC
		message(FATAL_ERROR "MSVC builds require at least CMake 3.15")
	endif()
endif()

option(GSR_SHARED "Build GSR lib as shared library" ON)

list(APPEND CMAKE_MODULE_PATH ${CMAKE_SOURCE_DIR}/../cmake)

include(FindRID)

set(CMAKE_C_STANDARD 99)
set(CMAKE_C_STANDARD_REQUIRED ON)
set(CMAKE_C_EXTENSIONS OFF)

if(GSR_SHARED)
	add_library(export_helper SHARED export_helper.c)
	target_compile_definitions(export_helper PRIVATE GSR_SHARED)
	set_target_properties(export_helper PROPERTIES POSITION_INDEPENDENT_CODE ON)
	set_target_properties(export_helper PROPERTIES INTERPROCEDURAL_OPTIMIZATION $<$<CONFIG:Debug>:OFF,ON>)
else()
	add_library(export_helper STATIC export_helper.c)
	set_target_properties(export_helper PROPERTIES INTERPROCEDURAL_OPTIMIZATION OFF)
	if(MSVC)
		set_target_properties(export_helper PROPERTIES MSVC_RUNTIME_LIBRARY MultiThreaded$<$<CONFIG:Debug>:Debug>)
	endif()
endif()

# Copy output to our runtime folders
add_custom_command(
	TARGET export_helper
	POST_BUILD
	COMMAND ${CMAKE_COMMAND}
	ARGS -E copy $<TARGET_FILE:export_helper> ${CMAKE_SOURCE_DIR}/../runtimes/${GSR_RID}/
)
