from lxml import html as LH
import sys

sys.stdout.reconfigure(encoding="utf-8")

doc = LH.parse(sys.argv[1]).getroot()

SKIP = {"script", "style", "pre", "code", "svg", "head", "title", "meta", "link"}
found = []
nested = []
attrs = []


def walk(el):
    for child in el:
        if not isinstance(child.tag, str):
            # comentario / PI: ignora o texto e o tail
            continue

        tag = child.tag
        if tag in SKIP:
            continue

        label = tag + (f".{child.get('class')}" if child.get("class") else "")

        if child.get("data-i18n-html"):
            # todo o conteudo e trocado como HTML; nao descer
            continue

        if child.get("data-i18n"):
            if len(child) > 0:
                nested.append((child.get("data-i18n"), label))
            walk(child)
            continue

        if child.text and child.text.strip():
            found.append((child.text.strip(), label))

        walk(child)

        if child.tail and child.tail.strip():
            found.append((child.tail.strip(), label + " (tail)"))


walk(doc)

for name in ("aria-label", "title", "placeholder", "alt"):
    for el in doc.iter():
        if not isinstance(el.tag, str):
            continue
        value = el.get(name)
        if value:
            attrs.append((name, value, el.tag))

print("TEXTO SEM TRADUCAO:")
seen = set()
for text, where in found:
    if text in seen:
        continue
    seen.add(text)
    print(f"  [{where}] {text!r}")

print("\nELEMENTOS data-i18n (textContent) COM FILHOS:")
for key, where in nested:
    print(f"  {key} -> {where}")

print("\nATRIBUTOS COM TEXTO FIXO:")
for name, value, tag in attrs:
    print(f"  <{tag} {name}={value!r}>")
