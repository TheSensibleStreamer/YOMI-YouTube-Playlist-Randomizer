#!/usr/bin/env python3
"""Non-regression guard for the actual WPF 'Check for updates' status popup.

The update installation has its own separate, already compact WPF window.
The no-newer-build result is shown via BuildYomiStatusDialog instead.
"""
from pathlib import Path

controller = Path("payload/app/YomiControllerWpf.cs").read_text(encoding="utf-8-sig")
start = controller.index("private Window BuildYomiStatusDialog(")
end = controller.index("private void ShowPublicUpdateHost(", start)
dialog = controller[start:end]
check_start = controller.index("private void CheckForUpdates(bool automatic)")
check_end = controller.index("private void WireClassicMenus()", check_start)
check = controller[check_start:check_end]

assert "Height = 220" not in dialog, "No-update popup still reserves an unnecessary 220px"
assert "MinHeight = 190" not in dialog, "No-update popup still has oversized height floor"
assert "SizeToContent = SizeToContent.Height" in dialog, "Status popup must measure content"
assert "root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });" in dialog
assert "content.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });" in dialog
assert "MaxHeight = 430" in dialog
assert "VerticalScrollBarVisibility = ScrollBarVisibility.Auto" in dialog, "Long update errors must remain readable"
assert '"YOMI " + InstalledVersionText()' in dialog, "Popup title must show installed version"
assert 'No newer public build is available.' in check
assert r'is up to date.\r\nNo newer public build is available.' in check, "No-update result has an extra blank line"
assert 'BuildYomiStatusDialog("Updates"' in check, "Check for Updates must still display the content-sized dialog"
assert 'ShowPublicUpdateHost(latestText, currentText)' in check, "Available updates must still use the existing condensed installer"
print("PASS: up-to-date popup sizes to its text; long errors scroll; actual update flow is unchanged")
