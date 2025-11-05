Shader "Custom/CircularVisionCutout"
{
    Properties
    {
        _Color("Overlay Color", Color) = (0,0,0,0.95)
        _Radius("Visible Radius", Range(0,1)) = 0.26
        _Offset("Center Offset (X,Y)", Vector) = (0, -0.04, 0, 0)
        _Aspect("Screen Aspect Ratio", Float) = 1.777  // default 16:9
    }

    SubShader
    {
        Tags { "Queue"="Overlay" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _Color;
            float _Radius;
            float4 _Offset;
            float _Aspect;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Flip Y for UI canvas
                float2 uv = float2(i.uv.x, 1.0 - i.uv.y);

                // Apply offset
                float2 center = float2(0.5 + _Offset.x, 0.5 + _Offset.y);

                // Correct for aspect ratio manually (scale X)
                float2 scaled = float2((uv.x - center.x) * _Aspect, uv.y - center.y);
                float dist = length(scaled);

                // Hard circular cutout
                float mask = step(_Radius, dist);

                fixed4 overlay = _Color;
                overlay.a *= mask;
                return overlay;
            }
            ENDCG
        }
    }
}
