using GasSafari.Core;
using GasSafari.Raft;
using UnityEngine;

namespace GasSafari.Tests
{
    // Agua plana + balsa del prefab, para pruebas PlayMode de física.
    public class RaftTestRig
    {
        private const string RAFT_PREFAB = "Assets/_Project/Prefabs/Raft/Raft.prefab";
        private const string BALANCE_PATH = "Assets/_Project/Data/Balance/BalanceConfig.asset";
        private const float WATER_SCALE = 20f;

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

        public void Destroy()
        {
            Object.Destroy(Raft);
            Object.Destroy(Water);
        }
    }
}
