---
name: save-progression
description: Implementar o modificar el guardado, la progresión (estrellas, mangos dorados, frijolitos, desbloqueos), la tienda de cosméticos, los logros o las estadísticas.
---

# Guardado y progresión

Formato del guardado: docs/10 §13. Reglas de progresión, tienda y logros: docs/06 §2, §5, §6.

## Guardado (`SaveSystem`)
1. Clase `SaveData` serializable con `JsonUtility` (sin diccionarios: usar listas de pares `LevelRecord { id, best, stars, mangos }`). Campo `version` para migraciones.
2. Ruta `Application.persistentDataPath/save.json`.
3. Escritura atómica: escribir `save.tmp` → `File.Replace(tmp, save, bak)` (o mover si no existe). Nunca escribir directo sobre el archivo bueno.
4. Carga: si falla el JSON, cargar `save.json.bak`; si también falla, datos nuevos y avisar en log.
5. Guardar en momentos clave (fin de nivel, compra, logro, cambio de opciones), no cada frame.
6. Migraciones: `SaveMigrator.Migrate(data)` sube de versión paso a paso.

## Progresión (`ProgressionService`)
- Estrellas: calcular con `LevelData` (Oro, Plata) + resultado (tiempo, hundimientos). Guardar solo si mejora.
- Mangos: máscara de 3 bits por nivel; contar con popcount.
- Frijolitos: tabla de docs/06 §2; nunca negativos.
- Desbloqueos de mundos, modos y modificadores: funciones puras `IsUnlocked(id, SaveData)` (fáciles de probar).
- Con modificadores activos no se guardan estrellas.

## Tienda y cosméticos (`CosmeticsService`)
- `CosmeticData` por artículo (categoría, precio, prefab o color). Compra: validar saldo → descontar → agregar → guardar.
- Equipamiento por jugador (sombrero, gas, celebración) y por equipo (balsa).

## Logros y estadísticas
- `AchievementService` escucha `GameEvents` y evalúa `AchievementData`. Cada logro se desbloquea una sola vez; aviso en HUD + sonido `ui_achievement`.
- `StatsService` acumula contadores en memoria y los vuelca al guardar.

## Verificación
Pruebas T-SAVE-01, T-SAVE-02, T-PROG-01, T-PROG-02 de docs/11 y QA-13.
