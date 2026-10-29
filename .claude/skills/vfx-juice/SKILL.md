---
name: vfx-juice
description: Efectos visuales y sensación de juego (partículas de pedos y habilidades, salpicaduras, sacudidas de cámara, cámara lenta, destellos, animación procedural FartJuice, cámara Cinemachine).
---

# VFX y juice

Lista de efectos: docs/09 §6. Cámara: docs/02 §12. Animación procedural: docs/09 §4.

## Partículas
1. Prefab en `Prefabs/VFX/vfx_<id>.prefab` con Particle System (URP Particles Unlit/Lit). Sin luces en tiempo real dentro del efecto.
2. Reproducir con `VfxPool.Play(id, posición, rotación, color)`; el color del gas viene del cosmético del jugador.
3. Duración corta (< 2 s) y `Stop Action = Callback` para devolver al pool.
4. Presupuesto global: 300 partículas activas.

## Sensación de juego (orden de prioridad)
| Evento | Feedback |
|---|---|
| Toque | Puff + squash del animal 0.1 s + sonido |
| Sync | Destello del color del equipo + texto + pedo más fuerte |
| Súper Pedo | Cámara lenta 0.4x 0.6 s + sacudida fuerte + estela |
| Cámara reventada | Pedazos de goma + sacudida media + la balsa se desinfla en ese cuadrante (escala) |
| Caída al agua | Splash grande + grito + icono del jugador en el borde de pantalla |
| Checkpoint | Bandera + confeti + sonido |
| Meta | Cámara lenta breve + fanfarria |

- Sacudidas con `CinemachineImpulseSource` (respetar la opción de accesibilidad).
- Cámara lenta solo con `TimeController` (ajusta `fixedDeltaTime`); respetar la opción de accesibilidad.

## FartJuice (procedural)
- Toque: escala Y de las caderas 0.85 → 1.1 → 1 en 0.2 s.
- Ráfaga: vibración pequeña (seno 20 Hz, 0.02 m) mientras dura.
- Carga: cabeza escala 1 → 1.15 según la carga + temblor creciente.
- Megapedo: estiramiento fuerte y retroceso 0.15 m.
- Implementado con interpolación propia en `LateUpdate` (después del Animator), sin asignaciones.

## Cámara
`CinemachineVirtualCamera` + Framing Transposer + `CinemachineTargetGroup` (balsa peso 2; animales en el agua peso 1). Distancia 14-18 m, inclinación 50°, anticipación 0.3 s, +2 m a máxima velocidad.
