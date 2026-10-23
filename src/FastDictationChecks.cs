using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Web.Script.Serialization;

namespace LocalDictation
{
    static class FastDictationChecks
    {
        public static int Run(string[] args)
        {
            if (args[0] == "--live-grammar-smoke")
            {
                using (var started = new ManualResetEvent(false))
                using (var release = new ManualResetEvent(false))
                {
                    int calls = 0;
                    var grammar = new LiveGrammar(raw => { Interlocked.Increment(ref calls); started.Set(); release.WaitOne(); return "I have a meeting."; });
                    if (grammar.Resolve("", 0) != "" || calls != 0) throw new Exception("Silence started grammar work.");
                    grammar.Offer("i has a meeting");
                    if (!started.WaitOne(2000)) throw new Exception("Background grammar did not start.");
                    var clock = Stopwatch.StartNew();
                    try
                    {
                        if (grammar.Resolve("i has a meeting", 100) != "i has a meeting" || clock.ElapsedMilliseconds > 500) throw new Exception("Slow grammar blocked release.");
                        if (grammar.Resolve("i has a meeting tomorrow", 0) != "i has a meeting tomorrow" || calls != 1) throw new Exception("Stale correction lost words or queued another request.");
                    }
                    finally { release.Set(); }
                    if (grammar.Resolve("i has a meeting", 2000) != "I have a meeting.") throw new Exception("Completed correction was not reused.");
                    if (grammar.Resolve("i has a meeting", 0) != "I have a meeting." || calls != 1) throw new Exception("Cached correction ran twice.");
                    using (var tailStarted = new ManualResetEvent(false))
                    using (var tailRelease = new ManualResetEvent(false))
                    {
                        var prefix = new LiveGrammar(raw => { if (raw != "i has a meeting") { tailStarted.Set(); tailRelease.WaitOne(); } return "I have a meeting."; });
                        if (prefix.Resolve("i has a meeting", 2000) != "I have a meeting.") throw new Exception("Prefix correction was not ready.");
                        try
                        {
                            if (prefix.Resolve("i has a meeting tomorrow", 0) != "I have a meeting tomorrow") throw new Exception("Corrected prefix was lost as speech grew.");
                            if (!tailStarted.WaitOne(2000)) throw new Exception("Tail correction did not start.");
                            if (prefix.Resolve("i has a meetingful day", 0) != "i has a meetingful day" || prefix.Resolve("different words", 0) != "different words") throw new Exception("An unrelated cached correction was applied.");
                        }
                        finally { tailRelease.Set(); }
                    }
                    var failed = new LiveGrammar(raw => { throw new Exception("Engine unavailable"); });
                    if (failed.Resolve("keep my words", 2000) != "keep my words") throw new Exception("Failed grammar lost words.");
                    Console.Write("live-grammar-ok|bounded-release|exact-cache|no-stale-edit|single-request|silence-empty|failure-fallback");
                    return 0;
                }
            }
            SpeechEngine.DiagnosticRoot(args[1]);
            var config = Config.Load();
            bool useGrammar = Array.IndexOf(args, "--grammar") >= 0;
            if (!useGrammar) config.QualityEnabled = false;
            string problem = SpeechEngine.EnsureRunning(config, 10);
            if (problem != null) throw new Exception(problem);
            if (useGrammar) SpeechQuality.TryRefine(config, "Hello.");
            var live = new LiveGrammar(raw => SpeechQuality.TryRefine(config, raw));
            byte[] pcm = SpeechEngine.PcmFromWav(File.ReadAllBytes(args[2]));
            var elapsed = Stopwatch.StartNew();
            using (var recognition = new LiveRecognition())
            {
                int offset = 0;
                while (offset + 8000 < pcm.Length)
                {
                    offset += 8000;
                    int pause = (int)(offset / 32L - elapsed.ElapsedMilliseconds);
                    if (pause > 0) Thread.Sleep(pause);
                    var snapshot = new byte[offset]; Buffer.BlockCopy(pcm, 0, snapshot, 0, offset);
                    live.Offer(recognition.Snapshot(snapshot, Peak(snapshot), false).Trim());
                }
                int tailPause = (int)(pcm.Length / 32L - elapsed.ElapsedMilliseconds);
                if (tailPause > 0) Thread.Sleep(tailPause);
                var release = Stopwatch.StartNew();
                string raw = recognition.Snapshot(pcm, Peak(pcm), true).Trim();
                long recognitionMs = release.ElapsedMilliseconds;
                string refined = live.Resolve(raw, 600);
                string text = WritingFormat.Compose(config, refined);
                Console.Write(new JavaScriptSerializer().Serialize(new { AudioMs = pcm.Length / 32, ReleaseToTextMs = release.ElapsedMilliseconds, FinalRecognitionMs = recognitionMs, Raw = raw, Text = text, ModelCorrectionUsed = raw != refined, GrammarEnabled = useGrammar, Scope = "Real-time replay of a WAV, not a physical microphone/shortcut timing." }));
            }
            SpeechQuality.Shutdown(); SpeechEngine.Shutdown(); return 0;
        }
        static int Peak(byte[] pcm) { int peak = 0; for (int i = 0; i + 1 < pcm.Length; i += 2) peak = Math.Max(peak, Math.Abs((int)BitConverter.ToInt16(pcm, i))); return peak; }
    }
}
