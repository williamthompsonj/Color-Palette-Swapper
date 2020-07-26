using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using UnityEngine;

public struct ColorPlus : IEquatable<ColorPlus>, IComparable<ColorPlus>
{
	// sRGB values in 0-255 range
	public int red { get { return _color32.r; } }
	public int green { get { return _color32.g; } }
	public int blue { get { return _color32.b; } }
	public int alpha { get { return _color32.a; } }

	public bool achromatic
	{
		get
		{
			// get minimum and maximum RGB values
			int max = _color32.r;
			if (max < _color32.g) max = _color32.g;
			if (max < _color32.b) max = _color32.b;
			int min = _color32.r;
			if (min > _color32.g) min = _color32.g;
			if (min > _color32.b) min = _color32.b;

			/*
				* check if color is close to gray
				**/
			if (max - min > ImageUtilities.achromatic_tolerance)
				return false;
			else
				return true;
		}
	}

	private Color32 _color32;
	public Color32 color32 { get { return _color32; } }

	// web safe color (used in mapping palettes as simple text)
	public string hex
	{
		get
		{
			return String.Format("#{0:x2}{1:x2}{2:x2}{3:x2}", _color32.r, _color32.g, _color32.b, _color32.a);
		}
	}

	public string hex_match
	{
		get
		{
			return String.Format("#{0:x2}{1:x2}{2:x2}{3:x2} -> #{4:x2}{5:x2}{6:x2}{7:x2}", _color32.r, _color32.g, _color32.b, _color32.a, _match.r, _match.g, _match.b, _match.a);
		}
	}

	// HSV definition (used in sorting)
	private bool _hsv_set;
	private float _hue, _saturation, _value;
	public float hue
	{
		get
		{
			if (_hsv_set) return _hue;
			SetupHSV();
			return _hue;
		}
	}
	public float saturation
	{
		get
		{
			if (_hsv_set) return _saturation;
			SetupHSV();
			return _saturation;
		}
	}
	public float value
	{
		get
		{
			if (_hsv_set) return _value;
			SetupHSV();
			return _value;
		}
	}

	// intermediate values to convert to CIE color space
	private bool _cie_set;
	private double _x, _y, _z;
	public double x
	{
		get
		{
			if (_cie_set) return _x;
			CIE2000Setup();
			return _x;
		}
	}
	public double y
	{
		get
		{
			if (_cie_set) return _y;
			CIE2000Setup();
			return _y;
		}
	}
	public double z
	{
		get
		{
			if (_cie_set) return _z;
			CIE2000Setup();
			return _z;
		}
	}

	// alias for y, https://en.wikipedia.org/wiki/Relative_luminance#Relative_luminance_in_colorimetric_spaces
	public double brightness
	{
		get
		{
			if (_cie_set) return _y;
			CIE2000Setup();
			return _y;
		}
	}

	// CIE L*A*B* color codes
	private double _l, _a, _b;
	public double l
	{
		get
		{
			if (_cie_set) return _l;
			CIE2000Setup();
			return _l;
		}
	}
	public double a
	{
		get
		{
			if (_cie_set) return _a;
			CIE2000Setup();
			return _a;
		}
	}
	public double b
	{
		get
		{
			if (_cie_set) return _b;
			CIE2000Setup();
			return _b;
		}
	}

	// stores nearest neighbor color match
	private Color32 _match;
	public Color32 match {
		get {
			return _match;
		}
		set {
			_match = value;
		}
	}

	private int _hashcode;

	public ColorPlus(Color32 col) : this()
	{
		_color32 = col;
		_match = col;

		_hashcode = _color32.r << 24 | _color32.g << 16 | _color32.b << 8 | _color32.a;
	}

	public ColorPlus(int Red, int Green, int Blue, int Alpha = 255) : this()
	{
		Setup((byte)Red, (byte)Green, (byte)Blue, (byte)Alpha);
	}

	public ColorPlus(byte Red, byte Green, byte Blue, byte Alpha = 255) : this()
	{
		Setup(Red, Green, Blue, Alpha);
	}

	private void Setup(int Red, int Green, int Blue, int Alpha = 255)
	{
		Setup((byte)Red, (byte)Green, (byte)Blue, (byte)Alpha);
	}

	private void Setup(byte Red, byte Green, byte Blue, byte Alpha = 255)
	{
		_color32 = new Color32(Red, Green, Blue, Alpha);
		_match = _color32;

		_hashcode = _color32.r << 24 | _color32.g << 16 | _color32.b << 8 | _color32.a;
	}

	public void SetupHSV()
    {
		_hsv_set = true;
		Color.RGBToHSV(_color32, out _hue, out _saturation, out _value);
	}

