namespace GasSafari.Save
{
    // Sube un guardado antiguo a la versión actual, un paso por versión (docs/10 §13).
    public static class SaveMigrator
    {
        public static SaveData Migrate(SaveData data)
        {
            // Un archivo sin versión es de antes del campo: se trata como la 1.
            if (data.version <= 0)
                data.version = 1;

            // Cada versión futura agrega aquí su paso: if (data.version == 1) { ...; data.version = 2; }
            data.version = SaveData.CURRENT_VERSION;
            return data;
        }
    }
}
