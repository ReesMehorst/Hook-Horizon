using System.Collections.Generic;
using UnityEngine;
using HookHorizon.Domain.World;
using HookHorizon.Unity.Data;

namespace HookHorizon.Unity.World
{
    /// <summary>
    /// Streams the generated world as isometric tile chunks around a moving target (the
    /// player). Instead of building the whole window up front — which stalls on load — it
    /// keeps a cache of built chunks, builds only a few new ones per frame as the target
    /// moves, and unloads chunks that fall outside the view radius (freeing their mesh and
    /// texture). Deterministic per coordinate, so a chunk always rebuilds identically.
    ///
    /// Each tile is a short column: a top face at its terrain height plus four walls down to
    /// a shared base, so the sun shades the sides and the height reads. A per-chunk texture
    /// holds one biome-colour pixel per tile.
    /// </summary>
    public sealed class WorldRenderer : MonoBehaviour
    {
        [SerializeField] private WorldMap map;
        [SerializeField, Min(1)] private int chunkSize = 16;
        [SerializeField, Min(0)] private int renderRadiusChunks = 4;
        [Tooltip("How many chunks to build per frame while streaming (higher = fills faster, bigger hitches).")]
        [SerializeField, Min(1)] private int chunksPerFrame = 2;
        [Tooltip("Extra ring kept loaded beyond the render radius before a chunk is unloaded (hysteresis).")]
        [SerializeField, Min(0)] private int keepPadding = 1;
        [Tooltip("Radius (in chunks) built instantly on the first BuildAround, so there is ground on spawn.")]
        [SerializeField, Min(0)] private int initialRadiusChunks = 1;

        private Transform _target;
        private Material _baseMaterial;
        private readonly Dictionary<long, GameObject> _chunks = new Dictionary<long, GameObject>();
        private readonly List<(int cx, int cz)> _buildQueue = new List<(int, int)>();
        private int _centerCx, _centerCz;
        private bool _hasCenter;

        public void Configure(WorldMap worldMap) => map = worldMap;

        /// <summary>Stream chunks around this transform as it moves (usually the player).</summary>
        public void SetTarget(Transform target) => _target = target;

        /// <summary>
        /// Seed the view at a tile: build the immediate area now (so the player never spawns
        /// over the void) and queue the rest of the radius to stream in over the next frames.
        /// </summary>
        public void BuildAround(int centerTileX, int centerTileZ)
        {
            EnsureMaterial();
            _centerCx = FloorDiv(centerTileX, chunkSize);
            _centerCz = FloorDiv(centerTileZ, chunkSize);
            _hasCenter = true;

            for (int dz = -initialRadiusChunks; dz <= initialRadiusChunks; dz++)
                for (int dx = -initialRadiusChunks; dx <= initialRadiusChunks; dx++)
                    EnsureChunk(_centerCx + dx, _centerCz + dz);

            RecomputeQueue();
        }

        private void Update()
        {
            if (map == null) return;

            if (_target != null)
            {
                int tx = map.WorldToTileX(_target.position.x);
                int tz = map.WorldToTileZ(_target.position.z);
                int ccx = FloorDiv(tx, chunkSize);
                int ccz = FloorDiv(tz, chunkSize);
                if (!_hasCenter || ccx != _centerCx || ccz != _centerCz)
                {
                    _centerCx = ccx;
                    _centerCz = ccz;
                    _hasCenter = true;
                    RecomputeQueue();
                    UnloadFar();
                }
            }

            for (int built = 0; built < chunksPerFrame && _buildQueue.Count > 0; built++)
            {
                int last = _buildQueue.Count - 1;
                (int cx, int cz) = _buildQueue[last];   // nearest sits at the end (pop = nearest first)
                _buildQueue.RemoveAt(last);
                EnsureChunk(cx, cz);
            }
        }

        private void OnDestroy()
        {
            foreach (GameObject chunk in _chunks.Values) DestroyChunk(chunk);
            _chunks.Clear();
            if (_baseMaterial != null) Destroy(_baseMaterial);
        }

        /// <summary>Rebuild the pending list: missing chunks in radius, nearest last so we pop them first.</summary>
        private void RecomputeQueue()
        {
            _buildQueue.Clear();
            int r = renderRadiusChunks;
            var wanted = new List<(int cx, int cz, int d)>();
            for (int dz = -r; dz <= r; dz++)
                for (int dx = -r; dx <= r; dx++)
                {
                    int cx = _centerCx + dx, cz = _centerCz + dz;
                    if (_chunks.ContainsKey(Key(cx, cz))) continue;
                    wanted.Add((cx, cz, dx * dx + dz * dz));
                }
            wanted.Sort((a, b) => b.d.CompareTo(a.d)); // farthest first => nearest ends up last
            foreach (var w in wanted) _buildQueue.Add((w.cx, w.cz));
        }

        private void UnloadFar()
        {
            int limit = renderRadiusChunks + keepPadding;
            var stale = new List<long>();
            foreach (KeyValuePair<long, GameObject> kv in _chunks)
            {
                Unpack(kv.Key, out int cx, out int cz);
                if (Mathf.Abs(cx - _centerCx) > limit || Mathf.Abs(cz - _centerCz) > limit)
                {
                    DestroyChunk(kv.Value);
                    stale.Add(kv.Key);
                }
            }
            foreach (long k in stale) _chunks.Remove(k);
        }

        private void EnsureChunk(int chunkX, int chunkZ)
        {
            long key = Key(chunkX, chunkZ);
            if (_chunks.ContainsKey(key)) return;
            _chunks[key] = BuildChunk(chunkX, chunkZ);
        }

