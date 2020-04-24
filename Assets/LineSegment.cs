using System;
using UnityEngine;

public class LineSegment
{
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
        set { mEnd = value;  }
    }

    protected Vector3 mStart;
    protected Vector3 mEnd;

    /*
     * Find the intersection point between two line segments
     * @returns 0 lines are coincident
     *         -1 if lines are coincident or parallel
     *         1 if line segments intersect
     */
    public int FindIntersection(LineSegment line2, ref Vector3 intersection)
    {
        float d, e, f;
        float x1lo, x1hi, y1lo, y1hi;
        Vector3 A = End - Start;
        Vector3 B = line2.Start - line2.End;
        Vector3 C = Start - line2.Start;

        // X bound box test/
        if (A.x < 0)
        {
            x1lo = End.x; x1hi = Start.x;
        }
        else
        {
            x1hi = End.x; x1lo = Start.x;
        }
        if (B.x > 0)
        {
            if (x1hi < line2.End.x || line2.Start.x < x1lo)
            {
                return -1;
            }
        }
        else if (x1hi < line2.Start.x || line2.End.x < x1lo)
        {
            return -1;
        }

        // Y bound box test//
        if (A.y < 0)
        {
            y1lo = End.y; y1hi = Start.y;
        }
        else
        {
            y1hi = End.y; y1lo = Start.y;
        }

        if (B.y > 0)
        {
            if (y1hi < line2.End.y || line2.Start.y < y1lo)
            {
                return -1;
            }
        }
        else if (y1hi < line2.Start.y || line2.End.y < y1lo)
        {
            return -1;
        }
        d = B.y * C.x - B.x * C.y;  // alpha numerator//
        f = A.y * B.x - A.x * B.y;  // both denominator//

        // alpha tests//
        if (Mathf.Abs(f) > 1e-7)
        {
            intersection = Start + A * (d / f);
            if (f > 0)
            {
                // compute intersection coordinates //
                if (d < 0 || d > f)
                    return 0;
            }
        }
        else if (d > 0 || d < f)
        {
            return 0;
        }

        e = A.x * C.y - A.y * C.x;  // beta numerator//

        // beta tests //
        if (f > 0)
        {
            if (e < 0 || e > f) return 0;
        }
        else if (e > 0 || e < f) return 0;

        // check to see if they are coincident
        if ((Mathf.Abs(d) < 1e-7) &&
            (Mathf.Abs(e) < 1e-7))
        {
            return 0;
        }

        // check if they are parallel
        if (Mathf.Abs(f) < 1e-7)
        {
            return -1;
        }

        // segments intersect
        return 1;
    }

}