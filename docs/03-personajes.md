# 03 · Personajes

5 personajes; cada balsa lleva de 1 a 4, sin repetir. Cada uno tiene stats (`CharacterData`), una pasiva y una habilidad activa (`AbilityBase`). La base de comparación es: puffImpulse 20, blastForce 120, gas 100, recarga 12/s.

## Resumen

| Personaje | Rol | Gas | Toque | Ráfaga | Recarga/s | Retardo | Peso (kg) | Altura | Dificultad |
|---|---|---|---|---|---|---|---|---|---|
| Alex (león) | Líder todoterreno | 100 | 22 | 135 | 12 | 0.6 s | 3.0 | 1.8 m | Fácil |
| Marty (cebra) | Velocista | 80 | 20 | 120 | 17 | 0.3 s | 2.0 | 1.7 m | Media |
| Melman (jirafa) | Soporte y navegante | 100 | 17 | 100 | 12 | 0.6 s | 2.5 | 2.6 m | Media |
| Gloria (hipopótama) | Motor y ancla | 140 | 25 | 150 | 8 | 0.8 s | 4.0 | 1.6 m | Media |
| Rey Julien (lémur) | Caos y coordinador | 80 | 16 | 95 | 15 | 0.5 s | 1.0 | 1.0 m | Difícil |

## Alex — El león

- **Personalidad para animar:** artista, seguro de sí mismo, poses teatrales.
- **Pasiva "Estrella del Show":** si Alex forma parte de un grupo de sincronía, la `syncWindow` de todo el equipo sube +0.08 s durante 3 s (no se acumula, se renueva).
- **Habilidad "Rugido de la Selva"** (recarga 18 s): cono de 8 m y 70° hacia la proa (dirección de avance).
  - Destruye obstáculos con etiqueta `Fragile` (barriles, cajas, troncos podridos, telarañas).
  - Espanta a la fauna con etiqueta `Fauna` (cocodrilos, fosas, cangrejos, murciélagos) durante 4 s: retroceden y no atacan.
  - Frena la balsa con un impulso de 15 hacia atrás.
- **Brilla en:** tramos con fauna y escombros (W2, W3). Ideal para principiantes.

## Marty — La cebra

- **Personalidad:** hiperactivo, rápido, no se queda quieto.
- **Pasiva "Pulmones de Cebra":** ya incluida en sus datos (recarga 17/s y retardo 0.3 s, frente a 12/s y 0.6 s base).
- **Habilidad "Galope Turbo"** (recarga 12 s, sin costo de gas): impulso instantáneo de 60 en la dirección de su empuje actual (según asiento y puntería) y `waterDrag` x0.5 durante 2 s. Deja una estela de rayas.
- **Brilla en:** rampas, saltos, Contrarreloj y escapes de corrientes.

## Melman — La jirafa

- **Personalidad:** nervioso, exagerado, se asusta con todo.
- **Rasgo "Vista Alta":** mientras Melman va a bordo, los avisos de peligro del HUD aparecen 3 s antes.
- **Pasiva "Botiquín Hipocondríaco":** mantener el botón de habilidad 1.5 s repara 1 cámara reventada. 1 uso por checkpoint.
- **Habilidad "Cuello Gancho"** (pulsar; recarga 15 s):
  - Prioridad 1: si hay un compañero en el agua a menos de 10 m, lo atrae a la balsa en 0.5 s.
  - Prioridad 2: se engancha al `Anchor` más cercano a menos de 10 m (resaltado con un aro cuando está en rango). La balsa pivota alrededor del ancla (joint de distancia fija) hasta 2.5 s o hasta soltar el botón. Conserva la velocidad al soltar.
- **Brilla en:** giros cerrados, laberintos, cuevas oscuras, niveles con muchas caídas.

## Gloria — La hipopótama

