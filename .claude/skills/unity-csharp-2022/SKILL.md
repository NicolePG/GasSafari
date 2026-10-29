---
name: unity-csharp-2022
description: Reglas obligatorias para crear o editar cualquier script C#, paquete o configuración de este proyecto Unity 2022.3.62f2. Úsala antes de tocar un .cs.
---

# C# en Unity 2022.3 (Gas Safari)

## APIs correctas para esta versión
- Rigidbody: `velocity`, `angularVelocity`, `drag`, `angularDrag`. PROHIBIDO `linearVelocity`, `linearDamping`, `angularDamping`.
- Búsquedas: `FindFirstObjectByType<T>()` / `FindObjectsByType<T>(FindObjectsSortMode.None)` solo en inicialización; mejor referencias `[SerializeField]`.
- Cinemachine 2.x: `CinemachineVirtualCamera`, `CinemachineTargetGroup`, `CinemachineImpulseSource`. No usar `CinemachineCamera` (3.x).
- Netcode for GameObjects 1.x: `NetworkBehaviour`, `NetworkVariable<T>`, `[ServerRpc]` / `[ClientRpc]` con sufijo `ServerRpc` / `ClientRpc` (estilo clásico en todo el proyecto, por consistencia). Nada de APIs exclusivas de NGO 2.x (Distributed Authority).
- Input System: `PlayerInput`, `PlayerInputManager`, `InputAction.CallbackContext`.
- Pools: `UnityEngine.Pool.ObjectPool<T>`.
- C# 9 como máximo (sin `required`, sin primary constructors en clases, sin collection expressions `[...]`).

## Estructura
- Archivos en `Assets/_Project/Scripts/<Carpeta>/`, un archivo por clase, namespace `GasSafari.<Carpeta>`.
- `[SerializeField] private` en lugar de campos públicos; propiedades de solo lectura para exponer estado.
- Cachear componentes en `Awake`. Suscribir eventos en `OnEnable` y desuscribir en `OnDisable`.
- Física solo en `FixedUpdate`. Entrada solo a través de `IPlayerCommandSource` (nunca leer el Input System desde la lógica del avatar).
- Sin `GetComponent`, `Find`, LINQ, `new` de clases ni concatenación de strings dentro de `Update`/`FixedUpdate`.
- Valores de balance en ScriptableObjects (`Assets/_Project/Data`); los valores salen de los docs, no se inventan.
- Un script = una responsabilidad. Si pasa de ~200 líneas, dividir.
- Comentarios solo donde el porqué no es obvio. Sin código comentado ni `Debug.Log` sueltos (usar `[Conditional("UNITY_EDITOR")]` para logs de depuración).
- `Time.timeScale` solo lo cambia `TimeController`.

## Al terminar
1. `read_console` filtrando errores (y warnings propios).
2. Si hay errores, corregirlos antes de responder.
3. Responder solo con los archivos que cambiaron y 2 líneas de resumen.
