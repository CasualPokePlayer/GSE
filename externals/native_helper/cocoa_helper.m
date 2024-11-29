// Copyright (c) 2024 CasualPokePlayer
// SPDX-License-Identifier: MPL-2.0

#include <stddef.h>
#include <stdint.h>

#include <Availability.h>
#include <AppKit/AppKit.h>
#include <UniformTypeIdentifiers/UniformTypeIdentifiers.h>

#ifdef GSE_SHARED
	#define GSE_EXPORT __attribute__((visibility("default")))
#else
	#define GSE_EXPORT
#endif

static void set_common_properties(NSSavePanel* dialog, const char* title, const char* base_dir, const char** file_types, uint32_t num_file_types)
{
	NSString* title_string = [NSString stringWithUTF8String:title];
	[dialog setTitle:title_string];

	NSString* base_dir_string = [NSString stringWithUTF8String:baseDir];
	NSURL* base_dir_url = [NSURL fileURLWithPath:base_dir_string isDirectory:YES];
	[dialog setDirectoryUrl:base_dir_url];

	if (file_types)
	{
		NSMutableArray* types = [[NSMutableArray alloc] initWithCapacity:num_file_types];

		for (uint32_t i = 0; i < num_file_types; i++)
		{
			if (@available(macOS 11.0, *))
			{
				[types addObject:[UTType typeWithFilenameExtension:[NSString stringWithFormat:@"%s", file_types[0]]]];
			}
			else
			{
				[types addObject:[NSString stringWithFormat:@"%s", file_types[0]]];
			}
		}

		if (@available(macOS 11.0, *))
		{
			[dialog setAllowedContentTypes:types];
		}
		else
		{
			[dialog setAllowedFileTypes:types];
		}
	}
}

static char* alloc_path(NSSavePanel* dialog)
{
	const NSURL* url = [dialog URL];
	const char* path = [[url path] UTF8String];
	return strdup(path);
}

GSE_EXPORT char* cocoa_helper_show_open_file_dialog(void* main_window, const char* title, const char* base_dir, const char** file_types, uint32_t num_file_types)
{
	@autoreleasepool
	{
		NSWindow* key_window = (__bridge NSWindow*)main_window;
		[key_window makeKeyAndOrderFront:nil];

		NSOpenPanel* dialog = [NSOpenPanel openPanel];
		[dialog setAllowsMultipleSelection:NO];
		[dialog setCanChooseDirectories:NO];
		[dialog setCanChooseFiles:YES];
		[dialog setAllowsOtherFileTypes:NO];

		set_common_properties(dialog, title, base_dir, file_types, num_file_types);

		NSModalResponse response = [dialog runModal];
		[key_window makeKeyAndOrderFront:nil];
		return response == NSModalResponseOK ? alloc_path(dialog) : NULL;
	}
}

GSE_EXPORT char* cocoa_helper_show_save_file_dialog(void* main_window, const char* title, const char* base_dir, const char* filename, const char* ext)
{
	@autoreleasepool
	{
		NSWindow* key_window = (__bridge NSWindow*)main_window;
		[key_window makeKeyAndOrderFront:nil];

		NSSavePanel* dialog = [NSSavePanel savePanel];
		[dialog setAllowsOtherFileTypes:NO];

		NSString* filename_string = [NSString stringWithUTF8String:filename];
		[dialog setNameFieldStringValue:filename_string];

		set_common_properties(dialog, title, base_dir, &ext, 1);

		NSModalResponse response = [dialog runModal];
		[key_window makeKeyAndOrderFront:nil];
		return response == NSModalResponseOK ? alloc_path(dialog) : NULL;
	}
}

GSE_EXPORT char* cocoa_helper_show_select_folder_dialog(void* main_window, const char* title, const char* base_dir)
{
	@autoreleasepool
	{
		NSWindow* key_window = (__bridge NSWindow*)main_window;
		[key_window makeKeyAndOrderFront:nil];

		NSOpenPanel* dialog = [NSOpenPanel openPanel];
		[dialog setAllowsMultipleSelection:NO];
		[dialog setCanChooseDirectories:YES];
		[dialog setCanCreateDirectories:YES];
		[dialog setCanChooseFiles:NO];

		set_common_properties(dialog, title, base_dir, NULL, 0);

		NSModalResponse response = [dialog runModal];
		[key_window makeKeyAndOrderFront:nil];
		return response == NSModalResponseOK ? alloc_path(dialog) : NULL;
	}
}

GSE_EXPORT void cocoa_helper_free_path(char* path)
{
	free(path);
}
