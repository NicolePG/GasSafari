# 05 · Obstáculos y power-ups

Reglas comunes:
- Todo peligro avisa al menos 0.5 s antes (sonido, sombra, parpadeo o movimiento previo).
- Movimientos con patrones fijos (seno, curva o secuencia), nunca aleatorios en tiempo real, para que se puedan aprender.
- Daño a la balsa solo con `RaftChambers.PopChamberAt(puntoMundial)`. Empujes con `ForceMode.Impulse`.
- Etiquetas que usan las habilidades: `Fragile` (Rugido rompe), `Heavy` (Terremoto rompe), `Fauna` (Rugido y cebolla espantan), `Burnable` (locoto quema), `Anchor` (Cuello Gancho).
- Configuración de cada tipo en un `HazardConfig` (SO); los valores por nivel se ajustan en el prefab.

## Obstáculos

| ID | Nombre | Comportamiento | Parámetros base | Cómo se supera | Etiquetas | Primer nivel |
|---|---|---|---|---|---|---|
| H01 | Pared / orilla | Rebote físico (material con rebote 0.3) | — | Toques para alinear | — | T-0 |
| H03 | Compuerta temporizada | Abre y cierra en ciclo; cerrada empuja hacia atrás | ciclo 2-3 s, abierta 50 % | Mirar un ciclo y cruzar en sync | — | W1-2 |
| H04 | Oleaje | Ola que levanta y empuja | amplitud 0.6 m, empuje 20 | Cortar la ola de frente | — | W1-3 |
| H05 | Bloqueador móvil | Contenedor o plataforma que se desliza | velocidad 2 m/s, golpe 30 | Leer el patrón | Heavy | W1-5 |
| H06 | Barril explosivo | Al tocarlo explota tras 1.5 s (radio 4 m) | revienta 1 cámara, empuje 35 | Alejarse o romper antes | Fragile | W2-1 |
| H07 | Caja rompible | Frena; se rompe con Rugido o impacto mayor a 20 | — | Rugido o golpe fuerte | Fragile | W1-4 |
| H08 | Viento | Fuerza lateral por ráfagas con aviso 1 s antes | 3-5 m/s² de aceleración, 2 s | Gloria a bordo, empuje contrario | — | W2-2 |
| H09 | Remolino | Atrae al centro y hace girar | radio 6-15 m, atracción 4 m/s², giro 60°/s | Sync fuerte hacia afuera o Panzazo | — | W2-4 |
| H10 | Pinchos | Revientan la cámara que toca | — | Rodear; gaseosa pasa por encima de los bajos | — | W2-5 |
| H11 | Banco de arena | Debajo de 3 m/s la balsa encalla 2 s | — | Llegar con velocidad | — | W3-1 |
| H12 | Liana | Muro que bloquea | se quema en 0.5 s | Locoto | Burnable | W3-2 |
| H13 | Tronco giratorio | Gira en el agua y golpea | 90°/s, golpe 25 | Pasar en el hueco | Fragile si está podrido | W3-2 |
| H14 | Fosa | Salta desde la orilla y tira a un animal al agua | aviso: arbusto se mueve 1 s antes | Agarrarse, Rugido, cebolla | Fauna | W3-3 |
| H15 | Cocodrilo | Se acerca y muerde: revienta 1 cámara | despierta con pedos fuertes a menos de 8 m | Toques suaves, Rugido, cebolla | Fauna | W3-4 |
| H16 | Obstáculo rítmico | Se mueve al compás de la música | 110 o 128 BPM | Disparar en el pulso | — | W3-5 |
| H17 | Cascada / caída | Caída vertical | 3-20 m | Alinear antes del borde | — | W4-1 |
| H18 | Zona oscura | Visibilidad 6 m | — | Luciérnagas, Melman, pings | — | W4-2 |
| H19 | Géiser | Lanza la balsa hacia arriba | ciclo 3 s, velocidad 12 m/s | Usarlo de trampolín | — | W4-3 |
| H20 | Roca que cae | Golpe desde arriba con sombra de aviso 1 s | revienta 1 cámara | Esquivar la sombra | Heavy | W4-5 |
| H21 | Rápidos | Corriente muy fuerte con rocas | 8 m/s² | Ráfagas cortas y sync | — | W5-1 |
| H22 | Abismo | Hueco; caer = reaparición. Rampa de despegue antes; si el equipo cae 2 veces en el mismo abismo aparece un puente de boyas | 10 m en W4-3; 15-25 m en W5-2 y W6-5 | Súper Pedo (el medidor se llena con H36 justo antes) | — | W4-3 |
| H23 | Cocodrilo gigante (jefe) | Persigue; si alcanza, reaparición | velocidad = 90 % de la media del equipo | No detenerse; Rugido lo frena 2 s | Fauna | W5-5 |
| H24 | Grúa con gancho (jefe) | El gancho cae donde marca la sombra roja | aviso 1 s, cada 2-4 s | Esquivar la sombra | — | W1-5 |
| H25 | Tentáculos (jefe) | Golpean el agua en patrón por fases | cada 3 / 2 / 1.5 s, golpe 35 | Leer el patrón | — | W2-5 |
| H26 | Murciélagos | Bandada que tira a un animal al agua | aviso: chillido 1 s | Agarrarse, Rugido | Fauna | W4-4 |
| H27 | Trampolín / toldo / fuente | Rebota la balsa hacia arriba | 10 m/s hacia arriba | Aprovecharlo | — | W1-1 |
| H28 | Barco que cruza | Bloqueador grande + estela de oleaje | — | Pasar antes o esperar | Heavy | W1-3 |
| H29 | Cangrejo | Sube a la balsa y roba 20 de gas por segundo a un animal | se va tras 3 s o al tirarse un pedo encima | Moverse a su lado y disparar | Fauna | W3-1 |
| H30 | Coco que cae | Golpe desde arriba con sombra 0.8 s | empuje 20, sin daño | Esquivar | — | W6-4 |
| H31 | Corriente cruzada | Dos corrientes perpendiculares que chocan | 4 m/s² cada una | Puntería y Gloria | — | W4-4 |
| H32 | Red de pesca | Frena (`waterDrag` x3) mientras la balsa está dentro | — | Rugido la rompe; rodearla | Fragile | W2-1 |
| H33 | Pared de globos | Muro que bloquea el paso; revienta con un impulso mayor a 30 o con un grupo de sync cerca | impulso 30; "cerca" = la balsa dentro de su zona de aviso | Sync o golpe fuerte; Rugido | Fragile | T-0 |
| H34 | Chorro de manguera o fuente | Empuje lateral constante en un tramo de 4 m de ancho; tira al agua a quien no esté agarrado tras 1.5 s dentro | 6 m/s² de aceleración lateral, 1.5 s | Agarrarse y cruzar rápido | — | T-0 |
| H35 | Plataforma que se hunde | Plataforma flotante: empieza a hundirse 1 s después de que la balsa la toca y desaparece bajo el agua en 6 s; reaparece al reiniciar el checkpoint | 1 s de aviso, 6 s de hundimiento | Salir antes de 6 s | — | W1-5 |
| H36 | Géiser de gas dorado | Al pasar la balsa por encima llena el medidor de equipo al 100 %; no daña ni empuja | rellena como mucho 1 vez cada 4 s (`cooldown`) | Pasar por encima justo antes de un abismo que exige Súper Pedo | — | W4-3 |
| H37 | Compuerta de represa | Puerta sólida que cierra el cauce y abre por ciclos; al abrir libera una oleada aguas abajo | cerrada 6 s (avisa 1.5 s), abierta 4 s, oleada 10 m/s² durante 3 s | Subir con la ola o esperar | — | W5-4 |

