using System;
using UnityEngine;

public class LineSegment
{
    public enum IntersectResult
    {
        INTERSECTING = 1,
        TOUCHING = 0,
        COINCIDENT = -1,
        NONINTERSECTING = -2,
    }

    public LineSegment(Vector3 start, Vector3 end)
    {
        mStart = start;
        mEnd = end;
    }

    public Vector3 Start
    {
        get { return mStart; }
        set { mStart = value; }
    }

    public Vector3 End
    {
        get { return mEnd; }
        set { mEnd = value; }
    }

    protected Vector3 mStart;
    protected Vector3 mEnd;

    /*
     * Find the intersection point between two line segments
     * @returns COINCIDENT lines are coincident
     *          NONINTERSECTING if lines segments do not intersect
     *          INTERSECTING if line segments intersect
     *          TOUCHING if line segments touch but dont penetrate
     */
    public int FindIntersection(LineSegment line2, ref Vector3 intersection)
    {
        Vector3 A = End - Start;                // s
        Vector3 B = line2.End - line2.Start;    // r
        Vector3 C = Start - line2.Start;        // q - p
        float f = A.x * B.y - A.y * B.x;  // denom (r x s)
        float e = A.x * C.y - A.y * C.x;  // snumer (q - p) x s
        float d = B.x * C.y - B.y * C.x;  // tnumer (q - p) x r
        bool denomPositive = (f > 0);

        // check to see if they are coincident
        if (f == 0)
        {
            if (d == 0)
            {
                return (int) IntersectResult.COINCIDENT;
            }
            return (int) IntersectResult.NONINTERSECTING;
        }
        if (f == e)
        {
            return (int) IntersectResult.COINCIDENT;
        }
        if ((e < 0) == denomPositive)
        {
            return (int) IntersectResult.NONINTERSECTING;
        }
        if ((d < 0) == denomPositive)
        {
            return (int) IntersectResult.NONINTERSECTING;
        }
        if (((e > f) == denomPositive) || ((d > f) == denomPositive))
        {
            return (int) IntersectResult.NONINTERSECTING;
        }
        d /= f;
        intersection = Start + d * A;
        if ((d == 0) || (d == 1))
        {
            return (int) IntersectResult.TOUCHING;
        }
        return (int) IntersectResult.INTERSECTING;
    }

    public float EvaluateAtX(float x)
    {
        float m = (End.y - Start.y) / (End.x - Start.x);
        float b = Start.y - m * Start.x;
        return m * x + b;
    }

}