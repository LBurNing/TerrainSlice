using UnityEngine;
public struct LightmapTexturePath
{
    public string lightmapColorPath;
    public string lightmapDirPath;
}

public struct DyncRenderInfo
{
    public int lightIndex;
    public Vector4 lightOffsetScale;
    public int hash;
    public Vector3 pos;
}

public static class TerrainConst
{
    /// <summary>
    /// 切割成8*8
    /// </summary>
    public static int TERRAIN_BLOCK_SLICE = 8;

    public static string savePath = "Assets/Resources/TerrainData/TestTerrain";
    public static string lightMapPath = "Assets/LightMapInfo.json";
}


