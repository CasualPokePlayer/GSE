// Copyright (c) 2024 CasualPokePlayer
// SPDX-License-Identifier: MPL-2.0

#include <stddef.h>
#include <stdint.h>

#ifdef GSE_SHARED
	#ifdef _WIN32
		#define GSE_EXPORT __declspec(dllexport)
	#else
		#define GSE_EXPORT __attribute__((visibility("default")))
	#endif
#else
	#define GSE_EXPORT
#endif

#ifdef _WIN32
	#define GSE_MEM_EXPORT __declspec(dllexport)
#else
	#define GSE_MEM_EXPORT __attribute__((visibility("default"))) __attribute__((used))
#endif

GSE_MEM_EXPORT void* GSE_GB_WRAM_PTR = NULL;
GSE_MEM_EXPORT size_t GSE_GB_WRAM_LEN = 0;

GSE_MEM_EXPORT void* GSE_GB_SRAM_PTR = NULL;
GSE_MEM_EXPORT size_t GSE_GB_SRAM_LEN = 0;

GSE_MEM_EXPORT void* GSE_GB_HRAM_PTR = NULL;
GSE_MEM_EXPORT size_t GSE_GB_HRAM_LEN = 0;

GSE_MEM_EXPORT void* GSE_GBA_IWRAM_PTR = NULL;
GSE_MEM_EXPORT size_t GSE_GBA_IWRAM_LEN = 0;

GSE_MEM_EXPORT void* GSE_GBA_EWRAM_PTR = NULL;
GSE_MEM_EXPORT size_t GSE_GBA_EWRAM_LEN = 0;

GSE_MEM_EXPORT void* GSE_GBA_SRAM_PTR = NULL;
GSE_MEM_EXPORT size_t GSE_GBA_SRAM_LEN = 0;

enum MemExportType
{
	GB_WRAM,
	GB_SRAM,
	GB_HRAM,
	GBA_IWRAM,
	GBA_EWRAM,
	GBA_SRAM,
};

GSE_EXPORT void export_helper_set_mem_export(enum MemExportType which, void* ptr, size_t len)
{
	#define MEM_EXPORT_CASE(MEM_EXPORT_TYPE) \
		case MEM_EXPORT_TYPE: \
			GSE_##MEM_EXPORT_TYPE##_PTR = ptr; \
			GSE_##MEM_EXPORT_TYPE##_LEN = len; \
			break;

	switch (which)
	{
		MEM_EXPORT_CASE(GB_WRAM)
		MEM_EXPORT_CASE(GB_SRAM)
		MEM_EXPORT_CASE(GB_HRAM)
		MEM_EXPORT_CASE(GBA_IWRAM)
		MEM_EXPORT_CASE(GBA_EWRAM)
		MEM_EXPORT_CASE(GBA_SRAM)
	}

	#undef MEM_EXPORT_CASE
}