        private GameObject BuildChunk(int chunkX, int chunkZ)
        {
            EnsureMaterial();
            int n = chunkSize;
            float ts = map.TileSize;
            float baseY = map.WaterSurfaceY - 2f; // shared floor the columns sit on (hidden below water)

            var go = new GameObject($"Chunk_{chunkX}_{chunkZ}");
            go.transform.SetParent(transform, false);
            go.transform.position = new Vector3(chunkX * n * ts, 0f, chunkZ * n * ts);

            // 5 quads per tile (top + 4 walls) = 20 verts, 30 indices.
            var verts = new List<Vector3>(n * n * 20);
            var norms = new List<Vector3>(n * n * 20);
            var uvs = new List<Vector2>(n * n * 20);
            var tris = new List<int>(n * n * 30);
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            for (int lz = 0; lz < n; lz++)
            {
                for (int lx = 0; lx < n; lx++)
                {
                    int tx = chunkX * n + lx, tz = chunkZ * n + lz;
                    WorldCell cell = map.CellAtTile(tx, tz);
                    BiomeData biome = map.BiomeAt(cell.BiomeIndex);
                    tex.SetPixel(lx, lz, biome != null ? biome.TileColor : Color.magenta);

                    var uv = new Vector2((lx + 0.5f) / n, (lz + 0.5f) / n); // point-sample this tile's pixel
                    float top = map.SurfaceY(tx, tz, biome != null && biome.IsWater);
                    float x0 = lx * ts, x1 = (lx + 1) * ts, z0 = lz * ts, z1 = (lz + 1) * ts;

                    // Top face (normal up)
                    AddQuad(verts, norms, uvs, tris, uv, Vector3.up,
                        new Vector3(x0, top, z0), new Vector3(x1, top, z0),
                        new Vector3(x1, top, z1), new Vector3(x0, top, z1));
                    // Four walls down to the shared base (outward normals so the sun shades them)
                    AddQuad(verts, norms, uvs, tris, uv, new Vector3(0, 0, -1),
                        new Vector3(x0, baseY, z0), new Vector3(x1, baseY, z0),
                        new Vector3(x1, top, z0), new Vector3(x0, top, z0));
                    AddQuad(verts, norms, uvs, tris, uv, new Vector3(0, 0, 1),
                        new Vector3(x1, baseY, z1), new Vector3(x0, baseY, z1),
                        new Vector3(x0, top, z1), new Vector3(x1, top, z1));
                    AddQuad(verts, norms, uvs, tris, uv, new Vector3(-1, 0, 0),
                        new Vector3(x0, baseY, z1), new Vector3(x0, baseY, z0),
                        new Vector3(x0, top, z0), new Vector3(x0, top, z1));
                    AddQuad(verts, norms, uvs, tris, uv, new Vector3(1, 0, 0),
                        new Vector3(x1, baseY, z0), new Vector3(x1, baseY, z1),
                        new Vector3(x1, top, z1), new Vector3(x1, top, z0));
                }
            }
            tex.Apply();

            var mesh = new Mesh();
            if (verts.Count > 65535) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.SetVertices(verts);
            mesh.SetNormals(norms);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();

            go.AddComponent<MeshFilter>().sharedMesh = mesh;

            var mat = new Material(_baseMaterial);
            mat.mainTexture = tex;                                    // _MainTex (Standard/Diffuse)
            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex); // URP/Lit
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.white);
            go.AddComponent<MeshRenderer>().sharedMaterial = mat;

            return go;
        }

        private void DestroyChunk(GameObject go)
        {
            if (go == null) return;
            var mf = go.GetComponent<MeshFilter>();
            if (mf != null && mf.sharedMesh != null) Destroy(mf.sharedMesh);
            var mr = go.GetComponent<MeshRenderer>();
            if (mr != null && mr.sharedMaterial != null)
            {
                if (mr.sharedMaterial.mainTexture != null) Destroy(mr.sharedMaterial.mainTexture);
                Destroy(mr.sharedMaterial);
            }
            Destroy(go);
        }

        private void EnsureMaterial()
        {
            if (_baseMaterial != null) return;
            Shader shader = Shader.Find("Universal Render Pipeline/Lit")
                            ?? Shader.Find("Standard")
                            ?? Shader.Find("Legacy Shaders/Diffuse")
                            ?? Shader.Find("Sprites/Default");
            _baseMaterial = new Material(shader);
            if (_baseMaterial.HasProperty("_Smoothness")) _baseMaterial.SetFloat("_Smoothness", 0f);
            if (_baseMaterial.HasProperty("_Glossiness")) _baseMaterial.SetFloat("_Glossiness", 0f);
        }

        /// <summary>Append one quad (two triangles) with a single shared normal and UV.</summary>
        private static void AddQuad(List<Vector3> verts, List<Vector3> norms, List<Vector2> uvs,
            List<int> tris, Vector2 uv, Vector3 normal, Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            int i = verts.Count;
            verts.Add(a); verts.Add(b); verts.Add(c); verts.Add(d);
            norms.Add(normal); norms.Add(normal); norms.Add(normal); norms.Add(normal);
            uvs.Add(uv); uvs.Add(uv); uvs.Add(uv); uvs.Add(uv);
            tris.Add(i); tris.Add(i + 2); tris.Add(i + 1);
            tris.Add(i); tris.Add(i + 3); tris.Add(i + 2);
        }

        // Pack/unpack a chunk coordinate into a dictionary key.
        private static long Key(int cx, int cz) => ((long)(uint)cx << 32) | (uint)cz;
        private static void Unpack(long key, out int cx, out int cz)
        {
            cx = (int)(key >> 32);
            cz = (int)(key & 0xFFFFFFFF);
        }

        private static int FloorDiv(int a, int b) => Mathf.FloorToInt(a / (float)b);
    }
}
