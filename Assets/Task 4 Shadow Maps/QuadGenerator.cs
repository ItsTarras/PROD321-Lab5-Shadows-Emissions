using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* This class generates a quad of a defined size and mesh resolution
 * which is rendered as triangles. 
 *
 * PROD321 - Interactive Computer Graphics and Animation 
 * Copyright 2021, University of Canterbury
 * Written by Adrian Clark
 */

public class QuadGenerator : MonoBehaviour
{
    // Defines the resolution of the mesh (number of rows and columns)
    public int Rows = 2, Columns = 2;

    // Defines the size of the mesh in world units
    public float Width = 1, Height = 1;

    // Defines the mesh offset
    public Vector2 offset = new Vector2(0.5f, 0.5f);

    // Defines the material to use on the quad
    public Material quadMaterial;

    // Start is called before the first frame update
    void Start()
    {
        // Calculate the size of each row
        float xScale = Width / (Rows - 1);

        // Calculate the size of each column
        float yScale = Height / (Columns - 1);

        // Create a list to store our vertices
        List<Vector3> vertices = new List<Vector3>();

        // Loop through the number of rows and columns
        for (int y = 0; y < Rows; y++)
            for (int x = 0; x < Columns; x++)
                // Generate vertices based on the size and add to
                // our vertex list
                vertices.Add(new Vector3(x * xScale - offset.x, y * yScale - offset.y, 0));

        // Create a list to store our triangles
        List<int> triangles = new List<int>();

        // Loop through the number of rows -1 and columns -1
        for (int y = 0; y < Rows - 1; y++)
        {
            for (int x = 0; x < Columns - 1; x++)
            {
                // Calculate the vertex indices for the current vertex
                // and 3 neighbouring vertices which will make up our
                // triangle
                int p0 = y * Columns + x;
                int p1 = y * Columns + x + 1;
                int p2 = (y + 1) * Columns + x + 1;
                int p3 = (y + 1) * Columns + x;

                // Create the two triangles which make each element in the quad

                // Triangle p2->p1->p0
                triangles.Add(p2);
                triangles.Add(p1);
                triangles.Add(p0);

                // Triangle p3->p2->p0
                triangles.Add(p3);
                triangles.Add(p2);
                triangles.Add(p0);
            }
        }

        // Create our mesh object
        Mesh mesh = new Mesh();

        // Assign the vertices
        mesh.vertices = vertices.ToArray();

        // Set the indicies with meshtopology set to triangles
        // And recalculate normals to get correct lighting
        mesh.SetIndices(triangles.ToArray(), MeshTopology.Triangles, 0);
        mesh.RecalculateNormals();

        // Create a meshfilter component on our gameobject
        MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>();
        // Assign the mesh to it
        meshFilter.mesh = mesh;

        // Create a meshrendering component on our gameobject
        MeshRenderer meshRenderer = gameObject.AddComponent<MeshRenderer>();

        // Use the defines material
        meshRenderer.material = quadMaterial;
    }
}
