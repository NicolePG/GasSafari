---
name: qa-check
description: Verificar una tarea, un sistema o un nivel antes de darlo por terminado (consola, Play Mode, pruebas automáticas, casos de docs/11, rendimiento). Úsala al final de cada cambio importante.
---

# Verificación

Plan completo: docs/11.

## Versión corta (cada tarea)
1. `read_console` filtrando errores. Corregir lo propio antes de seguir.
2. Entrar a Play Mode 30 s en la escena afectada (`manage_editor`) y volver a leer la consola.
3. Correr las pruebas EditMode/PlayMode relacionadas (activar el grupo `testing` de MCP for Unity solo para esto y desactivarlo después).
4. Si toca la balsa o los avatares: probar con 1 y con 2 jugadores.
5. Reportar en 3 líneas: qué se probó, qué falló, qué falta.

## Versión completa (fin de hito o nivel)
- Todas las pruebas de docs/11 §2 en verde.
- Checklist de "terminado" del nivel (docs/11 §4); delegar el recorrido al subagente `level-reviewer`.
- Rendimiento contra docs/10 §16 con el Profiler (activar el grupo `profiling` solo para esto): FPS, ms de CPU, física, 0 B de GC en `Running`.
- Casos manuales de docs/11 §3 que apliquen.

## Reglas
- No marcar una tarea como hecha con errores en consola o pruebas en rojo.
- Bugs encontrados que no son de esta tarea: anotarlos en `docs/bugs.md` con la plantilla de docs/11 §5 y seguir.
- Para revisiones largas (muchos archivos o logs), usar el subagente `unity-qa` para no llenar el contexto principal.
