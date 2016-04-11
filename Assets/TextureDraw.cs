using UnityEngine;
using System.Collections;

public class TextureDraw : MonoBehaviour {

    public void DrawLine(Texture2D tex, int x0, int y0, int x1, int y1, Color col)
    {
        int dy = (int)(y1 - y0);
        int dx = (int)(x1 - x0);
        int stepx, stepy;

        if (dy < 0) { dy = -dy; stepy = -1; }
        else { stepy = 1; }
        if (dx < 0) { dx = -dx; stepx = -1; }
        else { stepx = 1; }
        dy <<= 1;
        dx <<= 1;

        float fraction = 0;

        tex.SetPixel(x0, y0, col);
        if (dx > dy)
        {
            fraction = dy - (dx >> 1);
            while (Mathf.Abs(x0 - x1) > 1)
            {
                if (fraction >= 0)
                {
                    y0 += stepy;
                    fraction -= dx;
                }
                x0 += stepx;
                fraction += dy;
                tex.SetPixel(x0, y0, col);
            }
        }
        else
        {
            fraction = dx - (dy >> 1);
            while (Mathf.Abs(y0 - y1) > 1)
            {
                if (fraction >= 0)
                {
                    x0 += stepx;
                    fraction -= dy;
                }
                y0 += stepy;
                fraction += dx;
                tex.SetPixel(x0, y0, col);
            }
        }
    }
    public void DrawLine(byte[] tex,int width,int height, int x0, int y0, int x1, int y1, byte r, byte g, byte b, byte a = 255)
    {
        int dy = (int)(y1 - y0);
        int dx = (int)(x1 - x0);
        int stepx, stepy;

        if (dy < 0) { dy = -dy; stepy = -1; }
        else { stepy = 1; }
        if (dx < 0) { dx = -dx; stepx = -1; }
        else { stepx = 1; }
        dy <<= 1;
        dx <<= 1;

        float fraction = 0;

        tex[y0 * width * 4 + x0] =  r;
        tex[y0 * width * 4 + x0 + 1] = g;
        tex[y0 * width * 4 + x0 + 2] = b;
        tex[y0 * width * 4 + x0 + 3] = a;

        if (dx > dy)
        {
            fraction = dy - (dx >> 1);
            while (Mathf.Abs(x0 - x1) > 1)
            {
                if (fraction >= 0)
                {
                    y0 += stepy;
                    fraction -= dx;
                }
                x0 += stepx;
                fraction += dy;
                tex[y0 * width * 4 + x0] = r;
                tex[y0 * width * 4 + x0 + 1] = g;
                tex[y0 * width * 4 + x0 + 2] = b;
                tex[y0 * width * 4 + x0 + 3] = a;
            }
        }
        else
        {
            fraction = dx - (dy >> 1);
            while (Mathf.Abs(y0 - y1) > 1)
            {
                if (fraction >= 0)
                {
                    x0 += stepx;
                    fraction -= dy;
                }
                y0 += stepy;
                fraction += dx;

                tex[y0 * width * 4 + x0] = r;
                tex[y0 * width * 4 + x0 + 1] = g;
                tex[y0 * width * 4 + x0 + 2] = b;
                tex[y0 * width * 4 + x0 + 3] = a;
            }
        }
    }
}
