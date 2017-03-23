using UnityEngine;
using System.Collections;

public class ColorHSL
{

    public float h; // hue
    public float s; // saturation
    public float l; // lightness (aka value)
    public float a; // alpha (transparency)

    private static float ONE_SIXTH = 1.0f / 6.0f;
    private static float ONE_THIRD = 1.0f / 3.0f;
    private static float TWO_THIRDS = 2.0f / 3.0f;

    public ColorHSL(Color c)
    {
        float min, max, delta, sixDelInv;
        a = c.a;

        max = c.r;
        if (max < c.g) max = c.g;
        if (max < c.b) max = c.b;
        min = c.r;
        if (min > c.g) min = c.g;
        if (min > c.b) min = c.b;

        delta = max - min;

        l = (max + min) * 0.5f;

        if (delta == 0.0f)
        {
            h = 0.0f;
            s = 0.0f;
        }
        else
        {
            if (l < 0.5f) s = delta / (max + min);
            else s = delta / (2.0f - max - min);

            sixDelInv = 1.0f / (6.0f * delta);
            if (c.r == max) h = (c.g - c.b) * sixDelInv;
            else if (c.g == max) h = ONE_THIRD + (c.b - c.r) * sixDelInv;
            else if (c.b == max) h = TWO_THIRDS + (c.r - c.g) * sixDelInv;

            if (h < 0.0f) h += 1.0f;
            else if (h > 1.0f) h -= 1.0f;
        }

    }

    public Color getRGBColor()
    {
        Color c = new Color();

        float u, v;
        c.a = a;

        if (s == 0.0f)
        {
            c.r = l;
            c.g = l;
            c.b = l;
        }
        else
        {
            v = 0.0f;
            if (l <= 0.5f) v = l * (1.0f + s);
            else v = l + s - l * s;

            u = 1.0f * l - v;

            if (h > TWO_THIRDS) c.r = getComponentFromHue(u, v, h + ONE_THIRD - 1.0f);
            else c.r = getComponentFromHue(u, v, h + ONE_THIRD);

            c.g = getComponentFromHue(u, v, h);

            if (h < ONE_THIRD) c.b = getComponentFromHue(u, v, h - ONE_THIRD + 1.0f);
            else c.b = getComponentFromHue(u, v, h - ONE_THIRD);
        }

        return c;
    }

    private float getComponentFromHue(float s, float t, float h)
    {
        if (h < ONE_SIXTH) return s + (t - s) * 6.0f * h;
        else if (h <= 0.5f) return t;
        else if (h < TWO_THIRDS) return s + (t - s) * 6.0f * (TWO_THIRDS - h);
        else return s;
    }
}
