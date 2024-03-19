using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/* This class acts as a base class for a renderering pipeline 
 * capable of rendering a model using depth buffered, culled, 
 * Blinn-Phong lit texels. Implementing classes will still
 * need to handle the specifics of rendering.
 *
 * PROD321 - Interactive Computer Graphics and Animation 
 * Copyright 2023, University of Canterbury
 * Written by Adrian Clark
 */
public class PROD321RendererBaseClass : MonoBehaviour
{
    // Reference to the camera which our scene will be rendered from
    public Camera renderingCamera;

    // Set the clear colour for our frame buffer to green
    public Color BackgroundClearColour = new Color(.19f, .47f, .36f, 1);

    // The size of the frame buffer we will create
    public Vector2Int frameBufferSize = new Vector2Int(600, 600);

    // We will store our frame buffer in an array of colours
    protected Color[] frameBuffer;

    // We will store our depth buffer in an array of floats
    protected float[] depthBuffer;

    // The frame buffer Texture2D
    protected Texture2D frameBufferTexture;

    // The Raw Image UI Element which we will place our frame buffer texture in
    public RawImage frameBufferTextureUIContainer;

    // The light sources in the scene
    public Light[] lightSources;

    // Start is called before the first frame update
    protected virtual void Start()
    {
        // Create our frame buffer and texture
        frameBuffer = new Color[frameBufferSize.x * frameBufferSize.y];
        frameBufferTexture = new Texture2D(frameBufferSize.x, frameBufferSize.y);

        // Set the UI container's texture to be our frame buffer texture
        frameBufferTextureUIContainer.texture = frameBufferTexture;

        // Create our depth buffer and texture
        depthBuffer = new float[frameBufferSize.x * frameBufferSize.y];
    }

    // This function clears our frame buffer and depth buffer
    protected void ClearFrameAndDepthBuffer(Color clearColour, float depthClearValue)
    {
        // Loop through each frame buffer pixel and set it's color to the clear colour
        for (int i = 0; i < frameBuffer.Length; i++) frameBuffer[i] = clearColour;

        // Loop through each depth buffer pixel and set it's value to the clear value
        for (int i = 0; i < depthBuffer.Length; i++) depthBuffer[i] = depthClearValue;
    }

    // This function copies our frame buffer to the textures to display
    protected void CopyFrameBufferToTexture()
    {
        // The frame buffer can just be copied directly to the texture
        frameBufferTexture.SetPixels(frameBuffer);
        frameBufferTexture.Apply();
    }


    // This function calculates the model to world matrix for a transform
    protected Matrix4x4 CalculateModelToWorldMatrix(Transform initialTransform)
    {

        // This is where we will store the model to world matrix as we traverse
        // the hierarchy. Initialise it to the identity matrix
        Matrix4x4 modelToWorldMatrix = Matrix4x4.identity;

        // Start at the meshFilterToRenders transform
        Transform currentTransform = initialTransform;

        // As long as the current transform isn't null
        while (currentTransform != null)
        {
            // Multiply the existing modelToWorldMatrix by the current transform's
            // local position, local rotation and local scale (stored in matrix form)
            modelToWorldMatrix = Matrix4x4.TRS(currentTransform.localPosition, currentTransform.localRotation, currentTransform.localScale) * modelToWorldMatrix;

            // Update the current transform to it's parent - once we reach
            // root node, this will set current transform to null, exiting out
            // of our loop
            currentTransform = currentTransform.parent;
        }

        // return the calculated matrix
        return modelToWorldMatrix;
    }


    // This function calculates the Model to View Matrix (or ModelViewMatrix)
    protected Matrix4x4 CalculateModelToViewMatrix(Matrix4x4 modelToWorldMatrix, Camera camera)
    {
        // The model view matrix is just the World to Camera matrix multiplied
        // by the model to world matrix
        return camera.worldToCameraMatrix * modelToWorldMatrix;
    }


