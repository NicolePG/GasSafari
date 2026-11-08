# Modelos de obstáculos (H01-H35)

Generados con `Tools/Blender/hazards.py` (`blender --background --python Tools/Blender/hazards.py [-- H33_BalloonWall ...]`).
Mismo estilo que `Tools/Export/Environment`: colores planos, formas redondeadas, ≤ 3000 triángulos, pivote en la base,
frente hacia -Y, agua en z = 0. Los que tienen partes móviles exportan un vacío raíz con `Static` y cada parte móvil como
objeto aparte con su pivote. Hoja de revisión: `Tools/Blender/previews/Hazards_sheet.png` (no se sube a Git).

## Cómo se armó la lista

- Se leyeron `docs/05` y los 22 prefabs de `Assets/_Project/Prefabs/Hazards` (sin modificarlos): se contaron las mallas
  primitivas (`Cube`, `Sphere`, `Cylinder`) de cada uno. Hoy solo existen prefabs hasta W2-5.
- "Con modelo" = el prefab envuelve una instancia de un FBX propio, o el FBX ya existe en `Tools/Export` (Environment,
  Fauna, Bosses, Party). **Límite:** no se pudo resolver qué FBX concreto envuelve cada prefab (la regla del proyecto
  prohíbe leer los `.meta`), así que esa correspondencia está inferida por el nombre del prefab y el mundo.

## Lista H01-H35

| ID | Obstáculo | Estado hoy | Modelo |
|---|---|---|---|
| H01 | Pared / orilla | Prefab con 1 `Cube` | **NUEVO** `H01_Wall` |
| H03 | Compuerta temporizada | Prefab con modelo | ya existe `W1_SluiceGate` (hoja `Gate` móvil); no se rehízo |
| H04 | Oleaje | Prefab sin malla (agua + script) | **NUEVO** `H04_WaveCrest` (cresta visual, opcional) |
| H05 | Bloqueador móvil | Prefab con modelo | ya existe (contenedores W1) |
| H06 | Barril explosivo | Prefab con modelo | ya existe (`W*_Barrel`) |
| H07 | Caja rompible | Prefab con modelo | ya existe (`W*_Crate`) |
| H08 | Viento | Prefab con 8 `Cube` | **NUEVO** `H08_WindSock` (manga que gira) y `H08_GustStreak` (ráfaga) |
| H09 | Remolino | Prefab con 1 `Cylinder` | **NUEVO** `H09_Whirlpool` (embudo + `Spin` con 4 brazos en espiral) |
| H10 | Pinchos | Prefab con modelo | ya existe (coral W2, por nombre) |
| H11 | Banco de arena | Sin prefab ni modelo | **NUEVO** `H11_Sandbar` |
| H12 | Liana | Sin prefab | ya existe `W3_Liana` |
| H13 | Tronco giratorio | Sin prefab; `W3_Log_A/B` son fijos y sin eje | **NUEVO** `H13_SpinningLog` y `H13_SpinningLog_Rotten` (poste fijo + `Spinner`) |
| H14 | Fosa | Sin prefab | ya existe `Fauna/Fossa` |
| H15 | Cocodrilo | Sin prefab | ya existe `Fauna/Croc` |
| H16 | Obstáculo rítmico | Sin prefab ni modelo | **NUEVO** `H16_BeatPillar` (`Pulse` y `BeatRing`) |
| H17 | Cascada / caída | Sin prefab ni modelo del borde | **NUEVO** `H17_WaterfallEdge` (`Sheet` y `Mist`) |
| H18 | Zona oscura | Sin prefab | sin malla: es un volumen de niebla/luz (en Unity) |
| H19 | Géiser | Sin prefab; `W4_GeyserVent` es un cráter fijo | **NUEVO** `H19_Geyser` (cráter + `Plume` + `ShockRing`) |
| H20 | Roca que cae | Sin prefab ni modelo | **NUEVO** `H20_FallingRock` (`Rock` + marca de sombra `ShadowDisc`/`ShadowRing` fija) |
| H21 | Rápidos | Sin prefab | ya existen rocas (`W5_CanyonRock_*`, `W3_Rock_*`) |
| H22 | Abismo | Sin prefab | ya existen `W4_Crack`, `W5_BrokenBridge` y las rampas `W5_Ramp_A/B`, `W1_Canal_Ramp`, `W5_Slide_Ramp` |
| H23 | Cocodrilo gigante | Sin prefab | ya existe `Fauna/GiantCroc` |
| H24 | Grúa con gancho | Prefab con modelo | ya existe `Bosses/Crane` |
| H25 | Tentáculos | Prefab con modelo (x4) | ya existe `Fauna/Tentacle` |
| H26 | Murciélagos | Sin prefab | ya existe `Fauna/Bat` |
| H27 | Toldo / fuente | Prefabs con modelo | ya existen `W1_Awning`, `W1_Fountain_Launch` |
| H27 | Chorro de la fuente | `H27_Jet` con 1 `Cylinder` | **NUEVO** `H27_Jet` (`Jet` + `Splash`) |
| H27 | Trampolín | Sin modelo | **NUEVO** `H27_Trampoline` (flotador + `Mat`) |
| H28 | Barco que cruza | Prefabs con modelo | ya existen `W1_Ferry`, `W1_Speedboat` |
| H29 | Cangrejo | Sin prefab | ya existe `Fauna/Crab` |
| H30 | Coco que cae | Sin prefab | **NUEVO** `H30_Coconut` (`Coconut` + sombra fija) |
| H31 | Corriente cruzada | Sin prefab | sin malla: flechas/partículas de corriente (VFX) |
| H32 | Red de pesca | Prefab con modelo | ya existe `W2_Net` |
| H33 | Pared de globos | Prefab con 18 `Sphere` | **NUEVO** `H33_BalloonWall` (marco + `Balloon_00..17` sueltos) |
| H34 | Chorro de manguera | Prefab con `Cylinder` + `Cube` (y `Jet_Lateral` con `Cylinder`) | **NUEVO** `H34_HoseJet` (`Nozzle` + `Stream`; `Stream` sirve también para `Jet_Lateral`) |
| H35 | Plataforma que se hunde | Prefab con 1 `Cube` | **NUEVO** `H35_SinkingPlatform` (postes fijos + `Deck` que baja) |

