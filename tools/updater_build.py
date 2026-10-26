#!/usr/bin/env python3
"""Publisher tool for L2Updater (docs/format.md): builds and signs files.json and manifest.json.

Needs Python 3.9+ and the `cryptography` package. The private key never leaves this machine.

  updater_build.py keygen   --out keys/release            (makes release.pem + release.spki.b64)
  updater_build.py files    --key K.pem --client ID --version V --root GAMEDIR --out OUT [--user PATH]... [--delete PATH]...
  updater_build.py manifest --key K.pem --config manifest.config.json --out OUT
  updater_build.py verify   --key K.pem FILE.json         (checks FILE.json against FILE.json.sig)
"""
import argparse
import base64
import hashlib
import json
import re
import sys
import time
from pathlib import Path

from cryptography.hazmat.primitives import hashes, serialization
from cryptography.hazmat.primitives.asymmetric import ec
from cryptography.hazmat.primitives.asymmetric.utils import decode_dss_signature, encode_dss_signature

CLIENT_ID = re.compile(r"^[a-z0-9_]{1,32}$")
RESERVED = {"CON", "PRN", "AUX", "NUL", *(f"COM{i}" for i in range(1, 10)), *(f"LPT{i}" for i in range(1, 10))}
SKIP_DIRS = {".l2updater"}


def load_key(path):
    return serialization.load_pem_private_key(Path(path).read_bytes(), password=None)


def spki(key):
    return key.public_key().public_bytes(serialization.Encoding.DER, serialization.PublicFormat.SubjectPublicKeyInfo)


def key_id(key):
    return hashlib.sha256(spki(key)).hexdigest()[:16]


def sign(key, data):
    r, s = decode_dss_signature(key.sign(data, ec.ECDSA(hashes.SHA256())))
    return base64.b64encode(r.to_bytes(32, "big") + s.to_bytes(32, "big")).decode()


def verify(key, data, sig_text):
    raw = base64.b64decode(sig_text.strip())
    if len(raw) != 64:
        return False
    der = encode_dss_signature(int.from_bytes(raw[:32], "big"), int.from_bytes(raw[32:], "big"))
    try:
        key.public_key().verify(der, data, ec.ECDSA(hashes.SHA256()))
        return True
    except Exception:
        return False


def write_signed(key, path, document):
    path.parent.mkdir(parents=True, exist_ok=True)
    data = (json.dumps(document, ensure_ascii=False, indent=2) + "\n").encode()
    path.write_bytes(data)
    Path(str(path) + ".sig").write_text(sign(key, data) + "\n")
    print("wrote", path, "and", str(path) + ".sig")


def check_path(rel):
    if not rel or rel.startswith("/") or "\\" in rel or ":" in rel or len(rel) > 200:
        raise SystemExit(f"bad path: {rel!r}")
    for part in rel.split("/"):
        if part in ("", ".", "..") or part.endswith((" ", ".")):
            raise SystemExit(f"bad path: {rel!r}")
        if part.split(".")[0].upper() in RESERVED:
            raise SystemExit(f"reserved Windows name in path: {rel!r}")
    return rel


def cmd_keygen(a):
    out = Path(a.out)
    out.parent.mkdir(parents=True, exist_ok=True)
    pem = Path(str(out) + ".pem")
    if pem.exists():
        raise SystemExit(f"{pem} already exists, not overwriting")
    key = ec.generate_private_key(ec.SECP256R1())
    pem.write_bytes(key.private_bytes(serialization.Encoding.PEM, serialization.PrivateFormat.TraditionalOpenSSL,
                                      serialization.NoEncryption()))
    pem.chmod(0o600)
    b64 = base64.b64encode(spki(key)).decode()
    Path(str(out) + ".spki.b64").write_text(b64)
    print("private key:", pem, "(keep secret, back it up, never upload)")
    print("key_id:", key_id(key))
    print("public key for AppConfig.TrustedKeys:", b64)


def cmd_files(a):
    if not CLIENT_ID.match(a.client):
        raise SystemExit("client id must match [a-z0-9_]{1,32}")
    key = load_key(a.key)
    root = Path(a.root).resolve()
    users = {check_path(p) for p in a.user}
    entries = []
    for f in sorted(p for p in root.rglob("*") if p.is_file()):
        rel = f.relative_to(root).as_posix()
        if rel.split("/")[0] in SKIP_DIRS:
            continue
        check_path(rel)
        h = hashlib.sha256()
        with f.open("rb") as fh:
            for chunk in iter(lambda: fh.read(1 << 20), b""):
                h.update(chunk)
        entry = {"path": rel, "size": f.stat().st_size, "sha256": h.hexdigest()}
        if rel in users:
            entry["user"] = True
        entries.append(entry)
    missing = users - {e["path"] for e in entries}
    if missing:
        raise SystemExit(f"--user paths not found in the folder: {sorted(missing)}")
    doc = {"format": 1, "key_id": key_id(key), "client": a.client, "version": a.version,
           "files": entries, "delete": [check_path(p) for p in a.delete]}
    write_signed(key, Path(a.out) / "clients" / a.client / "files.json", doc)
    print(f"{len(entries)} files, {sum(e['size'] for e in entries)} bytes")


def cmd_manifest(a):
    key = load_key(a.key)
    cfg = json.loads(Path(a.config).read_text(encoding="utf-8"))
    for c in cfg["clients"]:
        if not CLIENT_ID.match(c["id"]):
            raise SystemExit(f"bad client id {c['id']!r}")
    doc = {"format": 1, "key_id": key_id(key), "serial": int(time.time()),
           "issued_at": time.strftime("%Y-%m-%dT%H:%M:%SZ", time.gmtime())}
    for name in ("updater", "clients", "links", "news_url", "status_url"):
        if name in cfg:
            doc[name] = cfg[name]
    urls = [u for c in cfg["clients"] for u in [c["files"], *c["base_urls"]]]
    urls += [cfg["updater"]["url"]] if "updater" in cfg else []
    for u in urls:
        if not u.startswith("https://"):
            raise SystemExit(f"all URLs must be https: {u}")
    write_signed(key, Path(a.out) / "manifest.json", doc)


def cmd_verify(a):
    key = load_key(a.key)
    data = Path(a.file).read_bytes()
    ok = verify(key, data, Path(str(a.file) + ".sig").read_text())
    print("signature OK" if ok else "SIGNATURE BAD")
    sys.exit(0 if ok else 1)


def main():
    p = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    sub = p.add_subparsers(dest="cmd", required=True)
    s = sub.add_parser("keygen"); s.add_argument("--out", required=True); s.set_defaults(fn=cmd_keygen)
    s = sub.add_parser("files")
    s.add_argument("--key", required=True); s.add_argument("--client", required=True)
    s.add_argument("--version", required=True); s.add_argument("--root", required=True)
    s.add_argument("--out", required=True)
    s.add_argument("--user", action="append", default=[]); s.add_argument("--delete", action="append", default=[])
    s.set_defaults(fn=cmd_files)
    s = sub.add_parser("manifest")
    s.add_argument("--key", required=True); s.add_argument("--config", required=True); s.add_argument("--out", required=True)
    s.set_defaults(fn=cmd_manifest)
    s = sub.add_parser("verify"); s.add_argument("--key", required=True); s.add_argument("file")
    s.set_defaults(fn=cmd_verify)
    a = p.parse_args()
    a.fn(a)


if __name__ == "__main__":
    main()
