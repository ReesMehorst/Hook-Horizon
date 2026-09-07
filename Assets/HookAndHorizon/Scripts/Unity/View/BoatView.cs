using UnityEngine;
using HookHorizon.Unity.Data;
using HookHorizon.Unity.Player;

namespace HookHorizon.Unity.View
{
    /// <summary>
    /// Shows the current boat's model under the player while sailing. Pure view: it subscribes to
    /// <see cref="BoatController"/> board/disembark events and is never referenced by gameplay.
    /// Each <see cref="BoatData.ModelPrefab"/> is instantiated on board; boats without a model get
    /// a plain procedural hull so the game is always playable. Syncs to the restored state on load.
    /// </summary>
    public sealed class BoatView : MonoBehaviour
    {
        [SerializeField] private BoatController boat;
        private GameObject _current;

        public void Configure(BoatController controller)
        {
            Unsub();
            boat = controller;
            if (boat != null)
            {
                boat.Boarded += OnBoarded;
                boat.Disembarked += OnDisembarked;
            }
        }

        // Runs after the bootstrap has applied any loaded save, so IsSailing/ActiveBoat are set.
        private void Start()
        {
            if (boat != null && boat.IsSailing) Show(boat.ActiveBoat);
        }

        private void OnDestroy() => Unsub();

        private void Unsub()
        {
            if (boat == null) return;
            boat.Boarded -= OnBoarded;
            boat.Disembarked -= OnDisembarked;
        }

        private void OnBoarded(BoatData data) => Show(data);
        private void OnDisembarked() => Clear();

        private void Show(BoatData data)
        {
            Clear();
            _current = data != null && data.ModelPrefab != null
                ? Instantiate(data.ModelPrefab, transform, false) // prefab defines its own local offset
                : BuildFallbackHull();
            _current.name = "BoatModel";
        }

        private void Clear()
        {
            if (_current != null) Destroy(_current);
            _current = null;
        }

        private GameObject BuildFallbackHull()
        {
            var hull = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(hull.GetComponent<Collider>());
            hull.transform.SetParent(transform, worldPositionStays: false);
            hull.transform.localScale = new Vector3(1.6f, 0.4f, 2.6f);
            hull.transform.localPosition = new Vector3(0f, -0.9f, 0f); // cradles the capsule at the waterline
            hull.GetComponent<Renderer>().material.color = new Color(0.45f, 0.30f, 0.18f);
            return hull;
        }
    }
}
