using UnityEngine;
using System.Collections;

public static class lineDrawer
{
    public static void toDraw(this Texture2D tex, Vector2 p1, Vector2 p2, Color col)
    {
        Vector2 t = p1;
        float frac = 1 / Mathf.Sqrt(Mathf.Pow(p2.x - p1.x, 2) + Mathf.Pow(p2.y - p1.y, 2));
        float ctr = 0;

        while ((int)t.x != (int)p2.x || (int)t.y != (int)p2.y)
        {
            t = Vector2.Lerp(p1, p2, ctr);
            ctr += frac;
            tex.SetPixel((int)t.x, (int)t.y, col);
        }
    }

    public static void toDraw(this byte[] tex,int width,int height, Vector2 p1, Vector2 p2, Color col)
    {
        Vector2 t = p1;
        float frac = 1 / Mathf.Sqrt(Mathf.Pow(p2.x - p1.x, 2) + Mathf.Pow(p2.y - p1.y, 2));
        float ctr = 0;

        while ((int)t.x != (int)p2.x || (int)t.y != (int)p2.y)
        {
            t = Vector2.Lerp(p1, p2, ctr);
            ctr += frac;
            //tex.SetPixel((int)t.x, (int)t.y, col);
            tex[(int)t.y * width * 4 + (int)t.x*4] = (byte)(col.r*255);
            tex[(int)t.y * width * 4 + (int)t.x*4 + 1] = (byte)(col.g * 255);
            tex[(int)t.y * width * 4 + (int)t.x*4 + 2] = (byte)(col.b * 255);
            tex[(int)t.y * width * 4 + (int)t.x*4 + 3] = (byte)(col.a * 255);
        }
    }
}
