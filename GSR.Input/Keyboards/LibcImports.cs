using System;
using System.Runtime.InteropServices;

namespace GSR.Input.Keyboards;

internal static partial class LibcImports
{
	// errno values
	public const int EAGAIN = 11;
	public const int ENODEV = 19;

	public static readonly bool HasRoot = getuid() == 0;

	[LibraryImport("libc.so.6")]
	public static partial uint getuid();

	[LibraryImport("libc.so.6")]
	public static partial uint geteuid();

	[LibraryImport("libc.so.6", SetLastError = true)]
	public static partial int seteuid(uint euid);

	public const int O_RDONLY = 0;
	public const int O_NONBLOCK = 0x800;
	public const int O_CLOEXEC = 0x80000;

	[LibraryImport("libc.so.6", StringMarshalling = StringMarshalling.Utf8)]
	public static partial int open(string pathname, int flags);

	[LibraryImport("libc.so.6")]
	public static partial int close(int fd);

	[LibraryImport("libc.so.6")]
	public static partial int ioctl(int fd, nuint request, Span<byte> data);

	[LibraryImport("libc.so.6")]
	public static partial int ioctl(int fd, nuint request, Span<ushort> data);

	[LibraryImport("libc.so.6")]
	public static partial int ioctl(int fd, nuint request, ref uint data);

	[LibraryImport("libc.so.6", SetLastError = true)]
	public static partial nint read(int fd, ref EvDevImports.EvDevKeyboardEvent buf, nuint count);

	public const int PROT_READ = 1;
	public const int MAP_PRIVATE = 2;
	public const nint MAP_FAILED = -1;

	[LibraryImport("libc.so.6")]
	public static partial IntPtr mmap(IntPtr addr, nuint length, int prot, int flags, int fd, nint offset);

	[LibraryImport("libc.so.6")]
	public static partial int munmap(IntPtr addr, nuint length);
}
