# ELEGIR · selección automática de sonidos (M2-02)

Nadie escuchó estos archivos: la selección salió del análisis de `Tools/Audio/curate_audio.py` con las reglas del pedido del 2026-11-04. Lo que sigue en `Tools/Export/Audio/` es lo aprobado (ya recortado y normalizado); lo descartado se borró (queda en el historial de Git).

## Reglas aplicadas

- **Calidad**: sin clipping (tramos planos de pico); silencios de más de 20 ms al inicio/final recortados (margen de 4 ms); fades de 5 ms; WAV 44.1 kHz 16 bits.
- **Duración por tipo**: toques 0.2-0.6 s, megapedos 0.6-1.5 s, toses 0.1-1.0 s, voces de esfuerzo/ánimo 0.1-1.0 s, caída/risa 0.2-2.5 s, UI 0.01-1.0 s, loops de pedo 0.5-6 s, loops de ambiente 0.25-12 s, resto 0.05-3 s.
- **Volumen**: cada ID se lleva al LUFS (BS.1770, con compuerta) mediano de sus variantes; si el pico pasa de -1 dBFS, limitador suave.
- **Casi idénticas**: similitud coseno >= 0.997 (espectro medio en 48 bandas + envolvente); es el 1 % más parecido de los pares medidos (mediana 0.926). Se queda la de número menor.
- **Tono de pedos**: por tipo (toque, ráfaga, carga, mega, tos) los rangos de centroide espectral quedan disjuntos y en el orden Gloria < Alex < Marty < Melman < Julien; mientras dos vecinos se cruzan cae la variante más alejada de la mediana de su personaje. No se compara entre tipos (una tos es más aguda que un toque).
- **Loops** (ráfaga, carga, `*_loop`): salto de muestra <= 3 pendientes típicas y diferencia de nivel <= 3 dB entre 25 ms finales e iniciales; si no, crossfade de 40 ms; si aun así falla, se descarta.
- **Música**: `_base` siempre; `_intensity` solo si pasa la prueba de sincronía (abajo); `mus_w3_boss` siempre.
- **Voces**: las de Windows (proyecto no comercial); solo pasan el control de calidad.

## Cambio de partida: megapedos

Los 20 megapedos originales duraban 2.1-3.1 s y todos fallaban 0.6-1.5 s. Los regeneré con `python fart_synth.py --mega-only` (núcleo de 0.5-0.85 s + cola corta, generador propio) y pasaron el mismo control. Los demás archivos no cambiaron.

## `SFX/Abilities`

- `ability_bellyflop`: quedan 01 02 de 2; -19.5 LUFS
- `ability_gallop`: quedan 01 02 de 2; -18.7 LUFS
- `ability_neck`: quedan 01 02 03 de 3; -20.3 LUFS
- `ability_party`: quedan 01 02 de 2; -18.1 LUFS
- `ability_repair`: quedan 01 02 de 2; -13.4 LUFS
- `ability_roar`: quedan 01 02 03 de 3; -15.5 LUFS

## `SFX/Events`

- `checkpoint`: quedan 01 02 03 de 3; -13.0 LUFS
- `countdown_beep`: quedan 01 02 de 2; -5.0 LUFS
- `countdown_go`: quedan 01 02 de 2; -5.9 LUFS
- `finish_fanfare`: quedan 01 02 03 de 3; -9.8 LUFS
- `ping_nogas`: quedan 01 de 1; -8.4 LUFS
- `ping_now`: quedan 01 de 1; -4.5 LUFS
- `ping_turn`: quedan 01 de 1; -7.5 LUFS
- `ping_wait`: quedan 01 de 1; -7.9 LUFS
- `star_earn`: quedan 01 02 03 de 3; -15.0 LUFS
- `superfart_launch`: quedan 01 02 de 2; -14.8 LUFS
- `superfart_ready`: quedan 01 de 2; descartadas 02 (casi idéntica a superfart_ready_01.wav); -16.5 LUFS
- `sync_2`: quedan 01 02 de 2; -11.4 LUFS
- `sync_3`: quedan 01 02 de 2; -11.6 LUFS
- `sync_4`: quedan 01 02 de 2; -11.7 LUFS

## `SFX/Farts`

- `fart_bubble`: quedan 01 02 03 04 05 06 de 6; -15.7 LUFS
- `fart_fire`: quedan 01 02 03 04 05 06 de 6; -15.0 LUFS

## `SFX/Farts (por personaje)`

