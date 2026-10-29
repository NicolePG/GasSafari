# 09 · Arte, modelos 3D y VFX

## 1. Dirección de arte
- 3D low-poly estilizado con sombreado toon (2-3 bandas de luz + contorno opcional por mundo). Colores saturados, formas redondeadas, nada realista.
- Animales caricaturescos bípedos de **diseño propio**: no se usan capturas, modelos ni texturas de las películas o sus juegos como referencia de entrada para la IA.
- Paletas por mundo: docs/04 (tabla "Mundos").
- Postproceso (URP Volume por mundo): Bloom (fuerte en W1 y W6), Color Adjustments, Vignette suave. Sin motion blur (comodidad).
- Iluminación: 1 Directional Light por mundo; objetos estáticos con lightmaps baked + light probes para la balsa y los animales.

## 2. Shaders (Shader Graph, URP 14)
| Shader | Detalle |
|---|---|
| `SG_Toon` | Rampa de luz de 3 bandas, color base + textura, rim light suave, color de sombra por mundo |
| `SG_ToonWater` | Color por profundidad (requiere Depth Texture en el URP Asset), espuma en intersecciones, olas por vértice (seno, 2 octavas), desplazamiento de UV según la dirección de la corriente |
| `SG_Gas` | Partículas de gas con color del cosmético, disolución con ruido |
| `SG_Fog` | Niebla para W5-3 y cuevas |

## 3. Lista de modelos y presupuestos

| Grupo | Modelos | Triángulos | Rig | Animaciones |
|---|---|---|---|---|
| Personajes | Alex, Marty, Melman, Gloria, Julien | ≤ 15k c/u | Humanoid | Lista de docs/03 |
| Balsa | Balsa naranja + 7 skins de la tienda | ≤ 3k c/u | — | — (deformación por cámara reventada con escala por cuadrante) |
| Sombreros | 8 (docs/06) | ≤ 800 c/u | — | — (hijos del hueso Head) |
| Fauna | Fosa, cocodrilo, cocodrilo gigante, cangrejo, murciélago, tentáculo | 5k, 6k, 12k, 1.5k, 1k, 3k | Generic | idle, ataque, retirada (cocodrilo: dormir, despertar, nadar, morder) |
| Jefes | Grúa con gancho | ≤ 8k | Generic (huesos simples) | mover, bajar gancho |
| Power-ups | Lata de frijoles, locoto, mango, coco, gaseosa, cebolla, mango dorado | ≤ 600 c/u | — | rotación y flote por código |
| Escenario W1 | Bancas, estatua, fuentes, rejas, túneles, compuertas, boyas, lanchas, ferry, puentes, muelle, puestos, toldos, contenedores, barco de carga | ≤ 3k c/u | — | — |
| Escenario W2 | Cajas, barriles, redes, barcos hundidos, mástiles, faro, rocas, corales | ≤ 3k c/u | — | — |
| Escenario W3 | Palmeras, lianas, troncos, nenúfares, raíces, escenario de DJ, castillos de arena | ≤ 3k c/u | — | — |
| Escenario W4 | Rocas de cueva, cristales, estalactitas, géiseres, grietas | ≤ 3k c/u | — | — |
| Escenario W5 | Rocas de cañón, puentes rotos, rampas, manglares, represa, megatobogán (modular) | ≤ 3k c/u | — | — |
| Cauce | Canales modulares con ProBuilder + paredes por spline | — | — | — |

## 4. Flujo de producción de modelos

1. **Generar** (Meshy, meshy.ai): Text to 3D o Image to 3D con un dibujo propio. Modo low-poly o Smart Remesh en el presupuesto de la tabla. Texturizar.
2. **Riggear y animar**:
   - Primero el auto-rig de Meshy con sus animaciones predefinidas (idle sentado, nadar, caer, celebrar, baile).
   - Si falla: Mixamo (gratis con cuenta de Adobe; solo bípedos en pose T o A; exporta FBX). Descargar el personaje "With Skin" y las animaciones "Without Skin".
   - Cuello de la jirafa y colas: revisar deformaciones; si fallan, añadir huesos en Blender (Rigify) o animar la cola por código (rotación con seno).
