# Tools · modelos y sonidos generados por script

Nada de esta carpeta se importa solo en Unity: lo exportado se revisa y luego se copia a `Assets/_Project/`.

## Modelos (Blender 4.5 LTS)

```
blender --background --python Tools/Blender/alex.py      # también marty, melman, gloria, julien
blender --background --python Tools/Blender/props.py     # balsa y power-ups (--raft / --powerups)
blender --background --python Tools/Blender/char_anims.py  # clips que faltan de los 5 personajes (-- Alex ...)
```

- `gs_common.py`: paleta, materiales planos, primitivas redondeadas, esqueleto Humanoid, animaciones
  (Idle_Sit, Swim, Fall, Climb, Celebrate, Hit), exportación FBX (Apply Transform, -Z forward, Y up) y vistas previas.
- Salida: `Tools/Export/Characters/<Nombre>/<Nombre>.fbx`, `Tools/Export/Props/{Raft,PowerUps}/`.
- Clips extra (sin malla, mismo esqueleto): `Tools/Export/CharacterAnims/<Nombre>/<Nombre>@<Clip>.fbx` con
  Shuffle_L/R, Grab, Victory y Select_Pose en bucle; Ability y Disappointed sin bucle. En Unity: rig Humanoid
  con "Copy From Other Avatar" del modelo del personaje.
- Vistas previas en `Tools/Blender/previews/` (no se suben a Git).
- Balsa: cámaras `Chamber_0_NE`, `Chamber_1_NO`, `Chamber_2_SE`, `Chamber_3_SO` (índices de `RaftChambers`), cada una con su pivote.

## Sonidos (Python 3 + numpy, scipy, soundfile)

```
python Tools/Audio/fart_synth.py                      # pedos por personaje + fire/bubble
python Tools/Audio/voices.py                          # voces sin palabras (voz de Windows)
python -I Tools/Audio/oga_fetch.py <packs>/oga_sfx <slug> ...    # descarga CC0 de OpenGameArt con manifiesto
python -I Tools/Audio/build_sfx.py <packs>            # resto de efectos de docs/08 §4
python -I Tools/Audio/build_music.py <packs>          # música OGG + capas de intensidad
python -I Tools/Audio/gen_docs.py <packs>             # ELEGIR.md y CREDITS_pendiente.md
```

`<packs>` contiene `kenney_impact/` (kenney.nl), `kenney_interfaceSounds/`, `jingleSounds_Kenney/`,
`oga_sfx/<slug>/` y `oga_music/<slug>/`. Los packs no se suben a Git; los slugs usados están en
`Tools/Export/CREDITS_pendiente.md`. Salida en `Tools/Export/Audio/` (misma estructura que `Assets/_Project/Audio/`),
con el origen de cada archivo en `sources.json` y la lista para escuchar en `ELEGIR.md`.
