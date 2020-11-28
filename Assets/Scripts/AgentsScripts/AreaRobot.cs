using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AreaRobot : MonoBehaviour
{
    public GameObject handRight;
    public GameObject handLeft;
    public GameObject agentHand;
    public GameObject cloth;
    public MeshGenerator mesh;

   //EnvironmentParameters  resetparameters;

    Camera agentCam;

    GameObject robot;
    GameObject plane;
    GameObject plane2;

    Vector3 robotInitial;
    Vector3 planeInitial;
    Vector3 plane2Initial;
    Vector3 clothInitial;
    Vector3 rightInitial;
    Vector3 leftInitial;
    Vector3 agentCamInitial;
    Vector3 agentHandInitial;

    Vector3 initialPosition;
    List<GameObject> clothPosition;



    void Start()
    {
        //resetparameters = Academy.Instance.EnvironmentParameters;
        clothPosition = new List<GameObject>();

        agentCam = transform.Find("agentCam").GetComponent<Camera>();
        agentCamInitial = agentCam.transform.position;

        robot = transform.Find("baxter").gameObject;
        plane = transform.Find("Plane").gameObject;
        plane2 = transform.Find("PlaneRobot").gameObject;

        robotInitial = robot.transform.position;
        planeInitial = plane.transform.position;
        plane2Initial = plane2.transform.position;
        clothInitial = cloth.transform.position;
        initialPosition = transform.position;

        agentHandInitial = agentHand.transform.position;
        rightInitial = handRight.transform.position;
        leftInitial = handLeft.transform.position;
    }

    public void SetEnvironment()
    {
        transform.position = initialPosition;
        robot.transform.position = robotInitial;
        plane.transform.position = planeInitial;
        plane2.transform.position = plane2Initial;
        cloth.transform.position = clothInitial;

        agentCam.transform.position = agentCamInitial;
    }

    IEnumerator Example()
    {
        //print(Time.time);
        yield return new WaitForSeconds(4);
        //print(Time.time);
    }

    public void AreaReset()
    {
        for(int i = 0; i < cloth.transform.childCount; i++)
        {
            Destroy(cloth.transform.GetChild(i).gameObject);    
        }
        mesh.Restart();

        SetEnvironment();

        Debug.Log("reset - Area Reset Script");

        handRight.transform.position = rightInitial;
        handLeft.transform.position = leftInitial;
        agentHand.transform.position = agentHandInitial;
        //StartCoroutine(Example());
    }
}
