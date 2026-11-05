# Tools · modelos y sonidos generados por script

Nada de esta carpeta se importa solo en Unity: lo exportado se revisa y luego se copia a `Assets/_Project/`.

## Modelos (Blender 4.5 LTS)

```
blender --background --python Tools/Blender/alex.py      # también marty, melman, gloria, julien
blender --background --python Tools/Blender/props.py     # balsa y power-ups (--raft / --powerups)
blender --background --python Tools/Blender/char_anims.py  # clips que faltan de los 5 personajes (-- Alex ...)
blender --background --python Tools/Blender/fauna.py       # fosa, cocodrilos, cangrejo, murciélago, tentáculo
blender --background --python Tools/Blender/bosses.py      # grúa con gancho (jefe W1-5)
blender --background --python Tools/Blender/env_w1.py      # escenario por mundo: env_w1 .. env_w5 (-- Ferry ...)
```

- `gs_common.py`: paleta, materiales planos, primitivas redondeadas, esqueleto Humanoid, animaciones
  (Idle_Sit, Swim, Fall, Climb, Celebrate, Hit), exportación FBX (Apply Transform, -Z forward, Y up) y vistas previas.
- Salida: `Tools/Export/Characters/<Nombre>/<Nombre>.fbx`, `Tools/Export/Props/{Raft,PowerUps}/`.
- Clips extra (sin malla, mismo esqueleto): `Tools/Export/CharacterAnims/<Nombre>/<Nombre>@<Clip>.fbx` con
  Shuffle_L/R, Grab, Victory y Select_Pose en bucle; Ability y Disappointed sin bucle. En Unity: rig Humanoid
  con "Copy From Other Avatar" del modelo del personaje.
- Fauna (rig Generic, hueso raíz `Root`, clips en el mismo FBX): `Tools/Export/Fauna/<Nombre>.fbx`. Fossa, Bat y
  Tentacle: Idle, Attack, Retreat. Crab: + Walk. Croc: Sleep, Wake, Idle, Swim, Bite, Retreat. GiantCroc: Idle, Swim,
  Bite, Retreat.
- Grúa (`Tools/Export/Bosses/Crane.fbx`): rig Generic, huesos Root, Wheel_1..8, Turret, Boom, Trolley y Hook;
  clips Idle, Move (en el sitio) y Lower_Hook (el gancho baja 4.5 m; el cable se estira entre Trolley y Hook).
- Escenario (`env_kit.py` + `env_w<N>.py`): `Tools/Export/Environment/<Mundo>/<Mundo>_<Nombre>.fbx`, ≤ 3000
  triángulos, pivote en la base (z = 0 = suelo o línea de agua). Los props con partes móviles exportan un vacío
  raíz con `Static` y cada parte con su pivote (`Propeller`, `Flag`, `Wheel`, `Gondola_0..7`, `Door_L/R`, `Gate`...).
  Piezas modulares: `Canal_*` (cauce de 10 m, paso de 5 m) y `Tunnel_*`; agua en z = 0.
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
