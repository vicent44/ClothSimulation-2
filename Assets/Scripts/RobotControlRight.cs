using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RobotControlRight : MonoBehaviour
{
    private ParticlesBehaviour particle = null;
    private bool detectedBefore = false;
    private int particleNum;
    Collision collision = null;
    private bool anchor = false;
    public Transform guide;
    private Vector3 collisionposition;

    public MeshGenerator mesh;

    void OnCollisionEnter(Collision col)
    {
        if(col.gameObject.name == "New Particle")
        {
            if(!detectedBefore)
            {
                this.collision = col;
                var particle = col.gameObject.GetComponent<ParticlesBehaviour>();
                particle.particles.isActive = false;
                detectedBefore = true;
                anchor = true;
                particleNum = particle.particles.I;
                //col.transform.parent = col.contacts[0].thisCollider.transform;
                collisionposition = col.contacts[0].normal;
            }
            
        }
    }

    void OnCollisionStay(Collision col)
    {
        if(col.gameObject.name == "New Particle")
        {
            if(anchor && detectedBefore && particleNum == col.gameObject.GetComponent<ParticlesBehaviour>().particles.I)
            {
                this.collision = col;
                var particle = col.gameObject.GetComponent<ParticlesBehaviour>();
                particle.particles.SetPosition(guide.position);
            }
        }        
    }

    void OnCollisionExit(Collision col)
    {
        if(col.gameObject.name == "New Particle")
        {
            if(detectedBefore)
            {
                this.collision = col;
                var particle = col.gameObject.GetComponent<ParticlesBehaviour>();
                particle.particles.isActive = true;
                //detectedBefore = false;
            }
        }
    }

    void Update()
    {
        //particle.transform.position = guide.position;
        if(Input.GetKey("r"))
        {
            if(detectedBefore){
                Debug.Log("Unanchor the particle from the robot hand");
                anchor = false;
                //detectedBefore = false;
                collision.gameObject.GetComponent<ParticlesBehaviour>().particles.isActive = true;
                collision.gameObject.GetComponent<ParticlesBehaviour>().particles.SetPosition(collision.contacts[0].point);
                //collision.contacts[0].thisCollider.transform.DetachChildren();
            }
        }
        if(Input.GetKey("f"))
        {
            detectedBefore = false;
            Debug.Log("Now you can select another particle to move");
        }
    }
    void FixedUpdate()
    {
        mesh.transform.GetChild(8).gameObject.GetComponent<ParticlesBehaviour>().particles.SetPosition(guide.position);
    }
}
