
float4 Color0, Color1;

float4 PSMain(float2 Pos : TEXCOORD0) : COLOR0
{
    float p = Pos.x;
    float m = 1 - p;

    return float4(
        Color0[0] * m + Color1[0] * p,
        Color0[1] * m + Color1[1] * p,
        Color0[2] * m + Color1[2] * p,
        Color0[3] * m + Color1[3] * p
        );
}

technique HGradient
{
    pass Main
    {
        // TODO: set renderstates here.

        PixelShader = compile ps_2_0 PSMain();
    }
}
