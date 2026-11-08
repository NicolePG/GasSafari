# Arte de interfaz (Tools/Export/UI)

Imágenes renderizadas en Blender 4.5 (EEVEE, sombreado toon de 3 bandas y contorno oscuro) con los mismos modelos
del juego. Nada se importa solo: se revisa y se copia a `Assets/_Project/` (sugerido: `Art/UI/`, tipo de textura
Sprite (2D and UI), sin compresión con pérdida para los iconos).

```
blender --background --python Tools/Blender/ui_art.py        # todo
blender --background --python Tools/Blender/ui_art.py -- portraits poses icons logo map
blender --background --python Tools/Blender/story.py -- --loading   # fondos de carga (Loading/)
blender --background --python Tools/Blender/ui_icons.py      # iconos de tienda, logros, modos y dificultades
blender --background --python Tools/Blender/ui_icons.py -- achievements modes   # solo esos grupos
blender --background --python Tools/Blender/menu_scene.py    # escena 3D del título (Tools/Export/Menu/)
python Tools/Blender/ui_kit.py                               # kit de menús y HUD (Kit/), con Pillow, sin Blender
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

## Iconos de la tienda, logros, modos y dificultades — `Icons/<Grupo>/`

Todos 256x256, PNG con transparencia, mismo estilo toon con contorno de ~3 px que los iconos de arriba (`ui_icons.py`
y los dibujos de `ui_glyphs.py`). Sin textos. Hojas de revisión: `Tools/Blender/previews/UI_Icons_<Grupo>_sheet.png`.

### Tienda (docs/06 §2)

| Grupo | Archivos | Qué es |
|---|---|---|
| `Hats/` | `Icon_Hat_H01_ZooCap`, `H02_DivingGoggles`, `H03_PirateHat`, `H04_ExplorerHelmet`, `H05_FlowerCrown`, `H06_ChefHat`, `H07_CowboyHat`, `H08_GoldenCrown` | Los 8 sombreros (los mismos modelos de `Cosmetics/Hats`), 3/4 desde arriba |
| `Rafts/` | `Icon_Raft_R00_Orange` (inicial), `R01_LilyPad`, `R02_ZooCrate`, `R03_HollowLog`, `R04_CoconutFloat`, `R05_Lifeboat`, `R06_WatermelonRind` | Las 7 balsas (modelos de `Cosmetics/Rafts` y balsa naranja), vista desde arriba en 3/4 |
| `Gas/` | `Icon_Gas_G01_Classic` (verde clásico), `G02_BlackSmoke` (humo negro), `G03_NeonPink` (neón rosa), `G04_Bubbles` (burbujas), `G05_Rainbow` (arcoíris), `G06_GoldGlitter` (brillantina dorada) | Una nube de pedo con el color de cada artículo |
| `Celebrations/` | `Icon_Celebrate_Aplauso` (Alex), `BaileRobot` (Marty), `Reverencia` (Gloria), `SaludoCapitan` (Julien con sombrero pirata), `Voltereta` (Melman), `PasoConga` (Gloria) | El personaje en la pose clave del clip, sobre un disco de color con aro blanco |

Los nombres de archivo no llevan tildes ni espacios (`SaludoCapitan`, `PasoConga`, `BaileRobot`). El gas "Mango" del hito
de 10 mangos dorados, la balsa "Mango gigante" y el sombrero "Cáscara de mango" no tienen icono: no son artículos de la tienda.

### Logros (docs/06 §5) — `Achievements/Icon_Ach_<NN>_<Nombre>.png`

Medalla común: aro dorado, disco de color por categoría, borde blanco interior y cinta magenta y lima. El dibujo va en
el centro del disco (radio útil ≈ 70 % del disco).

| # | Archivo | Logro | Dibujo (color del disco) |
|---|---|---|---|
| 1 | `01_PrimerPedo` | Primer Pedo | Nube de gas con líneas de velocidad (verde) |
| 2 | `02_Dueto` | Dueto | Dos nubes con una chispa en medio (turquesa) |
| 3 | `03_Orquesta` | Orquesta | Cuatro nubes de colores alrededor de una nota (azul) |
| 4 | `04_SuperPedo` | ¡Súper Pedo! | Estallido amarillo con una nube verde (violeta) |
| 5 | `05_HondaSelva` | Honda de la Selva | Honda con piedra y hoja (naranja) |
| 6 | `06_Terremoto` | Terremoto | Suelo agrietado, rocas y ondas (naranja) |
| 7 | `07_FiestaTotal` | Fiesta Total | Gorro de fiesta y confeti (naranja) |
| 8 | `08_DuoPesado` | Dúo Pesado | Pesa de gimnasio (naranja) |
| 9 | `09_RescateReal` | Rescate Real | Salvavidas con corona (naranja) |
| 10 | `10_CapitanTitanic` | Capitán Titanic | Balsa hundiéndose con burbujas (azul) |
| 11 | `11_SinRasguno` | Sin un Rasguño | Escudo con visto (turquesa) |
| 12 | `12_Mundo1` | Mundo 1 | Puerta del zoológico (azul marino) |
| 13 | `13_Mundo2` | Mundo 2 | Faro con olas (turquesa) |
| 14 | `14_Mundo3` | Mundo 3 | Palmera con cocos (naranja) |
| 15 | `15_Mundo4` | Mundo 4 | Volcán con lava y humo (cian) |
| 16 | `16_Mundo5` | Mundo 5 | Arco natural con sol (dorado) |
| 17 | `17_Mundo6` | Mundo 6 | Corona dorada con gemas (magenta) |
| 18 | `18_Perfeccionista` | Perfeccionista | Estrella grande con tres pequeñas (azul noche) |
| 19 | `19_Constelacion` | Constelación | Estrellas unidas por líneas (azul noche) |
| 20 | `20_ColeccionistaReal` | Coleccionista Real | Mango dorado con corona (morado) |
| 21 | `21_Velocista` | Velocista | Cronómetro con líneas de velocidad (rojo naranja) |
| 22 | `22_AmistadesRotas` | Amistades Rotas | Corazón partido (rosa) |
| 23 | `23_Fiestero` | Fiestero | Pelota de playa con chispas (magenta) |
| 24 | `24_Kilometro` | Kilómetro | Infinito sobre una regla de marcas (morado) |
| 25 | `25_Constante` | Constante | Calendario con una llama (verde) |
| 26 | `26_ReyDeLaBalsa` | Rey de la Balsa (secreto, terminar W6-5) | Corona dorada sobre una balsa naranja, con una nube de gas y chispas (turquesa). Se genera con `Kit/make_raft_king_icon.py` (Blender, sin tocar `ui_icons.py`) |

### Modos (docs/06 §1) — `Modes/Icon_Mode_<N>_<Nombre>.png`

Insignia redonda: aro blanco, disco de color y brillo arriba a la izquierda.

| Archivo | Modo | Dibujo |
|---|---|---|
| `M1_Historia` | Historia | Libro abierto con marcapáginas |
| `M2_Contrarreloj` | Contrarreloj | Cronómetro con un fantasma |
| `M3_Espejo` | Espejo | Espejo ovalado dorado |
| `M4_Caos` | Modificadores (Caos) | Dado con un rayo |
| `M5_Fiesta` | Fiesta | Cañón de confeti con chispas |
| `M6_Infinito` | Infinito | Símbolo infinito en magenta y lima |
| `M7_DesafioDiario` | Desafío Diario | Calendario con una estrella |
| `M8_CarreraEquipos` | Carrera por Equipos | Dos balsas (roja y azul) y bandera de cuadros |

### Dificultad (docs/06 §3) — `Difficulty/Icon_Difficulty_<N>_<Nombre>.png`

Misma insignia con disco azul noche y una cara: `D1_Familiar` (verde sonriente con gorro de fiesta), `D2_Normal`
(naranja, gesto firme) y `D3_AmistadesRotas` (roja, ceño fruncido y grieta).

## Escena 3D del título — `Tools/Export/Menu/`

Fondo animado del menú principal (`menu_scene.py`). Unidades en metros; el .blend es Z arriba y el FBX sale con la
conversión de siempre (-Z adelante, Y arriba).

| Archivo | Contenido |
|---|---|
| `TitleScene.blend` | Escena completa con materiales planos, cielo, sol y la cámara `TitleCamera` (32 mm) |
| `TitleScene.fbx` | Mallas, 5 esqueletos y la cámara, con una sola toma de 360 cuadros a 30 fps (12 s) que cierra en bucle |
| `TitleScene_preview.png` | Fotograma de referencia 1920x1080 (cuadro 120), por si se quiere como imagen fija o carga |

La balsa naranja con Alex, Marty, Gloria y Melman sentados (clip `Idle_Sit`) y Julien de pie sobre un barril de frijoles
bailando (`Victory`), en una piscina redonda de zoológico de 10 m de radio con carril de flotadores, máquina de olas, tumbonas,
palmeras y pelotas flotando; al fondo la puerta del zoo, la noria y una ciudad pastel. Animación: la balsa cabecea (3 veces),
cada personaje repite su clip un número exacto de veces (Idle_Sit x6, Victory x8) para que el bucle no salte, tres nubes de
pedo salen por la popa, la noria da una vuelta con las góndolas siempre derechas, las banderas ondean y la cámara se mece
en un arco corto y se acerca y aleja. Objetos que Unity puede reemplazar o animar por su cuenta: `Water` (agua),
`Wheel_Wheel` y `Wheel_Gondola_0..7`, `Flagpole<N>_Flag`, `Puff_0..2`, `Raft` y sus `Chamber_*`.
Unos 100 mil triángulos en total (la tripulación ~35 mil; el resto es el escenario). Los clips se hornean cada 2 cuadros;
el FBX hornea además una pista constante para los objetos estáticos (es normal que todos tengan curva de animación).

## Otros archivos de este lote (fuera de `UI/`)

- `Tools/Export/CharacterAnims/<Nombre>/<Nombre>@Celebrate_<Celebración>.fbx`: las 6 celebraciones de la tienda para los 5
  personajes (30 clips), solo la animación con el mismo esqueleto del modelo. Clips `Celebrate_Aplauso`, `Celebrate_BaileRobot`,
  `Celebrate_Reverencia`, `Celebrate_SaludoCapitan`, `Celebrate_Voltereta` y `Celebrate_PasoConga`; de pie, empiezan y terminan
  en la postura neutra y no hacen bucle (66-84 cuadros). En Unity: rig Humanoid con "Copy From Other Avatar".
- `Tools/Export/Environment/W6/W6_<Nombre>.fbx`: escenario del Mundo 6 (35 piezas): hitos `Stage`, `Throne`, `GiantCrown`,
  `DiscoBall`, `LightTruss`; relleno `Searchlight`, `SpeakerStack`, `ConfettiCannon`, `TikiTorch`, `Banner`, `Buffet`,
  `MangoStatue`, `FloatingLanterns`, `DanceFloor`; y con 3 variantes (A, B, C) `Balloons`, `Garland`, `Drum`, `GiftBox`,
  `Buoy`, `Palm`, `Rock`.
- `Tools/Export/Party/<Nombre>.fbx`: arenas y objetos de los minijuegos (20 piezas): `MiniRaft` y `MiniRaft_J1..J4` (radio 1 m,
  4 cámaras separadas `Chamber_0_NE..3_SO`, marcador de forma por jugador), `BeachBall`, `Goal_A`, `Goal_B`, `GoalArena`,
  `RoyalPool` (parte móvil `Rotor`), `CoconutBomb` (`Spark`) y la pista `Track_Straight`, `Track_Curve90`, `Track_Start`,
  `Track_Finish`, `Track_Gates`, `Track_Ramp`, `Track_Spinner`, `Track_Slalom`, `Track_Booster`.

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

## Kit de menús y HUD — `Kit/<Grupo>/`

Dibujado en 2D con Python + Pillow (`Tools/Blender/ui_kit.py`, a 4x y reducido; no usa Blender): estilo caricatura con
contorno grueso casi negro (`#1C1A26`, el de los demás iconos), relleno con degradado suave, brillo blanco arriba y borde
interior claro. Todo a tamaño 1x para el Canvas de 1920x1080 (se importa con **Pixels Per Unit 100**, Mesh Type *Full Rect*,
sin compresión con pérdida). Sin textos. Hojas de revisión (no se suben a Git): `Tools/Blender/previews/UI_Kit_<grupo>_sheet.png`
y `UI_Kit_9slice_sheet.png` (paneles, botones, barras y carteles estirados con sus bordes).

