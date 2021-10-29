using LitJson;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class TerrainLoadEditor : Editor
{
    public static string terrainBlockName;
    private static float size;
    private static float terrainPosx;
    private static float terrainPosy;
    private static float terrainPosz;
    private static float treeDistance;
    private static float treeBillboardDistance;
    private static float treeCrossFadeLength;
    private static int treeMaximumFullLODCount;
    private static float detailObjectDistance;
    private static float detailObjectDensity;
    private static float heightmapPixelError;
    private static int heightmapMaximumLOD;
    private static float basemapDistance;
    private static int lightmapIndex;
    private static bool castShadows;
    private static string shaderName;

    private static List<LightmapTexturePath> lightmapTextures = new List<LightmapTexturePath>();
    private static Dictionary<string, DyncRenderInfo> lightMapDats = new Dictionary<string, DyncRenderInfo>();

    [MenuItem("Terrain/Load")]
    private static void LoadTerrainBlocks()
    {
        string terrainRoot = "terrain_root";
        GameObject terrainRootGo = GameObject.Find(terrainRoot);
        if (terrainRootGo == null)
        {
            terrainRootGo = AttachGameobject(terrainRoot);
        }

        string readPath = TerrainConst.savePath + ".json";
        if (!File.Exists(readPath))
        {
            Debug.LogError("no find file, path: " + readPath);
            return;
        }

        LoadTerrainLightMapData();
        SetLightingSettings();

        bool complete = LoadTerrainData();
        if (!complete)
        {
            return;
        }

        int terrainBlockSize = TerrainConst.TERRAIN_BLOCK_SLICE;
        for (int x = 0; x < terrainBlockSize; ++x)
        {
            for (int y = 0; y < terrainBlockSize; ++y)
            {
                string terrainName = string.Format("{0}_{1}_{2}", terrainBlockName, x, y);
                string terrainDataPath = TerrainConst.savePath + "/" + terrainName + ".asset";

                GameObject go = new GameObject(terrainName);
                go.transform.SetParent(terrainRootGo.transform);
                go.transform.localPosition = new Vector3(x * size, 0, y * size);
                Terrain terrain = go.AddComponent<Terrain>();
                TerrainData terrainData = AssetDatabase.LoadAssetAtPath<TerrainData>(terrainDataPath);
                terrain.terrainData = terrainData;

                var collider = go.AddComponent<TerrainCollider>();
                collider.terrainData = terrain.terrainData;

                terrain.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
                terrain.allowAutoConnect = true;
                terrain.treeDistance = treeDistance;
                terrain.treeBillboardDistance = treeBillboardDistance;
                terrain.treeCrossFadeLength = treeCrossFadeLength;
                terrain.treeMaximumFullLODCount = treeMaximumFullLODCount;
                terrain.detailObjectDistance = detailObjectDistance;
                terrain.detailObjectDensity = detailObjectDensity;
                terrain.heightmapPixelError = heightmapPixelError;
                terrain.heightmapMaximumLOD = heightmapMaximumLOD;
                terrain.basemapDistance = basemapDistance;
                terrain.castShadows = castShadows;
                terrain.materialTemplate = new Material(Shader.Find(shaderName));
                terrain.gameObject.isStatic = true;
                terrain.bakeLightProbesForTrees = true;
                terrain.deringLightProbesForTrees = true;
                DyncRenderInfo info;
                if (lightMapDats.TryGetValue(terrainName, out info))
                {
                    terrain.lightmapIndex = info.lightIndex;
                    terrain.lightmapScaleOffset = info.lightOffsetScale;
                }
                else
                {
                    terrain.lightmapIndex = lightmapIndex;
                }
            }
        }
    }

    private static void SetLightingSettings()
    {
        if (lightmapTextures.Count == 0)
        {
            return;
        }

        LightmapData[] datas = new LightmapData[lightmapTextures.Count];
        for (int i = 0; i < lightmapTextures.Count; i++)
        {
            LightmapData data = new LightmapData();
            data.lightmapColor = AssetDatabase.LoadAssetAtPath<Texture2D>(lightmapTextures[i].lightmapColorPath);
            data.lightmapDir = AssetDatabase.LoadAssetAtPath<Texture2D>(lightmapTextures[i].lightmapDirPath);
            datas[i] = data;
        }

        LightmapSettings.lightmaps = datas;
    }

    private static bool LoadTerrainData()
    {
        string readPath = TerrainConst.savePath + ".json";
        if (!File.Exists(readPath))
        {
            Debug.LogError("no find file, path: " + readPath);
            return false;
        }

        try
        {
            string terrainInfo = File.ReadAllText(readPath);
            JsonData data = JsonMapper.ToObject(terrainInfo);
            JsonData terrainData = data["TerrainInfo"];
            terrainBlockName = (string)terrainData["name"];

            size = float.Parse(terrainData["size"].ToString());
            terrainPosx = float.Parse(terrainData["terrainPosx"].ToString());
            terrainPosy = float.Parse(terrainData["terrainPosy"].ToString());
            terrainPosz = float.Parse(terrainData["terrainPosz"].ToString());
            treeDistance = float.Parse(terrainData["treeDistance"].ToString());
            treeBillboardDistance = float.Parse(terrainData["treeBillboardDistance"].ToString());
            treeCrossFadeLength = float.Parse(terrainData["treeCrossFadeLength"].ToString());
            treeMaximumFullLODCount = (int)terrainData["treeMaximumFullLODCount"];
            detailObjectDistance = float.Parse(terrainData["detailObjectDistance"].ToString());
            detailObjectDensity = float.Parse(terrainData["detailObjectDensity"].ToString());
            heightmapPixelError = float.Parse(terrainData["heightmapPixelError"].ToString());
            heightmapMaximumLOD = (int)terrainData["heightmapMaximumLOD"];
            basemapDistance = float.Parse(terrainData["basemapDistance"].ToString());
            lightmapIndex = (int)terrainData["lightmapIndex"];
            castShadows = (bool)terrainData["castShadows"];
            shaderName = (string)terrainData["shaderName"];

            return true;
        }
        catch (Exception e)
        {
            Debug.LogError(e.Message);
            return false;
        }
    }

    private static bool LoadTerrainLightMapData()
    {
        string readPath = TerrainConst.lightMapPath;
        if (!File.Exists(readPath))
        {
            Debug.LogError("no find file, path: " + readPath);
            return false;
        }

        try
        {
            string terrainLightMapInfo = File.ReadAllText(readPath);
            lightMapDats = new Dictionary<string, DyncRenderInfo>();
            lightmapTextures = new List<LightmapTexturePath>();
            JsonData data = JsonMapper.ToObject(terrainLightMapInfo);
            JsonData lightMapData = data["LightMapInfo"];
            JsonData lightmapTextureData = data["LightMapTextureInfo"];

            for (int i = 0; i < lightMapData.Count; i++)
            {
                DyncRenderInfo info = new DyncRenderInfo();
                info.lightIndex = (int)lightMapData[i]["LightMapIndex"];
                float LightMapUVx = float.Parse(lightMapData[i]["LightMapUVx"].ToString());
                float LightMapUVy = float.Parse(lightMapData[i]["LightMapUVy"].ToString());
                float LightMapOffsetx = float.Parse(lightMapData[i]["LightMapOffsetx"].ToString());
                float LightMapOffsety = float.Parse(lightMapData[i]["LightMapOffsety"].ToString());
                info.lightOffsetScale = new Vector4(LightMapUVx, LightMapUVy, LightMapOffsetx, LightMapOffsety);

                lightMapDats[(string)lightMapData[i]["TerrainBlockNanme"]] = info;
            }

            for (int i = 0; i < lightmapTextureData.Count; i++)
            {
                LightmapTexturePath lightmapTexturePath = new LightmapTexturePath();
                lightmapTexturePath.lightmapColorPath = lightmapTextureData[i]["lightmapColorPath"].ToString();
                lightmapTexturePath.lightmapDirPath = lightmapTextureData[i]["lightmapDirPath"].ToString();
                lightmapTextures.Add(lightmapTexturePath);
            }

            return true;
        }
        catch(Exception e)
        {
            Debug.LogError(e.Message);
            return false;
        }
    }

    private static GameObject AttachGameobject(string name, GameObject parent = null)
    {
        GameObject newGo = new GameObject(name);
        if (parent != null)
        {
            newGo.transform.SetParent(parent.transform);
        }

        newGo.transform.localPosition = Vector3.zero;
        newGo.transform.localScale = Vector3.one;
        newGo.transform.localRotation = Quaternion.identity;
        return newGo;
    }
}