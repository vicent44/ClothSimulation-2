using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class LeftFoldControl : MonoBehaviour
{
    //public GameObject mesh;
    private bool agentLeftCatch;
    private bool agentLeftDone;
    private float val;

/*    private GameObject left_goal;
    private GameObject right_goal;

    private float distance_left_before;
    private float distance_right_before;
    private float distance_left_after;
    private float distance_right_after;
    private Vector3 past;*/

    // Start is called before the first frame update
    void Start()
    {
        agentLeftCatch = false;
        agentLeftDone = false;
        val = 0f;
    }

    // Update is called once per frame
    void Update()
    {
        Debug.Log("Left: "+val);

        /*left_goal = mesh.transform.Find("left").gameObject;
        Debug.Log(left_goal.transform.position);

        distance_left_before = Vector3.Distance(left_goal.transform.position, past);

        //left_goal = mesh.transform.Find("left").gameObject;
        distance_left_after = Vector3.Distance(left_goal.transform.position, this.transform.position);
        
        //Debug.Log(distance_left_after-distance_left_before);
        if(Math.Abs(distance_left_after) < Math.Abs(distance_left_before))
        {
            //AddReward(1f);
            Debug.Log("More near");
        }
        past = this.transform.position;*/

    }


    void OnTriggerStay(Collider col)
    {
        /*if(float.IsNaN(col.gameObject.GetComponent<ParticlesBehaviour>().particles.Position.x))
        {
            transform.parent.GetComponent<AgentRobotHand>().Error();
        }*/
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
