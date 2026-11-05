# ELEGIR · sonidos para escuchar (M2-02)

No pude escuchar nada: todo se generó o eligió por nombre y análisis. Escucha y marca con [x] los que se quedan. Lo que no marques se borra antes de importar a `Assets/_Project/Audio/`.
Rutas relativas a `Tools/Export/Audio/`. Todos los efectos son WAV 44.1 kHz 16 bits con pico a -1 dB; la música es OGG.

## 1. Pedos por personaje (lo más importante)

Generé el doble de lo que pide docs/08 §4. Timbre: Gloria muy grave, Alex grave, Marty rápido y medio, Melman agudo y tembloroso, Julien muy agudo. Los loops (`blast`, `charge`) deben sonar sin corte al repetirse: escúchalos en loop (por ejemplo en Audacity con Shift+Espacio).

### Alex · `SFX/Farts/alex/`

| Tipo | Elegir | Variantes |
|---|---|---|
| `fart_puff_alex` | 6 de 12 | [ ] 01 [ ] 02 [ ] 03 [ ] 04 [ ] 05 [ ] 06 [ ] 07 [ ] 08 [ ] 09 [ ] 10 [ ] 11 [ ] 12 |
| `fart_blast_alex` | 2 de 4 | [ ] 01 [ ] 02 [ ] 03 [ ] 04 |
| `fart_charge_alex` | 1 de 2 | [ ] 01 [ ] 02 |
| `fart_mega_alex` | 2 de 4 | [ ] 01 [ ] 02 [ ] 03 [ ] 04 |
| `fart_cough_alex` | 2 de 4 | [ ] 01 [ ] 02 [ ] 03 [ ] 04 |

### Marty · `SFX/Farts/marty/`

| Tipo | Elegir | Variantes |
|---|---|---|
| `fart_puff_marty` | 6 de 12 | [ ] 01 [ ] 02 [ ] 03 [ ] 04 [ ] 05 [ ] 06 [ ] 07 [ ] 08 [ ] 09 [ ] 10 [ ] 11 [ ] 12 |
| `fart_blast_marty` | 2 de 4 | [ ] 01 [ ] 02 [ ] 03 [ ] 04 |
| `fart_charge_marty` | 1 de 2 | [ ] 01 [ ] 02 |
| `fart_mega_marty` | 2 de 4 | [ ] 01 [ ] 02 [ ] 03 [ ] 04 |
| `fart_cough_marty` | 2 de 4 | [ ] 01 [ ] 02 [ ] 03 [ ] 04 |

### Melman · `SFX/Farts/melman/`

| Tipo | Elegir | Variantes |
|---|---|---|
| `fart_puff_melman` | 6 de 12 | [ ] 01 [ ] 02 [ ] 03 [ ] 04 [ ] 05 [ ] 06 [ ] 07 [ ] 08 [ ] 09 [ ] 10 [ ] 11 [ ] 12 |
| `fart_blast_melman` | 2 de 4 | [ ] 01 [ ] 02 [ ] 03 [ ] 04 |
| `fart_charge_melman` | 1 de 2 | [ ] 01 [ ] 02 |
| `fart_mega_melman` | 2 de 4 | [ ] 01 [ ] 02 [ ] 03 [ ] 04 |
| `fart_cough_melman` | 2 de 4 | [ ] 01 [ ] 02 [ ] 03 [ ] 04 |

### Gloria · `SFX/Farts/gloria/`

| Tipo | Elegir | Variantes |
|---|---|---|
| `fart_puff_gloria` | 6 de 12 | [ ] 01 [ ] 02 [ ] 03 [ ] 04 [ ] 05 [ ] 06 [ ] 07 [ ] 08 [ ] 09 [ ] 10 [ ] 11 [ ] 12 |
| `fart_blast_gloria` | 2 de 4 | [ ] 01 [ ] 02 [ ] 03 [ ] 04 |
| `fart_charge_gloria` | 1 de 2 | [ ] 01 [ ] 02 |
| `fart_mega_gloria` | 2 de 4 | [ ] 01 [ ] 02 [ ] 03 [ ] 04 |
| `fart_cough_gloria` | 2 de 4 | [ ] 01 [ ] 02 [ ] 03 [ ] 04 |

