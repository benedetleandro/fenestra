from PIL import Image, ImageDraw

S = 1024
img = Image.new("RGBA", (S, S), (0, 0, 0, 0))
d = ImageDraw.Draw(img)

# fundo
d.rounded_rectangle((28, 28, S - 28, S - 28), radius=232, fill=(17, 18, 22, 255))
d.rounded_rectangle((28, 28, S - 28, S - 28), radius=232, outline=(42, 44, 52, 255), width=10)

silver = (214, 219, 229, 255)
soft = (150, 156, 170, 255)

# janela
d.rounded_rectangle((286, 312, 738, 712), radius=58, outline=silver, width=32)
d.line((286, 418, 738, 418), fill=silver, width=32)
d.line((512, 418, 512, 712), fill=silver, width=32)
d.ellipse((330, 356, 372, 398), fill=soft)
d.ellipse((396, 356, 438, 398), fill=soft)

img.save(r"C:\Users\deben\Documents\NavegadorLocalHost\macos\icon.png")
print("icon.png", img.size)
