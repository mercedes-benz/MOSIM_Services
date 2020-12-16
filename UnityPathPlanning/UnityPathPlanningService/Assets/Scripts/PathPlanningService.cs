// SPDX-License-Identifier: MIT
// The content of this file has been developed in the context of the MOSIM research project.
// Original author(s): Felix Gaisbauer

using MMICSharp.Adapter;
using MMICSharp.Common;
using MMICSharp.Services;
using MMIStandard;
using MMIUnity;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;
using UnityEngine.AI;
using Logger = MMICSharp.Adapter.Logger;

/// <summary>
/// Implementation of a basic path planning service using the unity in-build nav mesh agent
/// </summary>
public class PathPlanningService : MonoBehaviour, MPathPlanningService.Iface
{
    private readonly Mutex agentMutex = new Mutex();
    private readonly Mutex environmentMutex = new Mutex();

    /// <summary>
    /// Dictionary contains the different environments
    /// </summary>
    private readonly ConcurrentDictionary<string, PathPlanningEnvironment> environments = new ConcurrentDictionary<string, PathPlanningEnvironment>();

    /// <summary>
    /// The presently active environment
    /// </summary>
    private PathPlanningEnvironment activeEnvironment;

    /// <summary>
    /// The default enviornment that is used if no one is specified
    /// </summary>
    private PathPlanningEnvironment defaultEnvironment = new PathPlanningEnvironment("default");

    /// <summary>
    /// The address where the service is hosted 
    /// </summary>
    private MIPAddress address = new MIPAddress();

    /// <summary>
    /// The address of the register 
    /// </summary>
    private MIPAddress registerAddress = new MIPAddress();


    /// <summary>
    /// The utilized nav mesh agent
    /// </summary>
    private NavMeshAgent agent;

    /// <summary>
    /// The service description
    /// </summary>
    private MServiceDescription description = new MServiceDescription()
    {
        ID = "pathPlanning10032020",
        Language = "UnityC#",
        Name = "pathPlanningService",

        //Directly define the parameters
        Parameters = new List<MParameter>()
        {
            new MParameter("mode", "{2D,3D}", "The mode of the path planning", true),
            new MParameter("pathPlanningID", "string", "An optional id which allows the reutilization of the environment. " +
                "(if not set, the environment will not be stored)", false),
            new MParameter("reuseEnvironment", "bool", "Specifies if the identical environment should be reused", false),
        }
    };


    /// <summary>
    /// The utilized nav mesh surface
    /// </summary>
    private NavMeshSurface navMeshSurface;

    /// <summary>
    /// The utlized service controller to host the actual service
    /// </summary>
    private ServiceController controller;


    /// <summary>
    /// Log level for log output
    /// </summary>
    private Log_level logLevel = Log_level.L_DEBUG;

    /// <summary>
    /// Flag inidcates whether the service is a server build (not running with ui)
    /// </summary>
    public static bool IsServerBuild = false;