- `fart_blast_alex`: quedan 01 02 03 04 de 4; -12.0 LUFS; loop: crossfade en 1 de 4
- `fart_blast_gloria`: quedan 01 02 03 04 de 4; -12.9 LUFS; loop: empalme ok sin tocar
- `fart_blast_julien`: quedan 01 02 03 04 de 4; -13.2 LUFS; loop: crossfade en 1 de 4
- `fart_blast_marty`: quedan 01 02 03 04 de 4; -13.5 LUFS; loop: empalme ok sin tocar
- `fart_blast_melman`: quedan 01 02 03 04 de 4; -14.8 LUFS; loop: empalme ok sin tocar
- `fart_charge_alex`: quedan 01 02 de 2; -16.9 LUFS; loop: crossfade en 1 de 2
- `fart_charge_gloria`: quedan 01 02 de 2; -17.4 LUFS; loop: empalme ok sin tocar
- `fart_charge_julien`: quedan 01 02 de 2; -16.2 LUFS; loop: empalme ok sin tocar
- `fart_charge_marty`: quedan 01 02 de 2; -17.6 LUFS; loop: crossfade en 1 de 2
- `fart_charge_melman`: quedan 02 de 2; descartadas 01 (tono); -16.8 LUFS; loop: empalme ok sin tocar
- `fart_cough_alex`: quedan 01 02 03 04 de 4; -16.5 LUFS
- `fart_cough_gloria`: quedan 01 02 03 04 de 4; -18.7 LUFS
- `fart_cough_julien`: quedan 01 02 03 04 de 4; -15.8 LUFS
- `fart_cough_marty`: quedan 01 02 03 04 de 4; -17.0 LUFS
- `fart_cough_melman`: quedan 02 03 de 4; descartadas 01 (tono), 04 (tono); -17.3 LUFS
- `fart_mega_alex`: quedan 01 02 03 04 de 4; -13.5 LUFS
- `fart_mega_gloria`: quedan 01 02 03 04 de 4; -15.0 LUFS
- `fart_mega_julien`: quedan 01 02 03 04 de 4; -13.8 LUFS
- `fart_mega_marty`: quedan 01 02 03 04 de 4; -13.4 LUFS
- `fart_mega_melman`: quedan 01 02 03 04 de 4; -14.6 LUFS
- `fart_puff_alex`: quedan 01 02 03 05 08 09 de 12; descartadas 04 (tono), 06 (tono), 07 (casi idéntica a fart_puff_alex_03.wav), 10 (tono), 11 (casi idéntica a fart_puff_alex_03.wav), 12 (tono); -12.9 LUFS
- `fart_puff_gloria`: quedan 01 02 03 04 05 07 08 09 10 11 12 de 12; descartadas 06 (tono); -14.5 LUFS
- `fart_puff_julien`: quedan 01 02 03 04 05 06 07 08 09 11 12 de 12; descartadas 10 (tono); -13.5 LUFS
- `fart_puff_marty`: quedan 01 02 03 04 05 07 09 11 12 de 12; descartadas 06 (tono), 08 (casi idéntica a fart_puff_marty_02.wav), 10 (tono); -13.9 LUFS
- `fart_puff_melman`: quedan 01 02 03 04 05 07 08 09 11 12 de 12; descartadas 06 (tono), 10 (tono); -13.9 LUFS

## `SFX/Hazards`

- `barrel_explode`: quedan 01 02 de 2; -20.2 LUFS
- `barrel_fuse`: quedan 01 02 de 2; -14.3 LUFS
- `bats_screech`: quedan 01 02 de 2; -12.7 LUFS
- `beat_click`: quedan 01 02 de 2; -15.4 LUFS
- `coconut_whistle`: quedan 01 02 de 2; -4.3 LUFS
- `crab_snip`: quedan 01 02 de 2; -17.4 LUFS
- `crane_slam`: quedan 01 02 de 2; -20.0 LUFS
- `crane_warn`: quedan 01 02 de 2; -10.4 LUFS
- `crate_break`: quedan 01 02 03 de 3; -21.1 LUFS
- `croc_bite`: quedan 01 02 de 2; -18.6 LUFS
- `croc_wake`: quedan 01 02 de 2; -23.8 LUFS
- `fossa_attack`: quedan 01 02 de 2; -18.8 LUFS
- `fossa_rustle`: quedan 01 02 de 2; -15.1 LUFS
- `gate_close`: quedan 01 02 de 2; -21.2 LUFS
- `gate_open`: quedan 01 02 de 2; -18.1 LUFS
- `geyser_burst`: quedan 01 de 2; descartadas 02 (casi idéntica a geyser_burst_01.wav); -16.2 LUFS
- `geyser_warn`: quedan 01 02 de 2; -17.1 LUFS
- `giant_croc_roar`: quedan 01 02 de 2; -17.5 LUFS
- `log_spin_loop`: quedan 01 de 2; descartadas 02 (loop con salto tras el crossfade); -15.1 LUFS; loop: empalme ok sin tocar
- `net_tangle`: quedan 01 02 de 2; -16.4 LUFS
- `rock_impact`: quedan 01 02 de 2; -19.8 LUFS
- `rock_warn`: quedan 01 02 de 2; -15.5 LUFS
- `sand_stuck`: quedan 01 de 2; descartadas 02 (casi idéntica a sand_stuck_01.wav); -16.6 LUFS
- `spikes_hit`: quedan 01 02 de 2; -20.8 LUFS
- `tentacle_slam`: quedan 01 02 de 2; -17.8 LUFS
- `tentacle_warn`: quedan 01 02 de 2; -19.4 LUFS
- `vine_burn`: quedan 01 02 de 2; -17.2 LUFS
- `whirlpool_loop`: quedan 01 02 de 2; -17.0 LUFS; loop: empalme ok sin tocar
- `wind_gust`: quedan 01 02 de 3; descartadas 03 (casi idéntica a wind_gust_01.wav); -15.0 LUFS

