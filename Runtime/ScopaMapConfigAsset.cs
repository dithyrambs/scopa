using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Scopa {
    /// <summary> ScriptableObject to use for configuring how Scopa imports .MAPs, even for runtime imports too. </summary>
    [CreateAssetMenu(fileName = "New ScopaMapConfig", menuName = "Scopa/MAP Config Asset", order = 1)]
    public class ScopaMapConfigAsset : ScriptableObject {
        public ScopaMapConfig config = new ScopaMapConfig();
    }

    [System.Serializable]
    public class ScopaMapConfig {
        [Header("MESHES")]

        [Tooltip("(default: 0.03125, 1 m = 32 units) The global scaling factor for all brush geometry and entity origins.")]
        public float scalingFactor = 0.03125f;

        [Tooltip("(default: 4) vertex welding distance threshold in unscaled map units; set to 0 to disable vertex welding (not recommended). Pretty important for minimizing seams and cracks. Avoid building smaller than the welding threshold.")]
        public float weldingThreshold = 4f;

        [Tooltip("(default: true) Try to detect whether a face is completely covered by another face within the same entity, and discard it. It's not perfect; it can't detect if a face is covered by 2+ faces. But it helps. Note the extra calculations increase map import times.")]
        public bool removeHiddenFaces = true;

        [Tooltip("(default: true) Generate tangent data needed for normal mapping. If you're not using normal maps, disable for small memory savings.")]
        public bool addTangents = true;

        [Tooltip("(EDITOR-ONLY) (default: true) Generate lightmap UVs using Unity's built-in lightmap unwrapper.")]
        public bool addLightmapUV2 = true;

        [Tooltip("(EDITOR-ONLY) (default: Off) Use Unity's built-in mesh compressor. Reduces file size but may cause glitches and seams.")]
        public ModelImporterMeshCompression meshCompression = ModelImporterMeshCompression.Off;

        // TODO: merge brushes for each entity
        // TODO: remove unseen faces
        // TODO: vertex snapping

        [Tooltip("(default: sky, trigger, skip, hint, nodraw, null, clip, origin) When a face's texture name contains any word in this list, discard that face from the mesh. But this does not affect mesh colliders.")]
        public List<string> cullTextures = new List<string>() {"sky", "trigger", "skip", "hint", "nodraw", "null", "clip", "origin"};


        [Space(), Header("COLLIDERS")]

        [Tooltip("(default: Box and Convex) For each brush we add a collider. Axis-aligned boxy brushes use Box Colliders, anything else gets a convex Mesh Collider. You can also force just one type, or use a big complex expensive concave Mesh Collider.")]
        public ColliderImportMode colliderMode = ColliderImportMode.BoxAndConvex;

        [Tooltip("(default: illusionary) If an entity's classname contains a word in this list, do not generate a collider for it and disable Navigation Static for it.")]
        public List<string> nonsolidEntities = new List<string>() {"illusionary"};

        [Tooltip("(default: trigger, water) If an entity's classname contains a word in this list, mark that collider as a non-solid trigger and disable Navigation Static for it.")]
        public List<string> triggerEntities = new List<string>() {"trigger", "water"};


        [Space(), Header("TEXTURES & MATERIALS")]

        [Tooltip("(EDITOR-ONLY) (default: true) try to automatically match each texture name to a similarly named Material already in the project")]
        public bool findMaterials = true;

        [Tooltip("(default: 1.0) map-wide scaling factor for all texture faces; < 1.0 enlarges textures, > 1.0 shrinks textures")]
        public float globalTexelScale = 1.0f;

        [Tooltip("(default: 128) To calculate texture coordinates, we need to know the texture image size; but if we can't find a matching texture, use this default size")]
        public int defaultTexSize = 128;

        [Tooltip("(optional) when we can't find a matching Material name, then use this default Material instead")]
        public Material defaultMaterial;

        [Tooltip("(optional) manually set a specific Material for each texture name")]
        public MaterialOverride[] materialOverrides;


        [Space(), Header("GAMEOBJECTS & ENTITIES")]

        [Tooltip("(default: func_group, func_detail) If an entity classname contains any word in this list, then merge its brushes (mesh and collider) into worldspawn and discard entity data. WARNING: most per-entity mesh and collider configs will be overriden by worldspawn; only the discarded entity's solidity will be respected.")]
        public List<string> mergeToWorld = new List<string>() {"func_group", "func_detail"};

        [Tooltip("(default: worldspawn, func_wall) If an entity classname contains any word in this list AND it doesn't have prefab overrides (see Entity Overrides), then set its mesh objects to be static -- batching, lightmapping, navigation, reflection, everything. However, non-solid and trigger entities will NOT be navigation static.")]
        public List<string> staticEntities = new List<string>() {"worldspawn", "func_wall"};

        [Tooltip("(default: Default) Set ALL objects to use this layer. For example, maybe you have a 'World' layer. To set per-entity layers, see prefab slots below / Entity Overrides.")]
        [Layer] public int layer = 0;

        [Tooltip("(default: true) if enabled, automatically add ScopaEntity component to all game objects (if not already present in the entityPrefab)... disable this if you don't want to use the built-in ScopaEntity at all, and override it with your own")]
        public bool addScopaEntityComponent = true;

        [Tooltip("(optional) Prefab template to use for the root of EVERY entity including worldspawn. Ignores the config-wide static / layer settings above.")]
        public GameObject entityPrefab;
        
        [Tooltip("(optional) Prefab template to use for each mesh + material in each entity. meshFilter.sharedMesh and meshRenderer.sharedMaterial will be overridden. Useful for setting layers, renderer settings, etc. Ignores the global static / layer settings above.")]
        public GameObject meshPrefab;

        [Tooltip("(optional) Override the prefabs used for each entity type. For example, a door might need its own special prefab. Order matters, we use the first override that matches. Ignores the global static / layer settings above.")]
        public EntityOverride[] entityOverrides;

        [Tooltip("(optional) If there isn't an entity override defined above, then the next place we look for entity prefabs is in this FGD asset.")]
        public ScopaFgdConfigAsset fgdAsset;

        /// <summary> note: textureName must already be ToLowerInvariant() </summary>
        public bool IsTextureNameCulled(string textureName) {
            if ( string.IsNullOrWhiteSpace(textureName) )
                return true;

            var search = textureName;
            for(int i=0; i<cullTextures.Count; i++) {
                if ( search.Contains(cullTextures[i]) ) {
                    return true;
                }
            }
            return false;
        }

        /// <summary> note: entityClassname must already be ToLowerInvariant() </summary>
        public bool IsEntityMergeToWorld(string entityClassname) {
            var search = entityClassname;
            for(int i=0; i<mergeToWorld.Count; i++) {
                if ( search.Contains(mergeToWorld[i]) ) {
                    return true;
                }
            }
            return false;
        }

        /// <summary> note: entityClassname must already be ToLowerInvariant() </summary>
        public bool IsEntityStatic(string entityClassname) {
            var search = entityClassname;
            for(int i=0; i<staticEntities.Count; i++) {
                if ( search.Contains(staticEntities[i]) ) {
                    return true;
                }
            }
            return false;
        }

        /// <summary> note: entityClassname must already be ToLowerInvariant() </summary>
        public bool IsEntityNonsolid(string entityClassname) {
            var search = entityClassname;
            for(int i=0; i<nonsolidEntities.Count; i++) {
                if ( search.Contains(nonsolidEntities[i]) ) {
                    return true;
                }
            }
            return false;
        }

        /// <summary> note: entityClassname must already be ToLowerInvariant() </summary>
        public bool IsEntityTrigger(string entityClassname) {
            var search = entityClassname;
            for(int i=0; i<triggerEntities.Count; i++) {
                if ( search.Contains(triggerEntities[i]) ) {
                    return true;
                }
            }
            return false;
        }

        /// <summary> note: textureName must already be ToLowerInvariant() </summary>
        public MaterialOverride GetMaterialOverrideFor(string textureName) {
            if ( materialOverrides == null || materialOverrides.Length == 0) {
                return null;
            }

            var search = materialOverrides.Where( ov => textureName.Contains(ov.textureName.ToLowerInvariant()) ).FirstOrDefault();
            return search;
        }

        /// <summary> note: entityClassname must already be ToLowerInvariant() </summary>
        public GameObject GetEntityPrefabFor(string entityClassname) {
            // special early out for default case
            if ( fgdAsset == null && (entityOverrides == null || entityOverrides.Length == 0) ) {
                return entityPrefab;
            }

            // try looking in the MAP config
            var search = entityOverrides.Where( cfg => entityClassname.Contains(cfg.entityClassName.ToLowerInvariant()) ).FirstOrDefault();
            if ( search != null && search.entityPrefab != null) {
                return search.entityPrefab;
            }

            // try looking in the FGD config
            if ( fgdAsset != null ) {
                var fgdSearch = fgdAsset.config.GetEntityPrefabFor(entityClassname);
                if ( fgdSearch != null) {
                    // Debug.Log("found FGD prefab for " + entityClassname);
                    return fgdSearch;
                }
            }

            return entityPrefab;
        }

        /// <summary> note: entityClassname must already be ToLowerInvariant() </summary>
        public GameObject GetMeshPrefabFor(string entityClassname) {
            if ( entityOverrides == null || entityOverrides.Length == 0) {
                return meshPrefab;
            }

            var search = entityOverrides.Where( cfg => entityClassname.Contains(cfg.entityClassName.ToLowerInvariant()) ).FirstOrDefault();
            if ( search != null && search.meshPrefab != null) {
                return search.meshPrefab;
            }
            return meshPrefab;
        }


        [System.Serializable]
        public class EntityOverride {
            [Tooltip("for example: func_detail, func_wall, light, etc... worldspawn is for world brushes. Partial matches count, e.g. 'func' will match all func_ entities.")]
            public string entityClassName;

            [Tooltip("the prefabs to use, just for this entity type")]
            public GameObject entityPrefab, meshPrefab;
        }

        [System.Serializable]
        public class MaterialOverride {
            [Tooltip("If a face has a texture name that matches this override, then use this Material no matter what. Partial matches count, e.g. an override for 'stone' will match all faces with texture names that contain the word 'stone'")]
            public string textureName;
            public Material material;

            [Tooltip("(optional) if defined, all faces textured with this override will be hotspot UV'd -- unwrapped to fit the nearest rectangle area ('hotspot')")]
            public ScopaMaterialConfig hotspotAtlas;

            public MaterialOverride(string texName, Material mat) {
                this.textureName = texName;
                this.material = mat;
            }
        }

        public enum ColliderImportMode {
            None,
            BoxColliderOnly,
            ConvexMeshColliderOnly,
            BoxAndConvex,
            MergeAllToOneConcaveMeshCollider
        }

        public enum ModelImporterMeshCompression
        {
            Off = 0,
            Low = 1,
            Medium = 2,
            High = 3
        }

        public ScopaMapConfig ShallowCopy() {
            return (ScopaMapConfig) this.MemberwiseClone();
        }
    }

    
}

