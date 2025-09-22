# Packaging images

Add the MSIX visual assets to this folder before building the packaging project. Use PNG files with the following names and base (100% scale) dimensions:

| File name | Size (px) | Notes |
| --- | --- | --- |
| `StoreLogo.png` | 50 × 50 | Displayed in the Microsoft Store listing and installer.
| `Square44x44Logo.png` | 44 × 44 | Small tile/icon.
| `Square71x71Logo.png` | 71 × 71 | Medium tile/icon.
| `Square150x150Logo.png` | 150 × 150 | Start menu/installer tile.
| `Square310x310Logo.png` | 310 × 310 | Large tile.
| `Wide310x150Logo.png` | 310 × 150 | Wide tile.
| `SplashScreen.png` | 620 × 300 | Splash screen image shown at launch.

Provide higher scale assets (e.g., `*.scale-200.png`) if desired. The packaging project references these files directly, so they must exist for the MSIX build to succeed.
