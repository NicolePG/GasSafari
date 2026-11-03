using System.Collections.Generic;
using GasSafari.Characters;
using GasSafari.Core;
using GasSafari.Raft;
using UnityEngine;

namespace GasSafari.Tests
{
    // Agua plana + balsa del prefab, para pruebas PlayMode de física.
    public class RaftTestRig
    {
        private const string RAFT_PREFAB = "Assets/_Project/Prefabs/Raft/Raft.prefab";
        private const string AVATAR_PREFAB = "Assets/_Project/Prefabs/Characters/Avatar_Placeholder.prefab";
        private const string BALANCE_PATH = "Assets/_Project/Data/Balance/BalanceConfig.asset";
        private const float WATER_SCALE = 20f;

        private readonly List<GameObject> avatars = new List<GameObject>();

        public GameObject Water { get; }
        public GameObject Raft { get; }
        public BalanceConfig Balance { get; }

        public RaftTestRig(Vector3 raftPosition)
        {
            Water = GameObject.CreatePrimitive(PrimitiveType.Plane);
            Water.name = "TestWater";
            Water.layer = GameLayers.Water;
            Water.transform.localScale = new Vector3(WATER_SCALE, 1f, WATER_SCALE);

#if UNITY_EDITOR
            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(RAFT_PREFAB);
            Balance = UnityEditor.AssetDatabase.LoadAssetAtPath<BalanceConfig>(BALANCE_PATH);
            Raft = Object.Instantiate(prefab, raftPosition, Quaternion.identity);
#endif
        }

        public T Get<T>() where T : Component => Raft.GetComponent<T>();

        // Avatar provisional sentado en el ángulo dado, controlado por un ScriptedCommandSource.
        public ScriptedCommandSource AddAvatar(float angle)
        {
            GameObject go = null;
#if UNITY_EDITOR
            go = Object.Instantiate(UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(AVATAR_PREFAB));
#endif
            avatars.Add(go);
            var source = go.AddComponent<ScriptedCommandSource>();
            go.GetComponent<PlayerAvatar>().SetCommandSource(source);
            go.GetComponent<PlayerSeat>().Sit(Get<RaftSeats>(), angle);
            return source;
        }

        public void Destroy()
        {
            foreach (var avatar in avatars)
                Object.Destroy(avatar);
            Object.Destroy(Raft);
            Object.Destroy(Water);
        }
    }
}
