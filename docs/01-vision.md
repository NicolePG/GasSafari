# 01 · Visión del juego

## Pitch

Cinco animales escapados del zoológico, una balsa inflable y ningún remo. La única forma de volver a casa es tirarse pedos... y hacerlo al mismo tiempo.

## Ficha

| Campo | Valor |
|---|---|
| Título | GAS SAFARI: ¡Todos a la Balsa! |
| Género | Cooperativo de físicas, party game, carreras de obstáculos. 3D, cámara en tercera persona elevada |
| Jugadores | 1 a 4 en la misma balsa (local en una pantalla u online). 1 jugador + bots. Hasta 8 en Carrera por Equipos online (2 balsas) |
| Plataforma | PC Windows. Mando Xbox / DualSense y teclado |
| Motor | Unity 2022.3.62f2 LTS, URP |
| Público | 10+ y familiar. Humor ligero, sin violencia |
| Contenido | Tutorial + 6 mundos x 5 niveles = 31 niveles; 8 modos; 5 minijuegos de fiesta |
| Duración | Historia: 4-5 h la primera vez. Completar todo (93 estrellas, 93 mangos dorados, logros): 8-10 h. Modos rejugables sin fin |
| Referente | Fart Together (GamesMrkt, Steam, acceso anticipado 29/10/2026): 1 circuito, hipopótamos iguales, solo online |
| Personajes | Alex (león), Marty (cebra), Melman (jirafa), Gloria (hipopótama), Rey Julien (lémur). Uso no comercial |

## Pilares (toda decisión se valida contra esto)

1. **Coordinar ES la mecánica.** Dos jugadores sincronizados siempre rinden más que uno solo empujando fuerte.
2. **Cada animal se siente distinto.** Stats, peso, sonido y una habilidad propia; elegir personaje cambia la estrategia.
3. **Fallar es gracioso.** Muñeco de trapo, cámara lenta, repetición "¿Quién fue?", premios de vergüenza.
4. **Variedad constante.** Algo nuevo cada 30 s; una mecánica nueva por nivel; un ambiente por mundo.
5. **Nadie se queda afuera.** Juega solo con bots, sin micrófono con pings, en Familiar con ayudas.

## Historia (se cuenta con viñetas cortas de 3 a 5 imágenes entre mundos, saltables)

| Momento | Viñeta |
|---|---|
| Intro | Noche en el zoológico de la ciudad. Los cinco se escapan, llegan al muelle y solo encuentran una balsa inflable y un barril de frijoles. Descubren el "método". |
| Fin Mundo 1 | Escapan del puerto escondidos tras un barco de carga. Mar abierto por delante. |
| Fin Mundo 2 | La tormenta los deja en la playa de una isla tropical. |
| Fin Mundo 3 | Ganan la fiesta de los lémures; el Rey Julien se une "oficialmente" (antes iba de polizón) y les señala el volcán como atajo a casa. |
| Fin Mundo 4 | Salen disparados del volcán sobre la balsa hacia el cañón. |
| Fin Mundo 5 | El megatobogán los deja frente al barco de regreso. Final feliz en el zoológico, ahora con piscina de olas. |
| Mundo 6 (secreto) | El Rey Julien organiza "El Reino de Julien": desafíos remix para coronar al mejor equipo. |

## Alcance completo

- Mecánica de balsa compartida: toque, ráfaga, pedo cargado, puntería, gas, sincronía, medidor de equipo, Súper Pedo, agarrarse, cámaras de aire, caídas, nado, rescate, checkpoints, pings.
- 5 personajes con pasiva y habilidad activa; sinergias entre habilidades.
- 31 niveles (docs/04), 20+ tipos de obstáculo, 7 power-ups (docs/05).
- Modos (docs/06): Historia, Contrarreloj con fantasma, Espejo, Modificadores (Caos), Fiesta (5 minijuegos), Infinito, Desafío Diario, Carrera por Equipos.
- Progresión: estrellas, mangos dorados, tienda de cosméticos, 25 logros, estadísticas, 3 dificultades + ayuda opcional.
- Cooperativo local (PlayerInputManager), online (NGO 1.x + Relay + Lobby) y bots.
- Guardado local, opciones completas, accesibilidad básica.

## Lo que NO es

- No tiene violencia ni muertes: los animales caen al agua y vuelven.
- No tiene microtransacciones ni anuncios.
- No usa assets de las películas (modelos, voces, música, frases).

## Diferenciadores frente al referente

| Elemento | Fart Together | Gas Safari |
|---|---|---|
| Personajes | Hipopótamos iguales | 5 animales con stats y habilidad |
| Contenido | 1 circuito | 31 niveles + 8 modos |
| Solo | No | Bots o 2 animales a la vez |
| Local | No | Hasta 4 en una pantalla |
| Comunicación | Voz | Voz + pings + cuenta regresiva automática |
| Dificultad | Única | Familiar / Normal / Amistades Rotas + ayuda |
