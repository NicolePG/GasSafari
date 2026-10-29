---
name: bot-ai
description: Implementar o ajustar los bots que acompañan al jugador (navegación de la balsa, sincronía, habilidades, agarrarse, Súper Pedo, pings) o el modo solo con 2 animales.
---

# Bots

Diseño: docs/10 §15.

## Reglas
1. `BotBrain` produce `PlayerCommand` a través de `BotCommandSource` (misma interfaz que un humano). Nunca toca la física directamente.
2. Decide cada 0.1 s (no cada frame); entre decisiones mantiene el último comando.
3. Percepción sin asignaciones: punto objetivo en `FlowSpline` 2 s adelante; `Physics.SphereCastNonAlloc` de 8 m; lectura de `GameEvents.OnHazardWarning`.
4. Navegación: error de rumbo = ángulo entre la velocidad de la balsa y la dirección al objetivo. Error pequeño → toques del lado correcto; error grande → puntería + ráfaga; recta larga → ráfagas sincronizadas.
5. Asientos: los bots ocupan los asientos traseros libres y se reacomodan cuando cambia el rumbo necesario.
6. Sincronía: al recibir `OnFart` de un humano, si el bot tiene 20 de gas o más, programa su pedo tras 0.05-0.15 s con probabilidad 0.9 / 0.75 / 0.6 según dificultad.
7. Habilidades: reglas por personaje de docs/10 §15 en un método por personaje.
8. Agarrarse ante avisos de `Fauna` o golpes; soltarse 0.5 s después.
9. Súper Pedo: pulsar en cuanto se abre la ventana. Pings: "¡Ahora!" = disparar en el siguiente tick; "Espera" = no disparar 1.5 s.
10. Un bot nunca pasa más de 4 s sin disparar si el equipo está detenido (evita balsas quietas).

## Modo solo con 2 animales
`DualHumanCommandSource`: divide un mando en dos `PlayerCommand` (stick izquierdo + LT/RT para el primero; stick derecho + LB/RB para el segundo; habilidades con X e Y). Ver docs/02 §13.

## Verificación
QA-10 de docs/11: 1 jugador + 3 bots completan W1-1 en Normal por debajo de Plata x1.3.
