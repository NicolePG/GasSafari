# 08 · Audio

## 1. Mezclador (AudioMixer `GasSafariMixer`)

```
Master
├── Music      (ducking -6 dB cuando suena un evento grande)
├── SFX
│   ├── Farts
│   ├── World  (agua, obstáculos, ambiente)
│   └── Hazards
├── Voices
└── UI
```
Parámetros expuestos: `vol_master`, `vol_music`, `vol_sfx`, `vol_voices`, `vol_ui` (en dB, de -80 a 0). Snapshots: `Normal`, `Pausa` (música filtrada pasa-bajos), `SlowMo` (pitch 0.6 y reverb).

## 2. Reglas de reproducción
- Todo pasa por `AudioDirector.Play(SoundId id, Vector3 pos)`: pool de 16 `AudioSource` 3D + 4 2D para UI.
- Variación: pitch aleatorio 0.9-1.15 y nunca el mismo clip dos veces seguidas del mismo `SoundId`.
- Límite de voces: máximo 4 pedos simultáneos; el pedo que completa un grupo de sync suena +3 dB.
- Cada `SoundId` apunta a un `SoundDef` (SO): lista de clips, grupo del mixer, volumen, rango de pitch, 2D/3D, distancia máxima.

## 3. Importación
| Tipo | Load Type | Formato | Otros |
|---|---|---|---|
| SFX cortos (< 2 s) | Decompress On Load | Vorbis 70 % | Force To Mono |
| Loops (ráfaga, agua) | Compressed In Memory | Vorbis 60 % | Force To Mono |
| Música | Streaming | Vorbis 50 % | Estéreo, Preload off |

Ruta: `Assets/_Project/Audio/{SFX,Music,Voices}/`. Nombre: `<categoria>_<nombre>_<nn>.wav`.

## 4. Lista de efectos (IDs)

### Pedos (por personaje: alex, marty, melman, gloria, julien)
| ID | Clips | Nota |
|---|---|---|
| fart_puff_<pj> | 6 | 0.2-0.4 s |
| fart_blast_<pj> | 2 loops | se reproduce mientras dura la ráfaga |
| fart_charge_<pj> | 1 loop | sube de pitch con la carga |
| fart_mega_<pj> | 2 | cargado y Súper Pedo |
| fart_cough_<pj> | 2 | sin gas |
| fart_fire | 3 | variante con locoto (capa extra) |
| fart_bubble | 3 | variante con gaseosa |

### Habilidades y voces
| ID | Nota |
|---|---|
| ability_roar | Rugido de Alex (rugido caricaturesco propio) |
| ability_gallop | Galope de Marty (cascos + whoosh) |
| ability_neck | Estirón del cuello de Melman (elástico) |
| ability_bellyflop | Panzazo de Gloria (golpe + splash grande) |
| ability_party | Fiesta Real: 4 s de loop de fiesta |
| ability_repair | Botiquín de Melman (parche + inflado) |
| voice_<pj>_effort / fall / laugh / cheer | Sin palabras, grabadas por el equipo: 3 / 2 / 2 / 2 clips |

### Balsa y agua
raft_hit_soft, raft_hit_hard, raft_chamber_pop, raft_chamber_repair, raft_sink, raft_land_water, raft_bounce, raft_creak_loop, water_splash_small, water_splash_big, water_flow_loop, water_rapids_loop, water_waterfall_loop, swim_stroke, climb_aboard.

### Obstáculos
gate_open, gate_close, barrel_fuse, barrel_explode, crate_break, wind_gust, whirlpool_loop, spikes_hit, sand_stuck, vine_burn, log_spin_loop, fossa_rustle, fossa_attack, croc_wake, croc_bite, beat_click, geyser_warn, geyser_burst, rock_warn, rock_impact, bats_screech, crab_snip, coconut_whistle, crane_warn, crane_slam, tentacle_warn, tentacle_slam, giant_croc_roar, net_tangle.

### Power-ups y eventos
pickup_beans, pickup_locoto, pickup_mango, pickup_coconut, pickup_soda, pickup_onion, pickup_golden_mango, sync_2, sync_3, sync_4, superfart_ready, superfart_launch, checkpoint, countdown_beep, countdown_go, finish_fanfare, star_earn (x3 tonos), ping_now, ping_wait, ping_turn, ping_nogas.

### UI
ui_move, ui_select, ui_back, ui_locked, ui_buy, ui_achievement, ui_join, ui_ready.

## 5. Música

| ID | Uso | Estilo |
|---|---|---|
| mus_title | Título y menú | Tema principal alegre, 100 BPM |
| mus_select | Selección de personajes | Variación ligera del tema |
| mus_map | Mapa de mundos | Tema en acústico |
| mus_tutorial | T-0 | Tranquila |
| mus_w1 / mus_w1_boss | Mundo 1 | Funk urbano |
| mus_w2 / mus_w2_boss | Mundo 2 | Aventura marina |
| mus_w3 / mus_w3_boss | Mundo 3 | Percusión tropical; el jefe es la pista rítmica de 110 → 128 BPM |
| mus_w4 / mus_w4_boss | Mundo 4 | Tensión con tambores |
| mus_w5 / mus_w5_boss | Mundo 5 | Épica ligera |
| mus_w6 / mus_w6_boss | Mundo 6 | Fiesta electrónica |
| mus_party | Fiesta | Rápida, caótica |
| mus_results | Resultados | Corta, en loop |
| stinger_victory / stinger_fail | Meta / hundimiento | 2-4 s |

Música adaptativa simple: cada pista de mundo tiene 2 capas (base + intensidad). La capa de intensidad sube cuando la velocidad de la balsa supera 8 m/s o en jefes; transición de 1 s.

## 6. De dónde sale cada sonido (y licencias)

| Fuente | Uso | Licencia / condición |
|---|---|---|
| Grabación propia (celular) | Todos los pedos y voces | Propio. Pedos con la boca, globo desinflándose, botella de plástico apretada, mano en la axila. Grabar 20-30 de cada tipo en lugar silencioso y elegir los mejores |
| jsfxr (sfxr.me) | UI, pickups, sync, checkpoint | Los sonidos generados son tuyos |
| Freesound (filtro CC0) | Agua, madera, golpes, ambiente | CC0: sin atribución obligatoria (anotarlo igual) |
| ElevenLabs Sound Effects | Efectos difíciles (rugido caricaturesco, tentáculos, géiser) | Plan gratis: uso no comercial y atribución "elevenlabs.io" en créditos |
| OpenGameArt (CC0 o CC-BY) | Música | CC-BY exige crédito |

Toda fuente se registra en `Assets/_Project/CREDITS.md` (archivo, autor, licencia, URL).

## 7. Edición
Audacity: recortar silencios, normalizar a -1 dB, fade de 5 ms al inicio y al final, exportar WAV 44.1 kHz 16 bits. Ráfagas en loop: recortar en cruce por cero.
