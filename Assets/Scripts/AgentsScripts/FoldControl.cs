using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FoldControl : MonoBehaviour
{

    public GameObject trainEnvironment;

    AreaRobot m_Area;
    AgentRobotHand m_AgentLeft;
    AgentRobotHand m_AgentRight;

    public bool agentLeftDone;
    public bool agentRightDone;
    public bool agentLeftCatch;
    public bool agentRightCatch;

    void Start()
    {
        agentLeftDone = false;
        agentRightDone = false;
        agentLeftCatch = false; 
        agentRightCatch = false;

        //var trainEnvironment = transform.Find("TrainEnvironment").gameObject;
        //var trainEnvironment = this.transform.parent.gameObject.transform.parent.gameObject;
        m_Area = trainEnvironment.GetComponent<AreaRobot>();
        m_AgentLeft = m_Area.agentLeft.GetComponent<AgentRobotHand>();
        m_AgentRight = m_Area.agentRight.GetComponent<AgentRobotHand>();
    }

    void Reset()
    {
        m_AgentLeft.EndEpisode();
        m_AgentRight.EndEpisode();
        m_Area.AreaReset();
        agentLeftDone = false;
        agentRightDone = false;
        agentLeftCatch = false; 
        agentRightCatch = false;
    }

    void FoldCompleted()
    {
        m_AgentLeft.AddReward(1f);
        m_AgentRight.AddReward(1f);
        Reset();
    }

    void ClothCath(AgentRobotHand agent)
    {
        agent.AddReward(0.25f);
    }

    void ClothLost(AgentRobotHand agent)
    {
        agent.AddReward(-0.25f);
    }

    void FoldedRight()
    {
        m_AgentRight.AddReward(0.25f);
        m_AgentLeft.AddReward(-0.25f);
        //agentRightDone = true;
    }

    void FoldedLeft()
    {
        m_AgentLeft.AddReward(0.25f);
        m_AgentRight.AddReward(-0.25f);
        //agentLeftDone = true;
    }

    void FixedUpdate()
    {
        //FoldCompleted();
    }

    void OnTriggerStay(Collider col)
    {
        if(col.gameObject.tag == m_AgentLeft.gameObject.tag)
        {
            //Ma esquerra agafan la roba
            Debug.Log("1");
            agentLeftCatch = true;
            ClothCath(m_AgentLeft);
        }
        else if(col.gameObject.tag == m_AgentRight.gameObject.tag)
        {
            //Ma dreta agafan la roba
            Debug.Log("2");
            agentRightCatch = true;
            ClothCath(m_AgentRight);
        }
        if(col.gameObject.name == m_AgentLeft.gameObject.tag && agentLeftCatch)
        {
            Debug.Log("3");
            agentLeftDone = true;
            FoldedLeft();
        }
        else if(col.gameObject.name == m_AgentRight.gameObject.tag && agentRightCatch)
        {
            Debug.Log("4");
            agentRightDone = true;
            FoldedRight();
        }
        if((agentRightDone && m_Area.agentLeft.GetComponent<FoldControl>().agentLeftDone) || (agentLeftDone && m_Area.agentRight.GetComponent<FoldControl>().agentRightDone))
        {
            FoldCompleted();
        }
    }
    void OnTriggerExit(Collider col)
    {
        if(col.gameObject.tag == m_AgentLeft.gameObject.tag)
        {
            Debug.Log("1.1");
            //Ma esquerra deixa anar la roba
            agentLeftCatch = false;
            ClothLost(m_AgentLeft);
        }
        else if(col.gameObject.tag == m_AgentRight.gameObject.tag)
        {
            Debug.Log("2.1");
            //Ma dreta deixa anar la roba
            agentRightCatch = false;
            ClothLost(m_AgentRight);
        }
        if(col.gameObject.name == m_AgentLeft.gameObject.tag)
        {
            Debug.Log("3.1");
            agentLeftDone = false;
            m_AgentLeft.AddReward(-0.1f);
        }
        else if(col.gameObject.name == m_AgentRight.gameObject.tag)
        {
            Debug.Log("4.1");
            agentRightDone = false;
            m_AgentRight.AddReward(-0.1f);
        }
    } 
}
