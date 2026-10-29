# 04 · Niveles (31)

Busca un nivel con Grep (por ejemplo `## W3-2`) y lee solo esa sección. IDs de obstáculos (H..) y power-ups (P..) en docs/05.

## Reglas generales

- Estructura: Tutorial T-0 + 6 mundos x 5 niveles. El nivel 5 de cada mundo es **jefe** (persecución o evento).
- Cada nivel introduce **1 mecánica nueva** y la combina con una anterior.
- Ritmo 30/60/180: algo nuevo cada 30 s, checkpoint o cambio de zona cada 45-60 s, un **momento estrella** por nivel.
- 3 mangos dorados (P07) por nivel: 1 en la ruta, 1 que exige habilidad o riesgo, 1 escondido (atajo o rincón).
- Estrellas: 1 por terminar; 2 con tiempo igual o menor a **Plata**; 3 con tiempo igual o menor a **Oro** y sin hundir la balsa.
- Plata = Oro x 1.4, redondeado hacia arriba a múltiplo de 5 s.
- Escena: `Assets/_Project/Scenes/Levels/L_<ID>_<Nombre>.unity` duplicada de `_LevelTemplate`.
- Datos: un `LevelData` por nivel en `Data/Levels/` (docs/10).
- Ancho del cauce: normal 8-12 m; paso estrecho 5 m; agua abierta sin paredes en W2.

## Mundos

| Mundo | Ambiente | Paleta | Música | Mecánicas que introduce | Desbloqueo |
|---|---|---|---|---|---|
| T | Piscina del zoológico, día | Celestes y blancos | mus_tutorial | Todos los controles | Inicio |
| W1 Fuga del Zoológico | Ciudad de noche | Azul #1B2A4A, neón #FF4FA3 / #3AE1FF, farolas ámbar | mus_w1 (funk urbano) | Puntería, compuertas, oleaje, power-ups, persecución | Inicio |
| W2 Mar Abierto | Mar y tormenta | Turquesa #2EC4B6, gris #5C6B73, espuma | mus_w2 (aventura marina) | Rompibles, viento, anclas, remolinos | 8 estrellas |
| W3 Selva de Madagascar | Playa y selva | Verde #2D6A4F, naranja #F77F00, amarillo | mus_w3 (percusión tropical) | Arena, fuego, caídas y rescate, fauna, ritmo | 20 estrellas |
| W4 Cuevas y Volcán | Cuevas y volcán | Morado #3C1642, cian #4CC9F0, rojo magma #D00000 | mus_w4 (tensión) | Caídas, oscuridad, géiseres y aire, gaseosa, corrientes cruzadas | 32 estrellas |
| W5 El Gran Regreso | Cañón al atardecer | Dorado #FFB703, rojo #9C3D1C, naranja #FB8500 | mus_w5 (épica) | Rápidos, abismos, rutas, presa | 45 estrellas |
| W6 Reino de Julien (secreto) | Fiesta nocturna | Magenta #F72585, lima #B5E48C | mus_w6 (fiesta) | Remix difícil de todo | 60 mangos dorados |

Cada jefe usa la versión `_boss` de la música del mundo.

---

## T-0 Piscina del Zoológico

- Escena `L_T0_PiscinaZoo` · Oro 2:30 · Plata 3:30 · Checkpoints 7 (uno por lección) · Sin cronómetro visible
- Mecánica nueva: todas las básicas. No se avanza hasta cumplir cada lección (cartel grande + icono del botón del dispositivo).
- Lecciones:
  1. Tirarse un toque y ver moverse la balsa.
  2. Moverse por el borde.
  3. Apuntar para rodear una boya (giro).
  4. Ráfaga para cruzar la piscina.
  5. Sincronizar para romper una pared de globos (en solo: el bot te sigue).
  6. Recoger frijoles (P01) y ver el gas.
  7. Pings y cuenta 3-2-1.
  8. Agarrarse mientras un chorro de manguera empuja.
  9. Pedo cargado para saltar un tobogán.
  10. Primer Súper Pedo (medidor regalado al 100 %) para saltar el muro del zoológico.
