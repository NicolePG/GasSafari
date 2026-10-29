---
name: game-modes
description: Implementar o modificar modos de juego (Historia, Contrarreloj, Espejo, Modificadores, minijuegos de Fiesta, Infinito, Desafío Diario, Carrera por Equipos), la dificultad o la Ayuda Real.
---

# Modos de juego

Reglas: docs/06 §1 (modos) y §3 (dificultad y Ayuda Real).

## Arquitectura
- Cada modo es un `GameModeData` (SO) + una clase que hereda de `GameModeBase` con ganchos: `OnSessionStart`, `OnLevelLoaded`, `OnRaftSpawned`, `OnCheckpoint`, `OnFinish`, `BuildResults`.
- `GameSession` guarda el modo, la dificultad y los modificadores activos. El nivel no sabe en qué modo está: solo llama a los ganchos.
- Los modos no duplican lógica de la balsa: cambian reglas a través de `BalanceConfig` en tiempo de ejecución (copia), `DifficultyData` o componentes extra.

## Por modo
- **Contrarreloj:** `GhostRecorder` graba pose de la balsa a 10 Hz en un buffer prealocado; `GhostPlayer` interpola. Guardar solo el mejor intento por nivel.
- **Espejo:** `LevelMirror` pone la raíz de geometría en escala X = -1 y espeja los nodos de las splines; verificar triggers y anclas.
- **Modificadores:** cada `ModifierData` aplica su efecto al cargar el nivel y lo revierte al salir. Con modificadores activos no se guardan estrellas.
- **Fiesta:** una escena por minijuego (`Scenes/Party/`), cada una con su controlador de reglas, puntaje y rondas. Mini balsa individual = prefab de balsa con radio 1 m y 1 asiento.
- **Infinito:** `EndlessGenerator` coloca `EndlessChunk` (prefabs con punto de entrada y salida alineados) según `EndlessChunkData`; recicla los tramos que quedan atrás con pool; dificultad +1 cada 200 m.
- **Desafío Diario:** semilla `int.Parse(DateTime.Today.ToString("yyyyMMdd", CultureInfo.InvariantCulture))` → `System.Random` propio (nunca `UnityEngine.Random`) → 1500 m + 1 modificador.
- **Carrera por Equipos:** 2 balsas; local con 2 cámaras (pantalla dividida vertical) y online con NGO. Usa niveles `raceCompatible` y sus `TeamRaceSpawn`.

## Dificultad y ayuda
- Aplicar `DifficultyData` al cargar cada nivel (sync, recarga, daño, penalización, velocidad de peligros, filtro de checkpoints).
- Ayuda Real: `CheckpointSystem` cuenta reapariciones por checkpoint; a la tercera, ofrece las 3 opciones de docs/06 §3.

## Verificación
Prueba T-DAILY-01, un recorrido completo de cada modo con 2 jugadores, y que los resultados se guarden en el lugar correcto.
