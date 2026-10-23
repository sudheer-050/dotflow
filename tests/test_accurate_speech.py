import base64
import hashlib
import importlib.util
from pathlib import Path
from types import SimpleNamespace

import pytest
import sys
from test_app import run, write_config

ROOT = Path(__file__).parents[1]


def worker_module():
    spec = importlib.util.spec_from_file_location('accurate_worker', ROOT / 'engine/accurate_worker.py')
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


@pytest.fixture(scope='module')
def detector():
    pytest.importorskip('numpy')
    pytest.importorskip('onnxruntime')
    return worker_module().SpeechPresence(ROOT / 'assets/silero_vad.onnx')


@pytest.mark.parametrize('kind', ['silence', 'noise', 'hum', 'clicks'])
def test_neural_gate_rejects_non_speech_controls(detector, kind):
    np = detector.np
    frame = np.arange(80000)
    audio = np.zeros(80000, dtype=np.float32)
    if kind == 'noise':
        audio = np.random.default_rng(20261023).uniform(-2000 / 32768, 2000 / 32768, 80000).astype(np.float32)
    elif kind == 'hum':
        audio = (2000 / 32768 * np.sin(2 * np.pi * 60 * frame / 16000)).astype(np.float32)
    elif kind == 'clicks':
        audio[frame % 8000 < 16] = 2000 / 32768
    assert detector.inspect(audio)['end'] == 0


def test_no_speech_never_calls_recognition_or_reuses_previous_text():
    np = pytest.importorskip('numpy')
    module = worker_module()
    recognizer = module.Recognizer.__new__(module.Recognizer)
    recognizer.np = np
    recognizer.gate = SimpleNamespace(inspect=lambda audio: {'speech_ms': 0, 'start': 0, 'end': 0})
    recognizer.last_audio = b'old'
    recognizer.last_text = 'Do not repeat this previous dictation.'
    def forbidden(*args, **kwargs):
        raise AssertionError('The model must not run for rejected audio')
    recognizer.model = SimpleNamespace(recognize=forbidden)
    result = recognizer.run({'pcm': base64.b64encode(b'\0\0' * 16000).decode()})
    assert result['text'] == '' and result['speech_ms'] == 0


def test_worker_caches_only_identical_detected_audio():
    np = pytest.importorskip('numpy')
    module = worker_module()
    recognizer = module.Recognizer.__new__(module.Recognizer)
    recognizer.np = np
    recognizer.gate = SimpleNamespace(inspect=lambda audio: {'speech_ms': 160, 'start': 0, 'end': len(audio)})
    calls = []
    recognizer.model = SimpleNamespace(recognize=lambda audio, **kwargs: calls.append(len(audio)) or 'Hello.')
    recognizer.last_audio = recognizer.last_text = None
    request = {'pcm': base64.b64encode(b'\1\0' * 16000).decode()}
    assert recognizer.run(request)['cached'] is False
    assert recognizer.run(request)['cached'] is True
    request['pcm'] = base64.b64encode(b'\2\0' * 16000).decode()
    assert recognizer.run(request)['cached'] is False
    assert calls == [16000, 16000]


def test_bundled_speech_detector_is_pinned_to_verified_model():
    module = worker_module()
    assert hashlib.sha256((ROOT / 'assets/silero_vad.onnx').read_bytes()).hexdigest() == module.VAD_SHA256


@pytest.mark.parametrize('preferences,expected', [
    ({'QualityEnabled': True}, 'True|True'),
    ({'QualityEnabled': False}, 'False|False'),
    ({'QualityEnabled': False, 'AccurateDictationEnabled': True}, 'True|False'),
    ({'QualityEnabled': True, 'AccurateDictationEnabled': False}, 'False|True'),
])
def test_accurate_dictation_migrates_legacy_preferences_and_is_independent_of_notes(tmp_path, preferences, expected):
    config = write_config(tmp_path, QualityPython=sys.executable, **preferences)
    result = run('--speech-preferences', config)
    assert result.returncode == 0, result.stderr
    assert result.stdout == expected
