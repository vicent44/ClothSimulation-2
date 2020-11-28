using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RightFoldControl : MonoBehaviour
{

    private bool agentRightCatch;
    private bool agentRightDone;

    // Start is called before the first frame update
    void Start()
    {
        agentRightCatch = false;
        agentRightDone = false;
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }


    void OnTriggerStay(Collider col)
    {
        if(col.gameObject.tag == this.gameObject.tag)
        {
            //Ma esquerra agafan la roba
            Debug.Log("1-Right");
            transform.parent.GetComponent<AgentRobotHand>().ClothCathRight();
            agentRightCatch = true;
            //ClothCath(m_AgentLeft);
        }
        if(col.gameObject.name == this.gameObject.tag && agentRightCatch)
        {
            Debug.Log("2-Right");
            transform.parent.GetComponent<AgentRobotHand>().ClothFoldedRight();
            agentRightDone = true;
            //FoldedLeft();
        }
    }
    void OnTriggerExit(Collider col)
    {
        if(col.gameObject.tag == this.gameObject.tag)
        {
            Debug.Log("1.1-Right");
            transform.parent.GetComponent<AgentRobotHand>().ClothLostRight();
            //Ma esquerra deixa anar la roba
            agentRightCatch = false;
            //ClothLost(m_AgentLeft);
        }
        if(col.gameObject.name == this.gameObject.tag)
        {
            Debug.Log("2.1-Right");
            transform.parent.GetComponent<AgentRobotHand>().ClothLostFoldedRight();
            agentRightDone = false;
            //m_AgentLeft.AddReward(-0.1f);
        }
    }    
}
