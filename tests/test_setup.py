import hashlib
import subprocess
import zipfile
from pathlib import Path

from test_app import ROOT

SETUP = ROOT / "dist" / "Dotflow-Setup-0.7.0.exe"


def test_setup_reports_installed_size_in_rounded_up_kilobytes(tmp_path):
    (tmp_path / "app.exe").write_bytes(b"x" * 1024)
    model = tmp_path / "model"
    model.mkdir()
    (model / "weights").write_bytes(b"x" * 1025)
    result = subprocess.run([str(SETUP), "--installed-size", str(tmp_path)], capture_output=True, text=True, timeout=15)
    assert result.returncode == 0
    assert result.stdout == "3"


def test_setup_contains_the_exact_current_application():
    result = subprocess.run([str(SETUP), "--inspect"], capture_output=True, text=True, timeout=15)
    assert result.returncode == 0
    assert "engine=Vosk-small-English|model-download=once" in result.stdout
    assert result.stdout.endswith(hashlib.sha256((ROOT / "build" / "LocalDictation.exe").read_bytes()).hexdigest())


def test_setup_extracts_only_native_libraries(tmp_path):
    archive = tmp_path / "runtime.zip"
    with zipfile.ZipFile(archive, "w") as zip:
        zip.writestr("vosk-win64/libvosk.dll", b"native test fixture")
        zip.writestr("vosk-win64/unused.lib", b"not installed")
    target = tmp_path / "native"
    result = subprocess.run([str(SETUP), "--test-extract", str(archive), str(target), "dll"], capture_output=True, text=True)
    assert result.returncode == 0
    assert [p.name for p in target.iterdir()] == ["libvosk.dll"]


def test_setup_rejects_archive_path_traversal(tmp_path):
    archive = tmp_path / "model.zip"
    with zipfile.ZipFile(archive, "w") as zip:
        zip.writestr("model/../../outside.txt", b"should never be written")
    result = subprocess.run([str(SETUP), "--test-extract", str(archive), str(tmp_path / "model"), "model"], capture_output=True, text=True)
    assert result.returncode == 2 and "invalid path" in result.stderr
    assert not (tmp_path.parent / "outside.txt").exists()
