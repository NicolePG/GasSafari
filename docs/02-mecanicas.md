# 02 · Mecánicas

Unidades: metros, segundos, kilogramos, newtons. Todos los valores viven en `BalanceConfig` (SO global) y `CharacterData` (SO por personaje). La sección 14 es la tabla maestra.

## 1. La balsa

- Rigidbody circular (radio 2 m, alto 0.6 m). Masa = `raftBaseMass` + suma de `weight` de los animales a bordo.
- 4 cámaras de aire (cuadrantes NE, NO, SE, SO), cada una con un punto de flotación.
- Flotación por punto: `fuerza = Clamp01(profundidad / floatDepthRange) * buoyancyPerPoint * factorCámara`, aplicada con `AddForceAtPosition` en `FixedUpdate`.
- Cámara reventada: `factorCámara = poppedFactor` (0.4). La balsa se inclina hacia ese lado.
- En el agua: `drag = waterDrag`, `angularDrag = waterAngularDrag`. En el aire: `airDrag`.
- Centro de masa bajado a `(0, -0.3, 0)` para que no vuelque fácil.
- Inclinación por peso: cada animal aplica `weight * 9.81 * seatTiltFactor` hacia abajo en su asiento. Un animal pesado hunde su lado. Solo en el agua: en el aire no se aplica (giraba la balsa en cada salto). Es intencional pero sutil: objetivo de 1-3° por animal y máximo 6° de cabeceo con 4 animales empujando a fondo; la proa nunca se mete bajo el agua (valores medidos en §14).
- Altura del agua: no es fija. La flotación y el nado consultan `WaterSurface.GetHeight(x, z)` (raycast contra la capa Water); en toboganes, espirales y cascadas el agua sigue la rampa. Fuera de un tramo se usa el nivel base del nivel.
- Corriente: `FlowSpline` devuelve dirección (tangente de la spline) e intensidad del tramo; se aplica con `ForceMode.Acceleration` (no depende de la masa).
- Volcar: si la balsa supera 70° de inclinación durante 1.5 s, el equipo reaparece en el checkpoint.
- Punto de empuje de los pedos (toque, ráfaga, cargado y Galope): el asiento proyectado en el plano horizontal del centro de masa. Así el pedo gira la balsa (par en Y) pero no la hace cabecear; empujar a la altura del asiento volcaba la balsa por la proa con 4 ráfagas en sync (medido: 830 N → 1140 N·m de cabeceo, vuelta completa en 0.3 s a 10 m/s).
- Estabilidad en el agua (`RaftStabilizer`, nunca en el aire): par de enderezado suave cuando la inclinación supera `rightingStartAngle`, proporcional a lo que la supera (`rightingStrength` rad/s² por radián, `ForceMode.Acceleration`), y velocidad horizontal limitada a `maxWaterSpeed`.

## 2. Asientos y puntería

- Cada animal ocupa un ángulo en el borde (0-360°). Se mueve con el stick izquierdo a `seatMoveSpeed` (120°/s).
- Separación mínima entre animales: `minSeatSeparation` (45°). Si choca, se detiene.
- El gas sale hacia afuera del borde; la fuerza empuja la balsa hacia el centro.
- Puntería con el stick derecho: rota el trasero hasta ±`maxAimAngle` (40°). Un empuje inclinado no pasa por el centro y genera giro.
- Posiciones iniciales: 1 jugador: 180° (atrás). 2: 150° y 210°. 3: 120°, 180°, 240°. 4: 90°, 150°, 210°, 270°. (0° = proa, sentido horario visto desde arriba).

## 3. Pedos

| Tipo | Entrada | Costo de gas | Fuerza | Uso |
|---|---|---|---|---|
| Toque | Pulsar y soltar en menos de 0.15 s | 5 | Impulso `puffImpulse` (base 20) | Correcciones |
| Ráfaga | Mantener | 25 por segundo | Fuerza continua `blastForce` (base 120) | Velocidad |
| Cargado | Mantener "Aguantar" de 0.5 a 1.5 s, soltar con el botón de pedo | 35 | Impulso de 50 (0.5 s) a 100 (1.5 s), lineal | Saltos, escapes, Súper Pedo |

