# Tavrynexo Player

Modern Cinematic 4K/HDR Desktop Media Player built with C++20, Qt 6, FFmpeg, Vulkan, and miniaudio for Windows 10 and Windows 11.

## UI Palette:
- Midnight Black: #0B0D12
- Deep Charcoal:  #11151D
- Dark Slate:     #151A24
- Slate Gray:     #1C2330
- Electric Blue:  #4F8CFF
- Cyan:          #36D6E7
- Pure White:     #F5F7FA
- Soft Gray:      #98A2B3
- Dark Border:    #293142
- Emerald Green:  #35D49A

## 4 UI Zones:
1. TopBar (Logo, Title, Open File/Folder, Search, Settings, Window Controls)
2. SideBar (Library: Home, Videos, Music, Recent, Favorites | Playlists | Settings)
3. Video Viewport (Vulkan Surface, 2.5s Autohide Controls, Circular Play Button, 4K/HDR Badges, Subtitles)
4. Playlist Drawer (Collapsible right panel)

## Build Instructions:
- Run GitHub Actions Workflow (.github/workflows/build-windows.yml) to automatically compile TavrynexoPlayer.exe and package it with windeployqt.
- For local Windows build: run powershell ./build_windows.ps1
