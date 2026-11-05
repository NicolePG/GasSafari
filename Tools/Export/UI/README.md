# Arte de interfaz (Tools/Export/UI)

Imágenes renderizadas en Blender 4.5 (EEVEE, sombreado toon de 3 bandas y contorno oscuro) con los mismos modelos
del juego. Nada se importa solo: se revisa y se copia a `Assets/_Project/` (sugerido: `Art/UI/`, tipo de textura
Sprite (2D and UI), sin compresión con pérdida para los iconos).

```
blender --background --python Tools/Blender/ui_art.py        # todo
blender --background --python Tools/Blender/ui_art.py -- portraits poses icons logo map
blender --background --python Tools/Blender/story.py -- --loading   # fondos de carga (Loading/)
```

Hojas de revisión (no se suben a Git): `Tools/Blender/previews/UI_*_sheet.png`.

## Retratos — `Portraits/Portrait_<Nombre>.png`

| Archivo | Tamaño | Fondo | Uso |
|---|---|---|---|
| `Portrait_Alex.png`, `Portrait_Marty.png`, `Portrait_Melman.png`, `Portrait_Gloria.png`, `Portrait_Julien.png` | 512x512 | Transparente | HUD (esquinas por jugador), selección, resultados |

De frente, sonriendo, cabeza y hombros sobre un círculo con el color de UI del personaje (docs/03:
Alex `#E59A2F`, Marty `#5C6F82`, Melman `#F2C94C`, Gloria `#9B8FD6`, Rey Julien `#6CC24A`), con aro blanco y borde
oscuro. Fuera del círculo solo queda la mitad superior: la cabeza puede sobresalir del borde (estilo insignia).
El círculo ocupa el 86 % del ancho y está centrado. Para el aro del color del jugador (J1-J4) poner otro círculo
detrás en la UI. Melman lleva una sonrisa dibujada solo para el retrato (su modelo tiene boca nerviosa).

## Poses de selección — `CharacterSelect/Select_<Nombre>.png`

| Archivo | Tamaño | Fondo | Uso |
|---|---|---|---|
| `Select_Alex.png` … `Select_Julien.png` | 1024x1024 | Transparente | Carrusel de la pantalla de selección (docs/07 §2) |

Cuerpo entero en vista 3/4 con un cuadro del clip `Select_Pose` (el mismo que se reproduce en el juego). Cada
personaje llena el cuadro (Melman se ve más delgado por su altura); alinear por la base en la UI.

## Iconos — `Icons/`

| Archivo | Tamaño | Fondo | Qué es |
|---|---|---|---|
| `PowerUps/Icon_P01_Beans.png` | 256x256 | Transparente | Lata de frijoles (P01) |
| `PowerUps/Icon_P02_Locoto.png` | 256x256 | Transparente | Locoto (P02) |
| `PowerUps/Icon_P03_Mango.png` | 256x256 | Transparente | Mango (P03) |
| `PowerUps/Icon_P04_CoconutShield.png` | 256x256 | Transparente | Coco escudo (P04) |
| `PowerUps/Icon_P05_Soda.png` | 256x256 | Transparente | Gaseosa (P05) |
| `PowerUps/Icon_P06_Onion.png` | 256x256 | Transparente | Cebolla (P06) |
| `PowerUps/Icon_P07_GoldenMango.png` | 256x256 | Transparente | Mango dorado (P07) |
| `Abilities/Icon_Ability_Alex_Roar.png` | 256x256 | Transparente | Rugido de la Selva: Alex rugiendo con ondas en cono |
| `Abilities/Icon_Ability_Marty_GallopTurbo.png` | 256x256 | Transparente | Galope Turbo: herradura con estela de rayas de cebra |
| `Abilities/Icon_Ability_Melman_NeckHook.png` | 256x256 | Transparente | Cuello Gancho: cuello en gancho que atrapa un aro |
| `Abilities/Icon_Ability_Gloria_BellyFlop.png` | 256x256 | Transparente | Panzazo: Gloria cae de panza sobre una salpicadura |
| `Abilities/Icon_Ability_Julien_RoyalParty.png` | 256x256 | Transparente | Fiesta Real: corona dorada, notas y confeti |

Los power-ups son los modelos de `props.py` (los mismos FBX del juego). Contorno de ~3 px para que se lean a
64-96 px en el HUD; la recarga radial se dibuja encima desde la UI.

## Logo — `Logo/Logo_GasSafari.png`

2048x1024, transparente. "GAS" en verde gas y "SAFARI" en naranja, letras de tubo gordo en 3D con volumen trasero,
nube de gas detrás de "GAS", burbujas y hojas en las puntas. Letras construidas con geometría propia (sin fuentes).
Pantalla de título y splash; se puede escalar a la mitad sin perder el contorno.

## Mapa de mundos — `WorldMap/WorldMap.png`

1920x1080, opaco (con fondo). Vista aérea ilustrada: 6 islas unidas por un río que cruza tierras del color de cada
mundo, con mar alrededor. De abajo a la izquierda hacia arriba a la derecha:

