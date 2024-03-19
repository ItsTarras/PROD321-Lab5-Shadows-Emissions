using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* This class creates a shadow map for an object relative
 * to a light position.
 * 
 * Using a camera attached to a shadow casting light source, a
 * shadow map is created by just rendering out the depth buffer. 
 * In this script, we loop through each vertex in our shadow 
 * receiving object, and calculate the distance from the vertex to 
 * the light source. If this distance is greater than the distance value 
 * stored in the Shadow map based on where that vertex would appear from 
 * the point of view of the light source, then there is another 
 * piece of geometry between that vertex and the light source - 
 * therefore that vertex is in shadow and we the vertex should be shaded.
 * 
 * Shadow maps implicitly support multiple shadow casting objects, as every
 * object in the scene will appear in the shadow map by default. Shadow receiving
 * objects need to be defined explicitly, as shadows are calculated on a per
 * vertex level, rather than a per pixel level. 
 * 
 * This implementation doesn't support multiple shadow casting light sources, 
 * and your job in this task is to add support for this.
 * 
 * As an extra for experts, try and render the pixels darker when they appear
 * in multiple shadow volumes, rather than just the same colour regardless of
 * how many light sources you have
 * 
 * PROD321 - Interactive Computer Graphics and Animation 
 * Copyright 2023, University of Canterbury
 * Written by Adrian Clark
 */

public class ShadowMap : MonoBehaviour
{
    /*****
     * TODO: Modify the code to support multiple shadow casting light sources
     * You will need to keep track of the different shadow casting light sources
     * and the depth buffer grabbers attached to those cameras
     *****/

    //The light source which will be casting the shadow
    public List<Light> lightSources = new List<Light>();

    // The Camera Depth Buffer Grabber that we will get the depth
    // map from our light source camera from
    CameraDepthBufferGrabber lightCameraDepthBufferGrabber;

    public List<CameraDepthBufferGrabber> depthBuffers = new List<CameraDepthBufferGrabber>();

    // An array of GameObjects which should receiver shadows in our scene
    public GameObject[] shadowReceivers;

    // The multiplier for our shadow - by default the pixel
    // Should be half the brightness if it is in shadow
    public float shadowMultiplier = .5f;

    // The error margin is used to approximate whether a pixel is
    // closer or further away
    public float errorMargin = 0.0015f;

    // Start is called before the first frame update
    void Start()
    {
        /*****
         * TODO: Modify the code to support multiple shadow casting light sources
         * You will need to create shadow map depth buffer grabbers for each shadow 
         * casting light source
         *****/

        //Create a new ShadowMapCamera gameobject as a child of our lightSource
        foreach (Light lightSource in lightSources)
        {
            GameObject ShadowMapCameraGO = new GameObject("ShadowMapCamera");
            ShadowMapCameraGO.transform.SetParent(lightSource.transform, false);
            //And add a CameraDepthBufferGrabber component to it
            lightCameraDepthBufferGrabber = ShadowMapCameraGO.AddComponent<CameraDepthBufferGrabber>();
            depthBuffers.Add(lightCameraDepthBufferGrabber);
        }
    }

    // Update is called once per frame
    void Update()
    {
        // Loop through every shadow receiver mesh filter
        foreach (GameObject shadowReceiverGO in shadowReceivers)
        {
            // Try get the mesh filter attached to the shadowReceiver game object
            MeshFilter shadowReceiver = shadowReceiverGO.GetComponent<MeshFilter>();
            if (shadowReceiver != null)
            {
                // Get the attached mesh
                Mesh m = shadowReceiver.mesh;

                // And an array of vertices in that mesh
                Vector3[] v = m.vertices;

                // Create an array of colours to apply to the mesh vertices
                // And set them all to white by default
                Color[] c = new Color[v.Length];
                for (int i = 0; i < v.Length; i++) c[i] = Color.white;

                /*****
                 * TODO: Modify the code to support multiple shadow casting light sources
                 * You will need to loop over each shadow casting light sources' depth buffer
                 * and calculate whether each vertex is in shadow for that light source
                 *****/
                foreach (CameraDepthBufferGrabber lightCameraDepthBufferGrabber in depthBuffers)
                {
                    // Get the depth texture from the depth buffer grabber
                    Texture2D depthTexture = lightCameraDepthBufferGrabber.cameraTexture;

                    // Loop through each vertex
                    for (int i = 0; i < v.Length; i++)
                    {
                        // Transform the vertex from model space to world space
                        Vector3 vertexInWorldSpace = shadowReceiver.transform.TransformPoint(v[i]);

                        // Calculate the vertex's position relative to the light camera's viewport
                        Vector3 vertexInLightViewportSpace = lightCameraDepthBufferGrabber.renderingCamera.WorldToViewportPoint(vertexInWorldSpace);

                        // InverseLerp the vertex's z coordinate in viewport space to calculate
                        // it's actual distance from the light in world space
                        float vertexWorldDistanceFromLight = Mathf.InverseLerp(lightCameraDepthBufferGrabber.renderingCamera.nearClipPlane, lightCameraDepthBufferGrabber.renderingCamera.farClipPlane, vertexInLightViewportSpace.z);

                        // Use the position of the vertex in the lights viewport space to
                        // look up what value was actually stored in the depth texture
                        // at that pixel
                        float texWorldDepthValue = depthTexture.GetPixelBilinear(vertexInLightViewportSpace.x, vertexInLightViewportSpace.y).r;


                        /*****
                         * TODO: Extra for Experts: Here we calculate whether a pixel is in
                         * shadow or not. At the moment it is a binary operation (the pixel
                         * is in full shadow, or it is not in shadow), try and make it so
                         * the pixel will be shaded more the more shadow casting light sources
                         * it appears in
                         *****/

                        // If the vertex is further away from the light than the pixel we
                        // stored at this point (+ the error margin)
                        if (vertexWorldDistanceFromLight > texWorldDepthValue + errorMargin)
                            // If must be in shadow - so set the vertex's colour to
                            // the value of the shadow multipler
                            c[i] = new Color(shadowMultiplier, shadowMultiplier, shadowMultiplier);
                    }
                }
                // Once we've looped through every vertex in our mesh filter
                // update the mesh vertex colours
                m.colors = c;

                // And update the meshfilter to use this updated mesh
                shadowReceiver.mesh = m;

            }
        }
    }

}
