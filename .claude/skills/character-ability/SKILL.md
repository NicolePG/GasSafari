---
name: character-ability
description: Crear o modificar un personaje (CharacterData), su habilidad activa, su pasiva o una sinergia entre habilidades (Alex, Marty, Melman, Gloria, Rey Julien).
---

# Personajes y habilidades

Fuente de verdad: docs/03 (Grep por el nombre del personaje).

## Personaje
1. `CharacterData` en `Data/Characters/<Nombre>.asset` con los valores exactos de la tabla "Resumen" de docs/03.
2. Referencias: prefab, `AbilityBase`, pasiva, `SfxSet`, Animator Override Controller, icono, color de UI.

## Habilidad activa
1. Clase en `Scripts/Abilities/<Nombre>Ability.cs` que hereda de `AbilityBase` (ScriptableObject).
2. Implementar `CanActivate(AbilityContext ctx)` y `Activate(AbilityContext ctx)`. El contexto trae: avatar, raft, seat, thruster, team (sync, medidor), services.
3. El asset NO guarda estado de partida. Recarga y duración las lleva `AbilityRunner` (MonoBehaviour del avatar). Efectos con duración: corrutina o componente temporal en el avatar o la balsa.
4. Fuerzas solo vía `RaftController.ApplyThrust`. Etiquetas (`Fragile`, `Fauna`...) vía `HazardTag` (docs/10 §11). Búsquedas con `Physics.OverlapSphereNonAlloc` y buffer prealocado.
5. Disparar `GameEvents.OnAbilityUsed`, sonido (`ability_*`) y VFX (`vfx_*`).
6. Asset en `Data/Abilities/` asignado al `CharacterData`.

## Pasiva
- Componente que hereda de `PassiveBase` en el prefab del personaje. Modifica valores al iniciar o escucha eventos; nunca sondea en `Update`.
- Marty no necesita componente: su pasiva son sus datos.

## Sinergias
- `AbilitySynergySystem` escucha `OnAbilityUsed` y `OnSync`, guarda las últimas activaciones con su tiempo y evalúa las condiciones de la tabla de docs/03. Al cumplirse: efecto, `GameEvents.OnSynergy`, texto en HUD.

## Bots
Toda habilidad nueva necesita su regla de uso en `BotBrain` (docs/10 §15).

## Verificación
Prueba T-ABIL-01 (recarga) + probar en `Sandbox_Raft` con 1 y 2 jugadores.