```
python Tools/Blender/ui_kit.py                  # todo
python Tools/Blender/ui_kit.py panels buttons   # panels buttons meter ring icons frames pings events
```

**Bordes 9-slice** = px del archivo en el Sprite Editor, orden **L / T / R / B** (Left, Top, Right, Bottom). En el `Image` usar
*Image Type = Sliced* y *Fill Center*. Las esquinas (radio grueso incluido) y el contorno quedan dentro del borde, por eso
no se deforman al estirar; probado de 160x60 hasta 640x360.

### Paneles y botones (`Panels/`, `Buttons/`)

| Archivo | Tamaño | Borde L/T/R/B | Qué es |
|---|---|---|---|
| `Panels/Panel_Light` | 192x192 | 58/58/58/58 | Panel crema con doble borde y base más oscura: menús, fichas, tienda, resultados |
| `Panels/Panel_Dark` | 192x192 | 58/58/58/58 | Panel azul noche: HUD, esquinas de jugador, pausa |
| `Panels/Panel_Inset` | 160x160 | 50/50/50/50 | Hueco hundido azul claro: casillas, rejillas de logros, campo del código de sala |
| `Buttons/Button_<Color>_<Estado>` | 192x112 | 52/52/52/52 | Botón chato con base de color. Colores: `Green` (principal), `Blue` (secundario), `Red` (salir, borrar). Estados: `Normal`, `Highlighted` (más claro con aro blanco interior), `Pressed` (la cara baja 8 px y la base se achica) y `Disabled` (gris con contorno suave). 12 archivos |

