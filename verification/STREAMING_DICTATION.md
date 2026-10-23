# Local streaming dictation verification - 2026-10-23

Shortcut dictation feeds Vosk every 250 ms while recording, regardless of whether live insertion is enabled. Local Ollama grammar runs speculatively with one request in flight. Successful corrections can be reused for identical text or an unchanged whole-word prefix. Release finalizes that stream and waits at most 600 ms for grammar. Otherwise the existing punctuation, capitalization, filler removal and personalization finish the text. No late grammar task edits a completed dictation. Notes, meetings, audio-file checks and explicit recovery retain their enhanced recognition paths.

The native engine and grammar service warm at startup. Processing stays local; no cloud service was added. This computer's installed PyTorch is CPU-only. Shortcut dictation skips the slower final Whisper pass, so its recognition accuracy can be lower. Fast fallback is not full model grammar correction, and the wait budget alone cannot guarantee under one second for all recordings or desktop conditions.

## Measured replay

Three existing local voice recordings were resampled from mono PCM16/22050 Hz to 16000 Hz into `output/streaming-fixtures`; originals were preserved. Each six-second WAV was fed in real time, using the installed native model and the local grammar service. The final build produced these times:

| Six-second recording | Final recognition | Release to formatted text |
| --- | ---: | ---: |
| user1_0 | 108 ms | 509 ms |
| user1_10 | 66 ms | 416 ms |
| user1_11 | 139 ms | 148 ms |

Raw results are in `output/streaming-dictation-timings.json`. These are three replay samples, not p95 estimates, accuracy scores, cold-start guarantees or measured physical shortcut-to-keyboard timings. At least one output used a changed model correction; an unchanged output can indicate unchanged grammar, rejection or fallback. A grammar expansion that added a long clause was observed and an expansion guard was added and tested. This is a limited safeguard, not proof that model edits always preserve meaning.

Repeat a replay after building:

```powershell
& .\build\LocalDictation.exe --streaming-dictation-benchmark "$env:LOCALAPPDATA\Programs\LocalDictation" output/streaming-fixtures/user1_10_20260102_220555.wav --grammar | Out-String
```

## Checks and remaining desktop acceptance

- Application and full installer compile successfully.
- 28 selected regression checks passed; two desktop-dependent checks skipped. The focused run covers live grammar, bounded release, cache reuse, preservation of newer words, grammar failure fallback, cleanup, stream state and personalization.
- `verification/quiet-dictation.ps1` passed against real empty-result/cancellation handlers: idle state, hidden overlay and cleared recovery.
- A broader run also encountered a settings-preview expectation failure and two microphone capture failures. Real capture reports `MMSYSERR 1` in this managed session. These checks are not claimed as passed. Physical keyboard injection is unavailable here.
- Working-tree whitespace check passed for edited tracked application files.

The installed executable has not been replaced in this session: its directory rejected writes previously. `Install quiet dictation.cmd` now installs both the updated executable and grammar worker, keeps backups, verifies their hashes and restarts the app. The full setup executable is rebuilt too.

After updating from the ordinary Windows desktop, click an empty text field, hold the configured shortcut, speak briefly and release. Confirm final insertion and overlay dismissal within the target interval, silence without warnings, and that completed text does not change later. Also check a sentence with names/numbers, a held-modifier shortcut, disabled live writing, and a switched target window. One-second release latency and recognition quality remain desktop acceptance requirements.
