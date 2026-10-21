"""Measure local STT on an explicit WAV/reference corpus; no network or third-party packages."""
import argparse
import json
import math
import random
import re
import struct
import subprocess
import wave
from pathlib import Path


def words(text):
    return re.findall(r"\w+(?:'\w+)?", text.lower())


def word_errors(reference, hypothesis):
    expected, actual = words(reference), words(hypothesis)
    row = list(range(len(actual) + 1))
    for index, token in enumerate(expected, 1):
        next_row = [index]
        for column, candidate in enumerate(actual, 1):
            next_row.append(min(row[column] + 1, next_row[-1] + 1, row[column - 1] + (token != candidate)))
        row = next_row
    return row[-1], len(expected)


def save_pcm(path, pcm):
    with wave.open(str(path), 'wb') as output:
        output.setparams((1, 2, 16000, 0, 'NONE', 'not compressed'))
        output.writeframes(pcm)


def add_noise_fixtures(root, corpus):
    rng = random.Random(42)
    for kind in ('silence', 'hum', 'clicks', 'quiet-noise'):
        pcm = bytearray()
        for frame in range(64000):
            value = 0 if kind == 'silence' else int(500 * math.sin(2 * math.pi * 60 * frame / 16000)) if kind == 'hum' else (10000 if frame % 16000 < 8 else 0) if kind == 'clicks' else rng.randint(-50, 50)
            pcm.extend(struct.pack('<h', value))
        name = kind + '.wav'
        save_pcm(root / name, pcm)
        corpus.append({'id': kind, 'file': name, 'reference': '', 'kind': kind, 'synthetic': True})
    speech = next((row for row in corpus if row['kind'] == 'basic'), None)
    if speech:
        with wave.open(str(root / speech['file']), 'rb') as source:
            values = struct.unpack('<' + 'h' * source.getnframes(), source.readframes(source.getnframes()))
        name = 'quiet-speech.wav'
        save_pcm(root / name, struct.pack('<' + 'h' * len(values), *(int(v * .08) for v in values)))
        corpus.append(dict(speech, id='quiet-speech', file=name, kind='quiet-speech'))


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('executable', type=Path)
    parser.add_argument('engine', type=Path)
    parser.add_argument('corpus', type=Path)
    parser.add_argument('--output', type=Path, required=True)
    parser.add_argument('--synthetic-variants', action='store_true')
    args = parser.parse_args()
    corpus = json.loads(args.corpus.read_text(encoding='utf-8-sig'))
    if args.synthetic_variants:
        add_noise_fixtures(args.corpus.parent, corpus)
    results = []
    for item in corpus:
        audio = (args.corpus.parent / item['file']).resolve()
        for gate in (False, True):
            command = [str(args.executable.resolve()), '--diagnostic-transcribe', str(args.engine.resolve()), str(audio)]
            if gate:
                command.append('--quiet-gate')
            run = subprocess.run(command, capture_output=True, text=True, timeout=90)
            if run.returncode:
                raise RuntimeError(run.stderr or run.stdout)
            measured = json.loads(run.stdout)
            errors, count = word_errors(item['reference'], measured['Text'])
            row = dict(item, quiet_gate=gate, word_errors=errors, reference_words=count, wer=errors / count if count else None, **measured)
            results.append(row)
            print(f"{item['id']} gate={gate} words={errors}/{count} inference={measured['InferenceMs']}ms", flush=True)
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps({'limitations': 'Synthetic fixtures are not human speech or a Wispr comparison. Processing is faster than real time; first-partial timing is not live capture latency. WER uses lowercase word tokens without numeric equivalence.', 'results': results}, indent=2), encoding='utf-8')


if __name__ == '__main__':
    main()
