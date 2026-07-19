#include "imgui.h"
#include "imgui_internal.h"

#if defined(_WIN32)
#define GSE_EXPORT extern "C" __declspec(dllexport)
#else
#define GSE_EXPORT extern "C" __attribute__((visibility("default")))
#endif

GSE_EXPORT void gseFocusMenuBarOnNav(void)
{
	ImGuiContext& g = *GImGui;

	if (g.NavLayer != ImGuiNavLayer_Main)
		return;
	if (g.NavWindow != nullptr && (g.NavWindow->Flags & (ImGuiWindowFlags_Popup | ImGuiWindowFlags_ChildMenu)) != 0)
		return;

	const bool nav_key = ImGui::IsKeyPressed(ImGuiKey_LeftArrow) || ImGui::IsKeyPressed(ImGuiKey_RightArrow)
		|| ImGui::IsKeyPressed(ImGuiKey_UpArrow) || ImGui::IsKeyPressed(ImGuiKey_DownArrow);
	if (!nav_key)
		return;

	ImGuiWindow* menubar = ImGui::FindWindowByName("##MainMenuBar");
	if (menubar == nullptr || !(menubar->DC.NavLayersActiveMask & (1 << ImGuiNavLayer_Menu)))
		return;

	ImGui::ClearActiveID();
	ImGui::FocusWindow(menubar);
	menubar->NavLastIds[ImGuiNavLayer_Menu] = 0;
	g.NavLayer = ImGuiNavLayer_Menu;
	ImGui::NavInitWindow(menubar, true);
	ImGui::NavRestoreHighlightAfterMove();
}

GSE_EXPORT void gseCloseDismissableModalOnEscape(void)
{
	ImGuiContext& g = *GImGui;

	// imgui closes non-modal popups (e.g. combos) on Escape during NewFrame, before this runs;
	// if one closed this frame, imgui already consumed the Escape -- don't also close the modal.
	static int prev_open_popups = 0;
	const int open_popups = g.OpenPopupStack.Size;
	const bool imgui_closed_a_popup = open_popups < prev_open_popups;
	prev_open_popups = open_popups;

	if (open_popups == 0 || imgui_closed_a_popup || g.ActiveId != 0
		|| !ImGui::IsKeyPressed(ImGuiKey_Escape, false))
		return;

	ImGuiWindow* top = g.OpenPopupStack.back().Window;
	if (top && (top->Flags & ImGuiWindowFlags_Modal) && top->HasCloseButton)
		ImGui::ClosePopupToLevel(open_popups - 1, true);
}
