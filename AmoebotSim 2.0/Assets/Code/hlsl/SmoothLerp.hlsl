float Flip_float(float x)
{
	return 1.0f - x;
}

float EaseIn_float(float t)
{
	return t * t;
}

void EaseInFinal_float(float t, out float output)
{
	output = t * t;
}

float EaseOut_float(float t)
{
	return Flip_float(EaseIn_float(Flip_float(t)));
}

void EaseOutFinal_float(float t, out float output)
{
	output = Flip_float(EaseIn_float(Flip_float(t)));
}

float Lerp_float(float s1, float s2, float pct)
{
	return (s1 + (s2 - s1) * pct);
}

void SmoothLerp_float(float t, out float output)
{
	output = Lerp_float(EaseIn_float(t), EaseOut_float(t), t);
}
