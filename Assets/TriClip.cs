using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using UnityEngine;

public class EdgeClip : IComparable<EdgeClip>
{
    public Edge Clipper;
    public Edge Clipped;
    public int ClipperEdgeStart;
    public int ClipperEdgeEnd;
    public int ClippedEdgeStart;
    public int ClippedEdgeEnd;
    public int Status;
    public int HashKey;
    public Vector3 IntersectionPoint;
    public Vector3 IsectBaryCoords;
    public const int INTERSECTING = 1;
    public const int EDGE = 2;
    public const int OUTSIDE = 4;
    public const int COINCIDENT = 8;
    public const float EPSILON = 2e-6f;

    public EdgeClip(Edge clipper, Edge clipped) 
    {
        Clipper = clipper;
        Clipped = clipped;
        ClipperEdgeStart = clipper.EdgeIndex;
        ClippedEdgeStart = clipped.EdgeIndex;
        ClipperEdgeEnd = (ClipperEdgeStart + 1) % 3;
        ClippedEdgeEnd = (ClippedEdgeStart + 1) % 3;
        HashKey = (ClipperEdgeStart << 6) | (ClipperEdgeEnd << 4) |
                  (ClippedEdgeStart << 2) | ClippedEdgeEnd;
    }

    public int Clip()
    {
        Status = FindIntersection();
        HashKey |= (Status << 8);
        return Status;
    }

    /*
    * Find the intersection point between two line segments
    * @returns COINCIDENT lines are coincident
    *          NONINTERSECTING if lines segments do not intersect
    *          INTERSECTING if line segments intersect
    *          VERTEX if line segments meet at an endpoint
    */
    private int FindIntersection()
    {
        Vector3 p1 = Clipped.Tri.GetVertex(ClippedEdgeStart);
        Vector3 p2 = Clipped.Tri.GetVertex(ClippedEdgeEnd);
        Vector3 p3 = Clipper.Tri.GetVertex(ClipperEdgeStart);
        Vector3 p4 = Clipper.Tri.GetVertex(ClipperEdgeEnd);
        Vector3 A = p2 - p1;
        Vector3 B = p3 - p4;
        Vector3 C = p1 - p3;
        float f = A.y * B.x - A.x * B.y;
        float e = A.x * C.y - A.y * C.x;
        float d = B.y * C.x - B.x * C.y;

        // check to see if they are coincident
        if (Math.Abs(f) < EPSILON)
        {
            if (Math.Abs(d) < EPSILON)
            {
                return COINCIDENT;
            }
            return 0;
        }
        float t = d / f;
        IntersectionPoint = Clipped.Tri.GetVertex(ClippedEdgeStart) + (t * A);
        Clipped.Tri.Bary(IntersectionPoint, ref IsectBaryCoords);

        if ((IsectBaryCoords[0] == 1) ||
            (IsectBaryCoords[1] == 1) ||
            (IsectBaryCoords[2] == 1))
        {
            return INTERSECTING | EDGE;
        }
        if ((d == f) || (e == f))
        {
            return INTERSECTING | EDGE;
        }
        if ((d == 0) && (e == 0))
        {
            return INTERSECTING | EDGE;
        }
        if (f > 0)
        {
            if ((d < 0) || (d > f))
            {
                return 0;
            }
            if ((e < 0) || (e > f))
            {
                return INTERSECTING | OUTSIDE;
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
                return INTERSECTING | OUTSIDE;
            }
        }

        Vector3 min1 = new Vector3(Math.Min(p1.x, p2.x), Math.Min(p1.y, p2.y));
        Vector3 max1 = new Vector3(Math.Max(p1.x, p2.x), Math.Max(p1.y, p2.y));
        Vector3 min2 = new Vector3(Math.Min(p3.x, p4.x), Math.Min(p3.y, p4.y));
        Vector3 max2 = new Vector3(Math.Max(p3.x, p4.x), Math.Max(p3.y, p4.y));

        if ((IntersectionPoint.x < min1.x) || (IntersectionPoint.x > max1.x))
        {
            return INTERSECTING | OUTSIDE;
        }
        return INTERSECTING;
    }

    public int CompareTo(EdgeClip other)
    {
        return HashKey - other.HashKey;
    }
}

public enum ClipResult
{
    OUTSIDE = -1,
    INSIDE = 0,
    CLIPPED = 2,
    COINCIDENT = 1,
    INTERSECTED = 3
}
public class TriClip
{
    public List<EdgeClip> EdgesClipped = new List<EdgeClip>();
    public Edge Clipper;
    public Edge Clipped;
    private int mCoincident;
    private int[] mInside = new int[3];
    public const float EPSILON = 2e-7f;

    public TriClip(Edge clipper, Edge clipped)
    {
        Clipper = clipper;
        Clipped = clipped;
        mInside[0] = Clipper.Tri.Contains(Clipped.Tri.GetVertex(0));
        mInside[1] = Clipper.Tri.Contains(Clipped.Tri.GetVertex(1));
        mInside[2] = Clipper.Tri.Contains(Clipped.Tri.GetVertex(2));
    }

