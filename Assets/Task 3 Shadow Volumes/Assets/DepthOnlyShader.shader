/* This shader only renders to the depth buffer, but does not write anything
 * to the frame buffer
 * 
 * PROD321 - Interactive Computer Graphics and Animation 
 * Copyright 2021, University of Canterbury
 * Written by Adrian Clark
 */

Shader "Custom/DepthOnlyShader" {
 
    SubShader {
        // Render the mask after regular geometry, but before masked geometry and
        // transparent things.
 
        Tags {"Queue" = "Geometry" }
 
        // Don't draw in the RGBA channels; just the depth buffer
 
        ColorMask 0
        ZWrite On
 
        // Do nothing specific in the pass:
 
        Pass {}
    }
}