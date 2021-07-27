struct VSOut // Allows multiple return values
{
	float4 position : SV_Position;
	float4 color : COLOR;
};

// Entry point ('main')
VSOut main(float4 position : POSITION, float4 color : COLOR) // POSITION & COLOR are semantics, specifying the intended use of the variable
{
	VSOut output;
	output.position = position;
	output.color = color;
	
	return output;
}