using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class VertexEvent
{
    private Vector3 mPoint;
    private Edge mEdge;
    private Edge mIntersectingEdge = null;

    public Vector3 Start
    {
        get { return mEdge.Line.Start; }
    }

    public Vector3 End
    {
        get { return mEdge.Line.End; }
    }

    public LineSegment Line
    {
        get { return mEdge.Line; }
    }

    public Edge TriEdge
    {
        get { return mEdge; }
    }

    public Edge IntersectingEdge
    {
        get { return mIntersectingEdge; }
    }

    public Vector3 Point
    {
        get { return mPoint; }
        set { mPoint = value; }
    }

    public int FindIntersection(Edge e2, ref Vector3 intersection)
    {
        return mEdge.FindIntersection(e2, ref intersection);
    }

    public override string ToString()
    {
        if (mPoint == mEdge.Line.Start)
        {
            return mPoint + " " + mEdge.ToString();
        }
        else
        {
            return mPoint + " T: " + (mEdge.Tri.VertexIndex / 3) +
                   " E: " + mEdge.EdgeIndex + " " +
                   End + " <- " + Start;
        }
    }

    public VertexEvent(Vector3 point, Triangle tri, int vertexIndex)
    {
        mPoint = point;
        mEdge = tri.Edges[vertexIndex];
    }

    public VertexEvent(Vector3 point, Edge edge)
    {
        mPoint = point;
        mEdge = edge;
    }

    public VertexEvent(Vector3 point, Edge edgeA, Edge edgeB)
    {
        mPoint = point;
        mEdge = edgeA;
        mIntersectingEdge = edgeB;
    }
}

public class EventCompare : IComparer<VertexEvent>
{
    int IComparer<VertexEvent>.Compare(VertexEvent p1, VertexEvent p2)
    {
        VecCompare vcompare = new VecCompare();

        int order = vcompare.Compare(p1.Point, p2.Point);
        if (order != 0)
        {
            return order;
        }
        Vector3 v1 = p1.Line.End - p1.Line.Start;
        Vector3 v2 = p2.Line.End - p2.Line.Start;
        Vector3 sweep = new Vector3(0, -1, 0);
        float a1, a2, t;

        v1.Normalize();
        v2.Normalize();
        a1 = Vector3.Dot(sweep, v1);
        a2 = Vector3.Dot(sweep, v2);
        t = a2 - a1;
        if (Math.Abs(t) > LineSegment.EPSILON)
        {
            return (t > 0) ? 1 : -1;
        }
        if (p1.IntersectingEdge != p2.IntersectingEdge)
        {
            if (p1.IntersectingEdge == null)
            {
                return -1;
            }
            if (p2.IntersectingEdge == null)
            {
                return 1;
            }
            return p1.IntersectingEdge.GetHashCode() - p2.IntersectingEdge.GetHashCode();
        }
        return p1.TriEdge.Tri.GetHashCode() - p2.TriEdge.Tri.GetHashCode();
     }
}
