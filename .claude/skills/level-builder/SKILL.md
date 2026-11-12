---
name: level-builder
description: Construir o modificar un nivel o escenario (cauce, greybox, checkpoints, obstáculos, power-ups, mangos, luz, ambiente, LevelData) o la plantilla de niveles y sus shaders.
---

# Construir un nivel

Diseño del nivel: docs/04 (Grep `## <ID>`, por ejemplo `## W3-2`). Obstáculos y power-ups: docs/05. Arte: docs/09.

## Pasos
1. **Escena:** duplicar `Scenes/Pruebas/TEST_LevelTemplate.unity` como `Scenes/Juego/Niveles/N<índice>_<ID>_<Nombre>.unity` (nombre exacto de docs/04). Agregarla a Build Settings. Organización completa en `Scenes/LEEME.md`.
2. **LevelData:** completar `Data/Levels/<ID>.asset` (tiempos Oro/Plata, música, texto de mecánica, `raceCompatible`).
3. **Cauce:** `SplineContainer` con `FlowSpline`. Ancho normal 8-12 m, pasos estrechos 5 m. Curva de intensidad de corriente por tramo según las secciones del doc.
4. **Greybox primero** con ProBuilder (canales, rampas, paredes). Probar el recorrido completo con 1 jugador antes de poner arte. Nunca greybox y arte en la misma tarea.
5. **Secciones:** una por cada punto de la lista del doc, en orden. Marcar cada sección con un objeto vacío `Section_<n>` para depurar.
6. **Checkpoints:** prefab `Prefabs/Level/Checkpoint` en la salida y cada 70-190 m de recorrido (unos 20-40 s; número del doc). Marcar `familiarOnly` en los extra y `hardKeep` en los que quedan en Amistades Rotas.
7. **Obstáculos y power-ups:** solo desde `Prefabs/Hazards` y `Prefabs/PowerUps`. Si falta un tipo, crearlo antes con la skill `hazards-powerups`.
8. **Mangos dorados:** 3 `MangoSpot` según el doc (ruta, habilidad o riesgo, escondido).
9. **Momento estrella:** construirlo con cuidado (Timeline si tiene cámara o evento guionado).
10. **Ambiente:** paleta y música del mundo (docs/04 "Mundos"), Volume del mundo, Directional Light, lightmaps baked en objetos estáticos y light probes en el cauce.
11. **Carrera por Equipos:** si `raceCompatible`, 2 `TeamRaceSpawn`.

## Dirección visual
Aplica a todo nivel con arte (no al greybox). Perfiles de Bloom por mundo en `Settings/Volumes/Volume_W<N>.asset`; props en `Art/Environment/W<N>/` (docs/09 §5).
- **Luz por mundo:** W1 noche azulada con farolas y neón emisivo con Bloom fuerte; W2 día soleado (W2-2 tormenta gris con niebla y rayos); W3 atardecer tropical cálido; W4 cuevas oscuras con cristales cian y lava roja emisivos; W5 atardecer dorado.
- **Cielo y niebla:** cielo degradado por mundo y niebla lineal del color del horizonte.
- **Partículas de ambiente:** luciérnagas (W3 y W4), hojas (W3), chispas de lava (W4), gaviotas simples (W2), confeti (W3-5).
- **Orillas siempre decoradas:** algún prop o planta cada 3-5 m; nunca una orilla vacía. Usa las 3 variantes de cada prop rotadas y escaladas (0.9-1.1) para que no se repitan.
- **Hito del mundo:** se ve desde la salida y enmarca el momento estrella.
- **Agua (regla fija): siempre se ve como agua.** En cada nivel se distingue claramente de las orillas y del suelo: más saturada y de otro tono que el terreno del mundo, con brillo especular, destellos, espuma en las orillas, líneas de corriente y estela detrás de la balsa. Nunca café, beige ni del mismo color que el terreno. W1 de noche: azul profundo con reflejos de neón; W2 turquesa; W3 turquesa azulado (no verde); W4 agua oscura con brillo cian; W5 río turquesa contra el cañón rojizo; W6 cian eléctrico contra el magenta. Los valores viven en `WorldLook` (Editor) y los materiales en `Art/Materials/World/M_Water_W<N>`, `M_WaterFlow_W<N>`; shaders `SG_ToonWater` (cáusticas, brillo, chispas, espuma por profundidad) y `SG_WaterFlow` (cinta de corriente con espuma de orilla). La estela es `RaftWake` (se agrega sola en `RaftSpawner`). Revísalo siempre con una captura desde la cámara del juego (`LevelShotsTests.CaptureLevels`, lee `Screenshots/shots_request.txt`), no desde la vista de escena.
- **Fondos por capas y cielo:** `LevelVisualPass.Apply` (menú `Gas Safari/Visual/Pulir escena abierta` o `Pulir todos los niveles`; `LevelDresser.Dress` lo llama solo) coloca `Tools/Export/Backdrops` importados con `Gas Safari/Visual/Importar fondos`: capa media (8 piezas/mundo, cada 70-105 m a 75-150 m del eje), lejana (6 piezas/mundo, a 330/500 m con remates detrás de la salida y delante de la meta) y suelo de relleno (losas de 200 m, falda y horizonte). Niebla lineal por mundo y skybox con nubes `SG_SkyClouds` (tabla en `WorldLook`). Desde la cámara del juego nunca se ve vacío a los costados ni al fondo, ni el borde del mundo. Rendimiento: static batching, sin colliders, sin sombras, `LODGroup`, 60 FPS. Colocación detallada: `Tools/Export/Backdrops/README.md`.
- **Partes móviles animadas** (noria, faro, banderas, hélices, aspas): con un componente simple de giro o vaivén.
- **Nada alto tapa la balsa:** los props altos van lejos del cauce o se desvanecen cerca de la cámara.
- **Rendimiento:** objetos estáticos con static batching y lightmaps; 60 FPS.

## MCP for Unity
- Crear objetos en lote con `batch_execute`; localizar con `find_gameobjects` antes de modificar.
- Activar el grupo `probuilder` solo para el greybox y desactivarlo al terminar.
- Captura de cámara para revisar el resultado en lugar de describirlo.

## Ritmo (validar al final)
- Algo nuevo cada ~30 s, checkpoint cada 70-190 m, 1 momento estrella.
- Todo peligro con aviso de 0.5 s o más.
- Revisión completa con el subagente `level-reviewer`.

## Shaders y plantilla
- `SG_Toon` y `SG_ToonWater` según docs/09 §2. El URP Asset debe tener Depth Texture y Opaque Texture.
- `TEST_LevelTemplate` contiene: LevelManager, RaftSpawner, StartGate, FinishGate, CheckpointSystem, cámara Cinemachine, agua, Directional Light, Volume, HUD, EventSystem.
