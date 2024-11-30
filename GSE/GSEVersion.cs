// Copyright (c) 2024 CasualPokePlayer
// SPDX-License-Identifier: MPL-2.0

namespace GSE;

/// <summary>
/// GSE versioning
/// Follows usual semantic versioning
/// Major/Minor versions are incremented with git tags
/// Patch is incremented by each commit since the last tag
/// If the master branch is not being used, the current branch will be appended to the version
/// </summary>
internal static class GSEVersion
{
	// ReSharper disable HeuristicUnreachableCode
	public const string FullSemVer = ThisAssembly.Git.SemVer.Major +
	                                 $".{ThisAssembly.Git.SemVer.Minor}" +
	                                 (ThisAssembly.Git.SemVer.Patch != "0" ? $".{ThisAssembly.Git.SemVer.Patch}" : "") +
	                                 $"{ThisAssembly.Git.SemVer.DashLabel}" +
	                                 $"{(ThisAssembly.Git.IsDirty ? "-dirty" : "")}" +
	                                 $"{(ThisAssembly.Git.Branch == "master" || ThisAssembly.Git.BaseTag == ThisAssembly.Git.Tag ? "" : $"/{ThisAssembly.Git.Branch}")}";
}
