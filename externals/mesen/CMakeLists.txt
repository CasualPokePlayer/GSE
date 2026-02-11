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
set(CMAKE_OSX_DEPLOYMENT_TARGET "10.15" CACHE STRING "")

project(mesen-gse C CXX)

if(MSVC)
	if(POLICY CMP0117)
	else()
		message(FATAL_ERROR "MSVC builds require at least CMake 3.20")
	endif()
endif()

option(GSE_SHARED "Build GSE lib as shared library" ON)

list(APPEND CMAKE_MODULE_PATH ${CMAKE_SOURCE_DIR}/../cmake)

include(FindRID)

set(CMAKE_C_STANDARD 11)
set(CMAKE_C_STANDARD_REQUIRED ON)
set(CMAKE_C_EXTENSIONS OFF)
set(CMAKE_C_VISIBILITY_PRESET hidden)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(CMAKE_CXX_EXTENSIONS OFF)
set(CMAKE_CXX_VISIBILITY_PRESET hidden)
set(CMAKE_VISIBILITY_INLINES_HIDDEN ON)

set(MESEN_SOURCE_DIR ${CMAKE_SOURCE_DIR}/Mesen2)
set(MESEN_SOURCES
	${MESEN_SOURCE_DIR}/Core/GBA/GbaConsole.cpp
	${MESEN_SOURCE_DIR}/Core/GBA/GbaControlManager.cpp
	${MESEN_SOURCE_DIR}/Core/GBA/GbaCpu.Arm.cpp
	${MESEN_SOURCE_DIR}/Core/GBA/GbaCpu.Thumb.cpp
	${MESEN_SOURCE_DIR}/Core/GBA/GbaCpu.cpp
	${MESEN_SOURCE_DIR}/Core/GBA/GbaDefaultVideoFilter.cpp
	${MESEN_SOURCE_DIR}/Core/GBA/GbaDmaController.cpp
	${MESEN_SOURCE_DIR}/Core/GBA/GbaMemoryManager.cpp
	${MESEN_SOURCE_DIR}/Core/GBA/GbaPpu.cpp
	${MESEN_SOURCE_DIR}/Core/GBA/GbaTimer.cpp
	${MESEN_SOURCE_DIR}/Core/GBA/APU/GbaApu.cpp
	${MESEN_SOURCE_DIR}/Core/GBA/APU/GbaNoiseChannel.cpp
	${MESEN_SOURCE_DIR}/Core/GBA/APU/GbaSquareChannel.cpp
	${MESEN_SOURCE_DIR}/Core/GBA/APU/GbaWaveChannel.cpp
	${MESEN_SOURCE_DIR}/Core/GBA/Cart/GbaCart.cpp
	${MESEN_SOURCE_DIR}/Core/GBA/Cart/GbaGpio.cpp
	${MESEN_SOURCE_DIR}/Core/GBA/Cart/GbaRtc.cpp
	${MESEN_SOURCE_DIR}/Core/GBA/Debugger/DummyGbaCpu.cpp
	${MESEN_SOURCE_DIR}/Core/Netplay/GameClient.cpp
	${MESEN_SOURCE_DIR}/Core/Netplay/GameClientConnection.cpp
	${MESEN_SOURCE_DIR}/Core/Netplay/GameConnection.cpp
	${MESEN_SOURCE_DIR}/Core/Netplay/GameServer.cpp
	${MESEN_SOURCE_DIR}/Core/Netplay/GameServerConnection.cpp
	${MESEN_SOURCE_DIR}/Core/Shared/BaseControlDevice.cpp
	${MESEN_SOURCE_DIR}/Core/Shared/BaseControlManager.cpp
	${MESEN_SOURCE_DIR}/Core/Shared/BatteryManager.cpp
	${MESEN_SOURCE_DIR}/Core/Shared/CheatManager.cpp
	${MESEN_SOURCE_DIR}/Core/Shared/DebuggerRequest.cpp
	${MESEN_SOURCE_DIR}/Core/Shared/EmuSettings.cpp
	${MESEN_SOURCE_DIR}/Core/Shared/Emulator.cpp
	${MESEN_SOURCE_DIR}/Core/Shared/EmulatorLock.cpp
	${MESEN_SOURCE_DIR}/Core/Shared/HistoryViewer.cpp
	${MESEN_SOURCE_DIR}/Core/Shared/InputHud.cpp
	${MESEN_SOURCE_DIR}/Core/Shared/KeyManager.cpp
	${MESEN_SOURCE_DIR}/Core/Shared/MessageManager.cpp
	${MESEN_SOURCE_DIR}/Core/Shared/NotificationManager.cpp
	${MESEN_SOURCE_DIR}/Core/Shared/RewindData.cpp
	${MESEN_SOURCE_DIR}/Core/Shared/RewindManager.cpp
	${MESEN_SOURCE_DIR}/Core/Shared/SaveStateManager.cpp
	${MESEN_SOURCE_DIR}/Core/Shared/ShortcutKeyHandler.cpp
	${MESEN_SOURCE_DIR}/Core/Shared/Audio/AudioPlayerHud.cpp
	${MESEN_SOURCE_DIR}/Core/Shared/Audio/BaseSoundManager.cpp
	${MESEN_SOURCE_DIR}/Core/Shared/Audio/SoundMixer.cpp
	${MESEN_SOURCE_DIR}/Core/Shared/Audio/SoundResampler.cpp
	${MESEN_SOURCE_DIR}/Core/Shared/Audio/WaveRecorder.cpp
	${MESEN_SOURCE_DIR}/Core/Shared/Movies/MesenMovie.cpp
	${MESEN_SOURCE_DIR}/Core/Shared/Movies/MovieManager.cpp
	${MESEN_SOURCE_DIR}/Core/Shared/Movies/MovieRecorder.cpp
	${MESEN_SOURCE_DIR}/Core/Shared/Video/BaseVideoFilter.cpp
	${MESEN_SOURCE_DIR}/Core/Shared/Video/DebugHud.cpp
	${MESEN_SOURCE_DIR}/Core/Shared/Video/DebugStats.cpp
	${MESEN_SOURCE_DIR}/Core/Shared/Video/DrawStringCommand.cpp
	${MESEN_SOURCE_DIR}/Core/Shared/Video/RotateFilter.cpp
	${MESEN_SOURCE_DIR}/Core/Shared/Video/ScaleFilter.cpp
	${MESEN_SOURCE_DIR}/Core/Shared/Video/SystemHud.cpp
	${MESEN_SOURCE_DIR}/Core/Shared/Video/VideoDecoder.cpp
	${MESEN_SOURCE_DIR}/Core/Shared/Video/VideoRenderer.cpp
	${MESEN_SOURCE_DIR}/Utilities/spng.c
	${MESEN_SOURCE_DIR}/Utilities/ArchiveReader.cpp
	${MESEN_SOURCE_DIR}/Utilities/AutoResetEvent.cpp
	${MESEN_SOURCE_DIR}/Utilities/CRC32.cpp
	${MESEN_SOURCE_DIR}/Utilities/FolderUtilities.cpp
	${MESEN_SOURCE_DIR}/Utilities/HexUtilities.cpp
	${MESEN_SOURCE_DIR}/Utilities/PNGHelper.cpp
	${MESEN_SOURCE_DIR}/Utilities/PlatformUtilities.cpp
	${MESEN_SOURCE_DIR}/Utilities/Serializer.cpp
	${MESEN_SOURCE_DIR}/Utilities/SimpleLock.cpp
	${MESEN_SOURCE_DIR}/Utilities/Socket.cpp
	${MESEN_SOURCE_DIR}/Utilities/Timer.cpp
	${MESEN_SOURCE_DIR}/Utilities/UPnPPortMapper.cpp
	${MESEN_SOURCE_DIR}/Utilities/UTF8Util.cpp
	${MESEN_SOURCE_DIR}/Utilities/VirtualFile.cpp
	${MESEN_SOURCE_DIR}/Utilities/ZipReader.cpp
	${MESEN_SOURCE_DIR}/Utilities/ZipWriter.cpp
	${MESEN_SOURCE_DIR}/Utilities/md5.cpp
	${MESEN_SOURCE_DIR}/Utilities/miniz.cpp
	${MESEN_SOURCE_DIR}/Utilities/sha1.cpp
	${MESEN_SOURCE_DIR}/Utilities/Audio/CrossFeedFilter.cpp
	${MESEN_SOURCE_DIR}/Utilities/Audio/Equalizer.cpp
	${MESEN_SOURCE_DIR}/Utilities/Audio/HermiteResampler.cpp
	${MESEN_SOURCE_DIR}/Utilities/Audio/ReverbFilter.cpp
	${MESEN_SOURCE_DIR}/Utilities/HQX/hq2x.cpp
	${MESEN_SOURCE_DIR}/Utilities/HQX/hq3x.cpp
	${MESEN_SOURCE_DIR}/Utilities/HQX/hq4x.cpp
	${MESEN_SOURCE_DIR}/Utilities/HQX/init.cpp
	${MESEN_SOURCE_DIR}/Utilities/KreedSaiEagle/2xSai.cpp
	${MESEN_SOURCE_DIR}/Utilities/KreedSaiEagle/Super2xSai.cpp
	${MESEN_SOURCE_DIR}/Utilities/KreedSaiEagle/SuperEagle.cpp
	${MESEN_SOURCE_DIR}/Utilities/NTSC/snes_ntsc.cpp
	${MESEN_SOURCE_DIR}/Utilities/Patches/BpsPatcher.cpp
	${MESEN_SOURCE_DIR}/Utilities/Patches/IpsPatcher.cpp
	${MESEN_SOURCE_DIR}/Utilities/Patches/UpsPatcher.cpp
	${MESEN_SOURCE_DIR}/Utilities/Scale2x/scale2x.cpp
	${MESEN_SOURCE_DIR}/Utilities/Scale2x/scale3x.cpp
	${MESEN_SOURCE_DIR}/Utilities/Scale2x/scalebit.cpp
	${MESEN_SOURCE_DIR}/Utilities/Video/AviRecorder.cpp
	${MESEN_SOURCE_DIR}/Utilities/Video/AviWriter.cpp
	${MESEN_SOURCE_DIR}/Utilities/Video/CamstudioCodec.cpp
	${MESEN_SOURCE_DIR}/Utilities/Video/GifRecorder.cpp
	${MESEN_SOURCE_DIR}/Utilities/Video/ZmbvCodec.cpp
	${MESEN_SOURCE_DIR}/Utilities/xBRZ/xbrz.cpp
)

