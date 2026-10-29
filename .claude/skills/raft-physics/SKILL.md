---
name: raft-physics
description: Implementar o ajustar la física de la balsa y de los animales en ella (flotación, cámaras de aire, asientos, gas, pedos, sincronía, Súper Pedo, corriente, caídas, vuelco). También para diagnosticar problemas de manejo.
---

# Física de la balsa

Valores: docs/02 §14 (`BalanceConfig`) y docs/03 (`CharacterData`). No escribir números en el código.

## Reglas de implementación
- Una sola entrada de fuerzas: `RaftController.ApplyThrust(Vector3 fuerza, Vector3 punto, ForceMode modo)`. Todo (pedos, habilidades, obstáculos) pasa por ahí para poder aplicar multiplicadores de modificadores y red.
- Rigidbody de la balsa: Interpolate, ContinuousDynamic, `centerOfMass = BalanceConfig.centerOfMassOffset`, masa = base + pesos a bordo (recalcular al subir o caer un animal).
- Flotación por 4 puntos (docs/02 §1). La cámara reventada solo cambia el factor de su punto.
- Pedo: dirección = desde el asiento hacia el centro (plano XZ), rotada por `aim * maxAimAngle` alrededor de Y. Toque y cargado con `ForceMode.Impulse`; ráfaga con `ForceMode.Force` en `FixedUpdate`.
- En el aire (`RaftBuoyancy.IsAirborne`) multiplicar por `airControlFactor`.
- Corriente y viento con `ForceMode.Acceleration` desde `RaftFlowReceiver`.
- `SyncDetector.Register(avatar, tipo, impulsoBase)` devuelve el multiplicador y aplica el bonus retroactivo a los miembros anteriores del grupo (docs/02 §5). Un mismo avatar no cuenta dos veces.
- Los animales sentados no tienen física propia; al caer al agua se les activa un Rigidbody y la capa `CharacterInWater`.
- La cámara lenta solo con `TimeController` (ajusta `fixedDeltaTime`).

## Diagnóstico rápido
| Síntoma | Ajuste (uno por prueba) |
|---|---|
| Gira demasiado | Subir `waterAngularDrag` o bajar `maxAimAngle` |
| Muy lenta | Subir `blastForce` del personaje; no bajar `waterDrag` |
| Vibra en el agua | Bajar `buoyancyPerPoint` o subir `floatDepthRange` |
| Se hunde demasiado | Subir `buoyancyPerPoint` |
| Vuelca fácil | Bajar más el centro de masa o `seatTiltFactor` |
| Sync casi imposible | Revisar `syncWindow` de la dificultad activa |
| Atraviesa paredes a alta velocidad | Confirmar ContinuousDynamic y colliders con grosor ≥ 0.3 m |

Cada ajuste se anota en docs/02 §14 si cambia el valor oficial.

## Verificación
Escena `Test/Sandbox_Raft` y pruebas T-RAFT, T-GAS, T-SYNC de docs/11.