- Mangos: sobre la boya (ruta), tras la pared de globos (sync), detrás del tobogán (escondido).
- Momento estrella: saltar el muro con el Súper Pedo y caer al canal de la ciudad (cámara lenta).

---

## W1-1 Fuentes del Zoo

- Escena `L_W1_1_FuentesZoo` · Oro 3:00 · Plata 4:15 · Checkpoints 3
- Mecánica nueva: giros con puntería en curvas cerradas.
- Secciones: 1) canal ancho entre bancas (H01); 2) rotonda alrededor de una estatua (giro de 270°); 3) zigzag entre fuentes que empujan (chorros laterales suaves); 4) fuente central que lanza la balsa (H27).
- Power-ups: P01 x3.
- Mangos: en la rotonda, pegado a la estatua (ruta); sobre un chorro (hay que dejarse elevar); tras un arbusto en la salida (escondido).
- Momento estrella: la fuente central lanza la balsa por encima de la reja hasta el canal de salida.

## W1-2 Alcantarillas

- Escena `L_W1_2_Alcantarillas` · Oro 3:15 · Plata 4:35 · Checkpoints 4
- Mecánica nueva: compuertas temporizadas (H03).
- Secciones: 1) túnel con luz de rejillas; 2) 3 compuertas en secuencia (ciclos 3 s, 2.5 s, 2 s); 3) bifurcación: izquierda segura, derecha más corta con compuerta rápida; 4) tobogán en espiral dentro de una tubería gigante.
- Power-ups: P01 x3, P03 x1 (en la ruta derecha).
- Mangos: entre la 2.ª y 3.ª compuerta; ruta derecha; nicho oscuro tras una cascada de desagüe (escondido).
- Momento estrella: bajada en espiral a toda velocidad con luces de neón en las paredes.

## W1-3 Río de la Ciudad

- Escena `L_W1_3_RioCiudad` · Oro 3:30 · Plata 4:55 · Checkpoints 4
- Mecánica nueva: oleaje (H04) y corriente variable.
- Secciones: 1) slalom de boyas; 2) lanchas que cruzan y dejan ola; 3) corriente fuerte junto al muelle (empuja hacia pilotes); 4) bajo los puentes, cruce del ferry (H28).
- Power-ups: P01 x3, P03 x1.
- Mangos: centro del slalom; montado en la estela de una lancha; bajo el último puente, entre pilotes (escondido).
- Momento estrella: el ferry cruza: pasar por delante (rápido) o esperar su ola y surfearla.

## W1-4 Mercado Flotante

- Escena `L_W1_4_MercadoFlotante` · Oro 3:45 · Plata 5:15 · Checkpoints 4
- Mecánica nueva: power-ups especiales Locoto (P02) y Coco escudo (P04), y cadenas de toldos trampolín (H27).
- Secciones: 1) pasillo de puestos con comida flotando; 2) cajas de cartón que se queman con locoto (atajo); 3) toldos trampolín que permiten saltar puestos; 4) puestos que se vuelcan al agua al pasar (H07 caen como obstáculo).
- Power-ups: P01 x2, P02 x2, P03 x2, P04 x1.
- Mangos: sobre un toldo (rebote); tras las cajas quemadas (locoto); en un puesto colgante (salto de Julien o toldo doble).
- Momento estrella: cadena de 3 toldos seguidos por encima del mercado.

## W1-5 Persecución en el Puerto (JEFE)

- Escena `L_W1_5_PuertoJefe` · Oro 4:00 · Plata 5:40 · Checkpoints 4
- Mecánica nueva: persecución (H24 grúa con gancho) + obstáculos móviles grandes (H05).
- Secciones: 1) la grúa aparece y baja el gancho 3 veces (sombra roja avisa 1 s antes); 2) pasillos de contenedores que se deslizan; 3) plataforma que se hunde (hay que salir antes de 6 s); 4) sprint final hacia la rampa del barco de carga mientras el gancho ataca cada 2 s.
- Power-ups: P01 x3, P04 x2.
- Mangos: en un contenedor abierto; bajo la plataforma que se hunde (riesgo); en la punta de la rampa (exige sync para alcanzarla).
- Momento estrella: la balsa sube por la rampa y el gancho cae detrás destrozando un contenedor.
- Fin de mundo: viñeta "Mar abierto".

