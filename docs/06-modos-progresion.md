# 06 · Modos, progresión, dificultad y logros

## 1. Modos de juego

| # | Modo | Jugadores | Desbloqueo | Resumen |
|---|---|---|---|---|
| 1 | Historia | 1-4 (local u online) + bots | Inicio | Los 31 niveles en orden con estrellas y mangos dorados |
| 2 | Contrarreloj | 1-4 | Al terminar un nivel | Repetir contra el fantasma del mejor intento |
| 3 | Espejo | 1-4 | Terminar W5-5 | Todos los niveles en espejo, mínimo dificultad Normal |
| 4 | Modificadores (Caos) | 1-4 | 30 estrellas | Reglas locas combinables |
| 5 | Fiesta | 2-4 (bots completan) | Terminar W1-5 | 5 minijuegos |
| 6 | Infinito | 1-4 | Terminar W2-5 | Río procedural sin fin |
| 7 | Desafío Diario | 1-4 | Terminar W2-5 | Río generado con la fecha + 1 modificador |
| 8 | Carrera por Equipos | 2 balsas: local 2 vs 2 en pantalla dividida, online hasta 4 vs 4 | Terminar W1-5 | Dos balsas en la misma pista |

### Contrarreloj
- Fantasma: se graba posición y rotación de la balsa a 10 Hz del mejor tiempo del equipo; se reproduce semitransparente.
- Tabla local con los 5 mejores tiempos por nivel (nombre del equipo + personajes).
- Decisiones provisionales, pendientes de playtest (M7-01, 2026-11-22):
  - El fantasma se graba en toda carrera limpia de Historia o Contrarreloj (sin modificadores, sin Espejo y sin saltar una sección con la Ayuda Real); se guarda solo el mejor por nivel, en `persistentDataPath/ghosts/<id>.ghost` junto al guardado. Así el primer Contrarreloj de un nivel ya terminado tiene fantasma. Solo la balsa, en la capa Ghost, sin colisiones.
  - La tabla de los 5 mejores también se llena con esas carreras. No hay pantalla para escribir el nombre del equipo: se arma con los nombres de los animales ("Alex · Marty").
  - La diferencia con el fantasma se muestra 3 s bajo el cronómetro al pasar cada checkpoint (verde si van adelante) y en la meta; en resultados, "¡Nuevo récord! -2.4 s frente al fantasma".
  - Contrarreloj no ofrece la Ayuda Real; da estrellas, récord del nivel y frijolitos igual que Historia. No hay Contrarreloj en T-0 (no tiene cronómetro).

### Espejo
- El nivel se carga con la raíz de la geometría en escala X = -1 (Unity invierte el culling solo) y las splines del cauce espejadas.
- Récords propios; recompensa de frijolitos x1.5.
- Decisión provisional, pendiente de playtest (M7-02, 2026-11-22): **se invierte la imagen y la entrada, no la geometría.** Las ~800 piezas de cada escena van en static batching (sus mallas se combinan al construir), así que una raíz con escala X = -1 en tiempo de ejecución movería los colisionadores pero no lo que se ve; tener 31 escenas espejo aparte sumaría ~100 MB al repositorio. En su lugar:
  - La cámara del nivel dibuja en una textura y una imagen a pantalla completa la muestra invertida en X, debajo del HUD (`MirrorScreen`); el HUD, la pausa y los resultados no se invierten.
  - La entrada de los humanos se invierte en X (moverse por el borde, apuntar, nadar) y los pings izquierda/derecha cambian de lado (`MirrorView`). Bots, física, splines, corriente, peligros, checkpoints y BotHints quedan iguales, por eso funcionan en los 31 niveles sin cambios.
  - Las flechas de peligro del HUD, los iconos de animales en el agua y los avisos (`Warn()`) usan la posición de pantalla invertida; los textos del mundo (carteles 3D, anillos y pings sobre los animales) se voltean para leerse al derecho; la repetición "¿Quién fue?" también se ve invertida.
  - Se nota en: los animales y los objetos con letras pintadas en la textura se ven espejados.
- Más decisiones provisionales: en Familiar la dificultad sube a Normal mientras se juega Espejo y vuelve a la elegida al jugar otro modo; Espejo no graba fantasma; los mangos dorados que se recogen cuentan para el nivel; terminar en Espejo con Amistades Rotas no cuenta para el logro "Amistades Rotas" (pide la historia). T-0 no tiene Espejo (se juega normal).

### Modificadores (Caos)
Se activan desde un menú antes del nivel. Los tiempos con modificadores no cuentan para estrellas.

