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




struct CreateChunk : IJobParallelFor
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

    public bool endlessTerrain;

    public GameObject player;
    public Vector3Int currentPlayerChunkID;


    public int width;
    public int height;
    public int lenght;

    public GameObject ChunkPrefab;
    public int chunkWidth;
    public int chunkHeight;

    public float noiseFrequency;
    public float minNoiseValue;

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
    public List<int> outOfPlayChunks;

    private int nrOfCreatedChunks = 0;

    private bool isCreated = false;

    Stopwatch st = new Stopwatch();

    private int loadedChunks = 0;
    private bool loadAllChunks = false;
    private float chunkLoadDelay = 0.0f;
    private float chunkLoadDelayCounter = 0.0f;

    private JobHandle jobHandle;
    CreateChunk jobData;

    void Awake()
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }


    public void Start()
    {
        jobData = new CreateChunk();

        areaOfPlay = new Vector3Int[width, height, lenght];
        HUDScript.Instance.nrOfChunks = width * height * lenght;

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
                    print("[" + i + "," + j + "," + k + "] = " + areaOfPlay[i, j, k]);
                    k++;
                }
                j++;
            }
            i++;
        }




        //currentPlayerChunkID = GetChunkIDFromPos(player.transform.position);

        currentPlayerChunkID = GetChunkIDFromPos(player.transform.position);


        Debug.Log("Player started in chunk: " + currentPlayerChunkID);

        //Application.targetFrameRate = 30;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return) && isCreated == false)
        {
            CreateTerrain();
            isCreated = true;
        }

        if (isCreated && loadedChunks == chunks.Length && endlessTerrain)
        {
            UpdateDynamicChunkLoading();
        }

        if (Input.GetKeyUp(KeyCode.Backspace))
        {
            FillEmptyChunk();
        }

        if (Input.GetKeyDown(KeyCode.Insert) && chunks != null)
        {
            loadAllChunks = true;
        }

        if (Input.GetKeyDown(KeyCode.Home) && chunks != null)
        {
            if (jobHandle.IsCompleted)
            {
                jobHandle = jobData.Schedule(nrOfCreatedChunks, 1);
                nrOfCreatedChunks++;
            }
        }

        chunkLoadDelayCounter += Time.deltaTime;
        if ((Input.GetKeyUp(KeyCode.Space) || loadAllChunks) && chunks != null && chunkLoadDelayCounter >= chunkLoadDelay)
        {
            if (loadedChunks < chunks.Length)
            {
                //GetChunk(loadedChunks).GetComponent<Chunk>().CreateMeshData();
                GetChunk(loadedChunks).GetComponent<Chunk>().ApplyMeshData();

                loadedChunks++;
                chunkLoadDelayCounter = 0.0f;

                if (loadedChunks == chunks.Length)
                {
                    HUDScript.Instance.averageChunkCreationTime /= loadedChunks;
                }
            }
        }

    }

    public Chunk GetChunk(int id)
    {
        return chunks[id];

    }

    void CreateTerrain()
    {


        chunks = new Chunk[width * height * lenght];

        Vector3 position;

        //int i = 0;
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                for (int z = 0; z < lenght; z++)
                {
                    position = Vector3Int.FloorToInt(player.transform.position);

                    position += new Vector3(areaOfPlay[x, y, z].x * chunkWidth, areaOfPlay[x, y, z].y * chunkHeight, areaOfPlay[x, y, z].z * chunkWidth);

                    chunks[nrOfCreatedChunks] = Instantiate(ChunkPrefab, position, Quaternion.identity).GetComponent<Chunk>();
                    chunks[nrOfCreatedChunks].InitChunk(position, chunkWidth, chunkHeight);

                    chunks[nrOfCreatedChunks].chunkID = GetChunkIDFromPos(position);
                    chunks[nrOfCreatedChunks].physicalChunkID = nrOfCreatedChunks;

                    nrOfCreatedChunks++;
                }
            }
        }
        st.Start();


        //for (int i = 0; i < chunks.Length; i++)
        //{
        //    GetChunk(i).GetComponent<Chunk>().CreateMeshData();
        //}





        // Schedule the job with one Execute per index in the results array and only 1 item per processing batch
        jobHandle = jobData.Schedule(chunks.Length, 1);
        // Wait for the job to complete
        jobHandle.Complete();

        //for (int i = 0; i < chunks.Length; i++)
        //{
        //    GetChunk(i).GetComponent<Chunk>().ApplyMeshData();
        //}

        loadAllChunks = true;

        st.Stop();
        createTime = st.ElapsedMilliseconds;
        Debug.Log(string.Format("Creating world took {0} ms to complete", createTime));
        //HUDScript.Instance.nrOfCubes -=1000000;

        //player.transform.position = new Vector3((width * chunkWidth / 2)+0.5f, height, (lenght * chunkWidth / 2) + 0.5f);

    }



    public ChunkNeighborCheckResult BlockHasChunkNeighbor(Vector3 chunkPos, Vector3 cubePosLocal, Vector3 faceNormal)
    {
        ChunkNeighborCheckResult result = new ChunkNeighborCheckResult();

        result.hasNeighborChunk = false;
        result.hasNeighborChunkCube = false;

        Vector3Int chunkID = Vector3Int.CeilToInt(chunkPos + faceNormal);


        int physicalChunkID = GetPhysicalChunkID(chunkID);
        if (physicalChunkID >= 0)
        {
            //Only change the vale of the normal axis, change it to the oposit pos 

            int posX = faceNormal.x == 0 ? (int)cubePosLocal.x : (int)cubePosLocal.x - (chunkWidth - 1) * (int)faceNormal.x;
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




    public Vector3Int GetChunkIDFromPos(Vector3 pos)
    {
        float offset = 1000.0f; //To come away from the -0 and 0 rounding problem 

        pos.x = (pos.x * offset) / (chunkWidth * offset);
        pos.y = (pos.y * offset) / (chunkHeight * offset);
        pos.z = (pos.z * offset) / (chunkWidth * offset);

        Vector3Int chunkID = Vector3Int.FloorToInt(pos);

        return chunkID;
    }

    private void UpdateDynamicChunkLoading()
    {
        if (chunks.Length > 0)
        {
            Vector3Int playerPos = GetChunkIDFromPos(player.transform.position);

            if (currentPlayerChunkID != playerPos)//&& IsChunkeLoaded(GetChunkIDFromPos(playerPos)) == false)
            {
                Debug.Log("Player is in chunk: " + playerPos);


                CalculateEmptyChunksInAreaOfPlay();

            }

            currentPlayerChunkID = playerPos;

            FillEmptyChunk();
        }


    }

    private void FillEmptyChunk()
    {
        if (emptyChunks.Count > 0)
        {

            //Vector3 playerPos = player.transform.position;
            //playerPos.x = (int)playerPos.x;
            //playerPos.y = (int)playerPos.y;
            //playerPos.z = (int)playerPos.z;
            int emptyChunkIndex = 0;
            int chunkToMoveIndex = GetFurthestOutOfPlayChunkIndex(currentPlayerChunkID);

            Chunk chunkToMove = chunks[outOfPlayChunks[chunkToMoveIndex]];
            Debug.Log("chunkToMove ID: " + chunkToMove.chunkID);

            chunkToMove.inPlay = true;
            Vector3Int position;

            position = new Vector3Int(emptyChunks[emptyChunkIndex].x * chunkWidth, emptyChunks[emptyChunkIndex].y * chunkHeight, emptyChunks[emptyChunkIndex].z * chunkWidth);

            chunkToMove.transform.position = position;

            // Vector3Int newChunkID = chunkToMove.transform.pos;
            chunkToMove.chunkID = GetChunkIDFromPos(position);

            //chunkToMove.InitChunk(position, chunkWidth, chunkHeight); //Optimize this
            //chunkToMove.CreateMeshData();
            //chunkToMove.ApplyMeshData();

            emptyChunks.RemoveAt(emptyChunkIndex);
            outOfPlayChunks.RemoveAt(chunkToMoveIndex);

        }
    }

    private int GetFurthestOutOfPlayChunkIndex(Vector3 pos)
    {
        int index = 0;
        float furthestDistance = (chunks[outOfPlayChunks[0]].chunkID - pos).magnitude;

        for (int i = 1; i < outOfPlayChunks.Count; i++)
        {
            int chunkArrayID = outOfPlayChunks[i];
            float distance = (chunks[chunkArrayID].chunkID - pos).magnitude;

            // Debug.Log("ID: " + chunks[i].chunkID + ", " + distance + ", " + furthestDistance);
            if (distance > furthestDistance && chunks[chunkArrayID].inPlay == false)
            {
                furthestDistance = distance;
                index = i;
            }
        }

        return index;
    }

    private void CalculateEmptyChunksInAreaOfPlay()
    {
        emptyChunks.Clear();
        outOfPlayChunks.Clear();

        Vector3Int emptyChunkId;
        int index = 0;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                for (int z = 0; z < lenght; z++)
                {

                    //Check out of play
                    chunks[index].inPlay = IsChunkInAreaOfPlay(chunks[index].chunkID);

                    if (chunks[index].inPlay == false)
                    {
                        outOfPlayChunks.Add(index);
                    }

                    //Check if empty chunk
                    emptyChunkId = GetChunkIDFromPos(player.transform.position);
                    emptyChunkId += new Vector3Int(areaOfPlay[x, y, z].x, areaOfPlay[x, y, z].y, areaOfPlay[x, y, z].z);

                    if (IsChunkeLoaded(emptyChunkId) == false)
                    {
                        AddChunkIDToEmpyChunksQueue(emptyChunkId);
                    }
                    index++;
                }
            }
        }

        print("Nr of empty chunks: " + emptyChunks.Count);
    }

    private void AddChunkIDToEmpyChunksQueue(Vector3Int chunkID)
    {
        //Could add so one cant add the same chunk more than once

        Vector3Int playerPos = Vector3Int.FloorToInt(player.transform.position);
        float distanceToPlayer = (playerPos - chunkID).magnitude;

        int indexToAddTo = 0;

        for (int i = 0; i < emptyChunks.Count; i++)
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
            if (chunks[i].chunkID == chunkID)
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
            if (chunks[i] != null)
            {
                if (chunks[i].chunkID == chunkID)
                {
                    return i;
                }
            }

        }

        return -1;
    }

    private bool IsChunkInAreaOfPlay(Vector3Int chunkID)
    {
        Vector3Int playerPos = GetChunkIDFromPos(player.transform.position);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                for (int z = 0; z < lenght; z++)
                {
                    if (chunkID == playerPos + new Vector3Int(areaOfPlay[x, y, z].x, areaOfPlay[x, y, z].y, areaOfPlay[x, y, z].z))
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }





    //3D perlin noice
    //https://www.youtube.com/watch?v=SoakEoUQ7Rg&ab_channel=motuDev

    private static int[] permutation = {
        151,160,137, 91, 90, 15,131, 13,201, 95, 96, 53,194,233,  7,225,
        140, 36,103, 30, 69,142,  8, 99, 37,240, 21, 10, 23,190,  6,148,
        247,120,234, 75,  0, 26,197, 62, 94,252,219,203,117, 35, 11, 32,
         57,177, 33, 88,237,149, 56, 87,174, 20,125,136,171,168, 68,175,
         74,165, 71,134,139, 48, 27,166, 77,146,158,231, 83,111,229,122,
         60,211,133,230,220,105, 92, 41, 55, 46,245, 40,244,102,143, 54,
         65, 25, 63,161,  1,216, 80, 73,209, 76,132,187,208, 89, 18,169,
        200,196,135,130,116,188,159, 86,164,100,109,198,173,186,  3, 64,
         52,217,226,250,124,123,  5,202, 38,147,118,126,255, 82, 85,212,
        207,206, 59,227, 47, 16, 58, 17,182,189, 28, 42,223,183,170,213,
        119,248,152,  2, 44,154,163, 70,221,153,101,155,167, 43,172,  9,
        129, 22, 39,253, 19, 98,108,110, 79,113,224,232,178,185,112,104,
        218,246, 97,228,251, 34,242,193,238,210,144, 12,191,179,162,241,
         81, 51,145,235,249, 14,239,107, 49,192,214, 31,181,199,106,157,
        184, 84,204,176,115,121, 50, 45,127,  4,150,254,138,236,205, 93,
        222,114, 67, 29, 24, 72,243,141,128,195, 78, 66,215, 61,156,180,

        151,160,137, 91, 90, 15,131, 13,201, 95, 96, 53,194,233,  7,225,
        140, 36,103, 30, 69,142,  8, 99, 37,240, 21, 10, 23,190,  6,148,
        247,120,234, 75,  0, 26,197, 62, 94,252,219,203,117, 35, 11, 32,
         57,177, 33, 88,237,149, 56, 87,174, 20,125,136,171,168, 68,175,
         74,165, 71,134,139, 48, 27,166, 77,146,158,231, 83,111,229,122,
         60,211,133,230,220,105, 92, 41, 55, 46,245, 40,244,102,143, 54,
         65, 25, 63,161,  1,216, 80, 73,209, 76,132,187,208, 89, 18,169,
        200,196,135,130,116,188,159, 86,164,100,109,198,173,186,  3, 64,
         52,217,226,250,124,123,  5,202, 38,147,118,126,255, 82, 85,212,
        207,206, 59,227, 47, 16, 58, 17,182,189, 28, 42,223,183,170,213,
        119,248,152,  2, 44,154,163, 70,221,153,101,155,167, 43,172,  9,
        129, 22, 39,253, 19, 98,108,110, 79,113,224,232,178,185,112,104,
        218,246, 97,228,251, 34,242,193,238,210,144, 12,191,179,162,241,
         81, 51,145,235,249, 14,239,107, 49,192,214, 31,181,199,106,157,
        184, 84,204,176,115,121, 50, 45,127,  4,150,254,138,236,205, 93,
        222,114, 67, 29, 24, 72,243,141,128,195, 78, 66,215, 61,156,180
    };

    const int permutationCount = 255;

    private static Vector3[] directions = {
        new Vector3( 1f, 1f, 0f),
        new Vector3(-1f, 1f, 0f),
        new Vector3( 1f,-1f, 0f),
        new Vector3(-1f,-1f, 0f),
        new Vector3( 1f, 0f, 1f),
        new Vector3(-1f, 0f, 1f),
        new Vector3( 1f, 0f,-1f),
        new Vector3(-1f, 0f,-1f),
        new Vector3( 0f, 1f, 1f),
        new Vector3( 0f,-1f, 1f),
        new Vector3( 0f, 1f,-1f),
        new Vector3( 0f,-1f,-1f),

        new Vector3( 1f, 1f, 0f),
        new Vector3(-1f, 1f, 0f),
        new Vector3( 0f,-1f, 1f),
        new Vector3( 0f,-1f,-1f)
    };

    private const int directionCount = 15;

    private float Scalar(Vector3 a, Vector3 b)
    {
        return a.x * b.x + a.y * b.y + a.z * b.z;
    }

    private float SmoothDistance(float d)
    {
        return d * d * d * (d * (d * 6f - 15f) + 10f);
    }
    public bool Get3DPerlinNoise(Vector3 point)
    {
        point *= noiseFrequency;

        int flooredPointX0 = Mathf.FloorToInt(point.x);
        int flooredPointY0 = Mathf.FloorToInt(point.y);
        int flooredPointZ0 = Mathf.FloorToInt(point.z);

        float distanceX0 = point.x - flooredPointX0;
        float distanceY0 = point.y - flooredPointY0;
        float distanceZ0 = point.z - flooredPointZ0;

        float distanceX1 = distanceX0 - 1f;
        float distanceY1 = distanceY0 - 1f;
        float distanceZ1 = distanceZ0 - 1f;

        flooredPointX0 &= permutationCount;
        flooredPointY0 &= permutationCount;
        flooredPointZ0 &= permutationCount;

        int flooredPointX1 = flooredPointX0 + 1;
        int flooredPointY1 = flooredPointY0 + 1;
        int flooredPointZ1 = flooredPointZ0 + 1;

        int permutationX0 = permutation[flooredPointX0];
        int permutationX1 = permutation[flooredPointX1];

        int permutationY00 = permutation[permutationX0 + flooredPointY0];
        int permutationY10 = permutation[permutationX1 + flooredPointY0];
        int permutationY01 = permutation[permutationX0 + flooredPointY1];
        int permutationY11 = permutation[permutationX1 + flooredPointY1];
        /*
                int permutationZ000 = permutation[permutationY00 + flooredPointZ0];
                int permutationZ100 = permutation[permutationY10 + flooredPointZ0];
                int permutationZ010 = permutation[permutationY01 + flooredPointZ0];
                int permutationZ110 = permutation[permutationY11 + flooredPointZ0];
                int permutationZ001 = permutation[permutationY00 + flooredPointZ1];
                int permutationZ101 = permutation[permutationY01 + flooredPointZ1];
                int permutationZ011 = permutation[permutationY10 + flooredPointZ1];
                int permutationZ111 = permutation[permutationY11 + flooredPointZ1];
        */
        Vector3 direction000 = directions[permutation[permutationY00 + flooredPointZ0] & directionCount];
        Vector3 direction100 = directions[permutation[permutationY10 + flooredPointZ0] & directionCount];
        Vector3 direction010 = directions[permutation[permutationY01 + flooredPointZ0] & directionCount];
        Vector3 direction110 = directions[permutation[permutationY11 + flooredPointZ0] & directionCount];
        Vector3 direction001 = directions[permutation[permutationY00 + flooredPointZ1] & directionCount];
        Vector3 direction101 = directions[permutation[permutationY10 + flooredPointZ1] & directionCount];
        Vector3 direction011 = directions[permutation[permutationY01 + flooredPointZ1] & directionCount];
        Vector3 direction111 = directions[permutation[permutationY11 + flooredPointZ1] & directionCount];

        /*
                Vector3 direction000 = directions[permutationZ000 & directionCount];
                Vector3 direction100 = directions[permutationZ100 & directionCount];
                Vector3 direction010 = directions[permutationZ010 & directionCount];
                Vector3 direction110 = directions[permutationZ110 & directionCount];
                Vector3 direction001 = directions[permutationZ001 & directionCount];
                Vector3 direction101 = directions[permutationZ101 & directionCount];
                Vector3 direction011 = directions[permutationZ011 & directionCount];
                Vector3 direction111 = directions[permutationZ111 & directionCount];
        */

        float value000 = Scalar(direction000, new Vector3(distanceX0, distanceY0, distanceZ0));
        float value100 = Scalar(direction100, new Vector3(distanceX1, distanceY0, distanceZ0));
        float value010 = Scalar(direction010, new Vector3(distanceX0, distanceY1, distanceZ0));
        float value110 = Scalar(direction110, new Vector3(distanceX1, distanceY1, distanceZ0));
        float value001 = Scalar(direction001, new Vector3(distanceX0, distanceY0, distanceZ1));
        float value101 = Scalar(direction101, new Vector3(distanceX1, distanceY0, distanceZ1));
        float value011 = Scalar(direction011, new Vector3(distanceX0, distanceY1, distanceZ1));
        float value111 = Scalar(direction111, new Vector3(distanceX1, distanceY1, distanceZ1));

        float smoothDistanceX = SmoothDistance(distanceX0);
        float smoothDistanceY = SmoothDistance(distanceY0);
        float smoothDistanceZ = SmoothDistance(distanceZ0);

        float noiseValue = Mathf.Lerp(
            Mathf.Lerp(Mathf.Lerp(value000, value100, smoothDistanceX), Mathf.Lerp(value010, value110, smoothDistanceX), smoothDistanceY),
            Mathf.Lerp(Mathf.Lerp(value001, value101, smoothDistanceX), Mathf.Lerp(value011, value111, smoothDistanceX), smoothDistanceY),
            smoothDistanceZ);

        return noiseValue > minNoiseValue;
    }
}