using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using System.Linq;
using System;

public class AgentRobotHand : Agent
{
    public GameObject plane;
    public GameObject plane2;
    public GameObject mesh;

    public AreaRobot arearobot;
    public float timeBetweenDecisionsAtInference;
    float m_TimeSinceDecision;

    private GameObject leftFin;
    private GameObject rightFin;
    private Rigidbody rBody;
    private Rigidbody rBodyLeftFin;
    private Rigidbody rBodyRightFin;
    private bool firstCollisionDone = false;

    const int noAction = 0;
    const int up = 1;
    const int down = 2;
    const int left = 3;
    const int right = 4;
    const int forward = 5;
    const int backward = 6;

    public bool leftDone;
    public bool rightDone;
    public bool leftCatch;
    public bool rightCatch;

    public enum Hand
    {
        Right,
        Left
    }
    public Hand hand;

    private GameObject left_hand;
    private GameObject right_hand;
    EnvironmentParameters m_ResetParams;

    private Vector3 left_goal;
    private Vector3 right_goal;
    private Vector3 left_start;
    private Vector3 right_start;
    private Vector3 left_start_2;
    private Vector3 right_start_2;    
    private Vector3 left_start_2_prev;
    private Vector3 right_start_2_prev;  

    private float distance_left_hand;
    private float distance_right_hand;
    private float distance_left_cloth;
    private float distance_right_cloth;
    private float distance_left_cloth_prev;
    private float distance_right_cloth_prev;

    private int count;
    private float rew;


    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        //left_goal = mesh.transform.Find("left").gameObject;
        //right_goal = mesh.transform.Find("right").gameObject;
        AddReward(-1f/(500f));
        //distance_left_before = Vector3.Distance(left_goal.transform.position, left_hand.transform.position);
        //distance_right_before = Vector3.Distance(right_goal.transform.position, right_hand.transform.position);

        var continuousActions = actionBuffers.ContinuousActions;
        var moveX_left = Mathf.Clamp(continuousActions[0], -1f, 1f);
        var moveY_left = Mathf.Clamp(continuousActions[1], -1f, 1f);
        var moveZ_left = Mathf.Clamp(continuousActions[2], -1f, 1f);
        //Debug.Log(moveZ);
        var targetPos_left = left_hand.transform.position;
        targetPos_left = left_hand.transform.position + new Vector3(moveX_left*0.001f, moveY_left*0.001f, moveZ_left*0.001f);
        
        var moveX_right = Mathf.Clamp(continuousActions[3], -1f, 1f);
        var moveY_right = Mathf.Clamp(continuousActions[4], -1f, 1f);
        var moveZ_right = Mathf.Clamp(continuousActions[5], -1f, 1f);
        //Debug.Log(moveZ);
        var targetPos_right = right_hand.transform.position;
        targetPos_right = right_hand.transform.position + new Vector3(moveX_right*0.001f, moveY_right*0.001f, moveZ_right*0.001f);


        var hit_left = Physics.OverlapBox(targetPos_left, new Vector3(0.02f, 0.02f, 0.02f));
        if(hit_left.Where(col => col.gameObject.CompareTag("plane")).ToArray().Length == 0)
        {
            left_hand.transform.position = targetPos_left;
            //Debug.Log("Next Position No Walls - Posible");
        }
        else
        {
            //AddReward(-0.01f);
            AddReward(-1f/(500f));
        }

        var hit_right = Physics.OverlapBox(targetPos_right, new Vector3(0.02f, 0.02f, 0.02f));
        if(hit_right.Where(col => col.gameObject.CompareTag("plane")).ToArray().Length == 0)
        {
            right_hand.transform.position = targetPos_right;
            //Debug.Log("Next Position No Walls - Posible");
        }
        else
        {
            //AddReward(-0.01f);
            AddReward(-1f/(500f));
        }

        if(float.IsNaN(mesh.transform.GetChild(2).GetComponent<ParticlesBehaviour>().particles.Position.x))
        {
            count += 1;
            Debug.Log("Error: "+count);
            Error();
        }

        if(leftDone && rightDone) FoldCompleted();

        if(leftCatch && !leftDone) ClothCathLeft();
        //else if (!leftCatch && !leftDone) ClothLostLeft();
        //else if (leftCatch && leftDone)  

