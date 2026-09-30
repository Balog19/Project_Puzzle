// Draws a GridMap's cell lines in world space, plus the filled hovered cells.
// The mesh/UVs don't matter: any part of the surface that overlaps the grid draws it.
// _GridOrigin, _GridParams and _HoverRect are set per-renderer by GridVisual.
Shader "Puzzle/GridOverlay"
{
    Properties
    {
        _LineColor ("Line Color", Color) = (1, 1, 1, 0.6)
        _CellColor ("Cell Color", Color) = (0, 0, 0, 0)
        _HoverColor ("Hover Color", Color) = (1, 1, 1, 0.35)
        _MouseColor ("Mouse Color", Color) = (1, 0.9, 0.1, 0.6)
        _InvalidColor ("Invalid Color", Color) = (1, 0.2, 0.2, 0.45)
        _LineWidth ("Line Width (fraction of cell)", Range(0.001, 0.25)) = 0.03

        [HideInInspector] _GridOrigin ("Grid Origin", Vector) = (0, 0, 0, 0)
        [HideInInspector] _GridParams ("Grid Params (width, height, cellSize)", Vector) = (10, 10, 1, 0)
        [HideInInspector] _HoverRect ("Hover Rect (minX, minY, maxX, maxY)", Vector) = (-1, -1, -2, -2)
        [HideInInspector] _MouseCell ("Mouse Cell", Vector) = (-1, -1, 0, 0)
        [HideInInspector] _HoverValid ("Hover Valid", Float) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "GridOverlay"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _LineColor;
                half4 _CellColor;
                half4 _HoverColor;
                half4 _MouseColor;
                half4 _InvalidColor;
                float _LineWidth;
                float4 _GridOrigin;
                float4 _GridParams;
                float4 _HoverRect;
                float4 _MouseCell;
                float _HoverValid;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs positions = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = positions.positionCS;
                output.positionWS = positions.positionWS;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 gridSize = _GridParams.xy;
                float cellSize = max(_GridParams.z, 0.0001);

                // Position in cell units: (0,0) is the bottom-left corner of cell (0,0).
                float2 g = (input.positionWS.xz - _GridOrigin.xz) / cellSize;

                // Nothing outside the grid (small margin keeps the outer border line whole).
                float margin = _LineWidth * 0.5;
                clip(g + margin);
                clip(gridSize + margin - g);

                // Distance (in cells) to the nearest grid line on each axis,
                // anti-aliased with screen-space derivatives so lines stay crisp at any zoom.
                float2 distToLine = abs(g - round(g));
                float2 aa = max(fwidth(g), 1e-5);
                float2 lines = 1.0 - smoothstep(_LineWidth * 0.5 - aa, _LineWidth * 0.5 + aa, distToLine);
                float lineMask = max(lines.x, lines.y);

                // Cell fill priority: mouse cell > hovered rectangle > base color.
                // The hovered rectangle turns to the invalid color when _HoverValid is 0.
                float2 cell = floor(g);
                bool hovered = all(cell >= _HoverRect.xy) && all(cell <= _HoverRect.zw);
                bool inside = all(g >= 0.0) && all(g < gridSize);
                bool isMouse = all(cell == _MouseCell.xy);
                half4 hoverFill = _HoverValid > 0.5 ? _HoverColor : _InvalidColor;
                half4 fill = !inside ? _CellColor : isMouse ? _MouseColor : hovered ? hoverFill : _CellColor;

                return lerp(fill, _LineColor, lineMask);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
