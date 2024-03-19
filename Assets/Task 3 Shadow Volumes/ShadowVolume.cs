using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/* This class creates a shadow volume for an shadow casting object relative
 * to a shadow casting light. It uses this shadow volume to calculate
 * the parts of the scene which should be in shadow, using the
 * following process:
 * 
 * 1)	Render the front faces of the Shadow Volume (with rendering set to depth only so there are no visible pixels)
 * 2)	Render the back faces of the Shadow Volume (with rendering set to depth only so there are no visible pixels)
 * 3)	Update the stencil buffer based on what’s visible in the front and back faces of the shadow volumes.
 * 4)	Render the Frame buffer with the un-shadowed scene.
 * 5)	Use the stencil buffer to determine what pixels should be in shadow.
 * 
 * The stencil buffer has a value of 1 ADDED to each pixel which has the front
 * facing shadow volume visible, and a value of 1 SUBTRACTED from each pixel
 * which has the rear facing shadow volume visible. If the final value in
 * the stencil buffer is >=1, then that pixel is inside 1 (or more) shadow frustums,
 * so should be in shadow.
 * 
 * Your job is to extend the code to support more than 1 shadow casting object 
 * and more than 1 shadow casting light source:
 * 
 * For multiple shadow casting objects: Each shadow casting object should render 
 * to the stencil buffer before we render the colour buffer – i.e., for each shadow 
 * casting object, repeat steps 1-3 before continuing to step 4 and 5.
 * 
 * For multiple shadow casting light sources: Each shadow casting light source will 
 * need its own shadow frustum, and this shadow frustum will need to be applied for 
 * all shadow casting objects – i.e., for each shadow casting light source, repeat 
 * steps 1-3 yet again before continuing to step 4 and 5.
 *  
 * As an extra for experts task, try and render the pixels darker when they appear
 * in multiple shadow volumes, rather than just the same colour regardless of
 * how many shadow volumes they appear in.
 *  
 * Note: The shadows produced aren't perfect, as the code does not calculate the triangles 
 * required to make up the near and far planes for the shadow volumes. As a result, there 
 * are sometimes some strange effects at the edge of the shadows

 * PROD321 - Interactive Computer Graphics and Animation 
 * Copyright 2023, University of Canterbury
 * Written by Adrian Clark
 */

public class ShadowVolume : MonoBehaviour
{
    [Header("Shadow Game Objects")]
    // The camera which we are using for rendering
    public Camera renderingCamera;

    /***** 
     * TODO: Modify the code to support multiple shadow casting light sources
     * You will need to keep track of all the shadow casting light sources
     * 
     * TODO: Modify the code to support multiple shadow casting objects
     * You will need to keep track of all the shadow casting objects and their
     * occlusion frustrums
     * 
     *****/

    // The transform for the light which is casting our shadow
    public List<Transform> lightTransforms = new List<Transform>();

    // The mesh filter of our shadow casting object
    public List<MeshFilter> shadowCastingObjectMeshFilter = new List<MeshFilter>();

    // The shadow frustum created by our shadow casting object relative to the light
    //OcclusionFrustum shadowFrustum;

    List<OcclusionFrustum> shadowFrustums = new List<OcclusionFrustum>();

    // The list of all objects which are in the scene
    List<Renderer> RenderersInScene;

    [Header("Shadow Volume")]
    // The mesh filters for our front facing and rear facing
    // shadow volumes
    public MeshFilter shadowVolumeFrontMeshFilter;
    public MeshFilter shadowVolumeBackMeshFilter;

    [Header("Shadow Rendering")]
    // We render all our geometry using a depth only shader
    // when rendering our shadow volume, so that the back side
    // of our shadow volume is properly occluded, and we can determine
    // pixels that are in shadow
    public Material DepthOnlyMaterial;

    // The colour multiplier for pixels which are in shadow
    // i.e. make this colour half the brightness
    public float shadowMultiplier = .5f;
   

    [Header("UI")]
    // The UI Container for our raw (unshadowed) image
    public RawImage cameraRawImage;

    // The UI Container for our front facing shadow volume
    public RawImage shadowFrontRawImage;

    // The UI Container for our rear facing shadow volume
    public RawImage shadowBackRawImage;

    // The UI Container for the pixels which should be shadowed
    // (i.e. pixels which appear in the front volume but not rear volume)
    public RawImage shadowOnlyRawImage;

    // The UI Container for our final shadowed image
    public RawImage finalRawImage;

