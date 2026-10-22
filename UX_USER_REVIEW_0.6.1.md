# User-experience review: Local Dictation / Dotflow 0.6.1

Reviewed 2026-10-21. Overall provisional product/UX rating: **6/10**.

The app has a useful local-first foundation and a broad feature set. It still feels like a collection of developer-facing tools in several secondary workflows. My biggest reservations as a prospective daily user are whether dictation consistently produces ready-to-use text, whether meeting capture is trustworthy, and whether exported conversations meet the promised read-only requirement. Adding more controls would not resolve those concerns.

## What this review actually covered

The installed executable is version 0.6.1.0 and matches the local build byte for byte:

```text
SHA-256: 7B22C19053DBE631CCCD17C36373B5F14ED75B3F5D8B830EAE7A11CE800D93B2
```

Both Local Dictation and Wispr Flow processes were running, but this managed session could not enumerate their desktop window controls. Therefore this is **not a complete hands-on walkthrough of the running applications or a speech-accuracy comparison**.

I used the installed app's own preview mode for Home, Notes, Playground, Help and all six Preferences categories in Light/Dark. A temporary inspection harness loaded the unchanged installed assembly and opened its actual dialog implementations against isolated synthetic notes: setup, dictionary, metadata editing, speaker labels, export, storage and issues; it also opened Conversation and Local review tabs. The toolbar's app-owned preview exercised action callbacks, focus preservation and edge placement. I inspected the corresponding source for operations that could not safely be exercised here.

No production app code, real note contents, user preferences, downloads, deletion or publication was changed for this review. In particular, I did not open the real Devices dialog through the harness because its close path saves preferences. The harness and fixtures are under `output/user-review-2026-10-21/`.

There are 44 generated PNGs, including repeat and DPI-aware captures. Native window capture sometimes omits child text or branding; I have not counted those omissions as confirmed product bugs. The storage harness's application-size number describes its host directory and must not be treated as the installed app's actual footprint. Secondary-dialog layout findings below use the harness built with the app's DPI-awareness manifest.

Wispr comparison uses current official product/help documentation, not fabricated observations of its installed UI. Vendor claims describe supported behavior, not measured superiority on my test audio.

## Rating and comparison

These are subjective design/product scores, not a weighted benchmark or percentage of feature parity.

| Area | Your app | Assessment |
|---|---:|---|
| Main-window visual design | 6/10 | Consistent base colors and obvious navigation; oversized hierarchy, empty space and heavy tabs dilute polish. |
| Discoverability and navigation | 6/10 | Home actions and grouped settings help. Important review, backup and speaker controls still require exploration. |
| Dictation workflow design | 6/10 | Good shortcut, live-write, recovery and terminal choices. Context-aware formatting and correction learning are limited. |
| Meeting review workflow | 5.5/10 | Strong capture/source controls and immutable original words; plain transcript/player and lexical review need refinement. |
| Export experience against the user's strict requirement | 4/10 | Scope choices exist, but CSV is editable and document restrictions cannot guarantee an immutable distributed copy. Preview presentation also needs work. |
| Local control and data ownership design | 8/10 | Offline recognition, explicit recording, export, backup and deleted-item recovery are valuable. This is not a security certification. |
| Onboarding and help | 5/10 | Mostly instructions rather than a verified first success; contextual help is difficult to find. |
| Distribution/update experience | 4.5/10 | Installation now succeeds, but this installed binary is unsigned and consumer update setup exposes publisher details. |
| Actual accuracy, real-call reliability, latency and accessibility | Not rated | No permitted complete live comparison in this session. |

I would consider it for private English voice notes and offline use. I would not yet assume it can replace my primary dictation/meeting tool for important work without the acceptance tests below.

Wispr's documented advantage is reducing manual work after speaking: contextual formatting, learned spellings, styles, polished meeting summaries and cross-meeting answers. Its desktop dictation inserts the completed result after stopping; your live-writing approach is a meaningful alternative and should not be removed just to imitate it. [Wispr features](https://wisprflow.ai/features), [desktop behavior](https://docs.wisprflow.ai/articles/2772472373-what-is-flow), [context awareness](https://docs.wisprflow.ai/articles/4678293671-Context-Awareness), [Notetaker](https://wisprflow.ai/notetaker).

