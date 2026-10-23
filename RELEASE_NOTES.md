# Notewll 0.8.0 - faster local dictation

Windows x64, English speech, unsigned preview. Recognition and grammar stay on your computer.

- Shortcut dictation now recognizes speech every 250 ms while you speak, including when live insertion is disabled or held modifier keys delay typing.
- Optional enhanced grammar warms at startup and corrects partial sentences in the background. Matching corrections and unchanged whole-word prefixes are reused as speech grows.
- Release finalizes the live recognizer instead of running a second full Whisper transcription. Grammar has a 600 ms wait budget; slow or unavailable grammar falls back to basic local formatting and filler removal. Late corrections never rewrite a completed dictation.
- Silence, quick shortcut taps and empty recognition results return quietly to idle without warning popups, inserted text or clipboard changes.
- Enhanced setup includes local contextual grammar, saved vocabulary hints for Whisper-based note/file/recovery paths, and meeting voice grouping. The standard installer does not install Python/Ollama or grammar models; enhanced grammar requires the optional setup documented in SPEECH_QUALITY_UPDATE.md.
- Playground preview no longer clears a computed result when an empty optional result is supplied.
- Existing notes, recordings, shortcuts and preferences are retained. Installer, application metadata and About now report 0.8.0.

Measured latency: three six-second local voice recordings, replayed in real time, produced formatted text 509, 416 and 148 ms after simulated release. These are three warm replay measurements, not physical microphone-to-keyboard timings, p95 estimates, accuracy scores or a guarantee for every PC. Skipping the second Whisper pass can reduce recognition accuracy; fallback cleanup is not full model grammar editing.

Verification: 91 application, installer, speech-worker and regression checks passed; four live microphone/keyboard checks were excluded. Direct empty-result/cancellation checks passed too. The full installer embeds the exact verified 0.8.0 application. A cleanup-preview failure found during packaging was fixed and both preview checks now pass. Real microphone capture returns MMSYSERR 1 in this managed session, and keyboard injection is unavailable. Physical shortcut timing, cross-app insertion and recognition accuracy still need desktop acceptance. The installer is unsigned and the certificate-verified automatic updater remains unconfigured; install this preview manually from GitHub Releases.

See verification/STREAMING_DICTATION.md for the measured scope and remaining checks.

---

# Notewll 0.7.0

Windows x64, English-only, offline speech after initial setup download.

- PDF-only conversation export with readable scope previews and explicit participant-label wording. Original words remain read-only; external PDF copies are not tamper-proof.
- Responsive export, playback, storage and backup interfaces; separate retention and deleted-record recovery.
- Saved-record review separated from new recording controls; actual saved audio sources only, device/activity feedback and grouped actions.
- Apply/Cancel for device changes; permanent toolbar hiding and snooze respected across note actions.
- Compact Home, contextual recovery, consistent Notewll branding, improved themed tabs/dropdowns, searchable word/snippet forms and explicit correction saving.
- Standard, Chat, Email and conservative Code styles; user-controlled Practice and guided first-use checks.

Verification: 85 automated checks passed, four live checks excluded. Isolated UI checks preserve original words and preferences. See FIX_REPORT_0.7.0.md for all 38 review findings and remaining acceptance work.

The installer is unsigned because no code-signing certificate was provided. Published as an unsigned preview. Installation and live desktop acceptance have not been verified in this managed session. Real microphone/call capture, browser/terminal injection, mixed-DPI pointer behavior, Narrator and consented speech-accuracy comparison require a normal desktop session. The managed session denies those operations. No macOS/mobile/cloud sync release is included.


October 22, 2026 update: renamed the application to Notewll across the UI, setup, shortcuts, exports and documentation. Dictation uses direct typing without reading the target field or an automatic paste retry. Existing LocalDictation data and installation identifiers are retained for upgrades. Shortcut preferences are preserved. The Windows preview installer is unsigned; real cross-app insertion still needs desktop acceptance.
