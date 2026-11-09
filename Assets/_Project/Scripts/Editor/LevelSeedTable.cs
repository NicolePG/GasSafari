namespace GasSafari.Editor
{
    // Datos de docs/04 (Escena, Oro, Plata, Checkpoints, mecánica). Los docs son la fuente de verdad;
    // LevelDataDocsTests compara los assets con el documento.
    internal static class LevelSeedTable
    {
        internal struct Row
        {
            public string Id, Scene, Mechanic;
            public int Gold, Silver, Checkpoints;
            public bool Boss;
        }

        internal static readonly Row[] Rows =
        {
            new Row { Id = "T-0", Scene = "N00_T0_PiscinaZoo", Gold = 150, Silver = 210, Checkpoints = 10, Boss = false,
                Mechanic = "todas las básicas. No se avanza hasta cumplir cada lección (cartel grande + icono del botón del dispositivo)." },
            new Row { Id = "W1-1", Scene = "N01_W1-1_FuentesZoo", Gold = 180, Silver = 255, Checkpoints = 3, Boss = false,
                Mechanic = "giros con puntería en curvas cerradas." },
            new Row { Id = "W1-2", Scene = "N02_W1-2_Alcantarillas", Gold = 195, Silver = 275, Checkpoints = 4, Boss = false,
                Mechanic = "compuertas temporizadas (H03)." },
            new Row { Id = "W1-3", Scene = "N03_W1-3_RioCiudad", Gold = 210, Silver = 295, Checkpoints = 4, Boss = false,
                Mechanic = "oleaje (H04) y corriente variable." },
            new Row { Id = "W1-4", Scene = "N04_W1-4_MercadoFlotante", Gold = 225, Silver = 315, Checkpoints = 4, Boss = false,
                Mechanic = "power-ups especiales Locoto (P02) y Coco escudo (P04), y cadenas de toldos trampolín (H27)." },
            new Row { Id = "W1-5", Scene = "N05_W1-5_PuertoJefe", Gold = 240, Silver = 340, Checkpoints = 4, Boss = true,
                Mechanic = "persecución (H24 grúa con gancho) + obstáculos móviles grandes (H05)." },
            new Row { Id = "W2-1", Scene = "N06_W2-1_CajasDeriva", Gold = 210, Silver = 295, Checkpoints = 4, Boss = false,
                Mechanic = "obstáculos rompibles (H07) y barriles explosivos (H06). Primer nivel sin paredes: límites con boyas y corriente que devuelve." },
            new Row { Id = "W2-2", Scene = "N07_W2-2_Tormenta", Gold = 225, Silver = 315, Checkpoints = 4, Boss = false,
                Mechanic = "viento lateral por ráfagas (H08) con indicador 1 s antes." },
            new Row { Id = "W2-3", Scene = "N08_W2-3_CementerioBarcos", Gold = 240, Silver = 340, Checkpoints = 4, Boss = false,
                Mechanic = "anclas (`Anchor`) para el Cuello Gancho de Melman. Sin Melman, las curvas se hacen con puntería y toques." },
            new Row { Id = "W2-4", Scene = "N09_W2-4_GranRemolino", Gold = 240, Silver = 340, Checkpoints = 4, Boss = false,
                Mechanic = "remolinos (H09)." },
            new Row { Id = "W2-5", Scene = "N10_W2-5_ArrecifeJefe", Gold = 270, Silver = 380, Checkpoints = 4, Boss = true,
                Mechanic = "pinchos (H10) + jefe Tentáculos (H25) que golpean en patrón." },
            new Row { Id = "W3-1", Scene = "N11_W3-1_PlayaLlegada", Gold = 210, Silver = 295, Checkpoints = 3, Boss = false,
                Mechanic = "bancos de arena (H11): por debajo de 3 m/s la balsa encalla 2 s." },
            new Row { Id = "W3-2", Scene = "N12_W3-2_RioLianas", Gold = 225, Silver = 315, Checkpoints = 4, Boss = false,
                Mechanic = "lianas que bloquean (H12), se queman con locoto (P02); troncos giratorios (H13)." },
            new Row { Id = "W3-3", Scene = "N13_W3-3_TerritorioFosas", Gold = 240, Silver = 340, Checkpoints = 4, Boss = false,
                Mechanic = "fauna que tira animales al agua (H14 fosas) y rescate." },
            new Row { Id = "W3-4", Scene = "N14_W3-4_PantanoCocodrilos", Gold = 255, Silver = 360, Checkpoints = 4, Boss = false,
                Mechanic = "cocodrilos (H15) que muerden cámaras; cebolla (P06) que ahuyenta fauna." },
            new Row { Id = "W3-5", Scene = "N15_W3-5_FiestaLemurJefe", Gold = 270, Silver = 380, Checkpoints = 4, Boss = true,
                Mechanic = "obstáculos rítmicos (H16) a 110 BPM. Pedo en el pulso (±0.1 s) = cuenta como sync y suma medidor." },
            new Row { Id = "W4-1", Scene = "N16_W4-1_CascadasEscalonadas", Gold = 240, Silver = 340, Checkpoints = 4, Boss = false,
                Mechanic = "caídas (H17): alinear antes del borde; aterrizar torcido hace rebotar." },
            new Row { Id = "W4-2", Scene = "N17_W4-2_CuevaCristal", Gold = 255, Silver = 360, Checkpoints = 4, Boss = false,
                Mechanic = "oscuridad (H18). Luciérnagas iluminan zonas; Melman avisa antes." },
            new Row { Id = "W4-3", Scene = "N18_W4-3_GeiseresVapor", Gold = 270, Silver = 380, Checkpoints = 4, Boss = false,
                Mechanic = "géiseres (H19) y control en el aire." },
            new Row { Id = "W4-4", Scene = "N19_W4-4_RioSubterraneo", Gold = 270, Silver = 380, Checkpoints = 4, Boss = false,
                Mechanic = "corrientes cruzadas (H31) y gaseosa (P05) para flotar sobre pinchos bajos; murciélagos (H26)." },
            new Row { Id = "W4-5", Scene = "N20_W4-5_VolcanJefe", Gold = 300, Silver = 420, Checkpoints = 5, Boss = true,
                Mechanic = "rocas que caen (H20) con sombra de aviso; escape contrarreloj (el techo se derrumba detrás)." },
            new Row { Id = "W5-1", Scene = "N21_W5-1_RapidosCanon", Gold = 240, Silver = 340, Checkpoints = 4, Boss = false,
                Mechanic = "rápidos (H21): corriente fuerte con rocas en el cauce." },
            new Row { Id = "W5-2", Scene = "N22_W5-2_RampasAbismo", Gold = 270, Silver = 380, Checkpoints = 4, Boss = false,
                Mechanic = "abismos (H22) que exigen Súper Pedo; rampas de salto." },
            new Row { Id = "W5-3", Scene = "N23_W5-3_LaberintoManglares", Gold = 300, Silver = 420, Checkpoints = 5, Boss = false,
                Mechanic = "rutas alternativas: segura, rápida y la de mangos dorados; atajo secreto." },
            new Row { Id = "W5-4", Scene = "N24_W5-4_Represa", Gold = 285, Silver = 400, Checkpoints = 4, Boss = false,
                Mechanic = "compuertas gigantes que liberan oleadas: subir con la ola o esperar." },
            new Row { Id = "W5-5", Scene = "N25_W5-5_MegatoboganJefe", Gold = 360, Silver = 505, Checkpoints = 6, Boss = true,
                Mechanic = "todo combinado + cocodrilo gigante perseguidor (H23)." },
            new Row { Id = "W6-1", Scene = "N26_W6-1_FiestaEspejo", Gold = 270, Silver = 380, Checkpoints = 4, Boss = false,
                Mechanic = "tramos de W1 y W3 en espejo, con obstáculos rítmicos." },
            new Row { Id = "W6-2", Scene = "N27_W6-2_NocheSinLuz", Gold = 285, Silver = 400, Checkpoints = 4, Boss = false,
                Mechanic = "oscuridad total con luciérnagas móviles + fosas + remolinos." },
            new Row { Id = "W6-3", Scene = "N28_W6-3_Contracorriente", Gold = 300, Silver = 420, Checkpoints = 5, Boss = false,
                Mechanic = "la corriente va en contra todo el nivel; exige ráfagas sincronizadas y gestión de gas." },
            new Row { Id = "W6-4", Scene = "N29_W6-4_LluviaCocos", Gold = 300, Silver = 420, Checkpoints = 4, Boss = false,
                Mechanic = "cocos caen del cielo (H30) con sombra; cocodrilos y pinchos." },
            new Row { Id = "W6-5", Scene = "N30_W6-5_CoronaRey", Gold = 390, Silver = 550, Checkpoints = 6, Boss = true,
                Mechanic = "recorrido con un tramo de cada mundo, 2 Súper Pedos obligatorios y final rítmico." },
        };
    }
}
