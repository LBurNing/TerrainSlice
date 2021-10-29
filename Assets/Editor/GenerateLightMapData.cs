using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using LitJson;
using System;

public class GenerateLightMapData
{
    private static string lightMapPath = TerrainConst.lightMapPath;

    [MenuItem("Terrain/生成lightmapping信息")]
    public static void GenerateLightMapInfo()
    {
        Terrain[] terrainBlocks = GameObject.FindObjectsOfType<Terrain>();
        if (terrainBlocks.Length == 0)
        {
            Debug.LogError("no find terrain");
            return;
        }

        int lightMapLength = LightmapSettings.lightmaps.Length;
        Texture2D[] lightMapColors = new Texture2D[lightMapLength];
        Texture2D[] lightMapDirs = new Texture2D[lightMapLength];

        //存储光照贴图的路径
        JsonData LightMapTextureInfo = new JsonData();
        for (int i = 0; i < lightMapLength; i++)
        {
            JsonData JD = new JsonData();
            string lightmapColorPath = AssetDatabase.GetAssetPath(LightmapSettings.lightmaps[i].lightmapColor);
            string lightmapDirPath = AssetDatabase.GetAssetPath(LightmapSettings.lightmaps[i].lightmapDir);
            JD["lightmapColorPath"] = lightmapColorPath;
            JD["lightmapDirPath"] = lightmapDirPath;
            LightMapTextureInfo.Add(JD);
        }

        JsonData jsonData = new JsonData();
        JsonData terrainData = new JsonData();
        Terrain terrain = null;

        //存储光照贴图信息
        for (int i = 0; i < terrainBlocks.Length; i++)
        {
            terrain = terrainBlocks[i];
            JsonData JD = new JsonData();
            JD["TerrainBlockNanme"] = terrain.name;
            JD["LightMapIndex"] = terrain.lightmapIndex;
            JD["LightMapUVx"] = terrain.lightmapScaleOffset.x;
            JD["LightMapUVy"] = terrain.lightmapScaleOffset.y;
            JD["LightMapOffsetx"] = terrain.lightmapScaleOffset.z;
            JD["LightMapOffsety"] = terrain.lightmapScaleOffset.w;
            terrainData.Add(JD);
            EditorUtility.DisplayProgressBar("正在生成LightMap数据", terrain.name, (float)(i) / (float)(terrainBlocks.Length));
        }
        EditorUtility.ClearProgressBar();

        jsonData["Desc"] = "terrain blocks lightmap info";
        jsonData["LightMapLength"] = lightMapLength;
        jsonData["LightMapInfo"] = terrainData;
        jsonData["LightMapTextureInfo"] = LightMapTextureInfo;

        string terrainInfo = jsonData.ToJson();
        File.WriteAllText(lightMapPath, terrainInfo);
        EditorUtility.DisplayDialog("LightMap.json生成成功!", "成功", "OK");
    }
}
