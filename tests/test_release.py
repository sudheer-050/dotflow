import hashlib
import json
import os
import subprocess
import zipfile
from pathlib import Path

import pytest

from test_app import ROOT, run, write_config
from test_setup import SETUP


def test_release_workflows_preserve_data_and_search_content(tmp_path):
    result = run('--release-smoke', str(tmp_path))
    assert result.returncode == 0, result.stderr
    assert result.stdout == 'release-ok|append|backup|restore|search|rules|excerpts|themes|discard'
    restored = next((tmp_path / 'restored').glob('*/note.json'))
    note = json.loads(restored.read_text(encoding='utf-8'))
    assert note['Segments'][0]['RawText'] == 'We should send the report tomorrow.'
    assert (restored.parent / 'microphone.wav').stat().st_size == 64044


def test_backup_restore_rejects_traversal_and_leaves_existing_data(tmp_path):
    archive = tmp_path / 'bad.zip'
    with zipfile.ZipFile(archive, 'w') as file:
        file.writestr('../outside.txt', 'must never be extracted')
    result = run('--restore-backup', str(tmp_path / 'notes'), str(archive))
    assert result.returncode != 0
    assert not (tmp_path / 'outside.txt').exists()
    assert not list((tmp_path / 'notes').glob('.import-*'))


def test_dictionary_rejects_duplicate_triggers(tmp_path):
    result = run('--validate-config', write_config(tmp_path, Personalization=[
        {'Kind': 'Word', 'Trigger': 'sue deer', 'Replacement': 'Sudheer'},
        {'Kind': 'Snippet', 'Trigger': 'SUE DEER', 'Replacement': 'another value'},
    ]))
    assert result.returncode == 2 and 'unique' in result.stderr


def test_update_feed_requires_https(tmp_path):
    result = run('--validate-config', write_config(tmp_path, UpdateManifestUrl='http://example.com/update.json'))
    assert result.returncode == 2 and 'HTTPS' in result.stderr


def test_setup_checks_actual_download_hash(tmp_path):
    data = tmp_path / 'download.zip'
    data.write_bytes(b'test download')
    expected = hashlib.sha256(data.read_bytes()).hexdigest()
    result = subprocess.run([str(SETUP), '--verify-hash', str(data), expected], capture_output=True, text=True)
    assert result.returncode == 0 and result.stdout == 'valid'
    data.write_bytes(b'changed download')
    result = subprocess.run([str(SETUP), '--verify-hash', str(data), expected], capture_output=True, text=True)
    assert result.stdout == 'invalid'


def test_real_native_stream_survives_shutdown():
    installed = Path(os.environ.get('LOCALAPPDATA', '')) / 'Programs/LocalDictation'
    if not (installed / 'native/libvosk.dll').is_file():
        pytest.skip('Installed native engine unavailable')
    result = run('--engine-lifetime-smoke', str(installed))
    assert result.returncode == 0, result.stderr
    assert result.stdout == 'engine-ok|deferred-shutdown|active-stream-retained|released'
