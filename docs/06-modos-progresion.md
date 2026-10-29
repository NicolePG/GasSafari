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

### Espejo
- El nivel se carga con la raíz de la geometría en escala X = -1 (Unity invierte el culling solo) y las splines del cauce espejadas.
- Récords propios; recompensa de frijolitos x1.5.

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

### Fiesta (minijuegos)

| Minijuego | Reglas | Duración |
|---|---|---|
| Balsa-Gol | 2 balsas (1 vs 1 o 2 vs 2) empujan una pelota de playa gigante a la portería rival | 3 min o 5 goles |
| Rey de la Balsa | Todos en una balsa que gira en una piscina redonda; cada pedo empuja a los animales a menos de 2.5 m. Gana el último a bordo | Mejor de 3 rondas |
| Pesca de Mangos | Cada jugador en una mini balsa individual; recoger más mangos | 90 s |
| Carrera de Burbujas | Mini balsas individuales con gaseosa infinita sobre una pista de obstáculos | 3 vueltas |
| Coco Caliente | Balsa compartida; un coco-bomba pasa a quien se tira un pedo más cerca; al explotar lanza al agua a quien lo tiene | Rondas hasta que quede uno |

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
| Daño a cámaras | Solo pinchos y mordidas | Pinchos, mordidas y golpes > 25 | Pinchos, mordidas y golpes > 15 |
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

## 5. Logros (25)

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

## 6. Estadísticas guardadas
Pedos totales, grupos de sync por tamaño, Súper Pedos, caídas, rescates, hundimientos, distancia recorrida, tiempo de juego, personaje más usado.
