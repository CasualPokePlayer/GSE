cmake_minimum_required(VERSION 3.15...3.20 FATAL_ERROR)

# .NET 8 macOS requires at least 10.15
set(CMAKE_OSX_DEPLOYMENT_TARGET "10.15.0" CACHE STRING "")

project(cimgui-gse CXX)

option(GSE_SHARED "Build GSE lib as shared library" ON)

list(APPEND CMAKE_MODULE_PATH ${CMAKE_SOURCE_DIR}/../cmake)

include(FindRID)

if(GSE_SHARED)
	set(IMGUI_STATIC OFF CACHE BOOL "" FORCE)
	set(CMAKE_POSITION_INDEPENDENT_CODE ON)
	set(CMAKE_INTERPROCEDURAL_OPTIMIZATION $<$<CONFIG:Debug>:OFF,ON>)
else()
	set(IMGUI_STATIC ON CACHE BOOL "" FORCE)
	set(CMAKE_INTERPROCEDURAL_OPTIMIZATION OFF)
	set(CMAKE_MSVC_RUNTIME_LIBRARY MultiThreaded$<$<CONFIG:Debug>:Debug>)
endif()

set(IMGUI_FREETYPE OFF CACHE BOOL "" FORCE)
set(CIMGUI_TEST OFF CACHE BOOL "" FORCE)

add_subdirectory(cimgui EXCLUDE_FROM_ALL)

# cimgui cmakelist decides to clear the prefix always
# we don't like that, so restore the prefix
set_property(TARGET cimgui PROPERTY PREFIX)

# Copy output to our runtime folders
add_custom_target(cimgui-gse ALL)
add_dependencies(cimgui-gse cimgui)
add_custom_command(
	TARGET cimgui-gse
	POST_BUILD
	COMMAND ${CMAKE_COMMAND}
	ARGS -E copy $<TARGET_FILE:cimgui> ${CMAKE_SOURCE_DIR}/../runtimes/${GSE_RID}/
)
