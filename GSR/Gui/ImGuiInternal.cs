// Copyright (c) 2024 CasualPokePlayer
// SPDX-License-Identifier: MPL-2.0

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using ImGuiNET;

namespace GSR.Gui;

/// <summary>
/// A few internal ImGui API bindings (not exposed in base ImGui.NET)
/// </summary>
public static unsafe partial class ImGuiInternal
{
	[LibraryImport("cimgui", StringMarshalling = StringMarshalling.Utf8)]
	[UnmanagedCallConv(CallConvs = [ typeof(CallConvCdecl) ])]
	[return: MarshalAs(UnmanagedType.U1)]
	private static partial bool igBeginViewportSideBar(string name, ImGuiViewport* viewport, ImGuiDir dir, float size, ImGuiWindowFlags window_flags);

	public static bool BeginViewportSidebar(string name, ImGuiViewportPtr viewport, ImGuiDir dir, float size, ImGuiWindowFlags window_flags)
	{
		return igBeginViewportSideBar(name, viewport.NativePtr, dir, size, window_flags);
	}
}
