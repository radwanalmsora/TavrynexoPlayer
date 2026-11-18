# Microsoft Store submission – Tavrynexo Player

## Identity (already set in `msix/AppxManifest.xml`)
| Field | Value |
|---|---|
| Package/Identity/Name | `RadwanAlmsora.TavrynexoPlayer` |
| Package/Identity/Publisher | `CN=DB86E487-92CC-4FB9-BDBC-F161416E3A4E` |
| PublisherDisplayName | `Radwan Almsora` |
| Package Family Name | `RadwanAlmsora.TavrynexoPlayer_ys76xt3sgtetj` |
| Store ID | `9P41W48FZG20` |

## Get the package
1. Push to GitHub, open **Actions → Build Tavrynexo Player → latest run**.
2. Download the artifact **TavrynexoPlayer-Store-MSIX**. It contains
   - `TavrynexoPlayer_x.y.z.0_x64.msix` (unsigned – the Store signs it for you)
   - `TavrynexoPlayer_x.y.z.0_x64.msixupload` (same package, upload format)
3. Optional local test: artifact **TavrynexoPlayer-MSIX-Test-Signed** has a self-signed MSIX and its `.cer`.
   Install the `.cer` into *Trusted People* (Local Machine) first, then double-click the MSIX.
   Never upload the test-signed file to the Store.

## Versions
Each submission must have a higher version. Normal pushes use `1.0.<run number>.0`.
For a chosen version push a tag, e.g. `git tag v1.1.0 && git push origin v1.1.0` → MSIX version `1.1.0.0`.
(The Store reserves the 4th number, so it is always 0.)

## Partner Center → your app → Start submission
1. **Packages**: upload the `.msixupload` (or `.msix`). Targets Windows 10 version 2004+ / Windows 11, x64.
2. **Store listing** (per language): description, release notes, and images from `branding/store/`:
   - `store-logo-1080x1080.png` (1:1 logo / box art), `store-logo-300x300.png`
   - `store-poster-720x1080.png` (2:3 poster)
   - `store-hero-1920x1080.png` (16:9 hero)
   - Screenshots: take 4+ real screenshots of the app (1366×768 or larger) – required, I cannot generate real ones.
3. **Properties**: Category *Photo & video* (or *Music*), privacy policy URL (required – the app collects no data, say so),
   website `https://github.com/radwanalmsora/TavrynexoPlayer`, support email `retwan.tech@gmail.com`.
4. **Restricted capability `runFullTrust`**: in *Submission options* write a justification, e.g.
   "Desktop media player (WPF/.NET) that plays local files and network streams with the native LibVLC engine; it needs full trust to load native codecs."
5. **Age rating** questionnaire, pricing, availability, then **Submit**.

## Notes
- The app writes settings to `%AppData%\TavrynexoPlayer` (virtualized automatically inside MSIX).
- Open-source licence notice for LibVLC is shown in the About dialog and in `THIRD-PARTY-NOTICES.md`.
