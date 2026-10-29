# 12 · Backlog

Formato: `- [ ] **ID** tarea · skill · ref`. La skill `next-task` toma la primera tarea sin marcar, en orden. Al terminar: `- [x]` + fecha. Si se bloquea: `BLOQUEADO: motivo` al final de la línea.
Tareas marcadas **(TÚ)** las hace el usuario fuera de Claude Code (generar modelos, grabar sonidos, playtests).

Orden pensado para que el juego sea jugable desde el día 2 y cada día crezca. Entrega: 10/11/2026. Si falta tiempo, posponer en este orden: M8 Online → M7-09/10/11 (Infinito, Diario, Carrera) → Mundo 6.

## M0 · Base del proyecto (29 oct)
- [ ] **M0-01** Estructura de carpetas y asmdefs · unity-csharp-2022 · 10 §2
- [ ] **M0-02** Instalar paquetes y configurar proyecto (Input System, Linear, Depth/Opaque Texture, Fixed Timestep 0.02) · unity-csharp-2022 · 10 §1
- [ ] **M0-03** Capas físicas y matriz de colisiones · raft-physics · 10 §11
- [ ] **M0-04** Input actions `GasSafariControls` (Player y UI) · unity-csharp-2022 · 02 §13, 10 §8
- [ ] **M0-05** `PlayerCommand`, `IPlayerCommandSource`, `HumanCommandSource` · unity-csharp-2022 · 10 §8
- [ ] **M0-06** `GameServices`, `Services`, bootstrap del editor y escena 00_Boot · unity-csharp-2022 · 10 §3-4
- [ ] **M0-07** `BalanceConfig` y `DifficultyData` (3 assets) con valores de los docs · unity-csharp-2022 · 02 §14, 06 §3
- [ ] **M0-08** Escena `Test/Sandbox_Raft` con agua plana y luz · level-builder · 10 §3

## M1 · Núcleo de la balsa (29-31 oct)
- [ ] **M1-01** `RaftController` + `RaftBuoyancy` · raft-physics · 02 §1
- [ ] **M1-02** `RaftChambers` (reventar, reparar, escudo, hundimiento) · raft-physics · 02 §8
- [ ] **M1-03** `PlayerSeat` (movimiento por el borde, separación, posiciones iniciales) · raft-physics · 02 §2
- [ ] **M1-04** `GasTank` · raft-physics · 02 §4
- [ ] **M1-05** `FartThruster`: toque, ráfaga y puntería · raft-physics · 02 §3
- [ ] **M1-06** `FartThruster`: pedo cargado · raft-physics · 02 §3
- [ ] **M1-07** `SyncDetector` + `TeamMeter` · raft-physics · 02 §5-6
- [ ] **M1-08** `SuperFartController` + `TimeController` · raft-physics · 02 §6
- [ ] **M1-09** `AvatarStateMachine` + agarrarse · raft-physics · 02 §7
- [ ] **M1-10** Caída, nado, subir y reaparición automática · raft-physics · 02 §9
- [ ] **M1-11** `FlowSpline` + `RaftFlowReceiver` · raft-physics · 02 §1
- [ ] **M1-12** `RaftStateMonitor` (vuelco, aire, aterrizaje) + control aéreo · raft-physics · 02 §1, §15
- [ ] **M1-13** `CheckpointSystem` + reaparición + `LevelTimer` · level-builder · 02 §10
- [ ] **M1-14** `InputHub`: unión de 1-4 jugadores locales + colores · ui-hud · 02 §13, 03 Reglas
- [ ] **M1-15** Cámara Cinemachine 2.x con TargetGroup e Impulse · vfx-juice · 02 §12
- [ ] **M1-16** `PingSystem` + cuenta regresiva · ui-hud · 02 §11
- [ ] **M1-17** VFX y SFX provisionales de pedos con pool · vfx-juice, audio-sfx · 09 §6, 08 §2
- [ ] **M1-18** Pruebas T-GAS, T-SYNC, T-RAFT, T-FALL, T-CP · qa-check · 11 §2

## M2 · Personajes (31 oct-2 nov)
- [ ] **M2-01** (TÚ) Generar los 5 personajes en Meshy y riggear · — · 09 §4
- [ ] **M2-02** (TÚ) Grabar pedos y voces de los 5 personajes · — · 08 §6
- [ ] **M2-03** `CharacterData` x5 · character-ability · 03 Resumen
- [ ] **M2-04** `AbilityBase`, `AbilityRunner`, `AbilityContext`, `PassiveBase` · character-ability · 03
- [ ] **M2-05** Alex: Rugido + Estrella del Show · character-ability · 03 Alex
- [ ] **M2-06** Marty: Galope Turbo · character-ability · 03 Marty
- [ ] **M2-07** Melman: Cuello Gancho, Botiquín, Vista Alta · character-ability · 03 Melman
- [ ] **M2-08** Gloria: Panzazo + Peso Pesado · character-ability · 03 Gloria
- [ ] **M2-09** Julien: Fiesta Real, Corona Magnética, Salto · character-ability · 03 Julien
- [ ] **M2-10** `AbilitySynergySystem` (5 sinergias) · character-ability · 03 Sinergias
- [ ] **M2-11** `FartJuice` (animación procedural de pedos) · vfx-juice · 09 §4
- [ ] **M2-12** Importar personajes, `CharacterBase.controller` y overrides · model-import · 09 §3-4
- [ ] **M2-13** Prefabs finales de personaje con sonidos (`SfxSet`) · model-import, audio-sfx · 03, 08 §4
- [ ] **M2-14** Prueba T-ABIL-01 · qa-check · 11 §2

