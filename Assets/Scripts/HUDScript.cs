using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class HUDScript : MonoBehaviour
{

    public static HUDScript Instance { get; private set; }

    public Text fpsCounterText;
    public Text maxFpsText;
    public Text minFpsText;
    public Text nrOfChunksText;
    public Text nrOfCubesText; 
    public Text averageChunkCreationTimeText;
    private int fpsCounter;
    private int maxFps;
    private int minFps;
    public int nrOfChunks;
    public int nrOfLoadedChunks;
    public int nrOfCubes;
    public float averageChunkCreationTime;

    public float delayStartTime;


    public int count;
    public int samples = 100;
    public float totalTime;

    void Awake()
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // Start is called before the first frame update
    void Start()
    {
        maxFps = 0;
        minFps = 10000;
    }

    // Update is called once per frame
    void Update()
    {
        count -= 1;
        totalTime += Time.deltaTime;

        if (count <= 0)
        {
            float fps = samples / totalTime;
            fpsCounterText.text = "FPS: " + fps;
            totalTime = 0f;
            count = samples;
        }

        fpsCounter = (int)(1f/ Time.deltaTime);
       // fpsCounterText.text = "FPS: " + fpsCounter;

        if(fpsCounter > maxFps)
        {
            maxFps = fpsCounter;
            maxFpsText.text = "Max FPS: " + maxFps;
        }

        if (fpsCounter < minFps && Time.unscaledTime > delayStartTime)
        {
            minFps = fpsCounter;
            minFpsText.text = "Min FPS: " + minFps;
        }

        
        nrOfChunksText.text = "#Chunks: " + nrOfLoadedChunks + "/" + nrOfChunks;
        nrOfCubesText.text = "#Cubes: " + nrOfCubes;
        averageChunkCreationTimeText.text = "AVG chunk ms: " + averageChunkCreationTime;

    }
}
