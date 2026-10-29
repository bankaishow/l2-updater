#!/usr/bin/env python3
"""Publisher tool for L2Updater (docs/format.md): builds and signs files.json and manifest.json.

Needs Python 3.9+ and the `cryptography` package. The private key never leaves this machine.

  updater_build.py keygen   --out keys/release [--no-password]   (makes release.pem + release.spki.b64; the key is
                                                           encrypted with a password you type)
  updater_build.py protect  --key OLD.pem [--out NEW.pem]  (encrypts an existing unprotected key, same public key)
  updater_build.py files    --key K.pem --client ID --version V --root GAMEDIR --out OUT [--user PATH]... [--delete PATH]...
  updater_build.py manifest --key K.pem --config manifest.config.json --out OUT
  updater_build.py verify   --key K.pem|K.spki.b64 FILE.json   (checks FILE.json against FILE.json.sig;
                                                           the .spki.b64 public key needs no password)
"""
import argparse
import base64
import getpass
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


MIN_PASSWORD = 12


def ask_new_password():
    """Asks twice; the password is never printed or stored."""
    for _ in range(3):
        first = getpass.getpass(f"New key password (at least {MIN_PASSWORD} characters): ")
        if len(first) < MIN_PASSWORD:
            print(f"Too short: at least {MIN_PASSWORD} characters.")
            continue
        if getpass.getpass("Repeat the password: ") != first:
            print("The passwords differ.")
            continue
        return first.encode()
    raise SystemExit("no valid password given")


def encrypted_pem(key, password):
    return key.private_bytes(serialization.Encoding.PEM, serialization.PrivateFormat.PKCS8,
                             serialization.BestAvailableEncryption(password))


def load_key(path):
    """Loads the private key; an encrypted key asks for its password (up to 3 tries)."""
    data = Path(path).read_bytes()
    try:
        return serialization.load_pem_private_key(data, password=None)
    except TypeError:
        pass  # encrypted
    for _ in range(3):
        password = getpass.getpass(f"Password for {path}: ").encode()
        try:
            return serialization.load_pem_private_key(data, password=password)
        except ValueError:
            print("Wrong password.")
    raise SystemExit("wrong password")


def load_verify_key(path):
    """For verify: a .b64 file holds the public key (SubjectPublicKeyInfo DER), anything else is a private key."""
    if str(path).endswith(".b64"):
        return serialization.load_der_public_key(base64.b64decode(Path(path).read_text().strip()))
    return load_key(path)


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
        public = key.public_key() if hasattr(key, "public_key") else key
        public.verify(der, data, ec.ECDSA(hashes.SHA256()))
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
    if a.no_password:
        print("WARNING: the private key is stored WITHOUT a password.")
        pem.write_bytes(key.private_bytes(serialization.Encoding.PEM, serialization.PrivateFormat.TraditionalOpenSSL,
                                          serialization.NoEncryption()))
    else:
        pem.write_bytes(encrypted_pem(key, ask_new_password()))
    pem.chmod(0o600)
    b64 = base64.b64encode(spki(key)).decode()
    Path(str(out) + ".spki.b64").write_text(b64)
    print("private key:", pem, "(keep secret, back it up, never upload)")
    print("key_id:", key_id(key))
    print("public key for AppConfig.TrustedKeys:", b64)


def cmd_protect(a):
    old = Path(a.key)
    new = Path(a.out) if a.out else old.with_name(old.stem + ".enc.pem")
    if new.exists():
        raise SystemExit(f"{new} already exists, not overwriting")
    key = load_key(old)
    password = ask_new_password()
    data = encrypted_pem(key, password)
    # Prove the new file opens with the password and gives the same key before anything else is done with it.
    check = serialization.load_pem_private_key(data, password=password)
    if key_id(check) != key_id(key):
        raise SystemExit("internal error: the encrypted key differs from the original")
    new.write_bytes(data)
    new.chmod(0o600)
    print("encrypted key:", new)
    print("key_id:", key_id(key), "(same as before: AppConfig does not change)")
    print("Now: test it (updater_build.py verify), keep a copy of the new file and the password in two places,")
    print(f"then delete the old unprotected file {old} (and any copies of it).")


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
    key = load_verify_key(a.key)
    data = Path(a.file).read_bytes()
    ok = verify(key, data, Path(str(a.file) + ".sig").read_text())
    print("signature OK" if ok else "SIGNATURE BAD")
    sys.exit(0 if ok else 1)


def main():
    p = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    sub = p.add_subparsers(dest="cmd", required=True)
    s = sub.add_parser("keygen"); s.add_argument("--out", required=True)
    s.add_argument("--no-password", action="store_true"); s.set_defaults(fn=cmd_keygen)
    s = sub.add_parser("protect"); s.add_argument("--key", required=True); s.add_argument("--out")
    s.set_defaults(fn=cmd_protect)
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
