# GSR

GSR is a Game Boy, Game Boy Color, and Game Boy Advance emulator written in C#. It is intended first and foremost to speedrunners, with various features placed in making it optimal for speedrunning use.

This emulator is intended as a successor to the [Gambatte-Speedrun](https://github.com/pokemon-speedrunning/gambatte-speedrun) emulator. As such, GSR uses the [Gambatte](https://github.com/pokemon-speedrunning/gambatte-core) emulation core for Game Boy and Game Boy Color emulation. New in GSR is Game Boy Advance emulation (i.e. with Game Boy Advance games), which currently uses the [mGBA](https://github.com/mgba-emu/mgba) emulation core.

---
## Features

***Emulation core***
* Original Game Boy games playable in Game Boy Color mode (emulated properly)
* Game Boy / Game Boy Color games playable in Game Boy Advance mode (i.e. as the Game Boy Color in the Game Boy Advance)
* Game Boy Player emulation (i.e. Game Boy Advance mode with hard reset fadeout timing)
* Super Game Boy 2 emulation (with reset timing properly emulated)
* Battleground tested Game Boy / Game Boy Color emulation, capable of replicating Pokémon RNG manipulations and creating console verifiable TASes[^1]
* Highly accurate Game Boy Advance emulation (i.e. with Game Boy Advance games), although not at the same level as Game Boy / Game Boy Color emulation[^2]

***Speedrunning***
* Bootrom/BIOS files are required for usage
* Status bar present on the bottom of the screen, showing the current ROM CRC32 and current emulator cycle count, along with status messages
	* The status bar can be disabled, in which case, the ROM CRC32 will be shown on hard reset on top of the game view. Status messages will also be presented in a similar manner
	* This status bar can be easily captured by window capture software (e.g. OBS), unlike the window title bar
* Different game inputs cannot be bound to the same host input (e.g. you cannot bind Start and Select to Enter)
* Left+Right and Up+Down inputs are prohibited
* "Dead battery" RTC enabled by default for Game Boy Advance games
* Always runs at the correct framerate (i.e. ~59.7275 FPS)
* "Clock sync" used for host timing purposes, ensuring extremely consistent frame pacing (i.e. minimal "judder") and minimal input lag (both important for 1 frame tricks and such)
* Features not useful for speedrunners in runs or practice are not present (e.g. no cheat code support)

***Quality of Life***
* 100 savestate slots (operating on a "set" system with 10 slots available per slot, i.e. 10 sets with 10 slots each)
* Drag-n-drop support, for both ROM files and savestates
* Support for 7z/rar/tar/gz/zip compressed ROMs
* Color correction config option, using formulas from [SameBoy](https://github.com/LIJI32/SameBoy)
* SGB border (for Super Game Boy 2 emulation) can be hidden with a config option

***GUI***
* Nearest Neighbor, Billinear, and Sharp Billinear filtering options
* DPI aware GUI scaling[^3]
* Dark and light mode options
* Dark mode title bar on Windows 10+ when GUI is in dark mode

***Input***
* Background input option, available for both keyboard and joystick inputs
	* Background input can be set to only apply to joystick inputs
* Input bindings can have a "modifier" key set.
	* There is no limitation on what key can be a modifier (outside of above game input restrictions). Cross keyboard+joystick modifiers can be done.
* Input bindings can have up to 4 bindings per input (matching up to how the Game Boy Player accepts 4 GameCube controllers at once)
* Hotkeys are all configurable, there are no hardcoded hotkeys
* Keyboard input text is localized according to keyboard layout. However, the config will refer to keyboard key positions, ensuring the config is layout agnostic

***Audio***
* The host audio device can be selected as a config option
* Device disconnection will result in automatic reconnection to the default audio device
* Volume can be configured within the emulator
* Volume uses logarithmic scaling, not linear scaling (more in line with how humans perceive loudness)

[^1]: TASes are created with the [BizHawk](https://github.com/TASEmulators/BizHawk) project, which shares the same Game Boy / Game Boy Color emulation core. GSR itself does not have TAS creation capabilities.
[^2]: As of now, Game Boy Advance emulation should not be assumed to be completely accurate to console for speedrunning timing purposes. Individual speedrunning communities should decide how to treat emulation for their boards. This situation is subject to change.
[^3]: Linux users might not get an automatically scaled GUI, due to X11 not providing reliable DPI info. GUI scaling can be overriden by the GSR_SCALE environment variable.

---
## User Requirements

GSR currently requires one of the following operating systems:
* Windows 7 SP1+
* macOS 10.15+
* Linux (glibc 2.31+ / libstdc++ 3.4.28+)
* Android 5.0+ (Lollipop)

Both x64 and ARM64 machines are supported. On macOS, a "universal" binary is distributed, which works on both x64 and ARM64 machines.

x86 and ARM32 are not currently supported. This is a limitation on NativeAOT compilation, which this project uses. In the future, this limitation may go away and thus supporting these architectures may be possible in the future.

On Linux, both X11 and Wayland are supported (although X11 will be preferred if available). Linux file dialogs rely on either libdbus with the [Portal](https://flatpak.github.io/xdg-desktop-portal/docs/doc-org.freedesktop.portal.FileChooser.html) D-Bus API, or GTK3/GTK2. If neither are present, file dialogs will not work, although the application will still launch fine. In practice, every Linux distro should provide at least one of these options.

---
## Building from source

> Below is a relatively quick overview of building GSR, for the whole sh'bang, you'll want to read the [contributing guidelines](https://github.com/CasualPokePlayer/GSR/blob/master/CONTRIBUTING.md).

[git](https://git-scm.com/download) should be used to clone the repository. Many submodules are present (with some having submodules within themselves), so ensure that they are all checked out (e.g. `git submodule update --init --recursive`)

Before the C# side can be built, various C/C++ libraries must be built. CMake is used for building all C/C++ libraries. Helper scripts are provided to build all C/C++ libraries (build_all_dev.bat for Windows, build_all_dev.sh for macOS/Linux).

The [dotnet8 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) is required to build the C# side.

Windows developers can use [VS Community 2022](https://visualstudio.microsoft.com/vs/community).
Windows, macOS, and Linux developers can all use Rider, VS Code, or `dotnet` directly from the command-line.