| Modificador | Efecto | Se desbloquea con |
|---|---|---|
| Balsa jabonosa | `waterDrag` x0.5 | 30 estrellas |
| Gas infinito | El gas no se gasta | 30 estrellas |
| Gravedad lunar | Gravedad x0.4 | 40 estrellas |
| Pedos al revés | La fuerza se invierte | 40 estrellas |
| Cabezones | Cabezas x2 (cosmético) | 50 estrellas |
| Turbo corriente | Corrientes x1.5 | 50 estrellas |
| Cámara mareada | La cámara se balancea | 60 estrellas |
| Cámara única | La balsa se hunde al primer golpe | 70 estrellas |

Decisiones provisionales, pendientes de playtest (M7-03, 2026-11-22):
- Se combinan todos. El menú sale de la ficha del nivel (botón Modificadores, o Jugar con la tarjeta Modificadores); la selección queda en la partida para Repetir y Siguiente. Jugar sin ninguno pide elegir al menos uno.
- Alcance de cada uno: Pedos al revés invierte toque, ráfaga y cargado (no el Súper Pedo ni las habilidades); Turbo corriente multiplica la corriente del cauce (no el viento ni las zonas de rápidos); Gravedad lunar afecta toda la física del nivel y se devuelve al salir; Cabezones agranda la cabeza x2 (el sombrero crece con ella); Cámara mareada gira la cámara ±6° y la mueve ±2° de lado en un vaivén de 4 s; Cámara única: el escudo de coco sigue parando un golpe.
- Sin estrellas ni récord: tampoco graban fantasma ni entran en la tabla del Contrarreloj. Frijolitos: 20 por terminar + grupos x4 (sin "primera vez" ni estrellas nuevas, que no se guardan). Los mangos dorados recogidos sí cuentan (regla de M3-10).
- Ayuda Real: se ofrece igual (con Cámara única llega antes); saltar la sección no cambia nada porque no hay estrellas.
- Logros: cuentan los de acciones (pedos, sync, Súper Pedo, sinergias, Capitán Titanic) y las estadísticas; no cuentan los de terminar niveles (Sin un Rasguño, avance de Amistades Rotas; los de estrellas y mundos tampoco, porque no se guardan estrellas).

### Fiesta (minijuegos)

| Minijuego | Reglas | Duración |
|---|---|---|
| Balsa-Gol | 2 balsas (1 vs 1 o 2 vs 2) empujan una pelota de playa gigante a la portería rival | 3 min o 5 goles |
| Rey de la Balsa | Todos en una balsa que gira en una piscina redonda; cada pedo empuja a los animales a menos de 2.5 m. Gana el último a bordo | Mejor de 3 rondas |
| Pesca de Mangos | Cada jugador en una mini balsa individual; recoger más mangos | 90 s |
| Carrera de Burbujas | Mini balsas individuales con gaseosa infinita sobre una pista de obstáculos | 3 vueltas |
| Coco Caliente | Balsa compartida; un coco-bomba pasa a quien se tira un pedo más cerca; al explotar lanza al agua a quien lo tiene | Rondas hasta que quede uno |

