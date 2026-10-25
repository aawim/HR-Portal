"""Read-only reference inventory; candidates require review, never automatic deletion.
Routes, controllers, EF models/configurations, migrations and tests are preserved.
Run: python tools/audit_project.py
"""
import json
import os
from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[1]
SKIP = {"bin", "obj", ".git", ".vs", "node_modules", "lib"}


def inventory():
    sources = {}
    for directory, directories, files in os.walk(ROOT):
        directories[:] = [name for name in directories if name not in SKIP]
        for name in files:
            path = Path(directory) / name
            if path.suffix in {".cs", ".razor", ".cshtml", ".csproj", ".js", ".css", ".json"}:
                sources[path.relative_to(ROOT)] = path.read_text(encoding="utf-8-sig", errors="replace")
    return sources


def candidates(sources):
    references = {}
    for source_path, source_text in sources.items():
        for token in set(re.findall(r"\b[A-Za-z_]\w*\b", source_text)):
            references.setdefault(token, set()).add(source_path)
    for path, text in sources.items():
        if path.suffix not in {".cs", ".razor"} or any(part in path.parts for part in
                {"Migrations", "Tests", "Database", "Controllers", "Properties", "Data"}):
            continue
        if "IEntityTypeConfiguration<" in text:
            continue
        if path.stem == "_Imports" or "@page" in text:
            continue
        names = re.findall(r"\b(?:class|interface|record|enum)\s+(\w+)", text) if path.suffix == ".cs" else [path.stem]
        # Extension methods are referenced through their method name, not declaring type.
        extension_methods = re.findall(r"\b(\w+)\s*\(\s*this\s+", text)
        if any(references.get(name, set()) - {path} for name in extension_methods):
            continue
        if names and all(not (references.get(name, set()) - {path}) for name in names):
            yield {"file": path.as_posix(), "types": names}


if __name__ == "__main__":
    sources = inventory()
    print(json.dumps({"source_files": len(sources), "review_candidates": list(candidates(sources))}, indent=2))