---

## W2-1 Cajas a la Deriva

- Escena `L_W2_1_CajasDeriva` · Oro 3:30 · Plata 4:55 · Checkpoints 4
- Mecánica nueva: obstáculos rompibles (H07) y barriles explosivos (H06). Primer nivel sin paredes: límites con boyas y corriente que devuelve.
- Secciones: 1) campo de cajas flotantes; 2) barriles con mecha (explotan 1.5 s después de tocarlos); 3) pasillo entre redes de pesca (atrapan y frenan); 4) montaña de cajas que se derrumba.
- Power-ups: P01 x3, P02 x1, P04 x1.
- Mangos: dentro de una caja (romper con Rugido o golpe fuerte); entre dos barriles; tras la montaña de cajas si se derrumba a tiempo (atajo).
- Momento estrella: romper la montaña de cajas (Rugido o Terremoto) abre un atajo.

## W2-2 La Tormenta

- Escena `L_W2_2_Tormenta` · Oro 3:45 · Plata 5:15 · Checkpoints 4
- Mecánica nueva: viento lateral por ráfagas (H08) con indicador 1 s antes.
- Secciones: 1) lluvia y olas grandes; 2) ráfagas alternas izquierda/derecha; 3) rayos que iluminan rocas ocultas; 4) faro como guía final; ola gigante surfeable.
- Power-ups: P01 x3, P03 x2.
- Mangos: en el ojo de un remolino pequeño; detrás de una roca que solo se ve con el rayo; en la cresta de la ola gigante.
- Momento estrella: surfear la ola gigante en sincronía hasta la playa del faro.

## W2-3 Cementerio de Barcos

- Escena `L_W2_3_CementerioBarcos` · Oro 4:00 · Plata 5:40 · Checkpoints 4
- Mecánica nueva: anclas (`Anchor`) para el Cuello Gancho de Melman. Sin Melman, las curvas se hacen con puntería y toques.
- Secciones: 1) pasillos entre cascos hundidos; 2) mástiles con ancla en curvas de 180°; 3) bodega inundada (techo bajo, oscuro parcial); 4) salida entre dos barcos que se juntan lentamente.
- Power-ups: P01 x3, P04 x1.
- Mangos: en la cofa de un mástil (Cuello Gancho o salto de Julien); en la bodega; tras el timón de un barco (escondido).
- Momento estrella: Honda de la Selva alrededor del gran mástil para escapar de los barcos que se cierran.

## W2-4 El Gran Remolino

- Escena `L_W2_4_GranRemolino` · Oro 4:00 · Plata 5:40 · Checkpoints 4
- Mecánica nueva: remolinos (H09).
- Secciones: 1) remolino pequeño de práctica; 2) slalom entre dos remolinos; 3) cadena de 3 remolinos con corrientes cruzadas; 4) el gran remolino central (radio 15 m).
- Power-ups: P01 x3, P03 x1.
- Mangos: en el borde del primer remolino; entre los dos del slalom; en el centro del gran remolino (entrar y salir con Panzazo o sync x4).
- Momento estrella: escapar del gran remolino.

## W2-5 Arrecife de Coral (JEFE)

- Escena `L_W2_5_ArrecifeJefe` · Oro 4:30 · Plata 6:20 · Checkpoints 4
- Mecánica nueva: pinchos (H10) + jefe Tentáculos (H25) que golpean en patrón.
- Secciones: 1) pasillos de coral con pinchos; 2) fase 1 del jefe: 2 tentáculos, golpes cada 3 s; 3) fase 2: 4 tentáculos, cada 2 s; 4) fase 3: pasillo final, cada 1.5 s.
- Power-ups: P01 x3, P04 x3.
- Mangos: en una cueva de coral; entre tentáculos de la fase 2; en la salida tras un coral rompible.
- Momento estrella: cruzar el pasillo final mientras los tentáculos golpean detrás.
- Fin de mundo: viñeta "La isla".

---