Los 4 estados tienen el mismo lienzo: en `Pressed` los 8 px de arriba son transparentes (cabe en el borde superior).
Asignarlos al `Button` con *Transition = Sprite Swap* (Highlighted y Selected = `Highlighted`). Texto centrado un poco arriba
de la mitad (la base ocupa los 13 px de abajo), y 8 px más abajo en `Pressed`.

### Medidor de equipo y anillo de gas (`Meter/`, `Ring/`)

| Archivo | Tamaño | Borde L/T/R/B | Qué es |
|---|---|---|---|
| `Meter/Meter_Back` | 512x72 | 40/8/40/8 | Fondo de la barra: cápsula azul noche con sombra interior |
| `Meter/Meter_Fill` | 512x72 | 40/8/40/8 | Relleno verde gas con rayas diagonales y brillo blanco arriba. Mismo lienzo que el fondo (ya trae el margen de 10 px): poner ambos con el mismo RectTransform; *Image Type = Filled, Horizontal* |
| `Meter/Meter_Fill_Ready` | 512x72 | 40/8/40/8 | Igual en dorado con más brillo: se cambia al llegar a 100 ("¡SÚPER PEDO LISTO!") |
| `Meter/Meter_Shine` | 128x72 | — | Destello blanco inclinado con borde suave; se desliza por la barra (máscara del relleno) |
| `Meter/Meter_Glow` | 640x160 | 90/60/90/60 | Halo dorado difuso; poner detrás de la barra y pulsarlo cuando está lista |
| `Ring/Ring_Gas_Back` | 256x256 | — | Pista del anillo sobre cada animal: aro azul noche con contorno |
| `Ring/Ring_Gas_Fill` | 256x256 | — | Aro blanco que cabe dentro de la pista; *Image Type = Filled, Radial 360* y teñir con el color del jugador (J1-J4, normal o daltonismo) |
| `Ring/Ring_Gas_Glow` | 256x256 | — | Halo blanco; teñir y pulsar al cargar la ráfaga |

