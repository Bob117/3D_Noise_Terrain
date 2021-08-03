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

    public GameObject marker;

    public GameObject player;
    public Vector3Int currentPlayerChunkID;


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
    public Vector3Int[,,] areaOfPlay; //3D grid with chunk offsets relativ to the player. Add the player pos to this to get chunk id.
    public List<Vector3Int> emptyChunks;

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

        areaOfPlay = new Vector3Int[width,height,lenght];

        Vector3 position = new Vector3(0, 0, 0);

        int i = 0;
        int j = 0;
        int k = 0;


        for (float x = (-width * 0.5f); x < (width * 0.5f); x++)
        {
            j = 0;

            for (float y = (-height * 0.5f); y < (height * 0.5f); y++)
            {
                k = 0;

                for (float z = (-lenght * 0.5f); z < (lenght * 0.5f); z++)
                {
                    areaOfPlay[i, j, k] = new Vector3Int((int)(x - width) + width, (int)(y - height) + height, (int)(z - width) + width);
                    print("["+ i +","+ j+","+ k+"] = " + areaOfPlay[i, j, k]);
                    k++;
                }
                j++;
            }
            i++;
        }




        currentPlayerChunkID = GetChunkIDFromPos(player.transform.position);



        Debug.Log("Player started in chunk: " + currentPlayerChunkID);

        Application.targetFrameRate = 30;
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





        if (Input.GetKeyUp(KeyCode.Backspace))
        {

            FillEmptyChunk();
        }




    }



    
    public Chunk GetChunk(int id)
    {
        return chunks[id];

    }

    void CreateTerrain()
    {

   
        chunks = new Chunk[width* height* lenght];

        Vector3 position = player.transform.position;

        //int i = 0;
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                for (int z = 0; z < lenght; z++)
                {
                    position = Vector3Int.FloorToInt(player.transform.position) + areaOfPlay[x, y, z];

                    chunks[nrOfCreatedChunks] = Instantiate(ChunkPrefab, position, Quaternion.identity).GetComponent<Chunk>();
                    chunks[nrOfCreatedChunks].InitChunk(position, chunkWidth, chunkHeight);

                    chunks[nrOfCreatedChunks].chunkID = GetChunkIDFromPos(chunks[nrOfCreatedChunks].transform.position);
                    chunks[nrOfCreatedChunks].physicalChunkID = nrOfCreatedChunks;


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

        Vector3Int chunkID = Vector3Int.CeilToInt(chunkPos+ faceNormal);


        int physicalChunkID = GetPhysicalChunkID(chunkID);
        if (physicalChunkID >= 0)
        {
            //Only change the vale of the normal axis, change it to the oposit pos 

            int posX = faceNormal.x == 0 ? (int)cubePosLocal.x : (int)cubePosLocal.x - (chunkWidth-1) * (int)faceNormal.x;
            int posY = faceNormal.y == 0 ? (int)cubePosLocal.y : (int)cubePosLocal.y - (chunkHeight - 1) * (int)faceNormal.y;
            int posZ = faceNormal.z == 0 ? (int)cubePosLocal.z : (int)cubePosLocal.z - (chunkWidth - 1) * (int)faceNormal.z;

            Vector3Int blockIndex = new Vector3Int(posX, posY, posZ);
            result.hasNeighborChunkCube = chunks[physicalChunkID].HasBlockAt(blockIndex); 

            result.hasNeighborChunk = true;
        }
        else
        {
            result.hasNeighborChunk = false;
            result.hasNeighborChunkCube = false;
        }
        return result;
    }


    

    public Vector3Int GetChunkIDFromPos(Vector3 pos) //Need fix?
    {
        Vector3Int chunkID = new Vector3Int();

        chunkID.x = (int)(pos.x / chunkWidth);
        chunkID.y = (int)(pos.y / chunkHeight);
        chunkID.z = (int)(pos.z / chunkWidth);



        return chunkID;
    }

    private void UpdateDynamicChunkLoading()
    {
        if( chunks.Length > 0)
        {
            Vector3 playerPos = player.transform.position;
            playerPos.x = (int)playerPos.x;
            playerPos.y = (int)playerPos.y;
            playerPos.z = (int)playerPos.z;




            if (currentPlayerChunkID != GetChunkIDFromPos(playerPos) )//&& IsChunkeLoaded(GetChunkIDFromPos(playerPos)) == false)
            {
                Debug.Log("Player is in chunk: " + GetChunkIDFromPos(playerPos));

                
                CalculateEmptyChunksInAreaOfPlay();

            }

            currentPlayerChunkID = GetChunkIDFromPos(playerPos);

            FillEmptyChunk();
        }


    }

    private void FillEmptyChunk()
    {
        if (emptyChunks.Count > 0)
        {

            Vector3 playerPos = player.transform.position;
            playerPos.x = (int)playerPos.x;
            playerPos.y = (int)playerPos.y;
            playerPos.z = (int)playerPos.z;
            int index = 0;

            Chunk chunkToMove = GetFurthestChunkFromPos(playerPos);

            chunkToMove.transform.position = new Vector3(emptyChunks[index].x * chunkWidth,
                                                         emptyChunks[index].y * chunkHeight,
                                                         emptyChunks[index].z * chunkWidth);

            Vector3Int newChunkID = GetChunkIDFromPos(chunkToMove.transform.position);
            chunkToMove.chunkID = newChunkID;
            
            emptyChunks.RemoveAt(index);
        }
    }

    private Chunk GetFurthestChunkFromPos(Vector3 pos)
    {
        Chunk chunk;
        chunk = chunks[0];
        chunk.physicalChunkID = 0;
        float furthestDistance = (chunks[0].transform.position - pos).magnitude;


        for (int i = 1; i < chunks.Length; i++)
        {
            float distance = (chunks[i].transform.position - pos).magnitude;
            //Debug.Log("ID: " + chunks[i].chunkID + ", " + distance + ", " + furthestDistance);
            if(distance > furthestDistance)
            {
                chunk = chunks[i];
                chunk.physicalChunkID = chunks[i].physicalChunkID;
                furthestDistance = distance;
            }
        }

        return chunk;
    }

    private void CalculateEmptyChunksInAreaOfPlay()
    {
        emptyChunks.Clear();

        Vector3Int playerPos = Vector3Int.FloorToInt(player.transform.position);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                for (int z = 0; z < lenght; z++)
                {
                    if (IsChunkeLoaded(areaOfPlay[x, y, z] + playerPos) == false)
                    {
                        AddChunkIDToEmpyChunksQueue(areaOfPlay[x, y, z] + playerPos);
                    }
                }
            }
        }

        print("Nr of empty chunks: " + emptyChunks.Count);
    }

    private void AddChunkIDToEmpyChunksQueue(Vector3Int chunkID) //Doesnt seem to work as intended
    {
        //Could add so one cant add the same chunk more than once

        Vector3Int playerPos = Vector3Int.FloorToInt(player.transform.position);
        float distanceToPlayer = (playerPos - chunkID).magnitude;

        int indexToAddTo = 0;

        for(int i = 0; i < emptyChunks.Count; i++)
        {
            if ((playerPos - emptyChunks[i]).magnitude < distanceToPlayer)
            {
                indexToAddTo = i;
            }
        }

        emptyChunks.Insert(indexToAddTo, chunkID);


    }


    private bool IsChunkeLoaded(Vector3Int chunkID)
    { 
        for (int i = 0; i < chunks.Length; i++)
        {
            if(chunks[i].chunkID == chunkID)
            {
                return true;
            }
        }

        return false;
    }

    private int GetPhysicalChunkID(Vector3Int chunkID)
    {
        for (int i = 0; i < chunks.Length; i++)
        {
            if (chunks[i].chunkID == chunkID)
            {
                return i;
            }
        }

        return -1;
    }


}
