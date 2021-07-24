using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics;
using Debug = UnityEngine.Debug;
using Unity.Jobs;
using Unity.Collections;

public struct ChunkNeighborCheckResult
{
    public bool hasNeighborChunk;
    public bool hasNeighborChunkCube;
}

public struct ChunkData
{
    public Chunk chunk;
    public int physicalChunkId;
}

struct CreateChunk: IJobParallelFor
{
  

    public void Execute(int index)
    {
     //-- Debug.Log("Chunk id: " + index);
        TerrainGenerator.Instance.GetChunk(index).CreateMeshData();
    }
}


public class TerrainGenerator : MonoBehaviour
{
   


    public static TerrainGenerator Instance { get; private set; }


    public GameObject player;
    public int currentPlayerChunk;


    public int width;
    public int height;
    public int lenght;

    public GameObject ChunkPrefab;
    public int chunkWidth;
    public int chunkHeight;

    public int nrOfCubes;
    public int nrOfFaces;
    public long createTime;

    // public MapGenerator mapGenerator;

    public GameObject[,] terrainBlocks;
    private GameObject block;

    public float[,] noiseMap;

    private GameObject terrainObject;



    public static Chunk[] chunks;
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


        currentPlayerChunk = GetChunkIDFromPos(player.transform.position);

        Debug.Log("Player started in chunk: " + currentPlayerChunk);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return) && isCreated == false)
        {
            CreateTerrain();
            isCreated = true;
        }

        if(isCreated)
        {
            UpdateDynamicChunkLoading();
        }

    }



    
    public Chunk GetChunk(int id)
    {
        return chunks[id];

    }

    void CreateTerrain()
    {

   
        chunks = new Chunk[width* height* lenght];

        Vector3 position = new Vector3(0, 0, 0);

        //int i = 0;
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                for (int z = 0; z < lenght; z++)
                {
                    position.x = x;
                    position.y = y;
                    position.z = z;

                    int chunkArrayIndex = x + height * (y + lenght * z);

                    chunks[chunkArrayIndex] = Instantiate(ChunkPrefab, position, Quaternion.identity).GetComponent<Chunk>();
                    chunks[chunkArrayIndex].InitChunk(position, chunkWidth, chunkHeight);

                    chunks[chunkArrayIndex].chunkID = GetChunkIDFromPos(chunks[chunkArrayIndex].transform.position);
                    nrOfCreatedChunks++;
                }
            }
        }
        st.Start();
        /* 

         for (int i = 0; i < chunks.Length; i++)
         {
             GetChunk(i).GetComponent<Chunk>().CreateMeshData();
         }
        */
/*   */
        CreateChunk jobData = new CreateChunk();
  

        // Schedule the job with one Execute per index in the results array and only 1 item per processing batch
        JobHandle handle = jobData.Schedule(chunks.Length, 1);

        // Wait for the job to complete
        handle.Complete();
     
        for (int i = 0; i < chunks.Length; i++)
        {
            GetChunk(i).GetComponent<Chunk>().ApplyMeshData();
        }

        st.Stop();
        createTime = st.ElapsedMilliseconds;
        Debug.Log(string.Format("Creating world took {0} ms to complete", createTime));

        //player.transform.position = new Vector3((width * chunkWidth / 2)+0.5f, height, (lenght * chunkWidth / 2) + 0.5f);

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
            result.hasNeighborChunkCube = chunks[GetChunkIDFromPos(chunkIndex)].HasBlockAt(blockIndex);

            result.hasNeighborChunk = true;
        }
        else
        {
            result.hasNeighborChunk = false;
            result.hasNeighborChunkCube = false;
        }
        return result;
    }

    public int GetChunkIDFromPos(Vector3 pos)
    {
        Vector3Int posInt = Vector3Int.CeilToInt(pos);
        return posInt.x + height * (posInt.y + lenght * posInt.z);
    }

    private void UpdateDynamicChunkLoading()
    {
        if( chunks.Length > 0)
        {
            Vector3 playerPos = player.transform.position;
            if (currentPlayerChunk != GetChunkIDFromPos(playerPos) && IsChunkeLoaded(GetChunkIDFromPos(playerPos)) == false)
            {
                Debug.Log("Player is in chunk: " + GetChunkIDFromPos(playerPos));


                ChunkData chunkData = GetFurthestChunkFromPos(playerPos);
                Chunk chunkToMove = chunkData.chunk;
              
                chunkToMove.transform.position = new Vector3((int)(playerPos.x / chunkWidth), (int)(playerPos.y / chunkHeight), (int)(playerPos.z / chunkWidth));



                chunkToMove.chunkID = GetChunkIDFromPos(chunkToMove.transform.position);
            }

            currentPlayerChunk = GetChunkIDFromPos(playerPos);


        }


    }

    private ChunkData GetFurthestChunkFromPos(Vector3 pos)
    {
        ChunkData result;
        result.chunk = chunks[0];
        result.physicalChunkId = 0;
        float furthestDistance = (chunks[0].transform.position - pos).magnitude;


        for (int i = 1; i < chunks.Length; i++)
        {
            float distance = (chunks[i].transform.position - pos).magnitude;
            //Debug.Log("ID: " + chunks[i].chunkID + ", " + distance + ", " + furthestDistance);
            if(distance > furthestDistance)
            {
                result.chunk = chunks[i];
                result.physicalChunkId = chunks[i].chunkID;
                furthestDistance = distance;
            }
        }

        return result;
    }

    private bool IsChunkeLoaded(int chunkID)
    {
        for (int i = 0; i < chunks.Length; i++)
        {
            if (chunks[i].chunkID == chunkID)
            {
                return true;
            }
        }

        return false;
    }



}