## W3-1 Playa de Llegada

- Escena `L_W3_1_PlayaLlegada` · Oro 3:30 · Plata 4:55 · Checkpoints 3
- Mecánica nueva: bancos de arena (H11): por debajo de 3 m/s la balsa encalla 2 s.
- Secciones: 1) aguas poco profundas; 2) cangrejos (H29) que roban gas; 3) entrada al río montando una ola de marea.
- Power-ups: P01 x3, P03 x1.
- Mangos: en un castillo de arena; sobre una roca con cangrejos; detrás de una palmera caída (escondido).
- Momento estrella: entrar al río sobre la ola de la marea.

## W3-2 Río de Lianas

- Escena `L_W3_2_RioLianas` · Oro 3:45 · Plata 5:15 · Checkpoints 4
- Mecánica nueva: lianas que bloquean (H12), se queman con locoto (P02); troncos giratorios (H13).
- Secciones: 1) río cubierto de vegetación; 2) cortinas de lianas (ruta normal más larga o atajo quemando); 3) troncos giratorios; 4) túnel de lianas en llamas.
- Power-ups: P01 x3, P02 x3.
- Mangos: tras la primera cortina de lianas; sobre un tronco giratorio; en una cueva detrás de una cascada.
- Momento estrella: atravesar el túnel en llamas a toda velocidad.

## W3-3 Territorio de las Fosas

- Escena `L_W3_3_TerritorioFosas` · Oro 4:00 · Plata 5:40 · Checkpoints 4
- Mecánica nueva: fauna que tira animales al agua (H14 fosas) y rescate.
- Secciones: 1) orillas altas con fosas que saltan (aviso: arbustos se mueven 1 s antes); 2) puentes de raíces; 3) emboscada en grupo (agarrarse y empujar por turnos); 4) cascada de salida.
- Power-ups: P01 x3, P04 x1.
- Mangos: en un nido sobre las raíces; en la emboscada (aguantar sin caer); bajo la cascada.
- Momento estrella: la emboscada en grupo.

## W3-4 Pantano de los Cocodrilos

- Escena `L_W3_4_PantanoCocodrilos` · Oro 4:15 · Plata 6:00 · Checkpoints 4
- Mecánica nueva: cocodrilos (H15) que muerden cámaras; cebolla (P06) que ahuyenta fauna.
- Secciones: 1) pantano con nenúfares; 2) cocodrilos dormidos que despiertan con ruido (pedos fuertes cerca); 3) pasaje en silencio (solo toques); 4) estampida de cocodrilos.
- Power-ups: P01 x3, P04 x2, P06 x2.
- Mangos: sobre el lomo de un cocodrilo dormido; en el pasaje silencioso; tras un tronco hueco (escondido).
- Momento estrella: estampida de cocodrilos con la nube de cebolla.

## W3-5 La Gran Fiesta Lémur (JEFE)

- Escena `L_W3_5_FiestaLemurJefe` · Oro 4:30 · Plata 6:20 · Checkpoints 4
- Mecánica nueva: obstáculos rítmicos (H16) a 110 BPM. Pedo en el pulso (±0.1 s) = cuenta como sync y suma medidor.
- Secciones: 1) entrada con tambores; 2) plataformas y chorros al ritmo; 3) cambio de canción a 128 BPM a mitad del nivel; 4) escenario final con lluvia de confeti.
- Power-ups: P01 x3, P03 x2.
- Mangos: en el escenario de DJ; sobre una plataforma que solo baja en el compás 4; tras el escenario (escondido).
- Momento estrella: el cambio de canción y patrón.
- Fin de mundo: viñeta "El volcán".

---

## W4-1 Cascadas Escalonadas

- Escena `L_W4_1_CascadasEscalonadas` · Oro 4:00 · Plata 5:40 · Checkpoints 4
- Mecánica nueva: caídas (H17): alinear antes del borde; aterrizar torcido hace rebotar.
- Secciones: 1) 3 cascadas pequeñas; 2) pozas con corriente circular; 3) cascada doble con elección de lado; 4) cascada de 20 m.
- Power-ups: P01 x3, P04 x1.
- Mangos: en una poza lateral; detrás de una cascada; en una cornisa a mitad de la gran caída (control aéreo).
- Momento estrella: cascada de 20 m en cámara lenta.

