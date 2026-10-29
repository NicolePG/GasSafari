---
name: next-task
description: Toma la siguiente tarea pendiente de docs/12-backlog.md, la implementa con la skill indicada, la verifica y la marca como hecha. Se usa con /next-task o "siguiente tarea".
disable-model-invocation: true
---

# Siguiente tarea del backlog

1. Busca con Grep la primera línea `- [ ]` de `docs/12-backlog.md`. No leas el archivo entero.
2. Si la tarea está marcada **(TÚ)**, dile al usuario qué tiene que hacer (con la referencia del doc) y pasa a la siguiente sin marcarla.
3. Lee SOLO las secciones de docs que indica `ref` (Grep por el encabezado y lee ese bloque).
4. Carga la skill que indica la tarea y además `unity-csharp-2022` si vas a tocar C#.
5. Antes de escribir código, resume el plan en 3-5 viñetas y la lista de archivos a crear o modificar. Si la tarea es grande o ambigua, espera confirmación.
6. Implementa. Si usas MCP for Unity, agrupa operaciones con `batch_execute` y activa grupos extra de herramientas solo para esta tarea.
7. Verifica con la skill `qa-check` (versión corta: consola sin errores, Play Mode de 30 s en la escena afectada, pruebas relacionadas).
8. Marca la línea como `- [x]` y agrega la fecha al final (`· hecho AAAA-MM-DD`). Si no se pudo, deja `- [ ]` y agrega `BLOQUEADO: motivo`.
9. Responde en 3 líneas: qué se hizo, cómo se verificó, qué sigue. Sugiere hacer commit y `/clear`.

Una sola tarea por invocación.
