using UnityEditor;

namespace GasSafari.Editor
{
    // Al terminar las pruebas EditMode el Test Runner deshace el grupo de Undo actual. Las herramientas
    // de MCP no cierran sus grupos, así que sin esto las pruebas revierten los últimos cambios a assets.
    public static class UndoGroupGuard
    {
        [MenuItem("Gas Safari/Dev/Cerrar grupo de Undo")]
        public static void CloseGroup()
        {
            Undo.IncrementCurrentGroup();
        }
    }
}
