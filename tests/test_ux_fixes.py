import pytest
from test_app import run, write_config


def test_redesigned_dialogs_preserve_originals_and_cancel_preferences(tmp_path):
    result = run('--ux-smoke', str(tmp_path))
    assert result.returncode == 0, result.stderr
    assert 'originals-preserved' in result.stdout
    assert 'pdf-only' in result.stdout
    assert 'devices-cancel' in result.stdout


@pytest.mark.parametrize('style,raw,expected', [
    ('Code', 'git status', 'git status'),
    ('Chat', 'hello world', 'Hello world'),
    ('Standard', 'one apples two oranges three pears', '1. Apples\n2. Oranges\n3. Pears'),
])
def test_explicit_writing_styles(style, raw, expected):
    result = run('--format-text', style, raw)
    assert result.returncode == 0, result.stderr
    assert result.stdout.replace('\r\n', '\n') == expected


def test_invalid_writing_style_is_rejected(tmp_path):
    assert run('--validate-config', write_config(tmp_path, WritingStyle='unknown')).returncode == 2