(H02 no existe en `docs/05`.)

## Archivos nuevos

| Archivo | Triángulos | Tamaño | Partes móviles |
|---|---|---|---|
| H01_Wall.fbx | 624 | 4.1 x 1.0 x 1.2 m | — |
| H04_WaveCrest.fbx | 752 | 8.0 x 5.1 x 1.9 m | — |
| H08_WindSock.fbx | 516 | 2.1 x 1.0 x 3.5 m | `Sock` (pivote en el aro, gira con el viento) |
| H08_GustStreak.fbx | 1044 | 3.6 x 0.1 x 2.1 m | — |
| H09_Whirlpool.fbx | 1922 | 6.7 x 6.7 x 2.2 m | `Spin` (girar sobre Z) |
| H11_Sandbar.fbx | 1130 | 7.2 x 4.4 x 1.8 m | — |
| H13_SpinningLog.fbx / _Rotten.fbx | 1040 / 1184 | 6.8 x 1.4 x 2.7 m | `Spinner` (girar sobre Z; la podrida es la `Fragile`) |
| H16_BeatPillar.fbx | 1576 | 3.2 x 3.2 x 2.5 m | `Pulse` (escala en el pulso), `BeatRing` |
| H17_WaterfallEdge.fbx | 948 | 17.5 x 5.9 x 6.2 m | `Sheet`, `Mist` |
| H19_Geyser.fbx | 1156 | 4.8 x 4.8 x 6.5 m | `Plume` (escala en Z), `ShockRing` |
| H20_FallingRock.fbx | 628 | 2.8 x 2.8 x 2.4 m | `Rock` |
| H27_Jet.fbx | 1140 | 2.1 x 2.1 x 5.4 m | `Jet` (escala en Z), `Splash` |
| H27_Trampoline.fbx | 1478 | 4.8 x 4.8 x 0.8 m | `Mat` |
| H30_Coconut.fbx | 844 | 1.8 x 1.8 x 1.0 m | `Coconut` |
| H33_BalloonWall.fbx | 2644 | 8.1 x 1.1 x 4.9 m | `Balloon_00..17` (pivote en el centro) |
| H34_HoseJet.fbx | 1616 | 4.2 x 1.4 x 2.2 m | `Nozzle`, `Stream` (chorro hacia +X) |
| H35_SinkingPlatform.fbx | 1564 | 5.0 x 5.0 x 2.1 m | `Deck` |

## Notas para quien los importe

- Tamaños de referencia en metros; los volúmenes de juego (radio del remolino, ancho de la pared, alto del chorro) los
  fija el prefab, así que escala el modelo para que coincida.
- `H20_FallingRock` y `H30_Coconut` traen un disco de sombra de aviso fijo (`ShadowDisc` + `ShadowRing`); si se prefiere
  proyectarla con un decal de Unity, se puede borrar de la jerarquía.
- Los modelos no llevan texto ni animación: el movimiento lo hacen los scripts sobre las partes móviles.
- Pendiente de revisión visual en Unity: `H04_WaveCrest` (la cresta aún se parece a un casco visto de lado).
