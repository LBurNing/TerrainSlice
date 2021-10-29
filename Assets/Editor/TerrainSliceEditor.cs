using System.IO;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using LitJson;

public class TerrainSliceEditor : Editor
{
    //开始分割地形
    [MenuItem("Terrain/Slicing")]
    private static void Slicing()
    {
        GameObject selectGo = Selection.activeGameObject;
        if (selectGo == null)
        {
            Debug.LogError("选中地形, 在进行此操作.");
            return;
        }

        Terrain terrain = selectGo.GetComponent<Terrain>();
        if (terrain == null)
        {
            Debug.LogError("选中的不是地形.");
            return;
        }

        string savepath = TerrainConst.savePath;
        if (Directory.Exists(savepath))
        {
            Directory.Delete(savepath, true);
        }
        Directory.CreateDirectory(savepath);

        TerrainData terrainData = terrain.terrainData;
        Vector3 oldSize = terrainData.size;
        Vector3 oldPos = terrain.transform.position;
        oldPos = new Vector3(oldPos.x - oldSize.x, oldPos.y, oldPos.z);
        int terrainBlockSize = TerrainConst.TERRAIN_BLOCK_SLICE;

        //得到新地图分辨率
        int newAlphamapResolution = terrainData.alphamapResolution / terrainBlockSize;
        TerrainLayer[] terrainLayers = terrainData.terrainLayers;

        var detailProtos = terrainData.detailPrototypes;
        var treeProtos = terrainData.treePrototypes;
        var treeInst = terrainData.treeInstances;
        var grassStrength = terrainData.wavingGrassStrength;
        var grassAmount = terrainData.wavingGrassAmount;
        var grassSpeed = terrainData.wavingGrassSpeed;
        var grassTint = terrainData.wavingGrassTint;

        
        int newDetailResolution = terrainData.detailResolution / terrainBlockSize;
        int resolutionPerPatch = 8;

        //设置高度
        int xBase = terrainData.heightmapResolution / terrainBlockSize;
        int yBase = terrainData.heightmapResolution / terrainBlockSize;

        TerrainData[] data = new TerrainData[terrainBlockSize * terrainBlockSize];
        Dictionary<int, List<TreeInstance>> map = new Dictionary<int, List<TreeInstance>>();

        int arrayPos = 0;

        try
        {
            //循环宽和长,生成小块地形
            for (int x = 0; x < terrainBlockSize; ++x)
            {
                for (int y = 0; y < terrainBlockSize; ++y)
                {
                    //创建资源
                    TerrainData newData = new TerrainData();
                    map[arrayPos] = new List<TreeInstance>();
                    data[arrayPos++] = newData;

                    string terrainName = string.Format("{0}_{1}_{2}.asset", terrain.name, x, y);
                    AssetDatabase.CreateAsset(newData, savepath + "/" + terrainName);

                    EditorUtility.DisplayProgressBar("正在分割地形", terrainName, (float)(x * terrainBlockSize + y) / (float)(terrainBlockSize * terrainBlockSize));

                    //设置分辨率参数
                    //高度地图的分辨率只能是2的N次幂加1,所以SLICING_SIZE必须为2的N次幂
                    newData.heightmapResolution = (terrainData.heightmapResolution - 1) / terrainBlockSize;
                    newData.alphamapResolution = terrainData.alphamapResolution / terrainBlockSize;
                    newData.baseMapResolution = terrainData.baseMapResolution / terrainBlockSize;

                    //设置大小
                    newData.size = new Vector3(oldSize.x / terrainBlockSize, oldSize.y, oldSize.z / terrainBlockSize);

                    //设置地形块原型
                    TerrainLayer[] newTerrainLayers = new TerrainLayer[terrainLayers.Length];
                    for (int i = 0; i < newTerrainLayers.Length; ++i)
                    {
                        newTerrainLayers[i] = new TerrainLayer();
                        string terrainLayerName = string.Format("{0}_{1}_{2}.terrainlayer", terrainLayers[i].name, x, y);
                        AssetDatabase.CreateAsset(newTerrainLayers[i], savepath + "/" + terrainLayerName);
                        newTerrainLayers[i].diffuseTexture = terrainLayers[i].diffuseTexture;

                        float newOffsetX = (newData.size.x * x) % terrainLayers[i].tileSize.x;
                        float newOffsetY = (newData.size.z * y) % terrainLayers[i].tileSize.y;

                        //设置UV偏移
                        float offsetX = newOffsetX + terrainLayers[i].tileOffset.x;
                        float offsetY = newOffsetY + terrainLayers[i].tileOffset.y;
                        newTerrainLayers[i].tileOffset = new Vector2(offsetX, offsetY);
                    }
                    newData.terrainLayers = newTerrainLayers;

                    //设置混合贴图
                    float[,,] alphamap = new float[newAlphamapResolution, newAlphamapResolution, newTerrainLayers.Length];
                    //前两个参数是读取的xy偏移量，后两个参数读取地图区域的宽度
                    alphamap = terrainData.GetAlphamaps(x * newData.alphamapWidth, y * newData.alphamapHeight, newData.alphamapWidth, newData.alphamapHeight);
                    newData.SetAlphamaps(0, 0, alphamap);

                    //获取高度样本
                    float[,] height = terrainData.GetHeights(xBase * x, yBase * y, xBase + 1, yBase + 1);
                    newData.SetHeights(0, 0, height);

                    newData.SetDetailResolution(newDetailResolution, resolutionPerPatch);

                    //获取索引多对应的小方块上对应草的数量
                    int[] layers = terrainData.GetSupportedLayers(x * newData.detailWidth - 1, y * newData.detailHeight - 1, newData.detailWidth, newData.detailHeight);
                    int layerLength = layers.Length;

                    //设置地形块原型（草之类的）
                    DetailPrototype[] tempDetailProtos = new DetailPrototype[layerLength];
                    for (int i = 0; i < layerLength; i++)
                    {
                        tempDetailProtos[i] = detailProtos[layers[i]];
                    }
                    newData.detailPrototypes = tempDetailProtos;

                    //设置地形块所用到的详细layer
                    for (int i = 0; i < layerLength; i++)
                    {
                        int[,] detailLayer = terrainData.GetDetailLayer(x * newData.detailWidth, y * newData.detailHeight, newData.detailWidth, newData.detailHeight, layers[i]);
                        newData.SetDetailLayer(0, 0, i, detailLayer);
                    }

                    newData.wavingGrassStrength = grassStrength;
                    newData.wavingGrassAmount = grassAmount;
                    newData.wavingGrassSpeed = grassSpeed;
                    newData.wavingGrassTint = grassTint;
                    newData.treePrototypes = treeProtos;
                }
            }

            int newWidth = (int)oldSize.x / terrainBlockSize;
            int newLength = (int)oldSize.z / terrainBlockSize;

            for (int i = 0; i < treeInst.Length; i++)
            {
                Vector3 origPos = Vector3.Scale(new Vector3(oldSize.x, 1, oldSize.z), treeInst[i].position);
                int column = Mathf.FloorToInt(origPos.x / newWidth);
                int row = Mathf.FloorToInt(origPos.z / newLength);
                Vector3 tempVect = new Vector3((origPos.x - newWidth * column) / newWidth, origPos.y, (origPos.z - newLength * row) / newWidth);

                TreeInstance tempTree = new TreeInstance();
                tempTree.position = tempVect;
                tempTree.widthScale = treeInst[i].widthScale;
                tempTree.heightScale = treeInst[i].heightScale;
                tempTree.color = treeInst[i].color;
                tempTree.rotation = treeInst[i].rotation;
                tempTree.lightmapColor = treeInst[i].lightmapColor;

                int index = (column * terrainBlockSize) + row;
                tempTree.prototypeIndex = 0;
                map[index].Add(tempTree);
            }

            for (int i = 0; i < terrainBlockSize * terrainBlockSize; i++)
            {
                data[i].treeInstances = map[i].ToArray();
                data[i].RefreshPrototypes();
            }

            WriteTerrainInfo(terrain, savepath + ".json");
        }
        catch (System.Exception e)
        {
            Debug.LogError(e.Message);
            Debug.LogError(e.StackTrace);
        }
        finally
        {
            EditorUtility.ClearProgressBar();
            AssetDatabase.Refresh();
        }
    }