    // We will use this temporary render texture for storing
    // renders from the camera along the way
    RenderTexture TmpRenderTexture;

    // The texture to sture our scene before shadows are applied
    Texture2D UnshadowedTexture;

    // The texture to store the front facing parts of our shadow volume
    Texture2D ShadowVolumeFrontTexture;

    // The texture to store the rear facing parts of our shadow volume
    Texture2D ShadowVolumeBackTexture;

    // Textures to store our pixels which should be shadowed, and our
    // final shadowed image in
    Texture2D shadowTexture, finalTexture;

    // Start is called before the first frame update
    void Start()
    {
        // Get a list of all visible objects in the scene. We will
        // update these objects materials to "DepthOnly" when rendering our
        // shadow volume
        RenderersInScene = new List<Renderer>(FindObjectsOfType<Renderer>());



        // Remove the front and back facing shadow volume renderers
        // themselves from the list of visible objects
        Renderer frontRenderer = shadowVolumeFrontMeshFilter.GetComponent<Renderer>();
        if (frontRenderer !=null && RenderersInScene.Contains(frontRenderer)) RenderersInScene.Remove(frontRenderer);
        Renderer backRenderer = shadowVolumeBackMeshFilter.GetComponent<Renderer>();
        if (backRenderer != null && RenderersInScene.Contains(backRenderer)) RenderersInScene.Remove(backRenderer);

        // Get a temporary render texture the size of the screen and 32 bits (i.e. containing alpha)
        TmpRenderTexture = RenderTexture.GetTemporary(Screen.width, Screen.height, 32);

        // Create the textures for the Unshadowed scene (no alpha), and the front and rear
        // facing volumes (with alpha)
        UnshadowedTexture = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
        ShadowVolumeFrontTexture = new Texture2D(Screen.width, Screen.height, TextureFormat.ARGB32, false);
        ShadowVolumeBackTexture = new Texture2D(Screen.width, Screen.height, TextureFormat.ARGB32, false);

        // Create our output textures
        finalTexture = new Texture2D(Screen.width, Screen.height);
        shadowTexture = new Texture2D(Screen.width, Screen.height);

        /*****
         * TODO: Update to calculate occlusion frustums for all our shadow casting objects
         * and add them to our list of shadowFrustums
         *****/


        // Create our occlusion frustum for our shadow casting object
        foreach (MeshFilter filter in shadowCastingObjectMeshFilter)
        {
            if (filter != null)
            {
                OcclusionFrustum shadowFrustum = new OcclusionFrustum(filter);
                shadowFrustums.Add(shadowFrustum);
            }
        }
    }

    private void OnDestroy()
    {
        // If our temporary render texture has been allocated, release it
        if (TmpRenderTexture != null)
            RenderTexture.ReleaseTemporary(TmpRenderTexture);

        // If our unshadowed, front and rear facing volumes, and output
        // textures have been created, destroy them
        if (UnshadowedTexture != null)
            Destroy(UnshadowedTexture);
        if (ShadowVolumeFrontTexture != null)
            Destroy(ShadowVolumeFrontTexture);
        if (ShadowVolumeBackTexture != null)
            Destroy(ShadowVolumeBackTexture);
        if (finalTexture != null)
            Destroy(finalTexture);
        if (shadowTexture != null)
            Destroy(shadowTexture);
    }

