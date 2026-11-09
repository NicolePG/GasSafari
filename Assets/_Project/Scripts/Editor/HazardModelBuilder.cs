using System;
using System.Collections.Generic;
using System.Linq;
using GasSafari.Core;
using GasSafari.Hazards;
using UnityEditor;
using UnityEngine;

namespace GasSafari.Editor
{
    // Integración de los modelos de Blender de Tools/Export/Hazards (README: correspondencia obstáculo -> modelo): cada prefab de
    // obstáculo cambia sus primitivas por el FBX de Art/Hazards, deja sus colliders y cablea las partes móviles (Plume, Jet,
    // Spin, Pulse...) a los scripts. Los constructores de obstáculos llaman a ApplyIfMapped al guardar cada prefab, así que
    // rehacer un prefab no pierde el modelo. El menú "Aplicar modelos a los obstáculos" lo aplica a todos.
    public static class HazardModelBuilder
    {
        private const string ART = "Assets/_Project/Art/Hazards/";
        private const string PREFABS = "Assets/_Project/Prefabs/Hazards/";

        private static readonly Dictionary<string, Action<GameObject>> Specs = new Dictionary<string, Action<GameObject>>
        {
            { "H01_Wall", Wall },
            { "H08_Wind", Wind },
            { "H09_Whirlpool", Whirlpool },
            { "H11_SandBar", SandBar },
            { "H13_SpinningLog", SpinningLog },
            { "H16_Drum", Drum },
            { "H16_Rhythm", RhythmBlock },
            { "H17_Waterfall", WaterfallEdge },
            { "H19_Geyser", Geyser },
            { "H20_RockFall", RockFall },
            { "H27_Jet", Jet },
            { "H30_Coconut", Coconut },
            { "Jet_Lateral", SideJet },
            { "H33_BalloonWall", BalloonWall },
            { "H34_HoseJet", HoseJet },
            { "H35_SinkingPlatform", SinkingPlatform },
            { "H36_GoldGasGeyser", GoldGasGeyser },
        };

        [MenuItem("Gas Safari/Setup/Aplicar modelos a los obstáculos")]
        public static void ApplyAll()
        {
            int applied = 0;
            foreach (var name in Specs.Keys)
            {
                if (AssetDatabase.LoadAssetAtPath<GameObject>(PREFABS + name + ".prefab") == null)
                    continue;
                ApplyIfMapped(name);
                applied++;
            }
            Debug.Log($"HAZARDMODELS {applied} prefabs de obstáculos con su modelo de Blender");
        }

