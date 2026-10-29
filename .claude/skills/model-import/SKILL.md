---
name: model-import
description: Importar y preparar modelos 3D (FBX/GLB de Meshy, Mixamo, Blender o packs CC0): escala, rig Humanoid o Generic, materiales toon URP, animaciones, Animator Override Controller y prefabs.
---

# Importar modelos

Lista de modelos, presupuestos y flujo: docs/09 §3-4. Animaciones obligatorias: docs/03 "Animaciones".

## Personajes
1. Archivos en `Art/Characters/<Nombre>/` (modelo, texturas, animaciones). Animaciones de Mixamo en `Art/Characters/<Nombre>/Anims/`.
2. Model tab: Scale Factor tal que la altura coincida con `CharacterData.height` (medir con un cubo de 1 m). Read/Write desactivado. Mesh Compression Low.
3. Rig: **Humanoid** si es bípedo con esqueleto estándar (Meshy o Mixamo). Revisar el Avatar (Configure): huesos en verde. Cuello largo de Melman: verificar Neck/Head; colas fuera del Avatar (animadas por código).
4. Animation: Loop Time en los clips de loop (docs/03); Root Transform Rotation/Position "Bake Into Pose" para clips sentados.
5. Materiales: Extract Materials a `Art/Characters/<Nombre>/Materials/` y cambiar al shader `SG_Toon`. Texturas de 2048 como máximo, compresión normal.
6. Controlador: `Art/Characters/_Shared/CharacterBase.controller` con estados de docs/03 y parámetros (`State` int, `Seat` float, `Charge` float, triggers). Cada personaje: Animator Override Controller con sus clips.
7. Prefab `Prefabs/Characters/<Nombre>.prefab`: modelo + `PlayerAvatar`, `PlayerSeat`, `GasTank`, `FartThruster`, `AbilityRunner`, pasiva, `FartJuice`, `CharacterAnimator`, punto `HatSocket` en el hueso Head, collider cápsula (capa Character).
8. Presupuesto: 15k triángulos por personaje. Si se pasa, reducir en Blender (Decimate) antes de importar.

## Props y escenario
- Generic o sin rig. Static si no se mueve (para lightmaps). Generate Lightmap UVs activado.
- Collider: primitivas o MeshCollider convexo simplificado; nunca el mesh visual de alto detalle.
- Materiales a `SG_Toon`.

## GLB
Solo con el paquete glTFast. Preferir FBX.

## Créditos
Agregar cada modelo externo a `Assets/_Project/CREDITS.md` (Meshy plan gratis = CC BY 4.0, poner "Meshy").
