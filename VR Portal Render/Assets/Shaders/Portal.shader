Shader "Custom/Portal"
{
    Properties
    {
        _MainTex("Main Texture", 2D) = "white" {}
        _LeftEyeTexture("Left Eye Texture", 2D) = "white" {}
        _RightEyeTexture("Right Eye Texture", 2D) = "white" {}
    }
        SubShader
    {
        Tags { "RenderType" = "Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 screenPos : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            sampler2D _LeftEyeTexture;
            sampler2D _RightEyeTexture;

            v2f vert(appdata v)
            {
                v2f o;

                // Setup for stereo rendering (VR)
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.vertex = UnityObjectToClipPos(v.vertex);
                o.screenPos = ComputeScreenPos(o.vertex);
                UNITY_TRANSFER_FOG(o, o.vertex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Setup stereo eye index for fragment shader
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

            // Calculate screen UV coordinates
            float2 uv = i.screenPos.xy / i.screenPos.w;

            // Transform UVs based on current eye for proper stereo rendering
            uv = UnityStereoTransformScreenSpaceTex(uv);

            // Sample the appropriate texture based on which eye is being rendered
            fixed4 col;

            // Use unity_StereoEyeIndex to determine which eye is currently being rendered
            if (unity_StereoEyeIndex == 0) {
                // Left eye
                col = tex2D(_LeftEyeTexture, uv);
            }
else {
                // Right eye
                col = tex2D(_RightEyeTexture, uv);
            }

            // Apply fog
            UNITY_APPLY_FOG(i.fogCoord, col);
            return col;
        }
        ENDCG
    }
    }
}