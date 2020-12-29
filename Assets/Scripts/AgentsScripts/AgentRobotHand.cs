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

    /*public override void WriteDiscreteActionMask(IDiscreteActionMask actionMask)
    {
        var positionX = transform.position.x;
        var positionY = transform.position.y;
        var positionZ = transform.position.z;

        var minPositionX = -1.5f;
        var maxPositionX = 2.0f;
        var minPositionY = plane.transform.position.y + 0.02f;
        var maxPositionY = plane.transform.position.y + 0.02f + 1f;
        var minPositionZ = plane2.transform.position.z + 0.02f;
        var maxPositionZ = plane2.transform.position.z + 0.02f + 1.5f;

        if(minPositionX >= positionX)
        {
            actionMask.WriteMask(0, new[] {left});
        }
        if(maxPositionX <= positionX)
        {
            actionMask.WriteMask(0, new[] {right});
        }
        if(minPositionY >= positionY)
        {
            actionMask.WriteMask(0, new[] {down});
        }
        if(maxPositionY <= positionY)
        {
            actionMask.WriteMask(0, new[] {up});
        }
        if(minPositionZ >= positionZ)
        {
            actionMask.WriteMask(0, new[] {backward});
        }
        if(maxPositionZ <= positionZ)
        {
            actionMask.WriteMask(0, new[] {forward});
        }
    }
*/
    /*public override void Initialize()
    {
        m_ResetParams = Academy.Instance.EnvironmentParameters;
        left_hand = transform.Find("TargetLeft").gameObject;
        right_hand = transform.Find("TargetRight").gameObject;
        arearobot.SetEnvironment();
    }*/

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        //left_goal = mesh.transform.Find("left").gameObject;
        //right_goal = mesh.transform.Find("right").gameObject;
        AddReward(-1f/(5000f));
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
                
        
        /*var action = actionBuffers.DiscreteActions[0];

        var targetPos = transform.position;
        switch (action)
        {
            case noAction:
                //nothing
                break;
            case right:
                targetPos = transform.position + new Vector3(0.01f, 0, 0f);
                break;
            case left:
                targetPos = transform.position + new Vector3(-0.01f, 0, 0f);
                break;
            case up:
                targetPos = transform.position + new Vector3(0f, 0.01f, 0f);
                break;
            case down:
                targetPos = transform.position + new Vector3(0f, -0.01f, 0f);
                break;
            case forward:
                targetPos = transform.position + new Vector3(0f, 0, 0.01f);
                break;
            case backward:
                targetPos = transform.position + new Vector3(0f, 0, -0.01f);
                break;
            //default:
            //    Debug.Log("Invalid action value");
        }*/
        var hit_left = Physics.OverlapBox(targetPos_left, new Vector3(0.02f, 0.02f, 0.02f));
        if(hit_left.Where(col => col.gameObject.CompareTag("plane")).ToArray().Length == 0)
        {
            left_hand.transform.position = targetPos_left;
            //Debug.Log("Next Position No Walls - Posible");
        }
        else
        {
            //AddReward(-0.01f);
            //Debug.Log("-0.01");
            AddReward(-1f/(5000f));
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
            AddReward(-1f/(5000f));
        }

        if(float.IsNaN(mesh.transform.GetChild(0).GetComponent<ParticlesBehaviour>().particles.Position.x))
        {
            count += 1;
            Debug.Log("Error: "+count);
            Error();
        }

        if(leftDone && rightDone) FoldCompleted();

        if(leftCatch) ClothCathLeft();
        else ClothLostLeft();

        if(rightCatch) ClothCathRight();
        else ClothLostRight();

        //left_goal = mesh.transform.Find("left").gameObject;
        //right_goal = mesh.transform.Find("right").gameObject;
        //distance_left_after = Vector3.Distance(left_goal.transform.position, left_hand.transform.position);
        //distance_right_after = Vector3.Distance(right_goal.transform.position, right_hand.transform.position);
        //AddReward(-(Math.Abs(distance_left_after)+Math.Abs(distance_right_after))*0.1f);
        /*if(distance_left_after < distance_left_before)
        {
            AddReward(0.01f);
            Debug.Log("More near");
        }
        if(distance_right_after < distance_right_before)
        {
            AddReward(0.01f);
            Debug.Log("More near");
        }*/
    }

    /*void OnTriggerStay(Collider col)
    {
        //Debug.Log("Play");
        if(col.gameObject.tag == this.gameObject.tag)
        {
            Debug.Log("Collision with the first line of particles");
            firstCollisionDone = true;
            SetReward(0.01f);
        }
    }
    void OnTriggerExit(Collider col)
    {
        if(col.gameObject.tag == this.gameObject.tag)
        {
            Debug.Log("The particle is not in the Robot hand any more");
            firstCollisionDone = false;
            SetReward(-0.01f);
        }
    }*/

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActionsOut = actionsOut.ContinuousActions;
        continuousActionsOut[0] = Input.GetAxis("Horizontal")*1f;    // Racket Movement
        continuousActionsOut[3] = Input.GetAxis("Horizontal")*1f; 
        continuousActionsOut[2] = Input.GetAxis("Vertical")*1f;
        continuousActionsOut[5] = Input.GetAxis("Vertical")*1f;    
    }

    public override void OnEpisodeBegin()
    {
        count = 0;
        rew = 0;
        //StartCoroutine(Example());
        arearobot.AreaReset();
        //arearobot.SetEnvironment();
        //System.Threading.Thread.Sleep(4000);
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

        distance_left_hand = Vector3.Distance(left_start, left_hand.transform.position);

        //Right
        right_start = mesh.transform.GetChild(8).GetComponent<ParticlesBehaviour>().particles.Position;

        right_goal = mesh.transform.Find("left").GetComponent<BoxCollider>().transform.position;
        right_start_2 = mesh.transform.GetChild(8).GetComponent<ParticlesBehaviour>().particles.Position;
        distance_right_cloth = Vector3.Distance(right_goal, right_start_2);
        distance_right_cloth = Math.Abs(distance_right_cloth);

        distance_right_hand = Vector3.Distance(right_start, right_hand.transform.position);


        /*if(float.IsNaN(mesh.transform.GetChild(0).GetComponent<ParticlesBehaviour>().particles.Position.x))
        {
            count += 1;
            Debug.Log("Error: "+count);
            Error();
        }*/
        /*if(float.IsNaN(distance_left_hand))
        {
            Debug.Log("Error");
            Error();
        }    
        else if(float.IsNaN(distance_left_cloth))
        {
            Debug.Log("Error");
            Error();            
        }
        if(float.IsNaN(distance_right_hand))
        {
            Debug.Log("Error");
            Error();
        }    
        else if(float.IsNaN(distance_right_cloth))
        {
            Debug.Log("Error");
            Error();            
        }*/


        /*if(leftDone && rightDone) FoldCompleted();

        if(leftCatch) ClothCathLeft();
        else ClothLostLeft();

        if(rightCatch) ClothCathRight();
        else ClothLostRight();
        Debug.Log(rew);*/


        /*if(leftDone) ClothFoldedLeft();
        else ClothLostFoldedLeft();

        if(rightDone) ClothFoldedRight();
        else ClothLostFoldedRight();*/

        /*else if(leftDone && !rightDone) AddReward(-0.01f);
        else if(!leftDone && rightDone) AddReward(-0.01f);

        if(!leftCatch) AddReward(-0.01f);
        if(!rightCatch) AddReward(-0.01f);

        if(leftCatch && !leftDone) AddReward(-0.01f);
        if(rightCatch && !rightDone) AddReward(-0.01f);*/
        

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
        AddReward(-5f);
        rew -=100;
        EndEpisode();
        arearobot.AreaReset(); 
    }

    void FoldCompleted()
    {
        AddReward(2f);
        EndEpisode();
        arearobot.AreaReset();
    }

    public void ClothCathLeft()
    {
        //AddReward(25f);
        //Debug.Log("hehe +");
        left_goal = mesh.transform.Find("left").GetComponent<BoxCollider>().transform.position;
        left_start_2 = mesh.transform.GetChild(0).GetComponent<ParticlesBehaviour>().particles.Position;

        left_start_2_prev = mesh.transform.GetChild(0).GetComponent<ParticlesBehaviour>().particles.Prev;

        distance_left_cloth = Vector3.Distance(left_goal, left_hand.transform.position);
        //distance_left_cloth_prev = Vector3.Distance(left_goal, left_start_2_prev);

        if(float.IsNaN(distance_left_cloth))
        {
            count += 1;
            Debug.Log("Error: "+ count);
            distance_left_cloth = 1.2f;
            //Error();
        }
        //rew -=(distance_left_cloth)*0.01f;
        //Debug.Log((-(distance_left_cloth))*1f);
        AddReward((-(distance_left_cloth))*(1f/(1.2f*5000f)));
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

        //rew -=(distance_right_cloth)*0.01f;
        AddReward((-(distance_right_cloth))*(1f/(1.2f*5000f)));
        rightCatch = true;
        //Debug.Log("ei dins");
    }

    public void ClothLostLeft()
    {
        //AddReward(-25f);
        //Debug.Log("hehe -");
        //left_goal = mesh.transform.Find("left").GetComponent<BoxCollider>().transform.position;
        left_start = mesh.transform.GetChild(0).GetComponent<ParticlesBehaviour>().particles.Position;

        left_goal = mesh.transform.Find("left").GetComponent<BoxCollider>().transform.position;
        left_start_2 = mesh.transform.GetChild(0).GetComponent<ParticlesBehaviour>().particles.Position;
        distance_left_cloth = Vector3.Distance(left_goal, left_start_2);
        distance_left_cloth = Math.Abs(distance_left_cloth);

        distance_left_hand = Vector3.Distance(left_start, left_hand.transform.position);

        if(distance_left_hand > 0.3f)
        {
            Error();
        } 
            //Debug.Log("ei fora broo");
        leftCatch = false;
        /*if(float.IsNaN(distance_left_cloth))
        {
            count += 1;
            Debug.Log("Error: "+count);
            distance_left_cloth = 10000f;
            //Error();
        } 
        if(float.IsNaN(distance_left_hand))
        {
            count += 1;
            Debug.Log("Error: "+count);
            distance_left_hand = 10000f;
            //Error();
        }*/

        //Debug.Log(-(distance_left_hand+ distance_left_cloth)*0.01f);
        //Debug.Log("hi");
        //AddReward(-(distance_left_hand+ distance_left_cloth)*0.01f);
        //leftCatch = false;
        //Error();
    }
    public void ClothLostRight()
    {
        //AddReward(-25f);
        //Debug.Log("hehe -");
        //right_goal = mesh.transform.Find("left").GetComponent<BoxCollider>().transform.position;
        right_start = mesh.transform.GetChild(8).GetComponent<ParticlesBehaviour>().particles.Position;

        right_goal = mesh.transform.Find("left").GetComponent<BoxCollider>().transform.position;
        right_start_2 = mesh.transform.GetChild(8).GetComponent<ParticlesBehaviour>().particles.Position;
        distance_right_cloth = Vector3.Distance(right_goal, right_start_2);
        distance_right_cloth = Math.Abs(distance_right_cloth);

        distance_right_hand = Vector3.Distance(right_start, right_hand.transform.position);

        if(distance_right_hand > 0.3f)
        {
            Error();
        }
        rightCatch = false;
        /*if(float.IsNaN(distance_right_cloth))
        {
            count += 1;
            Debug.Log("Error: "+count);
            distance_right_cloth = 10000f;
            //Error();
        } 
        if(float.IsNaN(distance_right_hand))
        {
            count += 1;
            Debug.Log("Error: "+count);
            distance_right_hand = 10000f;
            //Error();
        } */


        //AddReward(-(distance_right_hand+ distance_right_cloth)*0.01f);
        //Error();
        //rightCatch = false;
    }

    public void ClothFoldedLeft()
    {
        AddReward(0.5f);
        leftDone = true;
    }
    public void ClothFoldedRight()
    {
        AddReward(0.5f);
        rightDone = true;
    }

    public void ClothLostFoldedLeft()
    {
        AddReward(-0.5f);
        leftDone = false;
    }
    public void ClothLostFoldedRight()
    {
        AddReward(-0.5f);
        rightDone = false;
    }



