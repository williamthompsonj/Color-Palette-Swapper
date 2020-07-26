namespace UnityEngine
{
	public static class Color32Extension
	{
		public static int GetHashCode(this Color32 c)
		{
			return (c.r << 24 | c.g << 16 | c.b << 8 | c.a);
		}

		public static bool Equals(this Color32 c1, Color32 c2)
		{
			if (c1.r != c2.r) return false;
			if (c1.g != c2.g) return false;
			if (c1.b != c2.b) return false;
			if (c1.a != c2.a) return false;
			return true;
		}

		public static bool Equals(this Color32 c1, ColorPlus c2)
		{
			if (c1.r != c2.red) return false;
			if (c1.g != c2.green) return false;
			if (c1.b != c2.blue) return false;
			if (c1.a != c2.alpha) return false;
			return true;
		}
	}
}