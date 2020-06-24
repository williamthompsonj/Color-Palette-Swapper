using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class ImageUtilities : MonoBehaviour
{
    // how many steps in our color groups [0-255], represents distinct hues
    public static int sort_steps = 24;
    // 360 / 24 = 15 degree steps, seems to work pretty well and still captures gradiation

    // palette color square size in pixels
    public static int square_size = 16;

	// how tolerant we are to find gray colors [0-255]
	public static int achromatic_tolerance = 16;

	// how tolerant we are to transparent colors
	public static int transparent_threshhold = 245;

	// save filename
	public static string filename = "no_image_loaded";

    // color palette information
    public static List<ColorPlus> input_palette = new List<ColorPlus>();
    public static List<ColorPlus> output_palette = new List<ColorPlus>();

	// reference to our two images
	public static RawImage input_image;
	public static RawImage output_image;

	// palette mapping from input to output
	public static string color_mapping;

	// translated (and modified) from step sort function in python found here
	// https://www.alanzucconi.com/2015/09/30/colour-sorting/
	public static List<ColorPlus> StepSort(List<ColorPlus> colors)
	{
		List<ColorPlus> grays = new List<ColorPlus>();

		// calculate sort values for each color
		for (int i = colors.Count - 1; i > -1; i--)
		{
			// remove gray colors
			if (colors[i].achromatic)
			{
				grays.Add(colors[i]);
				colors.RemoveAt(i);
			}
		}

		// sort grays by brightness
		grays = grays.OrderBy(o => o.brightness).ToList();

		// sort colors by hue, brightness, value
		colors = colors.
			OrderBy(o => (int)Mathf.Round(o.hue * ImageUtilities.sort_steps))
			.ThenBy(o => (int)Mathf.Round(o.brightness * ImageUtilities.sort_steps))
			.ThenBy(o => (int)Mathf.Round(o.value * ImageUtilities.sort_steps))
			.ToList();

		// return a sorted list of colors (grays in front)
		return grays.Concat(colors).ToList();
	}

	// helper function for CIE2000
	private static double deg2Rad(double deg)
	{
		return (deg * (Math.PI / 180.0));
	}

	// helper function for CIE2000
	private static double rad2Deg(double rad)
	{
		return ((180.0 / Math.PI) * rad);
	}

	// translated from very good C++ source found at https://github.com/gfiumara/CIEDE2000
	public static double CIE2000(ColorPlus lab1, ColorPlus lab2)
	{
		/* 
		* "For these and all other numerical/graphical delta E00 values
		* reported in this article, we set the parametric weighting factors
		* to unity(i.e., k_L = k_C = k_H = 1.0)." (Page 27).
		*/
		double k_L = 1.0, k_C = 1.0, k_H = 1.0;
		double deg360InRad = deg2Rad(360.0);
		double deg180InRad = deg2Rad(180.0);
		double pow25To7 = 6103515625.0; /* pow(25, 7) */

		/*
		 * Step 1 
		 */
		/* Equation 2 */
		double C1 = Math.Sqrt((lab1.a * lab1.a) + (lab1.b * lab1.b));
		double C2 = Math.Sqrt((lab2.a * lab2.a) + (lab2.b * lab2.b));
		/* Equation 3 */
		double barC = (C1 + C2) / 2.0;
		/* Equation 4 */
		double G = 0.5 * (1 - Math.Sqrt(Math.Pow(barC, 7) / (Math.Pow(barC, 7) + pow25To7)));
		/* Equation 5 */
		double a1Prime = (1.0 + G) * lab1.a;
		double a2Prime = (1.0 + G) * lab2.a;
		/* Equation 6 */
		double CPrime1 = Math.Sqrt((a1Prime * a1Prime) + (lab1.b * lab1.b));
		double CPrime2 = Math.Sqrt((a2Prime * a2Prime) + (lab2.b * lab2.b));
		/* Equation 7 */
		double hPrime1;
		if (lab1.b == 0 && a1Prime == 0)
			hPrime1 = 0.0;
		else
		{
			hPrime1 = Math.Atan2(lab1.b, a1Prime);
			/* 
			 * This must be converted to a hue angle in degrees between 0 
			 * and 360 by addition of 2 degrees to negative hue angles.
			 */
			if (hPrime1 < 0)
				hPrime1 += deg360InRad;
		}
		double hPrime2;
		if (lab2.b == 0 && a2Prime == 0)
			hPrime2 = 0.0;
		else
		{
			hPrime2 = Math.Atan2(lab2.b, a2Prime);
			/* 
			 * This must be converted to a hue angle in degrees between 0 
			 * and 360 by addition of 2 degrees to negative hue angles.
			 */
			if (hPrime2 < 0)
				hPrime2 += deg360InRad;
		}

		/*
		 * Step 2
		 */
		/* Equation 8 */
		double deltaLPrime = lab2.l - lab1.l;
		/* Equation 9 */
		double deltaCPrime = CPrime2 - CPrime1;
		/* Equation 10 */
		double deltahPrime;
		double CPrimeProduct = CPrime1 * CPrime2;
		if (CPrimeProduct == 0)
			deltahPrime = 0;
		else
		{
			/* Avoid the fabs() call */
			deltahPrime = hPrime2 - hPrime1;
			if (deltahPrime < -deg180InRad)
				deltahPrime += deg360InRad;
			else if (deltahPrime > deg180InRad)
				deltahPrime -= deg360InRad;
		}
		/* Equation 11 */
		double deltaHPrime = 2.0 *
			Math.Sqrt(CPrimeProduct) *
			Math.Sin(deltahPrime / 2.0);

		/*
		 * Step 3
		 */
		/* Equation 12 */
		double barLPrime = (lab1.l + lab2.l) / 2.0;
		/* Equation 13 */
		double barCPrime = (CPrime1 + CPrime2) / 2.0;
		/* Equation 14 */
		double barhPrime, hPrimeSum = hPrime1 + hPrime2;
		if (CPrime1 * CPrime2 == 0)
		{
			barhPrime = hPrimeSum;
		}
		else
		{
			if (Math.Abs(hPrime1 - hPrime2) <= deg180InRad)
				barhPrime = hPrimeSum / 2.0;
			else
			{
				if (hPrimeSum < deg360InRad)
					barhPrime = (hPrimeSum + deg360InRad) / 2.0;
				else
					barhPrime = (hPrimeSum - deg360InRad) / 2.0;
			}
		}
		/* Equation 15 */
		double T = 1.0 - (0.17 * Math.Cos(barhPrime - deg2Rad(30.0))) +
			(0.24 * Math.Cos(2.0 * barhPrime)) +
			(0.32 * Math.Cos((3.0 * barhPrime) + deg2Rad(6.0))) -
			(0.20 * Math.Cos((4.0 * barhPrime) - deg2Rad(63.0)));
		/* Equation 16 */
		double deltaTheta = deg2Rad(30.0) *
			Math.Exp(-Math.Pow((barhPrime - deg2Rad(275.0)) / deg2Rad(25.0), 2.0));
		/* Equation 17 */
		double R_C = 2.0 * Math.Sqrt(Math.Pow(barCPrime, 7.0) /
			(Math.Pow(barCPrime, 7.0) + pow25To7));
		/* Equation 18 */
		double S_L = 1 + ((0.015 * Math.Pow(barLPrime - 50.0, 2.0)) /
			Math.Sqrt(20 + Math.Pow(barLPrime - 50.0, 2.0)));
		/* Equation 19 */
		double S_C = 1 + (0.045 * barCPrime);
		/* Equation 20 */
		double S_H = 1 + (0.015 * barCPrime * T);
		/* Equation 21 */
		double R_T = (-Math.Sin(2.0 * deltaTheta)) * R_C;

		/* Equation 22 */
		double deltaE = Math.Sqrt(
			Math.Pow(deltaLPrime / (k_L * S_L), 2.0) +
			Math.Pow(deltaCPrime / (k_C * S_C), 2.0) +
			Math.Pow(deltaHPrime / (k_H * S_H), 2.0) +
			(R_T * (deltaCPrime / (k_C * S_C)) * (deltaHPrime / (k_H * S_H))));

		return (deltaE);
	}

	// cycles through colors lists and set lowest LAB distance between each
	public static void FindClosest()
	{
		// ensure we received a valid colors array
		if (input_palette.Count == 0 || output_palette.Count == 0) return;

		double current, last;
		int i, j;

		ColorPlus color;

		for (i = 0; i < input_palette.Count; i++)
        {
			color = input_palette[i];
			// take first color value as current nearest match
			ColorPlus closest = output_palette[0];
			last = CIE2000(color, output_palette[0]);

			// see if another color is closer than the first one
			for (j = 1; j < output_palette.Count; j++)
			{
				current = CIE2000(color, output_palette[j]);
				if (last > current)
				{
					last = current;
					closest = output_palette[j];
				}
			}

			// best possible match is saved
			color.nearest_match = new Color32((byte)closest.red_int, (byte)closest.green_int, (byte)closest.blue_int, (byte)255);

			// ensure data is kept
			input_palette[i] = color;
			//UnityEngine.Debug.Log(color);
		}
	}

	public static void SetOutputImage()
    {
		// ensure we received a valid colors array
		if (input_palette.Count == 0 || output_palette.Count == 0)
		{
			return;
		}

		// duplicate input texture as Texture2D
		Texture2D texture = Instantiate(input_image.mainTexture) as Texture2D;
		Color32[] cols = texture.GetPixels32(0);
		ColorPlus transparent = new ColorPlus();
		transparent.nearest_match = new Color32();
		int i, j;

		// cycle through all the colors
		for (i = 0; i < cols.Length; ++i)
		{
			if (cols[i].a <= transparent_threshhold)
			{
				// set this pixel to transparent
				cols[i] = transparent.nearest_match;
				continue;
			}

			for (j = 0; j < input_palette.Count; j++)
			{
				if (cols[i].r == input_palette[j].red_int &&
				   cols[i].g == input_palette[j].green_int &&
				   cols[i].b == input_palette[j].blue_int)
				{
					cols[i] = input_palette[j].nearest_match;
					break;
				}
			}
		}

		// apply the changes
		texture.SetPixels32(cols);
		texture.Apply(false);

		// save the color transformed texture onto output_image.texture
		output_image.texture = texture;

		// stop images from wrapping
		input_image.texture.wrapMode = TextureWrapMode.Clamp;
		output_image.texture.wrapMode = TextureWrapMode.Clamp;
	}
}