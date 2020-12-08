using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using System.Linq;

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

    private bool leftDone;
    private bool rightDone;
    private bool leftCatch;
    private bool rightCatch;

    public enum Hand
    {
        Right,
        Left
    }
    public Hand hand;

    private GameObject left_hand;
    private GameObject right_hand;
    EnvironmentParameters m_ResetParams;

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

        var hit_right = Physics.OverlapBox(targetPos_right, new Vector3(0.02f, 0.02f, 0.02f));
        if(hit_right.Where(col => col.gameObject.CompareTag("plane")).ToArray().Length == 0)
        {
            right_hand.transform.position = targetPos_right;
            //Debug.Log("Next Position No Walls - Posible");
        }
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
        continuousActionsOut[0] = Input.GetAxis("Horizontal");    // Racket Movement
        continuousActionsOut[3] = Input.GetAxis("Horizontal"); 
        continuousActionsOut[2] = Input.GetAxis("Vertical");
        continuousActionsOut[5] = Input.GetAxis("Vertical");    
    }

    public override void OnEpisodeBegin()
    {
        //StartCoroutine(Example());
        arearobot.AreaReset();
        //arearobot.SetEnvironment();
        //System.Threading.Thread.Sleep(4000);
        left_hand = transform.Find("TargetLeft").gameObject;
        right_hand = transform.Find("TargetRight").gameObject;
    }

    public void FixedUpdate()
    {
        if(float.IsNaN(mesh.transform.GetChild(0).GetComponent<ParticlesBehaviour>().particles.Position.x))
        {
            Error();
        }        

        if(leftDone && rightDone) FoldCompleted();
        else if(leftDone && !rightDone) AddReward(-0.01f);
        else if(!leftDone && rightDone) AddReward(-0.01f);

        if(!leftCatch) AddReward(-0.01f);
        if(!rightCatch) AddReward(-0.01f);

        if(leftCatch && !leftDone) AddReward(-0.01f);
        if(rightCatch && !rightDone) AddReward(-0.01f);
        

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
        AddReward(-1f);
        EndEpisode();
        arearobot.AreaReset(); 
    }

    void FoldCompleted()
    {
        AddReward(1f);
        EndEpisode();
        arearobot.AreaReset();
    }

    public void ClothCathLeft()
    {
        AddReward(0.25f);
        leftCatch = true;
    }
    public void ClothCathRight()
    {
        AddReward(0.25f);
        rightCatch = true;
    }

    public void ClothLostLeft()
    {
        AddReward(-0.25f);
        leftCatch = false;
    }
    public void ClothLostRight()
    {
        AddReward(-0.25f);
        rightCatch = false;
    }

    public void ClothFoldedLeft()
    {
        AddReward(0.50f);
        leftDone = true;
    }
    public void ClothFoldedRight()
    {
        AddReward(0.50f);
        rightDone = true;
    }

    public void ClothLostFoldedLeft()
    {
        AddReward(-0.50f);
        leftDone = false;
    }
    public void ClothLostFoldedRight()
    {
        AddReward(-0.50f);
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
