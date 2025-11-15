cmake_minimum_required(VERSION 3.16...3.20 FATAL_ERROR)

set(CMAKE_POLICY_DEFAULT_CMP0069 NEW)
set(CMAKE_POLICY_DEFAULT_CMP0077 NEW)
set(CMAKE_POLICY_DEFAULT_CMP0091 NEW)

# .NET 8 macOS requires at least 10.15
set(CMAKE_OSX_DEPLOYMENT_TARGET "10.15" CACHE STRING "")

project(SDL3-GSE C CXX)

if(WIN32)
	# libusb handling does some magic which requires at least cmake 3.18
	if(CMAKE_VERSION VERSION_LESS "3.18") 
		message(FATAL_ERROR "Windows builds require at least CMake 3.18")
	endif()
endif()

option(GSE_SHARED "Build GSE lib as shared library" ON)

list(APPEND CMAKE_MODULE_PATH ${CMAKE_SOURCE_DIR}/../cmake)

include(FindRID)

if(GSE_SHARED)
	set(CMAKE_POSITION_INDEPENDENT_CODE ON)
	set(CMAKE_INTERPROCEDURAL_OPTIMIZATION $<$<CONFIG:Debug>:OFF,ON>)
else()
	set(CMAKE_INTERPROCEDURAL_OPTIMIZATION OFF)
endif()

# Disable subsystems we don't use
set(SDL_CAMERA OFF)
set(SDL_HAPTIC OFF)
set(SDL_POWER OFF)
set(SDL_SENSOR OFF)
set(SDL_DIALOG OFF)
set(SDL_TRAY OFF)

# Workaround SDL bug which makes compilation fail if SDL_Haptic is off
if(ANDROID)
	set(SDL_HAPTIC ON)
endif()

set(SDL_INSTALL OFF)
set(SDL_UNINSTALL OFF)

# Let's keep our build friendly to ALL x86-64 CPUs
set(SDL_AVX OFF)
set(SDL_AVX2 OFF)
set(SDL_AVX512F OFF)
set(SDL_SSE3 OFF)
set(SDL_SSE4_1 OFF)
set(SDL_SSE4_2 OFF)

# We want to statically link to the VC runtimes if we're building as a static library
# Otherwise, dynamically link
if(GSE_SHARED)
	set(CMAKE_MSVC_RUNTIME_LIBRARY "MultiThreaded$<$<CONFIG:Debug>:Debug>DLL")
else()
	set(CMAKE_MSVC_RUNTIME_LIBRARY "MultiThreaded$<$<CONFIG:Debug>:Debug>")
endif()
set(SDL_LIBC ON)

# Don't need this
set(SDL_RPATH OFF)

# Not sure how useful this is, but it appears to be buggy, so keep it off for now
set(SDL_DBUS OFF)

# Get rid of SDL audio stuff we will not be using
set(SDL_DISKAUDIO OFF)
set(SDL_DUMMYAUDIO OFF)

# Get rid of SDL video stuff we will not be using
set(SDL_DUMMYVIDEO OFF)
set(SDL_OFFSCREEN OFF)

# libusb gives us support for the official GC Adapter
set(SDL_HIDAPI_LIBUSB ON)

# Windows doesn't come with libusb, so we'll be statically linking it
# The way we do is means we'll need to do a hack with SDL
if(WIN32)
	set(SDL_HIDAPI_LIBUSB OFF)
	set(HAVE_LIBUSB ON)
endif()

# Mark this as a GSE build
set(SDL_VENDOR_INFO "GSE" CACHE STRING "" FORCE)

if(GSE_SHARED)
	set(SDL_SHARED ON)
	set(SDL_STATIC OFF)
	set(GSE_SDL_TARGET SDL3-shared)
else()
	set(SDL_SHARED OFF)
	set(SDL_STATIC ON)
	set(GSE_SDL_TARGET SDL3-static)
endif()

set(SDL_TEST_LIBRARY OFF)
set(SDL_TESTS OFF)
set(SDL_EXAMPLES OFF)

add_subdirectory(SDL)

if(WIN32)
	# We need to build libusb ourselves and statically link it
	set(LIBUSB_SOURCES
		libusb/libusb/core.c
		libusb/libusb/descriptor.c
		libusb/libusb/hotplug.c
		libusb/libusb/io.c
		libusb/libusb/strerror.c
		libusb/libusb/sync.c
		libusb/libusb/os/threads_windows.c
		libusb/libusb/os/windows_common.c
		libusb/libusb/os/windows_usbdk.c
		libusb/libusb/os/windows_winusb.c
		libusb/libusb/os/events_windows.c
	)

	# Silence warnings with clang-cl
	if(CMAKE_C_COMPILER_ID MATCHES "Clang")
		set_source_files_properties(
			${LIBUSB_SOURCES}
			TARGET_DIRECTORY ${GSE_SDL_TARGET}
			PROPERTIES
			COMPILE_OPTIONS -w
		)
	endif()

	set_source_files_properties(
		${LIBUSB_SOURCES}
		TARGET_DIRECTORY ${GSE_SDL_TARGET}
		PROPERTIES
		INCLUDE_DIRECTORIES ${CMAKE_SOURCE_DIR}/libusb/msvc
	)

	target_include_directories(${GSE_SDL_TARGET} PRIVATE libusb/libusb)
	target_sources(${GSE_SDL_TARGET} PRIVATE ${LIBUSB_SOURCES})
endif()

# ensure we have SDL3.dll / SDL3.lib / libSDL3.dylib / libSDL3.so / libSDL3.a
set_target_properties(${GSE_SDL_TARGET} PROPERTIES
	NO_SONAME ON
	OUTPUT_NAME "SDL3"
)

# Copy output to our runtime folders
# This requires a bit of magic (add_custom_command is not allowed for targets made with add_subdirectory)
add_custom_target(SDL3-GSE ALL)
add_dependencies(SDL3-GSE ${GSE_SDL_TARGET})
add_custom_command(
	TARGET SDL3-GSE
	POST_BUILD
	COMMAND ${CMAKE_COMMAND}
	ARGS -E copy $<TARGET_FILE:${GSE_SDL_TARGET}> ${CMAKE_SOURCE_DIR}/../runtimes/${GSE_RID}/
)