/*
    // Start is called before the first frame update
    void Start()
    {
        rBody = GetComponent<Rigidbody>();
        //Initialize();
    }

    void Initialize()
    {
        GameObject rightFin = GameObject.Find("TrainEnvironment/MeshGenerator/rightLast");
        Rigidbody rBodyRightFin = rightFin.GetComponent<Rigidbody>();
        GameObject leftFin = GameObject.Find("TrainEnvironment/MeshGenerator/leftLast");
        Rigidbody rBodyLeftFin = leftFin.GetComponent<Rigidbody>();
    }

    public override void OnEpisodeBegin()
    {
        GameObject rightFin = GameObject.Find("TrainEnvironment/MeshGenerator/rightLast");
        Rigidbody rBodyRightFin = rightFin.GetComponent<Rigidbody>();
        GameObject leftFin = GameObject.Find("TrainEnvironment/MeshGenerator/leftLast");
        Rigidbody rBodyLeftFin = leftFin.GetComponent<Rigidbody>();
        //mesh.GetComponent<MeshGenerator>().Restart();
        if(this.gameObject.tag == "right")
        {
            mesh.GetComponent<MeshGenerator>().Restart();
            this.rBody.velocity = Vector3.zero;
            this.transform.localPosition = new Vector3(0.85f, 0f, -0.1f);
            //mesh.GetComponent<MeshGenerator>().Restart();
            //rightFin.transform.localPosition =
            //rBodyRightFin.velocity = Vector3.zero;
        }
        if(this.gameObject.tag == "left")
        {
            //mesh.GetComponent<MeshGenerator>().Restart();
            this.rBody.velocity = Vector3.zero;
            this.transform.localPosition = new Vector3(-0.05f, 0f, -0.1f);
            //mesh.GetComponent<MeshGenerator>().Restart();
            //leftFin.transform.localPosition =
            //rBodyLeftFin.velocity = Vector3.zero;
        }
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        if(this.gameObject.tag == "right")
        {
            sensor.AddObservation(this.transform.localPosition);
            sensor.AddObservation(this.rBody.velocity);
            sensor.AddObservation(rightFin.transform.localPosition);
            sensor.AddObservation(rBodyRightFin.velocity);
        }
        if(this.gameObject.tag == "left")
        {
            sensor.AddObservation(this.transform.localPosition);
            sensor.AddObservation(this.rBody.velocity);
            sensor.AddObservation(leftFin.transform.localPosition);
            sensor.AddObservation(rBodyLeftFin.velocity);
        }
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        //MoveAgent(actionBuffers.ContinuousActions);
        var continuousActions = actionBuffers.ContinuousActions;
        var moveX = Mathf.Clamp(continuousActions[0], -1f, 1f);
        var moveY = Mathf.Clamp(continuousActions[1], -1f, 1f);
        var moveZ = Mathf.Clamp(continuousActions[2], -1f, 1f);

        rBody.AddForce(10f * moveX, 10f * moveY, 10f * moveZ);

        if(this.transform.localPosition.y < (plane.transform.position.y + 0.02f))
        {
            SetReward(-1f);
            EndEpisode();
        }

        if(this.gameObject.tag == "right")
        {
            float distanceToTargetright = Vector3.Distance(this.transform.localPosition, rightFin.transform.localPosition);
        
            if(distanceToTargetright < 0.1f)
            {
                SetReward(1.0f);
                EndEpisode();
            }
            else if(this.transform.localPosition.y < (plane.transform.localPosition.y + 0.2f))
            {
                EndEpisode();
            }
        }
        if(this.gameObject.tag == "left")
        {
            float distanceToTargetleft = Vector3.Distance(this.transform.localPosition, leftFin.transform.localPosition);
        
            if(distanceToTargetleft < 0.1f)
            {
                SetReward(1.0f);
                EndEpisode();
            }
            else if(this.transform.localPosition.y < (plane.transform.localPosition.y + 0.2f))
            {
                EndEpisode();
            }
        }
    }

    public void MoveAgent(ActionSegment<int> act)
    {
        int directionX = 0;
        int directionY = 0;
        int directionZ = 0;
        int movement = Mathf.FloorToInt(act[0]);

        if (movement == 1) { directionX = -1; }
        if (movement == 2) { directionX = 1; }
        if (movement == 3) { directionY = -1; }
        if (movement == 4) { directionY = 1; }
        if (movement == 5) { directionZ = -1; }
        if (movement == 6) { directionZ = 1; }

        rBody.AddForce(10f * directionX, 10f * directionY, 10f * directionZ);

    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discreteActionsOut = actionsOut.DiscreteActions;
        discreteActionsOut.Clear();
        //forward
        if (Input.GetKeyDown("w"))
        {
            discreteActionsOut[0] = 1;
        }
        if (Input.GetKey(KeyCode.S))
        {
            discreteActionsOut[0] = 2;
        }
        //rotate
        if (Input.GetKey(KeyCode.A))
        {
            discreteActionsOut[2] = 1;
        }
        if (Input.GetKey(KeyCode.D))
        {
            discreteActionsOut[2] = 2;
        }
        //right
        if (Input.GetKey(KeyCode.E))
        {
            discreteActionsOut[1] = 1;
        }
        if (Input.GetKey(KeyCode.Q))
        {
            discreteActionsOut[1] = 2;
        }
    }*/
}
