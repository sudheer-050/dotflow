# Audit implementation status â€” 0.6.1

2026-10-21. All 54 findings from output/ux-audit/GAPS.csv are tracked below. Implemented means the described local change exists; it does not assert a passed real microphone/call/desktop acceptance test. Partial items retain their stated limits. This is not a claim that all audit work is complete.

## Reliability

| ID | Status | Delivered / remaining |
|---|---|---|
| R01 | Implemented | In-memory retry/copy/discard; no disk dictation history. |
| R02 | Implemented; stress QA pending | Native stream leases defer shared model destruction; background notes keep recoverable jobs. Actual native shutdown lifetime fixture passes. Repeated exit-under-load desktop stress remains. |
| R03 | Implemented | Modifier-key live-insertion limitations explained; function-key recommendation. |
| R04 | Implemented | Partial insertion produces explicit recovery without blind full-text paste. Cross-app acceptance pending. |
| R05 | Implemented | Clipboard sequence-aware restoration after paste; format support best effort. |
| R06 | Implemented | Timestamped session issues remain available from Home/tray until acknowledged, including note/device errors. No dictation transcript log is written to disk. |
| R07 | Implemented | Mouse Cancel while dictating/processing. |
| R08 | Implemented; accuracy QA pending | Continuous recognizer per source and speech-aware queue boundaries; shared speech/overlap fixture comparison remains. |
| R09 | Partial; speech evaluation pending | Quiet gate preference and explicit limits. Four synthetic noise fixtures with/without gate produced no words. Windows denied speech-fixture synthesis; soft speech, music and human corpus testing remain. |
| R10 | Implemented | Cached engine readiness on Home and check/repair actions. |
| R11 | Implemented | Saved microphone/output endpoint IDs and selection fallback. |
| R12 | Implemented; hardware QA pending | Default device change stops capture safely, preserves audio and permits explicit device reselection/resume. No seamless automatic switching claim. |
| R13 | Implemented; hardware QA pending | Separate visible mic/output meters. |
| R14 | Implemented; hardware QA pending | Pause/resume one session; append preserves existing WAV samples. |
| R15 | Pending external evaluation | No consented shared corpus or usable Wispr desktop session. No comparative accuracy/latency claim. |

## Interface

| ID | Status | Delivered / remaining |
|---|---|---|
| U01 | Implemented | Home actions, readiness and recent work. |
| U02 | Implemented | Home meeting action and shared library filters. |
| U03 | Implemented | Write/record-personal/record-meeting empty state. |
| U04 | Implemented; user acceptance pending | Home cards compacted further; resizable content workspace and grouped settings. Broader user testing remains. |
| U05 | Implemented; accessibility acceptance pending | Shared palette/buttons/tabs/list rows; high-contrast foreground/selection colors. Native controls preserved. Narrator and normal-desktop polish review remain. |
| U06 | Partial | Literal ampersands/separators corrected; 150%/200% desktop inspection pending. |
| U07 | Implemented | Light/Dark/System themes, live OS preference refresh marshalled to UI thread, event handler detached on disposal. |
| U08 | Implemented; accessibility QA pending | Keyboard action menu from system tray preserves nonactivating floating buttons. Narrator acceptance pending. |
| U09 | Implemented; pointer QA pending | Fade, reduced motion, delayed collapse and one-time hover drag hint. |
| U10 | Implemented | Separate voice animation preference; disabled for fresh installs, preserved for legacy users. |
| U11 | Implemented | Permanent visibility setting and one-hour snooze; existing show/reposition controls. |
| U12 | Implemented; hardware QA pending | Display/work-area and DPI relocation handlers; real mixed-DPI monitor tests pending. |
| U13 | Implemented | Open-window glyph, consistent icon names/tooltips and accessibility labels; no idle text added. |

## Notes

