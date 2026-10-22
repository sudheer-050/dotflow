# Notewll 0.7.0

Windows x64, English-only, offline speech after initial setup download.

- PDF-only conversation export with readable scope previews and explicit participant-label wording. Original words remain read-only; external PDF copies are not tamper-proof.
- Responsive export, playback, storage and backup interfaces; separate retention and deleted-record recovery.
- Saved-record review separated from new recording controls; actual saved audio sources only, device/activity feedback and grouped actions.
- Apply/Cancel for device changes; permanent toolbar hiding and snooze respected across note actions.
- Compact Home, contextual recovery, consistent Notewll branding, improved themed tabs/dropdowns, searchable word/snippet forms and explicit correction saving.
- Standard, Chat, Email and conservative Code styles; user-controlled Practice and guided first-use checks.

Verification: 84 automated checks passed, four live checks excluded. Isolated UI checks preserve original words and preferences. See FIX_REPORT_0.7.0.md for all 38 review findings and remaining acceptance work.

The installer is unsigned because no code-signing certificate was provided. This session built the package locally; it did not install or publish it. Real microphone/call capture, browser/terminal injection, mixed-DPI pointer behavior, Narrator and consented speech-accuracy comparison require a normal desktop session. The managed session denies those operations. No macOS/mobile/cloud sync release is included.


October 22, 2026 update: renamed the application to Notewll across the UI, setup, shortcuts, exports and documentation. Dictation uses direct typing without reading the target field or an automatic paste retry. Existing LocalDictation data and installation identifiers are retained for upgrades. Shortcut preferences are preserved. The Windows preview installer is unsigned; real cross-app insertion still needs desktop acceptance.