### Julien · `SFX/Farts/julien/`

| Tipo | Elegir | Variantes |
|---|---|---|
| `fart_puff_julien` | 6 de 12 | [ ] 01 [ ] 02 [ ] 03 [ ] 04 [ ] 05 [ ] 06 [ ] 07 [ ] 08 [ ] 09 [ ] 10 [ ] 11 [ ] 12 |
| `fart_blast_julien` | 2 de 4 | [ ] 01 [ ] 02 [ ] 03 [ ] 04 |
| `fart_charge_julien` | 1 de 2 | [ ] 01 [ ] 02 |
| `fart_mega_julien` | 2 de 4 | [ ] 01 [ ] 02 [ ] 03 [ ] 04 |
| `fart_cough_julien` | 2 de 4 | [ ] 01 [ ] 02 [ ] 03 [ ] 04 |

### Capas globales · `SFX/Farts/`

| Tipo | Elegir | Variantes |
|---|---|---|
| `fart_fire` | 3 de 6 | [ ] 01 [ ] 02 [ ] 03 [ ] 04 [ ] 05 [ ] 06 |
| `fart_bubble` | 3 de 6 | [ ] 01 [ ] 02 [ ] 03 [ ] 04 [ ] 05 [ ] 06 |

## 2. Voces sin palabras · `Voices/<pj>/`

Windows solo tiene voces en inglés (David y Zira); no hay voz en español instalada. Usé sílabas sin palabras ("Oof", "Hup", "Yoo hoo", "Ha ha", "Aaay", "Waaah") y les cambié tono y velocidad. Si instalas una voz en español (Configuración → Hora e idioma → Voz → Agregar voces, p. ej. Sabina o Raúl de es-MX), puedo regenerarlas con `python voices.py --voz-es "<nombre>"`.

| Personaje | effort (3 de 6) | fall (2 de 4) | laugh (2 de 4) | cheer (2 de 4) |
|---|---|---|---|---|
| Alex | [ ] 01 [ ] 02 [ ] 03 [ ] 04 [ ] 05 [ ] 06 | [ ] 01 [ ] 02 [ ] 03 [ ] 04 | [ ] 01 [ ] 02 [ ] 03 [ ] 04 | [ ] 01 [ ] 02 [ ] 03 [ ] 04 |
| Marty | [ ] 01 [ ] 02 [ ] 03 [ ] 04 [ ] 05 [ ] 06 | [ ] 01 [ ] 02 [ ] 03 [ ] 04 | [ ] 01 [ ] 02 [ ] 03 [ ] 04 | [ ] 01 [ ] 02 [ ] 03 [ ] 04 |
| Melman | [ ] 01 [ ] 02 [ ] 03 [ ] 04 [ ] 05 [ ] 06 | [ ] 01 [ ] 02 [ ] 03 [ ] 04 | [ ] 01 [ ] 02 [ ] 03 [ ] 04 | [ ] 01 [ ] 02 [ ] 03 [ ] 04 |
| Gloria | [ ] 01 [ ] 02 [ ] 03 [ ] 04 [ ] 05 [ ] 06 | [ ] 01 [ ] 02 [ ] 03 [ ] 04 | [ ] 01 [ ] 02 [ ] 03 [ ] 04 | [ ] 01 [ ] 02 [ ] 03 [ ] 04 |
| Julien | [ ] 01 [ ] 02 [ ] 03 [ ] 04 [ ] 05 [ ] 06 | [ ] 01 [ ] 02 [ ] 03 [ ] 04 [ ] 05 | [ ] 01 [ ] 02 [ ] 03 [ ] 04 [ ] 05 | [ ] 01 [ ] 02 [ ] 03 [ ] 04 [ ] 05 |

## 3. Habilidades y efectos con varias opciones

Para cada ID: quédate con 1 o más (el juego alterna entre los clips que dejes). Origen de cada archivo en `sources.json`.