## M3 · Sistema de niveles (2-3 nov)
- [ ] **M3-01** `LevelData` x31 y `WorldData` x6 · level-builder · 04
- [ ] **M3-02** `LevelManager`, `RaftSpawner`, `StartGate`, `FinishGate` · level-builder · 10 §5
- [ ] **M3-03** Escena `_LevelTemplate` · level-builder · 10 §3
- [ ] **M3-04** Shaders `SG_Toon` y `SG_ToonWater` · level-builder · 09 §2
- [ ] **M3-05** `HazardBase`, `HazardTag`, `HazardConfig` y aviso común · hazards-powerups · 05
- [ ] **M3-06** `PowerUpBase` + P01-P07 · hazards-powerups · 05 Power-ups
- [ ] **M3-07** HUD completo · ui-hud · 07 §3
- [ ] **M3-08** Pausa · ui-hud · 07 §4
- [ ] **M3-09** `ResultsBuilder`, premios y repetición "¿Quién fue?" · ui-hud · 06 §4
- [ ] **M3-10** `SaveSystem` + `ProgressionService` + pruebas T-SAVE y T-PROG · save-progression · 10 §13, 06 §2

## M4 · Bots y modo solo (3 nov)
- [ ] **M4-01** `BotBrain`: percepción y navegación · bot-ai · 10 §15
- [ ] **M4-02** Bots: sincronía, habilidades, agarrarse, Súper Pedo, pings · bot-ai · 10 §15
- [ ] **M4-03** Modo solo con 2 animales (doble control) · unity-csharp-2022 · 02 §13
- [ ] **M4-04** Validar QA-10 · qa-check · 11 §3

## M5 · Contenido (3-7 nov)
- [ ] **M5-01** T-0 Piscina del Zoológico · level-builder · 04 T-0
- [ ] **M5-02** Obstáculos de W1: H01, H03, H04, H05, H07, H24, H27, H28 · hazards-powerups · 05
- [ ] **M5-03** W1-1 Fuentes del Zoo · level-builder · 04 W1-1
- [ ] **M5-04** W1-2 Alcantarillas · level-builder · 04 W1-2
- [ ] **M5-05** W1-3 Río de la Ciudad · level-builder · 04 W1-3
- [ ] **M5-06** W1-4 Mercado Flotante · level-builder · 04 W1-4
- [ ] **M5-07** W1-5 Persecución en el Puerto (jefe) · level-builder · 04 W1-5
- [ ] **M5-08** Obstáculos de W2: H06, H08, H09, H10, H25, H32 + `Anchor` · hazards-powerups · 05
- [ ] **M5-09** W2-1 Cajas a la Deriva · level-builder · 04 W2-1
- [ ] **M5-10** W2-2 La Tormenta · level-builder · 04 W2-2
- [ ] **M5-11** W2-3 Cementerio de Barcos · level-builder · 04 W2-3
- [ ] **M5-12** W2-4 El Gran Remolino · level-builder · 04 W2-4
- [ ] **M5-13** W2-5 Arrecife de Coral (jefe) · level-builder · 04 W2-5
- [ ] **M5-14** Obstáculos de W3: H11, H12, H13, H14, H15, H16, H29 · hazards-powerups · 05
- [ ] **M5-15** W3-1 Playa de Llegada · level-builder · 04 W3-1
- [ ] **M5-16** W3-2 Río de Lianas · level-builder · 04 W3-2
- [ ] **M5-17** W3-3 Territorio de las Fosas · level-builder · 04 W3-3
- [ ] **M5-18** W3-4 Pantano de los Cocodrilos · level-builder · 04 W3-4
- [ ] **M5-19** W3-5 La Gran Fiesta Lémur (jefe) · level-builder · 04 W3-5
- [ ] **M5-20** Obstáculos de W4: H17, H18, H19, H20, H22, H26, H31 · hazards-powerups · 05
- [ ] **M5-21** W4-1 Cascadas Escalonadas · level-builder · 04 W4-1
- [ ] **M5-22** W4-2 Cueva de Cristal · level-builder · 04 W4-2
- [ ] **M5-23** W4-3 Géiseres de Vapor · level-builder · 04 W4-3
- [ ] **M5-24** W4-4 Río Subterráneo · level-builder · 04 W4-4
- [ ] **M5-25** W4-5 Corazón del Volcán (jefe) · level-builder · 04 W4-5
- [ ] **M5-26** Obstáculos de W5: H21, H23 + compuertas de represa · hazards-powerups · 05
- [ ] **M5-27** W5-1 Rápidos del Cañón · level-builder · 04 W5-1
- [ ] **M5-28** W5-2 Rampas del Abismo · level-builder · 04 W5-2
- [ ] **M5-29** W5-3 Laberinto de Manglares · level-builder · 04 W5-3
- [ ] **M5-30** W5-4 La Represa · level-builder · 04 W5-4
- [ ] **M5-31** W5-5 El Megatobogán Final (jefe) · level-builder · 04 W5-5
- [ ] **M5-32** Obstáculo H30 · hazards-powerups · 05
- [ ] **M5-33** W6-1 Fiesta en Espejo · level-builder · 04 W6-1
- [ ] **M5-34** W6-2 Noche sin Luz · level-builder · 04 W6-2
- [ ] **M5-35** W6-3 Todo a Contracorriente · level-builder · 04 W6-3
- [ ] **M5-36** W6-4 Lluvia de Cocos · level-builder · 04 W6-4
- [ ] **M5-37** W6-5 La Corona del Rey (jefe) · level-builder · 04 W6-5
- [ ] **M5-38** (TÚ) Revisar arte de escenario por mundo (packs CC0 + Meshy) · — · 09 §5
- [ ] **M5-39** Viñetas entre mundos con Timeline · ui-hud · 01 Historia
- [ ] **M5-40** Música por mundo con capas · audio-sfx · 08 §5