    private void Update()
    {
        // Create our stencil buffer
        int[] stencilBuffer = new int[Screen.width * Screen.height];

        /***** 
         * TODO: Modify the code to support multiple shadow casting objects
         * You will need to render the front and back faces of the shadow volume
         * and update the stencil buffer (Steps 1-3) for each of the shadow casting 
         * objects
         * 
         * TODO: Modify the code to support multiple shadow casting light sources
         * You will need to render the front and back faces of the shadow volume
         * and update the stencil buffer (Steps 1-3) for each of the shadow casting 
         * light sources
         * 
         * STEPS 1-3 START HERE
         * 
         *****/
        foreach (OcclusionFrustum shadowFrustum in shadowFrustums)
        {

            // Store a list of the vertices and triangles which make up our
            // Shadow volume
            List<Vector3> volumeVertices = new List<Vector3>();
            List<int> volumeTriangles = new List<int>();

            // We will make the length of the shadow volume 1000 units arbitrarily
            float length = 1000;

            // Make a backup of all the original rendered objects materials
            // And then change them for a depth only material
            List<Material> originalMaterials = new List<Material>();
            for (int i = 0; i < RenderersInScene.Count; i++)
            {
                originalMaterials.Add(RenderersInScene[i].material);
                RenderersInScene[i].material = DepthOnlyMaterial;
            }

            // Calculate the frustum which defines the shadow volume relative
            // to our light transforms position



            /*****
             * STEP 1: Render the Front Faces of the Shadow Volume
             *****/
            foreach (Transform lightTransform in lightTransforms)
            {
                shadowFrustum.CalcFrustum(lightTransform.position, length, ref volumeVertices, ref volumeTriangles);
            }
            // Destroy the mesh attached to our Front Mesh Filter
            if (shadowVolumeFrontMeshFilter.mesh != null)
                Destroy(shadowVolumeFrontMeshFilter.mesh);

            // Create a new mesh for the front facing shadow volume and
            // assign it to our shadow volume front facing mesh filter
            Mesh meshFront = new Mesh();
            meshFront.vertices = volumeVertices.ToArray();
            meshFront.triangles = volumeTriangles.ToArray();
            meshFront.RecalculateNormals();
            shadowVolumeFrontMeshFilter.mesh = meshFront;

            // Render the Front Facing Shadow Volume
            shadowVolumeFrontMeshFilter.gameObject.SetActive(true);
            shadowVolumeBackMeshFilter.gameObject.SetActive(false);
            RenderCameraToTexture2D(renderingCamera, ref TmpRenderTexture, ref ShadowVolumeFrontTexture, true);
            shadowFrontRawImage.texture = ShadowVolumeFrontTexture;




            /*****
             * STEP 2: Render the Back Faces of the Shadow Volume
             *****/

            // Destroy the mesh attached to our Back Mesh Filter
            if (shadowVolumeBackMeshFilter.mesh != null)
                Destroy(shadowVolumeBackMeshFilter.mesh);

            // Reverse the order of triangle indices for our rear facing
            // shadow volume, and create a mesh for that too, and then
            // assign it to our shadow volume back facing mesh filter
            volumeTriangles.Reverse();
            Mesh meshBack = new Mesh();
            meshBack.vertices = volumeVertices.ToArray();
            meshBack.triangles = volumeTriangles.ToArray();
            meshBack.RecalculateNormals();
            shadowVolumeBackMeshFilter.mesh = meshBack;

            // Render the Back Facing Shadow Volume
            shadowVolumeFrontMeshFilter.gameObject.SetActive(false);
            shadowVolumeBackMeshFilter.gameObject.SetActive(true);
            RenderCameraToTexture2D(renderingCamera, ref TmpRenderTexture, ref ShadowVolumeBackTexture, true);
            shadowBackRawImage.texture = ShadowVolumeBackTexture;

            // Revert the original materials for the rendered objects 
            for (int i = 0; i < RenderersInScene.Count; i++)
                RenderersInScene[i].material = originalMaterials[i];




            /*****
             * STEP 3: Update the Stencil Buffer
             *****/

            // Get our pixel buffers for the front and rear facing shadow volumes
            Color32[] cShadowFront = ShadowVolumeFrontTexture.GetPixels32();
            Color32[] cShadowBack = ShadowVolumeBackTexture.GetPixels32();

            // Loop over all pixels in our shadow buffers
            int idx1 = 0;
            for (int y = 0; y < Screen.height; y++)
            {
                for (int x = 0; x < Screen.width; x++)
                {
                    // If the front facing buffer is visible for this pixel
                    // Add one to the stencil buffer
                    if (cShadowFront[idx1].a > 0)
                        stencilBuffer[idx1]++;

                    // If the rear facing buffer is visible for this pixel
                    // Subtract one from the stencil buffer
                    if (cShadowBack[idx1].a > 0)
                        stencilBuffer[idx1]--;

                    idx1++;
                }
            }
        }


        /*****
         * STEPS 1-3 END HERE
         *****/

        
        /***** 
         * STEP 4: Render the Scene
         *****/

        // Render the Scene with No Shadows
        shadowVolumeFrontMeshFilter.gameObject.SetActive(false);
        shadowVolumeBackMeshFilter.gameObject.SetActive(false);
        RenderCameraToTexture2D(renderingCamera, ref TmpRenderTexture, ref UnshadowedTexture, false);
        cameraRawImage.texture = UnshadowedTexture;



        /***** 
         * STEP 5: Use the Stencil Buffer to Shadow pixels
         *****/

        // If our final texture is not the size of the screen
        if (finalTexture.width != Screen.width || finalTexture.height != Screen.height)
        {
            // Destroy it
            Destroy(finalTexture);
            // Create a new final texture which is the size of the screen
            finalTexture = new Texture2D(Screen.width, Screen.height);
        }

        // If our shadow texture is not the size of the screen
        if (shadowTexture.width != Screen.width || shadowTexture.height != Screen.height)
        {
            // Destroy it
            Destroy(shadowTexture);
            // Create a new shadow texture which is the size of the screen
            shadowTexture = new Texture2D(Screen.width, Screen.height);
        }

        // Get all the pixels of our various textures
        Color32[] cFinal = finalTexture.GetPixels32();
        Color32[] cShadow = shadowTexture.GetPixels32();
        Color32[] cUnshadowed = UnshadowedTexture.GetPixels32();
        
        // Loop over each pixel
        int idx2 = 0;
        for (int y = 0; y < finalTexture.height; y++)
        {
            for (int x = 0; x < finalTexture.width; x++)
            {
                // Grab the unshadowed scene's pixel and store
                // it in finalPixelColour
                Color finalPixelColour = cUnshadowed[idx2];

                // Set the pixel in shadowed texture to black by default
                cShadow[idx2] = Color.black;

                /*****
                 * TODO: Extra for Experts: Here we calculate whether a pixel is in
                 * shadow or not. At the moment it is a binary operation (the pixel
                 * is in full shadow, or it is not in shadow), try and make it so
                 * the pixel will be shaded more the more shadow volumes it is inside
                 *****/

                // If the value in the stencil buffer is greater than 0, it is in shadow
                if (stencilBuffer[idx2] > 0)
                {
                    // Multiply the finalPixelColour colour by the shadow multiplier
                    finalPixelColour *= shadowMultiplier;
                    finalPixelColour.a = 255;
                    // Set the pixel alpha in the shadow image to 255
                    cShadow[idx2].a = 255;
                }
                else
                {
                    // If the value in the stencil buffer is less than or equal to 0
                    // it is not in shadow
                    // Set the pixel alpha in the shadow image to 0
                    cShadow[idx2].a = 0;
                }

                // Store the finalPixelColour into the final texture
                cFinal[idx2] = finalPixelColour;

                // Update the index
                idx2++;
            }
        }

        //Update and assign the shadow texture
        shadowTexture.SetPixels32(cShadow);
        shadowTexture.Apply();
        shadowOnlyRawImage.texture = shadowTexture;

        //Update and assign the final texture
        finalTexture.SetPixels32(cFinal);
        finalTexture.Apply();
        finalRawImage.texture = finalTexture;

    }