### `SFX/Abilities/`

- `ability_bellyflop`: [ ] 01 [ ] 02
- `ability_gallop`: [ ] 01 [ ] 02
- `ability_neck`: [ ] 01 [ ] 02 [ ] 03
- `ability_party`: [ ] 01 [ ] 02
- `ability_repair`: [ ] 01 [ ] 02
- `ability_roar`: [ ] 01 [ ] 02 [ ] 03

### `SFX/Raft/`

- `raft_bounce`: [ ] 01 [ ] 02 [ ] 03
- `raft_chamber_pop`: [ ] 01 [ ] 02 [ ] 03
- `raft_chamber_repair`: [ ] 01 [ ] 02
- `raft_creak_loop`: [ ] 01 [ ] 02
- `raft_hit_hard`: [ ] 01 [ ] 02 [ ] 03
- `raft_hit_soft`: [ ] 01 [ ] 02 [ ] 03
- `raft_land_water`: [ ] 01 [ ] 02 [ ] 03
- `raft_sink`: [ ] 01 [ ] 02

### `SFX/Water/`

- `climb_aboard`: [ ] 01 [ ] 02
- `swim_stroke`: [ ] 01 [ ] 02 [ ] 03
- `water_flow_loop`: [ ] 01 [ ] 02 [ ] 03
- `water_rapids_loop`: [ ] 01 [ ] 02
- `water_splash_big`: [ ] 01 [ ] 02 [ ] 03
- `water_splash_small`: [ ] 01 [ ] 02 [ ] 03
- `water_waterfall_loop`: [ ] 01 [ ] 02

### `SFX/Hazards/`

- `barrel_explode`: [ ] 01 [ ] 02
- `barrel_fuse`: [ ] 01 [ ] 02
- `bats_screech`: [ ] 01 [ ] 02
- `beat_click`: [ ] 01 [ ] 02
- `coconut_whistle`: [ ] 01 [ ] 02
- `crab_snip`: [ ] 01 [ ] 02
- `crane_slam`: [ ] 01 [ ] 02
- `crane_warn`: [ ] 01 [ ] 02
- `crate_break`: [ ] 01 [ ] 02 [ ] 03
- `croc_bite`: [ ] 01 [ ] 02
- `croc_wake`: [ ] 01 [ ] 02
- `fossa_attack`: [ ] 01 [ ] 02
- `fossa_rustle`: [ ] 01 [ ] 02
- `gate_close`: [ ] 01 [ ] 02
- `gate_open`: [ ] 01 [ ] 02
- `geyser_burst`: [ ] 01 [ ] 02
- `geyser_warn`: [ ] 01 [ ] 02
- `giant_croc_roar`: [ ] 01 [ ] 02
- `log_spin_loop`: [ ] 01 [ ] 02
- `net_tangle`: [ ] 01 [ ] 02
- `rock_impact`: [ ] 01 [ ] 02
- `rock_warn`: [ ] 01 [ ] 02
- `sand_stuck`: [ ] 01 [ ] 02
- `spikes_hit`: [ ] 01 [ ] 02
- `tentacle_slam`: [ ] 01 [ ] 02
- `tentacle_warn`: [ ] 01 [ ] 02
- `vine_burn`: [ ] 01 [ ] 02
- `whirlpool_loop`: [ ] 01 [ ] 02
- `wind_gust`: [ ] 01 [ ] 02 [ ] 03

### `SFX/PowerUps/`

- `pickup_beans`: [ ] 01 [ ] 02
- `pickup_coconut`: [ ] 01 [ ] 02
- `pickup_golden_mango`: [ ] 01 [ ] 02
- `pickup_locoto`: [ ] 01 [ ] 02
- `pickup_mango`: [ ] 01 [ ] 02
- `pickup_onion`: [ ] 01 [ ] 02
- `pickup_soda`: [ ] 01 [ ] 02

### `SFX/Events/`

