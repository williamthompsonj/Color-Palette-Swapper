using System.Collections.Generic;
using UnityEngine;

class Color32EqualityComparer : IEqualityComparer<Color32>
{
	public bool Equals(Color32 a, Color32 b)
	{
		if (a.r != b.r) return false;
		if (a.g != b.g) return false;
		if (a.b != b.b) return false;
		if (a.a != b.a) return false;
		return true;
	}

	public bool Equals(ColorPlus a, Color32 b)
	{
		if (a.red != b.r) return false;
		if (a.green != b.g) return false;
		if (a.blue != b.b) return false;
		if (a.alpha != b.a) return false;
		return true;
	}

	public bool Equals(Color32 b, ColorPlus a)
	{
		return Equals(a, b);
	}

	public int GetHashCode(Color32 a)
	{
		return
			(a.r << 24) |
			(a.g << 16 & 255 << 16) |
			(a.b << 8 & 255 << 16) |
			(a.a & 255);
	}
}