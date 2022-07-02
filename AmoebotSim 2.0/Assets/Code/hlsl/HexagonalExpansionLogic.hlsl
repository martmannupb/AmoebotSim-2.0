void HexagonalExpansion_float(bool ExpansionBool, float ExpansionPercentage, float ExpansionMesh, float CurrentMesh, float2 ExpansionVector1, float2 ExpansionVector2, float2 ContrOffsetClockwise, float2 ContrOffsetCounterclockwise, out float2 CalculatedOffset)
{
	// CurrentMesh - ExpansionMesh (range: -5 to +5)
	// RelMeshPos in range -2 to +3
	int relMeshPos = (CurrentMesh - ExpansionMesh + 6) % 6;
	if (relMeshPos > 3) relMeshPos -= 6;

	float mult = 1.0f;
	float2 zeroFloat = { 0.0f, 0.0f };
	CalculatedOffset = zeroFloat;
	if (ExpansionBool) {
		[branch] switch (relMeshPos)
		{
		case -2:
			mult = 1.0f - (clamp(ExpansionPercentage - 0.5f, 0.0f, 0.5f) * 2.0f);
			CalculatedOffset = mult * ContrOffsetCounterclockwise;
			break;

		case -1:
			mult = 1.0f - clamp(ExpansionPercentage * 2.0f, 0.0f, 1.0f);
			CalculatedOffset = mult * ContrOffsetCounterclockwise;
			break;

		case 0:
			CalculatedOffset = zeroFloat;
			break;

		case 1:
			mult = 1.0f - clamp(ExpansionPercentage * 2.0f, 0.0f, 1.0f);
			CalculatedOffset = mult * ContrOffsetClockwise;
			break;

		case 2:
			mult = 1.0f - (clamp(ExpansionPercentage - 0.5f, 0.0f, 0.5f) * 2.0f);
			CalculatedOffset = mult * ContrOffsetClockwise;
			break;

		case 3:
			CalculatedOffset = zeroFloat;
			mult = 1.0f - (clamp(ExpansionPercentage - 0.5f, 0.0f, 0.5f) * 2.0f);
			float mult2 = 1.0f - clamp(ExpansionPercentage * 2.0f, 0.0f, 1.0f);
			CalculatedOffset = mult * ExpansionVector1 + mult2 * ExpansionVector2;
			break;

		default:
			break;
		}
	}
	else {
		CalculatedOffset = zeroFloat;
	}
}