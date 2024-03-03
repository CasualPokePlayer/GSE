using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace GSR;

internal static partial class RootHelper
{
	[LibraryImport("libc.so.6")]
	public static partial uint getegid();

	[LibraryImport("libc.so.6", SetLastError = true)]
	public static partial int setegid(uint euid);

	[LibraryImport("libc.so.6")]
	public static partial uint geteuid();

	[LibraryImport("libc.so.6", SetLastError = true)]
	public static partial int seteuid(uint euid);

	public static void DropRoot()
	{
		var egid = getegid();
		var euid = geteuid();
		if (egid == 0 || euid == 0)
		{
			Console.WriteLine("Detected root permissions, attempting to drop to normal user");
			var sudoGid = Environment.GetEnvironmentVariable("SUDO_GID");
			var sudoUid = Environment.GetEnvironmentVariable("SUDO_UID");
			if (sudoGid == null || sudoUid == null)
			{
				Console.WriteLine("sudo was not used, cannot drop to normal use, will continue as root");
				return;
			}

			if (!uint.TryParse(sudoGid, out var gid) || !uint.TryParse(sudoUid, out var uid))
			{
				Console.WriteLine("sudo was used, but the normal user could not be determined, will continue as root");
				return;
			}

			if (setegid(gid) == -1)
			{
				throw new Win32Exception("Failed to set effective gid");
			}

			if (seteuid(uid) == -1)
			{
				throw new Win32Exception("Failed to set effective uid");
			}

			Console.WriteLine("Root permissions successfully dropped");
		}
	}
}