Parámetros provisionales de los obstáculos de W1 (no están en la tabla; se afinan con QA, viven en `Data/Hazards/H<id>_*.asset`):
- H03: empuje hacia atrás de la compuerta cerrada `pushBack` 12; abre y cierra en 0.25 s; avisa 0.5 s antes de cerrar.
- H04: la cresta recorre el cauce a `crestSpeed` 4 m/s; levanta con un impulso vertical que sube la balsa `amplitude` (m) y empuja `push` en el punto de contacto.
- H05: parada `dwell` de 1 s en cada extremo (avisa 0.5 s antes de arrancar).
- H24: radio del golpe 3 m, empuje 35, la grúa sigue a la balsa a 14 m/s; intervalos 3, 2, 4, 2.5, 2, 3.5 s en bucle (el sprint final fija 2 s); se reinicia al reaparecer.
- H28: velocidad 3 m/s, parada de 2.5 s en cada orilla (aviso), golpe 30; su estela es una cresta de H04 que baja por el cauce.
- H07: el impacto es el impulso del choque en N·s (misma unidad que los golpes de la tabla).
- H33: "cerca" = la balsa a 6 m o menos de la pared (`nearDistance`); suena como la caja (docs/08 no tiene sonido de globos). H34: el empuje es `accel` 6 m/s² hacia el costado del prefab. H35: la posición de reposo se toma al primer toque.
- W2 (M5-08): H06 la mecha es el aviso (warningTime 1.5 s) y la cámara que revienta es la más cercana; suena RaftHitHard (no hay sonido de explosión en docs/08). H08 calma de 3 s entre ráfagas y la fuerza sube de 3 a 5 m/s² y baja dentro de los 2 s (por `RaftFlowReceiver.Wind`). H09 el radio es el del trigger (8 m en el prefab; el nivel lo escala a 6-15) y el giro se acerca a 60°/s con ganancia 2/s. H25 radio del golpe 3 m, aviso 1 s (el tentáculo se alza); la fase la fija `BossPhaseZone`. H32 se suma al arrastre de las habilidades (`RaftBuoyancy.ZoneDragMultiplier`).
- W3 (M5-14): H11 encalla `groundTime` 2 s si entra a menos de `minSpeed` 3 m/s horizontales (frena en seco hasta que pasa el tiempo). H12 reutiliza `BurnableWall` (se quema en 0.5 s). H13 gira 90°/s y empuja con 25 en el sentido del punto que toca (Fragile con `Break`). H14 avisa 1 s con el arbusto y tira al animal suelto más cercano hacia afuera de la balsa (3 m/s + 1.5 de elevación); una vez por pasada. H15: duerme; despierta con una ráfaga o un pedo cargado (no con toques) a menos de 8 m, persigue a 3 m/s, avisa 1 s a 2.5 m y muerde (revienta 1 cámara aunque la dificultad desactive el daño por impactos), se aleja 4 s y vuelve a dormir; la estampida (`CrocStampede`) los despierta a todos. H16: `RhythmClock` (110 BPM, 128 tras `RhythmTempoZone`); la pieza va y vuelve en un compás de 4 pulsos con golpe 20; un pedo a ±0.1 s del pulso suma 10 al medidor (`RhythmSyncBonus`, valor provisional). H29: sube al animal más cercano a su roca, le roba 20 de gas/s durante 3 s y se va antes si ese animal se tira un pedo.
- W4 (M5-20): H17 es un trigger de aviso antes del borde (la caída es agua inclinada del cauce; parámetro `drop`). H18 reduce la niebla a negro con visibilidad 6 m mientras la balsa está dentro; con `blackoutSeconds` apaga las luciérnagas (luces) ese tiempo. H19 avisa con vapor 0.5 s cada 3 s y, si la balsa está encima, la lanza a 12 m/s hacia arriba. H20 avisa con una sombra 1 s (patrón fijo, un punto cada `interval` s, solo con la balsa a menos de 45 m) y revienta 1 cámara si la balsa está a menos de 2.5 m del punto (siempre daña, como pinchos). H22: tramo sin agua (`GreyboxLevel.WaterGap`) más un trigger debajo que pide la reaparición. H26 usa la regla de la fosa (chillido 1 s, tira a un animal suelto; Fauna). H31: aceleración de 4 m/s² hacia `transform.right`; dos zonas perpendiculares forman la corriente que choca. Los pinchos de W4 son `H10_CrystalSpikes` (mismo comportamiento que H10). `CollapseChaser` (derrumbe de W4-5) avanza a 4.5 m/s tras entrar la balsa en su zona y, si la alcanza a 4 m, pide la reaparición.
- W3/W4 (revisión 2026-11-08): H15 despierta con ráfaga, pedo cargado o Súper Pedo a menos de 8 m (`wakeDistance`; los toques no) y el mordisco alcanza desde el borde de la balsa (el collider del cocodrilo le impide acercar su centro a menos de ~3.4 m). H16 en W3-5: `RhythmClock` sigue los pulsos de `mus_w3_boss` (tabla `mus_w3_boss_beats.json`; 110 y luego 128 BPM) y el cambio de canción salta la pista al tramo rápido; `RhythmDrum` (tambor decorado que late en cada pulso) y `RhythmJet` (brota en los pulsos 1 y 3 de cada compás y empuja 4 m/s² hacia `transform.right`; no daña). H18 con `blackoutSeconds` (W4-2): apagón gradual de 1.5 s (las luciérnagas se apagan una a una con parpadeos y el sonido `LightsOut`), 20 s con visibilidad 3 m (`blackoutVisibility`) y luz ambiente al 20 %, y vuelven con `LightsOn`. `CollapseChaser` (W4-5) avisa 2.5 s antes de avanzar: sacudidas de cámara, `RockWarn`, borde rojo que late (`ScreenEdgeWarning`) y 8 rocas que caen detrás de la balsa con `RockImpact` (sin daño). H17 de W3-3: cascada de salida de 6 m con agua inclinada (30 m).
- W5 (M5-26): H21 es una zona `RapidsZone` que acelera la balsa hacia `transform.forward` 8 m/s² x `intensity` (0.5 en los rápidos de clase 1, 1 en clase 2 y el rápido largo); las rocas del cauce son colliders sólidos del nivel (a más de 7 m/s revientan cámaras en Normal). H37 (id provisional: docs/04 las pide sin id) es `DamGate`: puerta sólida + zona de oleada de 24 m aguas abajo; la puerta no cierra mientras la balsa está en el paso; las esclusas de W5-4 S2 son la misma compuerta con oleada débil (x0.3): el nivel del agua no sube ni baja (decisión provisional, pendiente de diseño). H23 es `GiantCrocChaser`: arranca al entrar la balsa en su zona, persigue por el cauce a `speedFactor` 0.9 de la velocidad media del equipo (media móvil de 4 s, entre 3 y 16 m/s), si queda a menos de 5 m pide la reaparición y el Rugido (IScareable) lo frena 2 s; `CrocFinaleZone` lo coloca tras la balsa para el cierre de boca final.
- H22, aviso (2026-11-08): 30 m antes del hueco de cada abismo hay una hilera de boyas rojas que cruza el cauce, boyas guía a los lados, un cartel de peligro a cada orilla y un trigger `AbyssWarning` (hereda de `HazardBase`): al cruzarlo la balsa emite el aviso de peligro del HUD (`GameEvents.OnHazardWarning`, texto "¡Abismo adelante!" `hud.abyss` y borde rojo de pantalla 3 s). Lo arma `AbyssSignage.Add` desde los constructores de W4-3, W5-2, W5-5 y W6-5.
- H30 (M5-32): `CoconutFall` avisa con una sombra de 0.8 s y el coco cae en 0.3 s; si la balsa está a menos de `radius` 2.5 m del punto, la empuja `push` 20 hacia afuera, sin dañar cámaras. Dos modos: puntos fijos (`Configure`) o lluvia sobre la balsa (`ConfigureRain`), donde cada coco cae `aimLead` 0.8 s por delante de la balsa con un desplazamiento lateral de un patrón fijo (solo ~1 de cada 3 cae a menos de 2.5 m). W6-4 usa lluvias de 1.4 a 2.4 s entre cocos.
- Rutas de bots (2026-11-08): `BotHint` tipo Route lleva una lista de puntos; los bots solo la siguen si `BotHint.ForcedRoute` es su id (las pruebas de W5-3 fuerzan "rapida", "mangos" y "atajo"); en el juego siguen el cauce principal.
- Abismos (M5-23): H22 es un `WaterGap` de 10 m en W4-3 con una rampa de despegue antes (el agua sube 1.5 m en 14 m). Justo antes de la rampa (30 m) hay un H36 que llena el medidor; en el despegue hay un `BotHint` tipo `SuperFart` (docs/10 §15): ahí los bots pulsan el Súper Pedo aunque no haya humanos (única excepción a la regla de que los bots solos no lo lanzan). Ayuda: `Abyss` cuenta las caídas del equipo (una por reaparición) y a la 2.ª activa el puente de boyas (`PuenteBoyas`: franja de agua de 8 m de ancho entre dos hileras de boyas; la balsa flota en ella y cruza sin Súper Pedo). W5-2 y W6 usan el mismo conjunto: rampa + H36 + hint + puente.
- H27: si la balsa vuelve a caer al agua dentro de la zona más de 1 s después del rebote (salto fallido), rebota otra vez. La fuente central de W1-1 tiene delante una resaca suave (chorro hacia atrás) que devuelve a la fuente una balsa que no pasó la reja.

