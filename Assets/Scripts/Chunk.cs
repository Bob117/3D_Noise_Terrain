using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;




public class Chunk : MonoBehaviour
{
    public Vector3Int chunkID;
    public int physicalChunkID;

    public int chunkWidth;
    public int chunkHeight;


    [SerializeField] private Vector3 chunkPosition;
    public bool inPlay = true;

    [SerializeField] private int maxNrOfCubes;
    [SerializeField] private int nrOfCubes;
    [SerializeField] private int nrOfBorderFaces;
    [SerializeField] private int nrOfFaces;
    [SerializeField] long createTime = 0;

    public Material testMaterial;


    private const int MAX_NR_OF_CUBES = 2500;
    private const int NR_OF_VERTICES_PER_CUBES = 24;
    private const int NR_OF_UV_PER_CUBES = 24;
    private const int NR_OF_TRIANGLES_PER_CUBES = 36;

    private const int NR_OF_VERTICES_PER_FACE = 4;
    private const int NR_OF_UV_PER_FACE = 4;
    private const int NR_OF_TRIANGLES_PER_FACE = 6;

    private bool[,,] chunkBlocks;
    private Mesh mesh;

    private Vector3[] vertices;
    private Vector3[] normals;
    private Vector2[] uv;
    private int[] triangles;


    Stopwatch st = new Stopwatch();


    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        
       
