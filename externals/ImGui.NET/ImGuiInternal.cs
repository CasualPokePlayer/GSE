using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

// ReSharper disable once CheckNamespace
namespace ImGuiNET;

/// <summary>
/// A few internal ImGui API bindings (not exposed in base ImGuiNET)
/// </summary>
public static unsafe partial class ImGuiInternal
{
	[LibraryImport("cimgui", StringMarshalling = StringMarshalling.Utf8)]
	[UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
	[return: MarshalAs(UnmanagedType.U1)]
	private static partial bool igBeginViewportSideBar(string name, ImGuiViewport* viewport, ImGuiDir dir, float size, ImGuiWindowFlags window_flags);

	public static bool BeginViewportSidebar(string name, ImGuiViewportPtr viewport, ImGuiDir dir, float size, ImGuiWindowFlags window_flags)
	{
		return igBeginViewportSideBar(name, viewport.NativePtr, dir, size, window_flags);
	}
}