        public static void ApplyIfMapped(string prefabName)
        {
            if (!Specs.TryGetValue(prefabName, out var apply))
                return;
            string path = PREFABS + prefabName + ".prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                apply(root);
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                    t.gameObject.layer = GameLayers.Hazard;
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        // ---------------------------------------------------------------- obstáculos
        // El collider sólido del primitivo se queda (la pared choca); solo se apaga su malla.
        private static void Wall(GameObject root)
        {
            HideRenderer(root, "Model");
            // La caja era de 10 x 2.5 x 1 con su base a -0.5 m: el modelo (4.1 x 1.0 x 1.2) se estira a esas medidas.
            AddModel(root, "H01_Wall", "Visual", new Vector3(0f, -0.5f, 0f), new Vector3(2.44f, 2.5f, 0.83f));
        }

        private static void Wind(GameObject root)
        {
            var old = Descendants(root, "Streak").ToList();
            var positions = old.Select(t => t.localPosition).ToList();
            foreach (var t in old)
                UnityEngine.Object.DestroyImmediate(t.gameObject);
            for (int i = 0; i < positions.Count; i++)
                AddModel(root, "H08_GustStreak", "Racha_" + i, positions[i], Vector3.one * 0.8f);
            // La manga señala hacia dónde empuja el viento (la zona empuja hacia +X local).
            AddModel(root, "H08_WindSock", "Manga", new Vector3(7f, 0f, -13f), Vector3.one * 1.3f);
        }

        private static void Whirlpool(GameObject root)
        {
            RemoveAll(root, "Vortex");
            var model = AddModel(root, "H09_Whirlpool", "Visual", new Vector3(0f, 0.02f, 0f), Vector3.one * 2.4f);
            var spin = Part(model, "Spin");
            if (spin != null)
                spin.gameObject.AddComponent<GasSafari.Utils.Spinner>().Configure(Vector3.up, 60f, null);
        }

        private static void SandBar(GameObject root)
        {
            RemoveAll(root, "Sand");
            // El banco asoma poco: la base del modelo queda 1 m bajo el agua.
            AddModel(root, "H11_Sandbar", "Visual", new Vector3(0f, -1f, 0f), Vector3.one * 1.1f);
        }

        private static void SpinningLog(GameObject root)
        {
            foreach (var child in root.transform.Cast<Transform>().ToList())
                UnityEngine.Object.DestroyImmediate(child.gameObject);
            AddModel(root, "H13_SpinningLog", "Visual", Vector3.zero, Vector3.one);
            RefitBoxCollider(root);
        }

        private static void Drum(GameObject root)
        {
            RemoveAll(root, "Cuerpo");
            var model = AddModel(root, "H16_BeatPillar", "Visual", Vector3.zero, Vector3.one * 0.75f);
            var drum = root.GetComponent<RhythmDrum>();
            drum.Configure(Part(model, "Pulse"), 0.2f);
            EditorUtility.SetDirty(drum);
            RefitBoxCollider(root); // sólido: los tambores están en las orillas, junto a las paredes
        }

        private static void RhythmBlock(GameObject root)
        {
            HideRenderer(root, "Bloque");
            AddModel(root, "H16_BeatPillar", "Visual", new Vector3(0f, -0.7f, 0f), Vector3.one * 0.85f);
        }

        private static void WaterfallEdge(GameObject root)
        {
            // El aviso está 10 m antes del borde (los niveles lo colocan así): el modelo del borde va en el borde.
            AddModel(root, "H17_WaterfallEdge", "Visual", new Vector3(0f, 0f, 10f), Vector3.one * 0.75f);
        }

        private static void Geyser(GameObject root)
        {
            foreach (var child in root.transform.Cast<Transform>().ToList())
                UnityEngine.Object.DestroyImmediate(child.gameObject);
            var model = AddModel(root, "H19_Geyser", "Visual", Vector3.zero, Vector3.one);
            var geyser = root.GetComponent<GasSafari.Hazards.Geyser>();
            geyser.Configure(Part(model, "Plume"), 0f);
            EditorUtility.SetDirty(geyser);
        }

        private static void RockFall(GameObject root)
        {
            RemoveAll(root, "RocaPlantilla");
            // Plantilla de la roca que cae: el modelo sin su base (la sombra la dibuja el disco rojo del aviso).
            var model = AddModel(root, "H20_FallingRock", "RocaPlantilla", Vector3.zero, Vector3.one * 0.85f);
            var staticPart = Part(model, "Static");
            if (staticPart != null)
                UnityEngine.Object.DestroyImmediate(staticPart.gameObject);
            model.SetActive(false);
            var rockFall = root.GetComponent<GasSafari.Hazards.RockFall>();
            var so = new SerializedObject(rockFall);
            so.FindProperty("rockTemplate").objectReferenceValue = model;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void Coconut(GameObject root)
        {
            RemoveAll(root, "CocoPlantilla");
            // Plantilla del coco que cae: el modelo sin su disco de sombra (la sombra la dibuja el disco rojo del aviso).
            var model = AddModel(root, "H30_Coconut", "CocoPlantilla", Vector3.zero, Vector3.one * 0.7f);
            foreach (var child in model.GetComponentsInChildren<Transform>(true).Where(t => t.name.StartsWith("Shadow")).ToList())
            {
                if (child != null)
                    UnityEngine.Object.DestroyImmediate(child.gameObject);
            }
            model.SetActive(false);
            var fall = root.GetComponent<GasSafari.Hazards.CoconutFall>();
            fall.SetTemplate(model);
            EditorUtility.SetDirty(fall);
        }

        private static void Jet(GameObject root)
        {
            RemoveAll(root, "Model");
            var model = AddModel(root, "H27_Jet", "Visual", new Vector3(0f, -0.2f, 0f), Vector3.one * 0.9f);
            var trampoline = root.GetComponent<Trampoline>();
            trampoline.Configure(Part(model, "Jet"), 0f);
            EditorUtility.SetDirty(trampoline);
        }

        private static void SideJet(GameObject root)
        {
            RemoveAll(root, "Model");
            // La manguera (H34) sirve también de chorro lateral: el chorro sale hacia +X, como empuja SoftJet.
            AddModel(root, "H34_HoseJet", "Visual", new Vector3(-0.3f, 0f, 0f), Vector3.one * 0.8f);
        }

        private static void BalloonWall(GameObject root)
        {
            RemoveAll(root, "Balloon");
            AddModel(root, "H33_BalloonWall", "Visual", new Vector3(0f, -0.5f, 0f), Vector3.one * 1.23f);
        }

        private static void HoseJet(GameObject root)
        {
            RemoveAll(root, "Nozzle");
            RemoveAll(root, "Stream");
            // Boquilla en un extremo del tramo de 12 m y el chorro estirado hasta el otro (el modelo mide 4.2 m).
            var model = AddModel(root, "H34_HoseJet", "Visual", new Vector3(-6.2f, 0f, 0f), Vector3.one);
            var stream = Part(model, "Stream");
            if (stream != null)
                stream.localScale = new Vector3(3.8f, 1f, 1f);
        }

        private static void SinkingPlatform(GameObject root)
        {
            RemoveAll(root, "Deck");
            AddModel(root, "H35_SinkingPlatform", "Visual", new Vector3(0f, 0f, 0f), Vector3.one * 1.6f);
            RefitBoxCollider(root);
        }

        private static void GoldGasGeyser(GameObject root)
        {
            RemoveAll(root, "Pozo");
            RemoveAll(root, "Chorro");
            var model = AddModel(root, "H36_GoldGasGeyser", "Visual", Vector3.zero, Vector3.one);
            var geyser = root.GetComponent<GasSafari.Hazards.GoldGasGeyser>();
            geyser.Configure(Part(model, "Jet"));
            EditorUtility.SetDirty(geyser);
        }

        // ---------------------------------------------------------------- utilidades
        internal static GameObject AddModel(GameObject root, string fbx, string name, Vector3 position, Vector3 scale)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(ART + fbx + ".fbx");
            if (asset == null)
                throw new InvalidOperationException($"Falta el modelo {ART}{fbx}.fbx (correr 'Importar obstáculos, tutorial y sombreros de Blender')");
            // Idempotente: si el prefab ya tiene ese visual (se aplicó antes), se reemplaza.
            foreach (var old in root.transform.Cast<Transform>().Where(t => t.name == name).ToList())
                UnityEngine.Object.DestroyImmediate(old.gameObject);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(asset, root.transform);
            instance.name = name;
            instance.transform.localPosition = position;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = scale;
            return instance;
        }

        private static Transform Part(GameObject model, string name)
        {
            return model.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == name);
        }

