# Notewll 0.9.0 Windows preview verification

## Installer

- One Windows x64 setup executable embeds the application, native Vosk runtime, small English model, speech detector, worker scripts, notices and uninstaller.
- Clean extraction into an isolated directory passed the installer bundle check and loaded its own bundled speech engine offline (`bundle-ok|offline=true`). No installation registry entries or user shortcuts were modified by that check.
- Installer tests cover payload hashes, embedded worker/detector hashes, extraction and archive path traversal.
- The installer is unsigned. Full interactive registry/shortcut installation on a clean Windows account remains a desktop acceptance check. Enhanced recognition models and Python environments are optional separate setup, not part of the base bundle.

## Application

119 application, installer, release, worker and regression checks passed; four live microphone/keyboard checks were excluded. Ball verification covers 171 appearance/gravity combinations, independent expanded sizing, live color/gradient previews, saved local images and image rotation. Focused native click, timer/physics, four-edge, DPI, reduced-motion and repaint checks passed in the local verification scripts.

Physical microphone/call capture and cross-application keyboard injection are not established by these checks. Speech accuracy has no new human-reference measurement in this release.

## Artifact

- Version: 0.9.0 Windows x64 preview
- File: Notewll-Setup-0.9.0.exe
- Bytes: 59069952
- SHA256: `23543d7f37e4103c652518c1b7b06c93182efc90480e70b835089bef36bdb5f4`