	public void CIE2000Setup()
	{
		_cie_set = true;
		/*
		 * Convert RGB to CIE XYZ color space
		 * https://www.codeproject.com/Articles/19045/Manipulating-colors-in-NET-Part-1
		 **/
		// normalize red, green, blue values
		double rLinear = (double)_color32.r / 255.0;
		double gLinear = (double)_color32.g / 255.0;
		double bLinear = (double)_color32.b / 255.0;

		/*
		 * http://www.brucelindbloom.com/index.html?Eqn_RGB_XYZ_Matrix.html
		 **/
		// convert sRGB to linear RGB (prepare for conversion to XYZ)
		const double E0Constant = 0.04044823627710785308233;
		rLinear = (rLinear > E0Constant) ? Math.Pow((rLinear + 0.055) / 1.055, 2.4) : (rLinear / 12.92);
		gLinear = (gLinear > E0Constant) ? Math.Pow((gLinear + 0.055) / 1.055, 2.4) : (gLinear / 12.92);
		bLinear = (bLinear > E0Constant) ? Math.Pow((bLinear + 0.055) / 1.055, 2.4) : (bLinear / 12.92);

		// convert to xyz
		// https://mina86.com/2019/srgb-xyz-matrix/
		_x = rLinear * 0.412386563252991700 + gLinear * 0.35759149092062537 + bLinear * 0.18045049120356368;
		_y = rLinear * 0.212636821677323840 + gLinear * 0.71518298184125070 + bLinear * 0.07218019648142547;
		_z = rLinear * 0.019330620152483987 + gLinear * 0.11919716364020845 + bLinear * 0.95037258700543540;

		// https://en.wikipedia.org/wiki/Illuminant_D65
		// Illuminant D65 white in XYZ space using standard 2° observer
		double[] D65 = { 0.95047, 1.0, 1.08883 };

		// Convert XYZ to LAB
		// https://www.codeproject.com/Articles/19045/Manipulating-colors-in-NET-Part-1
		double X_Xn = x / D65[0];
		double Y_Yn = y / D65[1];
		double Z_Zn = z / D65[2];

		// CIE intended value for theta 216/24389 = 0.00885645167903563081717167575546
		// CIE intended value for k   24389/27    = ‭903.2962962962962962962962962963‬
		_l = 116.0 * Fxyz(Y_Yn) - 16;
		_a = 500.0 * (Fxyz(X_Xn) - Fxyz(Y_Yn));
		_b = 200.0 * (Fxyz(Y_Yn) - Fxyz(Z_Zn));
	}

	// helper function to translate XYZ to LAB color space
	private static double Fxyz(double t)
	{
		// value for theta 216/24389 = 0.00885645167903563081717167575546
		// value for k      16/116   = 0.1379310344827586
		// value for    theta / 116  = ‭7.787037037037037037037037037037‬
		if (t > 0.00885645167903563081717167575546)
			return Math.Pow(t, (1.0 / 3.0));
		else
			return (7.787037037037037037037037 * t) + 0.1379310344827586;
	}

	public override string ToString()
	{
		return hex;
	}

	public bool Equals(ColorPlus a)
	{
		return a == this;
	}

	public bool Equals(Color32 a)
	{
		return a == this;
	}

	public override bool Equals(object o)
    {
		if (o is ColorPlus)
			return this.Equals((ColorPlus)o);
		if (o is Color32)
			return this.Equals((Color32)o);

		return false;
    }

	public override int GetHashCode()
	{
		return _hashcode;
	}

	// allow sort ColorPlus, IComparable
	public int CompareTo(ColorPlus m)
	{
		int result = (int)Mathf.Round(this.hue * ImageUtilities.sort_steps).CompareTo((int)Mathf.Round(m.hue * ImageUtilities.sort_steps));

		if (result == 0)
		{
			result = (int)Math.Round(this.brightness * ImageUtilities.sort_steps).CompareTo((int)Math.Round(m.brightness * ImageUtilities.sort_steps));
		}

		if (result == 0)
		{
			result = (int)Mathf.Round(this.value * ImageUtilities.sort_steps).CompareTo((int)Mathf.Round(m.value * ImageUtilities.sort_steps));
		}

		return result;
	}

	// convert implicitly between Color32 and ColorPlus
	public static implicit operator Color32(ColorPlus c) => c._color32;
	public static implicit operator ColorPlus(Color32 c) => new ColorPlus(c);

	// convert implicitly between int and ColorPlus
	public static implicit operator ColorPlus(int c) => new ColorPlus(new Color32(
		(byte)(c >> 24 & 255),
		(byte)(c >> 16 & 255),
		(byte)(c >> 8 & 255),
		(byte)(c & 255)
		));
	public static implicit operator int(ColorPlus c) => c.GetHashCode();

	public static bool operator ==(ColorPlus a, ColorPlus b)
	{
		if (a.red == b.red &&
			a.green == b.green &&
			a.blue == b.blue &&
			a.alpha == b.alpha)
			return true;
		return false;
	}

	public static bool operator ==(Color32 a, ColorPlus b)
	{
		if (a.r == b.red &&
			a.g == b.green &&
			a.b == b.blue &&
			a.a == b.alpha)
			return true;
		return false;
	}

	public static bool operator ==(ColorPlus b, Color32 a)
	{
		return a == b;
	}

	public static bool operator !=(ColorPlus a, ColorPlus b)
	{
		return !(a == b);
	}

	public static bool operator !=(Color32 a, ColorPlus b)
	{
		return !(a == b);
	}

	public static bool operator !=(ColorPlus b, Color32 a)
	{
		return !(a == b);
	}
}