3. **Limpiar** (Blender, opcional con Blender MCP): aplicar escala y rotación, origen a los pies, revisar normales, reducir polígonos si excede. Exportar FBX: Apply Scalings "FBX All", Forward "-Z Forward", Up "Y Up", Apply Transform activado.
4. **Importar** en Unity con la skill `model-import`.

Animaciones de pedo (Fart_Puff, Fart_Blast, Charge, Fart_Mega, Cough): se hacen **por código** (`FartJuice`: aplastar y estirar caderas, inflar cabeza en la carga, sacudida) para no depender del rig. El resto sale de Meshy o Mixamo.

Prompts (en inglés da mejores resultados; mantener el estilo idéntico entre personajes):
```text
Stylized cartoon lion character, bipedal, T-pose, big fluffy golden mane, friendly confident smile, simple shapes, low-poly game asset, full body
Stylized cartoon zebra character, bipedal, T-pose, bold black and white stripes, energetic expression, simple shapes, low-poly game asset, full body
Stylized cartoon giraffe character, bipedal, T-pose, very long neck, brown patches, nervous expression, simple shapes, low-poly game asset, full body
Stylized cartoon hippopotamus character, bipedal, T-pose, round body, grey skin, confident smile, simple shapes, low-poly game asset, full body
Stylized cartoon ring-tailed lemur character, bipedal, T-pose, big yellow eyes, long striped tail, small golden crown, simple shapes, low-poly game asset, full body
Inflatable round rubber raft, orange, rope handles around the rim, low-poly game asset
Giant cartoon crocodile, funny but menacing, low-poly game asset, full body
Cartoon fossa predator, slim cat-like body, low-poly game asset, full body
```

Licencias: lo generado en el plan gratis de Meshy es CC BY 4.0 (poner "Meshy" en créditos; derechos comerciales solo en Pro). Mixamo: gratis, sin regalías. Anotar todo en `CREDITS.md`.

## 5. Assets de escenario gratuitos
| Fuente | Contenido | Licencia |
|---|---|---|
| Kenney (kenney.nl) | Kits de naturaleza, ciudad, piratas, UI | CC0 |
| Quaternius | Packs low-poly de naturaleza y animales | CC0 |
| Poly Haven | Texturas y HDRI | CC0 |
| ProBuilder | Canales, rampas, toboganes y bloques del greybox | Propio |

Antes de mezclar packs, pasar todos los materiales a `SG_Toon` para que el estilo sea uniforme.

## 6. VFX (Particle System, URP)

| ID | Efecto |
|---|---|
| vfx_fart_puff | Nube de 8-12 partículas con el color de gas del jugador |
| vfx_fart_blast | Chorro continuo + estela en el agua |
| vfx_fart_mega | Anillo de onda + nube grande |
| vfx_cough | Humito gris pequeño |
| vfx_fart_fire / vfx_fart_bubble | Variantes de locoto y gaseosa |
| vfx_sync | Destello en anillo del color del equipo alrededor de la balsa |
| vfx_superfart | Estela de cohete + chispas + anillo al despegar |
| vfx_splash_small / big | Salpicaduras |
| vfx_wake | Estela detrás de la balsa según velocidad |
| vfx_chamber_pop | Pedazos de goma + soplo de aire |
| vfx_repair | Parche con brillo |
| vfx_roar | Onda en cono con hojas volando |
| vfx_gallop | Estela de rayas blanco y negro |
| vfx_neck | Polvo y líneas de movimiento en el cuello |
| vfx_bellyflop | Ola en anillo |
| vfx_party | Confeti + luces de colores |
| vfx_pickup | Brillo + estallido del color del jugador |
| vfx_checkpoint | Bandera con confeti |
| vfx_explosion | Barril explotando (caricaturesco, sin fuego realista) |
| vfx_vine_burn | Lianas quemándose |
| vfx_geyser | Vapor + chorro |
| vfx_rock_dust | Polvo de roca y sombra de aviso |
| vfx_onion | Nube verde-violeta |
| vfx_rain / vfx_lightning | Tormenta W2-2 |
| vfx_fireflies | Luciérnagas W4-2 y W6-2 |
| vfx_results_confetti | Pantalla de resultados |

Todos con pool; máximo 300 partículas activas en total en pantalla.