- Mientras carga, el animal infla cachetes y ojos (animación `Charge`) y no puede hacer toques ni ráfagas.
- Si suelta "Aguantar" sin pulsar pedo, la carga se pierde (el gas no se gasta).
- Sin gas suficiente: no hay empuje, sale un humito y el animal tose `emptyCoughTime` (1 s).

## 4. Gas

- Tanque por animal: `gasCapacity` (por personaje).
- Recarga: `regenPerSecond` tras `regenDelay` sin disparar (por personaje).
- El gas nunca baja de 0 ni supera la capacidad.
- HUD: anillo alrededor del animal y barra en la esquina del jugador.

## 5. Sincronía

- Grupo de sincronía: inicios de pedo (toque, inicio de ráfaga, liberación de cargado) de jugadores **distintos** dentro de `syncWindow` (0.25 s en Normal).
- Multiplicador según tamaño del grupo: 2 = x1.3, 3 = x1.6, 4 = x2.0.
- Aplicación: el pedo que completa el grupo recibe el multiplicador. Los que ya habían disparado en esa ventana reciben un impulso extra `(mult - 1) * su impulso base` en su asiento. Una ráfaga que entra en un grupo mantiene el multiplicador hasta 2 s.
- Medidor de equipo: +10 / +15 / +20 por grupo de 2 / 3 / 4.
- Feedback: texto "¡SYNC x2!", destello del color del equipo, pedo más fuerte en el mix.
- Bots: si un humano dispara, un bot puede unirse en 0.05-0.15 s (ver docs/10, Bots).

## 6. Medidor de equipo y Súper Pedo Safari

- Medidor 0-100. Sube por sincronías (+10/+15/+20), mango dorado (+15), habilidad Fiesta Real (x2 durante su efecto).
- Con el medidor en 100 aparece "¡SÚPER PEDO LISTO!". Cada jugador pulsa el botón de equipo; la ventana se abre con el primero y dura `superFartWindow` (1 s).
- Potencia = jugadores que pulsaron / jugadores a bordo (bots cuentan y pulsan solos).
- Lanzamiento: velocidad hacia adelante `superFartForward` (14 m/s) y hacia arriba `superFartUp` (9 m/s), escaladas por la potencia. A potencia completa: arco de unos 25 m. Cámara lenta al 40 % durante 0.6 s al despegar.
- Gasta el medidor completo. Algunos abismos (W4-3, W5-2, W6-5) lo exigen.

## 7. Agarrarse

- Mantener el botón: el animal se sujeta a la cuerda. No cae al agua por golpes, pero no puede disparar ni moverse por el borde.
- Soltar es instantáneo.

## 8. Cámaras de aire

- Daño: pinchos, mordidas, golpes con velocidad de impacto mayor a `chamberDamageSpeed` (7 m/s en Normal; componente de la velocidad contra la superficie, como `knockoffSpeed`) en un cuadrante. Revienta la cámara de ese cuadrante. Pinchos y mordidas revientan siempre; en Familiar los golpes no dañan.
- Coco escudo: absorbe el siguiente daño.
- Reparación: pasiva de Melman (1 por checkpoint) o al reaparecer.
- 0 cámaras: la balsa se hunde en 1.5 s (animación) y el equipo reaparece en el checkpoint.

## 9. Caída, nado y rescate

- Un animal que no está agarrado cae al agua solo si la balsa golpea una pared, orilla o prop a más de `knockoffSpeed` (6 m/s, componente de la velocidad contra la superficie) o si la balsa vuelca de su lado. Un roce a lo largo de una pared o un choque a velocidad normal no tira a nadie. Sale con una patada corta (máx. 3 m/s hacia afuera) y cae junto a la balsa.
- En el agua: nada con el stick izquierdo a `swimSpeed` (3 m/s); a menos de 1 m del borde pulsa A/Cruz para subir (animación `Climb`).
- El caído nunca desaparece: se ve nadando junto a la balsa (si cae fuera del agua, se le pone pegado a la balsa), la cámara lo encuadra (se abre hasta +6 m según su distancia) y, si sale de cámara, aparece su icono (color del jugador y retrato) en el borde de la pantalla apuntando hacia él.
- Rescate: Cuello Gancho de Melman, o la balsa pasa por encima del caído (se sube solo).
- Si en `autoRespawnTime` (6 s) no volvió, reaparece sobre la balsa al instante, con un rebote (cae desde 1.2 m y rebota 0.35 m, 0.55 s en total) y el 50 % de gas, sin fundido ni carga. Solo el hundimiento o el vuelco de la balsa usan el fundido de 0.4 s (§10), y tampoco recargan la escena.
- Estadística: cada caída suma para el premio "Capitán Titanic".

