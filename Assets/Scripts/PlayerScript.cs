using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerScript : MonoBehaviour
{
   public GameObject objectToSpawn;
   public float cooldown = 1.0f;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        cooldown -= Time.deltaTime;
        if (Input.GetKeyDown(KeyCode.Space) && cooldown <= 0.0f)
        {
            GameObject spawnedObject = Instantiate<GameObject>(objectToSpawn);
            spawnedObject.transform.position = transform.position;
        }
    }
}
