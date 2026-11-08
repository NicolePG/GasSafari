"""Revisa Tools/Export/Localization/strings.csv: claves únicas y bien formadas, mismos marcadores {n} en es y en,
sin espacios ni saltos sobrantes y el inglés a lo sumo un 20 % más largo que el español (en textos muy cortos se
tolera +2 caracteres).

    python Tools/Export/Localization/check_strings.py

Sale con código 1 si hay errores. Solo lee el CSV: no toca Unity."""
import csv
import os
import re
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
SKIP_LENGTH = ("lang.", "game.title_short", "sheet.no_time")


def main():
    rows = list(csv.DictReader(open(os.path.join(HERE, "strings.csv"), encoding="utf-8", newline="")))
    errors, seen = [], set()
    for r in rows:
        key, es, en = r["key"], r["es"], r["en"]
        if key in seen:
            errors.append(f"clave repetida: {key}")
        seen.add(key)
        if not re.fullmatch(r"[a-z0-9_]+(\.[a-z0-9_]+)+", key):
            errors.append(f"clave mal formada: {key}")
        if not es or not en:
            errors.append(f"texto vacío: {key}")
        if sorted(re.findall(r"\{\d+\}", es)) != sorted(re.findall(r"\{\d+\}", en)):
            errors.append(f"marcadores {{n}} distintos: {key}")
        if es != es.strip() or en != en.strip() or "\n" in es + en:
            errors.append(f"espacios o saltos de línea sobrantes: {key}")
        if not key.startswith(SKIP_LENGTH) and len(en) > max(len(es) * 1.2, len(es) + 2):
            errors.append(f"inglés demasiado largo ({len(en)} vs {len(es)}): {key}")
    print(f"{len(rows)} filas, {len(errors)} errores")
    for e in errors:
        print("  ", e)
    return 1 if errors else 0


if __name__ == "__main__":
    sys.exit(main())
