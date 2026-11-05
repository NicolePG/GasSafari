"""Descarga recursos CC0 de OpenGameArt a una carpeta por recurso y guarda un manifiesto (autor, licencia, URL).
uso: python -I oga_fetch.py <carpeta_destino> slug[:archivo] ..."""
import json, os, sys, urllib.request, zipfile
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from oga_search import info

dest = sys.argv[1]
man_path = os.path.join(dest, "manifest.json")
man = json.load(open(man_path, encoding="utf-8")) if os.path.exists(man_path) else {}
for arg in sys.argv[2:]:
    slug, _, pick = arg.partition(":")
    meta = info(slug)
    if meta["license"] != ["CC0"] and "CC0" not in meta["license"]:
        print("SALTADO (no CC0):", slug, meta["license"]); continue
    files = [f for f in meta["files"] if not pick or f.rsplit("/", 1)[-1] == pick]
    d = os.path.join(dest, slug); os.makedirs(d, exist_ok=True)
    for url in files:
        fn = os.path.join(d, urllib.request.unquote(url.rsplit("/", 1)[-1]))
        if not os.path.exists(fn):
            req = urllib.request.Request(url, headers={"User-Agent": "GasSafari-tools"})
            open(fn, "wb").write(urllib.request.urlopen(req, timeout=120).read())
        if fn.endswith(".zip"):
            zipfile.ZipFile(fn).extractall(d); os.remove(fn)
        print("ok", slug, os.path.basename(fn), meta["author"], meta["license"])
    meta["url"] = "https://opengameart.org/content/" + slug
    man[slug] = meta
json.dump(man, open(man_path, "w", encoding="utf-8"), indent=1, ensure_ascii=False)
