# 10 · Arquitectura técnica (Unity 2022.3.62f2)

## 1. Paquetes

| Paquete | Restricción | Uso |
|---|---|---|
| Universal RP (`com.unity.render-pipelines.universal`) 14.x + Shader Graph | La que trae 2022.3 | Render, shaders toon y agua |
| Input System (`com.unity.inputsystem`) | Última para 2022.3 | Controles, PlayerInputManager, rebinding |
| Cinemachine (`com.unity.cinemachine`) | **2.x** (2.10) | Cámara. No instalar 3.x |
| Splines (`com.unity.splines`) | 2.x | Cauce y corriente |
| ProBuilder (`com.unity.probuilder`) | 5.x | Greybox y canales |
| TextMeshPro | Incluido | Textos |
| Timeline (`com.unity.timeline`) | Incluido | Viñetas y jefes con secuencia |
| Test Framework (`com.unity.test-framework`) | Incluido | Pruebas |
| Netcode for GameObjects (`com.unity.netcode.gameobjects`) | **1.x**. La 2.x requiere Unity 6 | Online |
| Unity Transport | La que pida NGO | Online |
| Authentication, Relay, Lobby (UGS) | Últimas compatibles con 2022.3 | Salas online sin servidor propio |
| glTFast (`com.unity.cloud.gltfast`) | Opcional | Solo si algún modelo llega en GLB |

Configuración del proyecto: Active Input Handling = Input System Package (New). Color Space Linear. URP Asset con Depth Texture y Opaque Texture activados. Fixed Timestep 0.02. Default Solver Iterations 8.

## 2. Carpetas

```
Assets/_Project/
├── Art/ Characters/<Nombre>/  Characters/_Shared/  Environment/{W1..W6,Shared}/  Props/  PowerUps/  UI/  Materials/  Shaders/  VFX/
├── Audio/ SFX/  Music/  Voices/  Mixer/
├── Data/ Balance/  Characters/  Abilities/  Levels/  Worlds/  Hazards/  PowerUps/  Sounds/  Music/
│         Cosmetics/  Modes/  Modifiers/  Achievements/  Difficulty/  Endless/
├── Prefabs/ Raft/  Characters/  Hazards/  PowerUps/  Level/  UI/  VFX/  Endless/  Services/
├── Scenes/ Boot/  Menus/  Levels/  Party/  Endless/  Test/
├── Scripts/ Core/ Input/ Raft/ Characters/ Abilities/ Team/ Hazards/ PowerUps/ Level/ Modes/
│            Bots/ Progression/ Save/ UI/ Audio/ VFX/ Net/ Utils/
├── Settings/ (URP assets, GasSafariControls.inputactions, volúmenes)
├── Tests/ EditMode/  PlayMode/
└── CREDITS.md
```
Ensamblados (asmdef): `GasSafari.Runtime` (Scripts), `GasSafari.Tests.EditMode`, `GasSafari.Tests.PlayMode`.

## 3. Escenas

| Escena | Contenido |
|---|---|
| Boot/00_Boot | Prefab `GameServices` (persistente) y carga el título |
| Menus/01_Title, 02_MainMenu, 03_CharacterSelect, 04_WorldMap | Menús; Tienda, Logros y Opciones son paneles de MainMenu |
| Menus/99_Loading | Escena aditiva de carga |
| Levels/_LevelTemplate | Plantilla: LevelManager, StartGate, FinishGate, cámara, agua, luz, HUD, Volume |
| Levels/L_T0_PiscinaZoo ... L_W6_5_CoronaReyJefe | Los 31 niveles (nombres en docs/04) |
| Party/P_BalsaGol, P_ReyBalsa, P_PescaMangos, P_CarreraBurbujas, P_CocoCaliente | Minijuegos |
| Endless/E_Endless | Infinito y Desafío Diario |
| Test/Sandbox_Raft, Test/Sandbox_Hazards | Pruebas de física y obstáculos |

Arranque desde cualquier escena en el editor: `[RuntimeInitializeOnLoadMethod(BeforeSceneLoad)]` instancia `GameServices` desde `Resources/` si no existe.

## 4. Servicios persistentes (`GameServices`, DontDestroyOnLoad)

