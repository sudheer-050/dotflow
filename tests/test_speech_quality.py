import importlib.util
import json
import subprocess
import sys
from types import SimpleNamespace
from pathlib import Path
import pytest

ROOT = Path(__file__).parents[1]


def test_personalization_keeps_snippets_out_of_recognition():
    result = subprocess.run([str(ROOT / 'build/LocalDictation.exe'), '--personalization-smoke'], capture_output=True, text=True)
    assert result.returncode == 0, result.stderr
    assert result.stdout == 'personalization-ok'


def test_vocabulary_prompt_is_bounded_and_accepts_only_saved_terms():
    spec = importlib.util.spec_from_file_location('quality_worker', ROOT / 'engine/quality_worker.py')
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    assert module.vocabulary_prompt(None) is None
    assert module.vocabulary_prompt(['Sudheer', 'sudheer', '', None, 'bad\nentry']) == 'Vocabulary: Sudheer.'
    assert len(module.vocabulary_prompt(['x' * 79 + str(i) for i in range(10)])) <= 493


def test_recognition_uses_current_vocabulary_on_every_request(monkeypatch):
    import base64
    np = pytest.importorskip('numpy', reason='Optional enhanced speech dependency is not installed')
    spec = importlib.util.spec_from_file_location('quality_worker', ROOT / 'engine/quality_worker.py')
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    calls = []

    class Model:
        is_multilingual = False
        def transcribe(self, audio, **options):
            calls.append(options)
            return {'text': 'A test sentence.', 'segments': []}

    tokenizer = SimpleNamespace(encode=lambda text: list(text), decode=lambda tokens: ''.join(tokens))
    monkeypatch.setitem(sys.modules, 'torch', SimpleNamespace(set_num_threads=lambda count: None))
    monkeypatch.setitem(sys.modules, 'whisper', SimpleNamespace(tokenizer=SimpleNamespace(get_tokenizer=lambda *a, **kw: tokenizer)))
    monkeypatch.setitem(sys.modules, 'resemblyzer', SimpleNamespace(preprocess_wav=lambda audio, **kw: audio, VoiceEncoder=lambda **kw: object()))
    worker = module.Worker()
    worker.model = Model()
    pcm = base64.b64encode(np.full(16000, 1000, dtype='<i2').tobytes()).decode()
    for term in ['Sudheer', 'Notewll']:
        worker.run({'pcm': pcm, 'vocabulary': [term]})
    assert calls[0]['initial_prompt'] == 'Vocabulary: Sudheer.'
    assert calls[1]['initial_prompt'] == 'Vocabulary: Notewll.'
    assert calls[1]['carry_initial_prompt'] is True
    assert calls[0]['word_timestamps'] is False
    worker.run({'pcm': pcm, 'voices': True})
    assert calls[2]['word_timestamps'] is True


def test_voice_identity_and_late_name_linking(tmp_path):
    result = subprocess.run([str(ROOT / 'build/LocalDictation.exe'), '--voice-smoke', str(tmp_path)], capture_output=True, text=True)
    assert result.returncode == 0, result.stderr
    assert 'voices-ok' in result.stdout


def test_worker_rejects_changed_numeric_facts(monkeypatch):
    spec = importlib.util.spec_from_file_location('quality_worker', ROOT / 'engine/quality_worker.py')
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)

    class Response:
        def __enter__(self): return self
        def __exit__(self, *args): pass
        def read(self): return json.dumps({'response': json.dumps({'text': 'I paid 50 dollars.'})}).encode()

    class Opener:
        def open(self, request, timeout): return Response()

    monkeypatch.setattr(module, 'ensure_grammar', lambda: None)
    monkeypatch.setattr(module.urllib.request, 'build_opener', lambda *args: Opener())
    assert module.refine('I paid 15 dollars.') == 'I paid 15 dollars.'


def test_worker_rejects_grammar_that_invents_extra_clauses(monkeypatch):
    spec = importlib.util.spec_from_file_location('quality_worker', ROOT / 'engine/quality_worker.py')
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)

    class Response:
        def __enter__(self): return self
        def __exit__(self, *args): pass
        def read(self): return json.dumps({'response': json.dumps({'text': 'I have a meeting tomorrow with the team to discuss the entire project roadmap.'})}).encode()

    class Opener:
        def open(self, request, timeout): return Response()

    monkeypatch.setattr(module, 'ensure_grammar', lambda: None)
    monkeypatch.setattr(module.urllib.request, 'build_opener', lambda *args: Opener())
    assert module.refine('i has a meeting tomorrow') == 'i has a meeting tomorrow'
