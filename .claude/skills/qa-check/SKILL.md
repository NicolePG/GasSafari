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
- Colisiones y clipping (nada se traspasa), por nivel y por personaje:
  - La balsa no atraviesa paredes, orillas ni props a ninguna velocidad (`ContinuousDynamic`, colliders de 0.3 m o más de grosor).
  - Los animales no se meten dentro de la balsa ni entre ellos en ninguna animación (incluidas las celebraciones); los sombreros no atraviesan cabezas ni orejas; la fauna no atraviesa la balsa.
  - Todo prop con el que pueda chocar la balsa tiene un collider simple ajustado a su forma.
  - La cámara no atraviesa paredes ni techos (`CinemachineCollider` o equivalente).
- Agua y balsa: nunca se ve agua dentro de la balsa (con 4 animales el piso queda por encima del agua; máscara de agua en todas las balsas). Inclinación por peso 1-3° por animal y máximo 6° de cabeceo con 4 empujando; la proa nunca se hunde.
- Caídas: un roce a velocidad normal no tira a nadie; el caído nada junto a la balsa, la cámara lo encuadra, su icono sale en el borde si queda fuera de cámara y reaparece a los 6 s sin pantalla de carga.

## Reglas
- No marcar una tarea como hecha con errores en consola o pruebas en rojo.
- Bugs encontrados que no son de esta tarea: anotarlos en `docs/bugs.md` con la plantilla de docs/11 §5 y seguir.
- Para revisiones largas (muchos archivos o logs), usar el subagente `unity-qa` para no llenar el contexto principal.
