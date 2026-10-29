---
name: ui-hud
description: Crear o modificar pantallas, menús, HUD, selección de personajes, mapa de mundos, opciones, pausa, resultados, viñetas o la unión de jugadores locales.
---

# Interfaz

Diseño completo: docs/07. Premios y resultados: docs/06 §4.

## Reglas
1. UGUI + TextMeshPro. Canvas Scaler: Scale With Screen Size 1920x1080, match 0.5. Prefabs en `Prefabs/UI/`.
2. Navegación con mando: `InputSystemUIInputModule`, `Selectable` con navegación explícita, primer botón seleccionado al abrir cada pantalla.
3. Pantallas: clase base `UIScreen` (Show/Hide con animación) y `ScreenRouter` con pila para "Atrás".
4. El HUD se actualiza por eventos (`GameEvents`, eventos de `GasTank`, `TeamMeter`), nunca buscando objetos en `Update`. Sin asignaciones de strings por frame: el cronómetro usa `TMP_Text.SetText("{0:0}:{1:00}", ...)`.
5. Un panel de HUD por jugador con su color (docs/03 Reglas de selección) y forma en modo daltonismo.
6. Textos desde una tabla de claves (docs/07 §7) en `Data/UI/Strings_es.asset`; nunca textos escritos en el código.
7. Iconos de botones según el último dispositivo de cada jugador.
8. Textos de evento ("¡SYNC x2!"): escala 0 → 1.2 → 1 en 0.25 s; fade de salida 0.4 s; no tapar la balsa más de 1 s.
9. Accesibilidad: respetar tamaño de texto, sacudidas y paleta elegidos en Opciones.

## Unión de jugadores local
`InputHub` con `PlayerInputManager` (Join Behavior: Join Players When Button Is Pressed). En la selección, cada nuevo dispositivo crea un `PlayerSlot`; salir con B/Círculo desde "no listo".

## Viñetas
Timeline con imágenes y textos cortos; saltable manteniendo A/Cruz 1 s.

## Verificación
Navegar cada pantalla solo con mando y solo con teclado; probar 4 jugadores; probar texto al 150 %.
