using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    public enum DrawMode { NoiseMap, ColorMap }
    public enum TextureTypes { Add, Remove }
    public DrawMode drawMode;

    //public int width;
    //public int height;
    //public float noiseScale;

    //public int octaves;
    //[Range(0,1)]
    //public float persistance;
    //public float lacunarity;

    //public int seed;
    //public Vector2 offset;

    public bool autoUpdate;



    public TerrainType[] regions;
    public MapSettings[] mapSettings;



    public float[,] GenerateMap()
    {
        float[,] noiseMap = new float[mapSettings[0].width, mapSettings[0].height];
        for (int i = 0; i < mapSettings.Length; i++)
        {
            CreateTexture(ref noiseMap, ref mapSettings[i]);

        }





        //MapDisplay display = FindObjectOfType<MapDisplay>();
        //if (drawMode == DrawMode.NoiseMap)
        //{
        //    display.DrawTetxure(TextureGenerator.TextureFromHeightMap(noiseMap));
        //}
        //else if (drawMode == DrawMode.ColorMap)
        //{
        //    Color[] colorMap = new Color[mapSettings[0].width * mapSettings[0].height];


        //    for (int y = 0; y < mapSettings[0].height; y++)
        //    {
        //        for (int x = 0; x < mapSettings[0].width; x++)
        //        {
        //            float currentHeight = noiseMap[x, y];

        //            for (int i = 0; i < regions.Length; i++)
        //            {
        //                if (currentHeight <= regions[i].height)
        //                {
        //                    colorMap[y * mapSettings[0].width + x] = regions[i].color;
        //                    break;
        //                }
        //            }
        //        }
        //    }
        //    display.DrawTetxure(TextureGenerator.TextureFromColorMap(colorMap, mapSettings[0].width, mapSettings[0].height));


        //}

        return noiseMap;
    }

    private void CreateTexture(ref float[,] noiseMap, ref MapSettings settings)
    {

        //float[,] temp = Noise.GeneratNoiseMap(
        //     settings.width,
        //     settings.height,
        //     settings.noiseScale,
        //     settings.octaves,
        //     settings.persistance,
        //     settings.lacunarity,
        //     settings.seed,
        //     settings.offset);


        //for (int x = 0; x < settings.width; x++)
        //{
        //    for (int y = 0; y < settings.height; y++)
        //    {
        //        if (settings.textureType == TextureTypes.Add)
        //        {
        //            float scale = (1.0f / mapSettings.Length);
        //            noiseMap[x, y] += (temp[x, y] * scale);





        //            if (noiseMap[x, y] > 1)
        //            {

        //                noiseMap[x, y] = 1;
        //            }
        //        }
        //        else if (settings.textureType == TextureTypes.Remove)
        //        {
        //            //noiseMap[x, y] -= (temp[x, y] * 0.5f);

        //            //if (noiseMap[x, y] < 0)
        //            //{
        //            //    noiseMap[x, y] = 0;
        //            //}
        //        }
        //    }
        //}
        // return noiseMap;
    }



    private void OnValidate()
    {
        //if(width < 1)
        //{
        //    width = 1;
        //}

        //if (height < 1)
        //{
        //    height = 1;
        //}

        //if (lacunarity < 1)
        //{
        //    lacunarity = 1;
        //}

        //if (octaves < 0)
        //{
        //    octaves = 0;
        //}


    }

    [System.Serializable]
    public struct MapSettings
    {
        public TextureTypes textureType;

        public string name;
        public int width;
        public int height;
        public float noiseScale;

        public int octaves;
        [Range(0, 1)]
        public float persistance;
        public float lacunarity;

        public int seed;
        public Vector2 offset;
    }


    [System.Serializable]
    public struct TerrainType
    {

        public string name;
        public float height;
        public Color color;
    }




}