## W4-2 Cueva de Cristal

- Escena `L_W4_2_CuevaCristal` · Oro 4:15 · Plata 6:00 · Checkpoints 4
- Mecánica nueva: oscuridad (H18). Luciérnagas iluminan zonas; Melman avisa antes.
- Secciones: 1) entrada con luciérnagas; 2) cristales con pinchos (H10); 3) bifurcaciones sin luz; 4) las luciérnagas se apagan 20 s.
- Power-ups: P01 x3, P04 x2.
- Mangos: en un cristal gigante; en la bifurcación oscura; sobre una estalagmita (salto de Julien).
- Momento estrella: navegar a oscuras solo con avisos y pings.

## W4-3 Géiseres de Vapor

- Escena `L_W4_3_GeiseresVapor` · Oro 4:30 · Plata 6:20 · Checkpoints 4
- Mecánica nueva: géiseres (H19) y control en el aire.
- Secciones: 1) géiseres como trampolín; 2) aterrizajes en pozas; 3) cadena de 3 géiseres sin tocar agua; 4) abismo que exige Súper Pedo.
- Power-ups: P01 x3, P03 x2.
- Mangos: en lo alto del primer géiser; entre la cadena (en el aire); bajo un puente de roca.
- Momento estrella: cadena de 3 géiseres.

## W4-4 Río Subterráneo

- Escena `L_W4_4_RioSubterraneo` · Oro 4:30 · Plata 6:20 · Checkpoints 4
- Mecánica nueva: corrientes cruzadas (H31) y gaseosa (P05) para flotar sobre pinchos bajos; murciélagos (H26).
- Secciones: 1) canales con dos corrientes que chocan; 2) campo de pinchos bajos (gaseosa o rodeo largo); 3) colonia de murciélagos; 4) salida por una grieta iluminada.
- Power-ups: P01 x3, P05 x3, P06 x1.
- Mangos: en el cruce de corrientes; sobre el campo de pinchos (gaseosa); en el techo de murciélagos (escondido).
- Momento estrella: flotar en burbuja sobre el campo de pinchos.

## W4-5 Corazón del Volcán (JEFE)

- Escena `L_W4_5_VolcanJefe` · Oro 5:00 · Plata 7:00 · Checkpoints 5
- Mecánica nueva: rocas que caen (H20) con sombra de aviso; escape contrarreloj (el techo se derrumba detrás).
- Secciones: 1) río subterráneo con temblores; 2) rocas cayendo en patrón; 3) derrumbe que persigue (si alcanza la balsa, reaparición); 4) salida por la grieta del volcán con géiser final.
- Power-ups: P01 x4, P04 x3.
- Mangos: en un desvío lateral (riesgo con el derrumbe); entre rocas que caen; en la grieta final (salto).
- Momento estrella: el géiser final lanza la balsa fuera del volcán.
- Fin de mundo: viñeta "El cañón".

---

## W5-1 Rápidos del Cañón

- Escena `L_W5_1_RapidosCanon` · Oro 4:00 · Plata 5:40 · Checkpoints 4
- Mecánica nueva: rápidos (H21): corriente fuerte con rocas en el cauce.
- Secciones: 1) entrada tranquila; 2) rápidos clase 1; 3) rápidos clase 2 con rocas; 4) el rápido más largo del juego.
- Power-ups: P01 x3, P04 x2.
- Mangos: en un remanso tras una roca; en una cascada pequeña lateral; al final del rápido largo sin frenar.
- Momento estrella: el rápido más largo sin frenar.

## W5-2 Rampas del Abismo

- Escena `L_W5_2_RampasAbismo` · Oro 4:30 · Plata 6:20 · Checkpoints 4
- Mecánica nueva: abismos (H22) que exigen Súper Pedo; rampas de salto.
- Secciones: 1) rampas pequeñas; 2) plataformas flotantes; 3) puentes rotos; 4) el gran salto (Súper Pedo obligatorio; mangos dorados del tramo cargan el medidor).
- Power-ups: P01 x3, P03 x3.
- Mangos: sobre la primera rampa; en una plataforma lateral; en el aire durante el gran salto.
- Momento estrella: cruzar el abismo con el Súper Pedo.