## `SFX/PowerUps`

- `pickup_beans`: quedan 01 02 de 2; -8.8 LUFS
- `pickup_coconut`: quedan 01 02 de 2; -14.0 LUFS
- `pickup_golden_mango`: quedan 01 02 de 2; -13.1 LUFS
- `pickup_locoto`: quedan 01 02 de 2; -13.1 LUFS
- `pickup_mango`: quedan 01 02 de 2; -14.3 LUFS
- `pickup_onion`: quedan 01 de 2; descartadas 02 (casi idéntica a pickup_onion_01.wav); -10.0 LUFS
- `pickup_soda`: quedan 01 02 de 2; -14.8 LUFS

## `SFX/Raft`

- `raft_bounce`: quedan 01 02 03 de 3; -16.5 LUFS
- `raft_chamber_pop`: quedan 01 de 3; descartadas 02 (casi idéntica a raft_chamber_pop_01.wav), 03 (casi idéntica a raft_chamber_pop_01.wav); -18.0 LUFS
- `raft_chamber_repair`: quedan 01 02 de 2; -13.3 LUFS
- `raft_creak_loop`: quedan 01 de 2; descartadas 02 (loop con salto tras el crossfade); -23.4 LUFS; loop: empalme ok sin tocar
- `raft_hit_hard`: quedan 01 02 03 de 3; -19.6 LUFS
- `raft_hit_soft`: quedan 01 02 03 de 3; -17.5 LUFS
- `raft_land_water`: quedan 01 02 03 de 3; -21.7 LUFS
- `raft_sink`: quedan 01 02 de 2; -17.3 LUFS

## `SFX/UI`

- `ui_achievement`: quedan 01 02 de 2; -11.8 LUFS
- `ui_back`: quedan 01 02 de 2; -19.7 LUFS
- `ui_buy`: quedan 01 02 de 2; -7.9 LUFS
- `ui_join`: quedan 01 02 de 2; -10.9 LUFS
- `ui_locked`: quedan 01 02 de 2; -14.4 LUFS
- `ui_move`: quedan 01 02 03 de 3; -18.7 LUFS
- `ui_ready`: quedan 01 02 03 de 3; -11.9 LUFS
- `ui_select`: quedan 01 02 03 de 3; -15.2 LUFS

## `SFX/Water`

- `climb_aboard`: quedan 01 02 de 2; -19.7 LUFS
- `swim_stroke`: quedan 01 02 03 de 3; -20.8 LUFS
- `water_flow_loop`: quedan 02 03 de 3; descartadas 01 (loop con salto tras el crossfade); -13.0 LUFS; loop: empalme ok sin tocar
- `water_rapids_loop`: quedan 01 02 de 2; -16.6 LUFS; loop: empalme ok sin tocar
- `water_splash_big`: quedan 01 02 03 de 3; -26.7 LUFS
- `water_splash_small`: quedan 01 02 03 de 3; -22.9 LUFS
- `water_waterfall_loop`: quedan 01 02 de 2; -14.9 LUFS; loop: empalme ok sin tocar

## `Voices/alex`

- `voice_alex_cheer`: quedan 01 02 03 04 de 4; -15.9 LUFS
- `voice_alex_effort`: quedan 01 02 03 04 05 06 de 6; -18.0 LUFS
- `voice_alex_fall`: quedan 01 02 03 04 de 4; -16.6 LUFS
- `voice_alex_laugh`: quedan 01 02 03 04 de 4; -18.1 LUFS

## `Voices/gloria`

- `voice_gloria_cheer`: quedan 01 02 03 04 de 4; -13.6 LUFS
- `voice_gloria_effort`: quedan 01 02 03 04 05 06 de 6; -16.8 LUFS
- `voice_gloria_fall`: quedan 01 02 03 04 de 4; -14.2 LUFS
- `voice_gloria_laugh`: quedan 01 02 03 04 de 4; -14.7 LUFS

## `Voices/julien`