    private bool IsInsideClipper()
    {
        const int V2_IN = 0x20;
        const int V2_EDGE = 0x10;
        const int V1_IN = 8;
        const int V1_EDGE = 4;
        const int V0_IN = 2;
        const int V0_EDGE = 1;
        int mask = ((mInside[2] > 0) ? V2_IN : ((mInside[2] == 0) ? V2_EDGE : 0));
        mask |= ((mInside[1] > 0) ? V1_IN : ((mInside[1] == 0) ? V1_EDGE : 0));
        mask |= ((mInside[0] > 0) ? V0_IN : ((mInside[0] == 0) ? V0_EDGE : 0));
        switch (mask)
        {
            case V0_IN | V1_IN | V2_IN:         // all verts inside
            case V0_IN | V1_EDGE | V2_EDGE:     // one inside, 2 on the edge
            case V1_IN | V0_EDGE | V2_EDGE:
            case V2_IN | V0_EDGE | V1_EDGE:
            case V0_IN | V1_IN | V2_EDGE:       // two inside, 1 on the edge
            case V0_IN | V2_IN | V1_EDGE:
            case V1_IN | V2_IN | V0_EDGE:

            case V0_EDGE | V1_EDGE | V2_EDGE:
            return true;                         // Clipped inside Clipper
        }
        return false;
    }

    public ClipResult ClipTri(List<Triangle> clipped)
    {
        if (IsInsideClipper())
        {
            return ClipResult.INSIDE;
        }
        List<EdgeClip> edgesclipped = new List<EdgeClip>();
        EdgeClip edge1;
        EdgeClip edge2;

        mCoincident = -1;
        int n = ClipAgainstEdge(Clipper, edgesclipped);

        if (mCoincident == Clipped.EdgeIndex)
        {
            return ClipResult.COINCIDENT;
        }
        if (n <= 0)
        {
            return ClipResult.OUTSIDE;
        }
        edge1 = edgesclipped[0];
        if (n == 2)
        {
            edge2 = edgesclipped[1];
        }
        else // n == 1
        {
            int adjedge1 = (Clipper.EdgeIndex + 1) % 3;
            int adjedge2 = (Clipper.EdgeIndex + 2) % 3;
            Edge adjacent;

            if (Clipped.Tri.Contains(edge1.Clipper.Tri.Vertices[edge1.ClipperEdgeStart]) > 0)
            {
                adjacent = Clipper.Tri.Edges[adjedge2];
            }
            else if (Clipped.Tri.Contains(edge1.Clipper.Tri.Vertices[edge1.ClipperEdgeEnd]) > 0)
            {
                adjacent = Clipper.Tri.Edges[adjedge1];
            }
            else
            {
                return (mCoincident == Clipped.EdgeIndex) ? ClipResult.COINCIDENT : ClipResult.OUTSIDE;
            }
            mCoincident = -1;
            edgesclipped.Clear();
            n = ClipAgainstEdge(adjacent, edgesclipped);
            if (n < 1)
            {
                return ClipResult.OUTSIDE;
            }
            edge2 = edgesclipped[0];
        }
        if (mCoincident == edge2.Clipped.EdgeIndex)
        {
            return ClipResult.COINCIDENT;
        }
        ClipResult r = ClipTriangles(edge1, edge2, clipped);
        switch (r)
        {
            case ClipResult.CLIPPED:
            case ClipResult.INSIDE:
            return r;
        }
        return ClipResult.OUTSIDE;
    }

    public ClipResult ClipOneEdge(List<Triangle> clipped)
    {
        if (IsInsideClipper())
        {
            return ClipResult.INSIDE;
        }
        List<EdgeClip> edgesclipped = new List<EdgeClip>();
        EdgeClip edge1;
        EdgeClip edge2;

        mCoincident = -1;
        int n = ClipAgainstEdge(Clipper, edgesclipped);

        if (mCoincident == Clipped.EdgeIndex)
        {
            return ClipResult.COINCIDENT;
        }
        if (n <= 0)
        {
            return ClipResult.OUTSIDE;
        }
        edge1 = edgesclipped[0];
        if (n == 2)
        {
            edge2 = edgesclipped[1];
        }
        else
        {            
            return ClipResult.OUTSIDE;
        }
        ClipResult r = ClipTriangles(edge1, edge2, clipped);
        switch (r)
        {
            case ClipResult.CLIPPED:
            case ClipResult.INSIDE:
            return r;
        }
        return ClipResult.OUTSIDE;
    }

    private int ClipAgainstEdge(Edge clipper, List<EdgeClip> clipedges)
    {
        int intersected = 0;
        int[] clipstatus = new int[3];
        EdgeClip[] alledges = new EdgeClip[3];

        for (int i = 0; i < 3; ++i)
        {
            alledges[i] = new EdgeClip(clipper, Clipped.Tri.Edges[i]);
            clipstatus[i] = alledges[i].Clip();

            if (((clipstatus[i] & EdgeClip.INTERSECTING) != 0) &&
                ((clipstatus[i] & EdgeClip.OUTSIDE) == 0))
            {
                if ((clipstatus[i] & EdgeClip.EDGE) != 0)
                {
                    clipedges.Add(alledges[i]);
                }
                else
                {
                    clipedges.Insert(0, alledges[i]);
                }
                if (++intersected > 1)
                {
                    return intersected;
                }
            }
            else if ((clipstatus[i] & EdgeClip.COINCIDENT) != 0)
            {
                mCoincident = alledges[i].Clipper.EdgeIndex;
            }
        }
        return intersected;
    }


