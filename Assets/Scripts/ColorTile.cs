using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ColorTile : MonoBehaviour
{

    private GameObject obj_ref;
    private Image image_ref, overlay;
    private bool is_input_palette;

    public static ChangeColors cc_ref;
    public static ColorTile input_ref = null, output_ref = null;

    public Color32 color;
    public Color32 match;

    private void ClickHandler()
    {
        if (is_input_palette)
        {
            // turn off output_ref
            if(output_ref != null)
            {
                output_ref.Highlight(false);
                output_ref = null;
            }

            // no input color
            if (input_ref == null)
            {
                ChangeColors.SetColorInputs(color, match);
                input_ref = this;
                Highlight();
                HighlightMatch(match);
            }
            // change input color
            else if (input_ref != this)
            {
                ChangeColors.SetColorInputs(color, match);
                input_ref.Highlight(false);
                input_ref = this;
                Highlight();
                HighlightMatch(match);
            }
            // de-select input color
            else if (input_ref == this)
            {
                ChangeColors.SetColorInputs();
                input_ref.Highlight(false);
                input_ref = null;
            }
        }
        else
        {
            // no colors selected
            if (output_ref == null)
            {
                // set color info so user can inspect output palette
                ChangeColors.SetColorInputs(color);
                output_ref = this;
                Highlight();
            }
            // change output color (with input)
            else if (output_ref != this && input_ref != null)
            {
                // add undo action
                ChangeColors.AddUndo("change match", input_ref.color, input_ref.match);

                // change the match color for the input
                ChangeColors.SetColorInputs(input_ref.color, this.color);
                output_ref.Highlight(false);
                Highlight();
                output_ref = this;
                input_ref.match = this.color;

                // apply the change
                cc_ref.PickApply();
            }
            // change output color (no input)
            else if (output_ref != this && input_ref == null)
            {
                ChangeColors.SetColorInputs(color);
                output_ref.Highlight(false);
                output_ref = this;
                Highlight();
            }
            // de-select output color (no input)
            else if (input_ref == this && input_ref == null)
            {
                ChangeColors.SetColorInputs();
                input_ref.Highlight(false);
                output_ref.Highlight(false);
                input_ref = null;
                output_ref = null;
            }
        }
    }

    public ColorPlus GetColorPlus()
    {
        ColorPlus cp = new ColorPlus(color);
        cp.match = match;
        return cp;
    }

    public void Setup(bool input_true, Color32 the_color, Color32 the_match, ref GameObject the_reference)
    {
        useGUILayout = false;
        is_input_palette = input_true;
        match = the_match;
        obj_ref = the_reference;
        image_ref = obj_ref.GetComponent<Image>();
        obj_ref.GetComponent<Button>().onClick.AddListener(ClickHandler);
        overlay = obj_ref.transform.Find("Overlay").GetComponent<Image>();
        overlay.enabled = false;
        SetColor(the_color);
    }

    public void SetColor(Color32 col)
    {
        color = col;
        image_ref.color = col;
    }

    public void Highlight(bool show = true)
    {
        overlay.enabled = show;
    }

    public void HighlightMatch(Color32 m, bool show = true)
    {
        Color32 o;
        ColorTile ct;
        for (int i = 0; i != ChangeColors.output_colors.Count; i++)
        {
            ct = ChangeColors.output_colors[i].GetComponent(typeof(ColorTile)) as ColorTile;
            o = ct.color;
            if (m.Equals(o))
            {
                output_ref = ct;
                ct.Highlight(show);
                break;
            }
        }
    }
}