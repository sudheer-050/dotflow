# Notewll 0.7.0 implementation report

The review fixes are implemented locally and packaged. This is not a claim that all 38 acceptance criteria are closed: signing, speech-quality measurement, desktop integration and accessibility need external inputs or a normal desktop session. The installed 0.6.1 app was left intact, and no release was published by this session.

The largest changes are PDF-only export, safer device preferences, clearer recording/review separation, responsive dialogs and playback, organized storage, explicit writing styles, guided Practice and consistent Notewll branding. Original conversation words remain read-only; editable titles, speaker metadata and personal notes are separate. No distributed PDF can be guaranteed impossible to modify externally.

## Review disposition

| Finding | Status | Result / remaining work |
|---|---|---|
| UR01 | Implemented | Removed editable formats from the export UI; PDF snapshot only. |
| UR02 | Improved; external limit | Original words remain read-only. PDF notice explains external alteration; cryptographic document signing is not implemented. |
| UR03 | Implemented | Plain readable preview, normalized line endings, dates and empty-section omission. |
| UR04 | Implemented; DPI acceptance pending | Content-sized export rows; isolated rendered dialog inspected. All physical display scales not tested. |
| UR05 | Implemented | Consumer About hides publisher URL/thumbprint fields; signed-feed status remains. |
| UR06 | External blocker | No real code-signing certificate supplied. Package is unsigned; signed public updater acceptance remains open. |
| UR07 | Improved; accuracy pending | Conservative greeting/name, weekday and explicit list formatting; verbatim and Code choices. No measured acoustic accuracy gain claimed. |
| UR08 | Implemented | Explicit Standard, Chat, Email and Code styles, without hidden context collection. |
| UR09 | Improved; live acceptance pending | Selected device names and per-source activity shown; meeting audio help retained. Activity is not speech detection. |
| UR10 | Implemented | Saved-review actions separated from recording transport/configuration. |
| UR11 | Implemented; audible acceptance pending | Styled read-only transcript and responsive player; only existing audio tracks offered. |
| UR12 | Implemented | Renamed lexical lookup to Find related statements. |
| UR13 | Improved | Multi-selection speaker assignment replaces dense grid. Long quotes may still require resizing; no diarization claim. |
| UR14 | Implemented | Apply commits device selection; Cancel/X preserve prior preferences, covered by isolated test. |
| UR15 | Implemented; desktop acceptance pending | Removed unconditional toolbar show; hidden preference and snooze honored. |
| UR16 | Open acceptance work | Needs consented reference speech/call corpus and comparison. Silence tests do not establish speech quality. |
| UR17 | Implemented | Smaller Home header/cards, recent work closer to primary actions. |
| UR18 | Implemented | Recovery/issues hidden from clean Home until relevant. |
| UR19 | Implemented | Notewll visible brand, installer, shortcuts and assembly product metadata. Internal upgrade/data identifiers preserved. |
| UR20 | Improved | Shared dialog spacing, themed tab strip/dropdowns/list headers; no framework rewrite. |
| UR21 | Improved; desktop acceptance pending | Dark dropdown/tab rendering improved. Native arrow chrome can remain; real high-contrast/theme matrix pending. |
| UR22 | Implemented | Search/filter list, Add/Edit forms, word/snippet choices; JSON actions under More. |
| UR23 | Implemented | Explicit Remember correction workflow stages a word rule. This is post-recognition replacement, not vocabulary biasing. |
| UR24 | Improved; desktop acceptance pending | Checklist tracks real mic/recognition success and asks user to confirm insertion. |
| UR25 | Implemented | Practice Record/Stop controlled by user; bounded duration and secondary file diagnostics. |
| UR26 | Implemented | Help leads with typing, mic, meeting, recovery, PDF and storage tasks. |
| UR27 | Implemented | Help reflects optional voice indicator and toolbar state. |
| UR28 | Improved | Font-relative history hierarchy and full-detail tooltips; physical scaling acceptance pending. |
| UR29 | Implemented | Overflow grouped into Review, Recording, Export and Manage. |
| UR30 | Implemented | Participant/speaker labels wording explicitly states spoken names remain. |
| UR31 | Implemented | Responsive storage categories; separate backups/deleted tabs; native app footprint excludes unrelated legacy build folders. |
| UR32 | Implemented | Separate Retention tab, forever default and explicit erase confirmation. |
| UR33 | Improved | DPI-aware issue dialog adds microphone, meeting and typing routes; unavailable recovery hidden. Error-specific retained-data reporting remains partial. |
| UR34 | Improved; pointer acceptance pending | Compact icon-only hover/edge design and first-use hint retained. Real pointer travel/mixed-DPI acceptance pending. |
| UR35 | Implemented; desktop acceptance pending | Toolbar status exposed in preferences; explicit hide/snooze state respected, reduced-motion fade stopped. |
| UR36 | Partial; accessibility acceptance pending | Native controls, names, keyboard actions and high-contrast handling retained/improved. Narrator and full desktop focus route not verified. |
| UR37 | Improved; install acceptance pending | Distinct installed-success/Open app page plus first-use validation. New installer built, not run over the installed app in this session. |
| UR38 | Intentional scope | Windows x64 / English / local speech. No unsupported mobile, macOS or cloud-sync promise. |

## Verification and evidence

- 79 existing application/installer/export/release/completion checks passed; four live checks deliberately excluded.
- Five new UX/formatting checks passed: PDF-only dialogs, original-word preservation, Cancel preference isolation, writing presets and invalid-style validation.
- Isolated UX evidence: `output/v070-ux-tests/test_redesigned_dialogs_preser0/` (export, conversation, devices, dictionary, speaker labels, storage and About). Export screenshot visually inspected; earlier Light/Dark and dialog previews also inspected during implementation.
- Native engine concurrency, silence/noise filtering, backup/import safety and export structure tests passed in the existing suite. These do not prove human speech accuracy.
- Final source rebuilt after the final Practice label and issue-dialog adjustments. The combined final regression rerun passed: 84 passed, 4 deselected in 18.53 seconds. git diff --check passed.
- This environment denies real WASAPI microphone/call capture, keyboard injection and synthetic speech. Browser/terminal insertion, actual Zoom/Meet calls, audible playback, real pointer/mixed-DPI behavior and Narrator require desktop acceptance. No consented speech corpus was available.

## Delivery

Installer: `dist/Notewll-Setup-0.7.0.exe` (Windows x64 bootstrapper). Initial installation needs internet to obtain the pinned runtime and small English model. Existing notes/preferences are preserved using compatible internal paths. The installer is unsigned because a real certificate was not provided. Keep a backup of important notes before any upgrade.

SHA-256: `C261A1FC832BCDDA472231C48DEED828DF6192C0444F0B1A4D62E4822054227C`

Source changes are primarily in `NotesExportDialog.cs`, `NotesReview.cs`, `NotesStorageDialog.cs`, `NotesPane.cs`, `Personalization.cs`, `WritingFormat.cs`, `Hub.cs`, `HubExtras.cs`, `FloatingDock.cs`, `ThemeColors.cs`, `AppTabs.cs` and installer/scripts. Regression coverage is in `tests/test_ux_fixes.py` and `src/UxVerification.cs`.

Before treating this as a public stable release: complete the desktop checks above, supply/sign with a real publisher certificate, test the signed updater against the actual repository, and measure consented English dictation/meeting speech against reference text. No new numerical Wispr comparison rating is justified until those tests run.
