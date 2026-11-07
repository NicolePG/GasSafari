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
| H22 | Abismo | Hueco; caer = reaparición | 15-25 m | Súper Pedo | — | W4-3 |
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

Parámetros provisionales de los obstáculos de W1 (no están en la tabla; se afinan con QA, viven en `Data/Hazards/H<id>_*.asset`):
- H03: empuje hacia atrás de la compuerta cerrada `pushBack` 12; abre y cierra en 0.25 s; avisa 0.5 s antes de cerrar.
- H04: la cresta recorre el cauce a `crestSpeed` 4 m/s; levanta con un impulso vertical que sube la balsa `amplitude` (m) y empuja `push` en el punto de contacto.
- H05: parada `dwell` de 1 s en cada extremo (avisa 0.5 s antes de arrancar).
- H24: radio del golpe 3 m, empuje 35, la grúa sigue a la balsa a 14 m/s; intervalos 3, 2, 4, 2.5, 2, 3.5 s en bucle (el sprint final fija 2 s); se reinicia al reaparecer.
- H28: velocidad 3 m/s, parada de 2.5 s en cada orilla (aviso), golpe 30; su estela es una cresta de H04 que baja por el cauce.
- H07: el impacto es el impulso del choque en N·s (misma unidad que los golpes de la tabla).
- H33: "cerca" = la balsa a 6 m o menos de la pared (`nearDistance`); suena como la caja (docs/08 no tiene sonido de globos). H34: el empuje es `accel` 6 m/s² hacia el costado del prefab. H35: la posición de reposo se toma al primer toque.
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
