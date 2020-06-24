using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

public struct ColorPlus
{
	// sRGB values in 0-255 range
	public int red_int, green_int, blue_int;

	// sRGB values in 0-1 range
	public float red, green, blue;

	// determine if gray scale (or very close)
	public bool achromatic { get {
			// get minimum and maximum RGB values
			int max = red_int;
			if (max < green_int) max = green_int;
			if (max < blue_int) max = blue_int;
			int min = red_int;
			if (min > green_int) min = green_int;
			if (min > blue_int) min = blue_int;

			/*
			 * check if color is close to gray
			 **/
			if (max - min > ImageUtilities.achromatic_tolerance)
				return false;
			else
				return true;
		} }

	// absolute brightness (helps with sorting)
	public float brightness;

	// web safe color (used in mapping palettes as simple text)
	public string hex;

	// HSV definition (used in sorting)
	public float hue, saturation, value;

	// intermediate values to convert to CIE color space
	public double x, y, z;

	// CIE L*A*B* color codes
	public double l, a, b;

	// stores nearest neighbor color match
	private Color32 _nearest_match;
	public Color32 nearest_match {
		get { return _nearest_match; }
        set { _nearest_match = value; }
	}

	/*
	 * 
	 * 
	 * CONSTRUCTOR
	 * 
	 * 
	 */
	public ColorPlus(Color32 col) : this()
	{
		// is this a transparent color?
		if (col.a <= ImageUtilities.transparent_threshhold)
		{
			nearest_match = new Color32();

			// dump to black transparent and monochrome
			return;
		}

		red_int = col.r;
		green_int = col.g;
		blue_int  = col.b;

		nearest_match = new Color32((byte)red_int, (byte)green_int, (byte)blue_int, (byte)255);

		red   = (float)col.r / 255f;
		green = (float)col.g / 255f;
		blue  = (float)col.b / 255f;

		// calculate luminance (Y) from YIQ color space -- ITU-R BT.601 (TV encoding standard)
		// page 4, https://www.itu.int/dms_pubrec/itu-r/rec/bt/R-REC-BT.601-7-201103-I!!PDF-E.pdf
		brightness = 0.299f * red_int + 0.587f * green_int + 0.114f * blue_int;

		// convert RGB to hex string
		hex = String.Format("#{0:x2}{1:x2}{2:x2}", red_int, green_int, col.b);

		/*
		 * convert RGB to HSB
		 **/
		Color.RGBToHSV(col, out hue, out saturation, out value);

		/*
		 * Convert RGB to CIE XYZ color space
		 * https://www.codeproject.com/Articles/19045/Manipulating-colors-in-NET-Part-1
		 **/
		// normalize red, green, blue values
		double rLinear = (double)red_int / 255.0;
		double gLinear = (double)green_int / 255.0;
		double bLinear = (double)blue_int / 255.0;

		/*
		 * Original has a bug, it does this: 
		 * ... Math.Pow(expression, 2.2) ...
		 * This should be Math.Pow(expression, 2.4) because 2.2 is from a different formula
		 * http://www.brucelindbloom.com/index.html?Eqn_RGB_XYZ_Matrix.html
		 **/
		// convert to a sRGB form
		double r = (rLinear > 0.04045) ? Math.Pow((rLinear + 0.055) / (1 + 0.055), 2.4) : (rLinear / 12.92);
		double g = (gLinear > 0.04045) ? Math.Pow((gLinear + 0.055) / (1 + 0.055), 2.4) : (gLinear / 12.92);
		double b = (bLinear > 0.04045) ? Math.Pow((bLinear + 0.055) / (1 + 0.055), 2.4) : (bLinear / 12.92);

		// convert to xyz
		// https://software.intel.com/en-us/forums/intel-integrated-performance-primitives/topic/798591
		x = r * 0.412453 + g * 0.357580 + b * 0.180423;
		y = r * 0.212671 + g * 0.715160 + b * 0.072169;
		z = r * 0.019334 + g * 0.119193 + b * 0.950227;

		// https://en.wikipedia.org/wiki/Illuminant_D65
		// Illuminant D65 white in XYZ space using standard 2° observer 
		double[] D65 = { 0.95047, 1.0, 1.08883 };

		// Convert XYZ to LAB
		// https://www.codeproject.com/Articles/19045/Manipulating-colors-in-NET-Part-1
		l = 116.0 * Fxyz(y / D65[1]) - 16;
		a = 500.0 * (Fxyz(x / D65[0]) - Fxyz(y / D65[1]));
		b = 200.0 * (Fxyz(y / D65[1]) - Fxyz(z / D65[2]));
	}

	// helper function to translate XYZ to LAB color space
	private static double Fxyz(double t)
	{
		return ((t > 0.008856) ? Math.Pow(t, (1.0 / 3.0)) : (7.787 * t + 16.0 / 116.0));
	}

	/*
	 * 
	 * 
	 * TO STRING METHOD
	 * 
	 *
	 */
	public override string ToString()
	{
		// return the color matched
		return String.Format("{0} -> #{1:x2}{2:x2}{3:x2}", hex, nearest_match.r, nearest_match.b, nearest_match.g);
	}
}