## 10. Checkpoints, reaparición y cronómetro

- Checkpoint cada 45-60 s de recorrido (definido por nivel en docs/04). Guarda posición, rotación y estado de cámaras.
- Reaparición: fundido a negro 0.4 s, balsa en el checkpoint, velocidad 0, gas de todos al 100 %.
- Penalización por reinicio: +3 s en Normal (ver dificultad en docs/06). No impide terminar.
- Reinicio manual al checkpoint: lo pide un jugador desde pausa y se aplica si la mayoría acepta (en solo, inmediato).
- Cronómetro: empieza en la puerta de salida tras la cuenta 3-2-1, se detiene en la meta.

## 11. Pings de intención

| Cruceta | Ping | Visual |
|---|---|---|
| Arriba | "¡Ahora!" | Exclamación naranja sobre el animal |
| Abajo | "Espera" | Mano abierta |
| Izquierda / Derecha | "Gira a la izquierda/derecha" | Flecha curva |
| Automático | "Sin gas" | Lata vacía cuando el gas baja de 10 % |

- Si 2 o más jugadores marcan "¡Ahora!" en 2 s, aparece una cuenta 3-2-1 en pantalla (0.5 s por número) y en "¡YA!" la ventana de sincronía se amplía +0.1 s.
- Cooldown por jugador: 1 s.

## 12. Cámara

- Cinemachine 2.x: `CinemachineVirtualCamera` con Framing Transposer siguiendo un `CinemachineTargetGroup` (balsa peso 2, animales en el agua peso 1).
- Inclinación 50°, distancia 14 m (balsa sola) a 18 m (alguien en el agua), anticipación en la dirección de la corriente de 0.3 s.
- Zoom out suave con la velocidad (+2 m a máxima velocidad). Sacudidas con Cinemachine Impulse (desactivables en opciones).
- Pantalla dividida solo en Carrera por Equipos local.

## 13. Controles

| Acción | Mando (Xbox / PlayStation) | Teclado (1 jugador) |
|---|---|---|
| Moverse por el borde | Stick izquierdo | A / D |
| Apuntar (±40°) | Stick derecho | Q / E |
| Pedo (toque / ráfaga) | RT / R2 | Espacio |
| Aguantar (cargar) | LT / L2 | Shift izquierdo |
| Habilidad | Y / Triángulo | F |
| Agarrarse | B / Círculo (mantener) | Ctrl izquierdo |
| Súper Pedo | LB + RB / L1 + R1 | R |
| Pings | Cruceta | 1, 2, 3, 4 |
| Nadar / subir / salto de Julien | Stick izq. + A / Cruz | WASD + C |
| Pausa | Start / Options | Esc |

- Unirse a la partida: pulsar A/Cruz (o Espacio) en la pantalla de selección.
- Remapeo completo desde Opciones (Input System rebinding).

### Control doble (un mando, dos animales)
Solo existe con mando, no con teclado. Para jugar solo con 2 animales o con otro jugador a mitad de mando.
- **Animal A = mitad izquierda del mando:** stick izquierdo para moverse por el borde, LT pedo (toque o ráfaga), LB aguantar, X habilidad, L3 salto de Julien.
- **Animal B = mitad derecha:** stick derecho, RT pedo, RB aguantar, Y habilidad, R3 salto de Julien.
- **Puntería fija en 0 para los dos:** la balsa se gira disparando uno u otro animal.
- **B / Círculo (mantener):** se agarran los dos.
- **A / Cruz:** si alguno de los dos está en el agua, sube a la balsa; si no, es el Súper Pedo y cuenta como pulsado por los dos animales.
- Los pedos de A y B sí forman grupo de sincronía entre sí.
- **Cruceta:** pings, un solo ping para el jugador (valen para los dos animales). **Start:** pausa.
- B ocupa el siguiente asiento trasero libre (se une como un jugador más, con su propio color). Los bots compañeros usan la dificultad Normal (`BotConfig.dualControlSyncChance` = 0.75).

