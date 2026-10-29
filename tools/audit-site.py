from lxml import html as LH
import os
import re
import sys

root = sys.argv[1]
doc_path = os.path.join(root, "docs", "index.html")
doc = LH.parse(doc_path).getroot()

print("== ARQUIVOS EM docs/ ==")
for name in sorted(os.listdir(os.path.join(root, "docs"))):
    p = os.path.join(root, "docs", name)
    if os.path.isfile(p):
        print(f"  {name}  {os.path.getsize(p)} bytes")

print("\n== HEAD ==")
head = doc.find("head")
for el in head:
    tag = el.tag if isinstance(el.tag, str) else "?"
    if tag == "meta":
        print(f"  meta {el.get('name') or el.get('property') or el.get('charset')} = {el.get('content') or ''}")
    elif tag == "link":
        print(f"  link rel={el.get('rel')} href={el.get('href')}")
    elif tag == "title":
        print(f"  title = {el.text}")

print("\n== ESTRUTURA ==")
for el in doc.iter():
    if not isinstance(el.tag, str):
        continue
    if el.tag == "section":
        h2 = el.find(".//h2")
        print(f"  section#{el.get('id')}  h2={h2.text.strip() if h2 is not None and h2.text else ''}")
    if el.tag in ("h1", "h2", "h3"):
        txt = " ".join(el.itertext()).strip().split("\n")[0][:70]
        if el.get("data-i18n") or el.get("data-i18n-html"):
            txt = "[" + (el.get("data-i18n") or el.get("data-i18n-html")) + "]"
        print(f"    {el.tag}: {txt}")

print("\n== LINKS ==")
ids = {el.get("id") for el in doc.iter() if isinstance(el.tag, str) and el.get("id")}
internal, external, anchors = 0, 0, []
for a in doc.iter("a"):
    href = a.get("href") or ""
    if href.startswith("#"):
        internal += 1
        anchors.append(href)
        if href[1:] not in ids:
            print(f"  BROKEN ANCHOR {href}")
    elif href.startswith("http"):
        external += 1
        print(f"  ext {href}")
    elif href:
        internal += 1
        print(f"  local {href}")
print(f"  anchors={len(anchors)} internal={internal} external={external}")
print(f"  anchors unicos: {sorted(set(anchors))}")

print("\n== I18N ==")
keys = set()
for el in doc.iter():
    if not isinstance(el.tag, str):
        continue
    for attr in ("data-i18n", "data-i18n-html", "data-i18n-aria"):
        if el.get(attr):
            keys.add(el.get(attr))
print(f"  chaves usadas no HTML: {len(keys)}")

js = open(os.path.join(root, "docs", "app.js"), encoding="utf-8").read()
pt = js[js.index("var PT"):js.index("var META")]
pt_keys = set(re.findall(r'"([a-z0-9]+(?:\.[a-zA-Z0-9]+)+)":', pt))
print(f"  chaves PT: {len(pt_keys)}  faltando: {sorted(keys - pt_keys)}  orfas: {sorted(pt_keys - keys)}")

print("\n== ARQUIVOS AUSENTES (SEO/infra) ==")
for name in ("robots.txt", "sitemap.xml", "404.html", "og.png", "og-image.png", "manifest.webmanifest"):
    print(f"  {name}: {'sim' if os.path.exists(os.path.join(root, 'docs', name)) else 'NAO'}")

print("\n== PESOS ==")
for name in ("index.html", "styles.css", "app.js"):
    p = os.path.join(root, "docs", name)
    print(f"  {name}: {os.path.getsize(p)} bytes")