if(GSE_SHARED)
	add_library(mesen SHARED ${MESEN_SOURCES} ${CMAKE_SOURCE_DIR}/gse_glue.cpp)
	target_compile_definitions(mesen PRIVATE GSE_SHARED)
	set_target_properties(mesen PROPERTIES POSITION_INDEPENDENT_CODE ON)
	set_target_properties(mesen PROPERTIES INTERPROCEDURAL_OPTIMIZATION $<$<CONFIG:Debug>:OFF,ON>)
	target_compile_definitions(mesen PRIVATE $<$<CONFIG:Debug>:"",HAVE_LTO>)
else()
	add_library(mesen STATIC ${MESEN_SOURCES} ${CMAKE_SOURCE_DIR}/gse_glue.cpp)
	set_target_properties(mesen PROPERTIES INTERPROCEDURAL_OPTIMIZATION OFF)
	set_target_properties(mesen PROPERTIES MSVC_RUNTIME_LIBRARY MultiThreaded$<$<CONFIG:Debug>:Debug>)
endif()

target_include_directories(mesen PRIVATE
	${MESEN_SOURCE_DIR}
	${MESEN_SOURCE_DIR}/Core
	${MESEN_SOURCE_DIR}/Utilities
)

# Set compiler specific options
if(MSVC)
	# shut up the compiler
	target_compile_definitions(mesen PRIVATE _CRT_SECURE_NO_WARNINGS)
	# Source files are UTF-8
	target_compile_options(mesen PRIVATE /utf-8)
	# Enforce standard conformance
	target_compile_options(mesen PRIVATE /permissive- /volatile:iso /fp:precise)
	# Exceptions must be compiled in
	target_compile_options(mesen PRIVATE /EHsc-)
	# Give Unicode WinAPI functions
	target_compile_definitions(mesen PRIVATE UNICODE _UNICODE)
	# Shared lib needs to link immediately
	if(GSE_SHARED)
		target_link_libraries(mesen PRIVATE winmm.lib)
	endif()
else()
	if(GSE_SHARED)
		target_link_options(mesen PRIVATE $<$<CONFIG:RELEASE>:-s>)
	endif()
	target_compile_options(mesen PRIVATE $<$<CONFIG:DEBUG>:-ggdb>)
endif()

# Copy output to our runtime folders
add_custom_command(
	TARGET mesen
	POST_BUILD
	COMMAND ${CMAKE_COMMAND}
	ARGS -E copy $<TARGET_FILE:mesen> ${CMAKE_SOURCE_DIR}/../runtimes/${GSE_RID}/
)