Decisiones provisionales, pendientes de playtest (M7-04, 2026-11-23):
- Flujo: tarjeta Fiesta → Local → sala (la Fiesta empieza con los puntos en cero) → pantalla de minijuegos (`PartySelectScreen`: los 5, los que faltan dicen "Próximamente", y los puntos acumulados) → escena del minijuego (`Scenes/Juego/Fiesta/<Escena>`) → resultados con "Otro minijuego" o "Repetir". "Terminar la Fiesta" muestra el ranking final y vuelve al menú principal.
- Participantes: los de la sala (humanos y bots) y, si faltan, bots de relleno hasta el mínimo del minijuego (2); Balsa-Gol pide número par, así que 3 se completan a 2 vs 2. Los bots de relleno usan los lugares y animales libres; no entran en la sala.
- Puntos de la Fiesta por puesto en cada minijuego: 1.º 3, 2.º 2, 3.º 1, 4.º 0. Los empatados comparten puesto (en un 2 vs 2 los ganadores son 1.º y los perdedores 3.º; un empate deja a todos 1.º). Valores en `ModesConfig.partyPoints`.
- Arenas: piscinas greybox con el agua y el cielo de día de T-0 y cámara fija que ve toda la arena. Los minijuegos no tienen pausa todavía.
- Cada minijuego terminado cuenta para el logro "Animal de Fiesta" (`partyPlayed`). Los pedos y las caídas suman a las estadísticas como en un nivel.
- Balsa-Gol: J1 y J3 en la balsa azul, J2 y J4 en la naranja (bandera del color en la balsa). Pelota de 1.4 m de radio y 2 kg que flota. Portería de 10 m de boca y 3.5 m de fondo, con chaflanes a los lados y esquinas de la cancha a 45°. Tras un gol, 2.5 s de pausa y saque desde las salidas. Si la pelota se mueve menos de 1.5 m en 5 s (atrapada contra un borde o entre las dos balsas), "¡Saque neutral!": vuelve cerca del centro, a un punto al azar. Si se acaba el tiempo con empate, es empate. Los bots se ponen detrás de la pelota mirando a la portería rival y empujan; si el rival está pegado del otro lado, la sacan de costado, y cada equipo llega a la pelota con un ángulo al azar de ±30° que cambia cada 3 s. En las arenas los bots alternan ráfagas (con más del 80 % de gas, hasta bajar al 15 %) y toques; sin eso se quedaban sin gas. En pruebas con 4 bots meten 1-2 goles en 90 s (4 de 4 partidas con gol), así que conviene afinarlos en el playtest.
- Rey de la Balsa (M7-05): piscina redonda de 10 m de radio; la balsa gira a 30°/s y un resorte suave la devuelve al centro. Cada pedo (toque, ráfaga o cargado) tira al agua a los animales sentados a menos de 2.5 m de quien lo tira, salvo a los que están agarrados (docs/02 §7). El que cae queda fuera de la ronda y desaparece al segundo. Cada ronda empieza con todos repartidos por igual en el borde (con 4, a 90°: 2.8 m, fuera de alcance). Gana el minijuego quien gane 2 rondas; una ronda dura como mucho 60 s y, si se acaba o caen todos a la vez, nadie la gana; tope de 5 rondas, y después gana quien tenga más (con empates compartidos). Entre rondas hay 3 s. Los bots se acercan por el borde al rival más cercano, tardan entre 0.8 y 1.8 s en tirarse el pedo y a ratos se agarran (en pruebas, rondas de 6 a 20 s).
- Pesca de Mangos (M7-06): piscina cuadrada de 26 m; cada jugador sale de una esquina en su mini balsa (la balsa de siempre con radio 1 m, el modelo MiniRaft_J<n> con los tubos del color del jugador). Siempre hay 8 mangos flotando (el power-up P03, que también da gas); cada mango recogido reaparece 1.5 s después en otro punto libre, a más de 3 m de las balsas y 2.5 m de otros mangos. Gana quien tenga más mangos a los 90 s (empates compartidos). Los bots van al mango libre más cercano y evitan los que otro tiene mucho más cerca (en pruebas, 13-15 mangos cada uno en 40 s).

### Infinito
- Tramos prefabricados de 30-60 m (`EndlessChunk`) etiquetados por mundo y dificultad (1-5).
- La dificultad sube cada 200 m. Un checkpoint cada 3 tramos. Termina al hundirse 3 veces.
- Récord de distancia local.

### Desafío Diario
- Semilla = fecha `yyyyMMdd`. Genera un recorrido de 1500 m con 1 modificador fijo para ese día.
- Guarda mejor tiempo del día y racha de días seguidos.

### Carrera por Equipos
- Pistas compatibles (marcadas `raceCompatible` en `LevelData`): W1-3, W2-2, W3-2, W4-1, W5-1, W5-5. Cada una tiene 2 puntos de salida.
- Choques entre balsas permitidos. Power-ups compartidos en la pista.
- Gana la primera balsa en la meta; mejor de 3 pistas.

## 2. Progresión

Tres sistemas separados:

| Sistema | Cómo se gana | Para qué sirve |
|---|---|---|
| Estrellas (93) | 3 por nivel (docs/04) | Abrir mundos y modificadores |
| Mangos dorados (93) | 3 escondidos por nivel | Hitos de colección y Mundo 6 |
| Frijolitos (moneda) | Jugando | Tienda de cosméticos |

### Desbloqueo de mundos
W1: inicio · W2: 8 estrellas · W3: 20 · W4: 32 · W5: 45 · W6: 60 mangos dorados.

### Frijolitos
- Terminar un nivel: 20 (primera vez +30).
- Por estrella nueva: 10.
- Por grupo de sync x4: 1 (máximo 10 por nivel).
- Fiesta, Infinito, Desafío Diario: 5 a 25 según resultado.

### Hitos de mangos dorados
10: color de gas "Mango" · 25: balsa "Mango gigante" · 40: sombrero "Cáscara de mango" · 60: Mundo 6 · 80: estela de mangos · 93: título "Coleccionista Real".

### Tienda de cosméticos (frijolitos)