        private static IEnumerable<Transform> Descendants(GameObject root, string name)
        {
            return root.GetComponentsInChildren<Transform>(true).Where(t => t != root.transform && t.name == name);
        }

        private static void RemoveAll(GameObject root, string name)
        {
            foreach (var t in Descendants(root, name).ToList())
            {
                if (t != null)
                    UnityEngine.Object.DestroyImmediate(t.gameObject);
            }
        }

        private static void HideRenderer(GameObject root, string name)
        {
            foreach (var t in Descendants(root, name))
            {
                var renderer = t.GetComponent<MeshRenderer>();
                if (renderer != null)
                    renderer.enabled = false;
            }
        }

        // Caja del collider de la raíz a partir de las mallas del modelo.
        private static void RefitBoxCollider(GameObject root)
        {
            foreach (var old in root.GetComponents<BoxCollider>())
                UnityEngine.Object.DestroyImmediate(old);
            var renderers = root.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
                return;
            var bounds = renderers[0].bounds;
            foreach (var r in renderers)
                bounds.Encapsulate(r.bounds);
            var box = root.AddComponent<BoxCollider>();
            box.center = root.transform.InverseTransformPoint(bounds.center);
            box.size = Vector3.Max(bounds.size, Vector3.one * 0.6f);
        }
    }
}
