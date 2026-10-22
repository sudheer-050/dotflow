# Dotflow 0.7.0

Native Windows dictation, personal notes and meeting transcripts with the small English Vosk model. No Docker, Python, Holly, cloud API or account. Supported target: Windows 10/11 x64 with .NET Framework 4.8.

## Install

Build `dist/Dotflow-Setup-0.7.0.exe` with `./scripts/build_setup.ps1` (this version has not been published by this session) and run it from your normal Windows desktop. The small executable is a bootstrapper: first installation needs internet for the native runtime and approximately 40 MB English model. Setup verifies pinned SHA-256 hashes, checks model loading, then offers Open app. Speech afterwards runs offline. Stop recording and exit the older app when Setup requests it; upgrades preserve notes and preferences.

This package is unsigned and has not replaced the installed older version. Source and the unsigned Windows preview release are published at https://github.com/sudheer-050/dotflow (see its Releases page; installer SHA-256 is in SHA256SUMS.txt). No signing certificate is configured, so Windows SmartScreen will warn and the in-app updater will not accept this build. No macOS, Android or iOS build is supplied.

### Unsigned preview

This is a hobby/preview release and the installer is not code-signed, so Windows SmartScreen may show "Windows protected your PC". Choose **More info** then **Run anyway** if you trust it, or build from source instead. To check your download, compare its hash with `SHA256SUMS.txt` from the same release:

```powershell
Get-FileHash .\Dotflow-Setup-0.7.0.exe -Algorithm SHA256
```

Automatic updates are disabled for unsigned builds; download new versions from the Releases page.
## Writing

Hold F8 or your recorded shortcut to speak; release to refine. Escape cancels. Held modifier shortcuts can delay live insertion until release. Preferences supports terminal paste methods; no Enter is sent. Interrupted insertion offers recovery rather than blindly duplicating text. Failed dictation retains the latest audio in memory for Retry, Copy or Discard. Recovery does not survive exit. Successful paste attempts restore the prior clipboard if no newer copy occurred; clipboard restoration is best effort across formats/apps.

The slim toolbar snaps to the nearest usable edge, horizontal at top/bottom and vertical at left/right, within approximately 1 mm at ordinary scaling. Hover to reveal icons, hover each icon for its name, and drag the bar/grip to reposition. Preferences provides visibility, optional voice animation, reduced motion and Light/Dark/System themes. The system tray offers a one-hour snooze and keyboard action menu.

## Notes and meetings

Home provides Dictate, New note, Record meeting, readiness and recent work. Notes captures microphone, computer output or both, remembers device IDs, and shows separate meters. Pause/resume preserves the timeline in one record. Changed default endpoints stop capture with saved audio and recovery instructions. Select the output actually used by the call; capture includes all apps using that device. Headphones reduce duplicated audio.

Continuous per-source recognition preserves context across queued chunks. Separate WAV tracks retain synchronized silence gaps. Quiet-noise suppression is a conservative signal-level gate, not a validated speech detector: quiet speech can be missed and loud noise/music retained.

Titles and names are inferred from content/introductions and editable later. Shared output remains labelled by source: participant names do not identify each speaker. Library search includes transcript/body, names and tags. Conversation is read-only; annotations remain editable. Ctrl+F/F3 finds conversation text. Double-click a timestamp for playback, with source, seek and speed controls. Local review groups source quotes into an overview, possible decisions/follow-ups and keyword answers. It uses original text rather than semantic AI inference. Speaker labels can be corrected without editing spoken words.

Export previews Conversation, Personal notes or Full record, with optional participant metadata. Names spoken in the transcript are not redacted. PDFs have searchable, tagged Unicode text; The export dialog offers view-oriented PDF only. External PDF copies can still be altered with specialist tools. Exports are not signed or tamper-proof evidence and never change original spoken words. File replacement is atomic.

Storage provides size breakdowns, recently deleted restore, confirmed permanent deletion, opt-in retention and active-note ZIP backup/restore. Existing note IDs are skipped on restore. Backups exclude deleted records, preferences and speech components. Default retention keeps deleted records until explicitly removed.

## Preferences and data

Recording, Writing, Notes, App, Storage and About are grouped with unsaved-change prompts. Dictation uses a press-to-record shortcut picker; the single note toggle shortcut uses a dropdown. Common reserved/prefix conflicts produce warnings. Dictionary/snippets support local import/export and affect dictation only.

Notes are in `%LOCALAPPDATA%\LocalDictation\Notes`. Dictation audio stays in memory; note capture deliberately saves audio/text. The updater offers opt-in startup checks and manual installation; it requires a configured HTTPS manifest and trusted certificate thumbprint, and verifies installer hash and Windows signature before launching. No public signed feed is configured. Uninstall retains user data by default and separately offers component/data removal.

## Build and verification

Run `./scripts/build_setup.ps1`. Builds use the installed Windows .NET compiler without downloading build dependencies. `scripts/prepare_release.ps1` prepares local metadata only; its default GitHub repository is a proposal, not a published location. A real CurrentUser signing certificate may be selected through `LOCALDICTATION_SIGN_CERTIFICATE`.

Local checks: 79 passed, four live checks deselected. Includes WAV append, backup/restore and traversal rejection, dictionary validation, content search, recovery discard, export structures, searchable Unicode PDF, setup hashes and real installed-engine shutdown lifetime. App-owned Light/Dark previews were rendered. Real microphone/call capture, playback, cross-app typing, multi-monitor DPI and accessibility require a normal desktop session. These checks do not establish Wispr-equivalent accuracy.

See [USER_GUIDE.md](USER_GUIDE.md), [RELEASE_NOTES.md](RELEASE_NOTES.md), [BACKLOG_STATUS.md](BACKLOG_STATUS.md) and [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).


Version 0.7.0 adds explicit Standard, Chat, Email and Code writing styles, a searchable Words & snippets editor, and user-controlled Record/Stop on Practice. Closing recording-device selection without Apply cancels changes. Setup records successful microphone and recognition checks, then asks you to confirm typing into another app. Conversations remain read-only; titles, participant labels and personal notes can be edited separately.
