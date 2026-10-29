---
name: build-release
description: Preparar y generar la build final de Windows (escenas, versión, ajustes de player y calidad, comprobaciones previas y empaquetado).
disable-model-invocation: true
---

# Build de Windows

1. **Escenas en Build Settings** en este orden: 00_Boot, 01_Title, 02_MainMenu, 03_CharacterSelect, 04_WorldMap, 99_Loading, los 31 niveles (orden de docs/04), los 5 minijuegos, E_Endless. Sin escenas de `Test/`.
2. **Player Settings:** Company y Product Name "Gas Safari", versión `1.0.0`, icono, Scripting Backend IL2CPP (o Mono si falta el módulo), API Compatibility .NET Standard 2.1, Fullscreen Window por defecto, splash propio.
3. **Calidad:** 3 niveles (Baja, Media, Alta) ligados al URP Asset correspondiente; Media por defecto.
4. **Comprobaciones previas** (skill `qa-check`, versión completa): pruebas en verde, bugs bloqueantes y críticos en 0, `CREDITS.md` completo, guardado nuevo funcionando (borrar `save.json` y probar desde cero).
5. **Build:** File > Build Settings > Windows, x86_64, carpeta `Builds/GasSafari_v1.0.0/`. Sin Development Build.
6. **Prueba de la build:** abrir el .exe, jugar T-0 y W1-1 con mando y teclado, revisar el log del jugador en `%USERPROFILE%/AppData/LocalLow/<Company>/<Product>/Player.log`.
7. **Entrega:** comprimir la carpeta en `GasSafari_v1.0.0_Windows.zip`.
