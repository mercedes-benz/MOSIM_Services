// SPDX-License-Identifier: MIT
// The content of this file has been developed in the context of the MOSIM research project.
// Original author(s): Felix Gaisbauer

using MMIStandard;
using MMIUnity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


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
    /// Flag specifies whether the evnironment has been initialized
    /// </summary>
    public bool IsInitialized = false;

    /// <summary>
    /// Flag specifies whether the environment is active
    /// </summary>
    public bool IsActive = false;

    /// <summary>
    /// Basic constructor
    /// </summary>
    /// <param name="id"></param>
    public PathPlanningEnvironment(string id)
    {
        this.ID = id;
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

        this.GameObjects.Clear();

        if (sceneObjects == null)
            return;

        //Create new colliders
        foreach (MSceneObject sceneObject in sceneObjects)
        {
            try
            {
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
                        collider.GetComponent<SphereCollider>().radius = 0.5f;
                        break;
 
                        //To do -> fix Capsule and cone as well
                }


                collider.name = sceneObject.Name + sceneObject.ID;

                if (collider != null)
                    this.GameObjects.Add(collider.gameObject);
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
        foreach (GameObject go in this.GameObjects)
        {
            go.SetActive(true);
        }

        this.IsActive = true;
    }

    /// <summary>
    /// Deactives the environment and all game objects
    /// </summary>
    public void Deactivate()
    {
        foreach (GameObject go in this.GameObjects)
        {
            go.SetActive(false);
        }

        this.IsActive = false;
    }
}


