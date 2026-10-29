# GAS SAFARI — Unity 2022.3.62f2 LTS (URP)

Juego 3D cooperativo de físicas: de 1 a 4 animales (Alex, Marty, Melman, Gloria, Rey Julien) impulsan UNA balsa con pedos. Juego completo: 31 niveles, 8 modos, cooperativo local y online, bots para jugar solo.

## Dónde está cada cosa (lee SOLO la sección que necesites)

| Necesito | Archivo |
|---|---|
| Visión, alcance, tono, historia | docs/01-vision.md |
| Mecánicas, controles, valores de balance | docs/02-mecanicas.md |
| Personajes, habilidades, animaciones | docs/03-personajes.md |
| Los 31 niveles | docs/04-niveles.md (Grep "## W2-3", no leer entero) |
| Obstáculos y power-ups | docs/05-obstaculos-powerups.md |
| Modos, progresión, dificultad, logros | docs/06-modos-progresion.md |
| Pantallas, HUD, textos | docs/07-ui-ux.md |
| Sonidos y música | docs/08-audio.md |
| Arte, modelos 3D, VFX | docs/09-arte-modelos.md |
| Arquitectura, clases, datos, red | docs/10-arquitectura.md |
| Pruebas | docs/11-qa.md |
| Tareas en orden | docs/12-backlog.md |

Usa Grep por el encabezado de la sección antes de leer un doc completo.

## Reglas fijas

- Unity 2022.3: `rb.velocity`, `rb.drag`, `rb.angularDrag`. PROHIBIDO `linearVelocity`, `linearDamping`, `angularDamping` (son de Unity 6).
- Cinemachine 2.x (`CinemachineVirtualCamera`, `CinemachineTargetGroup`). Netcode for GameObjects 1.x. Input System nuevo. C# 9.
- Código propio solo en `Assets/_Project/`. Namespace `GasSafari.<Carpeta>`.
- Valores de balance en ScriptableObjects (`Assets/_Project/Data`). Los docs son la fuente de verdad de los valores.
- No leer `Library/`, `Temp/`, `Logs/`, `obj/`, `UserSettings/` ni archivos `.meta`.
- Errores: `read_console` filtrando por error. Nunca leer el Editor.log completo.
- Una tarea por vez. Entregar solo lo que cambió + resumen de 2 líneas.
- Si un dato de diseño no está en los docs, preguntar. No inventar diseño.
- Personajes y nombres de Madagascar: proyecto no comercial. Nunca usar modelos, audio ni frases extraídos de las películas.

## Flujo de trabajo

- `/next-task` toma la siguiente tarea de docs/12-backlog.md.
- Reglas de código: skill `unity-csharp-2022` (siempre que se toque un .cs).
- Skills de dominio: `raft-physics`, `character-ability`, `level-builder`, `hazards-powerups`, `game-modes`, `bot-ai`, `save-progression`, `ui-hud`, `audio-sfx`, `vfx-juice`, `model-import`, `netcode-ngo`, `qa-check`, `build-release`.
- Revisiones largas (QA de un nivel, auditoría de assets): subagentes `unity-qa` y `level-reviewer`.

# Compact instructions

Al compactar conserva: tarea actual del backlog, archivos tocados, errores abiertos y decisiones nuevas. Descarta salidas de herramientas ya resueltas.