## W5-3 Laberinto de Manglares

- Escena `L_W5_3_LaberintoManglares` · Oro 5:00 · Plata 7:00 · Checkpoints 5
- Mecánica nueva: rutas alternativas: segura, rápida y la de mangos dorados; atajo secreto.
- Secciones: 1) primera bifurcación triple; 2) raíces como anclas; 3) zona de niebla; 4) reunión de rutas y salida.
- Power-ups: P01 x4, P02 x2, P06 x1.
- Mangos: uno en cada ruta; el atajo secreto reduce el nivel a la mitad.
- Momento estrella: descubrir el atajo secreto.

## W5-4 La Represa

- Escena `L_W5_4_Represa` · Oro 4:45 · Plata 6:40 · Checkpoints 4
- Mecánica nueva: compuertas gigantes que liberan oleadas: subir con la ola o esperar.
- Secciones: 1) canal de la represa; 2) esclusas que suben y bajan el nivel del agua; 3) compuertas gigantes; 4) caída por el aliviadero.
- Power-ups: P01 x3, P04 x2.
- Mangos: en una esclusa alta; en la cresta de una oleada; en la torre de control (escondido).
- Momento estrella: bajar por el aliviadero con la oleada.

## W5-5 El Megatobogán Final (JEFE)

- Escena `L_W5_5_MegatoboganJefe` · Oro 6:00 · Plata 8:25 · Checkpoints 6
- Mecánica nueva: todo combinado + cocodrilo gigante perseguidor (H23).
- Secciones: 1) salida en la montaña; 2) curvas peraltadas; 3) túnel oscuro; 4) géiseres y rampa; 5) el cocodrilo muerde la cola del tobogán; 6) llegada al mar y al barco.
- Power-ups: P01 x5, P04 x3, P03 x2.
- Mangos: en una curva peraltada exterior; en el túnel; en la rampa final.
- Momento estrella: el cocodrilo cierra la boca justo detrás de la balsa al saltar al barco.
- Fin: viñeta final y créditos.

---

## W6-1 Fiesta en Espejo

- Escena `L_W6_1_FiestaEspejo` · Oro 4:30 · Plata 6:20 · Checkpoints 4
- Remix: tramos de W1 y W3 en espejo, con obstáculos rítmicos.
- Mangos: en el tramo espejo del puerto; en el escenario de DJ; atajo oculto.

## W6-2 Noche sin Luz

- Escena `L_W6_2_NocheSinLuz` · Oro 4:45 · Plata 6:40 · Checkpoints 4
- Remix: oscuridad total con luciérnagas móviles + fosas + remolinos.
- Mangos: sobre una luciérnaga gigante; en un remolino; en una cueva.

## W6-3 Todo a Contracorriente

- Escena `L_W6_3_Contracorriente` · Oro 5:00 · Plata 7:00 · Checkpoints 5
- Remix: la corriente va en contra todo el nivel; exige ráfagas sincronizadas y gestión de gas.
- Mangos: en un remanso; tras una compuerta; en la cima de una cascada (subir con Súper Pedo).

## W6-4 Lluvia de Cocos

- Escena `L_W6_4_LluviaCocos` · Oro 5:00 · Plata 7:00 · Checkpoints 4
- Remix: cocos caen del cielo (H30) con sombra; cocodrilos y pinchos.
- Mangos: en el centro de la lluvia; sobre un cocodrilo; tras una palmera.

## W6-5 La Corona del Rey (JEFE FINAL SECRETO)

- Escena `L_W6_5_CoronaReyJefe` · Oro 6:30 · Plata 9:10 · Checkpoints 6
- Remix: recorrido con un tramo de cada mundo, 2 Súper Pedos obligatorios y final rítmico.
- Mangos: uno por cada tercio del recorrido.
- Recompensa: corona dorada (cosmético) y logro "Rey de la Balsa".
