# Manifest and file list format (version 1)

All files are UTF-8 JSON. Every signed document is published as two files: `<name>.json` and `<name>.json.sig`.

## Signature

- Algorithm: ECDSA with curve P-256 and SHA-256 over the **exact bytes** of `<name>.json` (no re-serialization, no whitespace normalization).
- `.sig` contains the signature in IEEE P1363 format (64 bytes: r || s), base64-encoded, one line.
- Public keys are compiled into the updater (`TrustedKeys`), several allowed (key rotation). Each document names its key: field `key_id` (first 16 hex chars of SHA-256 of the public key in SubjectPublicKeyInfo DER).
- A document whose signature does not verify against a trusted key is rejected entirely. Nothing from it is used, not even links or news.

## manifest.json

```json
{
  "format": 1,
  "key_id": "0123456789abcdef",
  "serial": 42,
  "issued_at": "2026-10-22T12:00:00Z",
  "updater": { "version": "1.0.2", "url": "https://cdn.example.com/updater/L2Updater-1.0.2.exe", "sha256": "<64 hex>", "size": 63000000 },
  "clients": [
    {
      "id": "mythras",
      "title": { "ru": "High Five", "en": "High Five" },
      "exe": "system/l2.exe",
      "args": "",
      "files": "https://cdn.example.com/mythras/files.json",
      "base_urls": ["https://cdn.example.com/mythras/", "https://mirror.example.net/mythras/"],
      "servers": ["mythras_x1"]
    }
  ],
  "links": { "site": "https://example.com", "forum": "https://forum.example.com", "cabinet": "https://example.com/cabinet" },
  "news_url": "https://example.com/api/updater/news",
  "status_url": "https://example.com/api/updater/status"
}
```

- `serial` grows with every publication. The updater stores the highest serial it has accepted and **rejects an older one** (rollback protection).
- `id`: `[a-z0-9_]{1,32}`. Unknown fields are ignored (forward compatibility); missing required fields reject the document.
- All URLs must be `https://`.
- `news_url` / `status_url` are **not** signed data, just display data: shown as plain text, never executed, never used to choose files.

## files.json (one per client)

```json
{
  "format": 1,
  "key_id": "0123456789abcdef",
  "client": "mythras",
  "version": "2026.10.22-1",
  "files": [
    { "path": "system/l2.exe", "size": 12345678, "sha256": "<64 hex>" },
    { "path": "system/l2.ini", "size": 2048, "sha256": "<64 hex>", "user": true }
  ],
  "delete": ["system/old_patch.dat"]
}
```

- `client` must equal the `id` that referenced the file list.
- `path`: relative, forward slashes, no `..`, no leading `/`, no drive letter, no `:` (alternate data streams), no reserved Windows names (`CON`, `NUL`, `COM1`…), max 260 chars after joining with the game folder. The joined full path must stay inside the game folder (checked after `Path.GetFullPath`).
- `user: true` — file is downloaded only if missing; existing user copy is never overwritten (settings, macros).
- `delete` — paths (same rules) removed if present.
- Download URL of a file = `base_url + path` (each path segment URL-encoded).

## Update procedure

1. Download `manifest.json` + `.sig`, verify, check `serial`.
2. For the chosen client: download `files.json` + `.sig`, verify, check `client`.
3. For each file: compare size, then SHA-256 of the local file (cache by size + last-write time to skip rehashing).
4. Download missing/changed files into `<game>/.l2updater/tmp/`, verify SHA-256, then move into place (replace). On hash mismatch: try the next mirror; after all mirrors fail, stop with an error, the old file stays untouched.
5. Apply `delete`.
6. Launch `exe` with `args` from the game folder.