    // This function calculates the perspective projection matrix for a camera
    protected Matrix4x4 CalculatePerspectiveProjectionMatrix(Camera camera)
    {
        // initialize our projection matrix to all zeros
        Matrix4x4 projectionMatrix = Matrix4x4.zero;

        // Get the camera vertical field of view in radians
        float rad_fovY = camera.fieldOfView * Mathf.Deg2Rad;

        // Manually calculate our projection matrix using the values:
        // a = vertical field of view, ar = aspect ratio
        // nz = near plane, fz = far plane

        // M_0,0 = 1 / ( ar * tan (a / 2) )
        projectionMatrix.m00 = 1f / (camera.aspect * Mathf.Tan(rad_fovY / 2));
        // M_1,1 = 1 / ( tan (a / 2) )
        projectionMatrix.m11 = 1f / (Mathf.Tan(rad_fovY / 2));
        // M_2,2 = -( (-nz - fz) / (nz - fz) )
        projectionMatrix.m22 = -((-camera.nearClipPlane - camera.farClipPlane) / (camera.nearClipPlane - camera.farClipPlane));
        // M_2,3 = (2 * fz * nz) / (nz - fz)
        projectionMatrix.m23 = (2 * camera.farClipPlane * camera.nearClipPlane) / (camera.nearClipPlane - camera.farClipPlane);
        // M_3,2 = -1
        projectionMatrix.m32 = -1;

        // Return the calculated projection matrix
        return projectionMatrix;
    }

    // This function transforms our model vertices into world space
    protected Vector3[] TransformModelVertsToWorldVerts(Vector3[] modelVerts, Matrix4x4 modelToWorldMatrix)
    {
        // Allocate space to store our world space vectors
        Vector3[] worldVerts = new Vector3[modelVerts.Length];
        // Loop through each vertex
        for (int i = 0; i < worldVerts.Length; i++)
            // Convert from model space to world space 
            worldVerts[i] = modelToWorldMatrix.MultiplyPoint3x4(modelVerts[i]);

        // return the world space vectors
        return worldVerts;
    }

    // This function transforms our model vertices into world space
    protected Vector3[] TransformModelNormalsToWorldNormals(Vector3[] modelNormals, Matrix4x4 modelToWorldMatrix)
    {
        // Allocate space to store our world space vectors
        Vector3[] worldNormals = new Vector3[modelNormals.Length];
        // Loop through each vertex
        for (int i = 0; i < worldNormals.Length; i++)
            // Convert from model space to world space 
            worldNormals[i] = modelToWorldMatrix.MultiplyVector(modelNormals[i]);

        // return the world space vectors
        return worldNormals;
    }

    // This function projects vertices in model space into homogeneous clip space
    protected Vector4[] ProjectModelSpaceVertices(Vector3[] modelSpaceVertices, Matrix4x4 modelToViewMatrix, Matrix4x4 projectionMatrix)
    {

        // Calculate the model to homogeneous clip space matrix
        // by multiplying the projection matrix by the model to view matrix
        Matrix4x4 modelToHomogeneousMatrix = projectionMatrix * modelToViewMatrix;

        // Create an array to store our projected vertices
        Vector4[] projectedVertices = new Vector4[modelSpaceVertices.Length];
        // Loop through each vertex
        for (int i = 0; i < modelSpaceVertices.Length; i++)
        {
            // Convert it from a 3D vertex to a 4D homogenous vertex (with w = 1)
            Vector4 homogeneousVertex = new Vector4(modelSpaceVertices[i].x, modelSpaceVertices[i].y, modelSpaceVertices[i].z, 1);

            // Project the 4D vertex to Homogeneous Space
            Vector4 projectedHomogeneousVertex = modelToHomogeneousMatrix * homogeneousVertex;

            // Convert the vertex from Homogeneous (4D) to Euclidean (3D)
            // We will retain the W coordinate, as this can be used for
            // other things (e.g. perspective correct texture mapping)
            projectedVertices[i] = new Vector4(projectedHomogeneousVertex.x / projectedHomogeneousVertex.w,
                projectedHomogeneousVertex.y / projectedHomogeneousVertex.w,
                projectedHomogeneousVertex.z / projectedHomogeneousVertex.w,
                projectedHomogeneousVertex.w);
        }

        // Return the array of projected vertices
        return projectedVertices;
    }

