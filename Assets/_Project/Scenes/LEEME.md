# Organización de las escenas

```
Scenes/
├── Juego/        escenas del juego final (únicas en Build Settings)
│   ├── 00_Boot.unity        crea los servicios y carga el título
│   ├── 01_Titulo.unity      título 3D animado; el menú, modos, selección, mapa, tienda, opciones y carga son paneles de MenuApp
│   ├── Niveles/             N00_T0_PiscinaZoo ... N30_W6-5_CoronaRey (31 niveles, en orden de juego)
│   ├── Fiesta/              minijuegos de Fiesta (aún vacía)
│   └── Infinito/            Infinito y Desafío Diario (aún vacía)
└── Pruebas/      todo lo que no se publica, con prefijo TEST_
    ├── TEST_LevelTemplate.unity   plantilla de la que salen los niveles
    ├── TEST_Sandbox_Raft.unity    física de la balsa
    └── TEST_Sandbox_Hazards.unity un ejemplar de cada obstáculo
```

## Reglas

- **Nombre de nivel:** `N<índice>_<ID>_<Nombre>`. El índice (00-30) sigue el orden de juego: N00 = T-0, N01-N05 = W1-1..W1-5, N06-N10 = W2, N11-N15 = W3, N16-N20 = W4, N21-N25 = W5, N26-N30 = W6. El nombre exacto está en `docs/04-niveles.md` y en `LevelData.sceneName` (Data/Levels).
- **Build Settings:** solo `Juego/`, con `00_Boot` primero. Las escenas `TEST_*` nunca se agregan; las pruebas las abren por ruta (`EditorSceneManager.LoadSceneAsyncInPlayMode`).
- **Mover o renombrar** siempre con el editor (`AssetDatabase.MoveAsset` o arrastrando en el Project) para no romper el GUID; luego actualiza `LevelData`, `LevelSeedTable`, las pruebas y docs/04.
- **Escena nueva de nivel:** duplica `Pruebas/TEST_LevelTemplate` hacia `Juego/Niveles/` con el nombre de la regla anterior (skill `level-builder`).
- No cambies una escena abierta en Unity desde fuera del editor (aparece un diálogo modal que bloquea las herramientas MCP).
