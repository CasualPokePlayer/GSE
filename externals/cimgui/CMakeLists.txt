cmake_minimum_required(VERSION 3.15 FATAL_ERROR)

project(cimgui-gsr CXX)

option(GSR_SHARED "Build GSR lib as shared library" ON)

list(APPEND CMAKE_MODULE_PATH ${CMAKE_SOURCE_DIR}/../cmake)

include(FindRID)

if(GSR_SHARED)
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

# Copy output to our runtime folders
add_custom_target(cimgui-gsr ALL)
add_dependencies(cimgui-gsr cimgui)
add_custom_command(
	TARGET cimgui-gsr
	POST_BUILD
	COMMAND ${CMAKE_COMMAND}
	ARGS -E copy $<TARGET_FILE:cimgui> ${CMAKE_SOURCE_DIR}/../runtimes/${GSR_RID}/
)