## Power-ups

| ID | Nombre | Efecto | Duración | Lo toma |
|---|---|---|---|---|
| P01 | Frijoles | Recarga el 100 % del gas | Instantáneo | El animal más cercano |
| P02 | Locoto | Pedos de fuego: x1.5 de fuerza y queman `Burnable` | 6 s | El animal más cercano |
| P03 | Mango | +50 % de gas a todo el equipo | Instantáneo | Equipo |
| P04 | Coco escudo | Absorbe el siguiente daño a una cámara | Hasta el golpe | Balsa |
| P05 | Gaseosa | Pedos de burbuja: la balsa flota a 1 m sobre el agua | 3 s | Balsa |
| P06 | Cebolla | Nube apestosa de 8 m: la fauna huye | 6 s | Balsa |
| P07 | Mango dorado | Coleccionable (3 por nivel) + 15 al medidor de equipo | Permanente | Equipo |

Reglas:
- Flotan con un movimiento de seno (amplitud 0.2 m, 1 Hz) y giran despacio.
- Se recogen por contacto (trigger) con la balsa o con un animal (salto de Julien).
- Se reutilizan con pool. P01-P06 reaparecen al reiniciar desde el checkpoint previo a su posición; P07 queda recogido.
- Cada power-up tiene un sonido propio y un destello del color del jugador que lo tomó.
