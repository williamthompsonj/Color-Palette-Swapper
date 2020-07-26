using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System;
using System.Threading.Tasks;

public class ChangeColors : MonoBehaviour, IBeginDragHandler, IDragHandler
{
    private static GameObject pick_btn_save, pick_btn_cancel, pick_btn_undo, pick_btn_add;
    private static InputField pick_input_red, pick_input_green, pick_input_blue, pick_input_hex,
        pick_output_red, pick_output_green, pick_output_blue, pick_output_hex,
        adder_red, adder_green, adder_blue, adder_hex;

    private List<ColorPlus> orig_input_palette, orig_output_palette;
    private GameObject color_picker = null, color_adder = null;
    private Image adder_preview;

    private Vector3 drag_vec, current_vec, pick_hide_vec, color_hide_vec;

    public GameObject prefab;

    public static GameObject input_viewport, output_viewport;
    public static List<GameObject> input_colors, output_colors;
    public static List<ColorTile> input_tiles, output_tiles;
    public static List<UndoAction> undo;

    private void Start()
    {
        useGUILayout = false;

        // get color picker items
        color_picker = GameObject.Find("ColorPicker");
        input_viewport = GameObject.Find("Input_Viewport_Content");
        output_viewport = GameObject.Find("Output_Viewport_Content");

        GameObject.Find("Main_BtnColorPicker").GetComponent<Button>().onClick.AddListener(ShowColorPicker);
        pick_btn_save = color_picker.transform.Find("Pick_BtnSave").gameObject;
        pick_btn_cancel = color_picker.transform.Find("Pick_BtnCancel").gameObject;
        pick_btn_undo = color_picker.transform.Find("Pick_BtnUndo").gameObject;
        pick_btn_add = color_picker.transform.Find("Pick_BtnAddColor").gameObject;

        pick_btn_save.GetComponent<Button>().onClick.AddListener(PickSave);
        pick_btn_cancel.GetComponent<Button>().onClick.AddListener(PickCancel);
        pick_btn_undo.GetComponent<Button>().onClick.AddListener(PickUndo);
        pick_btn_add.GetComponent<Button>().onClick.AddListener(PickAddColor);

        pick_input_red    = color_picker.transform.Find("Pick_Input_Red").GetComponent<InputField>();
        pick_input_green  = color_picker.transform.Find("Pick_Input_Green").GetComponent<InputField>();
        pick_input_blue   = color_picker.transform.Find("Pick_Input_Blue").GetComponent<InputField>();
        pick_input_hex    = color_picker.transform.Find("Pick_Input_Hex").GetComponent<InputField>();
        pick_output_red   = color_picker.transform.Find("Pick_Output_Red").GetComponent<InputField>();
        pick_output_green = color_picker.transform.Find("Pick_Output_Green").GetComponent<InputField>();
        pick_output_blue  = color_picker.transform.Find("Pick_Output_Blue").GetComponent<InputField>();
        pick_output_hex   = color_picker.transform.Find("Pick_Output_Hex").GetComponent<InputField>();

        // get color adder items
        color_adder = GameObject.Find("AddColor");

        adder_red   = color_adder.transform.Find("AddColor_Input_Red").GetComponent<InputField>();
        adder_green = color_adder.transform.Find("AddColor_Input_Green").GetComponent<InputField>();
        adder_blue  = color_adder.transform.Find("AddColor_Input_Blue").GetComponent<InputField>();
        adder_hex   = color_adder.transform.Find("AddColor_Input_Hex").GetComponent<InputField>();

        adder_preview = color_adder.transform.Find("AddColor_Preview").GetComponent<Image>();

        adder_red.text = "0";
        adder_green.text = "128";
        adder_blue.text = "128";
        adder_hex.text = "008080";

        adder_red.onValueChanged.AddListener(ColorAdder_Change_Red);
        adder_green.onValueChanged.AddListener(ColorAdder_Change_Green);
        adder_blue.onValueChanged.AddListener(ColorAdder_Change_Blue);
        adder_hex.onValueChanged.AddListener(ColorAdder_Change_Hex);

        GameObject.Find("AddColor_BtnOkay").GetComponent<Button>().onClick.AddListener(ColorAdderOkay);
        GameObject.Find("AddColor_BtnCancel").GetComponent<Button>().onClick.AddListener(ColorAdderCancel);

        // shared input and output color palettes
        input_colors = new List<GameObject>();
        output_colors = new List<GameObject>();

        input_tiles = new List<ColorTile>();
        output_tiles = new List<ColorTile>();

        orig_input_palette = new List<ColorPlus>();
        orig_output_palette = new List<ColorPlus>();

        // setup undo list
        undo = new List<UndoAction>();

        // initialize current menu position to center of the screen
        current_vec = new Vector3();
        pick_hide_vec = new Vector3(400, 600, 3);
        color_hide_vec = new Vector3(0, 800, 5);

        // make these menus really hidden
        color_picker.transform.position = pick_hide_vec;
        color_adder.transform.position = color_hide_vec;

        // set reference to this so all ColorTiles can access PickApply()
        ColorTile.cc_ref = this;
    }