| Servicio | Responsabilidad |
|---|---|
| `Services` | Registro estático de acceso: `Services.Save`, `Services.Audio`, etc. Se llena en Boot |
| `GameSession` | Modo, dificultad, modificadores, nivel actual, `List<PlayerSlot>` (índice, dispositivo, personaje, esBot, color, cosméticos, esRemoto) |
| `SceneLoader` | Carga asíncrona con 99_Loading; en online usa el SceneManager de NGO |
| `InputHub` | `PlayerInputManager`; unión y salida de jugadores; asigna dispositivos |
| `SaveSystem` | JSON en `persistentDataPath/save.json`, escritura atómica, copia `.bak`, campo `version` |
| `ProgressionService` | Estrellas, mangos, frijolitos, desbloqueos de mundos, modos y modificadores |
| `AchievementService` | Escucha `GameEvents` y desbloquea logros |
| `StatsService` | Estadísticas acumuladas |
| `CosmeticsService` | Compras y equipamiento |
| `AudioDirector` / `MusicDirector` | Pool de SFX, música por capas y crossfade |
| `VfxPool` | Pool de partículas por ID |
| `TimeController` | Cámara lenta: ajusta `Time.timeScale` y `Time.fixedDeltaTime = 0.02f * timeScale`; restaura al terminar |
| `NetSession` | NetworkManager, Relay, Lobby, autenticación anónima |

## 5. Sistemas del nivel

| Componente | Responsabilidad |
|---|---|
| `LevelManager` | Máquina de estados: `Intro → Countdown → Running → Respawning → Finished → Results` (+ Pausa superpuesta). Referencia su `LevelData` |
| `RaftSpawner` | Crea la balsa y un `PlayerAvatar` por `PlayerSlot` en las posiciones iniciales (docs/02 §2) |
| `CheckpointSystem` | Lista ordenada de `Checkpoint`; guarda estado; reaparición; filtra `familiarOnly` / `hardKeep` según dificultad |
| `LevelTimer` | Cronómetro y penalizaciones |
| `FlowSpline` | `SplineContainer` del cauce: ancho, curva de intensidad de corriente, consulta `GetFlow(pos) → (dir, accel)` con caché por tramo |
| `GasWindZone` | Viento por ráfagas con aviso |
| `Anchor` | Punto enganchable por Melman |
| `StartGate`, `FinishGate`, `MangoSpot`, `TeamRaceSpawn` | Marcadores del nivel |
| `LevelMirror` | Aplica espejo (modo Espejo) |
| `GhostRecorder` / `GhostPlayer` | Contrarreloj |
| `ResultsBuilder` | Junta tiempo, estrellas, premios y repetición "¿Quién fue?" (buffer circular de 5 s con poses de la balsa) |

## 6. Balsa y personajes

| Componente | Responsabilidad |
|---|---|
| `RaftController` | Rigidbody (Interpolate, ContinuousDynamic), masa total, centro de masa, punto de entrada de fuerzas `ApplyThrust(fuerza, punto, modo)` |
| `RaftBuoyancy` | 4 puntos de flotación, arrastre agua/aire, estado `IsAirborne` |
| `RaftChambers` | Estado de las 4 cámaras, `PopChamberAt(punto)`, `Repair(i)`, escudo de coco, evento de hundimiento |
| `RaftFlowReceiver` | Aplica corriente y viento (`ForceMode.Acceleration`), multiplicadores de Gloria y modificadores |
| `RaftStateMonitor` | Detecta vuelco, hundimiento y aterrizajes torcidos |
| `PlayerAvatar` | Raíz del animal: `PlayerSlot`, `CharacterData`, fuente de comandos, máquina de estados del avatar |
| `AvatarStateMachine` | `Seated, Charging, Grabbing, AbilityCast, Falling, Swimming, Climbing, Respawning` |
| `PlayerSeat` | Ángulo en el borde, `WorldPoint`, separación mínima |
| `GasTank` | Gas, recarga, eventos |
| `FartThruster` | Toque, ráfaga y cargado; pide multiplicador al `SyncDetector`; aplica fuerza vía `RaftController` |
| `AbilityRunner` | Recarga y activación del `AbilityBase` del personaje |
| `PassiveBase` + `AlexShowPassive`, `MelmanMedkitPassive`, `GloriaHeavyPassive`, `JulienMagnetPassive` (Marty: solo datos) | Pasivas |
| `FartJuice` | Animación procedural de pedos (aplastar/estirar, inflado) |
| `CharacterAnimator` | Traduce estados a parámetros del Animator |

Los animales sentados son hijos de la balsa, sin Rigidbody propio (o kinematic). Al caer al agua pasan a tener Rigidbody dinámico y capa `CharacterInWater`.

## 7. Equipo

