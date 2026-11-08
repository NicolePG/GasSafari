# Textos en español e inglés (preparación de M10-01)

`strings.csv` reúne **631 textos** del juego en español (`es`) e inglés (`en`). Nada se importa solo: es la fuente
para construir `Strings_en.asset` y completar `Strings_es.asset` cuando se haga M10-01 (docs/12). Esta carpeta no toca
Unity.

```
python Tools/Export/Localization/check_strings.py     # revisa el CSV (claves, {n}, largo del inglés)
```

## Formato

- UTF-8 **sin BOM**, separador coma, comillas dobles solo donde hace falta, saltos de línea `\n`, una fila por texto.
  En Excel / Hojas de cálculo: importar como UTF-8 (si se guarda desde Excel, volver a exportar como "CSV UTF-8" sin BOM).
- Columnas: `key`, `es`, `en`, `contexto` (dónde sale, límites, y si ya existe en Unity; el traductor la lee, el juego no).
- Claves en minúsculas con puntos y guiones bajos, estables (no cambian si se retoca el texto). Se leen como
  `grupo.parte.detalle`: `level.w1_1.name`, `shop.hat.pirate_hat`, `ach.duet.desc`, `char.alex.ability_name`.
- Marcadores de formato `{0}`, `{1}`... iguales en las dos columnas (los usa `UIStrings.Format`). Los textos con número
  evitan plurales (`Racha: {0}`, no "{0} días") para no necesitar reglas de plural.
- **Largo:** el inglés mide como máximo un 20 % más que el español (en textos de menos de 8 letras, +2 caracteres), para que
  entre en los mismos botones. `check_strings.py` lo comprueba. Si algún texto no cabe, se acorta el inglés, no el botón.
- Los nombres de personajes no cambian (Alex, Marty, Melman, Gloria, **Rey Julien**). Tampoco `Locoto` ni los nombres de idioma
  (`Español`, `English`).

## Cómo usarlo en Unity (propuesta para M10-01)

1. `UIStrings` ya guarda `entries[] {key, text}` y devuelve la clave si falta el texto. Añadir un `Strings_en.asset` igual y un
   selector de idioma en Opciones (`options.game.language`) que elija qué asset carga `GameServices`.
2. Un menú de editor `Gas Safari/Setup/Importar textos (CSV)` que lea `strings.csv` y rellene `Strings_es.asset` y `Strings_en.asset`
   (columna `es` y columna `en`). La columna `contexto` se ignora.
3. Cambiar los textos que hoy van dentro de otros assets o del código para que lean la clave (ver "Ya existe en Unity").
4. Cuando el idioma cambia, las pantallas abiertas vuelven a pedir sus textos (`UIStrings.Get`).

## Grupos de claves

"Sin texto usable en Unity" cuenta las filas que hoy no existen en el proyecto más las 31 frases de mecánica de nivel (ver abajo).

| Prefijo | Qué es | Filas | Sin texto usable en Unity |
|---|---|---|---|
| `game`, `ui`, `lock`, `lang` | Título, botones comunes, requisitos de desbloqueo, nombres de idioma | 30 | 27 |
| `menu`, `mode`, `tt`, `mirror`, `endless`, `daily`, `race`, `mod`, `party` | Menú principal, 8 modos, 8 modificadores, 5 minijuegos y textos de cada modo | 84 | 84 |
| `select`, `stat`, `char` | Selección de personajes, stats, pasivas, habilidades y roles de los 5 animales | 62 | 57 |
| `lobby`, `net` | Sala online y errores de conexión | 28 | 28 |
| `map`, `sheet`, `level`, `world` | Mapa, ficha del nivel, 31 niveles con T-0 (nombre y mecánica nueva) y 6 mundos | 89 | 83 |
| `hud`, `synergy`, `ping`, `pause`, `results`, `award`, `assist`, `diff` | HUD, sinergias, pings, pausa, resultados, 8 premios, Ayuda Real, dificultades | 86 | 41 |
| `loading` | Etiqueta y 7 consejos de carga (docs/07 §7) | 8 | 8 |
| `powerup`, `hazard` | 7 power-ups y 36 obstáculos (solo nombre) | 50 | 50 |
| `tutorial` | 10 lecciones de T-0 (título y texto) y 2 textos de apoyo | 22 | 2 |
| `ctrl`, `options` | Acciones de control y todas las pestañas de Opciones | 51 | 51 |
| `shop`, `reward` | Tienda (30 artículos: 9 sombreros, 7 gases, 8 balsas, 6 celebraciones; 3 son premios de mangos, 1 de W6-5) y hitos | 51 | 51 |
| `ach`, `stats` | 25 logros (nombre y condición) y 11 estadísticas | 66 | 66 |
| `credits` | Encabezados de los créditos | 4 | 4 |
| **Total** | | **631** | **552** |

## Ya existe en Unity (110 filas: 79 sirven y 31 no)

Estos textos ya están en el proyecto; el CSV los marca en `contexto` con `Unity: ...` y el español de los que sirven
coincide letra por letra:

