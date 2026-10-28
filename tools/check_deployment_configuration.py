"""Check current deployment configuration without printing secret values or reading development secret stores."""
import json
from pathlib import Path
import subprocess

root = Path(__file__).resolve().parents[1]
failures = []


def sensitive_fields(value, path=""):
    if isinstance(value, dict):
        for key, child in value.items():
            label = path + "." + key
            if key.lower().split(":")[-1] in {"clientsecret", "password", "certificatepassword", "apikey", "privatekey", "access_token", "refresh_token"}:
                if isinstance(child, str) and child.strip():
                    yield label
            yield from sensitive_fields(child, label)
    elif isinstance(value, list):
        for child in value:
            yield from sensitive_fields(child, path)


for location in [root, root / "HRM.Api", root / "HRM.UI"]:
    for file in location.glob("appsettings*.json"):
        if file.name.endswith(".local.json"):
            continue
        config = json.loads(file.read_text(encoding="utf-8-sig"))
        for field in sensitive_fields(config):
            failures.append(f"{file.relative_to(root)}: nonempty sensitive field {field}; use the secret provider")
        if file.name == "appsettings.json" and location != root:
            for section, key in [("Application", "PublicOrigin"), ("Backend", "Origin")]:
                if config.get(section, {}).get(key):
                    failures.append(f"{file.relative_to(root)}: {section}:{key} must be supplied per environment")

tracked = subprocess.run(["git", "-c", "safe.directory=" + root.as_posix(), "ls-files"], cwd=root,
                         capture_output=True, text=True, check=True).stdout.splitlines()
for name in tracked:
    file = root / name
    if not file.is_file():
        continue
    if file.suffix.lower() in {".pfx", ".p12", ".key"} or file.name.startswith(".env") and file.name != ".env.example":
        failures.append(f"{name}: credential file must not be tracked")
    elif file.suffix.lower() in {".pem", ".cs", ".json"}:
        text = file.read_text(encoding="utf-8-sig", errors="replace")
        if any(marker in text for marker in ["-----BEGIN PRIVATE KEY-----", "-----BEGIN RSA PRIVATE KEY-----", "-----BEGIN EC PRIVATE KEY-----"]):
            failures.append(f"{name}: private-key content must not be tracked")

if failures:
    raise SystemExit("\n".join(failures))
print("PASS deployment configuration: no current secret values/private keys; deployment origins are environment-specific")
