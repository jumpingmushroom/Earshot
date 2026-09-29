"""Thunderstore icon: 256x256 PNG. The same family look as the sibling mods (dark plate, golden
border), showing three caption lines, each with an arrowhead pointing a different way: an orange
threat line, a light-blue wildlife line and an off-white world line, matching the mod's category
colours. Written without PIL, which the build box lacks.
Run from the repo root: python3 build/make_icon.py"""
import math
import struct
import zlib

S = 256
SS = 3                       # supersample
W = S * SS

px = bytearray(W * W * 4)

def blend(x, y, r, g, b, a):
    if x < 0 or y < 0 or x >= W or y >= W:
        return
    i = (y * W + x) * 4
    ia = 1.0 - a
    px[i] = int(r * a + px[i] * ia)
    px[i + 1] = int(g * a + px[i + 1] * ia)
    px[i + 2] = int(b * a + px[i + 2] * ia)
    px[i + 3] = int(min(255, 255 * a + px[i + 3] * ia))

def rounded_rect(x0, y0, x1, y1, rad, col, alpha=1.0):
    for y in range(int(y0), int(y1)):
        for x in range(int(x0), int(x1)):
            dx = max(x0 + rad - x, 0, x - (x1 - 1 - rad))
            dy = max(y0 + rad - y, 0, y - (y1 - 1 - rad))
            if dx * dx + dy * dy <= rad * rad:
                blend(x, y, *col, alpha)

def polygon(points, col, alpha=1.0):
    """Even-odd fill of a polygon given in supersampled pixel coordinates (y down)."""
    ys = [p[1] for p in points]
    for y in range(int(min(ys)), int(max(ys)) + 1):
        for x in range(int(min(p[0] for p in points)), int(max(p[0] for p in points)) + 1):
            inside = False
            j = len(points) - 1
            for i in range(len(points)):
                xi, yi = points[i]
                xj, yj = points[j]
                if (yi > y) != (yj > y) and x < (xj - xi) * (y - yi) / (yj - yi) + xi:
                    inside = not inside
                j = i
            if inside:
                blend(x, y, *col, alpha)

def arrowhead(cx, cy, size, degrees, col):
    """The HUD's arrow shape (tip, right, notch, left), rotated clockwise by `degrees` (0 = up)."""
    shape = [(0.0, -1.0), (0.8, 0.85), (0.0, 0.4), (-0.8, 0.85)]
    t = math.radians(degrees)
    pts = []
    for sx, sy in shape:
        x = sx * math.cos(t) - sy * math.sin(t)
        y = sx * math.sin(t) + sy * math.cos(t)
        pts.append((cx + x * size, cy + y * size))
    polygon(pts, col)

PLATE = (0x1c, 0x1a, 0x17)
BORDER = (0xc8, 0xa0, 0x50)
BACKING = (0x00, 0x00, 0x00)
ENEMY = (0xff, 0x8a, 0x3d)
WILDLIFE = (0x7e, 0xc8, 0xff)
WORLD = (0xe8, 0xe4, 0xda)

border_w = 10 * SS
rounded_rect(0, 0, W, W, 34 * SS, BORDER)
rounded_rect(border_w, border_w, W - border_w, W - border_w, 26 * SS, PLATE)

# caption backing plate, like the HUD's
rounded_rect(28 * SS, 52 * SS, W - 28 * SS, W - 52 * SS, 14 * SS, BACKING, 0.55)

# three caption lines: arrowhead + "text" bar of varying length
lines = [(ENEMY, -45, 0.92), (WILDLIFE, 90, 0.70), (WORLD, 180, 0.50)]
line_h = 40 * SS
top = 72 * SS
x_arrow = 58 * SS
x_text0 = 84 * SS
x_text_max = W - 44 * SS
for i, (col, deg, frac) in enumerate(lines):
    cy = top + i * line_h + line_h // 2 - 8 * SS
    arrowhead(x_arrow, cy, 13 * SS, deg, col)
    bar_h = 14 * SS
    rounded_rect(x_text0, cy - bar_h // 2, x_text0 + (x_text_max - x_text0) * frac, cy + bar_h // 2, bar_h // 2, col)

# downsample
out = bytearray()
for y in range(S):
    row = bytearray([0])
    for x in range(S):
        r = g = b = a = 0
        for sy in range(SS):
            for sx in range(SS):
                i = ((y * SS + sy) * W + (x * SS + sx)) * 4
                r += px[i]; g += px[i + 1]; b += px[i + 2]; a += px[i + 3]
        n = SS * SS
        row += bytes((r // n, g // n, b // n, a // n))
    out += row

def chunk(tag, data):
    c = tag + data
    return struct.pack(">I", len(data)) + c + struct.pack(">I", zlib.crc32(c) & 0xffffffff)

png = b"\x89PNG\r\n\x1a\n"
png += chunk(b"IHDR", struct.pack(">IIBBBBB", S, S, 8, 6, 0, 0, 0))
png += chunk(b"IDAT", zlib.compress(bytes(out), 9))
png += chunk(b"IEND", b"")
open("thunderstore/icon.png", "wb").write(png)
print("wrote thunderstore/icon.png", S, "x", S, len(png), "bytes")