| Dónde | Claves | Estado |
|---|---|---|
| `Data/UI/Strings_es.asset` (45) | `ui.press_any/join/ready`, `hud.sync2-4/superfart_ready/go/checkpoint/no_gas/mangos/checkpoint_progress`, `ping.*`, `pause.*` (menos `online_note` y `confirm_quit`), `results.who/beans/next/retry/map`, `award.*` (solo el nombre), `assist.offer`, `synergy.*` (solo el nombre) | Solo español. Falta `Strings_en`. |
| `Data/Tutorial/T0_L01…L10.asset` (20) | `tutorial.l01…l10.title/text` | El texto está escrito dentro de cada `TutorialStep` (y en `T0LevelBuilder.cs`). Pasar a claves. |
| `WorldData.displayName` (6) | `world.w1…w6.name` | Texto fijo en el asset. |
| `CharacterData.displayName` (5) | `char.*.name` | Igual en los dos idiomas: no hace falta traducir. |
| `DifficultyConfig.displayName` (3) | `diff.*.name` | Texto fijo en el asset. |
| `LevelData.mechanicText` (31) | `level.*.mechanic` | **No sirve para el jugador**: es el texto de diseño copiado de docs/04 (lleva ids como "H03" y tecnicismos). El CSV trae una frase corta reescrita para la ficha del nivel; hay que sustituir el campo por la clave. |

## Textos que todavía no existen en Unity (521)

Todo lo demás: no hay pantalla ni dato que los use. En concreto faltan:

- **Pantallas completas:** menú principal, Elegir modo (con requisitos), selección de personajes (stats, pasivas, habilidades, roles, bots),
  sala online y sus errores, mapa de mundos, ficha del nivel, tienda, logros y estadísticas, opciones (5 pestañas), controles y créditos.
- **Modos:** Contrarreloj, Espejo, Modificadores (8), Fiesta (5 minijuegos con sus avisos), Infinito, Desafío Diario y Carrera por Equipos.
- **Datos:** nombres de los 31 niveles, descripciones de pasivas y habilidades, sinergias (descripción), power-ups (nombre y efecto),
  obstáculos (nombre), premios de resultados (descripción), dificultades (descripción) y Ayuda Real (3 opciones).
- **HUD y resultados sueltos:** `hud.player_tag`, `hud.medkit`, `hud.sunk`, `hud.rescue`, `results.title/time/stars/awards/new_record`,
  `pause.online_note`, `pause.confirm_quit`, `loading.tip_label`.

## Decisiones pendientes y huecos de los docs

- **Logro "Rey de la Balsa":** docs/04 (W6-5) dice que dar la vuelta a W6-5 desbloquea un logro con ese nombre, pero docs/06 §5 tiene
  25 logros y ese no está. No lo añadí. Si se decide, serían `ach.raft_king.name/desc` ("Rey de la Balsa" / "Raft King").
  Además "Rey de la Balsa" es el nombre de un minijuego (`party.king.name`).
- **Viñetas de historia** (`Tools/Export/Story`, docs/01): están dibujadas pero no tienen pie de texto definido. No inventé ninguno.
- **Minijuegos nuevos de M10-02** (Balsa Chocona, Relevo de Pedos, Tiro al Coco): sus reglas no existen todavía; no están en el CSV.
- **"Logros" → `Trophies`** en inglés (menú y pantalla): "Achievements" mide el doble que "Logros" y no cabe en el botón. Si se prefiere
  `Achievements`, hay que ensanchar el botón del menú.
- **"Rey Julien" sigue igual en inglés** (instrucción). En inglés se lee mejor "King Julien": cambiar `char.julien.name` y, si
  se quiere, "Reino de Julien" ya está como "Julien's Kingdom".
- **Familiar → `Family`** y **Amistades Rotas → `Broken Friendships`** (18 letras frente a 15: justo en el 20 %).
- **Voseo/tuteo en español:** el tutorial dice "Tiraos el pedo a la vez" (vosotros) y el resto del juego tutea ("Pulsa", "Agárrate").
  El CSV mantiene la cadena que hay en Unity; conviene unificar a "Tírense/Tírate" antes de importar.
- **Plurales:** `daily.streak` y `lock.mangos` no usan plurales porque la tabla no los maneja; con "1 estrellas" habría que decidir.
- **Mensajes de red, de pausa y de apoyo** (`net.err.*`, `pause.confirm_quit`, `pause.online_note`, `tutorial.step_done`...) son
  propuestas mías: no salen de los docs; confirmar antes de programarlos.
- **Créditos:** solo están los encabezados; el cuerpo sale de `CREDITS.md` y no se traduce. Un aviso legal de proyecto no comercial
  (docs/01, CLAUDE.md) no está en los docs como texto: decidir si se muestra y con qué palabras.
- **Mecánica de cada nivel:** reescrita en una frase para el jugador (docs/07 §2). Revisar que no contradiga cambios de diseño recientes.
