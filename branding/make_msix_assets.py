# Generates MSIX tile/logo assets (msix/Assets) and Microsoft Store listing images (branding/store).
from PIL import Image, ImageDraw, ImageFont
import os
here = os.path.dirname(os.path.abspath(__file__)); root = os.path.dirname(here)
out = os.path.join(root, "msix", "Assets"); store = os.path.join(here, "store")
os.makedirs(out, exist_ok=True); os.makedirs(store, exist_ok=True)
master = Image.open(os.path.join(here, "logo-1024.png")).convert("RGBA")
pop = "/usr/share/fonts/truetype/google-fonts/Poppins-Bold.ttf"
pop_l = "/usr/share/fonts/truetype/google-fonts/Poppins-Light.ttf"
BG = (11, 13, 20, 255)

def logo(size): return master.resize((size, size), Image.LANCZOS)
def save(img, name, folder=out): img.save(os.path.join(folder, name))

# Square logos
for name, s in [("StoreLogo.png", 50), ("Square44x44Logo.png", 44), ("Square150x150Logo.png", 150),
                ("SmallTile.png", 71), ("LargeTile.png", 310)]:
    save(logo(s), name)
for t in (16, 24, 32, 48, 256):
    save(logo(t), f"Square44x44Logo.targetsize-{t}.png")
    save(logo(t), f"Square44x44Logo.targetsize-{t}_altform-unplated.png")

# Wide tile 310x150
w = Image.new("RGBA", (310, 150), BG)
l = logo(104); w.paste(l, (22, 23), l)
d = ImageDraw.Draw(w)
d.text((140, 44), "Tavrynexo", font=ImageFont.truetype(pop, 28), fill=(255, 255, 255))
d.text((142, 82), "PLAYER", font=ImageFont.truetype(pop_l, 15), fill=(34, 211, 238))
save(w, "Wide310x150Logo.png")

# Store listing images
save(logo(1080), "store-logo-1080x1080.png", store)
save(logo(300), "store-logo-300x300.png", store)
poster = Image.new("RGBA", (720, 1080), BG)
p = logo(520); poster.paste(p, (100, 190), p)
d = ImageDraw.Draw(poster)
d.text((360, 800), "Tavrynexo", font=ImageFont.truetype(pop, 84), fill=(255, 255, 255), anchor="mm")
d.text((360, 880), "P L A Y E R", font=ImageFont.truetype(pop_l, 36), fill=(34, 211, 238), anchor="mm")
save(poster.convert("RGB"), "store-poster-720x1080.png", store)
hero = Image.open(os.path.join(here, "social-preview.png")).convert("RGB").resize((1920, 960), Image.LANCZOS)
canvas = Image.new("RGB", (1920, 1080), (11, 13, 20)); canvas.paste(hero, (0, 60))
save(canvas, "store-hero-1920x1080.png", store)
print(sorted(os.listdir(out)), sorted(os.listdir(store)))