    private static int PowerUp(int size)
    {
        while (!Mathf.IsPowerOfTwo(size))
        {
            size -= 1;
        }

        return size;
    }

    private static void WriteTerrainInfo(Terrain terrain, string path)
    {
        JsonData jsonData = new JsonData();
        JsonData terrainInfo = new JsonData();

        Vector3 terrainPos = terrain.transform.position;
        float size = terrain.terrainData.size.x / TerrainConst.TERRAIN_BLOCK_SLICE;
        terrainInfo["size"] = size;
        terrainInfo["name"] = terrain.name;
        terrainInfo["terrainPosx"] = terrainPos.x;
        terrainInfo["terrainPosy"] = terrainPos.y;
        terrainInfo["terrainPosz"] = terrainPos.z;
        terrainInfo["treeDistance"] = terrain.treeDistance;
        terrainInfo["treeBillboardDistance"] = terrain.treeBillboardDistance;
        terrainInfo["treeCrossFadeLength"] = terrain.treeCrossFadeLength;
        terrainInfo["treeMaximumFullLODCount"] = terrain.treeMaximumFullLODCount;
        terrainInfo["detailObjectDistance"] = terrain.detailObjectDistance;
        terrainInfo["detailObjectDensity"] = terrain.detailObjectDensity;
        terrainInfo["heightmapPixelError"] = terrain.heightmapPixelError;
        terrainInfo["heightmapMaximumLOD"] = terrain.heightmapMaximumLOD;
        terrainInfo["basemapDistance"] = terrain.basemapDistance;
        terrainInfo["lightmapIndex"] = terrain.lightmapIndex;
        terrainInfo["castShadows"] = terrain.castShadows;
        terrainInfo["shaderName"] = terrain.materialTemplate.shader.name;

        jsonData["Desc"] = "terrain info";
        jsonData["TerrainInfo"] = terrainInfo;

        string info = jsonData.ToJson();
        File.WriteAllText(path, info);
    }
}