    private void PickSave()
    {
        // clear palettes
        ImageUtilities.input_palette.Clear();
        ImageUtilities.output_palette.Clear();

        // load current color palette state
        foreach (ColorTile c in input_tiles)
        {
            ImageUtilities.input_palette.Add(c);
        }

        foreach (ColorTile c in output_tiles)
        {
            ImageUtilities.output_palette.Add(c);
        }

        // hide settings menu and restore buttons so user can work
        PickClose();
    }

    private void PickCancel()
    {
        // clear palettes
        ImageUtilities.input_palette.Clear();
        ImageUtilities.output_palette.Clear();

        // load original color palette state
        ImageUtilities.input_palette.AddRange(orig_input_palette);
        ImageUtilities.output_palette.AddRange(orig_output_palette);

        // apply original coloring
        ImageUtilities.SetOutputImage();

        // close menu
        PickClose();
    }

    private void PickUndo()
    {
        ColorTile ct, temp;

        // no undo actions to perform
        if (undo.Count == 0)
        {
            // remove highlight on current input
            if (ColorTile.input_ref != null)
            {
                temp = ColorTile.input_ref;
                temp.Highlight(false);
            }

            // remove highlight on current output
            if (ColorTile.output_ref != null)
            {
                temp = ColorTile.output_ref;
                temp.Highlight(false);
            }

            // clear color inputs
            SetColorInputs();

            return;
        }

        UndoAction action = undo[undo.Count - 1];
        undo.Remove(action);

        switch(action.action)
        {
            case "change match":
                for (int i = 0; i != input_tiles.Count; i++)
                {
                    ct = input_tiles[i];

                    // this is the input color we want to change
                    if (ct.color.Equals(action.color))
                    {
                        // remove highlight on current input
                        if (ColorTile.input_ref != null)
                        {
                            temp = ColorTile.input_ref;
                            temp.Highlight(false);
                        }

                        // remove highlight on current output
                        if (ColorTile.output_ref != null)
                        {
                            temp = ColorTile.output_ref;
                            temp.Highlight(false);
                        }

                        ColorTile.input_ref = ct;
                        ct.match = action.match;
                        ct.Highlight();
                        ct.HighlightMatch(ct.match);
                        SetColorInputs(ct.color, ct.match);
                        PickApply();
                        break;
                    }
                }
                break;

            case "delete color":
                for (int i = output_colors.Count - 1; i != -1; i--)
                {
                    ct = output_tiles[i];

                    // this is the input color we want to change
                    if (ct.color.Equals(action.color))
                    {
                        UnityEngine.Object.Destroy(output_colors[i]);
                        output_colors.RemoveAt(i);
                        output_tiles.RemoveAt(i);
                        break;
                    }
                }
                break;
        }
    }

    private void PickAddColor()
    {
        // default teal color for example, teal looks nice
        ColorAdder_Change_Hex("008080");
        PickToggleButtons(false);
        color_adder.transform.position = new Vector3(0, 0, 0);
    }

    private void PickClose()
    {
        undo.Clear();
        ImageUtilities.ShowMainButtons();
        current_vec = color_picker.transform.position;
        color_picker.transform.position = pick_hide_vec;
        ClearPalettes();
    }

    private void PickToggleButtons(bool show_buttons)
    {
        pick_btn_save.SetActive(show_buttons);
        pick_btn_cancel.SetActive(show_buttons);
        pick_btn_undo.SetActive(show_buttons);
        pick_btn_add.SetActive(show_buttons);
    }

    private void ShowColorPicker()
    {
        ImageUtilities.HideMainButtons();
        PopulatePalettes();
        color_picker.transform.position = current_vec;
    }

