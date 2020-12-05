using System;
using System.Collections.Generic;
using UnityEngine;


public class LineSegment
{
    public const float EPSILON = 1e-5f;

    public LineSegment(Vector3 start, Vector3 end)
    {
        mStart = start;
        mEnd = end;
        if (mEnd.x < mStart.x)
        {
            mStart = end;
            mEnd = start;
        }
        else
        {
            mStart = start;
            mEnd = end;
        }
        if (mStart.x > mEnd.x)
        {
            throw new ArgumentException("Start X > End X");
        }
    }

    public Vector3 Start
    {
        get { return mStart; }
    }

    public Vector3 End
    {
        get { return mEnd; }
    }

    protected Vector3 mStart;
    protected Vector3 mEnd;
    public int VertexIndex = -1;
    public List<VertexEvent> Users = new List<VertexEvent>();

    public void Set(Vector3 start, Vector3 end)
    {
        mStart = start;
        mEnd = end;
    }

    public float CalcY(float x)
    {
        Vector3 delta = End - Start;
        float slope = delta.y / delta.x;

        return slope * (x - Start.x) + Start.y;
    }


    public bool IsCoincident(LineSegment line2)
    {
        Vector3 p1 = line2.Start;
        Vector3 p2 = line2.End;
        Vector3 p3 = Start;
        Vector3 p4 = End;
        Vector3 A = p2 - p1;
        Vector3 B = p3 - p4;
        float f = A.y * B.x - A.x * B.y;

        return (Math.Abs(f) < EPSILON);
    }

    public int FindIntersection(LineSegment line2, ref Vector3 intersection)
    {
        Vector3 p1 = line2.Start;
        Vector3 p2 = line2.End;
        Vector3 p3 = Start;
        Vector3 p4 = End;
        Vector3 A = p2 - p1;
        Vector3 B = p3 - p4;
        Vector3 C = p1 - p3;
        float f = A.y * B.x - A.x * B.y;
        float e = A.x * C.y - A.y * C.x;
        float d = B.y * C.x - B.x * C.y;

        // check to see if they are coincident
        if (Math.Abs(f) < EPSILON)
        {
            
            return -1;
        }
        float t = d / f;
        intersection = p1 + (t * A);

        if ((d == f) || (e == f))
        {
            return 0;
        }
        if ((d == 0) && (e == 0))
        {
            return 0;
        }
        if (f > 0)
        {
            if ((d < 0) || (d > f))
            {
                return 0;
            }
            if ((e < 0) || (e > f))
            {
                return -1;
            }
        }
        else
        {
            if ((d > 0) || (d < f))
            {
                return 0;
            }
            if ((e > 0) || (e < f))
            {
                return -1;
            }
        }

        Vector3 min1 = new Vector3(Math.Min(p1.x, p2.x), Math.Min(p1.y, p2.y));
        Vector3 max1 = new Vector3(Math.Max(p1.x, p2.x), Math.Max(p1.y, p2.y));
        Vector3 min2 = new Vector3(Math.Min(p3.x, p4.x), Math.Min(p3.y, p4.y));
        Vector3 max2 = new Vector3(Math.Max(p3.x, p4.x), Math.Max(p3.y, p4.y));

        if ((max1.x < min2.x) || (max2.x < min1.x))
        {
            return -1;
        }
        return 1;
    }

    public override string ToString()
    {
        return Start + " -> " + End;
    }
}

