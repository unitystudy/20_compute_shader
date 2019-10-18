Shader "Unlit/NewUnlitShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
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
				float3 normal : NORMAL;
				float2 uv : TEXCOORD0;
				float2 uv1 : TEXCOORD1;
	};

            struct v2f
            {
				float3 normal : NORMAL;
				float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

			struct Particle
			{
				int id;
				bool active;
				float3 position;
				float3 rotation;
				float scale;
			};

#ifdef SHADER_API_D3D11
			StructuredBuffer<Particle> _Particles;
#endif
			int _IdOffset;

			sampler2D _MainTex;
            float4 _MainTex_ST;


			inline int getId(float2 uv1)
			{
				return (int)(uv1.x + 0.5) + _IdOffset;
			}

			float3 rotate(float3 p, float3 rotation)
			{
				float3 a = normalize(rotation);
				float angle = length(rotation);
				if (abs(angle) < 0.001) return p;
				float s = sin(angle);
				float c = cos(angle);
				float r = 1.0 - c;
				float3x3 m = float3x3(
					a.x * a.x * r + c,
					a.y * a.x * r + a.z * s,
					a.z * a.x * r - a.y * s,
					a.x * a.y * r - a.z * s,
					a.y * a.y * r + c,
					a.z * a.y * r + a.x * s,
					a.x * a.z * r + a.y * s,
					a.y * a.z * r - a.x * s,
					a.z * a.z * r + c
					);
				return mul(m, p);
			}

			v2f vert (appdata v)
            {
                v2f o;

#ifdef SHADER_API_D3D11
				Particle p = _Particles[getId(v.uv1)];
				v.vertex.xyz *= p.scale;
				v.vertex.xyz = rotate(v.vertex.xyz, p.rotation);
				v.vertex.xyz += p.position;
				o.normal = rotate(v.normal, p.rotation);
#endif

				o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);


				return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = float4(0.2, 0.7, 0.3, 1.0);
				col.xyz = col.xyz * (max(0, dot(i.normal, float3(0.7, 0.7, 0.0))) * 0.8 + 0.2);
                return col;
            }
            ENDCG
        }
    }
}