    private void PopulatePalettes()
    {
        // copy original state in case user hits cancel
        orig_input_palette.AddRange(ImageUtilities.input_palette);
        orig_output_palette.AddRange(ImageUtilities.output_palette);

        // ensure we have a valid input palette
        if (ImageUtilities.input_palette.Count == 0) return;

        ColorPlus[] input_palette = new ColorPlus[ImageUtilities.input_palette.Count];
        ColorPlus[] output_palette;

        if (ImageUtilities.sort_saved_palette)
            ImageUtilities.StepSort(ImageUtilities.input_palette).CopyTo(input_palette);
        else
            ImageUtilities.input_palette.CopyTo(input_palette);

        // figure out if an output palette has been defined or not (use input as default)
        if (ImageUtilities.output_palette.Count == 0)
        {
            output_palette = new ColorPlus[input_palette.Length];
            input_palette.CopyTo(output_palette, 0);
        }
        else
        {
            output_palette = new ColorPlus[ImageUtilities.output_palette.Count];
            if (ImageUtilities.sort_saved_palette)
                ImageUtilities.StepSort(ImageUtilities.output_palette).CopyTo(output_palette);
            else
                ImageUtilities.output_palette.CopyTo(output_palette);
        }

        // tell all the tiles they are input colors
        GameObject[] temp_objs = new GameObject[input_palette.Length];
        ColorTile[] temp_tiles = new ColorTile[input_palette.Length];
        int index = 0;

        for (int i = 0; i != input_palette.Length; i++)
        {
            // don't list transparent colors
            if (input_palette[i].alpha < ImageUtilities.transparent_threshhold) continue;

            // new color tile
            temp_objs[index] = Instantiate(prefab, input_viewport.transform) as GameObject;
            temp_objs[index].name = "input_color_" + index.ToString();

            // set color tile color
            temp_tiles[index] = temp_objs[index].GetComponent(typeof(ColorTile)) as ColorTile;
            temp_tiles[index].Setup(true, input_palette[i].color32, input_palette[i].match, ref temp_objs[index]);

            index++;
        }

        GameObject[] temp1 = new GameObject[index];
        Array.Copy(temp_objs, temp1, index);
        input_colors.AddRange(temp1);

        ColorTile[] temp2 = new ColorTile[index];
        Array.Copy(temp_tiles, temp2, index);
        input_tiles.AddRange(temp2);

        temp_objs = new GameObject[output_palette.Length];
        temp_tiles = new ColorTile[output_palette.Length];
        index = 0;

        for (int i = 0; i != output_palette.Length; i++)
        {
            // don't list transparent colors
            if (output_palette[i].alpha < ImageUtilities.transparent_threshhold) continue;

            // new color tile
            temp_objs[index] = Instantiate(prefab, output_viewport.transform) as GameObject;
            temp_objs[index].name = "output_color_" + index.ToString();

            // set color tile color
            temp_tiles[index] = temp_objs[index].GetComponent(typeof(ColorTile)) as ColorTile;
            temp_tiles[index].Setup(false, output_palette[i].color32, output_palette[i].match, ref temp_objs[index]);

            index++;
        }

        temp1 = new GameObject[index];
        Array.Copy(temp_objs, temp1, index);
        output_colors.AddRange(temp1);

        temp2 = new ColorTile[index];
        Array.Copy(temp_tiles, temp2, index);
        output_tiles.AddRange(temp2);
    }

    private void ClearPalettes()
    {
        // destroy input tiles
        for (int i = 0; i != input_colors.Count; i++)
        {
            input_colors[i].SetActive(false);
            GameObject.Destroy(input_colors[i]);
        }

        // destroy output tiles
        for (int i = 0; i != output_colors.Count; i++)
        {
            output_colors[i].SetActive(false);
            GameObject.Destroy(output_colors[i]);
        }

        // clear references from our shared lists so we can easily and quickly use them again
        input_colors.Clear();
        output_colors.Clear();

        input_tiles.Clear();
        output_tiles.Clear();

        // dump all child references in color picker so GC can easily clear them
        input_viewport.transform.DetachChildren();
        output_viewport.transform.DetachChildren();

        // clear local storage
        orig_input_palette.Clear();
        orig_output_palette.Clear();

        // set all our fields to empty strings
        pick_input_red.text = string.Empty;
        pick_input_green.text = string.Empty;
        pick_input_blue.text = string.Empty;
        pick_input_hex.text = string.Empty;
        pick_output_red.text = string.Empty;
        pick_output_green.text = string.Empty;
        pick_output_blue.text = string.Empty;
        pick_output_hex.text = string.Empty;
    }

    public static void AddUndo(string Action, Color32 TheColor, Color32 TheMatch)
    {
        undo.Add(new UndoAction(Action, TheColor, TheMatch));
    }

    public static void SetColorInputs()
    {
        pick_input_hex.text = string.Empty;
        pick_input_red.text = string.Empty;
        pick_input_green.text = string.Empty;
        pick_input_blue.text = string.Empty;

        pick_output_hex.text = string.Empty;
        pick_output_red.text = string.Empty;
        pick_output_green.text = string.Empty;
        pick_output_blue.text = string.Empty;
    }

    public static void SetColorInputs(Color32 output)
    {
        pick_output_red.text = output.r.ToString();
        pick_output_green.text = output.g.ToString();
        pick_output_blue.text = output.b.ToString();
        pick_output_hex.text = string.Format("{0:x2}{1:x2}{2:x2}", output.r, output.g, output.b);
    }