        if(rightCatch && !rightDone) ClothCathRight();
        //else ClothLostRight();

    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActionsOut = actionsOut.ContinuousActions;
        continuousActionsOut[0] = Input.GetAxis("Horizontal")*1f; 
        continuousActionsOut[3] = Input.GetAxis("Horizontal")*1f; 
        continuousActionsOut[2] = Input.GetAxis("Vertical")*1f;
        continuousActionsOut[5] = Input.GetAxis("Vertical")*1f;    
    }

    public override void OnEpisodeBegin()
    {
        count = 0;
        rew = 0;
        arearobot.AreaReset();
        //arearobot.SetEnvironment();
        left_hand = transform.Find("TargetLeft").gameObject;
        right_hand = transform.Find("TargetRight").gameObject;

        leftCatch = true;
        rightCatch = true;
        leftDone = false;
        rightDone = false;
    }

    public void FixedUpdate()
    {
        //Left
        left_start = mesh.transform.GetChild(0).GetComponent<ParticlesBehaviour>().particles.Position;

        left_goal = mesh.transform.Find("left").GetComponent<BoxCollider>().transform.position;
        left_start_2 = mesh.transform.GetChild(0).GetComponent<ParticlesBehaviour>().particles.Position;
        distance_left_cloth = Vector3.Distance(left_goal, left_start_2);
        distance_left_cloth = Math.Abs(distance_left_cloth);

        //Right
        right_start = mesh.transform.GetChild(8).GetComponent<ParticlesBehaviour>().particles.Position;

        right_goal = mesh.transform.Find("left").GetComponent<BoxCollider>().transform.position;
        right_start_2 = mesh.transform.GetChild(8).GetComponent<ParticlesBehaviour>().particles.Position;
        distance_right_cloth = Vector3.Distance(right_goal, right_start_2);
        distance_right_cloth = Math.Abs(distance_right_cloth);        

        WaitTimeInference();
    }

    void WaitTimeInference()
    {
        if(Academy.Instance.IsCommunicatorOn)
        {
            RequestDecision();
        }
        else
        {
            if (m_TimeSinceDecision >= timeBetweenDecisionsAtInference)
            {
                m_TimeSinceDecision = 0f;
                RequestDecision();
            }
            else
            {
                m_TimeSinceDecision += Time.fixedDeltaTime;
            }
        }
    }

    public void Error()
    {
        //AddReward(-100f);
        AddReward(-10f);
        EndEpisode();
        arearobot.AreaReset(); 
    }

    void FoldCompleted()
    {
        //AddReward(2f);
        AddReward(100f/500f);
        EndEpisode();
        arearobot.AreaReset();
    }

    public void ClothCathLeft()
    {
        //AddReward(25f);
        left_goal = mesh.transform.Find("left").GetComponent<BoxCollider>().transform.position;
        left_start_2 = mesh.transform.GetChild(0).GetComponent<ParticlesBehaviour>().particles.Position;

        left_start_2_prev = mesh.transform.GetChild(0).GetComponent<ParticlesBehaviour>().particles.Prev;

        distance_left_cloth = Vector3.Distance(left_goal, left_hand.transform.position);

        if(float.IsNaN(distance_left_cloth))
        {
            count += 1;
            Debug.Log("Error: "+ count);
            distance_left_cloth = 1.2f;
            //Error();
        }
        Debug.Log((-(distance_left_cloth))*1f);
        AddReward((-(distance_left_cloth))*(1f/(1.2f*50f)));
        leftCatch = true;
    }
    public void ClothCathRight()
    {
        //AddReward(25f);
        right_goal = mesh.transform.Find("left").GetComponent<BoxCollider>().transform.position;
        right_start_2 = mesh.transform.GetChild(8).GetComponent<ParticlesBehaviour>().particles.Position;

        right_start_2_prev = mesh.transform.GetChild(8).GetComponent<ParticlesBehaviour>().particles.Prev;

        distance_right_cloth = Vector3.Distance(right_goal, right_hand.transform.position);

        if(float.IsNaN(distance_right_cloth))
        {
            count += 1;
            Debug.Log("Error: "+count);
            distance_right_cloth = 1.2f;
            //Error();
        }

        AddReward((-(distance_right_cloth))*(1f/(1.2f*50f)));
        rightCatch = true;
    }

    public void ClothLostLeft()
    {
        left_start = mesh.transform.GetChild(0).GetComponent<ParticlesBehaviour>().particles.Position;

        left_goal = mesh.transform.Find("left").GetComponent<BoxCollider>().transform.position;
        left_start_2 = mesh.transform.GetChild(0).GetComponent<ParticlesBehaviour>().particles.Position;
        distance_left_cloth = Vector3.Distance(left_goal, left_start_2);
        distance_left_cloth = Math.Abs(distance_left_cloth);

        distance_left_hand = Vector3.Distance(left_start, left_hand.transform.position);

        leftCatch = false;
    }
    public void ClothLostRight()
    {
        right_start = mesh.transform.GetChild(8).GetComponent<ParticlesBehaviour>().particles.Position;

        right_goal = mesh.transform.Find("left").GetComponent<BoxCollider>().transform.position;
        right_start_2 = mesh.transform.GetChild(8).GetComponent<ParticlesBehaviour>().particles.Position;
        distance_right_cloth = Vector3.Distance(right_goal, right_start_2);
        distance_right_cloth = Math.Abs(distance_right_cloth);

        distance_right_hand = Vector3.Distance(right_start, right_hand.transform.position);

        rightCatch = false;
    }

    public void ClothFoldedLeft()
    {
        AddReward(25f/500f);
        //AddReward(1f);
        Debug.Log("Leftfol");
        leftDone = true;
    }
    public void ClothFoldedRight()
    {
        AddReward(25f/500f);
        //AddReward(1f);
        rightDone = true;
    }

    public void ClothLostFoldedLeft()
    {
        AddReward(-25f/500f);
        //AddReward(-1f);
        leftDone = false;
    }
    public void ClothLostFoldedRight()
    {
        AddReward(-25f/500f);
        //AddReward(-1f);
        rightDone = false;
    }
}
