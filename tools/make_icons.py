from PIL import Image, ImageDraw

S = 1024
img = Image.new("RGBA", (S, S), (0, 0, 0, 0))
d = ImageDraw.Draw(img)

# fundo: quadrado arredondado escuro
d.rounded_rectangle((28, 28, S - 28, S - 28), radius=232, fill=(17, 18, 22, 255))
d.rounded_rectangle((28, 28, S - 28, S - 28), radius=232, outline=(42, 44, 52, 255), width=10)

silver = (214, 219, 229, 255)
soft = (150, 156, 170, 255)

# janela: moldura
d.rounded_rectangle((286, 312, 738, 712), radius=58, outline=silver, width=32)
# barra superior
d.line((286, 418, 738, 418), fill=silver, width=32)
# divisoria vertical
d.line((512, 418, 512, 712), fill=silver, width=32)
# ponto da barra
d.ellipse((330, 356, 372, 398), fill=soft)
d.ellipse((396, 356, 438, 398), fill=soft)

img = img.resize((256, 256), Image.LANCZOS)
img.save("assets/fenestra.ico", sizes=[(256, 256), (128, 128), (64, 64), (48, 48), (32, 32), (16, 16)])
print("fenestra.ico ok")

cal = Image.open(r"C:\Users\deben\Documents\Calendario\dist\icons\icon-192.png").convert("RGBA")
cal = cal.resize((256, 256), Image.LANCZOS)
cal.save("assets/calendario.ico", sizes=[(256, 256), (128, 128), (64, 64), (48, 48), (32, 32), (16, 16)])
print("calendario.ico ok")
