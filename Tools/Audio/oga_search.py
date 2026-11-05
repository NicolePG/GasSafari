"""Busca en OpenGameArt música CC0 y lista título, autor, licencia y archivos (solo lectura)."""
import json, re, sys, urllib.parse, urllib.request

def get(url):
    return urllib.request.urlopen(urllib.request.Request(url, headers={"User-Agent": "GasSafari-tools"}), timeout=30).read().decode("utf-8", "replace")

def search(keys, typ="12"):
    # tipo 12 = Music, licencia 4 = CC0
    q = urllib.parse.urlencode({"keys": keys, "field_art_type_tid[]": typ, "field_art_licenses_tid[]": "4",
                                "sort_by": "count", "sort_order": "DESC"})
    html = get("https://opengameart.org/art-search-advanced?" + q)
    return sorted(set(re.findall(r'href="/content/([a-z0-9-]+)"', html)) - {"faq"})

def info(slug):
    html = get("https://opengameart.org/content/" + slug)
    lic = sorted(set(re.findall(r'(CC0|CC-BY-SA \d\.\d|CC-BY \d\.\d|GPL \d\.\d|OGA-BY \d\.\d)', html.split("License(s):")[-1][:1500])))
    files = re.findall(r'href="(https://opengameart.org/sites/default/files/[^"]+\.(?:ogg|mp3|wav|zip|flac))"', html)
    title = re.search(r"<title>(.*?) \|", html)
    author = re.search(r'Author:.*?href="/users/([^"]+)"', html, re.S)
    return dict(slug=slug, title=title.group(1) if title else slug, author=author.group(1) if author else "?",
                license=lic, files=sorted(set(files)))

if __name__ == "__main__":
    out = {}
    for keys in sys.argv[1:]:
        res = []
        typ = "13" if keys.startswith("sfx:") else "12"
        for slug in search(keys.replace("sfx:", ""), typ)[:8]:
            try:
                res.append(info(slug))
            except Exception as e:
                res.append(dict(slug=slug, error=str(e)))
        out[keys] = res
    print(json.dumps(out, indent=1, ensure_ascii=False))
