using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class LeftFoldControl : MonoBehaviour
{
    public GameObject mesh;
    private bool agentLeftCatch;
    private bool agentLeftDone;
    private float val;
    private ParticlesBehaviour particleleft;


    // Start is called before the first frame update
    void Start()
    {
        agentLeftCatch = false;
        agentLeftDone = false;
    }

    // Update is called once per frame
    void Update()
    {
        //Debug.Log("Left: "+val);
    }


    void OnTriggerStay(Collider col)
    {
        if(col.gameObject.tag == this.gameObject.tag && !agentLeftCatch)
        {
            //Ma esquerra agafan la roba
            Debug.Log("1-Left");
            transform.parent.GetComponent<AgentRobotHand>().ClothCathLeft();
            agentLeftCatch = true;
            val += 0.25f;
            //ClothCath(m_AgentLeft);
        }
        if(col.gameObject.name == this.gameObject.tag && agentLeftCatch && !agentLeftDone)
        {
            Debug.Log("2-Left");
            transform.parent.GetComponent<AgentRobotHand>().ClothFoldedLeft();
            agentLeftDone = true;
            val += 0.5f;
            //FoldedLeft();
        }
    }
    void OnTriggerExit(Collider col)
    {
        if(col.gameObject.name == this.gameObject.tag && agentLeftCatch && agentLeftDone)
        {
            Debug.Log("2.1-Left");
            transform.parent.GetComponent<AgentRobotHand>().ClothLostFoldedLeft();
            agentLeftDone = false;
            val -= 0.5f;
            //m_AgentLeft.AddReward(-0.1f);
        }        
        if(col.gameObject.tag == this.gameObject.tag && agentLeftCatch)
        {
            Debug.Log("1.1-Left");
            transform.parent.GetComponent<AgentRobotHand>().ClothLostLeft();
            //Ma esquerra deixa anar la roba
            agentLeftCatch = false;
            val -= 0.25f;
            //ClothLost(m_AgentLeft);
        }
    }     
}