## 14. Tabla maestra de balance (BalanceConfig)

| Clave | Valor | Notas |
|---|---|---|
| raftBaseMass | 10 | kg |
| raftRadius | 2.0 | m |
| floatDepthRange | 0.5 | m de profundidad para flotación completa |
| buoyancyPerPoint | 150 | N por punto (4 puntos) |
| poppedFactor | 0.4 | flotación de una cámara reventada |
| waterDrag / waterAngularDrag | 1.2 / 1.5 | |
| airDrag / airAngularDrag | 0.05 / 0.3 | |
| centerOfMassOffset | (0, -0.3, 0) | |
| seatTiltFactor | 0.3 | medido 2026-11-08: un animal inclina la balsa 0.7° (Marty) a 1.4° (Gloria); 4 animales 2.8° quietos; empujando a fondo desde la popa el cabeceo llega a 3.4° y la proa no se hunde (máx. permitido 6°). Con 0.4 los bots de W1-1 y W2-2 dejaron de terminar el nivel, así que se queda en 0.3 |
| floatPoints (y local) | -0.6 | puntos de flotación 0.6 m bajo el origen de la balsa (antes -0.3): con 4 animales el borde más bajo del piso queda a 0.17 m sobre el agua (las olas visuales llegan a 0.12 m); antes quedaba bajo el agua |
| waterMask (altura / radio) | 0.35 m / 0.95 | `RaftWaterMask`: la malla interior escribe profundidad sin color y se dibuja antes que el agua; también en las 6 balsas de la tienda y las mini balsas |
| capsizeAngle / capsizeTime | 70° / 1.5 s | |
| rightingStartAngle | 25° | desde aquí actúa el enderezado (solo en el agua) |
| rightingStrength | 8 | rad/s² por radián de inclinación sobre 25° |
| maxWaterSpeed | 12 m/s | velocidad horizontal máxima en el agua (en el aire no se limita) |
| puffMaxPressTime | 0.15 s | |
| puffCost / blastCostPerSecond / chargedCost | 5 / 25 / 35 | |
| chargedImpulseMin / Max | 50 / 100 | |
| chargeTimeMin / Max | 0.5 / 1.5 s | |
| maxAimAngle | 40° | |
| seatMoveSpeed / minSeatSeparation | 120°/s / 45° | |
| emptyCoughTime | 1.0 s | |
| syncWindow | 0.25 s | por dificultad (docs/06) |
| syncMultipliers | 1, 1, 1.3, 1.6, 2.0 | índice = jugadores |
| syncMeterGain | 0, 0, 10, 15, 20 | |
| syncBlastBonusTime | 2 s | |
| teamMeterMax | 100 | |
| superFartWindow | 1.0 s | |
| superFartForward / Up | 14 / 9 m/s | a potencia completa |
| superFartSlowMo | 0.4 de escala durante 0.6 s | |
| chamberDamageSpeed | 7 m/s | velocidad de impacto contra la superficie, por dificultad (docs/06 §3). Antes `chamberDamageImpulse` 25 N·s (≈1.2 m/s con una balsa de 21 kg: cualquier roce reventaba cámaras); se mide en velocidad para que no dependa de la masa |
| knockoffSpeed | 6 m/s | velocidad de impacto contra la superficie (antes `knockoffImpulse` 12 N·s, que con una balsa de 21 kg equivalía a 0.6 m/s y tiraba a todos en cada roce). 6 m/s ≈ 129 N·s con 4 animales (21.5 kg); se mide en velocidad para que no dependa de cuántos van a bordo |
| swimSpeed | 3 m/s | |
| autoRespawnTime | 6 s | |
| respawnFade | 0.4 s | |
| airControlFactor | 0.5 | los pedos en el aire aplican 50 % |
| landingBounceImpulse | 15 | impulso vertical de rebote al aterrizar torcido (§15) |
| pingCooldown | 1 s | |

## 15. Física en el aire

- En el aire (ningún punto de flotación sumergido) los pedos aplican `airControlFactor` (50 %) de su fuerza: sirven para girar y corregir el aterrizaje.
- Aterrizaje torcido (más de 35°) en una caída: impulso de rebote (`landingBounceImpulse`) y riesgo de caída de animales no agarrados.
