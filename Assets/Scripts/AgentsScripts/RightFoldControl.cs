using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RightFoldControl : MonoBehaviour
{

    private bool agentRightCatch;
    private bool agentRightDone;
    private float val;

    // Start is called before the first frame update
    void Start()
    {
        agentRightCatch = false;
        agentRightDone = false;
        val = 0f;
        
    }

    // Update is called once per frame
    void Update()
    {
        Debug.Log("Right: "+val);
    }


    void OnTriggerStay(Collider col)
    {
        if(col.gameObject.tag == this.gameObject.tag && !agentRightCatch)
        {
            //Ma esquerra agafan la roba
            Debug.Log("1-Right");
            transform.parent.GetComponent<AgentRobotHand>().ClothCathRight();
            agentRightCatch = true;
            val += 0.25f;
            //ClothCath(m_AgentLeft);
        }
        if(col.gameObject.name == this.gameObject.tag && agentRightCatch && !agentRightDone)
        {
            Debug.Log("2-Right");
            transform.parent.GetComponent<AgentRobotHand>().ClothFoldedRight();
            agentRightDone = true;
            val += 0.5f;
            //FoldedLeft();
        }
    }
    void OnTriggerExit(Collider col)
    {
        if(col.gameObject.name == this.gameObject.tag && agentRightCatch && agentRightDone)
        {
            Debug.Log("2.1-Right");
            transform.parent.GetComponent<AgentRobotHand>().ClothLostFoldedRight();
            agentRightDone = false;
            val -= 0.5f;
            //m_AgentLeft.AddReward(-0.1f);
        }        
        if(col.gameObject.tag == this.gameObject.tag && agentRightCatch)
        {
            Debug.Log("1.1-Right");
            transform.parent.GetComponent<AgentRobotHand>().ClothLostRight();
            //Ma esquerra deixa anar la roba
            agentRightCatch = false;
            val -= 0.25f;
            //ClothLost(m_AgentLeft);
        }

    }    
}
