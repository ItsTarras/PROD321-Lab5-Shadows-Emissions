using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* This class captures the contents of a depth buffer of a camera
 * and stores it in the variable "cameraTexture". There's a few
 * steps to making this work, so all the functionality is included
 * as a specific class
 *
 * This class should be attached to a camera that we want to get
 * the depth buffer of
 *
 * PROD321 - Interactive Computer Graphics and Animation 
 * Copyright 2021, University of Canterbury
 * Written by Adrian Clark
 */

public class CameraDepthBufferGrabber : MonoBehaviour
{
    // The camera we are rendering the depth of
    public Camera renderingCamera { get; set; }

    // The material with the depth rendering shader
    public Material depthMaterial;

    // The Render texture that our camera will render too
    public RenderTexture renderTexture;

    // The Texture2D to save our captured depth buffer into
    public Texture2D cameraTexture { get; private set; }

    private void Start()
    {
        // Try get the rendering camera attached to this GameObject
        renderingCamera = GetComponent<Camera>();

        // If there is none
        if (renderingCamera == null) {
            // Add a camera
            renderingCamera = gameObject.AddComponent<Camera>();

            // Set it's near and far clip plane
            renderingCamera.nearClipPlane = 0.01f;
            renderingCamera.farClipPlane = 20;
        }

        // Ensure that the camera we're getting the depth buffer from
        // has the depth texture mode enabled
        renderingCamera.depthTextureMode = DepthTextureMode.Depth;

        // If our render texture hasn't been set
        if (renderTexture == null)
            // Create a temporary one
            renderTexture = RenderTexture.GetTemporary(256, 256, 32, RenderTextureFormat.ARGB32);

        // Set the camera to render to our render texture
        renderingCamera.targetTexture = renderTexture;

        // If our depth material hasn't been set
        if (depthMaterial==null)
            // Create a new one with the Custom/Depth Render Shader shader
            depthMaterial = new Material(Shader.Find("Custom/DepthRenderShader"));

        // Create a new Texture2D the size of our render texture
        cameraTexture = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.ARGB32, false);
    }

    // On render image gets called when rendering is complete, and we are copying
    // the frame buffer to the screen
    private void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        // If we have a depth material assigned
        if (depthMaterial != null)
        {
            // Copy from the source buffers to the screen using the depth
            // Material - this will just render out our depth buffer instead
            // of the colour buffer
            Graphics.Blit(src, dest, depthMaterial);
        }
        else
        {
            // If there's no depth material then just copy from the frame buffer
            // to the screen
            Graphics.Blit(src, dest);
        }

    }

    // OnPostRender is called after this camera has finished it's rendering
    private void OnPostRender()
    {
        // If our camera texture exists but is not the same size as the render texture
        if (cameraTexture != null && (cameraTexture.width != renderTexture.width || cameraTexture.height != renderTexture.height))
        {
            // Destroy it
            Destroy(cameraTexture);
            cameraTexture = null;
        }

        // If we don't have a camera texture
        if (cameraTexture == null)
            // Create one the size of our render texture
            cameraTexture = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.ARGB32, false);

        // Store the current render texture
        RenderTexture current = RenderTexture.active;

        // Load the render texture that the camera is rendering too
        RenderTexture.active = renderTexture;

        // Read the pixels from the camera render texture into our Texture2D
        cameraTexture.ReadPixels(new Rect(0, 0, cameraTexture.width, cameraTexture.height), 0, 0);

        // Apply the changed pixels to our Texture 2D
        cameraTexture.Apply(false);

        // Restore the previously active render texture
        RenderTexture.active = current;
    }
}
