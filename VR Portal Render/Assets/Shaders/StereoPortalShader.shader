Shader "Portal/StereoPortalShader"
{
    Properties
    {
        _LeftEyeTexture("Left Eye Texture", 2D) = "white" {}
        _RightEyeTexture("Right Eye Texture", 2D) = "white" {}
    }

        SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile __ UNITY_SINGLE_PASS_STEREO STEREO_INSTANCING_ON STEREO_MULTIVIEW_ON

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _LeftEyeTexture;
            sampler2D _RightEyeTexture;

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

            // Use the appropriate texture based on which eye we're rendering for
            fixed4 col;
            if (unity_StereoEyeIndex == 0) {
                col = tex2D(_LeftEyeTexture, i.uv);
            }
else {
 col = tex2D(_RightEyeTexture, i.uv);
}

return col;
}
ENDCG
}
    }
}