- `checkpoint`: [ ] 01 [ ] 02 [ ] 03
- `countdown_beep`: [ ] 01 [ ] 02
- `countdown_go`: [ ] 01 [ ] 02
- `finish_fanfare`: [ ] 01 [ ] 02 [ ] 03
- `ping_nogas`: [ ] 01
- `ping_now`: [ ] 01
- `ping_turn`: [ ] 01
- `ping_wait`: [ ] 01
- `star_earn`: [ ] 01 [ ] 02 [ ] 03
- `superfart_launch`: [ ] 01 [ ] 02
- `superfart_ready`: [ ] 01 [ ] 02
- `sync_2`: [ ] 01 [ ] 02
- `sync_3`: [ ] 01 [ ] 02
- `sync_4`: [ ] 01 [ ] 02

### `SFX/UI/`

- `ui_achievement`: [ ] 01 [ ] 02
- `ui_back`: [ ] 01 [ ] 02
- `ui_buy`: [ ] 01 [ ] 02
- `ui_join`: [ ] 01 [ ] 02
- `ui_locked`: [ ] 01 [ ] 02
- `ui_move`: [ ] 01 [ ] 02 [ ] 03
- `ui_ready`: [ ] 01 [ ] 02 [ ] 03
- `ui_select`: [ ] 01 [ ] 02 [ ] 03

## 4. Música · `Music/`

Cada pista de mundo tiene `_base` (la canción CC0) y `_intensity` (percusión que sinteticé al tempo detectado). Escúchalas juntas: si la capa de intensidad va fuera de tiempo, anótalo y la rehago con otro BPM o la descartamos. Sugerencia de mezcla: intensidad a -6 dB respecto de la base.

| ID | Pista | Tempo detectado | ¿Va a tiempo? |
|---|---|---|---|
| `mus_title` | Enchanted Tiki 86 | — | — |
| `mus_select` | Flowerbed Fields [Loop] | — | — |
| `mus_map` | Bossa Nova | — | — |
| `mus_tutorial` | Balance | — | — |
| `mus_w1` | Funked Up | 87.5 | [ ] |
| `mus_w1_boss` | Action Track | 114.8 | [ ] |
| `mus_w2` | Beach Stage | 128.0 | [ ] |
| `mus_w2_boss` | Battle Theme A | 97.5 | [ ] |
| `mus_w3` | Tropical Fantasy | 97.5 | [ ] |
| `mus_w4` | 8-bit - Unknown Planet | 139.8 | [ ] |
| `mus_w4_boss` | Castle Stage | 180.0 | [ ] |
| `mus_w5` | Green Hills | 120.2 | [ ] |
| `mus_w5_boss` | Boss Battle Music | 172.2 | [ ] |
| `mus_w6` | Let the Games Begin | 129.2 | [ ] |
| `mus_w6_boss` | 8bit Action Final Boss Battle | 185.0 | [ ] |
| `mus_party` | Danza D' Bots II | — | — |
| `mus_results` | Apple Cider | — | — |
| `mus_w3_boss` | síntesis propia 110→128 BPM (build_music.py) | 110→128 | [ ] |

`mus_w3_boss` es propia: percusión tropical y marimba de 110 a 128 BPM; los tiempos de cada golpe están en `Music/mus_w3_boss_beats.json` (sirve para `beat_click` y el nivel rítmico).

### Stingers (elige 1 de cada uno)

- `stinger_victory`: [ ] 01 [ ] 02 [ ] 03 (Kenney Music Jingles: jingles_HIT15.ogg, jingles_HIT11.ogg, jingles_PIZZA02.ogg)
- `stinger_fail`: [ ] 01 [ ] 02 [ ] 03 (Kenney Music Jingles: jingles_STEEL06.ogg, jingles_STEEL01.ogg, jingles_STEEL05.ogg)

## 5. Pendiente de decidir

- Voces: ¿se quedan las de la voz de Windows o grabamos voces propias (docs/08 §6)? Ver la nota de licencia en `Tools/Export/CREDITS_pendiente.md`.
- `mus_tutorial`, `mus_party` y `mus_results` duran más de 200 s en el original: los corté a 200 s con fade (no loopean perfecto).
