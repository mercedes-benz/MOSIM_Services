// SPDX-License-Identifier: MIT
// The content of this file has been developed in the context of the MOSIM research project.
// Original author(s): Felix Gaisbauer

using MMICSharp.Adapter;
using MMIStandard;
using MMIUnity;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static PathPlanningService;

/// <summary>
/// Class representing a path planning environment which contains a set of scene objects
/// </summary>
public class PathPlanningEnvironment
{
    /// <summary>
    /// The unique ID of the environment
    /// </summary>
    public string ID;

    /// <summary>
    /// A list of the corresponding MSceneObjects contained in the environment
    /// </summary>
    public List<MSceneObject> SceneObjects = new List<MSceneObject>();

    /// <summary>
    /// A list of the corresponding game objects contained in the environment
    /// </summary>
    public List<GameObject> GameObjects = new List<GameObject>();

    /// <summary>
    /// The utilized plane
    /// </summary>
    public Transform Plane;

    /// <summary>
    /// Flag specifies whether the evnironment has been initialized
    /// </summary>
    public bool IsInitialized = false;

    /// <summary>
    /// Flag specifies whether the environment is active
    /// </summary>
    public bool IsActive = false;

    /// <summary>
    /// The plane size of the path planning environment
    /// </summary>
    public Vector3 PlaneSize = new Vector3(30, 0, 30);

    /// <summary>
    /// The offset of the avatar
    /// </summary>
    public Vector3 Offset = Vector3.zero;

    /// <summary>
    /// Basic constructor
    /// </summary>
    /// <param name="id"></param>
    public PathPlanningEnvironment(string id, Transform groundPlane)
    {
        this.ID = id;
        this.Plane = groundPlane;
    }


    /// <summary>
    /// Sets up the colliders
    /// To do proper updating of the scene
    /// </summary>
    public void Setup(List<MSceneObject> sceneObjects)
    {
        //Destroy all present objects -> in future reuse if possible
        for (int i = this.GameObjects.Count - 1; i >= 0; i--)
        {
            GameObject obj = this.GameObjects[i];
            obj.GetComponent<Renderer>().enabled = false;
            GameObject.Destroy(obj);
        }

        //Clear all present gameobjects
        this.GameObjects.Clear();

        //Skip if no scene objects are specified
        if (sceneObjects == null)
            return;

        if (sceneObjects.Count > 0)
        {
            Vector3 min = new Vector3((float)sceneObjects.Select(s => s.Transform.Position.X).Min(), 0, (float)sceneObjects.Select(s => s.Transform.Position.Z).Min());
            Vector3 max = new Vector3((float)sceneObjects.Select(s => s.Transform.Position.X).Max(), 0, (float)sceneObjects.Select(s => s.Transform.Position.Z).Max());


            //To do -> Determine the dimensions
            this.PlaneSize = new Vector3(Mathf.Max(30, (max.x - min.x)+10), 0, Mathf.Max(30, (max.z - min.z))+10);

            UnityLogger.Log(Log_level.L_DEBUG, $"Setting the environment dimensions to " + this.PlaneSize.ToString());
        }

        //Create new colliders and gameobjects for all MSceneObjects that possess a collider
        foreach (MSceneObject sceneObject in sceneObjects.Where(s=>s.Collider!=null))
        {
            try
            {
                //Create a collider using the UnityColliderFactory helper class 
                Collider collider = UnityColliderFactory.CreateCollider(sceneObject.Collider, sceneObject.Transform);


                //Unity uses the meshes for the nav mesh generation -> Adjust the meshes to match the collider size
                switch (sceneObject.Collider.Type)
                {
                    case MColliderType.Box:
                        collider.transform.localScale = collider.GetComponent<BoxCollider>().size;
                        collider.GetComponent<BoxCollider>().size = new Vector3(1,1,1);
                        break;

                    case MColliderType.Sphere:
                        SphereCollider sc = collider.GetComponent<SphereCollider>();
                        collider.transform.localScale = new Vector3(sc.radius*2, sc.radius*2, sc.radius*2);
                        sc.radius = 0.5f;
                        break;

                    case MColliderType.Capsule:
                        CapsuleCollider cc = collider.GetComponent<CapsuleCollider>();
                        collider.transform.localScale = new Vector3(cc.transform.localScale.x * cc.radius * 2, cc.transform.localScale.y * cc.radius * 2, cc.transform.localScale.z * cc.radius * 2);
                        cc.radius = 0.5f;
                        break;

                    case MColliderType.Cylinder:
                        CapsuleCollider cyc = collider.GetComponent<CapsuleCollider>();
                        collider.transform.localScale = new Vector3(cyc.transform.localScale.x * cyc.radius * 2, cyc.transform.localScale.y * cyc.radius * 2, cyc.transform.localScale.z * cyc.radius * 2);
                        cyc.radius = 0.5f;
                        break;
                }

                if (collider != null)
                    this.GameObjects.Add(collider.gameObject);

                collider.name = sceneObject.Name + sceneObject.ID;


            }
            catch (System.Exception e)
            {
                Debug.Log("Problem adding collider of: " + sceneObject.Name);
            }
        }

        this.IsInitialized = true;
        
        
    }

    /// <summary>
    /// Actives the environment and all game objects
    /// </summary>
    public void Activate()
    {
        //Adjust the plane size
        this.Plane.transform.localScale = new Vector3(this.PlaneSize.x/5, 1, this.PlaneSize.z/5); // /10 *2

        UnityLogger.Log(Log_level.L_DEBUG, $"Updated plane scale: " + this.Plane.transform.localScale);


        //Activate all game objects
        foreach (GameObject go in this.GameObjects)
            go.SetActive(true);

        //Set flag to true
        this.IsActive = true;
    }

    /// <summary>
    /// Deactives the environment and all game objects
    /// </summary>
    public void Deactivate()
    {
        //Disable all game objects
        foreach (GameObject go in this.GameObjects)
            go.SetActive(false);

        //Set flag to false
        this.IsActive = false;
    }
}


