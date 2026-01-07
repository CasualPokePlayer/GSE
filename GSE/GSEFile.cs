// Copyright (c) 2024 CasualPokePlayer
// SPDX-License-Identifier: MPL-2.0

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;

using SharpCompress.Archives;
using SharpCompress.Factories;

#if GSE_ANDROID
using GSE.Android;
#endif

namespace GSE;

/// <summary>
/// Reads and possibly decompressed a file into a buffer
/// Stores needed details about said file
/// </summary>
internal sealed class GSEFile
{
	public static readonly ImmutableArray<string> SupportedCompressionExtensions;

	static GSEFile()
	{
		var validExts = new List<string>();
		foreach (var factory in Factory.Factories.OfType<IArchiveFactory>())
		{
			validExts.AddRange(factory.GetSupportedExtensions().Select(ext => '.' + ext));
		}

		SupportedCompressionExtensions = [..validExts];
	}

	private readonly byte[] Buffer;

	public readonly string Directory;
	public readonly string UnderlyingFileName;
	public readonly string UnderlyingExtension;

	public ReadOnlyMemory<byte> UnderlyingFile => Buffer;

	public GSEFile(string path, IEnumerable<string> validExtensions)
	{
#if GSE_ANDROID
		var contentUri = path[..path.IndexOf('|')];
		path = path[(path.IndexOf('|') + 1)..];
#endif
		Directory = Path.GetDirectoryName(path);

		var validExts = validExtensions as string[] ?? [..validExtensions];
		var ext = Path.GetExtension(path);
		if (Array.Exists(validExts, validExt => validExt.Equals(ext, StringComparison.OrdinalIgnoreCase)))
		{
			UnderlyingFileName = Path.GetFileNameWithoutExtension(path);
			UnderlyingExtension = ext;
#if GSE_ANDROID
			Buffer = AndroidFile.OpenBufferedStream(contentUri).ToArray();
#else
			Buffer = File.ReadAllBytes(path);
#endif
			return;
		}

#if GSE_ANDROID
		using var fs = AndroidFile.OpenBufferedStream(contentUri);
#else
		using var fs = File.OpenRead(path);
#endif
		if (ArchiveFactory.IsArchive(fs, out _))
		{
			using var archive = ArchiveFactory.Open(fs);
			foreach (var entry in archive.Entries.Where(e => e is { IsDirectory: false, Key: not null }))
			{
				ext = Path.GetExtension(entry.Key);
				if (Array.Exists(validExts, validExt => validExt.Equals(ext, StringComparison.OrdinalIgnoreCase)))
				{
					UnderlyingFileName = Path.GetFileNameWithoutExtension(entry.Key);
					UnderlyingExtension = ext;
					using var ms = new MemoryStream((int)entry.Size);
					using var es = entry.OpenEntryStream();
					es.CopyTo(ms);
					Buffer = ms.ToArray();
				}
			}
		}

		if (Buffer == null)
		{
			throw new($"Could not find file under extensions {string.Join('/', validExts)}");
		}
	}

	public static string MakeFriendlyPath(string path)
	{
#if GSE_ANDROID
		return path?[(path.IndexOf('|') + 1)..];
#else
		return path;
#endif
	}
}