    // Start is called before the first frame update
    void Start()
    {
        //Check if we are within a server build
        IsServerBuild = IsHeadlessMode();

        //Fetch the navmesh agent
        this.agent = this.GetComponent<NavMeshAgent>();

        //Create a new instance of the logger
        Logger.Instance = new UnityLogger();
        Logger.Instance.Level = logLevel;

        //Find the nav mesh surface
        this.navMeshSurface = GameObject.FindObjectOfType<NavMeshSurface>();

        //Add the main thread dispatcher add the beginning if not already available 
        if (this.GetComponent<MainThreadDispatcher>() == null)
            this.gameObject.AddComponent<MainThreadDispatcher>();

        //Set the target frame rate relatively low
        Application.targetFrameRate = 15;

        //Set quality level to zero
        QualitySettings.SetQualityLevel(0, true);


        //Check if this is a server build which has no visualization and a console instead
        if (IsServerBuild)
        {
            System.Console.WriteLine(@"   __  __      _ __           ____        __  __       ____  __                  _                _____                 _         ");
            System.Console.WriteLine(@"  / / / /___  (_) /___  __   / __ \____ _/ /_/ /_     / __ \/ /___ _____  ____  (_)___  ____ _   / ___/___  ______   __(_)_______ ");
            System.Console.WriteLine(@" / / / / __ \/ / __/ / / /  / /_/ / __ `/ __/ __ \   / /_/ / / __ `/ __ \/ __ \/ / __ \/ __ `/   \__ \/ _ \/ ___/ | / / / ___/ _ \");
            System.Console.WriteLine(@"/ /_/ / / / / / /_/ /_/ /  / ____/ /_/ / /_/ / / /  / ____/ / /_/ / / / / / / / / / / / /_/ /   ___/ /  __/ /   | |/ / / /__/  __/");
            System.Console.WriteLine(@"\____/_/ /_/_/\__/\__, /  /_/    \__,_/\__/_/ /_/  /_/   /_/\__,_/_/ /_/_/ /_/_/_/ /_/\__, /   /____/\___/_/    |___/_/\___/\___/ ");
            System.Console.WriteLine(@"                 /____/                                                              /____/                                   ");
            System.Console.WriteLine(@"_________________________________________________________________");
        }



        //Only use this if self_hosted and within edit mode -> Otherwise the launcher which starts the service assigns the address and port
#if UNITY_EDITOR
        this.address.Address = "127.0.0.1";
        this.address.Port = 8950;

        this.registerAddress.Port = 9009;
        this.registerAddress.Address = "127.0.0.1";
#else
        //Parse the command line arguments
        if (!this.ParseCommandLineArguments(System.Environment.GetCommandLineArgs()))
        {
            Logger.Log(Log_level.L_ERROR, "Cannot parse the command line arguments. Closing the service!");
            return;
        }
#endif


        //Add the present address 
        this.description.Addresses = new List<MIPAddress>()
        {
            this.address
        };

        this.activeEnvironment = defaultEnvironment;

        //Create a new service controller
        this.controller = new ServiceController(description, registerAddress, new MPathPlanningService.Processor(this));
        //Start asynchronously
        this.controller.StartAsync();
    }

    private void OnApplicationQuit()
    {
        //Dispose the controller if not null
        if (this.controller != null)
            this.controller.Dispose();
    }

    /// <summary>
    /// Method is part of the MPathPlanning Interface and is called remotely.
    /// </summary>
    /// <param name="start"></param>
    /// <param name="goal"></param>
    /// <param name="sceneObjects"></param>
    /// <param name="properties"></param>
    /// <returns>A path constraint containing the generated path + tolerance. Please note that the MGeometryConstrains contained in the path have no parent (parent ="")</returns>
    public MPathConstraint ComputePath(MVector start, MVector goal, List<MSceneObject> sceneObjects, Dictionary<string, string> properties)
    {
        MPathConstraint result = new MPathConstraint()
        {
            PolygonPoints = new List<MGeometryConstraint>()
        };

        List<MVector> pathPoints = new List<MVector>();
        bool success = false;

        //Check if properties are defined
        if (properties != null)
        {
            UnityLogger.Log(Log_level.L_DEBUG, "Compute path called with the following properties:");

            foreach (var entry in properties)
            {
                UnityLogger.Log(Log_level.L_DEBUG, entry.Key + " : " + entry.Value);
            }


            //Set up /reuse the environment
            this.SetupEnvironment(sceneObjects, properties);


            //Execute on main thread
            MainThreadDispatcher.Instance.ExecuteBlocking(() =>
            {
                //Switch depending on the mode
                if (properties.ContainsKey("mode"))
                {
                    switch (properties["mode"])
                    {
                        case "2D":
                            pathPoints = this.ComputePathNavMeshAgent2D(new MVector2(start.Values[0], start.Values[1]), new MVector2(goal.Values[0], goal.Values[1]), out success);
                            break;
                        case "3D":
                            pathPoints = this.ComputePathNavMeshAgent3D(new MVector3(start.Values[0], start.Values[1], start.Values[2]), new MVector3(goal.Values[0], goal.Values[1], goal.Values[2]), out success);
                            break;
                        default:
                            UnityLogger.Log(Log_level.L_ERROR, $"Specified mode { properties["mode"]} is not supported");
                            break;
                    }
                }
            });

        }

        float tolerance = 0.2f;
        foreach (MVector vector in pathPoints)
        {
            //Create a new geometry constraint without a parent
            MGeometryConstraint geometryConstraint = new MGeometryConstraint("");

            MInterval3 positionInterval = new MInterval3();

            //Defined position interval differently depending on the actual dimensionality
            switch (vector.Values.Count)
            {
                case 2:
                    //Create new position interval with a defined tolerance
                    positionInterval = new MVector3(vector.Values[0], 0f, vector.Values[1]).ToMInterval3(tolerance);

                    //For 2d case y axis is not considered
                    positionInterval.Y.Max = 0;
                    positionInterval.Y.Min = 0;
                    break;

                case 3:
                    //Create new position interval with a defined tolerance
                    positionInterval = new MVector3(vector.Values[0], vector.Values[1], vector.Values[2]).ToMInterval3(tolerance);
                    break;
            }

            //Create a new translation constraint
            geometryConstraint.TranslationConstraint = new MTranslationConstraint(MTranslationConstraintType.BOX, positionInterval);

            //Add the generated geometry constraint
            result.PolygonPoints.Add(geometryConstraint);
        }

        //Return the compute results (if available)
        return result;
    }


    /// <summary>
    /// Interface which is remotely accessed.
    /// The method computes the direction vector(including the velocity) to steer to the goal position
    /// </summary>
    /// <param name="current"></param>
    /// <param name="goal"></param>
    /// <param name="sceneObjects"></param>
    /// <param name="properties"></param>
    /// <returns></returns>
    public MVector ComputePathDirection(MVector current, MVector goal, List<MSceneObject> sceneObjects, Dictionary<string, string> properties)
    {
        //Setup/reuse the environment
        this.SetupEnvironment(sceneObjects, properties);

        //Get the current and goal position
        Vector3 currentPosition = current.Values.Count == 3 ? new Vector3((float)current.Values[0], (float)current.Values[1], (float)current.Values[2]) : new Vector3((float)current.Values[0], 0, (float)current.Values[1]);
        Vector3 goalPosition = goal.Values.Count == 3 ? new Vector3((float)goal.Values[0], (float)goal.Values[1], (float)goal.Values[2]) : new Vector3((float)goal.Values[0], 0, (float)goal.Values[1]);

        //Result vector describing the velocity
        MVector result = new MVector()
        {
            Values = new List<double>()
        };

        //Enter restricted area in which the agent is manipulated
        agentMutex.WaitOne();

        //Perform update of agent on main thread
        MainThreadDispatcher.Instance.ExecuteBlocking(() =>
        {
            //Set the current agent position
            this.agent.transform.position = currentPosition;

            //Set the goal position of the agent
            this.agent.SetDestination(goalPosition);

            if (current.Values.Count == 3)
            {
                result.Values.Add(this.agent.velocity.x);
                result.Values.Add(this.agent.velocity.y);
                result.Values.Add(this.agent.velocity.z);

            }

            if (current.Values.Count == 2)
            {
                result.Values.Add(this.agent.velocity.x);
                result.Values.Add(this.agent.velocity.z);
            }
        });

        //Exit restricted area
        agentMutex.ReleaseMutex();

        //Return the computed result
        return result;
    }


    /// <summary>
    /// Method sets up the environment
    /// </summary>
    /// <param name="sceneObjects"></param>
    /// <param name="properties"></param>
    private void SetupEnvironment(List<MSceneObject> sceneObjects, Dictionary<string, string> properties)
    {
        this.environmentMutex.WaitOne();

        UnityLogger.Log(Log_level.L_DEBUG, "Setup enviornment:");

        foreach (var entry in properties)
        {
            UnityLogger.Log(Log_level.L_DEBUG, entry.Key + " : " + entry.Value);
        }


        //Check if a path planning ID is available
        bool hasPathPlanningID = properties.ContainsKey("pathPlanningID");

        bool reuseEnvironment = false;
        string pathPlanningID = System.Guid.NewGuid().ToString();

        if(hasPathPlanningID)
            pathPlanningID= properties["pathPlanningID"];


        PathPlanningEnvironment newEnvironment = defaultEnvironment;


        //Execute on main thread
        MainThreadDispatcher.Instance.ExecuteBlocking(() =>
        {
            if (hasPathPlanningID)
            {
                properties.TryGetBool("reuseEnvironment", out reuseEnvironment);

                //Add the new environment if not already available
                if (!this.environments.ContainsKey(pathPlanningID))
                    this.environments.TryAdd(pathPlanningID, new PathPlanningEnvironment(pathPlanningID));

                //Get the respective environment
                this.environments.TryGetValue(pathPlanningID, out newEnvironment);

                UnityLogger.Log(Log_level.L_DEBUG, $"Reusing the environment {pathPlanningID}");


                //If the environment changes
                if (this.activeEnvironment != newEnvironment)
                {
                    //Deactivate the current environment
                    this.activeEnvironment?.Deactivate();

                    //Assign the new environment as active
                    this.activeEnvironment = newEnvironment;
                }
            }

            //Setup the environment if not initalized or reuse disabled
            if (!this.activeEnvironment.IsInitialized || !reuseEnvironment)
            {
                UnityLogger.Log(Log_level.L_DEBUG, $"Setup the environment {pathPlanningID}");

                this.activeEnvironment.Setup(sceneObjects);
            }


            //Active the new environment
            this.activeEnvironment.Activate();

        });

        this.environmentMutex.ReleaseMutex();
    }



    #region legacy

    /// <summary>
    /// Method is part of the MPathPlanning Interface and is called remotely.
    /// </summary>
    /// <param name="start"></param>
    /// <param name="goal"></param>
    /// <param name="sceneObjects"></param>
    /// <param name="properties"></param>
    /// <returns></returns>
    public List<MVector> ComputePathLegacy(MVector start, MVector goal, List<MSceneObject> sceneObjects, Dictionary<string, string> properties)
    {
        List<MVector> results = new List<MVector>();
        bool success = false;

        //Check if properties are defined
        if (properties != null)
        {
            UnityLogger.Log(Log_level.L_DEBUG, "Compute path called with the following properties:");

            foreach (var entry in properties)
            {
                UnityLogger.Log(Log_level.L_DEBUG, entry.Key + " : " + entry.Value);
            }


            //Check if a path planning ID is available
            bool hasPathPlanningID = properties.ContainsKey("pathPlanningID");


            //Execute on main thread
            MainThreadDispatcher.Instance.ExecuteBlocking(() =>
            {
                bool reuseEnvironment = false;
                string pathPlanningID = properties["pathPlanningID"];

                PathPlanningEnvironment newEnvironment = defaultEnvironment;

                if (hasPathPlanningID)
                {
                    properties.TryGetBool("reuseEnvironment", out reuseEnvironment);

                    //Add the new environment if not already available
                    if (!this.environments.ContainsKey(pathPlanningID))
                        this.environments.TryAdd(pathPlanningID, new PathPlanningEnvironment(pathPlanningID));

                    //Get the respective environment
                    this.environments.TryGetValue(pathPlanningID, out newEnvironment);

                    UnityLogger.Log(Log_level.L_DEBUG, $"Reusing the environment {pathPlanningID}");


                    //If the environment changes
                    if (this.activeEnvironment != newEnvironment)
                    {
                        //Deactivate the current environment
                        this.activeEnvironment?.Deactivate();

                        //Assign the new environment as active
                        this.activeEnvironment = newEnvironment;
                    }
                }

                //Setup the environment if not initalized or reuse disabled
                if (!this.activeEnvironment.IsInitialized || !reuseEnvironment)
                {
                    UnityLogger.Log(Log_level.L_DEBUG, $"Setup the environment {pathPlanningID}");

                    this.activeEnvironment.Setup(sceneObjects);
                }

                //Active the new 
                this.activeEnvironment.Activate();

                //Switch depending on the mode
                if (properties.ContainsKey("mode"))
                {
                    switch (properties["mode"])
                    {
                        case "2D":
                            results = this.ComputePathNavMeshAgent2D(new MVector2(start.Values[0], start.Values[1]), new MVector2(goal.Values[0], goal.Values[1]), out success);
                            break;
                        case "3D":
                            results = this.ComputePathNavMeshAgent3D(new MVector3(start.Values[0], start.Values[1], start.Values[2]), new MVector3(goal.Values[0], goal.Values[1], goal.Values[2]), out success);
                            break;
                        default:
                            UnityLogger.Log(Log_level.L_ERROR, $"Specified mode { properties["mode"]} is not supported");
                            break;
                    }
                }
            });

        }

        //Return the compute results (if available)
        return results;

    }

    #endregion







    #region actual path planning

    /// <summary>
    /// Computes a two-dimensional path using the nav mesh agent
    /// </summary>
    /// <param name="start"></param>
    /// <param name="goal"></param>
    /// <param name="success"></param>
    /// <returns></returns>
    private List<MVector> ComputePathNavMeshAgent2D(MVector2 start, MVector2 goal, out bool success)
    {
        //Create the start and and vector
        Vector3 startVec3 = new Vector3((float)start.X, 0, (float)start.Y);
        Vector3 goalVec3 = new Vector3((float)goal.X, 0, (float)goal.Y);


        success = false;
        List<MVector> result = new List<MVector>();

        //Build the nav mesh
        this.navMeshSurface.BuildNavMesh();

        //Create a path object
        NavMeshPath path = new NavMeshPath();

        if (NavMesh.CalculatePath(startVec3, goalVec3, NavMesh.AllAreas, path))
        {
            //Check the status of the path
            if (path.status == NavMeshPathStatus.PathComplete)
            {
                success = true;

                UnityLogger.Log(Log_level.L_INFO, $"2D Path with {path.corners.Length} nodes has been found.");


                //Fetch the results and provide as List of MVector
                for (int i = 0; i < path.corners.Length; i++)
                {
                    result.Add(new MVector() { Values = new List<double>() { path.corners[i].x, path.corners[i].z } });
                }

                //Visualize the result on non server builds only
                if (!IsServerBuild)
                {
                    //Perform a non blocking visualization of the path
                    MainThreadDispatcher.Instance.ExecuteNonBlocking(() =>
                    {
                        LineRenderer lineRenderer = this.GetComponent<LineRenderer>();

                        if (lineRenderer == null)
                        {
                            lineRenderer = this.gameObject.AddComponent<LineRenderer>();
                            lineRenderer.material = new Material(Shader.Find("Standard"));
                            lineRenderer.material.color = Color.blue;
                            lineRenderer.startColor = Color.blue;
                            lineRenderer.endColor = Color.blue;
                            lineRenderer.startWidth = 0.1f;
                            lineRenderer.endWidth = 0.1f;
                        }

                        lineRenderer.positionCount = path.corners.Length;
                        lineRenderer.SetPositions(path.corners);
                    });
                }
            }

            else
            {
                UnityLogger.Log(Log_level.L_ERROR, $"Problem at finding a valid 2D path.");
            }
        }

        return result;
    }

    /// <summary>
    /// Computes a three-dimensional path using the nav mesh agent
    /// </summary>
    /// <param name="start"></param>
    /// <param name="goal"></param>
    /// <param name="success"></param>
    /// <returns></returns>
    private List<MVector> ComputePathNavMeshAgent3D(MVector3 start, MVector3 goal, out bool success)
    {
        Vector3 startVec3 = new Vector3((float)start.X, (float)start.Z, (float)start.Z);
        Vector3 goalVec3 = new Vector3((float)goal.X, (float)goal.Y, (float)goal.Z);


        success = false;
        List<MVector> result = new List<MVector>();

        //Build the nav mesh
        this.navMeshSurface.BuildNavMesh();

        NavMeshPath path = new NavMeshPath();
        if (NavMesh.CalculatePath(startVec3, goalVec3, NavMesh.AllAreas, path))
        {
            if (path.status == NavMeshPathStatus.PathComplete)
            {
                success = true;
                UnityLogger.Log(Log_level.L_INFO, $"3D Path with {path.corners.Length} nodes has been found.");

                for (int i = 0; i < path.corners.Length; i++)
                {
                    result.Add(new MVector() { Values = new List<double>() { path.corners[i].x, path.corners[i].y, path.corners[i].z } });
                }
            }
        }

        else
        {
            UnityLogger.Log(Log_level.L_ERROR, $"Problem at finding a valid 3D path.");
        }

        return result;
    }

    #endregion



    #region further methods required by the interfacve
    public Dictionary<string, string> GetStatus()
    {
        return new Dictionary<string, string>()
        {
            { "Running", "true"}
        };
    }

    public MServiceDescription GetDescription()
    {
        return this.description;
    }

    public MBoolResponse Setup(MAvatarDescription avatar, Dictionary<string, string> properties)
    {
        //Nothing to do
        return new MBoolResponse(true);
    }

    public Dictionary<string, string> Consume(Dictionary<string, string> properties)
    {
        throw new System.NotImplementedException();
    }


    #endregion

    /// <summary>
    /// Tries to parse the command line arguments
    /// </summary>
    /// <param name="args"></param>
    /// <returns></returns>
    private bool ParseCommandLineArguments(string[] args)
    {
        //Parse the command line arguments
        OptionSet p = new OptionSet()
            {
                { "a|address=", "The address of the hostet tcp server.",
                  v =>
                  {
                      //Split the address to get the ip and port
                      string[] addr  = v.Split(':');

                      if(addr.Length == 2)
                      {
                          this.address.Address = addr[0];
                          this.address.Port = int.Parse(addr[1]);
                      }
                      Debug.Log("Address: " + v);
                  }
                },

                { "r|raddress=", "The address of the register which holds the central information.",
                  v =>
                  {
                      //Split the address to get the ip and port
                      string[] addr  = v.Split(':');

                      if(addr.Length == 2)
                      {
                          this.registerAddress.Address = addr[0];
                          this.registerAddress.Port = int.Parse(addr[1]);
                      }
                      Debug.Log("Register address: " + v);
                  }
                }
            };

        try
        {
            p.Parse(args);
            return true;
        }

        catch (System.Exception)
        {
            Debug.Log("Cannot parse arguments");
        }

        return false;
    }

    /// <summary>
    /// Indicates whether the current build is in headless mode (no graphics device)
    /// </summary>
    /// <returns></returns>
    private static bool IsHeadlessMode()
    {
        return SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null;
    }


    #region experimental



    public void ComputePathDebug(Vector3 start, Vector3 target)
    {
        NavMeshAgent agent = GameObject.FindObjectOfType<NavMeshAgent>();

        agent.transform.position = start;

        agent.SetDestination(target);

        NavMeshPath path = new NavMeshPath();
        if (NavMesh.CalculatePath(start, target, NavMesh.AllAreas, path))
        {
            if (path.status == NavMeshPathStatus.PathComplete)
            {
                UnityLogger.Log(Log_level.L_INFO, $"Path with {path.corners.Length} nodes has been found.");

                for (int i = 0; i < path.corners.Length - 1; i++)
                    Debug.DrawLine(path.corners[i], path.corners[i + 1], Color.red, 30);
            }
        }
    }

    public MBoolResponse Dispose(Dictionary<string, string> properties)
    {
        throw new System.NotImplementedException();
    }

    public MBoolResponse Restart(Dictionary<string, string> properties)
    {
        throw new System.NotImplementedException();
    }


    #endregion




    /// <summary>
    /// Implementation of a logger which outputs the text on the unity console
    /// </summary>
    public class UnityLogger : MMICSharp.Adapter.Logger
    {
        protected override void LogDebug(string text)
        {
            if (PathPlanningService.IsServerBuild)
            {
                //Call the base class
                base.LogDebug(text);
            }
            else
            {
                Debug.Log(text);
            }
        }

        protected override void LogError(string text)
        {
            if (PathPlanningService.IsServerBuild)
            {
                //Call the base class
                base.LogError(text);
            }
            else
            {
                Debug.LogError(text);
            }
        }

        protected override void LogInfo(string text)
        {
            if (PathPlanningService.IsServerBuild)
            {
                //Call the base class
                base.LogInfo(text);
            }
            else
            {
                Debug.Log(text);
            }
        }
    }
}


    public static class DictionaryExtensions
    {
        public static bool TryGetBool(this Dictionary<string, string> dict, string key, out bool boolResult)
        {
            boolResult = false;
            string value;

            if (dict.TryGetValue(key, out value))
            {
                if (bool.TryParse(value, out boolResult))
                    return true;
            }

            return false;
        }
    }

