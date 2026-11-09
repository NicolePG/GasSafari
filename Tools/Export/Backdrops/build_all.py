"""Regenera todos los fondos (FBX) y las panorámicas de prueba. Uso:  python build_all.py [W1 W2 ...] [--no-pano]
Busca Blender 4.5 en la variable BLENDER o en la ruta de instalación habitual de Windows."""
import os
import subprocess
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
BLENDER = os.environ.get("BLENDER", r"C:\Program Files\Blender Foundation\Blender 4.5\blender.exe")


def run(script, *args):
    cmd = [BLENDER, "--background", "--python", os.path.join(HERE, script)] + (["--"] + list(args) if args else [])
    r = subprocess.run(cmd, capture_output=True, text=True)
    for line in r.stdout.splitlines():
        if line.startswith("[bd]") or "Error" in line or "Traceback" in line:
            print(line)
    if r.returncode != 0:
        print(r.stderr[-1500:])
        sys.exit(r.returncode)


if __name__ == "__main__":
    ws = [a for a in sys.argv[1:] if a.startswith("W")] or [f"W{i}" for i in range(1, 7)]
    for w in ws:
        run(f"backdrops_{w.lower()}.py")
    if "--no-pano" not in sys.argv:
        run("render_panorama.py", *ws)
