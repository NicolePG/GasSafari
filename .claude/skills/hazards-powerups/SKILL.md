---
name: hazards-powerups
description: Programar o ajustar un obstáculo, peligro, jefe o power-up (compuertas, remolinos, pinchos, fauna, géiseres, grúa, tentáculos, cocodrilo gigante, frijoles, locoto, mango, coco, gaseosa, cebolla, mango dorado).
---

# Obstáculos y power-ups

Catálogo con IDs y parámetros: docs/05.

## Obstáculo
1. Clase en `Scripts/Hazards/` que hereda de `HazardBase` (MonoBehaviour). Sobrescribir lo que aplique: `OnRaftEnter`, `OnRaftStay`, `OnRaftExit`, `OnAvatarHit`, `Warn()`.
2. Parámetros en un `HazardConfig` (`Data/Hazards/H<id>_<nombre>.asset`) con los valores de docs/05. Velocidad multiplicada por `DifficultyData.hazardSpeed`.
3. Etiquetas con el componente `HazardTag` (flags: Fragile, Heavy, Fauna, Burnable, Anchor).
4. Daño a la balsa solo con `RaftChambers.PopChamberAt(punto)`; respeta escudo de coco y reglas de dificultad.
5. Empujes con `RaftController.ApplyThrust(..., ForceMode.Impulse)`.
6. Movimiento determinista (seno, curva de animación o secuencia). Nada de `Random` en tiempo real; si hace falta variedad, semilla fija por nivel.
7. Aviso de 0.5 s o más: llamar a `Warn()` (sonido de aviso, sombra o parpadeo) y `GameEvents.OnHazardWarning` (lo usan el HUD, Melman y los bots).
8. Fauna: estado `Scared` durante el tiempo que indique quien la espanta (Rugido 4 s, cebolla mientras dure).
9. Jefes: secuencia por fases con Timeline o una máquina de estados simple; las fases se reinician al reaparecer en el checkpoint.
10. Prefab en `Prefabs/Hazards/H<id>_<Nombre>.prefab` con capa `Hazard`, sonido y VFX asignados.

## Power-up
1. Clase que hereda de `PowerUpBase` con `Apply(PowerUpContext ctx)` (avatar que lo tomó, balsa, equipo).
2. Datos en `PowerUpData` (`Data/PowerUps/`).
3. Trigger con capa `PowerUp`; flota con seno (0.2 m, 1 Hz) y gira despacio (en `Update` sin asignaciones).
4. Se obtiene y devuelve con pool. Reaparece al reiniciar desde un checkpoint previo, salvo P07.
5. Efectos con duración: componente temporal en la balsa o el avatar que se quita solo.
6. `GameEvents.OnPowerUp`, sonido `pickup_*`, VFX `vfx_pickup` con el color del jugador.

## Verificación
Escena `Test/Sandbox_Hazards`: un ejemplar de cada obstáculo nuevo, probado con balsa y con 2 jugadores.
