# Notewll 0.8.0 user guide

## Install and start

Run `dist/Notewll-Setup-0.8.0.exe`. First installation downloads the English model/runtime and needs internet; speech later works offline. Exit the old app if requested. Setup reports successful installation separately and offers Open app. Start opens the hub; closing it leaves the app running. Exit in the Windows system tray quits it.

Home offers Dictate, New note, Record meeting and recent work. First-run guidance includes permission and practice steps. Preferences > Recording has microphone testing, Check speech engine and setup repair.

## Write into other applications

Click your target field, hold F8 and speak, then release to refine. Escape cancels. In Recording, use the shortcut recorder to press/release another combination. Function keys support the most direct live writing; held Ctrl/Alt/Shift/Win combinations can delay insertion until release. Windows or other apps may intercept shortcuts.

Avoid moving the cursor or typing during capture. If insertion is interrupted, Recover last dictation shows the final text and warns about partial insertion. Retry processes retained audio without automatically pasting it; Copy explicitly changes the clipboard; Discard clears recovery. Recovery lasts until exit or the next attempt. An ordinary successful paste attempts to restore your previous clipboard unless you copied something newer.

Dictation types directly into the focused text field without inspecting its contents or automatically retrying by paste. Click inside the message field before starting; release shortcut modifiers before text is inserted. With live writing disabled, the finished transcript is typed on release.

Shortcut dictation recognizes speech continuously on this computer. Optional local grammar cleanup runs while you speak, and corrected prefixes are reused as you continue. Release finishes the live recognizer rather than running Whisper over the recording again. Grammar has a 600 ms wait budget on release; when the model is still busy, fast punctuation/filler cleanup finishes the text instead. Slow corrections never change text after the dictation has finished. This favors response time over the recognition accuracy of a second Whisper pass. Notes, meetings, audio-file checks and explicit recovery can still use enhanced recognition. Silence ends without a warning or clipboard change.

Writing preferences select cleanup/verbatim, live writing, terminal paste and local dictionary/snippets. Windows Terminal commonly uses Ctrl+Shift+V; classic consoles use Shift+Insert. No Enter is added. Windows can block insertion into an administrator window. Playground previews cleanup and can transcribe a PCM 16 kHz mono WAV.

## Edge toolbar

Hover the slim bar to reveal icons; hover each icon for its name. Dictate starts writing in the current field; its active control stops/refines and Cancel discards. Personal note/Meeting start recordings; activate again to stop/save. Open app shows the hub.

Drag the bar/grip and release to snap to the nearest edge. Side edges use a vertical bar; top/bottom use a horizontal bar. Position is remembered. App preferences control visibility, separate voice animation, reduced motion, theme and startup. The Windows system tray offers one-hour hide, Show toolbar and a keyboard action menu.

## Notes and meetings

Start without a title/name. Content and recognized introductions fill them; Edit details corrects them later. Manual title/name edits are preserved. Shared output remains labelled as computer audio; there is no voice diarization.

Next recording selects My microphone, Computer audio or Both sources. Devices chooses endpoints; use the output selected in Zoom/Meet. Recorded-source metadata is separate from the next-recording dropdown. Watch both meters when capturing both. Headphones reduce duplicates. Pause preserves elapsed time in the same note; Resume continues. Stop saves audio and finishes recognition. Device errors preserve audio: select a working device and use More > Resume where available.

Preferences > Notes uses one dropdown-selected toggle shortcut, with separate personal/meeting source defaults. Press once to start and again to stop without opening the hub. The toolbar makes shortcuts optional.

Search saved text, names, titles and tags, filter notes/meetings, resize the library, and sort by Newest, Oldest, Title or Longest. You can review another record while recording. My notes holds editable annotations; Conversation preserves recognizer words read-only. Ctrl+F/F3 finds text. Double-click timestamps for source playback with seek/speed. Missing ranges and Retry appear outside spoken text. Silence and engine errors are not inserted into conversations. Quiet-noise suppression cannot reject all noise.

Local review creates a source-quote overview and possible decisions/follow-ups. Type keywords or a question to retrieve matching original statements with timestamps. It does not infer facts or generate AI answers. More > Correct speaker labels edits attribution metadata without changing words. + Written note creates a note without audio.

## Export, storage and recovery

Export previews Conversation only, Personal notes only or Full record. Participant metadata is optional; names spoken inside the conversation remain. Export as a view-oriented PDF; editable Word, Excel and CSV exports are no longer offered. Saved files can be opened immediately. PDF supports text search/copy and contains reading-order structure tags. External PDF copies can still be altered with specialist tools. These are not tamper-proof originals.

More > Delete moves the record/audio to Recently deleted. Preferences > Storage shows app/model, recordings, deleted items and cache. Restore brings records back; permanent deletion needs confirmation. Retention is opt-in, with Forever as default. Backup creates a ZIP of active notes/audio; Restore backup skips existing IDs. Deleted records, preferences and the speech model are excluded.

Uninstall through Windows Installed apps. Data is retained by default. Downloaded-component removal and permanent local-data removal are separate choices; erasing data needs another confirmation. Notes live in `%LOCALAPPDATA%\LocalDictation\Notes`.

## Troubleshooting

- Missing engine: Check speech engine, then rerun full Setup; initial downloads need internet.
- Missing microphone: select the input, check its meter and Windows microphone permission for desktop apps.
- Missing call output: choose the actual device used by the call; capture includes all apps using it.
- Soft speech missing/noise transcribed: try disabling quiet-noise suppression and improve microphone placement. The small English model may misrecognize names.
- Failed dictation: Recover last dictation before exit to retry retained in-memory audio.
- Missing note words: inspect coverage status and Retry saved WAV audio.
- Updates unavailable: no signed public update feed is configured. About shows the version; use a newer trusted full installer when available.

This release is Windows x64. Real microphone, meeting, playback and cross-app operation still need desktop validation. BACKLOG_STATUS.md tracks remaining audit work.


Version 0.7.0 adds explicit Standard, Chat, Email and Code writing styles, a searchable Words & snippets editor, and user-controlled Record/Stop on Practice. Closing recording-device selection without Apply cancels changes. Setup records successful microphone and recognition checks, then asks you to confirm typing into another app. Conversations remain read-only; titles, participant labels and personal notes can be edited separately.
