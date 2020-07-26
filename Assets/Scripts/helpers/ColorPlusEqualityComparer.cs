using System.Collections.Generic;
using UnityEngine;

class ColorPlusEqualityComparer : IEqualityComparer<ColorPlus>
{
	public bool Equals(ColorPlus a, ColorPlus b)
	{
		if (a.red != b.red) return false;
		if (a.green != b.green) return false;
		if (a.blue != b.blue) return false;
		if (a.alpha != b.alpha) return false;
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

	public int GetHashCode(ColorPlus a)
	{
		return a.GetHashCode();
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