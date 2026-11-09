using System;
using UnityEngine;

namespace GasSafari.Progression
{
    public enum CosmeticCategory : byte
    {
        Hat,
        Gas,
        Raft,
        Celebration
    }

    // Cómo se consigue un artículo de la tienda (docs/06 §2).
    public enum CosmeticSource : byte
    {
        // Ya lo tienes desde el inicio.
        Starter,
        // Se compra con frijolitos.
        Price,
        // Premio por mangos dorados totales (hitos de 10, 25 y 40).
        MangoMilestone,
        // Premio por terminar W6-5 (corona dorada).
        FinishW65
    }

    // El clip de una celebración para un personaje (los 5 animales tienen los 6 clips).
    [Serializable]
    public struct CelebrationClip
    {
        public string character;
        public AnimationClip clip;
    }

    // Un artículo de la tienda de cosméticos (docs/10 §9 CosmeticData). Los textos salen de la tabla (nameKey).
    [CreateAssetMenu(menuName = "Gas Safari/Cosmetic", fileName = "Cosmetic")]
    public class CosmeticData : ScriptableObject
    {
        [Tooltip("hat_pirate_hat, gas_rainbow, raft_lily_pad, celebrate_applause: el mismo id del guardado")]
        public string id;
        public CosmeticCategory category;
        public CosmeticSource source;
        public int price;
        [Tooltip("Mangos dorados totales que lo desbloquean (solo MangoMilestone)")] public int mangosRequired;
        [Tooltip("shop.hat.pirate_hat: nombre en la tabla de textos")] public string nameKey;
        public Sprite icon;
        [Tooltip("Sombrero o piel de la balsa")] public GameObject prefab;
        [Tooltip("Solo para la vista previa de la tienda cuando no hay piel (la balsa naranja inicial)")] public GameObject previewPrefab;
        [Tooltip("Color del gas (categoría Gas)")] public Color gasColor = Color.white;
        [Tooltip("Gas Arcoíris: el color cambia en cada pedo")] public bool rainbow;
        [Tooltip("Celebración: nombre del clip Celebrate_* de cada personaje")] public string clipName;
        public CelebrationClip[] clips = new CelebrationClip[0];
        [Tooltip("Los artículos de equipo (balsa) los elige el anfitrión")] public bool teamItem;
        [Tooltip("Sin modelo propio todavía: usa otro modelo teñido")] public bool provisional;
        public Color tint = Color.white;
    }
}
