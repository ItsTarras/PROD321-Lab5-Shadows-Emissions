/* This shader renders out the depth of the geometry to
 * the frame buffer. We can apply this to the camera to
 * render out the entire scene as a depth buffer.
 *
 * More information can be found here:
 * https://docs.unity3d.com/Manual/SL-DepthTextures.html
 * 
 * PROD321 - Interactive Computer Graphics and Animation 
 * Copyright 2021, University of Canterbury
 * Written by Adrian Clark
 */

Shader "Custom/DepthRenderShader"
{
    Properties {}
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                half2 uv : TEXCOORD0;
            };

            uniform sampler2D _CameraDepthTexture;

            v2f vert(appdata i) {
                 v2f o;
                 o.vertex = UnityObjectToClipPos(i.vertex);
                 o.uv = MultiplyUV(UNITY_MATRIX_TEXTURE0, i.uv);
                 return o;
             }
 
             half4 frag(v2f i) : COLOR {
                 float depth = UNITY_SAMPLE_DEPTH(tex2D(_CameraDepthTexture, i.uv));
                 depth = Linear01Depth(depth);
                 return depth;
             }
             
            ENDCG
        }
    }
}
