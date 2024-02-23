cmake_minimum_required(VERSION 3.13...3.20 FATAL_ERROR)

set(CMAKE_POLICY_DEFAULT_CMP0069 NEW)
set(CMAKE_POLICY_DEFAULT_CMP0077 NEW)

# .NET 8 macOS requires at least 10.15
set(CMAKE_OSX_DEPLOYMENT_TARGET 10.15)

project(SDL2-GSR C CXX)

if(WIN32)
	# libusb handling does some magic which requires at least cmake 3.18
	if(CMAKE_VERSION VERSION_LESS "3.18") 
		message(FATAL_ERROR "Windows builds require at least CMake 3.18")
	endif()
endif()

option(GSR_SHARED "Build GSR lib as shared library" ON)

list(APPEND CMAKE_MODULE_PATH ${CMAKE_SOURCE_DIR}/../cmake)

include(FindRID)

if(GSR_SHARED)
	set(CMAKE_POSITION_INDEPENDENT_CODE ON)
	set(CMAKE_INTERPROCEDURAL_OPTIMIZATION $<$<CONFIG:Debug>:OFF,ON>)
else()
	set(CMAKE_INTERPROCEDURAL_OPTIMIZATION OFF)
endif()

# Disable subsystems we don't use
set(SDL_HAPTIC OFF)
set(SDL_POWER OFF)
set(SDL_SENSOR OFF)
set(SDL_LOCALE OFF)
set(SDL_MISC OFF)

set(SDL2_DISABLE_SDL2MAIN ON)
set(SDL2_DISABLE_INSTALL ON)
set(SDL2_DISABLE_UNINSTALL ON)

# Let's keep our build friendly to ALL x86-64 CPUs
# set(SDL_SSE3 OFF)

# Actually, SDL2 has a bug with clang-cl that makes SSE3 required
# Annoying, but SSE3 shouldn't really hurt compatibility
# (x86-64 was introduced in 2003, SSE3 was introduced in 2004)

# We want to statically link to the VC runtimes if we're building as a static library
# Otherwise, dynamically link
if(GSR_SHARED)
	set(SDL_FORCE_STATIC_VCRT OFF)
else()
	set(SDL_FORCE_STATIC_VCRT ON)
endif()
set(SDL_LIBC ON)

# Don't need this
set(SDL_RPATH OFF)

# Get rid of SDL audio stuff we will not be using
set(SDL_DISKAUDIO OFF)
set(SDL_DUMMYAUDIO OFF)

# Get rid of SDL video stuff we will not be using
set(SDL_DUMMYVIDEO OFF)
set(SDL_VULKAN OFF)
set(SDL_OFFSCREEN OFF)

# libusb gives us support for the official GC Adapter
set(SDL_HIDAPI_LIBUSB ON)

# Windows builds don't normally ship libusb support, we'll do some magic to avoid that
if(WIN32)
	# This is enough to trick SDL to build with libusb support, really!
	set(HAVE_LIBUSB ON)
endif()

# Mark this as a GSR build
set(SDL_VENDOR_INFO "GSR" CACHE STRING "" FORCE)

if(GSR_SHARED)
	set(SDL_SHARED ON)
	set(SDL_STATIC OFF)
	set(GSR_SDL_TARGET SDL2)
else()
	set(SDL_SHARED OFF)
	set(SDL_STATIC ON)
	set(GSR_SDL_TARGET SDL2-static)
endif()

set(SDL_TEST OFF)

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
			TARGET_DIRECTORY ${GSR_SDL_TARGET}
			PROPERTIES
			COMPILE_OPTIONS -w
		)
	endif()

	set_source_files_properties(
		${LIBUSB_SOURCES}
		TARGET_DIRECTORY ${GSR_SDL_TARGET}
		PROPERTIES
		INCLUDE_DIRECTORIES ${CMAKE_SOURCE_DIR}/libusb/msvc
	)

	target_include_directories(${GSR_SDL_TARGET} PRIVATE libusb/libusb)
	target_sources(${GSR_SDL_TARGET} PRIVATE ${LIBUSB_SOURCES})
endif()

# ensure we have SDL2.dll / SDL2.lib / libSDL2.dylib / libSDL2.so / libSDL2.a
set_target_properties(${GSR_SDL_TARGET} PROPERTIES
	NO_SONAME ON
	OUTPUT_NAME "SDL2"
)

# Copy output to our runtime folders
# This requires a bit of magic (add_custom_command is not allowed for targets made with add_subdirectory)
add_custom_target(SDL2-GSR ALL)
add_dependencies(SDL2-GSR ${GSR_SDL_TARGET})
add_custom_command(
	TARGET SDL2-GSR
	POST_BUILD
	COMMAND ${CMAKE_COMMAND}
	ARGS -E copy $<TARGET_FILE:${GSR_SDL_TARGET}> ${CMAKE_SOURCE_DIR}/../runtimes/${GSR_RID}/
)