    // This function calculates the Blinn-Phong Lit colour of a pixel, based
    // on the pixel world space position and normal, material diffuse colour, 
    // specular colour and shininess, and camera world pos, and light source
    protected Color CalculateBlinnPhongPixelColour(
        Vector3 pixelWorldPosition, Vector3 pixelWorldNormal,
        Color diffuseColour, Color specularColour, float shininess,
        Vector3 cameraWorldPos, Light[] lightSources
        )
    {
        // Set the diffuse and specular values for this pixel to
        // black initially
        Color diffuseSum = Color.black;
        Color specularSum = Color.black;

        // Loop through each light source
        foreach (Light lightSource in lightSources)
        {
            // Calculate the light position and colour based on the light properties
            // LightW is set to 0 if the light is directional, and 1 otherwise
            float lightW; Vector3 lightPos; Color lightColour;
            float attenuation = 0;
            if (lightSource.type == LightType.Directional)
            {
                lightPos = -lightSource.transform.forward; lightW = 0;
                lightColour = lightSource.color * lightSource.intensity;
                attenuation = 1.0f;
            }
            else
            {
                lightPos = lightSource.transform.position; lightW = 1;
                // For non direcitonal lights, we'll figure out the light
                // Colour per pixel, based on the distance from the light
                // To the pixel
                lightColour = Color.black;
            }

            // Normalize the normal direction
            //float3 normalDirection = normalize(i.normal);
            Vector3 normalDirection = pixelWorldNormal.normalized;

            // Calculate the normalized view direction (the camera position -
            // the pixel world position) 
            //float3 viewDirection = normalize(_WorldSpaceCameraPos - i.posWorld.xyz);
            Vector3 viewDirection = (cameraWorldPos - pixelWorldPosition).normalized;

            // if our light source is not directional
            if (lightSource.type != LightType.Directional)
            {
                // Calculate the distance from the light to the pixel, and 1/distance
                //float3 vert2LightSource = _WorldSpaceLightPos0.xyz - i.posWorld.xyz;
                Vector3 vert2LightSource = lightPos - pixelWorldPosition;
                //float oneOverDistance = 1.0 / length(vert2LightSource);
                //float attenuation = lerp(1.0, oneOverDistance, _WorldSpaceLightPos0.w); //Optimization for spot lights. This isn't needed if you're just getting started.
                attenuation = 1.0f / vert2LightSource.magnitude;

                // Calculate the colour based on the distance and lightsource range
                if (vert2LightSource.magnitude < lightSource.range)
                    lightColour = (lightSource.color * lightSource.intensity);
            }

            // Calculate light direction
            //float3 lightDirection = _WorldSpaceLightPos0.xyz - i.posWorld.xyz * _WorldSpaceLightPos0.w;
            Vector3 lightDirection = lightPos - pixelWorldPosition * lightW;

            // Calculate Diffuse Lighting
            //float3 diffuseReflection = attenuation * _LightColor0.rgb * _Color.rgb * max(0.0, dot(normalDirection, lightDirection)); //Diffuse component
            Color diffuseReflection = attenuation * lightColour * diffuseColour * Mathf.Max(0, Vector3.Dot(normalDirection, lightDirection));

            /*float3 specularReflection;
            if (dot(i.normal, lightDirection) < 0.0) //Light on the wrong side - no specular
            {
                specularReflection = float3(0.0, 0.0, 0.0);
            }
            else
            {
                //Specular component
                specularReflection = attenuation * _LightColor0.rgb * _SpecColor.rgb * pow(max(0.0, dot(s.Normal, normalize (light.dir + viewDir))), _Shininess * 128);
            }*/

            // Calculate Specular reflection if the normal is pointing in the
            // Lights direction
            Color specularReflection;
            if (Vector3.Dot(normalDirection, lightDirection) < 0.0)
            {
                specularReflection = Color.black;
            }
            else
            {
                // Calculate the specular colour using Blinn-Phong lighting
                specularReflection = attenuation * lightColour * specularColour * Mathf.Pow(Mathf.Max(0.0f, Vector3.Dot(normalDirection, Vector3.Normalize(lightDirection + viewDirection))), shininess * 128);
            }

            diffuseSum += diffuseReflection;
            specularSum += specularReflection;
        }

        // Calculate Ambient Lighting
        //float3 ambientLighting = UNITY_LIGHTMODEL_AMBIENT.rgb * _Color.rgb; //Ambient component
        Color ambientLighting = RenderSettings.ambientLight * diffuseColour;

        // The final colour for this pixel is the sum of the ambient, diffuse and specular
        //float3 color = (ambientLighting + diffuseReflection) * tex2D(_Tex, i.uv) + specularReflection; //Texture is not applient on specularReflection
        return (ambientLighting + diffuseSum) + specularSum;
    }
}
