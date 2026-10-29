from PIL import Image, ImageDraw, ImageFont, ImageFilter

W, H = 1200, 630
BG = (6, 7, 10)
GRID = (20, 24, 33)
TEXT = (240, 243, 250)
MUTED = (138, 138, 144)
ACCENT = (157, 180, 255)
EDGE = (58, 67, 88)

img = Image.new("RGB", (W, H), BG)
draw = ImageDraw.Draw(img)

for x in range(0, W, 72):
    draw.line([(x, 0), (x, H)], fill=GRID)
for y in range(0, H, 72):
    draw.line([(0, y), (W, y)], fill=GRID)

glow = Image.new("RGB", (W, H), (0, 0, 0))
g = ImageDraw.Draw(glow)
g.ellipse([(760, -180), (1320, 320)], fill=(40, 55, 110))
g.ellipse([(-120, 380), (320, 760)], fill=(35, 28, 70))
glow = glow.filter(ImageFilter.GaussianBlur(110))
img = Image.blend(img, Image.blend(img, glow, 0.55), 0.9)
draw = ImageDraw.Draw(img)

for x in range(0, W, 72):
    draw.line([(x, 0), (x, H)], fill=GRID)
for y in range(0, H, 72):
    draw.line([(0, y), (W, y)], fill=GRID)


def font(name, size):
    return ImageFont.truetype("C:/Windows/Fonts/" + name, size)


draw.text((80, 150), "Fenestra", font=font("segoeuib.ttf", 130), fill=TEXT)
draw.text((84, 330), "A window, not a browser.", font=font("segoeui.ttf", 46), fill=ACCENT)
draw.text((84, 470), "Windows  ·  Linux  ·  macOS", font=font("segoeui.ttf", 28), fill=MUTED)

# window frame
draw.rounded_rectangle([(760, 150), (1120, 470)], radius=16, outline=EDGE, width=2)
draw.rounded_rectangle([(776, 166), (1104, 206)], radius=8, outline=EDGE, width=1)
for i, color in enumerate([(90, 40, 45), (90, 80, 40), (40, 80, 55)]):
    draw.ellipse([(790 + i * 22, 180), (802 + i * 22, 192)], fill=color)
draw.text((900, 176), "127.0.0.1", font=font("consola.ttf", 22), fill=(140, 160, 200))
for i in range(3):
    draw.rounded_rectangle(
        [(790, 230 + i * 70), (1090, 286 + i * 70)], radius=8, outline=EDGE, width=1
    )

img.save("docs/og.png", "PNG")
print("docs/og.png", img.size)