    //This function renders our camera into a Texture2D via a Render Texture
    //It also makes sure the render texture and Texture2D are the right size and
    //If not sets them to be the right size
    //We can also specify whether the texture should include an alpha channel
    public void RenderCameraToTexture2D(Camera camera, ref RenderTexture renderTexture, ref Texture2D outputTexture, bool useAlpha)
    {
        // If the render texture is not the same size as the screen
        if (renderTexture.width != Screen.width || renderTexture.height != Screen.height)
        {
            // Release it and create a new one the correct size
            RenderTexture.ReleaseTemporary(renderTexture);
            renderTexture = RenderTexture.GetTemporary(Screen.width, Screen.height, 32);
        }

        // Set the camera to render into the renderTexture
        camera.targetTexture = renderTexture;

        // Render the scene from the camera's perspective
        camera.Render();

        // Set the camera to render back to the screen
        camera.targetTexture = null;


        // If the output texture is not the same size as the render texture
        if (outputTexture.width != renderTexture.width || outputTexture.height != renderTexture.height)
        {
            // Destroy it
            Destroy(outputTexture);

            // Create a new Texture2D the size of the renderTexture (either in ARGB or RGB format if alpha is enabled or not respectively)
            outputTexture = new Texture2D(renderTexture.width, renderTexture.height, useAlpha? TextureFormat.ARGB32:TextureFormat.RGB24, false);
        }

        // Store a reference to the currently active render texture
        RenderTexture previousRenderTexture = RenderTexture.active;

        // Set the current render texture to be the render texture we just rendered our camera into
        RenderTexture.active = renderTexture;

        // Read the pixels from our render texture into the output texture
        outputTexture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);

        // Apply the changes to our output texture
        outputTexture.Apply(false);

        // Reset the active render texture to what it was before
        RenderTexture.active = previousRenderTexture;

    }
}