- **Personalidad:** segura, con ritmo, carcajada contagiosa.
- **Pasiva "Peso Pesado":** mientras está a bordo, la componente lateral de corrientes y viento se reduce al 70 %.
- **Habilidad "Panzazo"** (recarga 20 s): salta 0.6 s y cae de panza sobre la balsa.
  - `angularVelocity = 0` y `angularDrag` x2 durante 2 s (estabiliza).
  - Impulso de 40 en la dirección de la corriente.
  - Anula la atracción de remolinos durante 2 s (escape garantizado).
- **Brilla en:** tormentas, remolinos, viento lateral y recuperación tras choques.

## Rey Julien — El lémur

- **Personalidad:** caótico, fiestero, se cree el jefe.
- **Pasiva "Corona Magnética":** atrae power-ups a 6 m.
- **Salto** (A/Cruz, recarga 3 s): salta hasta 4 m fuera de la balsa para atrapar un objeto. Si aterriza a menos de 1.5 m del borde, vuelve solo; si no, cae al agua con las reglas normales.
- **Habilidad "Fiesta Real"** (recarga 30 s): 4 s de música de fiesta. Todo inicio de pedo cuenta como grupo de sincronía completo (multiplicador según los animales a bordo) y el medidor de equipo sube x2.
- **Brilla en:** el nivel rítmico W3-5, junto al Súper Pedo, y con equipos que no se coordinan.

## Sinergias (las detecta `AbilitySynergySystem`)

| Sinergia | Condición | Efecto |
|---|---|---|
| Honda de la Selva | Marty usa Galope Turbo durante el Cuello Gancho de Melman | Al soltar el ancla, velocidad de salida x1.5 |
| Terremoto | Panzazo de Gloria y Rugido de Alex con menos de 1 s de diferencia | Destruye obstáculos `Heavy` (rocas, cajas grandes) a 10 m |
| Fiesta Total | Súper Pedo durante la Fiesta Real de Julien | Súper Pedo al 150 % de potencia |
| Dúo Pesado | Alex y Gloria en el mismo grupo de sync con asientos a menos de 60° | +20 % de fuerza e inclinación por peso ignorada 2 s |
| Rescate Real | Melman rescata a Julien con el Cuello Gancho | Julien vuelve con el gas al 100 % |

La primera vez que un equipo logra cada sinergia aparece su nombre en pantalla y se desbloquea un logro (docs/06).

## Animaciones (todas obligatorias por personaje)

| Clip | Loop | Uso |
|---|---|---|
| Idle_Sit | Sí | Sentado en el borde |
| Shuffle_L / Shuffle_R | Sí | Moverse por el borde |
| Fart_Puff | No | Toque (0.3 s) |
| Fart_Blast | Sí | Ráfaga |
| Charge | Sí | Aguantando (cachetes y ojos inflados) |
| Fart_Mega | No | Liberar cargado y Súper Pedo |
| Cough | No | Sin gas |
| Ability | No | Habilidad activa |
| Grab | Sí | Agarrado a la cuerda |
| Hit | No | Golpe recibido |
| Fall | No | Caer al agua |
| Swim | Sí | Nadar |
| Climb | No | Subir a la balsa |
| Celebrate | No | Sync x4, checkpoint, meta |
| Disappointed | No | Balsa hundida |
| Victory | Sí | Pantalla de resultados |
| Select_Pose | Sí | Pantalla de selección |

Controlador base `CharacterBase.controller` + un Animator Override Controller por personaje (docs/09).

## Sonidos por personaje (IDs en docs/08)

6 toques, 2 ráfagas en loop, 1 carga, 2 megapedos, 2 toses, 1 habilidad y voces sin palabras (3 esfuerzos, 2 gritos de caída, 2 risas, 2 celebraciones). Timbre: Gloria grave, Alex medio-grave, Marty rápido, Melman agudo y nervioso, Julien muy agudo.

## Reglas de selección

- Cada jugador elige un personaje distinto. Botón "Aleatorio".
- Con bots, el anfitrión elige a los bots o se asignan al azar entre los libres.
- Colores de jugador (aro, HUD, ping), independientes del personaje: J1 naranja `#F28C28`, J2 azul `#3A86FF`, J3 verde `#2DC653`, J4 morado `#9B5DE5`.
