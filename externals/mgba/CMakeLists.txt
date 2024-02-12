cmake_minimum_required(VERSION 3.1 FATAL_ERROR)

if(POLICY CMP0069)
	cmake_policy(SET CMP0069 NEW)
	set(CMAKE_POLICY_DEFAULT_CMP0069 NEW)
endif()

# MSVC policies, introduced in CMake 3.15
if(POLICY CMP0092)
	cmake_policy(SET CMP0091 NEW)
	cmake_policy(SET CMP0092 NEW)
	set(CMAKE_POLICY_DEFAULT_CMP0091 NEW)
	set(CMAKE_POLICY_DEFAULT_CMP0092 NEW)
endif()

project(mgba-gsr C)

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

set(CMAKE_C_STANDARD 11)
set(CMAKE_C_STANDARD_REQUIRED ON)
set(CMAKE_C_EXTENSIONS OFF)
set(CMAKE_C_VISIBILITY_PRESET hidden)

set(LIBMGBA_ONLY ON)
set(M_CORE_GB OFF)
set(MINIMAL_CORE ON)
set(BINARY_NAME mgba-static)

add_subdirectory(mgba EXCLUDE_FROM_ALL)

if(MSVC)
	# shut up the compiler
	if (CMAKE_C_COMPILER_ID MATCHES "Clang")
		target_compile_options(mgba-static PUBLIC -Wno-unused-command-line-argument -Wno-unused)
	endif()

	target_compile_definitions(mgba-static PUBLIC _CRT_NONSTDC_NO_WARNINGS)
endif()

target_compile_definitions(mgba-static PUBLIC MINIMAL_CORE=2 COLOR_16_BIT DISABLE_THREADING MGBA_STANDALONE)

if(GSR_SHARED)
	add_library(mgba-gsr SHARED gsr_glue.c)
	target_compile_definitions(mgba-gsr PRIVATE GSR_SHARED)
	target_link_libraries(mgba-gsr PRIVATE mgba-static)
	set_target_properties(mgba-gsr mgba-static PROPERTIES POSITION_INDEPENDENT_CODE ON)
	set_target_properties(mgba-gsr mgba-static PROPERTIES INTERPROCEDURAL_OPTIMIZATION $<$<CONFIG:Debug>:OFF,ON>)
	set(GSR_TARGET mgba-gsr)
else()
	target_sources(mgba-static PRIVATE ${CMAKE_SOURCE_DIR}/gsr_glue.c)
	set_target_properties(mgba-static PROPERTIES INTERPROCEDURAL_OPTIMIZATION OFF)
	if(MSVC)
		set_target_properties(mgba-static PROPERTIES MSVC_RUNTIME_LIBRARY MultiThreaded$<$<CONFIG:Debug>:Debug>)
	endif()
	# Fake target needs to be created here as add_custom_command is not allowed for targets made with add_subdirectory
	add_custom_target(mgba-gsr ALL)
	add_dependencies(mgba-gsr mgba-static)
	set(GSR_TARGET mgba-static)
endif()

set_target_properties(${GSR_TARGET} PROPERTIES OUTPUT_NAME "mgba")

# Copy output to our runtime folders
add_custom_command(
	TARGET mgba-gsr
	POST_BUILD
	COMMAND ${CMAKE_COMMAND}
	ARGS -E copy $<TARGET_FILE:${GSR_TARGET}> ${CMAKE_SOURCE_DIR}/../runtimes/${GSR_RID}/
)
