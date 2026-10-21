import csv
import zipfile
from xml.etree import ElementTree as ET

from test_app import run


def test_exports_preserve_words_protect_documents_and_delete_recoverably(tmp_path):
    result = run("--note-export-smoke", str(tmp_path))
    assert result.returncode == 0, result.stderr
    assert result.stdout == "exports-ok|four-formats|raw-conversation|quiet-filter|recoverable-delete"
    from pypdf import PdfReader
    tagged = PdfReader(tmp_path / 'conversation.pdf', strict=True)
    catalog = tagged.trailer['/Root']
    assert catalog['/MarkInfo']['/Marked']
    structure = catalog['/StructTreeRoot']
    assert len(structure['/K']) > 0 and '/ParentTree' in structure
    assert tagged.pages[0]['/StructParents'] == 0
    with zipfile.ZipFile(tmp_path / "conversation.xlsx") as archive:
        for path in archive.namelist():
            ET.fromstring(archive.read(path))
        sheet = ET.fromstring(archive.read("xl/worksheets/sheet1.xml"))
        ns = {"s": "http://schemas.openxmlformats.org/spreadsheetml/2006/main"}
        assert sheet.find("s:sheetProtection", ns).attrib["sheet"] == "1"
        values = [e.text for e in sheet.findall(".//s:t", ns)]
        assert "Hello Alex. The budget is 42,000." in values
        assert not any("never replace" in (value or "") for value in values)
    with zipfile.ZipFile(tmp_path / "conversation.docx") as archive:
        for path in archive.namelist():
            ET.fromstring(archive.read(path))
        settings = ET.fromstring(archive.read("word/settings.xml"))
        ns = "{http://schemas.openxmlformats.org/wordprocessingml/2006/main}"
        assert settings.find(ns + "documentProtection").attrib[ns + "edit"] == "readOnly"
        document = archive.read("word/document.xml").decode()
        assert "Hello Alex. The budget is 42,000." in document
        assert "No speech recognized" not in document
    with (tmp_path / "formula.csv").open(encoding="utf-8-sig", newline="") as file:
        assert ["Personal notes", "'=2+3"] in list(csv.reader(file))
    assert not list((tmp_path / "store").glob("*/note.json"))
    assert list((tmp_path / "store" / ".deleted").glob("*/note.json"))

    from pypdf import PdfReader

    pdf = PdfReader(tmp_path / "multipage.pdf", strict=True)
    assert len(pdf.pages) > 1
    text = "\n".join(page.extract_text() for page in pdf.pages)
    assert "Hello Alex." in text and "Jos\u00e9" in text and "\u674e" in text
    assert all(len(page.images) == 1 for page in pdf.pages)
    assert all(tuple(map(float, page.mediabox[2:])) == (595.0, 842.0) for page in pdf.pages)
