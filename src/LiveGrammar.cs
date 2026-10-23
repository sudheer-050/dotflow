using System;
using System.Threading.Tasks;

namespace LocalDictation
{
    // One speculative correction at a time; release never waits for the model indefinitely.
    sealed class LiveGrammar
    {
        readonly object gate = new object();
        readonly Func<string, string> refine;
        Task<string> pending;
        string pendingRaw, cachedRaw, cachedText;
        public LiveGrammar(Func<string, string> refine) { this.refine = refine; }
        public void Offer(string raw)
        {
            if (String.IsNullOrWhiteSpace(raw)) return;
            lock (gate)
            {
                if (raw == cachedRaw || pending != null && !pending.IsCompleted) return;
                pendingRaw = raw;
                pending = Task.Run(() =>
                {
                    string text;
                    try { text = refine(raw); } catch { return raw; }
                    if (!String.IsNullOrWhiteSpace(text)) lock (gate) { cachedRaw = raw; cachedText = text; }
                    return String.IsNullOrWhiteSpace(text) ? raw : text;
                });
            }
        }
        public string Resolve(string raw, int waitMilliseconds)
        {
            if (String.IsNullOrWhiteSpace(raw)) return "";
            Offer(raw);
            Task<string> task;
            string fallback = raw;
            lock (gate)
            {
                if (raw == cachedRaw) return cachedText;
                // Keep a corrected prefix as speech grows, but only across a whole-word boundary.
                if (!String.IsNullOrEmpty(cachedRaw) && raw.StartsWith(cachedRaw + " ", StringComparison.Ordinal))
                    fallback = cachedText.TrimEnd('.', '!', '?') + raw.Substring(cachedRaw.Length);
                task = pendingRaw == raw ? pending : null;
            }
            return task != null && task.Wait(waitMilliseconds) && task.Result != raw ? task.Result : fallback;
        }
    }
}