        //Whatever needs timing here
      
    }

    public void InitChunk(Vector3 chunkPos, int width, int height)
    {
        mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;



       

        chunkPosition = chunkPos;
        chunkWidth = width;
        chunkHeight = height;

        transform.position = new Vector3(chunkPos.x, chunkPos.y, chunkPos.z);
        chunkBlocks = new bool[chunkWidth, chunkHeight, chunkWidth];

        maxNrOfCubes = chunkWidth * chunkHeight * chunkWidth;
        // nrOfCubes = MAX_NR_OF_CUBES;


        for (int x = 0; x < chunkWidth; x++)
        {
            for (int y = 0; y < chunkHeight-1; y++)
            {
                for (int z = 0; z < chunkWidth; z++)
                {
                    //int rand = Random.Range(0, 100);
                    //if(rand == 1)
                    //{
                    //    chunkBlocks[x, y, z] = true;
                    //}
                    //else
                    //{
                    //    chunkBlocks[x, y, z] = false;
                    //}

                    //if (y < z)
                    //{
                    //    chunkBlocks[x, y, z] = true;
                    //}
                    //else
                    //{
                    //    chunkBlocks[x, y, z] = false;
                    //}

                    chunkBlocks[x, y, z] = true;

                }
            }
        }
    }

    public bool IsIndexInsideChunk(Vector3Int index)
    {
        bool result = false;

        if ((index.x >= 0 && index.x < chunkWidth) &&
            (index.y >= 0 && index.y < chunkHeight) &&
            (index.z >= 0 && index.z < chunkWidth))
        {
            result = true;
        }

        return result;
    }

    public bool HasBlockAt(Vector3Int index)
    {
        bool result = false;

        if (IsIndexInsideChunk(index))
        {
            if (chunkBlocks[index.x, index.y, index.z] == true)
            {
                result = true;
            }
        }

        return result;
    }


    private bool ShouldDrawFace(Vector3 cubePosLocal, Vector3 faceNormal, bool isBorderCube)
    {
        bool result = true;
        
        Vector3Int neighborIndex = Vector3Int.CeilToInt(cubePosLocal + faceNormal);
        ChunkNeighborCheckResult neighborCheckResult = new ChunkNeighborCheckResult();
        neighborCheckResult.hasNeighborChunk = true;
        neighborCheckResult.hasNeighborChunkCube = false;


        if (isBorderCube)
        {
            neighborCheckResult = TerrainGenerator.Instance.BlockHasChunkNeighbor(chunkPosition, cubePosLocal, faceNormal);
            nrOfBorderFaces++;
        }

        if (HasBlockAt(neighborIndex) || neighborCheckResult.hasNeighborChunkCube == true || 
            (isBorderCube && neighborCheckResult.hasNeighborChunk == false && IsIndexInsideChunk(neighborIndex) == false))
        {
            result = false;
        }

        return result;
    }

    private void CreateCube(int index, Vector3[] vertices, Vector3[] normals, Vector2[] uv, int[] triangles, Vector3 cubePosLocal, bool isBorderCube)
    {
        

        int offsetVertecies = 0;
        int offsetUV = 0;
        int offsetTriangles = 0;

        Vector3 left_top_front = new Vector3(cubePosLocal.x, 1.0f + cubePosLocal.y,cubePosLocal.z); //left_top_front
        Vector3 right_top_front = new Vector3(1.0f + cubePosLocal.x, 1.0f + cubePosLocal.y,cubePosLocal.z); //right_top_front
        Vector3 left_bottom_front = new Vector3(cubePosLocal.x, cubePosLocal.y, cubePosLocal.z); //left_bottom_front
        Vector3 right_bottom_front = new Vector3(1.0f + cubePosLocal.x,  cubePosLocal.y, cubePosLocal.z); //right_bottom_front

        Vector3 left_top_back = new Vector3(cubePosLocal.x, 1.0f + cubePosLocal.y, 1.0f + cubePosLocal.z); //left_top_back
        Vector3 right_top_back = new Vector3(1.0f + cubePosLocal.x, 1.0f + cubePosLocal.y, 1.0f + cubePosLocal.z); //right_top_back
        Vector3 left_bottom_back = new Vector3(cubePosLocal.x, cubePosLocal.y, 1.0f + cubePosLocal.z); //left_bottom_back
        Vector3 right_bottom_back = new Vector3(1.0f + cubePosLocal.x, cubePosLocal.y, 1.0f + cubePosLocal.z); //right_bottom_back

        Vector3 normal_front = new Vector3(0, 0, -1);
        Vector3 normal_left = new Vector3(-1, 0, 0);
        Vector3 normal_right = new Vector3(1, 0, 0);
        Vector3 normal_top = new Vector3(0, 1, 0);
        Vector3 normal_bottom = new Vector3(0, -1, 0);
        Vector3 normal_back = new Vector3(0, 0, 1);


        //Front
        if (ShouldDrawFace(cubePosLocal, normal_front, isBorderCube))
        {
            offsetVertecies = NR_OF_VERTICES_PER_FACE * nrOfFaces;
            offsetUV = NR_OF_UV_PER_FACE * nrOfFaces;
            offsetTriangles = NR_OF_TRIANGLES_PER_FACE * nrOfFaces;
            nrOfFaces++;

            vertices[0 + offsetVertecies] = left_top_front;
            vertices[1 + offsetVertecies] = right_top_front;
            vertices[2 + offsetVertecies] = left_bottom_front;
            vertices[3 + offsetVertecies] = right_bottom_front;

            normals[0 + offsetVertecies] = normal_front;
            normals[1 + offsetVertecies] = normal_front;
            normals[2 + offsetVertecies] = normal_front;
            normals[3 + offsetVertecies] = normal_front;

            triangles[0 + offsetTriangles] = 0 + offsetVertecies;
            triangles[1 + offsetTriangles] = 1 + offsetVertecies;
            triangles[2 + offsetTriangles] = 2 + offsetVertecies;

            triangles[3 + offsetTriangles] = 2 + offsetVertecies;
            triangles[4 + offsetTriangles] = 1 + offsetVertecies;
            triangles[5 + offsetTriangles] = 3 + offsetVertecies;

            uv[0 + offsetUV] = new Vector2(0, 1);
            uv[1 + offsetUV] = new Vector2(1, 1);
            uv[2 + offsetUV] = new Vector2(0, 0);
            uv[3 + offsetUV] = new Vector2(1, 0);
        }
       


        //Left
        if (ShouldDrawFace(cubePosLocal, normal_left, isBorderCube))
        {
            offsetVertecies = NR_OF_VERTICES_PER_FACE * nrOfFaces;
            offsetUV = NR_OF_UV_PER_FACE * nrOfFaces;
            offsetTriangles = NR_OF_TRIANGLES_PER_FACE * nrOfFaces;
            nrOfFaces++;

            vertices[0 + offsetVertecies] = left_top_back;
            vertices[1 + offsetVertecies] = left_top_front;
            vertices[2 + offsetVertecies] = left_bottom_back;
            vertices[3 + offsetVertecies] = left_bottom_front;

            normals[0 + offsetVertecies] = normal_left;
            normals[1 + offsetVertecies] = normal_left;
            normals[2 + offsetVertecies] = normal_left;
            normals[3 + offsetVertecies] = normal_left;

            triangles[0 + offsetTriangles] = 0 + offsetVertecies;
            triangles[1 + offsetTriangles] = 1 + offsetVertecies;
            triangles[2 + offsetTriangles] = 2 + offsetVertecies;

            triangles[3 + offsetTriangles] = 2 + offsetVertecies;
            triangles[4 + offsetTriangles] = 1 + offsetVertecies;
            triangles[5 + offsetTriangles] = 3 + offsetVertecies;

            uv[0 + offsetUV] = new Vector2(0, 1);
            uv[1 + offsetUV] = new Vector2(1, 1);
            uv[2 + offsetUV] = new Vector2(0, 0);
            uv[3 + offsetUV] = new Vector2(1, 0);
        }


        //Right
        if (ShouldDrawFace(cubePosLocal, normal_right, isBorderCube))
        {
            offsetVertecies = NR_OF_VERTICES_PER_FACE * nrOfFaces;
            offsetUV = NR_OF_UV_PER_FACE * nrOfFaces;
            offsetTriangles = NR_OF_TRIANGLES_PER_FACE * nrOfFaces;
            nrOfFaces++;

            vertices[0 + offsetVertecies] = right_top_front;
            vertices[1 + offsetVertecies] = right_top_back;
            vertices[2 + offsetVertecies] = right_bottom_front;
            vertices[3 + offsetVertecies] = right_bottom_back;

            normals[0 + offsetVertecies] = normal_right;
            normals[1 + offsetVertecies] = normal_right;
            normals[2 + offsetVertecies] = normal_right;
            normals[3 + offsetVertecies] = normal_right;

            triangles[0 + offsetTriangles] = 0 + offsetVertecies;
            triangles[1 + offsetTriangles] = 1 + offsetVertecies;
            triangles[2 + offsetTriangles] = 2 + offsetVertecies;

            triangles[3 + offsetTriangles] = 2 + offsetVertecies;
            triangles[4 + offsetTriangles] = 1 + offsetVertecies;
            triangles[5 + offsetTriangles] = 3 + offsetVertecies;

            uv[0 + offsetUV] = new Vector2(0, 1);
            uv[1 + offsetUV] = new Vector2(1, 1);
            uv[2 + offsetUV] = new Vector2(0, 0);
            uv[3 + offsetUV] = new Vector2(1, 0);
        }


        //Top  
        if (ShouldDrawFace(cubePosLocal, normal_top, isBorderCube))
        {
            offsetVertecies = NR_OF_VERTICES_PER_FACE * nrOfFaces;
            offsetUV = NR_OF_UV_PER_FACE * nrOfFaces;
            offsetTriangles = NR_OF_TRIANGLES_PER_FACE * nrOfFaces;
            nrOfFaces++;

            vertices[0 + offsetVertecies] = left_top_back;
            vertices[1 + offsetVertecies] = right_top_back;
            vertices[2 + offsetVertecies] = left_top_front;
            vertices[3 + offsetVertecies] = right_top_front;

            normals[0 + offsetVertecies] = normal_top;
            normals[1 + offsetVertecies] = normal_top;
            normals[2 + offsetVertecies] = normal_top;
            normals[3 + offsetVertecies] = normal_top;


            triangles[0 + offsetTriangles] = 0 + offsetVertecies;
            triangles[1 + offsetTriangles] = 1 + offsetVertecies;
            triangles[2 + offsetTriangles] = 2 + offsetVertecies;

            triangles[3 + offsetTriangles] = 2 + offsetVertecies;
            triangles[4 + offsetTriangles] = 1 + offsetVertecies;
            triangles[5 + offsetTriangles] = 3 + offsetVertecies;

            uv[0 + offsetUV] = new Vector2(0, 1);
            uv[1 + offsetUV] = new Vector2(1, 1);
            uv[2 + offsetUV] = new Vector2(0, 0);
            uv[3 + offsetUV] = new Vector2(1, 0);
        }


        //Bottom
        if (ShouldDrawFace(cubePosLocal, normal_bottom, isBorderCube))
        {
            offsetVertecies = NR_OF_VERTICES_PER_FACE * nrOfFaces;
            offsetUV = NR_OF_UV_PER_FACE * nrOfFaces;
            offsetTriangles = NR_OF_TRIANGLES_PER_FACE * nrOfFaces;
            nrOfFaces++;

            vertices[0 + offsetVertecies] = left_bottom_front;
            vertices[1+ offsetVertecies] = right_bottom_front;
            vertices[2 + offsetVertecies] = left_bottom_back;
            vertices[3 + offsetVertecies] = right_bottom_back;

            normals[0 + offsetVertecies] = normal_bottom;
            normals[1 + offsetVertecies] = normal_bottom;
            normals[2 + offsetVertecies] = normal_bottom;
            normals[3 + offsetVertecies] = normal_bottom;

            triangles[0 + offsetTriangles] = 0 + offsetVertecies;
            triangles[1 + offsetTriangles] = 1 + offsetVertecies;
            triangles[2 + offsetTriangles] = 2 + offsetVertecies;

            triangles[3 + offsetTriangles] = 2 + offsetVertecies;
            triangles[4 + offsetTriangles] = 1 + offsetVertecies;
            triangles[5 + offsetTriangles] = 3 + offsetVertecies;

            uv[0 + offsetUV] = new Vector2(0, 1);
            uv[1 + offsetUV] = new Vector2(1, 1);
            uv[2 + offsetUV] = new Vector2(0, 0);
            uv[3 + offsetUV] = new Vector2(1, 0);
        }


        //Back
        if (ShouldDrawFace(cubePosLocal, normal_back, isBorderCube))
        {
            offsetVertecies = NR_OF_VERTICES_PER_FACE * nrOfFaces;
            offsetUV = NR_OF_UV_PER_FACE * nrOfFaces;
            offsetTriangles = NR_OF_TRIANGLES_PER_FACE * nrOfFaces;
            nrOfFaces++;

            vertices[0 + offsetVertecies] = right_top_back;
            vertices[1 + offsetVertecies] = left_top_back;
            vertices[2 + offsetVertecies] = right_bottom_back;
            vertices[3 + offsetVertecies] = left_bottom_back;

            normals[0 + offsetVertecies] = normal_back;
            normals[1 + offsetVertecies] = normal_back;
            normals[2 + offsetVertecies] = normal_back;
            normals[3 + offsetVertecies] = normal_back;

            triangles[0 + offsetTriangles] = 0 + offsetVertecies;
            triangles[1 + offsetTriangles] = 1 + offsetVertecies;
            triangles[2 + offsetTriangles] = 2 + offsetVertecies;

            triangles[3 + offsetTriangles] = 2 + offsetVertecies;
            triangles[4 + offsetTriangles] = 1 + offsetVertecies;
            triangles[5 + offsetTriangles] = 3 + offsetVertecies;

            uv[0 + offsetUV] = new Vector2(-1, 0);
            uv[1 + offsetUV] = new Vector2(0, 0);
            uv[2 + offsetUV] = new Vector2(-1, -1);
            uv[3 + offsetUV] = new Vector2(0, -1);
        }

        nrOfCubes++;
    }


    public void CreateMeshData() //Multi-threaded
    {
        st.Start();

        Vector3[] verticesBuffer = new Vector3[NR_OF_VERTICES_PER_CUBES * maxNrOfCubes];
        Vector3[] normalsBuffer = new Vector3[NR_OF_VERTICES_PER_CUBES * maxNrOfCubes];
        Vector2[] uvBuffer = new Vector2[NR_OF_UV_PER_CUBES * maxNrOfCubes];
        int[] trianglesBuffer = new int[NR_OF_TRIANGLES_PER_CUBES * maxNrOfCubes];

        Vector3 position = new Vector3(0, 0, 0);
        bool isBorderCube = false;
        int i = 0;
        for (int x = 0; x < chunkWidth; x++)
        {
            for (int y = 0; y < chunkHeight; y++)
            {
                for (int z = 0; z < chunkWidth; z++)
                {
                    if(chunkBlocks[x, y, z] == true)
                    {
                        position.x = x;
                        position.y = y;
                        position.z = z;

                        isBorderCube = (x == 0 || y == 0 || z == 0 || x == chunkWidth - 1 || y == chunkHeight - 1 || z == chunkWidth - 1);

                        CreateCube(i, verticesBuffer, normalsBuffer, uvBuffer, trianglesBuffer, position, isBorderCube);
                        i++;
                    }


                }
            }
        }

        vertices = new Vector3[4 * nrOfFaces];
        normals = new Vector3[4 * nrOfFaces];
        uv = new Vector2[4 * nrOfFaces];
        triangles = new int[6 * nrOfFaces];


        System.Array.Copy(verticesBuffer, vertices, vertices.Length);
        System.Array.Copy(normalsBuffer, normals, normals.Length );
        System.Array.Copy(uvBuffer, uv, uv.Length);
        System.Array.Copy(trianglesBuffer, triangles, triangles.Length);

       




        st.Stop();
        createTime = st.ElapsedMilliseconds;
        Debug.Log(string.Format("Creating chunk mesh data took {0} ms to complete", createTime));


    }

    public void ApplyMeshData()
    {



        mesh.SetVertices(vertices);
        mesh.uv = uv;
        mesh.SetTriangles(triangles, 0);
        mesh.SetNormals(normals);

        GetComponent<MeshFilter>().mesh = mesh;
        GetComponent<MeshRenderer>().material = testMaterial;

        GetComponent<MeshCollider>().sharedMesh = mesh;

        TerrainGenerator.Instance.nrOfCubes += nrOfCubes;
        TerrainGenerator.Instance.nrOfFaces += nrOfFaces;

        
    }
}