| Isla | Mundo | Hito |
|---|---|---|
| Gris (abajo izquierda) | W1 Fuga del Zoológico | Puerta del zoológico y noria |
| Arena | W2 Mar Abierto | Faro y mástil hundido |
| Verde | W3 Selva de Madagascar | Escenario de DJ entre palmeras |
| Morada | W4 Cuevas y Volcán | Volcán con lava y cristales |
| Naranja | W5 El Gran Regreso | Arco natural y torre del megatobogán |
| Magenta (arriba derecha) | W6 Reino de Julien (secreto) | Corona gigante, escenario y Rey Julien |

La balsa naranja con su nube de gas está en el río entre W1 y W2 (inicio del viaje). Centros aproximados de las
islas en píxeles, para colocar botones de nivel encima: W1 (200, 720), W2 (580, 400), W3 (890, 640),
W4 (1180, 330), W5 (1510, 630), W6 (1670, 310). Sin textos (los pone la UI y se traducen).

## Fondos de carga — `Loading/Loading_W<N>.png`

1920x1080, opacos, uno por mundo. La balsa naranja con su tripulación avanza hacia el fondo del escenario; el
tercio inferior queda más tranquilo (agua) para el consejo y la barra de carga.

| Archivo | Mundo | Escena |
|---|---|---|
| `Loading_W1.png` | W1 Fuga del Zoológico | Canal de la ciudad de noche: muelles con farolas, puente, puerta del zoo, noria y luna |
| `Loading_W2.png` | W2 Mar Abierto | Mar con oleaje: faro, rocas, boyas y barco hundido |
| `Loading_W3.png` | W3 Selva de Madagascar | Río de selva: palmeras, baobabs, lianas, nenúfares y un cocodrilo dormido |
| `Loading_W4.png` | W4 Cuevas y Volcán | Cueva morada: estalactitas, cristales, hongos que brillan y lava |
| `Loading_W5.png` | W5 El Gran Regreso | Cañón al atardecer: paredes rojas, arco natural y la represa al fondo |
| `Loading_W6.png` | W6 Reino de Julien | Fiesta nocturna: escenario con trono y corona gigante, luces y confeti |

## Viñetas de la historia — `Tools/Export/Story/<Momento>/<Momento>_0N.png`

1920x1080, opacas, viñeta suave. Se generan con `story.py` (docs/01, tabla "Historia"); orden = número. Sin textos:
los globos o subtítulos los pone la UI (Timeline de M5-39).

| Momento | Archivo | Qué muestra |
|---|---|---|
| Intro | `Intro_01` | Noche en el zoo: los cinco salen de puntillas por la puerta bajo un reflector |
| | `Intro_02` | En el muelle solo hay una balsa inflable y un barril de frijoles |
| | `Intro_03` | Gloria se come los frijoles y... ¡pedo! (fondo de rayos, los demás retroceden) |
| | `Intro_04` | El "método": la balsa sale disparada con nubes de gas |
| | `Intro_05` | Todos a bordo celebran y se alejan de la ciudad bajo la luna |
| W1_End | `W1_End_01` | En el puerto, escondidos tras el barco de carga, con la grúa y un reflector |
| | `W1_End_02` | En silencio a la sombra del casco (Alex pide silencio) |
| | `W1_End_03` | Mar abierto al amanecer, Alex señala el horizonte |
| W2_End | `W2_End_01` | La tormenta: olas, rayo y lluvia, la balsa volcándose |
| | `W2_End_02` | Amanecen tirados en la playa de una isla tropical |
| | `W2_End_03` | De espaldas, miran la selva de baobabs y palmeras |
| W3_End | `W3_End_01` | Ganan la fiesta de los lémures frente al escenario de DJ |
| | `W3_End_02` | El Rey Julien se une "oficialmente" sobre un pedestal (fondo de rayos) |
| | `W3_End_03` | Julien señala el volcán como atajo a casa |
| | `W3_End_04` | Rumbo al volcán por el río de la selva |
| W4_End | `W4_End_01` | Dentro del volcán: un géiser levanta la balsa sobre la lava |
| | `W4_End_02` | Salen disparados del cráter entre lava y humo |
| | `W4_End_03` | Vuelan sobre el cañón al atardecer |
| W5_End | `W5_End_01` | Bajan el megatobogán |
| | `W5_End_02` | Caen al agua frente al barco de regreso |
| | `W5_End_03` | A bordo del barco, saludan a la ciudad al atardecer |
| | `W5_End_04` | De vuelta en el zoológico, ahora con piscina de olas |
| | `W5_End_05` | Final feliz: foto de grupo frente a la puerta con confeti |
| W6_Secret | `W6_Secret_01` | El Rey Julien en su trono organiza "El Reino de Julien" |
| | `W6_Secret_02` | Desafíos remix: tentáculo, cocodrilo gigante y grúa con luces de fiesta |
| | `W6_Secret_03` | Coronan al mejor equipo (Alex con la Corona dorada) |
