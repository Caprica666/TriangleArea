﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class Edge
{
    private Triangle mTriangle;
    private LineSegment mLine;
    private int mEdgeIndex;

    public LineSegment Line
    {
        get { return mLine; }
    }

    public Triangle Tri
    {
        get { return mTriangle; }
    }

    public int EdgeIndex
    {
        get { return mEdgeIndex; }
    }

    public Edge(Triangle tri, int edgeIndex)
    {
        mTriangle = tri;
        mEdgeIndex = edgeIndex;
        mLine = new LineSegment(tri.GetVertex(edgeIndex),
                                tri.GetVertex((edgeIndex + 1) % 3));
    }

    public Edge(Edge src, int edgeIndex)
    {
        mTriangle = src.mTriangle;
        mEdgeIndex = edgeIndex;
        mLine = src.Line;
    }

    public int FindIntersection(Edge e2, ref Vector3 intersection)
    {
        LineSegment line2 = e2.Line;
        if (Line.Start.x == Line.End.x)
        {
            if (line2.Start.x == line2.End.x)
            {
                return -1;
            }
            intersection.x = Line.Start.x;
            intersection.y = line2.CalcY(intersection.x);
            if ((intersection.y >= Line.Start.y) && (intersection.y <= Line.End.y))
            {
                return 1;
            }
            return -1;
        }
        return Line.FindIntersection(line2, ref intersection);
    }

    public override string ToString()
    {
        return " T: " + (Tri.VertexIndex / 3) + " E: " + EdgeIndex + " " + Line.ToString();
    }
}

public class EdgeCompare : Comparer<Edge>
{
    public float CurrentX = float.MaxValue;

    public EdgeCompare() { }

    public override int Compare(Edge e1, Edge e2)
    {
        LineSegment s1 = e1.Line;
        LineSegment s2 = e2.Line;

        if ((e1.Tri == e2.Tri) && (e1.EdgeIndex == e2.EdgeIndex))
        {
            return 0;
        }

        float y1 = s1.CalcY(CurrentX);
        float y2 = s2.CalcY(CurrentX);
        float t = y1 - y2;

        if (Math.Abs(t) > LineSegment.EPSILON)
        {
            return (t > 0) ? 1 : -1;
        }
        return e1.EdgeIndex - e2.EdgeIndex;
    }
}

public class VecCompare : Comparer<Vector3>
{
    public override int Compare(Vector3 v1, Vector3 v2)
    {
        float epsilon = 1e-6f;
        float t = v1.x - v2.x;

        if (Math.Abs(t) > epsilon)
        {
            return (t > 0) ? 1 : -1;
        }
        t = v1.y - v2.y;
        if (Math.Abs(t) > epsilon)
        {
            return (t > 0) ? 1 : -1;
        }
        return 0;
    }
}