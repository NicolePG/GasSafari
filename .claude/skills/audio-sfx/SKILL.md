---
name: audio-sfx
description: Agregar, importar, programar o mezclar sonidos y música (pedos, voces, habilidades, obstáculos, interfaz, música por mundo con capas) o actualizar CREDITS.md.
---

# Audio

Lista de IDs, mezclador, importación y fuentes: docs/08.

## Pasos para un sonido nuevo
1. Archivo en `Audio/{SFX,Music,Voices}/<categoria>_<nombre>_<nn>.wav` (WAV 44.1 kHz, 16 bits, normalizado a -1 dB).
2. Ajustes de importación según la tabla de docs/08 §3 (usar un `AssetPostprocessor` en `Scripts/Audio/Editor/AudioImportRules.cs` para aplicarlos solos por carpeta).
3. `SoundDef` en `Data/Sounds/<id>.asset`: clips, grupo del mixer, volumen, pitch 0.9-1.15, 2D/3D, distancia máxima.
4. Agregar el ID al enum `SoundId` (o a la tabla del `AudioDirector`).
5. Reproducir siempre con `AudioDirector.Play(SoundId, posición)`. Nunca `AudioSource.PlayClipAtPoint` ni AudioSources sueltos.
6. Registrar origen y licencia en `Assets/_Project/CREDITS.md`.

## Reglas del AudioDirector
- Pool de 16 fuentes 3D + 4 2D; si se acaban, reutilizar la más antigua de menor prioridad.
- No repetir el mismo clip dos veces seguidas por `SoundId`.
- Máximo 4 pedos simultáneos; el pedo que cierra un grupo de sync suena +3 dB.
- Loops (ráfaga, agua, remolino): `PlayLoop` devuelve un handle para detenerlo.

## Música
`MusicDirector`: 2 capas por pista (base e intensidad) en `AudioSource` sincronizados con `PlayScheduled`; la capa de intensidad sube con la velocidad de la balsa (> 8 m/s) o en jefes, transición de 1 s. Crossfade de 1.5 s entre pistas. El nivel rítmico W3-5 expone el pulso (BPM) para `H16` y para la sincronía en el pulso.

## Mezcla
Snapshots `Normal`, `Pausa`, `SlowMo`. Volúmenes expuestos al menú de Opciones (dB con conversión logarítmica: `20 * log10(valor)`, mínimo -80).