`SyncDetector` (grupos de sync y multiplicadores, docs/02 §5), `TeamMeter`, `SuperFartController`, `PingSystem`, `AbilitySynergySystem` (docs/03, Sinergias).

## 8. Entrada (clave para bots y red)

- Asset `Settings/GasSafariControls.inputactions`. Mapa `Player`: Move (Vector2), Aim (Vector2), Fart (Button), Hold (Button), Ability (Button), Grab (Button), SuperLeft (LB), SuperRight (RB) (en teclado R activa ambas), Ping (Vector2, cruceta), Jump (Button), Pause (Button). Mapa `UI` estándar.
- Toda fuente de control implementa `IPlayerCommandSource` y produce un `PlayerCommand` por frame:

```csharp
public struct PlayerCommand : INetworkSerializable
{
    public float SeatMove;      // -1..1
    public float Aim;           // -1..1
    public bool FartHeld, HoldHeld, AbilityHeld, GrabHeld, SuperHeld;
    public byte FartPresses, AbilityPresses, JumpPresses; // contadores: no se pierden pulsaciones en red
    public PingType Ping;
    public Vector2 Swim;
    // NetworkSerialize(...) serializa todos los campos
}
```
- Implementaciones: `HumanCommandSource` (PlayerInput), `BotCommandSource` (BotBrain), `RemoteCommandSource` (llega por red). La lógica del avatar nunca lee el Input System directamente.

## 9. Datos (ScriptableObjects)

| SO | Campos principales |
|---|---|
| `BalanceConfig` | Toda la tabla de docs/02 §14 |
| `DifficultyData` | syncWindow, regenMultiplier, chamberDamageImpulse, respawnPenalty, hazardSpeed, autoRespawnTime, checkpointFilter |
| `CharacterData` | id, nombre, prefab, gasCapacity, puffImpulse, blastForce, regenPerSecond, regenDelay, weight, height, ability (AbilityBase), passive, sfxSet, overrideController, icono, color de UI |
| `AbilityBase` (abstracto) | id, cooldown, icono, sonido, `Activate(AbilityContext)`, `CanActivate(AbilityContext)` |
| `LevelData` | id (W2-3), sceneName, world, goldTime, silverTime, mangoCount=3, musicId, mechanicText, raceCompatible, ordenEnMundo |
| `WorldData` | id, nombre, paleta, música, requisito de desbloqueo, niveles |
| `HazardConfig` | id, parámetros (docs/05), sonido de aviso, tiempo de aviso |
| `PowerUpData` | id, efecto, duración, sonido, vfx |
| `SoundDef` / `MusicTrack` | docs/08 |
| `CosmeticData` | id, categoría, precio, prefab o color, condición de desbloqueo |
| `GameModeData` | id, requisito, escena(s), reglas |
| `ModifierData` | id, efecto, requisito de estrellas |
| `AchievementData` | id, nombre, condición (tipo + valor) |
| `EndlessChunkData` | prefab, mundo, dificultad 1-5, largo |

## 10. Eventos (`GameEvents`, eventos estáticos de C#)

OnFart(avatar, tipo, impulso) · OnSync(cantidad, mult) · OnTeamMeterChanged(valor) · OnSuperFart(potencia) · OnChamberPopped(i) · OnChamberRepaired(i) · OnRaftSunk · OnPlayerFell(avatar) · OnPlayerRescued(rescatado, rescatador) · OnCheckpoint(i) · OnRespawn · OnPowerUp(avatar, id) · OnMango(i) · OnAbilityUsed(avatar, id) · OnSynergy(id) · OnHazardWarning(hazard) · OnPing(avatar, tipo) · OnGasEmpty(avatar) · OnLevelFinished(resultado).

Regla: suscribirse en `OnEnable` y desuscribirse en `OnDisable`. `GameEvents.ClearAll()` al descargar un nivel.

## 11. Física

| Capa | Choca con |
|---|---|
| Raft | Environment, Hazard, Raft, CharacterInWater, Trigger, PowerUp |
| Character (sentado) | Trigger, PowerUp |
| CharacterInWater | Environment, Raft, Hazard, Trigger |
| Environment | Raft, CharacterInWater |
| Hazard | Raft, CharacterInWater |
| Trigger | Raft, Character, CharacterInWater |
| PowerUp | Raft, Character |
| Water | Nada (solo consultas) |
| Ghost | Nada |

