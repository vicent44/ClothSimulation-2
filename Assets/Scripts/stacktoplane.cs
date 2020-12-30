using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class stacktoplane : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    void OnCollisionEnter(Collision col)
    {
        Debug.Log("printttt");
        if(col.gameObject.name == "stack")// && this.gameObject.name == "stack")
        {
            Debug.Log("printttt");
            var particle = col.gameObject.GetComponent<ParticlesBehaviour>();
            particle.particles.isActive = false;
        }
    }
}
