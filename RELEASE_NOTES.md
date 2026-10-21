# Dotflow / Local Dictation 0.6.1 release notes

Windows x64 preview. The application currently appears as Local Dictation; Dotflow is its public repository name.

- Safer native speech-engine shutdown, in-memory dictation retry and clipboard restoration.
- Compact Home actions, recent work, persistent session issues, grouped preferences and first-run guidance.
- Light/Dark/System themes, live system-theme refresh, high-contrast button/list colors, reduced motion and optional voice animation.
- Edge-docked hover toolbar, one-time drag hint and keyboard action menu.
- Continuous per-source meeting recognition, separate meters, pause/resume and explicit device-change recovery.
- Content search, sort by date/title/duration, tags, editable speaker labels that preserve original words, and timestamp playback.
- Local source-quote overview, possible decisions/follow-ups and keyword answers with timestamps. No new AI model, cloud service or invented speaker attribution.
- Previewed export scopes, remembered format, searchable/tagged Unicode PDF, Word/Excel convenience protections and CSV.
- Recently deleted recovery, opt-in retention, size breakdown and atomic note backup. Imports validate all metadata before moving records and never overwrite existing IDs.
- Opt-in startup update checks and manual hash/publisher verification. Unsigned installers are rejected by the updater.

First installation needs internet for the native runtime and small English model; speech afterwards is offline. No Docker, Python installation or cloud account is needed. The setup executable is a bootstrapper, not the whole installed runtime.

This installer is unsigned: no Windows code-signing certificate is configured. It is published as a preview, not a signed automatic update. Document editing restrictions are removable and are not tamper-proof evidence.

Verification: 79 application checks passed, four live checks deselected. Real installed Vosk passed 16-stream concurrent shutdown and continuous both-source silence tests. Synthetic silence, hum, clicks and quiet noise produced no words, with and without the quiet gate. Windows media playback seek/speed/pause/close passed on a fixture.

Remaining acceptance limits: this managed session denies microphone/call capture and keyboard injection, and blocks Windows synthetic speech generation. Human speech accuracy, real calls, terminal/browser insertion, audible playback, mixed-DPI monitors and Narrator still need a normal desktop session. No direct Wispr accuracy comparison was performed. Source-quote review is lexical, not a semantic AI summary or generative Q&A. PDF structure tags are present, without a PDF/UA certification claim. Windows English-only remains the platform scope; no mobile/macOS/cloud sync build is included.