Las etiquetas Fragile, Heavy, Fauna, Burnable y Anchor se implementan con un `[Flags] enum HazardTag` en un componente, no con los Tags de Unity (un objeto puede tener varias).

## 12. Pools
`UnityEngine.Pool.ObjectPool<T>` para VFX, SFX, power-ups, proyectiles y tramos de Infinito. Cero `Instantiate` durante `Running`.

## 13. Guardado (ejemplo)

```json
{
  "version": 1,
  "frijolitos": 340,
  "levels": { "W1-1": { "best": 172.4, "stars": 3, "mangos": 7 } },
  "cosmetics": { "owned": ["hat_pirate"], "equipped": { "p1": { "hat": "hat_pirate", "gas": "gas_rainbow" } }, "raft": "raft_orange" },
  "achievements": ["first_fart", "duet"],
  "stats": { "farts": 1520, "sync2": 80, "sync3": 22, "sync4": 9, "superFarts": 6, "falls": 41, "rescues": 12, "sinks": 15, "meters": 18234, "playSeconds": 9021 },
  "daily": { "lastDate": "20261105", "streak": 3, "best": 412.8 },
  "settings": { "volMaster": 0.8, "shake": true, "textScale": 1.0, "colorblind": false, "difficulty": "Normal" }
}
```
`mangos` es una máscara de bits (3 bits por nivel). Escritura: archivo temporal → reemplazo atómico → copia `.bak`. Si el JSON falla, cargar `.bak`.

## 14. Online (NGO 1.x)

- Anfitrión con autoridad: solo el anfitrión simula la física de la balsa. `NetworkRigidbody` deja la balsa kinematic en clientes; `NetworkTransform` con interpolación.
- Cada cliente envía su `PlayerCommand` en cada tick de red (`TickRate` 30) con `ServerRpc` no confiable; las pulsaciones viajan como contadores (no se pierden).
- Sincronía calculada en el anfitrión con la hora de recepción menos la mitad del RTT del cliente.
- Estado visual replicado: ángulo de asiento (`NetworkVariable<float>`), estado del avatar (`NetworkVariable<byte>`), gas (`NetworkVariable<byte>` 0-255).
- Escenas con `NetworkManager.SceneManager`. Relay: hasta 3 conexiones en cooperativo, 7 en Carrera por Equipos. Lobby con código de 6 caracteres. Autenticación anónima de UGS.
- Si un jugador se desconecta, su animal pasa a bot.
- Requiere vincular el proyecto a Unity Gaming Services (Project Settings > Services).

## 15. Bots (`BotBrain` → `BotCommandSource`)

- Percepción cada 0.1 s: punto objetivo en la `FlowSpline` 2 s adelante, SphereCast de 8 m hacia adelante para peligros, gas propio, estado del equipo.
- Navegación: calcula el rumbo deseado; los bots ocupan asientos traseros y usan toques para corregir el ángulo y ráfagas en rectas.
- Sincronía: cuando un humano inicia un pedo y el bot tiene 20 de gas o más, dispara tras 0.05-0.15 s con probabilidad 0.9 / 0.75 / 0.6 (Familiar / Normal / Amistades Rotas).
- Habilidades: Alex ruge si hay `Fragile` o `Fauna` en el cono; Marty galopa antes de rampas y abismos; Melman engancha si hay ancla y curva cerrada, o rescata a un caído; Gloria hace Panzazo si el giro supera 90°/s o está en un remolino; Julien activa la Fiesta con 2 o más jugadores y el medidor por debajo de 80.
- Se agarran ante avisos de fosas, murciélagos o golpes. Pulsan el Súper Pedo apenas se abre la ventana. Responden a los pings "¡Ahora!" y "Espera".

## 16. Presupuestos de rendimiento

| Métrica | Límite |
|---|---|
| FPS | 60 a 1080p en GTX 1050 Ti / RX 560 |
| CPU por frame | ≤ 12 ms |
| Física | ≤ 3 ms |
| Asignación de memoria (GC) en `Running` | 0 B por frame |
| Triángulos en pantalla | ≤ 500k |
| Texturas | ≤ 2048, atlas para props |
| Partículas activas | ≤ 300 |
| Voces de audio | ≤ 32 |
| Lightmaps por nivel | ≤ 4 de 1024 |

## 17. Convenciones
Detalle en la skill `unity-csharp-2022`. Nombres: clases PascalCase, campos privados `camelCase` con `[SerializeField]`, constantes `UPPER_CASE`, un archivo por clase, namespace `GasSafari.<Carpeta>`.