Your offline design is a meaningful differentiator: Wispr documents cloud processing and an internet requirement for dictation. That does not establish that your app is universally more secure, or that every user prefers local-only storage. [Wispr privacy overview](https://docs.wisprflow.ai/articles/3467817258-security-and-compliance-faq).

## Findings across the application

Evidence: **Rendered** = inspected an actual app-owned screen/dialog; **Source** = verified implementation path; **Pending** = requires normal desktop/user testing. P1 = address before positioning this as a polished daily replacement; P2 = next usability iteration; P3 = optional expansion.

| ID | Priority | Area / user reaction | Recommended change | Evidence |
|---|---|---|---|---|
| UR01 | P1 | Export: “You promised read-only, but CSV opens as an editable spreadsheet.” | Remove CSV from an immutable-conversation export flow. Treat editable data exchange as a separate capability only if the product allows it. | Source; explicit user requirement. |
| UR02 | P1 | Export: Word/Excel locks and PDF distribution do not ensure a recipient cannot change a copy. | Preserve an immutable app original. Make view-oriented PDF the default distribution format; explain external limits. Use signed snapshots if alteration detection is required, without promising copying/editing prevention. | Source. |
| UR03 | P1 | Export preview looks like raw data: Markdown markers, raw timestamp and joined lines. | Render headings, readable dates and paragraphs. Normalize Windows line endings. Show a preview appropriate to the chosen format, with empty sections omitted. | Rendered, `dialogs-dpi-aware/export.png`. |
| UR04 | P1 | Export dialog at the reviewed DPI truncates the scope label and clips its explanatory text. | Use content-sized responsive rows; keep scope, format, participant option and notice fully visible at 100/150/175/200%. | Rendered, DPI-aware export. |
| UR05 | P1 | App settings look simple until About asks for an update manifest URL and certificate thumbprint. | Publisher config belongs in release/build configuration or a collapsed administrator section. A normal user should see version, update status and one action. | Rendered and Source. |
| UR06 | P1 | Installation/signing: a publicly distributed unsigned executable imposes avoidable trust friction. | Obtain a real signing certificate, sign app and installer, verify public assets and exercise the signed updater. Publishing source alone does not solve this. | Installed signature reported NotSigned. |
| UR07 | P1 | Core writing: basic cleanup does not make ordinary raw text ready to send. | Prioritize names, punctuation, dates, numbered lists and technical vocabulary; retain a verbatim choice. Evaluate a better optional local model only against measured gains. | Direct installed cleanup probes; no acoustic inference. |
| UR08 | P1 | Smart formatting: “Why does the same output style appear in a chat, email and terminal?” | Offer explicit local formatting presets and safe application defaults. Keep command/code input conservative. Any context reading should be transparent and opt-in. | Source; Wispr documented comparison. |
| UR09 | P1 | Meeting trust: an aggregate “recording” state is insufficient reassurance. | Put actual mic/output names, per-source activity and capture status into a compact preflight; distinguish silence, disconnected device and recognition delay. | Rendered and Source; live capture Pending. |
| UR10 | P1 | Source selection: reviewing a Both-source meeting still presents “Start note / My microphone” prominently. | Separate saved-record review from a New recording action. Put capture configuration in an explicit recording panel rather than next to every saved transcript. | Rendered Notes. |
| UR11 | P1 | Conversation is a large plain text area; playback controls need horizontal scrolling in the inspected workspace. | Use readable timestamp/source blocks, larger play controls and a responsive player. Show only audio tracks that exist. Keep original words read-only. | Rendered Conversation; Source. |
| UR12 | P1 | Local review says “Find an answer” but performs keyword matching. | Label it “Find related statements” and show query-match/source context. If semantic summaries/Q&A are added, separate them from original words and make model/download/privacy costs explicit. | Rendered and Source, `NotesSummary.Answer`. |
| UR13 | P1 | Speaker correction uses a dense grid of recognizer sections. | Offer a small speaker list and assign selected transcript blocks to a speaker, with preview. Retain source provenance and no automatic diarization claim. | Rendered speaker-label dialog. |
| UR14 | P1 | Preferences: closing Devices saves selections even without an explicit Save/Cancel choice. | Use Apply/Cancel consistently; closing with X should have a clear, consistent result. | Source: `ShowDevices` saves after `ShowDialog`. Not executed against user config. |
| UR15 | P1 | Toolbar preference can be overridden: note toggle calls `dock.Show()` even if toolbar visibility was disabled. | Respect permanent hide/snooze on every entry path; use the system tray/status for feedback when hidden. | Source: `DictationApp.ToggleNotes`; desktop reproduction Pending. |
| UR16 | P1 | Small-model quality is unproven for quiet speech, names, overlap and long meetings. | Test a consented reference corpus against both apps; compare correction effort, not only transcript word errors. Do not infer accuracy from silence tests. | Pending. |
| UR17 | P2 | Home spends most of its space on large cards and repeated privacy/setup copy. | Reduce header/card heights and show useful recent work sooner. Preserve the three clear capture actions. | Rendered Home. |
| UR18 | P2 | Home displays recovery and issue actions prominently even in a clean session. | Show these only when relevant or as a compact status link; prioritize recent documents and readiness. | Rendered and Source. |
| UR19 | P2 | App identity is generic: the application is Local Dictation while the chosen repository identity is Dotflow. | Decide on one public product identity, then apply it consistently across installer, app, executable metadata, shortcuts and help. A repository nickname alone is not a finished brand. | Rendered; conversation context. |
| UR20 | P2 | Preferences and Notes combine rounded cards with strong native tab borders, flat fields and broad scrollbars. | Refine borders, selection, focus and spacing into one system while preserving native keyboard/accessibility behavior. A web-framework rewrite is not required. | Rendered Light/Dark screens. |
| UR21 | P2 | Dark theme still has conspicuous pale dropdown/tab chrome in captured screens. | Check actual Windows theme combinations, dropdown lists and secondary dialogs. Use consistent native or custom treatment and verify high-contrast readability. | Rendered; live accessibility Pending. |
| UR22 | P2 | Dictionary/snippets feel like editing a database table. | Provide separate word/snippet views, Add entry, examples, row delete and search. Keep JSON import/export under an overflow menu. | Rendered dictionary. |
| UR23 | P2 | Dictionary only applies after recognition and requires deliberate editing. | Add an explicit “Remember this correction” workflow. If the recognizer supports vocabulary biasing, evaluate it separately; do not imply post-replacement improves acoustic recognition. | Source. |
| UR24 | P2 | First-run setup is a list of tasks and links. | Guide one successful recording and one insertion, then mark setup complete. Make failures actionable without requiring the user to understand engine internals. | Rendered setup; actual capture Pending. |
| UR25 | P2 | Playground exposes “Open test WAV” beside normal recording controls and references four-second tests. | Keep advanced file testing secondary. Use a user-controlled Record/Stop test and explain progress in ordinary language. Consider renaming the page Practice. | Rendered Playground. |
| UR26 | P2 | Help needs substantial scrolling; contextual actions and meeting guidance are below the first screen. | Lead with “Cannot type”, “No microphone”, “No meeting audio”, “Recover text” and “Export”. Add links from the corresponding error surfaces. | Rendered and Source. |
| UR27 | P2 | Help says the indicator moves, although the separate voice animation is disabled for fresh configs. | Match guidance to the actual chosen settings and show the edge controller's current state. | Source and Rendered. |
| UR28 | P2 | Note rows can squeeze title/date/duration/status into a narrow column. | Give each a reliable hierarchy and tooltips; use concise status badges and responsive duration placement. Preserve resizing/search/sort already present. | Rendered and Source. |
| UR29 | P2 | Speaker labels, resume, original audio and deletion live together under More. | Organize overflow into Review / Recording / Manage, and make the next likely action visible for the selected note state. | Source; related dialogs Rendered. |
| UR30 | P2 | Participant export checkbox could be mistaken for name redaction. | Say “Include participant labels”; explicitly state spoken names remain unless a separate redaction operation exists. | Rendered export; Source scope logic. |
| UR31 | P2 | Storage is a large mostly empty list and a cluster of operations; the DPI-aware header clips several storage details. | Show size categories as responsive rows, a real empty state, then separate Backups and Recently deleted. Keep destructive actions away from ordinary restore/backup controls. | Rendered storage. Host-dependent size values excluded from findings. |
| UR32 | P2 | Retention is a dropdown plus Apply inside the same crowded storage footer. | Give retention its own clearly labelled setting with the next cleanup consequence visible. Preserve default retention and explicit erase confirmation. | Rendered and Source. |
| UR33 | P2 | Issue/recovery dialogs are useful, but mostly present text and generic actions. | Show one affected action, whether audio/text was retained, and the relevant next step. Distinguish a meeting-device error from a dictation insertion error. | Rendered issues; Source recovery. |
| UR34 | P2 | Toolbar is intentionally tiny, but a user must discover hover and drag affordances. | Preserve the requested icon-only design. Provide one short first-use hint and test pointer travel, collapse delay and side orientation on real monitors. | Toolbar preview callback/focus/edge checks passed; real pointer/DPI Pending. |
| UR35 | P2 | Permanent hiding, one-hour snooze and optional voice animation create several independent states. | Make current state legible in Preferences/system tray and ensure all recording paths honor it. Avoid adding more floating surfaces. | Source. |
| UR36 | P2 | Keyboard shortcuts, playback/find and overflow actions need a coherent accessible route. | Test complete keyboard use, focus order, accessible names, Narrator and high contrast. Verify returning to the target app after menu use. | Source; desktop acceptance Pending. |
| UR37 | P2 | A successful install is not yet a successful first dictation. | Keep the installed-success/Open app page, then run a short first-use validation. Show engine download/repair state without a terminal. | Installer implementation inspected; guided success Pending. |
| UR38 | P3 | Cross-device and multilingual gaps exist, but the stated product is English-only/offline Windows. | Treat these as deliberate scope choices, not urgent missing features. Prove daily English Windows use before expanding platforms or accounts. | Product scope; official Wispr device/language docs. |

## Concrete writing observations

I invoked the installed executable's cleanup operation on supplied text, not on recorded speech:

| Supplied text | Actual cleaned result |
|---|---|
| `hello alex please send the report by friday` | `Hello alex please send the report by friday.` |
| `one apples two bananas three oranges` | `One apples two bananas three oranges.` |

This demonstrates the current cleanup stage's limits: these inputs did not gain proper-name/day capitalization, internal punctuation or list layout. It does **not** demonstrate that Vosk misheard them or how Wispr would transcribe the same recording. Improving this stage can increase usefulness without changing the original meeting transcript.

## Strict read-only export requirement

The user's clarification during this review was: exported documents should be read-only and not editable.

That requirement needs a precise product contract:

1. **Immutable original conversation inside the app:** preserve original speech text, restrict the viewer, and keep annotations/speaker labels separately editable. This is the right foundation and much of it exists.
2. **View-oriented distribution:** PDF is a better default than spreadsheet/Word exchange. CSV cannot enforce non-editability; Word/Excel locks are removable. A read-only file attribute is also removable and should not be advertised as protection.
3. **Detecting alterations:** a signed snapshot can make changes detectable, provided signature verification is implemented and explained. A checksum alone is not authenticated proof, and signing does not prevent someone editing a copy or taking a screenshot.

Do not promise that recipients cannot ever modify exported files. If literal prevention is required, provide a controlled read-only viewer/access model and acknowledge that users can still copy visible content. Do not silently remove every requested export format without agreeing on how that changes the product: the current CSV/Word/Excel feature promise conflicts with absolute non-editability.

## Coverage ledger

| Surface/workflow | Coverage this review | Still needed |
|---|---|---|
| Installed version/provenance | Version and SHA-256 checked against build | None for binary identity |
| Home/actions/recent work | Actual installed preview + source | Real target-app transition from Dictate |
| Notes library/filter/sort/search | Rendered + source paths | Large real library and long-title testing |
| Written note/edit details/tags | Actual isolated dialog + source | Human typing/autosave/reopen round trip |
| Conversation/find/playback controls | Actual isolated tab + source | Real audible playback, seek/source behavior |
| Local review/questions | Actual isolated tab + lexical implementation inspected | User comprehension; substantive meeting evaluation |
| Speaker correction | Actual isolated grid + source | Repeated source chunks and long meeting ergonomics |
| Mic/output/both, pause/resume, device switch | Source reviewed; prior diagnostic limits retained | Normal-desktop call capture |
| Devices | Source only; deliberately not opened because close saves config | Actual endpoint names/meters and cancel behavior |
| Export preview/scopes/formats | Actual isolated dialog + implementation | Export/open documents in Office/PDF viewers and protection acceptance |
| Recently deleted/restore/permanent deletion/retention | Actual isolated storage dialog + source; no real deletes | Safe fixture round trips in installed interactive app |
| Backup/restore/cache cleanup | Dialog/source reviewed; operations not executed on real data | Background/progress/large backup UX |
| Recording/Writing/Notes/App/Storage/About | Both themes rendered; source inspected | Actual keyboard/microphone and live OS theme behavior |
| Dictionary/snippets | Actual isolated dialog + source | Editing/import validation discoverability and live correction flow |
| First-use guide/Help | Actual dialogs/pages + source | First successful real dictation |
| Issues and recovery | Actual issues dialog + recovery source | Real interrupted insertion and microphone-error recovery |
| Toolbar/drag/edge/no-focus | Installed app-owned callback/focus/edge probe | Real pointer, monitor changes, taskbar layouts |
| Startup/close/exit/update/signing | Source and installed signature checked | Startup after sign-in, exit during real capture, signed update end-to-end |
| Wispr comparison | Current official documentation | Direct matched-audio and interaction study |

## What I would keep

- Offline English speech after setup, without an account or extra runtime app.
- The four-dot identity and small hover-only edge controller.
- Immediate text with refinement on release, provided focus/range safety holds up in real apps.
- Clear mic/computer/both capture choice and separate original audio tracks.
- Read-only original conversation with separate annotations and speaker metadata.
- Recovery, recently deleted and portable backups.

## Recommended work order and acceptance criteria

1. **Resolve the read-only contract and export presentation.** A first-time user must understand what is protected, what can be edited and what leaves the app. No clipped choices or raw Markdown preview at tested DPI sizes.
2. **Validate daily writing and meeting trust.** Test at least 30 consented English clips covering names, numbers, technical terms, self-corrections, soft speech, noise and overlap; run a 30-minute both-source meeting. Compare both apps on identical audio where technically supported, logging manual correction count, first-result/finish latency and any lost or duplicated text.
3. **Run real destination tests.** Browser fields/contenteditable, Word/Notepad, VS Code, Windows Terminal and classic console; exercise cursor movement, cancellation, failed paste and existing clipboard preservation. Verify text surrounding the dictated range is unchanged and no command is submitted.
4. **Unify secondary workflows.** Responsive dialogs/player, simple dictionary forms, contextual help, and publisher-config-free updates. Honor toolbar hide/snooze consistently and give device changes explicit apply/cancel semantics.
5. **Finish distribution and accessibility.** Real certificate signing, signed-update round trip, keyboard/Narrator/high contrast and mixed-DPI monitor review. Only then expand platform/AI scope.

A higher rating should come from observed reductions in corrections and confusion, not from adding more checkboxes or copying Wispr's visual branding.

## Evidence links

- [Home](output/user-review-2026-10-21/screens/Light-Home.png)
- [Notes in dark theme](output/user-review-2026-10-21/screens/Dark-Notes.png)
- [Recording preferences](output/user-review-2026-10-21/screens/Light-Settings-Recording.png)
- [About/update setup](output/user-review-2026-10-21/screens/Light-Settings-About.png)
- [DPI-aware export dialog](output/user-review-2026-10-21/dialogs-dpi-aware/export.png)
- [DPI-aware storage dialog](output/user-review-2026-10-21/dialogs-dpi-aware/storage.png)
- [Dictionary](output/user-review-2026-10-21/dialogs/dictionary.png)
- [Speaker-label editor](output/user-review-2026-10-21/dialogs/speaker-labels.png)
- [Current implementation backlog](BACKLOG_STATUS.md)

Official comparison sources: [Features](https://wisprflow.ai/features), [What is Flow?](https://docs.wisprflow.ai/articles/2772472373-what-is-flow), [Context awareness](https://docs.wisprflow.ai/articles/4678293671-Context-Awareness), [Notetaker](https://wisprflow.ai/notetaker), [Privacy overview](https://docs.wisprflow.ai/articles/3467817258-security-and-compliance-faq). Retrieved for this review; these are vendor descriptions, not independent benchmarks.
