using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class ImageUtilities : MonoBehaviour
{
	/*
	 * These should never be directly accessible by the user
	 */

	// color palette information
	public static List<ColorPlus> input_palette = new List<ColorPlus>();
	public static List<ColorPlus> output_palette = new List<ColorPlus>();

	// reference to our two images
	public static RawImage input_image;
	public static RawImage output_image;

	public RawImage raw_input;
	public RawImage raw_output;

	private GameObject wait_panel;

	// main menu buttons
	public static GameObject[] buttons;

	// menu position offset, should be half the window width and height
	public static Vector3 screen_offset
	{
		get { return new Vector3(Camera.main.pixelHeight / 2, Camera.main.pixelWidth / 2, 0); }
	}

	public static bool load_input_palette;

	/*
	 * These are changable settings by user
	 */

	// allow user to choose whether they sort their colors before exporting
	public static bool sort_saved_palette = true;

	// allow user to choose if they want to auto-match colors
	public static bool auto_color_match = true;

	// quantity of steps (color groups) [0-255]; 36 steps (10 degrees) performs pretty good
	private static int _default_sort_steps = 36;
	public static int sort_steps;

	// palette color square size in pixels
	private static int _default_square_size = 16;
	public static int square_size;

	// how tolerant we are to find gray colors [0-255]
	private static int _default_achromatic_tolerance = 17;
	public static int achromatic_tolerance;

	// how far apart gray colors can be and still match [0-255]
	private static int _default_achromatic_match = 17;
	public static int achromatic_match;

	// how tolerant we are to transparent colors
	private static int _default_transparent_threshhold = 245;
	public static int transparent_threshhold;

	// save filename & palette name
	public static string _default_filename = "no_image_loaded";
	public static string filename;

	/*
	 * Methods
	 */
	// reset all user-accessable settings to default values
	public static void DefaultSettings()
	{
		Int64 time_start = PerfMon.Ticks();

		sort_steps = _default_sort_steps;
		square_size = _default_square_size;
		achromatic_tolerance = _default_achromatic_tolerance;
		achromatic_match = _default_achromatic_match;
		transparent_threshhold = _default_transparent_threshhold;
		filename = _default_filename;
		sort_saved_palette = true;
		auto_color_match = true;

		perf("DefaultSettings", time_start);
	}

	// helper function for CIE2000
	private static double deg2Rad(double deg)
	{
		// Math.PI / 180.0 = 0.01745329251994329576923690768489
		return (deg * 0.01745329251994329576923690768489);
	}

	// helper function for CIE2000
	private static double rad2Deg(double rad)
	{
		// 180 / Math.PI = 0.01745329251994329576923690768489
		return (rad * 0.01745329251994329576923690768489);
	}

	// translated from very good C++ source found at https://github.com/gfiumara/CIEDE2000
	public static double CIE2000(ColorPlus lab1, ColorPlus lab2)
	{
		Int64 time_start = PerfMon.Ticks();

		/*
		* "For these and all other numerical/graphical delta E00 values
		* reported in this article, we set the parametric weighting factors
		* to unity(i.e., k_L = k_C = k_H = 1.0)." (Page 27).
		*
		* http://www2.ece.rochester.edu/~gsharma/ciede2000/ciede2000noteCRNA.pdf
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

		perf("CIE2000", time_start);

		return (deltaE);
	}

	public static List<ColorPlus> LoadImagePalette(Color32[] colors)
	{
		Int64 time_start = PerfMon.Ticks();

		Color32EqualityComparer comp = new Color32EqualityComparer();
		HashSet<Color32> unique_hash = new HashSet<Color32>(colors, comp);

		List<ColorPlus> transparent = new List<ColorPlus>();
		List<ColorPlus> final_list = new List<ColorPlus>();

		// cycle through colors and sort out transparent
		foreach (Color32 hash_val in unique_hash)
		{
			if (hash_val.a < ImageUtilities.transparent_threshhold)
				transparent.Add(new ColorPlus(hash_val));
			else
				final_list.Add(new ColorPlus(hash_val));
		}

		// shift transparent colors to end of list
		final_list.AddRange(transparent);

		perf("LoadImagePalette", time_start);

		return final_list;
	}

	// translated (and modified) from step sort function in python found here
	// https://www.alanzucconi.com/2015/09/30/colour-sorting/
	public static List<ColorPlus> StepSort(List<ColorPlus> received)
	{
		Int64 time_start = PerfMon.Ticks();

		List<ColorPlus> colors = new List<ColorPlus>();
		List<ColorPlus> grays = new List<ColorPlus>();

		// figure out what to sort and how to put back together
		for (int i = 0; i != received.Count; i++)
		{
			if (received[i].achromatic)
			{
				// pull out gray colors
				grays.Add(received[i]);
			}
			else
			{
				// pull out gray colors
				colors.Add(received[i]);
			}
		}

		// sort grays by brightness
		grays.Sort();

		// sort colors by hue, brightness, value
		colors.Sort();

		// combine grays and colors
		grays.AddRange(colors);

		perf("StepSort", time_start);

		return grays;
	}

	// cycles through colors lists and set lowest LAB distance between each
	public static void FindClosest()
	{
		Int64 time_start = PerfMon.Ticks();

		// ensure we received a valid colors array
		if (!auto_color_match || input_palette.Count == 0 || output_palette.Count == 0) return;

		Color32 transparent = new Color32((byte)255, (byte)255, (byte)255, (byte)0);

#if UNITY_WEBGL && !UNITY_EDITOR
		for (var i=0; i < input_palette.Count; i++)
		{
#else
		Parallel.For(0, input_palette.Count, i =>
		{
#endif
			// some working varialbes
			double distance = 0, last = 0;
			int j;

			// short reference to this color
			ColorPlus in_color = input_palette[i];
			ColorPlus closest = new ColorPlus();

			bool not_done = true;

			// check if transparent
			if (in_color.alpha <= transparent_threshhold)
			{
				// set this pixel to transparent
				in_color.match = transparent;
				input_palette[i] = in_color;
				not_done = false;
			}

			// check if color is gray
			if (not_done && in_color.achromatic)
			{
				// max possible difference between two gray colors (black -> white)
				distance = 255;

				// cycle through output palette
				for (j = 0; j < output_palette.Count; j++)
				{
					// skip colors that aren't gray
					if (!output_palette[j].achromatic) continue;

					// calculate absolute difference between grays
					if (in_color.brightness > output_palette[j].brightness)
						last = in_color.brightness - output_palette[j].brightness;
					else
						last = output_palette[j].brightness - in_color.brightness;

					if (distance > last)
					{
						closest = output_palette[j];
						distance = last;
					}
				}

				// check if source gray is sufficiently close to the nearest match
				if (distance <= achromatic_match)
				{
					// best possible match is saved
					in_color.match = new Color32((byte)closest.red, (byte)closest.green, (byte)closest.blue, (byte)in_color.alpha);

					// assign data to input palette
					input_palette[i] = in_color;

					// stop calculating for this color
					not_done = false;
				}
			}

			if (not_done)
			{
				// take first output_palette color as nearest match
				closest = output_palette[0];
				last = CIE2000(in_color, output_palette[0]);

				// see if another color is closer than the first one
				for (j = 1; j < output_palette.Count; j++)
				{
					distance = CIE2000(in_color, output_palette[j]);
					if (last > distance)
					{
						last = distance;
						closest = output_palette[j];
					}
				}

				// best possible match is saved
				in_color.match = new Color32((byte)closest.red, (byte)closest.green, (byte)closest.blue, (byte)in_color.alpha);

				// ensure data is kept
				input_palette[i] = in_color;
			}
#if UNITY_WEBGL && !UNITY_EDITOR
		}
#else
		});
#endif

		perf("FindClosest", time_start);
	}

	public static void SetOutputImage()
	{
		Int64 time_start = PerfMon.Ticks();

		// ensure we received a valid colors array
		if (input_palette.Count == 0)
		{
			// no image loaded
			return;
		}

		if (output_palette.Count == 0)
		{
			// no palette, copy input and end function
			output_image.texture = Instantiate(input_image.texture) as Texture2D;
			return;
		}

		// get pixel data directly from input texture
		Color32[] pixels = (input_image.mainTexture as Texture2D).GetPixels32(0);

		var comp = new Color32EqualityComparer();
		Dictionary<Color32, Color32> dict = new Dictionary<Color32, Color32>(comp);
		foreach (ColorPlus c in input_palette)
		{
			dict.Add(c.color32, c.match);
		}

		// color replacement using dictionary lookup
		for (int i = 0; i != pixels.Length; i++)
		{
			// perform color match and swap
			dict.TryGetValue(pixels[i], out pixels[i]);
		}

		// apply changes directly to output texture
		(output_image.mainTexture as Texture2D).SetPixels32(pixels);
		(output_image.mainTexture as Texture2D).Apply(false);

		perf("SetOutputImage", time_start);
	}

	// make main menu buttons accessible or not
	public static void ShowMainButtons(bool toggle = true)
	{
		Int64 time_start = PerfMon.Ticks();

		var offset = -10000;
		if (toggle) offset = 10000;

		for (int i = 0; i != buttons.Length; i++)
		{
			var offset_vec = new Vector3(
				buttons[i].transform.position.x + offset,
				buttons[i].transform.position.y + offset,
				buttons[i].transform.position.z + offset);

			buttons[i].transform.position = offset_vec;
		}

		perf("ShowMainButtons", time_start);
	}

	// make main menu buttons invisible
	public static void HideMainButtons()
	{
		ShowMainButtons(false);
	}

	public static string SafeInt(string s, int start = 0, int finish = 255)
	{
		// remove non-number characters
		s = Regex.Replace(s, "[^0-9]*", string.Empty);

		// ensure number > 0 && number <= 255
		if (s.CompareTo(string.Empty) != 0)
			return Mathf.Clamp(int.Parse(s), start, finish).ToString();
		else
			return start.ToString();
	}

	public static string SafeString(string s, string pattern)
	{
		// remove non-pattern characters
		return Regex.Replace(s, pattern, string.Empty);
	}

	public static string RGB2Hex(int Red, int Green, int Blue)
	{
		return string.Format("{0:x2}{1:x2}{2:x2}", Red, Green, Blue);
	}

	public static string ToGPL(Color32 c)
	{
		// return GPL color palette format
		return String.Format("{0,3}     {1,3}     {2,3}", c.r, c.g, c.b);
	}

	public static string ToGPL(ColorPlus c)
	{
		return ToGPL(c.color32);
	}

	/*
	 * Setting default things and zoom related
	 */
	public void Start()
	{
		setupFuncs();

		useGUILayout = false;

		// setup default settings
		DefaultSettings();
		buttons = GameObject.FindGameObjectsWithTag("UI Button");

		input_image = raw_input;
		output_image = raw_output;

		// setup zoom buttons
		GameObject.Find("Main_BtnZoomIn").gameObject.GetComponent<Button>().onClick.AddListener(ZoomIn);
		GameObject.Find("Main_BtnZoomOut").gameObject.GetComponent<Button>().onClick.AddListener(ZoomOut);

		rt_in = input_image.transform.parent.gameObject.GetComponent<RectTransform>();
		rt_out = output_image.transform.parent.gameObject.GetComponent<RectTransform>();

		rt_in_min = new Vector2(rt_in.offsetMin.x, rt_in.offsetMin.y);
		rt_in_max = new Vector2(rt_in.offsetMax.x, rt_in.offsetMax.y);

		rt_out_min = new Vector2(rt_out.offsetMin.x, rt_out.offsetMin.y);
		rt_out_max = new Vector2(rt_out.offsetMax.x, rt_out.offsetMax.y);

		Texture.allowThreadedTextureCreation = true;
		load_input_palette = false;
	}

	public void Update()
	{
		if (
			!rt_in.offsetMin.Equals(rt_in_min) ||
			!rt_in.offsetMax.Equals(rt_in_max)
			)
		{
			SyncPreview_Output();
		}
		else if (
			!rt_out.offsetMin.Equals(rt_out_min) ||
			!rt_out.offsetMax.Equals(rt_out_max)
			)
		{
			SyncPreview_Input();
		}

		rt_in_min = new Vector2(rt_in.offsetMin.x, rt_in.offsetMin.y);
		rt_in_max = new Vector2(rt_in.offsetMax.x, rt_in.offsetMax.y);

		rt_out_min = new Vector2(rt_out.offsetMin.x, rt_out.offsetMin.y);
		rt_out_max = new Vector2(rt_out.offsetMax.x, rt_out.offsetMax.y);
	}

	public static void SyncPreview_Input()
	{
		Int64 time_start = PerfMon.Ticks();

		rt_in.offsetMin = new Vector2(rt_out.offsetMin.x, rt_out.offsetMin.y);
		rt_in.offsetMax = new Vector2(rt_out.offsetMax.x, rt_out.offsetMax.y);

		perf("SyncPreview_Input", time_start);
	}

	public static void SyncPreview_Output()
	{
		Int64 time_start = PerfMon.Ticks();

		rt_out.offsetMin = new Vector2(rt_in.offsetMin.x, rt_in.offsetMin.y);
		rt_out.offsetMax = new Vector2(rt_in.offsetMax.x, rt_in.offsetMax.y);

		perf("SyncPreview_Output", time_start);
	}

	// current zoom level of our image
	public static float ZoomLevel;
	private static RectTransform rt_in, rt_out;
	private static Vector2 rt_in_min, rt_in_max, rt_out_min, rt_out_max;

	public static void ZoomReset()
	{
		Int64 time_start = PerfMon.Ticks();

		ZoomLevel = 1.0f;

		rt_in.offsetMin = new Vector2(0, 0);
		rt_in.offsetMax = new Vector2(0, 0);

		rt_out.offsetMin = new Vector2(0, 0);
		rt_out.offsetMax = new Vector2(0, 0);

		perf("ZoomReset", time_start);
	}

	private void ZoomChange(bool toggle)
	{
		Int64 time_start = PerfMon.Ticks();

		// verify an image is loaded
		if (input_palette.Count == 0)
			return;

		// capture current width and height for offset later
		float old_x = input_image.texture.width * ZoomLevel;
		float old_y = input_image.texture.height * ZoomLevel;

		float zoom_diff = 0.0f;

		// determine correct zoom change
		if (ZoomLevel >= 1.0f)
			zoom_diff = 0.5f;
		else if (ZoomLevel < 1.0f)
			zoom_diff = 0.05f;

		if (toggle)
			ZoomLevel += zoom_diff;
		else
			ZoomLevel -= zoom_diff;

		// calcaulate new size delta for display rect
		float wide = input_image.texture.width * ZoomLevel;
		float high = input_image.texture.height * ZoomLevel;

		float new_x = input_image.texture.width * ZoomLevel;
		float new_y = input_image.texture.height * ZoomLevel;

		float dif_x = -Math.Abs(old_x - new_x) / 2;
		float dif_y = Math.Abs(old_x - new_y) / 2;

		if (!toggle)
		{
			dif_x *= -1;
			dif_y *= -1;
		}

		// set size delta so images display at new zoom level
		input_image.rectTransform.sizeDelta = new Vector2(wide, high);
		output_image.rectTransform.sizeDelta = new Vector2(wide, high);

		var content_input = GameObject.Find("ContentInput").gameObject.GetComponent<RectTransform>();
		var content_output = GameObject.Find("ContentOutput").gameObject.GetComponent<RectTransform>();

		// offset so image stays centered
		content_input.anchoredPosition = new Vector2(content_input.anchoredPosition.x + dif_x, content_input.anchoredPosition.y + dif_y);
		content_output.anchoredPosition = new Vector2(content_output.anchoredPosition.x + dif_x, content_output.anchoredPosition.y + dif_y);

		perf("ZoomChange", time_start);
	}

	private void ZoomIn()
	{
		ZoomChange(true);
	}

	private void ZoomOut()
	{
		ZoomChange(false);
	}

	static void log(object a)
	{
		UnityEngine.Debug.Log(a);
	}

	private static void perf(string func, Int64 runtime)
	{
		PerfMon.Call("ImageUtilities", func, runtime);
	}

	private static void setupFuncs()
	{
		PerfMon.SetupFunc("ImageUtilities", "CIE2000");
		PerfMon.SetupFunc("ImageUtilities", "DefaultSettings");
		PerfMon.SetupFunc("ImageUtilities", "FindClosest");
		PerfMon.SetupFunc("ImageUtilities", "LoadImagePalette");
		PerfMon.SetupFunc("ImageUtilities", "SetOutputImage");
		PerfMon.SetupFunc("ImageUtilities", "ShowMainButtons");
		PerfMon.SetupFunc("ImageUtilities", "SyncPreview_Input");
		PerfMon.SetupFunc("ImageUtilities", "SyncPreview_Output");
		PerfMon.SetupFunc("ImageUtilities", "ZoomChange");
		PerfMon.SetupFunc("ImageUtilities", "ZoomReset");
	}
}

public static class ExtensionMethods
{
	public static TaskAwaiter GetAwaiter(this AsyncOperation asyncOp)
	{
		var tcs = new TaskCompletionSource<object>();
		asyncOp.completed += obj => { tcs.SetResult(null); };

		return ((Task)tcs.Task).GetAwaiter();
	}
}