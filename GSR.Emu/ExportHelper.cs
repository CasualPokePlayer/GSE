using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace GSR.Emu;

internal static partial class ExportHelper
{
	public enum MemExport
	{
		GB_WRAM,
		GB_SRAM,
		GBA_IWRAM,
		GBA_EWRAM,
		GBA_SRAM,
		END,
	};

	[LibraryImport("export_helper")]
	[UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
	public static partial void export_helper_set_mem_export(MemExport which, IntPtr ptr, nuint len);
}
