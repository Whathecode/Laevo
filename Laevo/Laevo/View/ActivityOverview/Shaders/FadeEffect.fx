sampler2D implicitInput : register( s0 );
float width : register( c0 );

float4 main( float2 uv : TEXCOORD ) : COLOR
{
    float4 color = tex2D( implicitInput, uv );
	
	float perc = 1;
	float startFadeAt = 0.8;
	if ( uv.x > startFadeAt )
	{
		perc = 1 - (uv.x - startFadeAt) / (1 - startFadeAt);
	}
	
    float4 result;
    result.r = color.r * perc;
    result.g = color.g * perc;
    result.b = color.b * perc;
    result.a = color.a * perc;
    
    return result;
}