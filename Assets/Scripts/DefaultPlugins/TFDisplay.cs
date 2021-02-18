﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RosSharp.RosBridgeClient;
using RosSharp.RosBridgeClient.MessageTypes.Geometry;
using Microsoft.MixedReality.Toolkit.UI;
using RosSharp;
using TMPro;
using UnityEngine.Events;
using System.Linq;

public class TFDisplay : MonoBehaviour
{
    [SerializeField]
    GameObject tf_prefab;
    [SerializeField]
    GameObject checkbox_prefab;
    [SerializeField]
    GameObject arrowPrefab;
    [SerializeField]
    GameObject rosConnector;

    private List<TransformStamped> tf_dynamic;
    private List<TransformStamped> tf_static;
    private List<string> frame_name;
    private List<string> parent_name;
    private List<RosSharp.RosBridgeClient.MessageTypes.Geometry.Transform> parent_to_child_tf;
    private List<GameObject> arrows;
    public List<GameObject> TFTree;

    // Start is called before the first frame update
    void Start()
    {
        rosConnector = GameObject.Find("ROS Connector");
        tf_dynamic = new List<TransformStamped>();
        tf_static = new List<TransformStamped>();
        frame_name = new List<string>();
        parent_name = new List<string>();
        parent_to_child_tf = new List<RosSharp.RosBridgeClient.MessageTypes.Geometry.Transform>();
        TFTree = new List<GameObject>();
        arrows = new List<GameObject>();

        // StartCoroutine(populateMenu());
        StartCoroutine(TFUpdate());
    }
    IEnumerator populateMenu()
    {
        float offset = -0.2263f;

        // wait for tree to be populated
        yield return new WaitForSeconds(2f);

        // sanity check
        while (TFTree.Count == 0)
        {
            yield return null;
        }

        for (int i = 0; i < TFTree.Count; i++)
        {
            var checkbox = Instantiate(checkbox_prefab, transform.position, transform.rotation);

            checkbox.transform.parent = GameObject.Find("TFMenuPanel").transform;
            checkbox.transform.localPosition = new UnityEngine.Vector3(-0.2364f, offset, -0.0172f);
            checkbox.transform.localRotation = UnityEngine.Quaternion.identity;

            checkbox.transform.Find("ButtonContent").transform.Find("Label").GetComponent<TextMesh>().text = TFTree[i].name;
            checkbox.name = TFTree[i].name + "_checkbox";

            // next checkbox offset lower
            offset -= 0.06f;

            // Scale size of backplate to match number of entries
            GameObject.Find("BackPlate").transform.position += new UnityEngine.Vector3(0f, -1f, 0) * 0.06f / 2;
            GameObject.Find("BackPlate").transform.localScale += new UnityEngine.Vector3(0f, 1f, 0) * 0.06f;
        }
    }
    IEnumerator TFUpdate()
    {
        while (true)
        {
            // Update the list of TF frames
            tf_static = rosConnector.GetComponent<TFStaticSubscriber>().GetPublishedTransforms();
            tf_dynamic = rosConnector.GetComponent<TFSubscriber>().GetPublishedTransforms();

            if (tf_dynamic != null && tf_static != null)
            {
                frame_name.Clear();
                parent_name.Clear();
                parent_to_child_tf.Clear();

                foreach (TransformStamped parent_transform in tf_static)
                {
                    frame_name.Add(parent_transform.child_frame_id + "_tf");
                    parent_name.Add(parent_transform.header.frame_id + "_tf");
                    parent_to_child_tf.Add(parent_transform.transform);
                }
                foreach (TransformStamped parent_transform in tf_dynamic)
                {
                    frame_name.Add(parent_transform.child_frame_id + "_tf");
                    parent_name.Add(parent_transform.header.frame_id + "_tf");
                    parent_to_child_tf.Add(parent_transform.transform);
                }
                // Delete the TF frames that no longer exist in the newly received list
                foreach (GameObject frame in TFTree)
                {
                    if (frame)
                    {
                        // if the frame name is not found in the new list of frames and parent frames
                        if (frame_name.IndexOf(frame.name) == -1 && parent_name.IndexOf(frame.name) == -1)
                        {
                            Destroy(frame);
                        }
                    }
                }
                TFTree.RemoveAll(frame => frame == null);
                // arrows.RemoveAll(arrow => arrow == null);
                // Create TF frames that have not been added
                foreach (string new_frame in frame_name)
                {
                    if (!TFTree.Find(frame => frame.name == new_frame))
                    {
                        var tf_clone = Instantiate(tf_prefab, transform.position, UnityEngine.Quaternion.identity);
                        tf_clone.tag = "TF";
                        tf_clone.name = new_frame;
                        tf_clone.transform.parent = transform;
                        TFTree.Add(tf_clone);
                        // Just add arrows for child frames
                        // var arrow_clone = Instantiate(arrowPrefab, transform.position, UnityEngine.Quaternion.identity);
                        // arrow_clone.name = new_frame + "_arrow";
                        // arrow_clone.transform.parent = transform;
                        // arrows.Add(arrow_clone);
                        // Set the text to show name of TF
                        tf_clone.transform.GetChild(1).GetComponent<TextMeshPro>().text = tf_clone.name;

                        // Turn off TF Display
                        tf_clone.transform.GetChild(1).gameObject.SetActive(false);
                        tf_clone.transform.GetChild(0).gameObject.SetActive(false);
                    }
                }
                // Loop through the parent frames to create frames that are not in the frame list
                foreach (string parent_frame in parent_name)
                {
                    if (!TFTree.Find(frame => frame.name == parent_frame))
                    {
                        var tf_clone = Instantiate(tf_prefab, transform.position, UnityEngine.Quaternion.identity);
                        tf_clone.tag = "TF";
                        tf_clone.name = parent_frame;
                        tf_clone.transform.parent = transform;
                        tf_clone.transform.localPosition = new UnityEngine.Vector3(0, 0, 0);
                        tf_clone.transform.localRotation = new UnityEngine.Quaternion(0, 0, 0, 1);
                        TFTree.Add(tf_clone);
                        // Set the text to show name of TF
                        tf_clone.transform.GetChild(1).GetComponent<TextMeshPro>().text = tf_clone.name;

                        // Turn off TF Display
                        tf_clone.transform.GetChild(1).gameObject.SetActive(false);
                        tf_clone.transform.GetChild(0).gameObject.SetActive(false);
                    }
                }
                int arrow_index = 0;
                // Create the TF tree
                foreach (GameObject frame in TFTree)
                {
                    int parent_idx = frame_name.IndexOf(frame.name);
                    // If the parent is found, then setParent appropriately, otherwise, just set parent to TFDisplay
                    if (parent_idx > -1)
                    {
                        frame.transform.parent = TFTree.Find(t => t.name == parent_name[parent_idx]).transform;
                        frame.transform.localPosition = parent_to_child_tf[parent_idx].translation.rosMsg2Unity().Ros2Unity();
                        frame.transform.localRotation = parent_to_child_tf[parent_idx].rotation.rosMsg2Unity().Ros2Unity();
                        /*
                        if (arrow_index < arrows.Count)
                        {
                            arrows[arrow_index].transform.parent = TFTree.Find(t => t.name == parent_name[parent_idx]).transform;
                            arrows[arrow_index].GetComponent<ArrowManipulation>().SetArrow(parent_to_child_tf[parent_idx].translation.rosMsg2Unity().Ros2Unity(), UnityEngine.Vector3.zero);
                        }
                        */
                    }
                    arrow_index++;
                }
                arrow_index = 0;
            }
            yield return new WaitForSeconds(0.1f);
        }
    }
    public List<GameObject> GetTFTree()
    {
        return TFTree;
    }
}

