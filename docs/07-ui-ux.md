# 07 · Interfaz y experiencia

Tecnología: UGUI + TextMeshPro. Canvas Scaler: Scale With Screen Size 1920x1080, match 0.5. Navegación completa con mando (`InputSystemUIInputModule`). Idioma: español. Una fuente de títulos redondeada y gruesa + una fuente de texto legible (ambas con licencia OFL).

## 1. Flujo de pantallas

```
Boot ─▶ Splash (logo, 2 s, saltable) ─▶ Pantalla de título ("Pulsa cualquier botón")
   ─▶ Menú principal
        ├─ Jugar ─▶ Elegir modo ─▶ Local / Crear sala online / Unirse con código
        │            ─▶ Selección de personajes (lobby) ─▶ Mapa de mundos (Historia)
        │                                                ─▶ Ficha del nivel ─▶ Carga ─▶ Nivel
        │                                                                     ─▶ Resultados ─▶ siguiente / repetir / mapa
        ├─ Tienda (cosméticos)
        ├─ Logros y estadísticas
        ├─ Opciones
        ├─ Créditos
        └─ Salir
```

## 2. Pantallas

| Pantalla | Contenido |
|---|---|
| Título | Logo, balsa flotando con los 5 animales en idle, "Pulsa cualquier botón" |
| Menú principal | Botones verticales grandes; fondo: la balsa en la piscina del zoológico |
| Elegir modo | Tarjetas de los 8 modos; los bloqueados muestran el requisito |
| Selección de personajes | Hasta 4 columnas; "Pulsa A para unirte"; carrusel de personajes con Select_Pose, stats en barras, video corto de la habilidad; sombrero y color de gas; "Listo"; botones para añadir bots (solo anfitrión) y elegir dificultad |
| Sala online | Código de 6 letras grande, lista de jugadores, ping, botón de copiar código |
| Mapa de mundos | Mapa ilustrado con los 6 mundos como islas; niveles como puntos en el río; estrellas y mangos por nivel; candados con requisito |
| Ficha del nivel | Nombre, mecánica nueva en una frase, tiempos oro/plata, mejor tiempo, mangos 0/3, botón Contrarreloj si está desbloqueado, botón Modificadores |
| Carga | Consejo aleatorio + animación de la balsa |
| Resultados | Tiempo, estrellas animadas, mangos, frijolitos ganados, 3 premios, repetición "¿Quién fue?", botones Siguiente / Repetir / Mapa |
| Tienda | Pestañas Sombreros / Gas / Balsas / Celebraciones; vista previa 3D giratoria; saldo de frijolitos |
| Logros | Rejilla de 25 con progreso; pestaña de estadísticas |
| Opciones | Ver sección 5 |
| Créditos | Equipo, herramientas, assets y licencias (desde `CREDITS.md`) |

## 3. HUD en el nivel

| Zona | Elemento |
|---|---|
| Arriba al centro | Cronómetro + medidor de equipo (barra que brilla al llegar a 100 con "¡SÚPER PEDO LISTO!") |
| Arriba a la izquierda | 4 iconos de cámaras de aire (verde sana, gris reventada, escudo azul si hay coco) |
| Arriba a la derecha | Mangos dorados del nivel 0/3 y checkpoint actual |
| Esquinas inferiores (una por jugador) | Retrato del personaje con el color del jugador, barra de gas, icono de habilidad con recarga radial, uso de Botiquín (Melman) |
| Sobre cada animal | Anillo de gas del color del jugador, pings |
| Centro (eventos) | "¡SYNC x2!", "¡SYNC x4!", cuenta 3-2-1 "¡YA!", nombre de sinergia, "CHECKPOINT" |
| Bordes de pantalla | Flechas de aviso de peligro fuera de cámara; flecha de corriente en zonas confusas |

Animaciones: los textos de evento entran con escala 0 → 1.2 → 1 en 0.25 s y salen con fade en 0.4 s. Nunca tapar el centro de la balsa más de 1 s.

## 4. Pausa
Continuar · Reiniciar desde checkpoint (votación en multijugador) · Reiniciar nivel · Opciones · Controles · Salir al mapa. En online la pausa no detiene el juego: abre el menú solo para ese jugador.

## 5. Opciones

| Pestaña | Ajustes |
|---|---|
| Audio | Volumen general, música, efectos, voces, interfaz |
| Video | Resolución, pantalla completa/ventana, VSync, calidad (Baja/Media/Alta), límite de FPS (30/60/120/sin límite) |
| Controles | Remapeo por dispositivo (Input System rebinding), vibración on/off, sensibilidad de puntería, invertir puntería |
| Accesibilidad | Sacudida de cámara on/off, cámara lenta on/off, paleta para daltonismo (colores de jugador alternativos), tamaño de textos (100/125/150 %), alto contraste en pings, mantener vs pulsar para "Aguantar" y "Agarrarse" |
| Juego | Dificultad por defecto, mostrar consejos, idioma (español) |

Paleta de jugadores para daltonismo: J1 `#E69F00`, J2 `#56B4E9`, J3 `#F0E442`, J4 `#CC79A7` y además una forma distinta por jugador (círculo, triángulo, cuadrado, rombo).

## 6. Iconos de botones
Se muestran según el último dispositivo usado por cada jugador: Xbox, PlayStation o teclado. Sprites en `Art/UI/InputIcons/`.

## 7. Textos clave (español)

| Clave | Texto |
|---|---|
| ui.press_any | Pulsa cualquier botón |
| ui.join | Pulsa A para unirte |
| ui.ready | ¡Listo! |
| hud.sync2 / sync3 / sync4 | ¡SYNC x2! / ¡SYNC x3! / ¡SYNC x4! |
| hud.superfart_ready | ¡SÚPER PEDO LISTO! |
| hud.go | ¡YA! |
| hud.checkpoint | CHECKPOINT |
| hud.no_gas | Sin gas |
| ping.now / wait / left / right | ¡Ahora! / Espera / Gira a la izquierda / Gira a la derecha |
| results.who | ¿Quién fue? |
| assist.offer | ¿Necesitan una Ayuda Real? |

Consejos de carga (rotativos): "Empujar juntos vale más que empujar fuerte." · "Un toque corrige; una ráfaga acelera." · "Agárrate cuando tiemble la orilla." · "Gloria a bordo, viento a raya." · "Si no hay micrófono, usa los pings." · "El Cuello Gancho de Melman también rescata." · "Los frijoles recargan todo tu gas."