    /*
     * returns INVALID if intersections points are the same
     * returns INSIDE if clipped triangle inside clipper triangle
     * returns CLIPPED if clipped triangles were generated
     */
    ClipResult ClipTriangles(EdgeClip edge1, EdgeClip edge2, List<Triangle> clipped)
    {
        Vector3 temp1;
        Vector3 temp2;
        Triangle tri;
        const int ISECTA = 1;
        const int ISECTB = 2;
        const int ISECTC = 4;
        int mask = ISECTA | ISECTB | ISECTC;
        Vector3 va;
        Vector3 vb;
        Vector3 vc;
        Vector3 isect1 = edge1.IntersectionPoint;
        Vector3 isect2 = edge2.IntersectionPoint;

        if (edge1.ClippedEdgeEnd == edge2.ClippedEdgeStart)
        {
            va = Clipped.Tri.GetVertex(edge1.ClippedEdgeEnd);
            vb = Clipped.Tri.GetVertex(edge1.ClippedEdgeStart);
            vc = Clipped.Tri.GetVertex(edge2.ClippedEdgeEnd);
        }
        else // edge1.ClippedEdgeStart != edge2.ClippedEdgeEnd
        {
            va = Clipped.Tri.GetVertex(edge1.ClippedEdgeStart);
            vb = Clipped.Tri.GetVertex(edge1.ClippedEdgeEnd);
            vc = Clipped.Tri.GetVertex(edge2.ClippedEdgeStart);
        }
        temp1 = isect1 - isect2;
        if (temp1.sqrMagnitude <= EPSILON)
        {
            return ClipResult.OUTSIDE;
        }
        temp1 = va - isect1;
        temp2 = va - isect2;
        if ((temp1.sqrMagnitude > EPSILON) &&
            (temp2.sqrMagnitude > EPSILON))
        {
            mask &= ~ISECTA;
        }
        temp1 = vb - isect1;
        temp2 = vb - isect2;
        if ((temp1.sqrMagnitude > EPSILON) &&
            (temp2.sqrMagnitude > EPSILON))
        {
            mask &= ~ISECTB;
        }
        temp1 = vc - isect1;
        temp2 = vc - isect2;
        if ((temp1.sqrMagnitude > EPSILON) &&
            (temp2.sqrMagnitude > EPSILON))
        {
            mask &= ~ISECTC;
        }
        switch (mask)
        {
            case ISECTA:
            tri = new Triangle(vb, isect1, isect2);
            if (!Clipper.Tri.Contains(tri))
            {
                clipped.Add(tri);
            }
            tri = new Triangle(vc, isect1, isect2);
            if (!Clipper.Tri.Contains(tri))
            {
                clipped.Add(tri);
            }
            return ClipResult.CLIPPED;

            case ISECTB:
            tri = new Triangle(va, isect1, isect2);
            if (!Clipper.Tri.Contains(tri))
            {
                clipped.Add(tri);
            }
            tri = new Triangle(vc, isect1, isect2);
            if (!Clipper.Tri.Contains(tri))
            {
                clipped.Add(tri);
            }
            return ClipResult.CLIPPED;

            case ISECTC:
            tri = new Triangle(va, isect1, isect2);
            if (!Clipper.Tri.Contains(tri))
            {
                clipped.Add(tri);
            }
            tri = new Triangle(vb, isect1, isect2);
            if (!Clipper.Tri.Contains(tri))
            {
                clipped.Add(tri);
            }
            return ClipResult.CLIPPED;

            case ISECTA | ISECTB:
            return (Clipper.Tri.Contains(vc) > 0) ? 
                    ClipResult.INSIDE : ClipResult.COINCIDENT;

            case ISECTA | ISECTC:
            return (Clipper.Tri.Contains(vb) > 0) ? 
                    ClipResult.INSIDE : ClipResult.COINCIDENT;

            case ISECTB | ISECTC:
            return (Clipper.Tri.Contains(va) > 0) ?
                    ClipResult.INSIDE : ClipResult.COINCIDENT;

            case 0:
            tri = new Triangle(va, isect1, isect2);
            if (!Clipper.Tri.Contains(tri))
            {
                clipped.Add(tri);
            }
            tri = new Triangle(vb, isect1, isect2);
            if (!Clipper.Tri.Contains(tri))
            {
                clipped.Add(tri);
            }
            tri = new Triangle(vb, vc, isect2);
            if (!Clipper.Tri.Contains(tri))
            {
                clipped.Add(tri);
            }
            return ClipResult.CLIPPED;
        }
        return ClipResult.OUTSIDE;
    }
}

