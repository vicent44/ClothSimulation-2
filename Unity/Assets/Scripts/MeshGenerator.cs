﻿﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class MeshGenerator : MonoBehaviour
{
    //Create the mesh in unity
    Mesh mesh; 
    Mesh mesh2;
    Vector3[] vertices;
    Vector3[] vertices2;
    Vector3[] normals2;
    int[] triangles;
    int[] triangles2;
    Vector2[] uvs;
    Vector2[] uvs2;
    [SerializeField] GameObject TrainEnvironment;
    //If point-triangle or edge triangle collition
    [SerializeField] bool EdgeOrPointCheck = true; 
    //Parameter to set the size of the grid
    public int gridSizeNew;
    //Variable to choose if we want to draw the lines between particles
    [SerializeField] bool drawSprings = true;
    //Setting the cloth density
    [SerializeField] float clothDensity = 150f;
    //Wind direction, in normal vector
    [SerializeField] Vector3 windDirection=new Vector3(0,0,1);
    //Wind module
    [SerializeField] float windModule = 10f;

    //Setting all the constants for the springs
    [SerializeField] float elasticConstantStructural;
    [SerializeField] float elasticConstantShear;
    [SerializeField] float elasticConstantBend;
    [SerializeField] float dampingConstant;
    //Setting all the constants of friction and dissipation for cloth and plane
    [SerializeField] float frictionConstPlane = 0.7f;
    [SerializeField] float dissipationConstPlane = 0.1f;
    [SerializeField] float frictionConstCloth = 0.7f;
    [SerializeField] float dissipationConstCloth = 0.1f;
    [SerializeField] float frictionConstShpereHand = 0.1f;
    [SerializeField] float dissipationConstSphereHand = 0.1f;

    Simulate simulator;
    Hashing hash;

    float timePassed = 0.0f;
    public float deltaTimeStep = 0.02f;

    //List with all the objects: particles, springs, triangles.
    List<Particles> _particles;
    List<Springs> _springs;
    List<Triangles> _triangles;
    List<Edges> _edges;
    List<ParticlesBehaviour> _particlesBehaviour;

    Vector3 winddirectiondensity;
    //List of gameobjects to be able to select some vertex in the mesh (gameobjects)
    [SerializeField] GameObject sphereControl;
    List<GameObject> _sphere;
    [SerializeField] float sphereScale = 0.1f;
    //Set the transform plane to know where to do the plane collision
    [SerializeField] Transform plane;
    [SerializeField] Transform secondPlane;
    [SerializeField] Vector3 normalSecondPlane = new Vector3(0f, 0f, 1f);

    [SerializeField] Transform SphereRightHand;
    [SerializeField] Transform SphereLeftHand;

    //Mouse Drag Variables
    Vector3 screenPoint;
    Vector3 offset;
    Ray ray;
    RaycastHit hit;


    public Transform targetLeft;
    public Transform targetRight;


    public bool isPaused = false;

    void Start()
    {
        //Setting the size of the mesh, specially the density
        //int gridSize = gridSizeNew + (gridSizeNew - 1);
        int gridSize = gridSizeNew + gridSizeNew * (gridSizeNew - 1);
        
        //Initialice the list of particles, springs and triangles.
        _particles = new List<Particles>();
        _springs = new List<Springs>();
        _triangles = new List<Triangles>();
        _sphere = new List<GameObject>();
        _edges = new List<Edges>();
        _particlesBehaviour = new List<ParticlesBehaviour>();

        //To be able to change the size of the sphere
        sphereControl.transform.localScale = new Vector3(sphereScale, sphereScale, sphereScale);

        //In the editor to have a parent of all the particles
        //var particleParent = new GameObject("particleParent");
        //particleParent.transform.parent = this.transform;

        //Calcule the area of the cloth
        float area = 0.2f * (gridSize - 1) * 0.2f * (gridSize - 1);

        //Calcule the total mass of the cloth with the density
        float massTotal = clothDensity * area;
        //Calcule the mass of each particle
        float mass = massTotal / (gridSize * gridSize);

        //Instantiate the position of the particles and the gameobject.
        float jP = 0;
        float iP = 0;
        float iniJ = this.transform.position.x;
        float iniI = this.transform.position.z;
        int pi = 0;
        int pj = 0;
        for(int i = 0; i < gridSize; i++)
        {
            for(int j = 0; j < gridSize; j++)
            {
                jP = j * 0.1f;
                iP = i * 0.1f;
                
                var newP = ParticlesBehaviour.Create(this.transform.localPosition+new Vector3(jP,0.0f,iP), mass, pi, pj, sphereControl);
                newP.transform.SetParent(this.transform, false);
                newP.transform.localPosition = (new Vector3(jP,0.0f,iP)+this.transform.localPosition);
                newP.particles.Position = newP.transform.localPosition;
                _particles.Add(newP.particles);
                _particlesBehaviour.Add(newP);
                pi++;
            }
            pj++;
        }

        var rightFirst = _particlesBehaviour[gridSize - 1].gameObject;
        rightFirst.tag = "right";
        //rightFirst.name = "right_start";

        var leftFirst = _particlesBehaviour[0].gameObject;
        leftFirst.tag = "left";
        //leftFirst.name = "left_start";


        var rightLast = _particlesBehaviour[_particles.Count - 1].gameObject;
        rightLast.layer = 9;
        rightLast.name = "right";
        rightLast.tag = "goal";
        var collrightLast = rightLast.AddComponent<BoxCollider>();
        collrightLast.center = (new Vector3(0f, 2.5f, 0f));
        collrightLast.size = (new Vector3(3.5f, 3.5f, 3.5f));
        var leftLast = _particlesBehaviour[_particles.Count - gridSize].gameObject;
        leftLast.layer = 10;
        leftLast.name = "left";
        leftLast.tag = "goal";
        var collleftLast = leftLast.AddComponent<BoxCollider>();
        collleftLast.center = (new Vector3(0f, 2.5f, 0f));
        collleftLast.size = (new Vector3(3.5f, 3.5f, 3.5f));

        var midelstack1 = _particlesBehaviour[75].gameObject;
        midelstack1.name = "stack";

        var midelstack2 = _particlesBehaviour[76].gameObject;
        midelstack2.name = "stack";

        var midelstack3 = _particlesBehaviour[77].gameObject;
        midelstack3.name = "stack";

        int edgesCount = 0;
        //Instntiate all the springs and all the edges to
        //be able to check collisions (just edges that mades all the triangles)
        //Springs
        //Structural Horizontal - Right
        for(int j = 0; j < gridSize; j++)
        {
            for(int i = 0; i < gridSize - 1; i++)
            {
                var index = j * gridSize + i;
                var c = _particles[index];
                var right = _particles[index + 1];
                var re = new Springs(c, right, elasticConstantStructural, dampingConstant, 1);
                _springs.Add(re);
                var ed = new Edges(index, index + 1, edgesCount);
                edgesCount++;
                ed.PosEdges(c.Position, right.Position);
                _edges.Add(ed);
            }
        }

        //Structural Vertical - Bot
        for(int j = 0; j < gridSize - 1; j++)
        {
            for(int i = 0; i < gridSize; i++)
            {
                var index_1 = j * gridSize + i;
                var index_2 = (j + 1) * gridSize + i;
                var c = _particles[index_1];
                var right = _particles[index_2];
                var re = new Springs(c, right, elasticConstantStructural, dampingConstant, 1);
                _springs.Add(re);
                var ed = new Edges(index_1, index_2, edgesCount);
                edgesCount++;
                ed.PosEdges(c.Position, right.Position);
                _edges.Add(ed);
            }
        }

        //Shear - \ - Down
        for(int j = 0; j < gridSize - 1; j++)
        {
            for(int i = 0; i < gridSize - 1; i++)
            {
                var index_1 = j * gridSize + i;
                var index_2 = (j + 1) * gridSize + i + 1;
                var c = _particles[index_1];
                var bot = _particles[index_2];
                var re = new Springs(c, bot, elasticConstantShear, dampingConstant, 2);
                _springs.Add(re);
                var ed = new Edges(index_1, index_2, edgesCount);
                edgesCount++;
                ed.PosEdges(c.Position, bot.Position);
                _edges.Add(ed);
            }
        }

        //Shear - / - Down
        for(int j = 0; j < gridSize - 1; j++)
        {
            for(int i = 1; i < gridSize; i++)
            {
                var index_1 = j * gridSize + i;
                var index_2 = (j + 1) * gridSize + i - 1;
                var c = _particles[index_1];
                var top = _particles[index_2];
                var re = new Springs(c, top, elasticConstantShear, dampingConstant, 2);
                _springs.Add(re);
            }
        }

        //Bend - Left
        for(int j = 0; j < gridSize; j++)
        {
            for(int i = 0; i < gridSize - 2; i++)
            {
                var index_1 = j * gridSize + i;
                var index_2 = j * gridSize + i + 2;
                var c = _particles[index_1];
                var left = _particles[index_2];
                var re = new Springs(c, left, elasticConstantBend, dampingConstant, 3);
                _springs.Add(re);
            }
        }

        //Bend - Down
        for(int j = 0; j < gridSize - 2; j++)
        {
            for(int i = 0; i < gridSize; i++)
            {
                var index_1 = j * gridSize + i;
                var index_2 = (j + 2) * gridSize + i;
                var c = _particles[index_1];
                var left = _particles[index_2];
                var re = new Springs(c, left, elasticConstantBend, dampingConstant, 3);
                _springs.Add(re);
            }
        }
        
        //Fix two particles
        _particles[0].isActive = false;
        _particles[gridSize - 1].isActive = false;

        Vector3.Normalize(normalSecondPlane);

        //Calcule a part of the wind force and call the simulation script to incitialize it with all the needed information
        winddirectiondensity = windDirection * windModule * clothDensity;
        simulator = new Simulate(_particles, _springs, _triangles, _edges, winddirectiondensity, plane, secondPlane, normalSecondPlane, gridSize, frictionConstPlane, dissipationConstPlane, frictionConstCloth, dissipationConstCloth, drawSprings, EdgeOrPointCheck, SphereRightHand, frictionConstShpereHand, dissipationConstSphereHand, SphereLeftHand);

        //Creating the mess
        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;
        CreateShape();
        UpdateMesh();
        UpdateEdges();
    }

    void FixedUpdate()
    {
        this.transform.GetChild(0).gameObject.GetComponent<ParticlesBehaviour>().particles.SetPosition(targetLeft.position);
        this.transform.GetChild(8).gameObject.GetComponent<ParticlesBehaviour>().particles.SetPosition(targetRight.position);
        if(float.IsNaN(this.transform.GetChild(0).gameObject.GetComponent<ParticlesBehaviour>().particles.Position.x))
        {
            GameObject.Find("AgentHands").GetComponent<AgentRobotHand>().Error();
        }
    }

    void Update()
    {
        if(Input.GetKeyDown("q"))
        {
            Restart();
        }
        //Chech that a step is done only every deltaTimeStep and press space in the keyboard to pause/start
        timePassed += Time.deltaTime;
        if(timePassed >= deltaTimeStep) timePassed = 0.0f;
        if(!isPaused && timePassed == 0.0f)
        {
            int gridSize = gridSizeNew + gridSizeNew * (gridSizeNew - 1);
            //int gridSize = gridSizeNew + (gridSizeNew - 1);
            simulator.Update(deltaTimeStep, _triangles, _edges);
            UpdateMesh();
            UpdateEdges();
        }
        if (Input.GetKeyDown(KeyCode.Space))
        {
            isPaused = !isPaused;
        }
    }

    private Vector3 offsett;
    private Vector3 screenPointt;

    //Here I update the position of the edges for the self collision
    void UpdateEdges()
    {
        int gridSize = gridSizeNew + gridSizeNew * (gridSizeNew - 1);
        int edgeCount = 0;
        //Springs
        //Structural Horizontal - Right
        for(int j = 0; j < gridSize; j++)
        {
            for(int i = 0; i < gridSize - 1; i++)
            {
                var index = j * gridSize + i;
                var c = _particles[index];
                var right = _particles[index + 1];
                _edges[edgeCount].PosEdges(c.Position, right.Position);
                edgeCount++;
            }
        }

        //Structural Vertical - Bot
        for(int j = 0; j < gridSize - 1; j++)
        {
            for(int i = 0; i < gridSize; i++)
            {
                var index_1 = j * gridSize + i;
                var index_2 = (j + 1) * gridSize + i;
                var c = _particles[index_1];
                var right = _particles[index_2];
                _edges[edgeCount].PosEdges(c.Position, right.Position);
                edgeCount++;
            }
        }

        //Shear - \ - Down
        for(int j = 0; j < gridSize - 1; j++)
        {
            for(int i = 0; i < gridSize - 1; i++)
            {
                var index_1 = j * gridSize + i;
                var index_2 = (j + 1) * gridSize + i + 1;
                var c = _particles[index_1];
                var bot = _particles[index_2];
                _edges[edgeCount].PosEdges(c.Position, bot.Position);
                edgeCount++;
            }
        }
    }

    //Create the mesh and the triangles for the collisions.
    void CreateShape ()
    {
        //int gridSize = gridSizeNew + (gridSizeNew - 1);
        int gridSize = gridSizeNew + gridSizeNew * (gridSizeNew - 1);
        int triangleNum = (gridSize - 1) * (gridSize - 1) * 2 * 3;

        int vertexNum = gridSize * gridSize;

        triangles = new int[triangleNum];
        vertices = new Vector3 [vertexNum];

        triangles2 = new int[triangleNum];
        uvs = new Vector2 [vertices.Length];

        //First I set the triangles by setting the points
        //of it: And de direction of the normal
        int xx = 0;
        int x = 0;
        int ind = 0;
        for(int j = 0; j < gridSize - 1; j++)
        {
            for(int i = 0; i < gridSize - 1; i++)
            {
                int i0 = j * gridSize + i;
                int i1 = j * gridSize + i + 1;
                int i2 = (j + 1) * gridSize + i;
                int i3 = (j + 1) * gridSize + i +1;


                var p1 = new Triangles(i0, i2, i1, ind);
                ind++;
                p1.PosTriangles(_particlesBehaviour[i0].transform.localPosition, _particlesBehaviour[i2].transform.localPosition, _particlesBehaviour[i1].transform.localPosition);
                _triangles.Add(p1);
                var p2 = new Triangles(i2, i3, i1, ind);
                ind++;
                p2.PosTriangles(_particlesBehaviour[i2].transform.localPosition, _particlesBehaviour[i3].transform.localPosition, _particlesBehaviour[i1].transform.localPosition);
                _triangles.Add(p2);
                triangles[x] = i0;
                x++;
                triangles[x] = i2;
                x++;
                triangles[x] = i1;
                x++;
                triangles[x] = i2;
                x++;
                triangles[x] = i3;
                x++;
                triangles[x] = i1;
                x++;

                triangles2[xx] = i0;
                xx++;
                triangles2[xx] = i1;
                xx++;
                triangles2[xx] = i2;
                xx++;
                triangles2[xx] = i1;
                xx++;
                triangles2[xx] = i3;
                xx++;
                triangles2[xx] = i2;
                xx++;
            }
        } 

        //Second I set the position of all the points of the mesh
        for(int i = 0; i < gridSize; i++)
        {
            for(int j = 0; j < gridSize; j++)
            {
                var pos = j * gridSize + i;
                vertices[pos] = _particlesBehaviour[pos].transform.position;
            }
        }

        for(int i = 0; i < uvs.Length; i++)
        {
            uvs[i] = new Vector2(vertices[i].x, vertices[i].z);
        }

    }

    //Just update every time step the positions of the triangles. 
    void UpdateMesh()
    {
        //int gridSize = gridSizeNew + (gridSizeNew - 1);
        int gridSize = gridSizeNew + gridSizeNew * (gridSizeNew - 1);
        mesh.Clear();
        int posi = 0;
        for(int j = 0; j < gridSize - 1; j++)
        {
            for(int i = 0; i < gridSize - 1; i++)
            {
                int i0 = j * gridSize + i;
                int i1 = j * gridSize + i + 1;
                int i2 = (j + 1) * gridSize + i;
                int i3 = (j + 1) * gridSize + i +1;

                _triangles[posi].PosTriangles(_particlesBehaviour[i0].transform.localPosition, _particlesBehaviour[i2].transform.localPosition, _particlesBehaviour[i1].transform.localPosition);
                posi++;
                _triangles[posi].PosTriangles(_particlesBehaviour[i2].transform.localPosition, _particlesBehaviour[i3].transform.localPosition, _particlesBehaviour[i1].transform.localPosition);
                posi++;
            }
        } 

        for(int i = 0; i < gridSize; i++)
        {
            for(int j = 0; j < gridSize; j++)
            {
                var pos = j * gridSize + i;
                vertices[pos] = _particlesBehaviour[pos].transform.localPosition;
            }
        }
        for(int i = 0; i < uvs.Length; i++)
        {
            uvs[i].x = vertices[i].x;
            uvs[i].y = vertices[i].z;
        }



        //Two meshes one in one normal direction and
        //the other one in the other direction to be able
        //to see the mesh in both directions
        mesh.subMeshCount = 2;

        mesh.vertices = vertices;
        //mesh.uv = uvs;

        mesh.SetTriangles(triangles, 0);
        mesh.SetTriangles(triangles2, 1);

        mesh.RecalculateNormals();
    }

    void OnDrawGizmos()
    {
        simulator.DrawGizmos();
    }

    public void Restart()
    {
        int gridSize = gridSizeNew + gridSizeNew * (gridSizeNew - 1);

        /*for(int i = 0; i < _particles.Count; i++)
        {
            Destroy(transform.GetChild(i).gameObject);
        }*/
        Start();        
    }
}