| ID | Status | Delivered / remaining |
|---|---|---|
| N01 | Implemented | Date/kind/status/duration rows, filters and remembered Newest/Oldest/Title/Longest sort. |
| N02 | Implemented | Body/transcript/name/tag search and Ctrl+F/F3 in conversation. |
| N03 | Implemented; audible/call QA pending | Source-correct timestamp seeking, playback and speed. Windows media API seek/speed/pause/close fixture passed; audible playback in a real meeting remains. |
| N04 | Implemented for source/metadata scope | Participant introductions stay separate from voice identity. Per-section speaker-label editor preserves raw words and audio source. No automatic voice diarization claim. |
| N05 | Implemented | Browse saved records while active session remains pinned in recording status. |
| N06 | Implemented | Recorded source metadata separate from labelled Next recording selector. |
| N07 | Implemented | Missing-section counts/time ranges, retained-audio status and retry. |
| N08 | Implemented for initial scope | Resizable library and metadata tags. Folders/batch actions were conditional future extensions in the audit, not prerequisites for this stage. |
| N09 | Implemented | Visible Export, adjacent playback and conventional More menu. |
| N10 | Implemented | One scope-preview path for all export controls, participant metadata toggle, remembered format. Source quote labels respect metadata choice; spoken names are not redacted. |
| N11 | Implemented for stated scope | Read-only in app; protection limitations explained. No signed authenticity snapshots. |
| N12 | Implemented; assistive-reader QA pending | Unicode searchable PDF with structure tree, headings/paragraphs and ActualText, wrapped Excel cells, export progress/Open. No PDF/UA certification claim. |
| N13 | Implemented | Recently deleted, restore, confirmed permanent deletion and optional retention. |
| N14 | Implemented | Storage breakdown, scoped cleanup and active-note ZIP backup/restore. |
| N15 | Implemented for local extractive scope | Separate source-quote overview, possible decisions/follow-ups and keyword question answering with timestamps. No semantic inference or added model; generative AI remains optional future work. |

## Preferences and delivery

| ID | Status | Delivered / remaining |
|---|---|---|
| P01 | Implemented | Grouped settings, unsaved-change prompts and tests using draft microphone. |
| P02 | Implemented | Key picker versus note dropdown explained; duplicate rejection and common reserved/prefix conflict warnings. |
| P03 | Implemented | Check speech engine, readiness, model details and repair. |
| P04 | Implemented; desktop acceptance pending | Skippable permissions/practice guide and on-demand setup checks. This session cannot establish a successful microphone-to-other-app round trip. |
| P05 | Implemented; desktop review pending | Task help and rewritten guide; preview screenshots available in output/v060-layout-preview. |
| P06 | Partial / certificate required | Pinned download hashes, staged progress, cancellation and signing script. No actual signing certificate; package unsigned. |
| P07 | Partial; GitHub/signing blocked | Version 0.6.1, About/release notes, opt-in startup feed checks and hash/publisher-verifying manual installs. Public repository/release waits for valid GitHub login; signed feed needs a real certificate. |
| P08 | Implemented | Estimated app installed size plus separate storage breakdown and explicit optional uninstall cleanup. |
| P09 | Implemented | Local dictionary/snippets with validated import/export; dictation only. |
| P10 | Implemented for first-stage scope | Verbatim/clean switch and Playground before/after; no semantic rewrite/styles. |
| P11 | Implemented scope declaration | Active-note portable backup/restore. Current supported product is Windows x64 English-only. Mobile/macOS/cloud/team features remain separate future products, not claimed delivered. |

## Verification and release gate

79 application tests passed; four live integration tests intentionally deselected because desktop/audio/service capabilities are unavailable here. Tests include actual installed Vosk stream lifetime, ZIP traversal rejection, non-overwriting restore, preserved WAV append, dictionary validation, content search, searchable Unicode PDF, export protection structure and setup hash validation. App-owned light/dark screens and review control bounds/content were checked; note transport clipping found during review was fixed with an explicit table row constraint.

Remaining release acceptance: normal-user microphone and both-source call capture, pause/resume under device changes, playback/speed, terminal/browser insertion, clipboard formats, recovery after focus changes, exit-under-load, mixed-DPI edge positioning, keyboard/Narrator/high contrast, and consented speech/noise accuracy fixtures. No live Wispr comparison was performed.

The installer and update metadata are local only. Source and the unsigned preview prerelease target `sudheer-050/dotflow` (the verified GitHub login). A GitHub profile is not a Windows signing certificate. Supply/configure a real certificate and exact repository before public signed delivery. The installed 0.5.0 app was not overwritten by this build.

0.6.1 additional evidence: native 16-stream shutdown and continuous two-source silence checks passed; all eight synthetic-noise/gate combinations produced empty text; Windows media seek/speed/pause/close passed. Managed-session device capture failed with UnauthorizedAccessException and keyboard insertion was blocked. These are recorded external acceptance limits, not passing tests. Source-publication snapshots exclude output, recordings, caches and local preferences.

## User-reported manual checks (2026-10-21)

The owner reported that a meeting recording and dictation into a terminal and a browser worked. The installed app at that time was version 0.5.0 (verified: the installed executable reports 0.5.0.0), not 0.6.1, so these results do not count as 0.6.1 acceptance. Repeat them after installing 0.6.1.