| Categoría | Artículos (precio) |
|---|---|
| Sombreros | Gorra del zoológico (inicial), Gafas de buceo (100), Sombrero pirata (100), Casco de explorador (120), Corona de flores (120), Gorro de chef (150), Sombrero vaquero (150), Corona dorada (premio de W6-5) |
| Colores de gas | Verde clásico (inicial), Humo negro (100), Neón rosa (120), Burbujas (150), Arcoíris (200), Brillantina dorada (250) |
| Balsas | Balsa naranja (inicial), Hoja de nenúfar (150), Caja de embalaje del zoológico (150), Tronco hueco (180), Flotador de coco (200), Bote salvavidas (200), Cáscara de sandía (250) |
| Celebraciones | Aplauso (50), Baile robot (80), Reverencia (80), Saludo de capitán (80), Voltereta (120), Paso de conga (120) |

Los cosméticos de balsa son del equipo (los elige el anfitrión); sombreros, colores y celebraciones son por jugador.

## 3. Dificultad

| Ajuste | Familiar | Normal | Amistades Rotas |
|---|---|---|---|
| `syncWindow` | 0.40 s | 0.25 s | 0.18 s |
| Checkpoints | Normales + extra marcados `familiarOnly` | Normales | Solo los marcados `hardKeep` |
| Daño a cámaras (`chamberDamageSpeed`) | Solo pinchos y mordidas | Pinchos, mordidas y golpes a más de 7 m/s | Pinchos, mordidas y golpes a más de 5 m/s |
| Recarga de gas | +25 % | Base | -15 % |
| Penalización por reaparecer | 0 s | 3 s | 6 s |
| Velocidad de peligros | x0.85 | x1 | x1.15 |
| `autoRespawnTime` | 4 s | 6 s | 8 s |

Se elige al empezar la partida y se puede cambiar entre niveles. Las estrellas valen igual en todas.

### Ayuda Real (opcional)
Tras 3 reapariciones en el mismo checkpoint aparece una oferta:
1. Ventana de sync +0.1 s hasta el siguiente checkpoint.
2. Checkpoint extra 50 % más adelante.
3. Saltar la sección (el nivel queda con máximo 1 estrella).

## 4. Premios de la pantalla de resultados

Se muestran 3 al azar entre los que tengan ganador (al menos 1 positivo y 1 de vergüenza):

| Premio | Para quien | Tipo |
|---|---|---|
| Maestro de la Sincronía | Más participaciones en grupos de sync | Positivo |
| Salvavidas | Más rescates | Positivo |
| Rey del Show | Más habilidades usadas | Positivo |
| Tragón | Más power-ups recogidos | Positivo |
| Pedo más inoportuno | Más pedos en el segundo previo a un choque o hundimiento | Vergüenza |
| Capitán Titanic | Más caídas al agua | Vergüenza |
| Gas Desperdiciado | Más veces sin gas | Vergüenza |
| Bello Durmiente | Menos pedos en total | Vergüenza |

Además: repetición "¿Quién fue?" de 3 s del peor choque, con flecha al jugador cuyo pedo lo causó.

## 5. Logros (26)

| # | Logro | Condición |
|---|---|---|
| 1 | Primer Pedo | Tirarse el primer pedo |
| 2 | Dueto | Primer grupo de sync x2 |
| 3 | Orquesta | Grupo de sync x4 |
| 4 | ¡Súper Pedo! | Primer Súper Pedo |
| 5 | Honda de la Selva | Lograr la sinergia |
| 6 | Terremoto | Lograr la sinergia |
| 7 | Fiesta Total | Lograr la sinergia |
| 8 | Dúo Pesado | Lograr la sinergia |
| 9 | Rescate Real | Lograr la sinergia |
| 10 | Capitán Titanic | Hundir la balsa 10 veces |
| 11 | Sin un Rasguño | Terminar un nivel sin perder cámaras |
| 12-17 | Mundo 1 ... Mundo 6 | Terminar cada mundo |
| 18 | Perfeccionista | Todas las estrellas de un mundo |
| 19 | Constelación | 93 estrellas |
| 20 | Coleccionista Real | 93 mangos dorados |
| 21 | Velocista | 3 estrellas en 10 niveles |
| 22 | Amistades Rotas | Terminar la historia en esa dificultad |
| 23 | Fiestero | Jugar los 5 minijuegos |
| 24 | Kilómetro | 1000 m en Infinito |
| 25 | Constante | Desafío Diario 7 días seguidos |
| 26 | Rey de la Balsa (secreto) | Terminar W6-5 La Corona del Rey. Se muestra como "???" hasta desbloquearlo; da la corona dorada (cosmético) |

## 6. Estadísticas guardadas
Pedos totales, grupos de sync por tamaño, Súper Pedos, caídas, rescates, hundimientos, distancia recorrida, tiempo de juego, personaje más usado.
