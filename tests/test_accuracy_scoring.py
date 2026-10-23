import importlib.util
from pathlib import Path
import sys
import subprocess

import pytest

ROOT = Path(__file__).parents[1]


def scorer():
    scripts = str(ROOT / 'scripts')
    sys.path.insert(0, scripts)
    try:
        spec = importlib.util.spec_from_file_location('score_accuracy', ROOT / 'scripts/score_accuracy.py')
        module = importlib.util.module_from_spec(spec)
        spec.loader.exec_module(module)
        return module
    finally:
        sys.path.remove(scripts)


def test_accuracy_gate_refuses_unreviewed_or_missing_references():
    module = scorer()
    row = {'file': 'one.wav', 'verified_spoken_reference': 'Hello.', 'human_verified': 'false'}
    with pytest.raises(ValueError, match='ground truth'):
        module.score([row], [{'File': 'one.wav', 'Raw': 'Hello.'}])
    row['human_verified'] = 'true'
    with pytest.raises(ValueError, match='Missing prediction'):
        module.score([row], [])


def test_accuracy_gate_counts_errors_and_rejects_tiny_perfect_samples():
    module = scorer()
    references = [{'file': 'one.wav', 'verified_spoken_reference': 'hello world', 'human_verified': 'true'}]
    result = module.score(references, [{'File': 'one.wav', 'Raw': 'Hello word.'}])
    assert result['WordErrors'] == 1 and result['ReferenceWords'] == 2 and result['WER'] == .5
    result = module.score(references, [{'File': 'one.wav', 'Raw': 'Hello world.'}])
    assert result['WER'] == 0 and result['PassedAccuracyGate'] is False


def test_empty_reference_insertions_are_not_hidden_by_zero_denominator():
    module = scorer()
    references = [{'file': 'empty.wav', 'verified_spoken_reference': '', 'human_verified': 'true'}]
    result = module.score(references, [{'File': 'empty.wav', 'Raw': 'Huh.'}])
    assert result['WER'] is None and result['NonemptyOutputsOnEmptyReferences'] == 1
    assert result['PassedAccuracyGate'] is False


def test_publication_stops_before_github_without_verified_accuracy_inputs():
    result = subprocess.run(['powershell', '-NoProfile', '-File', str(ROOT / 'scripts/publish_release.ps1')],
                            capture_output=True, text=True, timeout=15)
    assert result.returncode != 0
    assert 'Verified accuracy references and hypotheses are required before publication' in result.stderr
