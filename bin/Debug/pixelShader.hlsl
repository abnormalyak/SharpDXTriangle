// Entry point ('main')
float4 main(float4 position : SV_Position, float4 color : COLOR) : SV_Target 
{
	return color;
}

/**
* The parameter to the method is the output from the vertex shader
* The vertex shader is run for each vertex, while pixel shader runs for each pixel, so this will be an interpolated position. 
* From this method we return a float4, in the format RGBA. 
* So this will produce a red color for all pixels. 
* The values in the float4 range from 0 to 1, so float4(0, 0, 0, 1) would give black, while float4(1, 1, 1, 1) would give a white pixel.
*/