## M6 · Menús y meta (7-8 nov)
- [ ] **M6-01** Título y menú principal · ui-hud · 07 §1-2
- [ ] **M6-02** Selección de personajes (lobby local, bots, dificultad) · ui-hud · 07 §2
- [ ] **M6-03** Mapa de mundos y ficha del nivel · ui-hud · 07 §2
- [ ] **M6-04** Opciones completas con remapeo y accesibilidad · ui-hud · 07 §5
- [ ] **M6-05** Dificultad aplicada + Ayuda Real · game-modes · 06 §3
- [ ] **M6-06** Tienda, `CosmeticsService` y cosméticos visibles en juego · save-progression · 06 §2
- [ ] **M6-07** Logros y estadísticas · save-progression · 06 §5-6
- [ ] **M6-08** Créditos desde `CREDITS.md` · ui-hud · 07 §2

## M7 · Modos (8-9 nov)
- [ ] **M7-01** Contrarreloj con fantasma · game-modes · 06 §1
- [ ] **M7-02** Modo Espejo · game-modes · 06 §1
- [ ] **M7-03** Modificadores (Caos) · game-modes · 06 §1
- [ ] **M7-04** Fiesta: Balsa-Gol · game-modes · 06 §1
- [ ] **M7-05** Fiesta: Rey de la Balsa · game-modes · 06 §1
- [ ] **M7-06** Fiesta: Pesca de Mangos · game-modes · 06 §1
- [ ] **M7-07** Fiesta: Carrera de Burbujas · game-modes · 06 §1
- [ ] **M7-08** Fiesta: Coco Caliente · game-modes · 06 §1
- [ ] **M7-09** Infinito (tramos y generador) · game-modes · 06 §1
- [ ] **M7-10** Desafío Diario + prueba T-DAILY-01 · game-modes · 06 §1
- [ ] **M7-11** Carrera por Equipos local (pantalla dividida) · game-modes · 06 §1

## M8 · Online (9 nov)
- [ ] **M8-01** `NetSession`: autenticación anónima, Relay y Lobby · netcode-ngo · 10 §14
- [ ] **M8-02** Balsa con autoridad del anfitrión + `RemoteCommandSource` · netcode-ngo · 10 §14
- [ ] **M8-03** Replicación de avatares (asiento, estado, gas) · netcode-ngo · 10 §14
- [ ] **M8-04** Escenas en red + UI de sala con código · netcode-ngo, ui-hud · 07 §2
- [ ] **M8-05** Desconexión → bot · netcode-ngo, bot-ai · 10 §14
- [ ] **M8-06** Carrera por Equipos online · netcode-ngo, game-modes · 06 §1
- [ ] **M8-07** Validar QA-11 y QA-12 · qa-check · 11 §3

## M9 · Pulido y entrega (9-10 nov)
- [ ] **M9-01** Pase de juice: sacudidas, cámara lenta, destellos, squash · vfx-juice · 09 §6
- [ ] **M9-02** Mezcla de audio y snapshots · audio-sfx · 08 §1
- [ ] **M9-03** Rendimiento según presupuestos · qa-check · 10 §16
- [ ] **M9-04** (TÚ) Playtest con 2 grupos · — · 11 §6
- [ ] **M9-05** Ajustes de balance del playtest · raft-physics · 02 §14
- [ ] **M9-06** Bugs bloqueantes y críticos en 0 · qa-check · 11 §5
- [ ] **M9-07** `CREDITS.md` completo · audio-sfx · 08 §6
- [ ] **M9-08** Build final de Windows · build-release · —
