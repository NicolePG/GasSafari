# Señalética del tutorial T-0 (Piscina del Zoológico)

Generada con `Tools/Blender/tutorial.py` (`blender --background --python Tools/Blender/tutorial.py [-- Tutorial_BigSign ...]`).
Mismo formato que `Tools/Export/Hazards`: ≤ 3000 triángulos, pivote en la base, frente hacia -Y, vacío raíz con `Static` y las
partes intercambiables como objetos aparte. **Ninguna pieza lleva texto**: el título, el texto y los iconos de botón los pone
Unity sobre `Panel` / `Icon_0..3` (docs/07 §8). Hoja de revisión: `Tools/Blender/previews/Tutorial_sheet.png` (no se sube a Git).

| Archivo | Triángulos | Tamaño | Partes aparte | Uso (docs/04 T-0) |
|---|---|---|---|---|
| Tutorial_BigSign.fbx | 2788 | 7.2 x 1.0 x 5.2 m | `Panel`, `Icon_0..3` | Cartel grande de cada lección: `Panel` en blanco para título + texto y 4 placas redondas para un icono por jugador |
| Tutorial_PostSign.fbx | 636 | 1.4 x 0.6 x 2.8 m | `Panel` | Cartel pequeño de poste (pistas sueltas) |
| Tutorial_ArrowSign.fbx | 240 | 2.0 x 0.6 x 2.6 m | `Panel` | Flecha direccional hacia +X; para la contraria, escala X = -1 |
| Tutorial_HangingSign.fbx | 1224 | 6.1 x 0.9 x 4.7 m | `Board` | Arco con tablón colgante sobre la piscina (inicio de zona) |
| Tutorial_BubbleBarrier.fbx | 2716 | 8.4 x 1.2 x 3.4 m | `Bubble_00..20` | Barrera de burbujas que cierra cada lección: 2 surtidores + 21 burbujas sueltas que revientan al cumplir la condición |
| Tutorial_LessonBuoy_Turn.fbx | 840 | 2.2 x 2.2 x 4.2 m | `Flag` | Boya grande de la lección 3 (rodearla); la bandera lleva un panel en blanco |
| Tutorial_LessonBuoy_Marker1/2/3.fbx | 552 / 588 / 624 | 1.3 x 1.3 x 2.3 m | — | Las 3 boyas de ruta de la lección 3 (pasar por 3 triggers en orden); roja, amarilla y verde con 1, 2 y 3 puntos en relieve para el orden sin texto |
| Tutorial_LessonBuoy_Zone.fbx | 400 | 1.9 x 1.9 x 3.6 m | `Star` | Boya de inicio de lección (checkpoint); la estrella puede girar |

Notas:

- La barrera de burbujas son objetos sueltos para que cada burbuja se pueda reventar con su propio efecto; el ancho de 8 m
  se escala al ancho de cada tramo de piscina.
- Los colores son planos como el resto de modelos; en Unity se pasan al material toon del proyecto.
