namespace GasSafari.Audio
{
    // IDs de docs/08 §4. Los pedos por personaje llegan con los SfxSet (M2-13).
    public enum SoundId
    {
        None,
        FartPuff,
        FartBlast,
        FartCharge,
        FartMega,
        FartCough,
        AbilityRoar,
        AbilityGallop,
        AbilityNeck,
        AbilityBellyflop,
        AbilityParty,
        AbilityRepair,
        PickupBeans,
        PickupLocoto,
        PickupMango,
        PickupCoconut,
        PickupSoda,
        PickupOnion,
        PickupGoldenMango,
        // Obstáculos y balsa (docs/08 §4)
        GateOpen,
        GateClose,
        CrateBreak,
        CraneWarn,
        CraneSlam,
        RaftBounce,
        RaftHitHard,
        WaterSplashBig,
        // Interfaz (docs/08 §4)
        Checkpoint,
        // Añadidos después (los valores anteriores no cambian): obstáculos de W4 (docs/08 §4 los lista; M5-22/M5-25)
        RockWarn,
        RockImpact,
        // Apagón de las luciérnagas de W4-2: provisional con wind_gust (se apagan con un soplo) y star_earn (vuelven con un destello)
        LightsOut,
        LightsOn
    }
}
