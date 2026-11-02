// Seasonal 360 sky for the built-in render pipeline. Samples two equirectangular (2:1) photos and blends them, with a
// "glitch" transition: mosaic blocks, CRT scan lines and a little chromatic flicker. Driven by SeasonSky.cs.
//
// VR note: the mosaic is computed from the VIEW DIRECTION (blocks of longitude/latitude), not from screen pixels, so
// both eyes see the same world-locked blocks and the effect stays comfortable in stereo. Only the scan lines are
// screen-space (that is what makes them read as a monitor) and they are faint and short-lived.
Shader "PittBradford/SeasonSky"
{
    Properties
    {
        [NoScaleOffset] _TexA ("Sky A (equirectangular)", 2D) = "grey" {}
        [NoScaleOffset] _TexB ("Sky B (equirectangular)", 2D) = "grey" {}
        _Blend ("Blend A to B", Range(0, 1)) = 0
        _Rotation ("Yaw rotation (degrees)", Range(0, 360)) = 0
        _Exposure ("Exposure", Range(0, 4)) = 1
        _Tint ("Tint", Color) = (1, 1, 1, 1)
        _Block ("Mosaic block size (degrees, 0 = off)", Range(0, 30)) = 0
        _Scan ("Scan line intensity", Range(0, 1)) = 0
        _ScanCount ("Scan lines per screen height", Float) = 220
        _Flicker ("Chromatic flicker", Range(0, 1)) = 0
        _Seed ("Glitch seed", Float) = 0
    }

    SubShader
    {
        Tags { "Queue" = "Background" "RenderType" = "Background" "PreviewType" = "Skybox" }
        Cull Off ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            sampler2D _TexA, _TexB;
            float4 _TexA_TexelSize, _TexB_TexelSize;
            float _Blend, _Rotation, _Exposure, _Block, _Scan, _ScanCount, _Flicker, _Seed;
            half4 _Tint;

            struct appdata
            {
                float4 vertex : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 dir : TEXCOORD0;
                float4 screen : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2f vert (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.pos = UnityObjectToClipPos(v.vertex);
                // Skybox mesh is centred on the camera: the object-space position is the view direction
                float a = radians(_Rotation);
                float s = sin(a), c = cos(a);
                float3 d = v.vertex.xyz;
                o.dir = float3(c * d.x - s * d.z, d.y, s * d.x + c * d.z);
                o.screen = ComputeScreenPos(o.pos);
                return o;
            }

            float hash (float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }

            static const float PI = 3.14159265;

            // longitude in [-PI, PI], latitude in [-PI/2, PI/2]
            float2 LonLat (float3 d)
            {
                d = normalize(d);
                return float2(atan2(d.x, d.z), asin(clamp(d.y, -1, 1)));
            }

            float2 ToUV (float2 ll)
            {
                return float2(ll.x / (2 * PI) + 0.5, ll.y / PI + 0.5);
            }

            half3 Sample (float2 ll, float lod)
            {
                float2 uv = ToUV(ll);
                half3 a = tex2Dlod(_TexA, float4(uv, 0, lod)).rgb;
                half3 b = tex2Dlod(_TexB, float4(uv, 0, lod)).rgb;
                return lerp(a, b, _Blend);
            }

            half4 frag (v2f i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float3 d = normalize(i.dir);
                float2 ll = LonLat(d);

                // Mip level from the angular size of a screen pixel (seam-free, unlike derivatives of atan2)
                float texH = max(_TexA_TexelSize.w, 1);
                float pixAngle = max(length(fwidth(d)), 1e-6);
                float lod = max(0, log2(pixAngle * texH / PI));

                float blockRad = radians(_Block);
                float2 cell = float2(0, 0);
                if (_Block > 0.01)
                {
                    // Quantise longitude and latitude into blocks; widen longitude blocks towards the poles so the
                    // mosaic keeps roughly square blocks
                    float latCell = floor(ll.y / blockRad);
                    float latC = (latCell + 0.5) * blockRad;
                    float lonBlock = blockRad / max(cos(latC), 0.12);
                    lonBlock = 2 * PI / max(1, floor(2 * PI / lonBlock)); // whole number of blocks around: no seam
                    float lonCell = floor((ll.x + PI) / lonBlock);
                    ll = float2((lonCell + 0.5) * lonBlock - PI, clamp(latC, -PI / 2, PI / 2));
                    cell = float2(lonCell, latCell);
                    // Each block shows the average colour of its area
                    lod = max(lod, log2(blockRad * texH / PI) - 0.5);
                }

                half3 col;
                if (_Flicker > 0.001)
                {
                    // Chromatic split: red and blue sampled a little to either side (in whole blocks when mosaic is on)
                    float jitter = (hash(float2(_Seed, cell.y)) - 0.5) * 2;
                    float off = radians(0.6 + 2.5 * _Flicker) * (0.6 + 0.4 * jitter) * _Flicker;
                    off = max(off, blockRad * 0.6 * _Flicker); // with the mosaic on, split by most of a block so it shows
                    col.r = Sample(ll + float2(off, 0), lod).r;
                    col.g = Sample(ll, lod).g;
                    col.b = Sample(ll - float2(off, 0), lod).b;
                    // Brightness flicker per block row
                    float f = hash(float2(cell.y * 3.1 + _Seed, floor(_Time.y * 24)));
                    col *= 1 + (f - 0.5) * 0.35 * _Flicker;
                }
                else
                {
                    col = Sample(ll, lod);
                }

                if (_Scan > 0.001)
                {
                    float2 sp = i.screen.xy / max(i.screen.w, 1e-5);
                    float scanWave = 0.5 + 0.5 * sin(sp.y * _ScanCount * PI * 2);
                    col *= lerp(1, 0.55 + 0.45 * scanWave, _Scan);
                    // A brighter band rolling down the screen, like an old monitor
                    float band = frac(sp.y + _Time.y * 0.9);
                    col += _Scan * 0.12 * smoothstep(0.0, 0.06, band) * (1 - smoothstep(0.06, 0.14, band));
                }

                col *= _Tint.rgb * _Exposure;
                return half4(col, 1);
            }
            ENDCG
        }
    }
    Fallback Off
}
