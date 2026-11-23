<p align="center"><img src="branding/lockup-dark.png" width="520" alt="Tavrynexo Player"></p>

# Tavrynexo Player.

Modern desktop media player for **Windows 10 (2004+) and Windows 11** only.
Built with C# / .NET 8 / WPF and the LibVLC engine (plays almost every video and audio format).

## Developer
**Radwan Almsora** · [retwan.tech@gmail.com](mailto:retwan.tech@gmail.com) · [github.com/radwanalmsora/TavrynexoPlayer](https://github.com/radwanalmsora/TavrynexoPlayer) (open source, MIT)

## Features
- **41 interface languages** (Settings → Language…): Arabic, Bosanski, Català, Čeština, Dansk, Deutsch, English, English (British), Español, Euskara, Français, Galego, Hrvatski, Indonesian, Italiano, Korean, Lietuvių, Magyar, Malay, Nederlands, Polski, Português (Portugal), Português Brasileiro, Română, Slovenčina, Slovenski, Suomi, Svenska, Thai, Türkçe, Vietnamese, Беларуская, Русский, Татар, Українська, Հայերեն, עברית, বাংলা, 中文(简体), 日本語, 繁體中文 – applied instantly, right-to-left layout for Arabic and Hebrew
- Custom dark Fluent-style window, own title bar and menus (Media, Playback, Audio, Video, Subtitle, View, Help)
- Playlist panel, shuffle, repeat (off / all / one), drag & drop, open folder, network streams
- Resume playback where you stopped, remembered volume, recent files
- Audio and subtitle track selection, external subtitles, audio/subtitle delay
- Playback speed 0.25x–4x, aspect ratio, snapshots, fullscreen with auto-hiding controls
- Keyboard shortcuts (Help > Keyboard Shortcuts)
- Installer with optional "Open with" file registration

## Get the .exe from GitHub Actions
1. Create a repository and push this folder (the `.github` folder must be at the repo root).
2. Open the **Actions** tab. The workflow **Build Tavrynexo Player** runs on every push (or press *Run workflow*).
3. When it finishes, open the run and download the artifacts:
   - **TavrynexoPlayer-Setup** – installer (`TavrynexoPlayer-Setup-x.y.z.exe`)
   - **TavrynexoPlayer-Portable** – ZIP, run `TavrynexoPlayer.exe` directly (no .NET install needed)
4. To publish a Release page automatically, push a tag: `git tag v1.0.0 && git push origin v1.0.0`.

## Microsoft Store (MSIX)
The workflow also builds `TavrynexoPlayer_x.y.z.0_x64.msix` + `.msixupload` with the Store identity. See [STORE.md](STORE.md).

## Build locally (Windows)
```
dotnet publish src/TavrynexoPlayer/TavrynexoPlayer.csproj -c Release -r win-x64 --self-contained true -o publish
```

## Project layout
```
src/TavrynexoPlayer/     WPF application (MainWindow, Theme.xaml, dialogs, Services)
src/TavrynexoPlayer/Assets/   app.ico (multi-size), logo.png, logo-small.png, mark-white.png
installer/               Inno Setup script + wizard images + setup.ico
tools/                   Language tables (tools/langs) + build_langs.py that generates Languages/*.json
branding/                1024px logo, lockup, social preview, make_brand.py (regenerates all assets)
.github/workflows/       CI build
```

## Branding
Colors: Violet `#7C5CFF` → Cyan `#22D3EE` on Night `#0B0D14`. Mark: rounded "T" + play triangle.
Run `python branding/make_brand.py` (needs Pillow + numpy) to regenerate every icon and installer image.

## Translations
Edit `tools/langs/b*.txt` (one line per string, same order as `tools/langs/keys.txt`) and run `python tools/build_langs.py`.

<p align="center"><img src="لقطة الشاشة 2026-11-18 031239.png" width="520" alt="Tavrynexo Player"></p>

## License
MIT (see `LICENSE`). Uses LibVLC (LGPL-2.1+), see `THIRD-PARTY-NOTICES.md`.