    public static void SetColorInputs(Color32 input, Color32 output)
    {
        pick_input_red.text = input.r.ToString();
        pick_input_green.text = input.g.ToString();
        pick_input_blue.text = input.b.ToString();
        pick_input_hex.text = string.Format("{0:x2}{1:x2}{2:x2}", input.r, input.g, input.b);

        SetColorInputs(output);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        Rect r = color_picker.GetComponent<RectTransform>().rect;
        drag_vec = Camera.main.ScreenToWorldPoint(color_picker.transform.position + ImageUtilities.screen_offset + new Vector3(r.width / 2, -r.height / 2, 0));
    }

    public void OnDrag(PointerEventData eventData)
    {
        color_picker.transform.position = Camera.main.ScreenToWorldPoint(eventData.position) - drag_vec;
    }

    public void PickApply()
    {
        // reset output palette
        ImageUtilities.input_palette.Clear();
        ImageUtilities.output_palette.Clear();

        // cycle through input color tiles and apply to input palette
        for (int i = 0; i != input_tiles.Count; i++)
        {
            ImageUtilities.input_palette.Add(input_tiles[i]);
        }

        // cycle through new output palette
        for (int i = 0; i != output_tiles.Count; i++)
        {
            ImageUtilities.output_palette.Add(output_tiles[i]);
        }

        // reload the preview with new color matches
        ImageUtilities.SetOutputImage();
    }

    /*
     * Color Adder Stuff
     */
    private void ColorAdderOkay()
    {
        bool exists = false;
        ColorTile ct;

        // cycle through output colors
        for (int i = 0; i != output_tiles.Count; i++)
        {
            // check if this exists in output colors
            if (adder_preview.color.Equals(output_tiles[i].color))
            {
                exists = true;
                break;
            }
        }
        
        // check if we have this color already
        if(!exists)
        {
            // create a new color tile
            GameObject newObj = Instantiate(prefab, output_viewport.transform) as GameObject;
            newObj.name = "output_color_" + output_tiles.Count.ToString();
            ct = newObj.GetComponent(typeof(ColorTile)) as ColorTile;
            ct.Setup(false, adder_preview.color, adder_preview.color, ref newObj);

            // add to the output palette
            output_colors.Add(newObj);

            // add color tile to our list of tiles
            output_tiles.Add(ct);

            // add undo action
            ChangeColors.AddUndo("delete color", adder_preview.color, adder_preview.color);
        }

        // close color adder normally
        ColorAdderCancel();
    }

    private void ColorAdderCancel()
    {
        // close color adder normally
        PickToggleButtons(true);
        color_adder.transform.position = color_hide_vec;
    }

    private void ColorAdder_Change_Red(string s)
    {
        adder_red.text = ImageUtilities.SafeInt(s, 0, 255);
        ColorAdder_Update_Hex();
    }

    private void ColorAdder_Change_Green(string s)
    {
        adder_green.text = ImageUtilities.SafeInt(s, 0, 255);
        ColorAdder_Update_Hex();
    }

    private void ColorAdder_Change_Blue(string s)
    {
        adder_blue.text = ImageUtilities.SafeInt(s, 0, 255);
        ColorAdder_Update_Hex();
    }

    private void ColorAdder_Change_Hex(string s)
    {
        // remove non-hex characters
        s = Regex.Replace(s.ToLower(), "[^0-9a-f]*", string.Empty);

        // set the field to the safe value
        adder_hex.text = s;

        // update if we have a full hex value
        if (s.Length == 6)
        {
            adder_red.text   = Mathf.Clamp(int.Parse(s.Substring(0, 2), System.Globalization.NumberStyles.HexNumber), 0, 255).ToString();
            adder_green.text = Mathf.Clamp(int.Parse(s.Substring(2, 2), System.Globalization.NumberStyles.HexNumber), 0, 255).ToString();
            adder_blue.text  = Mathf.Clamp(int.Parse(s.Substring(4, 2), System.Globalization.NumberStyles.HexNumber), 0, 255).ToString();

            ColorAdder_PreviewColor();
        }
    }

    private void ColorAdder_Update_Hex()
    {
        adder_hex.text = ImageUtilities.RGB2Hex(int.Parse(adder_red.text), int.Parse(adder_green.text), int.Parse(adder_blue.text));
    }

    private void ColorAdder_PreviewColor()
    {
        adder_preview.color = new Color32(
        (byte)int.Parse(adder_red.text),
        (byte)int.Parse(adder_green.text),
        (byte)int.Parse(adder_blue.text),
        (byte)255
        );
    }
}

public struct UndoAction
{
    public string action;
    public Color32 color, match;

    public UndoAction(string Action, Color32 TheColor, Color32 TheMatch)
    {
        action = Action;
        color = TheColor;
        match = TheMatch;
    }
}