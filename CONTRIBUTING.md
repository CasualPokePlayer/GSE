## Setting up development environment

> None of this guide currently covers building for Android

Before starting, fork the repository on GitHub (use the "Fork" button on the top right of the page).

GSE's source code must be obtained with [git](https://git-scm.com/download). This sometimes is included already within your IDE (e.g. XCode). It's recommended to use the git CLI for git operations (examples of CLI usage will be in this document), although a git GUI should work fine (examples not provided).

Once git is obtained, the repository can be cloned using:
```sh
git clone https://github.com/yourusername/GSE
```
Where yourusername is replaced with your GitHub username. This will create a GSE folder containing a clone of the repository, which later git operations should be done in.

GSE contains various submodules, some of which have submodules within themselves, which must all be checked out. This can be done using:
```sh
git submodule update --init --recursive
```

Building GSE requires installing C/C++ and C# build tools.
- Windows
	- All neccessary build tools can be installed with [VS Community 2022](https://visualstudio.microsoft.com/vs/community) (or any other Visual Studio edition) with the "Desktop development with C++" and ".NET desktop development" workloads.
	- It should be possible to manually install the [Windows SDK](https://developer.microsoft.com/en-us/windows/downloads/windows-sdk/), [clang](https://releases.llvm.org/download.html), [CMake](https://cmake.org/download/), [ninja](https://github.com/ninja-build/ninja/releases), and the [dotnet9 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/9.0) in order to build GSE, but this is not recommended.
- macOS
	- An XCode install covers C/C++ compilers and will install the macOS SDK.
	- CMake and ninja should be installed (`brew install cmake ninja`).
	- The [dotnet8 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) (not dotnet9!) should be installed.
	- The macOS dotnet workload must also be installed (`dotnet workload install macos`).
- Linux
	- Install gcc/g++ or clang/clang++ with your package manager (syntax varies, should be easy to look up).
	- ninja should be installed (should be provided by package manager, typically as `ninja-build`).
	- The [dotnet9 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/9.0) is typically provided in package managers, and could be manually installed.

Helper scripts are included in externals/ which will build all C/C++ libraries (build_all_dev.bat for Windows, build_all_dev.sh for macOS/Linux). If you want to do something custom, standard CMake build commands should work.

Note that Windows typically will need to use the "x64 Native Tools Command Prompt for VS2022" in order to build the C/C++ libraries (or the ARM64 variant, if your host PC is an ARM64 PC).

Once all C/C++ libraries are compiled, you can fire up your favorite IDE and start doing C# development.
- Windows
	- [VS Community 2022](https://visualstudio.microsoft.com/vs/community) can be used as a C# IDE (you likely already installed this anyways).
- macOS
	- [VS for Mac](https://visualstudio.microsoft.com/vs/mac/) likely works (although this has not been tested)
- All platforms
	- [Rider](https://www.jetbrains.com/rider/download/) or [VS Code](https://code.visualstudio.com/download) can be used as a C# IDE.
	- Any other kind of IDE (e.g. Sublime Text) could be used alongside direct `dotnet` CLI usage.

## Code style

> In general, standard [dotnet coding conventions](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions) are used, with some deviations (tabs instead of spaces, prefer var always). Correct style is enforced with the .editorconfig file.

- Use tabs rather than spaces for indentation.
- Use "Allman" style braces.
- For `using` namespace statements, place `System.*` first, then third-party namespaces (e.g. ImGui.NET), then finally internal (`GSE.*`) namespaces.
	- For SDL2, prefer `using static SDL2.SDL`.
- Prefer var over explicit types whenever possible.

## Copyright/Licensing

All original GSE code shall be licensed as MPL-2.0 and copyrighted to CasualPokePlayer. 

Any code contributions you make thus must either:
- Be derived from an open-source, publicly-licensed codebase, compatible with the MPL-2.0, with proper attributions. In general, such code shall be dual licensed as MPL-2.0 and its original license, except for (L)GPL code, which will retain its original license. Copyright statements should be modified to include the original author.
- Be authored by you, and you are willing to transfer all transferable rights to CasualPokePlayer, including but not limited to, re-licensing the code, modifying the code, and distributing it in source or binary forms. This includes a requirement that you assign copyright to CasualPokePlayer. Due to this, do not add your name to any copyright statements. Note that this transfer is on a nonexclusive basis only; it does not take away your own copyright (and thus you are permitted to use your contributions in other projects in any way you see fit).

> Any "contributions" from LLM AIs, such as GitHub Copilot, are forbidden and will not be accepted under any circumstances.