### Iconos (`Icons/`, 256x256, sin bordes 9-slice)

| Archivo | Qué es |
|---|---|
| `Icon_Chamber_Healthy` | Cámara de aire sana: flotador verde con anillos y válvula |
| `Icon_Chamber_Popped` | Cámara reventada (rehecha): tubo verde pálido flácido y arrugado, válvula caída, desgarro y aire que se escapa (antes parecía una roca gris) |
| `Icon_Chamber_Shield` | Escudo azul con un coco (hay coco escudo, P04); detrás asoma la cámara verde |
| `Icon_Star_Empty` / `Icon_Star_Full` | Estrella hundida azul gris / dorada con brillo (resultados y ficha del nivel) |
| `Icon_GoldenMango_Empty` / `Icon_GoldenMango_Full` | Mango dorado pendiente (silueta oscura) / conseguido |
| `Icon_Lock` | Candado dorado con ojo de cerradura (niveles, modos y artículos bloqueados) |
| `Icon_Bean` | Frijolito: moneda dorada con un frijol rojizo |
| `Icon_Gas_Empty` | Lata abierta y vacía con una nube de gas a rayas: aviso "Sin gas" cuando el gas baja de 10 % (docs/02 §11) |

### Marcos de retrato y marcas de jugador (`Frames/`)