- `voice_julien_cheer`: quedan 01 02 03 04 05 de 5; -15.8 LUFS
- `voice_julien_effort`: quedan 01 02 03 04 05 06 de 6; -17.4 LUFS
- `voice_julien_fall`: quedan 01 02 03 04 05 de 5; -16.5 LUFS
- `voice_julien_laugh`: quedan 01 02 03 04 05 de 5; -16.8 LUFS

## `Voices/marty`

- `voice_marty_cheer`: quedan 01 02 03 04 de 4; -15.9 LUFS
- `voice_marty_effort`: quedan 01 02 03 04 05 06 de 6; -17.6 LUFS
- `voice_marty_fall`: quedan 01 02 03 04 de 4; -15.8 LUFS
- `voice_marty_laugh`: quedan 01 02 03 04 de 4; -17.1 LUFS

## `Voices/melman`

- `voice_melman_cheer`: quedan 01 02 03 04 de 4; -15.9 LUFS
- `voice_melman_effort`: quedan 01 02 03 04 05 06 de 6; -17.9 LUFS
- `voice_melman_fall`: quedan 01 02 03 04 de 4; -16.0 LUFS
- `voice_melman_laugh`: quedan 01 02 03 04 de 4; -17.7 LUFS

## Tono de pedos (centroide espectral en Hz, mín-máx de las variantes que quedan)

| Tipo | Gloria | Alex | Marty | Melman | Julien |
|---|---|---|---|---|---|
| puff | 47-80 (n=11) | 82-136 (n=6) | 144-187 (n=9) | 240-405 (n=10) | 419-883 (n=11) |
| blast | 63-67 (n=4) | 81-100 (n=4) | 113-153 (n=4) | 243-306 (n=4) | 442-584 (n=4) |
| charge | 102-110 (n=2) | 136-149 (n=2) | 173-206 (n=2) | 231-231 (n=1) | 306-324 (n=2) |
| mega | 68-80 (n=4) | 119-141 (n=4) | 159-189 (n=4) | 335-378 (n=4) | 516-637 (n=4) |
| cough | 233-268 (n=4) | 373-504 (n=4) | 679-779 (n=4) | 918-941 (n=2) | 1022-1799 (n=4) |

## Música · `Music/`

Prueba de sincronía: onsets (flujo espectral a 100 fps) de la intensidad contra la base, por tramos de 12 s. Pasa si en todos los tramos |desfase| <= 40 ms y correlación >= 0.10, deriva <= 40 ms y misma duración (±20 ms).

| Pista | Capa de intensidad | Desfase máx. | Correlación mín. | Deriva |
|---|---|---|---|---|
| `mus_w1` | descartada | 240 ms | 0.14 | 470 ms |
| `mus_w1_boss` | descartada | 230 ms | 0.16 | 450 ms |
| `mus_w2` | queda | 10 ms | 0.34 | 0 ms |
| `mus_w2_boss` | descartada | 230 ms | 0.11 | 380 ms |
| `mus_w3` | descartada | 210 ms | 0.03 | 370 ms |
| `mus_w3_boss` | queda | 0 ms | 0.83 | 0 ms |
| `mus_w4` | descartada | 190 ms | 0.51 | 370 ms |
| `mus_w4_boss` | queda | 10 ms | 0.55 | 0 ms |
| `mus_w5` | descartada | 140 ms | 0.45 | 170 ms |
| `mus_w5_boss` | descartada | 150 ms | 0.05 | 270 ms |
| `mus_w6` | descartada | 200 ms | 0.15 | 390 ms |
| `mus_w6_boss` | queda | 10 ms | 0.32 | 0 ms |

Pistas sin capa de intensidad: usar solo la `_base` (`MusicDirector` debe tolerar la capa ausente). `mus_title`, `mus_select`, `mus_map`, `mus_tutorial`, `mus_party` y `mus_results` no tienen capas.

### Stingers

- `stinger_victory`: queda `stinger_victory_02.ogg`. stinger_victory_01.ogg: 1.16 s, pendiente 1.11 oct/s, brillo final +457 Hz; stinger_victory_02.ogg: 0.96 s, pendiente 1.48 oct/s, brillo final +442 Hz; stinger_victory_03.ogg: 0.96 s, pendiente 1.41 oct/s, brillo final +263 Hz
- `stinger_fail`: queda `stinger_fail_03.ogg`. stinger_fail_01.ogg: 1.38 s, pendiente -0.22 oct/s, brillo final -187 Hz; stinger_fail_02.ogg: 1.38 s, pendiente -1.58 oct/s, brillo final -258 Hz; stinger_fail_03.ogg: 0.94 s, pendiente -2.62 oct/s, brillo final -243 Hz

Nota: docs/08 §5 pide stingers de 2-4 s; los de Kenney duran 1-1.4 s. Quedan así hasta que se decida alargarlos.
