# Generates all Tavrynexo branding assets (logo, ICO, installer images).
from PIL import Image, ImageDraw, ImageFilter, ImageFont
import numpy as np, os
S = 4096
K = S / 1024.0
def k(v): return int(round(v * K))

def gradient(size, c1, c2, angle_deg=135, mid=(124, 92, 255)):
    w, h = size
    yy, xx = np.mgrid[0:h, 0:w].astype(np.float32)
    a = np.deg2rad(angle_deg)
    d = (xx - w/2) * np.cos(a) + (yy - h/2) * np.sin(a)
    d = (d - d.min()) / (d.max() - d.min())
    d = d[..., None]
    c1 = np.array(c1, np.float32); c2 = np.array(c2, np.float32); mid = np.array(mid, np.float32)
    out = np.where(d < 0.5, c1 + (mid - c1) * (d / 0.5), mid + (c2 - mid) * ((d - 0.5) / 0.5))
    return Image.fromarray(out.astype(np.uint8), "RGB").convert("RGBA")

def rounded_poly(draw, pts, r, fill):
    draw.polygon(pts, fill=fill)
    draw.line(pts + [pts[0]], fill=fill, width=2*r, joint="curve")
    for x, y in pts:
        draw.ellipse([x-r, y-r, x+r, y+r], fill=fill)

def build_mark(size_px=1024, with_bg=True):
    img = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    mask = Image.new("L", (S, S), 0)
    if with_bg:
        bg = gradient((S, S), (110, 72, 255), (24, 190, 235), 50, mid=(98, 92, 255))
        ImageDraw.Draw(mask).rounded_rectangle([k(56), k(56), k(968), k(968)], radius=k(224), fill=255)
        img.paste(bg, (0, 0), mask)
        glow = Image.new("RGBA", (S, S), (0, 0, 0, 0))
        ImageDraw.Draw(glow).ellipse([k(-200), k(-420), k(800), k(380)], fill=(255, 255, 255, 34))
        glow = glow.filter(ImageFilter.GaussianBlur(k(120)))
        gm = Image.new("RGBA", (S, S), (0, 0, 0, 0)); gm.paste(glow, (0, 0), mask)
        img = Image.alpha_composite(img, gm)
        stroke = Image.new("RGBA", (S, S), (0, 0, 0, 0))
        ImageDraw.Draw(stroke).rounded_rectangle([k(60), k(60), k(964), k(964)], radius=k(220), outline=(255, 255, 255, 40), width=k(5))
        img = Image.alpha_composite(img, stroke)
    g = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    d = ImageDraw.Draw(g)
    ox = -14
    d.rounded_rectangle([k(262+ox), k(284), k(742+ox), k(396)], radius=k(56), fill=(255, 255, 255, 255))
    d.rounded_rectangle([k(446+ox), k(284), k(558+ox), k(750)], radius=k(56), fill=(255, 255, 255, 255))
    r = k(26)
    tri = [(k(612+ox)+r, k(488)+2*r), (k(612+ox)+r, k(738)-2*r), (k(790+ox)-2*r, k(613))]
    rounded_poly(d, tri, r, (255, 255, 255, 235))
    if with_bg:
        sh = Image.new("RGBA", (S, S), (0, 0, 0, 0))
        sh.paste((20, 10, 70, 110), (0, k(18)), g.split()[3])
        sh = sh.filter(ImageFilter.GaussianBlur(k(22)))
        img = Image.alpha_composite(img, sh)
    img = Image.alpha_composite(img, g)
    return img.resize((size_px, size_px), Image.LANCZOS)

here = os.path.dirname(os.path.abspath(__file__))
root = os.path.dirname(here)
assets = os.path.join(root, "src", "TavrynexoPlayer", "Assets")
os.makedirs(assets, exist_ok=True)

master = build_mark(1024)
master.save(os.path.join(here, "logo-1024.png"))
master.resize((512, 512), Image.LANCZOS).save(os.path.join(assets, "logo.png"))
master.resize((64, 64), Image.LANCZOS).save(os.path.join(assets, "logo-small.png"))
sizes = [16, 20, 24, 32, 40, 48, 64, 128, 256]
imgs = [build_mark(s) if s <= 48 else master.resize((s, s), Image.LANCZOS) for s in sizes]
for p in (os.path.join(assets, "app.ico"), os.path.join(root, "installer", "setup.ico")):
    imgs[-1].save(p, format="ICO", sizes=[(s, s) for s in sizes], append_images=imgs[:-1])
build_mark(512, with_bg=False).save(os.path.join(assets, "mark-white.png"))

pop = "/usr/share/fonts/truetype/google-fonts/Poppins-Bold.ttf"
pop_l = "/usr/share/fonts/truetype/google-fonts/Poppins-Light.ttf"
def font(n, s): return ImageFont.truetype(n, s)

lock = Image.new("RGBA", (1600, 480), (11, 13, 20, 255))
m320 = master.resize((320, 320), Image.LANCZOS)
lock.paste(m320, (80, 80), m320)
dr = ImageDraw.Draw(lock)
dr.text((450, 120), "Tavrynexo", font=font(pop, 150), fill=(255, 255, 255))
dr.text((456, 300), "P L A Y E R", font=font(pop_l, 64), fill=(34, 211, 238))
lock.save(os.path.join(here, "lockup-dark.png"))

sp = gradient((1280, 640), (11, 13, 20), (28, 20, 70), 135, mid=(18, 16, 44)).convert("RGBA")
m = master.resize((300, 300), Image.LANCZOS); sp.paste(m, (130, 170), m)
d2 = ImageDraw.Draw(sp)
d2.text((480, 205), "Tavrynexo", font=font(pop, 118), fill=(255, 255, 255))
d2.text((486, 345), "Modern media player for Windows 10 & 11", font=font(pop_l, 36), fill=(180, 190, 220))
sp.convert("RGB").save(os.path.join(here, "social-preview.png"))

big = gradient((328, 628), (14, 16, 30), (40, 22, 110), 100, mid=(24, 18, 64)).convert("RGB")
mm = master.resize((200, 200), Image.LANCZOS)
big.paste(mm, (64, 120), mm)
bd = ImageDraw.Draw(big)
bd.text((164, 360), "Tavrynexo", font=font(pop, 44), fill=(255, 255, 255), anchor="mm")
bd.text((164, 408), "PLAYER", font=font(pop_l, 26), fill=(34, 211, 238), anchor="mm")
big.resize((164, 314), Image.LANCZOS).save(os.path.join(root, "installer", "wizard-large.bmp"))
small = gradient((110, 110), (14, 16, 30), (40, 22, 110), 120, mid=(24, 18, 64)).convert("RGB")
sm = master.resize((88, 88), Image.LANCZOS); small.paste(sm, (11, 11), sm)
small.resize((55, 55), Image.LANCZOS).save(os.path.join(root, "installer", "wizard-small.bmp"))
print("ok")
