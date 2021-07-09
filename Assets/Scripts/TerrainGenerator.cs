using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

public struct ChunkNeighborCheckResult
{
    public bool hasNeighborChunk;
    public bool hasNeighborChunkCube;
}

public class TerrainGenerator : MonoBehaviour
{
   


    public static TerrainGenerator Instance { get; private set; }


    public int width;
    public int height;
    public int lenght;

    public GameObject ChunkPrefab;
    public int chunkWidth;
    public int chunkHeight;

    public int nrOfCubes;
    public int nrOfFaces;
    public long createTime = 0;

    // public MapGenerator mapGenerator;

    public GameObject[,] terrainBlocks;
    private GameObject block;

    public float[,] noiseMap;

    private GameObject terrainObject;



    private GameObject[,,] chunks;
    private int nrOfCreatedChunks;

    private bool isCreated = false;

    Stopwatch st = new Stopwatch();


    void Awake()
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }


    public void Start()
    {
       
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return) && isCreated == false)
        {
            CreateTerrain();
            isCreated = true;
        }
    }

    void CreateTerrain()
    {


        chunks = new GameObject[width, height, lenght];

        Vector3 position = new Vector3(0, 0, 0);

        int i = 0;
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                for (int z = 0; z < lenght; z++)
                {
                    position.x = x;
                    position.y = y;
                    position.z = z;

                    chunks[x, y, z] = Instantiate(ChunkPrefab, position, Quaternion.identity);
                    chunks[x, y, z].GetComponent<Chunk>().InitChunk(position, chunkWidth, chunkHeight);

                    nrOfCreatedChunks++;
                }
            }
        }
        st.Start();
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                for (int z = 0; z < lenght; z++)
                {
                    chunks[x, y, z].GetComponent<Chunk>().CreateChunk();
                }
            }
        }

        st.Stop();
        createTime = st.ElapsedMilliseconds;
        Debug.Log(string.Format("Creating world took {0} ms to complete", createTime));
    }
    


    public ChunkNeighborCheckResult BlockHasChunkNeighbor(Vector3 chunkPos, Vector3 cubePosLocal, Vector3 faceNormal)
    {
        ChunkNeighborCheckResult result = new ChunkNeighborCheckResult();

        result.hasNeighborChunk = false; 
        result.hasNeighborChunkCube = false;

        Vector3Int chunkIndex = Vector3Int.CeilToInt(chunkPos+ faceNormal);

        if (chunkIndex.x >= 0 && chunkIndex.x < width &&
            chunkIndex.y >= 0 && chunkIndex.y < height &&
            chunkIndex.z >= 0 && chunkIndex.z < lenght)
        {
            //Only change the vale of the normal axis, change it to the oposit pos 

            int posX = faceNormal.x == 0 ? (int)cubePosLocal.x : (int)cubePosLocal.x - (chunkWidth-1) * (int)faceNormal.x;
            int posY = faceNormal.y == 0 ? (int)cubePosLocal.y : (int)cubePosLocal.y - (chunkHeight - 1) * (int)faceNormal.y;
            int posZ = faceNormal.z == 0 ? (int)cubePosLocal.z : (int)cubePosLocal.z - (chunkWidth - 1) * (int)faceNormal.z;

            Vector3Int blockIndex = new Vector3Int(posX, posY, posZ);
            result.hasNeighborChunkCube = chunks[chunkIndex.x, chunkIndex.y, chunkIndex.z].GetComponent<Chunk>().HasBlockAt(blockIndex);
            result.hasNeighborChunk = true;
        }
        else
        {
            result.hasNeighborChunk = false;
            result.hasNeighborChunkCube = false;
        }
        return result;
    }



  

    
}
