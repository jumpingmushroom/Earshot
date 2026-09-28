#!/usr/bin/env bash
# Assemble a Thunderstore-ready zip in dist/.
#
# Thunderstore requires, at the ZIP ROOT (not nested in a folder):
#   manifest.json   name matching ^[a-zA-Z0-9_]+$, semver version_number,
#                   description <= 250 chars, dependencies as "Namespace-Name-Version"
#   README.md       rendered as the package page
#   icon.png        exactly 256x256
#   CHANGELOG.md    optional, rendered as the changelog tab
# A version can never be re-uploaded, so version_number must be bumped each release.
set -euo pipefail

# The SDK on the build box has no ICU; without this dotnet aborts before parsing arguments.
export DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export PATH="$HOME/.dotnet:$HOME/.dotnet/tools:$PATH"
export DOTNET_ROOT="${DOTNET_ROOT:-$HOME/.dotnet}"

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
STAGE="$ROOT/dist/stage"
PROJ="$ROOT/src/Earshot/Earshot.csproj"
DLL="$ROOT/src/Earshot/bin/Release/net472/Earshot.dll"

echo "==> building"
dotnet build "$PROJ" -c Release --nologo -v minimal

VERSION=$(python3 -c "import json;print(json.load(open('$ROOT/thunderstore/manifest.json'))['version_number'])")
ASM_VERSION=$(grep -oP '(?<=PluginVersion = ")[^"]+' "$ROOT/src/Earshot/Plugin.cs")

if [ "$VERSION" != "$ASM_VERSION" ]; then
    echo "version mismatch: manifest.json says $VERSION, Plugin.cs says $ASM_VERSION" >&2
    exit 1
fi

echo "==> validating manifest"
python3 - "$ROOT" <<'PY'
import json, re, sys, os
root = sys.argv[1]
m = json.load(open(os.path.join(root, "thunderstore/manifest.json")))
problems = []
if not re.fullmatch(r"[a-zA-Z0-9_]+", m.get("name", "")):
    problems.append("name must match ^[a-zA-Z0-9_]+$")
if not re.fullmatch(r"\d+\.\d+\.\d+", m.get("version_number", "")):
    problems.append("version_number must be x.y.z")
if len(m.get("description", "")) > 250:
    problems.append("description exceeds 250 characters")
for dep in m.get("dependencies", []):
    if not re.fullmatch(r"[^-]+-[^-]+-\d+\.\d+\.\d+", dep):
        problems.append(f"dependency not Namespace-Name-Version: {dep}")
icon = os.path.join(root, "thunderstore/icon.png")
with open(icon, "rb") as f:
    head = f.read(24)
assert head[:8] == b"\x89PNG\r\n\x1a\n", "icon.png is not a PNG"
import struct
w, h = struct.unpack(">II", head[16:24])
if (w, h) != (256, 256):
    problems.append(f"icon.png must be exactly 256x256, got {w}x{h}")
if problems:
    print("\n".join("  - " + p for p in problems)); sys.exit(1)
print(f"  ok: {m['name']} {m['version_number']}, {len(m['dependencies'])} dependencies")
PY

echo "==> staging"
rm -rf "$STAGE"
mkdir -p "$STAGE/plugins/Earshot"
cp "$DLL"                        "$STAGE/plugins/Earshot/"
cp "$ROOT/thunderstore/manifest.json" "$STAGE/"
cp "$ROOT/thunderstore/README.md"     "$STAGE/"
cp "$ROOT/thunderstore/icon.png"      "$STAGE/"
cp "$ROOT/CHANGELOG.md"               "$STAGE/"
cp "$ROOT/LICENSE"                    "$STAGE/"

OUT="$ROOT/dist/Earshot-$VERSION.zip"
rm -f "$OUT"
( cd "$STAGE" && zip -qr "$OUT" . )

echo "==> $OUT"
unzip -l "$OUT"
