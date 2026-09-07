using UnityEngine;
using HookHorizon.Unity.Data;

namespace HookHorizon.Unity.Player
{
    /// <summary>
    /// Tags a collider as fishable water and links it to a biome. The
    /// <see cref="WaterSensor"/> finds these near the player to decide where a cast lands
    /// and which fish pool to roll from.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public sealed class WaterZone : MonoBehaviour
    {
        [SerializeField] private BiomeData biome;

        public BiomeData Biome => biome;

        public void SetBiome(BiomeData value) => biome = value;
    }
}
