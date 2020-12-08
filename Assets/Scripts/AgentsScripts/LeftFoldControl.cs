using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LeftFoldControl : MonoBehaviour
{

    private bool agentLeftCatch;
    private bool agentLeftDone;

    // Start is called before the first frame update
    void Start()
    {
        agentLeftCatch = false;
        agentLeftDone = false;
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }


    void OnTriggerStay(Collider col)
    {
        /*if(float.IsNaN(col.gameObject.GetComponent<ParticlesBehaviour>().particles.Position.x))
        {
            transform.parent.GetComponent<AgentRobotHand>().Error();
        }*/
        if(col.gameObject.tag == this.gameObject.tag)
        {
            //Ma esquerra agafan la roba
            Debug.Log("1-Left");
            transform.parent.GetComponent<AgentRobotHand>().ClothCathLeft();
            agentLeftCatch = true;
            //ClothCath(m_AgentLeft);
        }
        if(col.gameObject.name == this.gameObject.tag && agentLeftCatch)
        {
            Debug.Log("2-Left");
            transform.parent.GetComponent<AgentRobotHand>().ClothFoldedLeft();
            agentLeftDone = true;
            //FoldedLeft();
        }
    }
    void OnTriggerExit(Collider col)
    {
        if(col.gameObject.tag == this.gameObject.tag)
        {
            Debug.Log("1.1-Left");
            transform.parent.GetComponent<AgentRobotHand>().ClothLostLeft();
            //Ma esquerra deixa anar la roba
            agentLeftCatch = false;
            //ClothLost(m_AgentLeft);
        }
        if(col.gameObject.name == this.gameObject.tag)
        {
            Debug.Log("2.1-Left");
            transform.parent.GetComponent<AgentRobotHand>().ClothLostFoldedLeft();
            agentLeftDone = false;
            //m_AgentLeft.AddReward(-0.1f);
        }
    }     
}
