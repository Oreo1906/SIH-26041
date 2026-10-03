from pathlib import Path
import json
import hashlib
import re
from pypdf import PdfReader

root = Path(__file__).resolve().parents[2]
pdf = root / 'output/pdf/SurakshaXR_Internal_Guide.pdf'
md = root / 'output/pdf/SurakshaXR_Internal_Guide.md'
reader = PdfReader(pdf)
text = md.read_text(encoding='utf-8')
assert len(reader.pages) == 14
assert len(re.findall(r'^# ', text, re.M)) == 14
assert text.count('```mermaid') == 5
assert '{{diagram:' not in text
assert all(len(p.extract_text()) > 1200 for p in reader.pages)
assert '\ufffd' not in '\n'.join(p.extract_text() for p in reader.pages)
assert 'Context.getFilesDir()' in text
assert 'no new certificate' in text
assert 'surakshaxr.db under Application.persistentDataPath' not in text
report = {
    'date': '2026-10-03', 'type': 'documentation-only verification',
    'pdf': str(pdf.relative_to(root)), 'editable_text': str(md.relative_to(root)),
    'pages': 14, 'mermaid_diagrams': 5,
    'pdf_bytes': pdf.stat().st_size,
    'pdf_sha256': hashlib.sha256(pdf.read_bytes()).hexdigest(),
    'markdown_sha256': hashlib.sha256(md.read_bytes()).hexdigest(),
    'content_checks': 'passed',
    'source_review': ['artifacts/documentation/mobile-audit.md', 'artifacts/documentation/stack-audit.md'],
    'visual_review': {
        'all_pages': 'Reviewed at 120 DPI; no clipping, overlap, broken tables or footer collisions.',
        'root': [2,4,5,6,8,11], 'ui_review_agent': [1,2,3,6,7],
        'mobile_review_agent': [9,10,12,13,14],
        'final_changed_pages_rechecked': [2,4,5,6,8,11]
    },
    'application_source_changed': False, 'unity_build_run': False
}
(root / 'artifacts/documentation/guide-verification.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print(json.dumps(report,indent=2))
