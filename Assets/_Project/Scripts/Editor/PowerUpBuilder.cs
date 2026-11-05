using System;
using GasSafari.Audio;
using GasSafari.Core;
using GasSafari.PowerUps;
using GasSafari.VFX;
using UnityEditor;
using UnityEngine;

namespace GasSafari.Editor
{
    // Crea los PowerUpData P01-P07 (docs/05), sus prefabs provisionales (esfera de color) y el vfx_pickup.
    // Se puede ejecutar varias veces.
    public static class PowerUpBuilder
    {
        private const string DATA_FOLDER = "Assets/_Project/Data/PowerUps/";
        private const string PREFAB_FOLDER = "Assets/_Project/Prefabs/PowerUps/";
        private const string MATERIAL_FOLDER = "Assets/_Project/Art/PowerUps/";
        private const string VFX_LIBRARY = "Assets/_Project/Prefabs/VFX/VfxLibrary.asset";
        private const float PICKUP_RADIUS = 0.8f;

        private struct Spec
        {
            public string Id, Name;
            public Type Script;
            public PowerUpTarget Target;
            public float Duration, Magnitude, Radius;
            public bool Permanent;
            public SoundId Sound;
            public Color Color;
        }

        // Valores de docs/05. P02 radius (alcance de quema) es provisional.
        private static readonly Spec[] Specs =
        {
            new Spec { Id = "P01", Name = "Beans", Script = typeof(BeansPowerUp), Target = PowerUpTarget.NearestAnimal, Magnitude = 1f, Sound = SoundId.PickupBeans, Color = new Color(0.55f, 0.35f, 0.2f) },
            new Spec { Id = "P02", Name = "Locoto", Script = typeof(LocotoPowerUp), Target = PowerUpTarget.NearestAnimal, Duration = 6f, Magnitude = 1.5f, Radius = 3f, Sound = SoundId.PickupLocoto, Color = new Color(0.85f, 0.15f, 0.1f) },
            new Spec { Id = "P03", Name = "Mango", Script = typeof(MangoPowerUp), Target = PowerUpTarget.Team, Magnitude = 0.5f, Sound = SoundId.PickupMango, Color = new Color(1f, 0.6f, 0.15f) },
            new Spec { Id = "P04", Name = "CoconutShield", Script = typeof(CoconutPowerUp), Target = PowerUpTarget.Raft, Sound = SoundId.PickupCoconut, Color = new Color(0.45f, 0.3f, 0.15f) },
            new Spec { Id = "P05", Name = "Soda", Script = typeof(SodaPowerUp), Target = PowerUpTarget.Raft, Duration = 3f, Magnitude = 1f, Sound = SoundId.PickupSoda, Color = new Color(0.3f, 0.85f, 0.95f) },
            new Spec { Id = "P06", Name = "Onion", Script = typeof(OnionPowerUp), Target = PowerUpTarget.Raft, Duration = 6f, Radius = 8f, Sound = SoundId.PickupOnion, Color = new Color(0.7f, 0.4f, 0.8f) },
            new Spec { Id = "P07", Name = "GoldenMango", Script = typeof(GoldenMangoPowerUp), Target = PowerUpTarget.Team, Magnitude = 15f, Permanent = true, Sound = SoundId.PickupGoldenMango, Color = new Color(1f, 0.82f, 0.1f) }
        };

        [MenuItem("Gas Safari/Setup/Crear power-ups P01-P07")]
        public static void Build()
        {
            EnsureFolder("Assets/_Project/Art", "PowerUps");
            foreach (var spec in Specs)
            {
                var data = BuildData(spec);
                BuildPrefab(spec, data);
            }
            AddPickupVfx();
            AssetDatabase.SaveAssets();
        }

        private static PowerUpData BuildData(Spec spec)
        {
            var data = ProvisionalFartFeedbackBuilder.LoadOrCreate<PowerUpData>(DATA_FOLDER + spec.Id + "_" + spec.Name + ".asset");
            data.id = spec.Id;
            data.displayName = spec.Name;
            data.target = spec.Target;
            data.duration = spec.Duration;
            data.magnitude = spec.Magnitude;
            data.radius = spec.Radius;
            data.permanent = spec.Permanent;
            data.pickupSound = spec.Sound;
            EditorUtility.SetDirty(data);
            return data;
        }

        private static void BuildPrefab(Spec spec, PowerUpData data)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = spec.Id + "_" + spec.Name;
            go.layer = GameLayers.PowerUp;
            go.transform.localScale = Vector3.one * 0.6f;
            UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
            var trigger = go.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = PICKUP_RADIUS / 0.6f;

            go.GetComponent<MeshRenderer>().sharedMaterial = BuildMaterial(spec);
            var script = (PowerUpBase)go.AddComponent(spec.Script);
            var so = new SerializedObject(script);
            so.FindProperty("data").objectReferenceValue = data;
            so.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(go, PREFAB_FOLDER + go.name + ".prefab");
            UnityEngine.Object.DestroyImmediate(go);
        }

        private static Material BuildMaterial(Spec spec)
        {
            var path = MATERIAL_FOLDER + "M_" + spec.Id + "_" + spec.Name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("GasSafari/SG_Toon"));
                AssetDatabase.CreateAsset(material, path);
            }
            material.SetColor("_BaseColor", spec.Color);
            EditorUtility.SetDirty(material);
            return material;
        }

        // Destello de recogida: anillo corto de partículas con el color del jugador.
        private static void AddPickupVfx()
        {
            var library = AssetDatabase.LoadAssetAtPath<VfxLibrary>(VFX_LIBRARY);
            var entries = new System.Collections.Generic.List<VfxLibrary.Entry>(library.entries);
            entries.RemoveAll(e => e.id == VfxId.Pickup);
            entries.Add(ProvisionalFartFeedbackBuilder.VfxEntry(VfxId.Pickup, "vfx_pickup", 6, new ProvisionalFartFeedbackBuilder.Spec
            {
                Duration = 0.5f, LifeMin = 0.4f, LifeMax = 0.6f, SpeedMin = 1.5f, SpeedMax = 3f,
                SizeMin = 0.15f, SizeMax = 0.3f, Angle = 90f, BurstMin = 14, BurstMax = 18, Max = 20
            }));
            library.entries = entries.ToArray();
            EditorUtility.SetDirty(library);
        }

        private static void EnsureFolder(string parent, string name)
        {
            if (!AssetDatabase.IsValidFolder(parent + "/" + name))
                AssetDatabase.CreateFolder(parent, name);
        }
    }
}