Marco 512x512, aro de 31 px que **empieza en r = 216** (el círculo del retrato mide r = 220, ver Retratos): ponerlo encima
o detrás del retrato, centrado. Tres bandas: contorno, color con degradado y línea blanca interior.

| Archivo | Color | Notas |
|---|---|---|
| `Frame_J1` `Frame_J2` `Frame_J3` `Frame_J4` | Naranja `#F28C28`, azul `#3A86FF`, verde `#2DC653`, morado `#9B5DE5` (docs/03) | Aro liso |
| `Frame_CB_J1` … `Frame_CB_J4` | Daltonismo (docs/07 §5): `#E69F00`, `#56B4E9`, `#F0E442`, `#CC79A7` | Con la forma del jugador en la esquina inferior derecha: J1 círculo, J2 triángulo, J3 cuadrado, J4 rombo |
| `Badge_J1_Circle` `Badge_J2_Triangle` `Badge_J3_Square` `Badge_J4_Diamond` | Colores normales | 128x128, la misma forma suelta (para anillos de gas y pings sobre el animal) |
| `Badge_CB_J1_Circle` … `Badge_CB_J4_Diamond` | Colores de daltonismo | Ídem |

### Pings y avisos (`Pings/`, 256x256)

| Archivo | Qué es |
|---|---|
| `Ping_Arrow_Left` / `Ping_Arrow_Right` | Flecha curva blanca "Gira a la izquierda/derecha" (docs/02 §11). Teñir con el color del jugador |
| `Arrow_Hazard` | Flecha roja con exclamación, apunta arriba: aviso de peligro fuera de cámara en el borde de pantalla (rotarla hacia el peligro) |
| `Arrow_Current` | Doble chevrón cian, apunta arriba: flecha de corriente en zonas confusas |
| `Ping_Now_Exclamation` | Exclamación naranja con destellos: ping "¡Ahora!" (Cruceta arriba). Ya es naranja: no teñir |
| `Ping_Wait_Hand` | Mano abierta blanca: ping "Espera" (Cruceta abajo). Teñir con el color del jugador |

Los tres iconos que faltaban (exclamación, mano y lata vacía) y la nueva cámara reventada salen de `Kit/make_extra_icons.py`, que importa `Tools/Blender/ui_kit.py` sin tocarlo:

```
python Tools/Export/UI/Kit/make_extra_icons.py
```

`python Tools/Blender/ui_kit.py icons` vuelve a escribir la cámara reventada vieja: si se regenera el kit, correr este script después.

### Fondos de texto de evento (`Events/`)

| Archivo | Tamaño | Borde L/T/R/B | Qué es |
|---|---|---|---|
| `Event_Banner_Sync` | 384x128 | 72/40/72/40 | Cápsula amarillo naranja para "¡SYNC x2!", "¡SYNC x3!", "¡SYNC x4!" |
| `Event_Banner_Super` | 384x128 | 72/40/72/40 | Rosa neón: "¡SÚPER PEDO LISTO!" y nombres de sinergia |
| `Event_Banner_Info` | 384x128 | 72/40/72/40 | Azul: "CHECKPOINT" y "¡YA!" |
| `Event_Burst` | 512x512 | — | Estallido de cómic amarillo para la cuenta 3-2-1 o un SYNC grande (no se estira) |

Texto blanco con contorno oscuro, centrado; la animación (escala 0 → 1.2 → 1 en 0.25 s) la pone la UI (docs/07 §3).

