cmake_minimum_required(VERSION 3.1 FATAL_ERROR)

if(POLICY CMP0069)
	cmake_policy(SET CMP0069 NEW)
	set(CMAKE_POLICY_DEFAULT_CMP0069 NEW)
endif()

if(POLICY CMP0092)
	cmake_policy(SET CMP0092 NEW)
	set(CMAKE_POLICY_DEFAULT_CMP0092 NEW)
endif()

project(libmgba-gsr C)

set(CMAKE_C_STANDARD 11)
set(CMAKE_C_STANDARD_REQUIRED ON)
set(CMAKE_C_EXTENSIONS OFF)
set(CMAKE_C_VISIBILITY_PRESET hidden)

set(LIBMGBA_ONLY ON)
set(M_CORE_GB OFF)
set(MINIMAL_CORE ON)
set(BUILD_STATIC ON)

add_subdirectory(mgba EXCLUDE_FROM_ALL)

if(MSVC)
	# shut up the compiler
	if (CMAKE_C_COMPILER_ID MATCHES "Clang")
		target_compile_options(mgba PUBLIC -Wno-unused-command-line-argument -Wno-unused)
	endif()

	target_compile_definitions(mgba PUBLIC _CRT_NONSTDC_NO_WARNINGS)
endif()

target_compile_definitions(mgba PUBLIC MINIMAL_CORE=2 COLOR_16_BIT DISABLE_THREADING MGBA_STANDALONE)

option(GSR_SHARED "Build GSR lib as shared library" ON)

if(GSR_SHARED)
	add_library(mgba-gsr SHARED gsr_glue.c)
	target_compile_definitions(mgba-gsr PRIVATE GSR_SHARED)
	set_target_properties(mgba-gsr mgba PROPERTIES POSITION_INDEPENDENT_CODE $<$<CONFIG:Debug>:0,1>)
	set_target_properties(mgba-gsr mgba PROPERTIES INTERPROCEDURAL_OPTIMIZATION ON)
else()
	add_library(mgba-gsr STATIC gsr_glue.c)
	if(MSVC)
		set_target_properties(mgba-gsr mgba PROPERTIES MSVC_RUNTIME_LIBRARY MultiThreaded$<$<CONFIG:Debug>:Debug>)
	endif()
endif()

target_link_libraries(mgba-gsr PRIVATE mgba)

add_custom_command(
	TARGET mgba-gsr
	POST_BUILD
	COMMAND ${CMAKE_COMMAND}
	ARGS -E copy $<TARGET_FILE:mgba-gsr> ${CMAKE_SOURCE_DIR}/../runtimes
)
