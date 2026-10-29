# SETUP — Gas Safari con Claude Code

Guía de instalación de una sola vez. Copia la carpeta completa (`CLAUDE.md`, `SETUP.md`, `docs/`, `.claude/`) en la raíz de tu proyecto de Unity, al mismo nivel que `Assets/`.

```
TuProyectoUnity/
├── Assets/
├── Packages/
├── ProjectSettings/
├── CLAUDE.md
├── SETUP.md
├── docs/            (01 a 12)
└── .claude/
    ├── settings.json
    ├── skills/      (16 skills)
    └── agents/      (2 subagentes)
```

## 1. Requisitos

| Herramienta | Para qué |
|---|---|
| Unity 2022.3.62f2 con plantilla 3D (URP) | Motor |
| Claude Code (terminal o extensión de VS Code) | Desarrollo con Claude |
| Git | Puntos de restauración (más seguro que deshacer) |
| Python 3.10+ y `uv` | Servidor de MCP for Unity |
| Node.js LTS | Comando `npx skills add` |
| .NET SDK 6+ | Plugin `csharp-lsp` |
| Blender 3.0+ (opcional) | Limpiar modelos generados con IA |

## 2. Git (antes que nada)

```bash
git init
# Usa el .gitignore oficial de Unity: github.com/github/gitignore/blob/main/Unity.gitignore
git add .
git commit -m "Proyecto base"
```
Haz un commit al terminar cada tarea del backlog.

## 3. Plugins y skills: qué instalar

### Instalar (necesarios)

| Qué | Cómo | Por qué |
|---|---|---|
| **MCP for Unity** (CoplayDev, MIT) | En Unity: Window > Package Manager > + > Add package from git URL: `https://github.com/CoplayDev/unity-mcp.git?path=/MCPForUnity#main`. Luego Window > MCP for Unity y elegir Claude Code. | Claude ve la consola, crea objetos, escenas y prefabs. Es el que soporta Unity 2021.3 a 6.x. |
| **unity-mcp-orchestrator** (skill del mismo repo) | `npx skills add https://github.com/coplaydev/unity-mcp --skill unity-mcp-orchestrator` | Enseña a Claude a leer la escena antes de modificarla y a verificar después. Léela antes de usarla: una de tres auditorías la marcó con advertencia. |
| **csharp-lsp** (oficial de Anthropic) | `dotnet tool install --global csharp-ls` y luego en Claude Code: `/plugin install csharp-lsp@claude-plugins-official` | Errores de tipos y "ir a definición" sin leer archivos enteros: ahorra muchos tokens. |
| **Skills de este paquete** | Ya están en `.claude/skills/` | Las 16 skills del juego. |

### Opcionales (útiles)

| Qué | Cómo | Nota |
|---|---|---|
| `code-review` y `commit-commands` (oficiales) | `/plugin install code-review@claude-plugins-official` y `/plugin install commit-commands@claude-plugins-official` | Revisión de cambios y mensajes de commit. |
| `skill-creator` (oficial) | `/plugin install skill-creator@claude-plugins-official` | Solo si vas a crear más skills. |
| Disciplinas de **awesome-gamedev-agent-skills** (Apache-2.0) | Ver la lista: `npx skills add gamedev-skills/awesome-gamedev-agent-skills --list`. Agregar una por una: `npx skills add gamedev-skills/awesome-gamedev-agent-skills --skill game-feel` (repetir con level-design, audio-design, save-systems, camera-systems, game-ui-ux, physics-tuning, performance-optimization, game-ai) | Son independientes del motor. **No instales el paquete completo ni sus skills de Unity**: están fijadas a Unity 6.3 y chocan con 2022.3. Confirma los nombres exactos con `--list`. |
| **unity-dev-toolkit** (Dev-GOM, experimental) | `/plugin marketplace add https://github.com/Dev-GOM/claude-code-marketplace.git` y `/plugin install unity-dev-toolkit@dev-gom-plugins` | Declara soporte para 2022.3 LTS. Trae compile-fixer y test runner. Se solapa con `qa-check`: instálalo solo si lo vas a usar. |
| **Blender MCP** | Instala `addon.py` del repo ahujasid/blender-mcp en Blender y luego `claude mcp add --transport stdio blender -- uvx blender-mcp` | Para que Claude limpie modelos (escala, pivotes, reducir polígonos). Ejecuta Python arbitrario: guarda tu archivo antes de usarlo. |

### No usar en este proyecto

| Qué | Motivo |
|---|---|
| Plugin oficial de Unity para Claude Code (`Unity-Technologies/unity-agent-plugin`) | Funciona con el Unity CLI, que necesita Unity 6.0 LTS o superior. |
| `unity-technologies/skills` | Dependen del Unity CLI. |
| Citronetic/unity-claude-skill y Nice-Wolf-Studio/unity-claude-skills | Escritas para Unity 6 / 6.3: generan APIs incorrectas en 2022.3. |
| tjboudreaux/cc-plugin-unity-gamedev | Pensado para librerías que no usamos (VContainer, UniTask, Wwise, Behavior Designer); proyecto con poca actividad. |

En la extensión de VS Code los comandos `/plugin ...` no existen: escribe `/plugins` para abrir el gestor y agrega ahí el marketplace o el plugin.

Después de instalar, ejecuta `/context` en Claude Code: cada skill o plugin suma su descripción a cada sesión. Si algo no se usa, desactívalo con `/plugin`.

## 4. Paquetes de Unity

Pídele a Claude: `Usa la skill unity-csharp-2022 y la tarea M0-02 del backlog`. Instalará con MCP (`manage_packages`) los paquetes de docs/10-arquitectura.md, sección "Paquetes". Si prefieres a mano: Window > Package Manager.

## 5. Comprobar que todo funciona

1. Abre Unity con el proyecto.
2. En la terminal, en la raíz del proyecto: `claude`
3. `/mcp` → debe aparecer el servidor de Unity conectado.
4. `/context` → revisa cuánto ocupan skills, MCP y CLAUDE.md.
5. Escribe: `/next-task`

## 6. Forma de trabajar (ahorro de tokens)

| Hábito | Comando o forma |
|---|---|
| Una tarea por sesión | `/next-task`, terminar, commit, `/clear` |
| Volver a una sesión | `/rename nombre` antes de `/clear`, luego `/resume` |
| Sesión larga | `/compact conserva la tarea actual y los errores` |
| Modelo | `/model` Sonnet para lo rutinario; Opus para arquitectura, red y bugs difíciles |
| Cambios pequeños | `/effort low` |
| Tareas grandes | Modo plan (Shift+Tab) antes de ejecutar |
| Se va por mal camino | `Esc` para detener; `/rewind` para volver |
| Pedidos precisos | Nombrar archivos con `@Assets/_Project/...` |
| MCP solo cuando hace falta | Activar grupos (animation, ui, testing, probuilder, vfx, profiling) con `manage_tools` solo para esa tarea. Si solo escribes C#, desactiva el servidor con `/mcp`. |
| Lecturas pesadas | Pedir al subagente `unity-qa` o `level-reviewer`; al chat principal vuelve solo el resumen |

## 7. Modelos 3D y sonidos

- Modelos: docs/09-arte-modelos.md (lista completa, prompts, flujo Meshy → rig → Blender → Unity).
- Sonidos y música: docs/08-audio.md (lista de IDs, cómo grabar los pedos, licencias).
- Créditos y licencias de todo lo externo: `Assets/_Project/CREDITS.md`.
