using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using UnityEngine;

public class EdgeClip : IComparable<EdgeClip>
{
    public Edge Clipper;
    public Edge Clipped;
    public int ClipperStart;
    public int ClipperEnd;
    public int ClippedStart;
    public int ClippedEnd;
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
        ClipperStart = clipper.EdgeIndex;
        ClippedStart = clipped.EdgeIndex;
        ClipperEnd = (ClipperStart + 1) % 3;
        ClippedEnd = (ClippedStart + 1) % 3;
        HashKey = (ClipperStart << 6) | (ClipperEnd << 4) |
                  (ClippedStart << 2) | ClippedEnd;
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
        Vector3 p1 = Clipped.Tri.GetVertex(ClippedStart);
        Vector3 p2 = Clipped.Tri.GetVertex(ClippedEnd);
        Vector3 p3 = Clipper.Tri.GetVertex(ClipperStart);
        Vector3 p4 = Clipper.Tri.GetVertex(ClipperEnd);
        Vector3 A = p2 - p1;
        Vector3 B = p3 - p4;
        Vector3 C = p1 - p3;
        float f = A.y * B.x - A.x * B.y;
        float e = A.x * C.y - A.y * C.x;
        float d = B.y * C.x - B.x * C.y;
        Vector3 min1 = new Vector3(Math.Min(p1.x, p2.x), Math.Min(p1.y, p2.y));
        Vector3 max1 = new Vector3(Math.Max(p1.x, p2.x), Math.Max(p1.y, p2.y));
        Vector3 min2 = new Vector3(Math.Min(p3.x, p4.x), Math.Min(p3.y, p4.y));
        Vector3 max2 = new Vector3(Math.Max(p3.x, p4.x), Math.Max(p3.y, p4.y));

        // check to see if they are coincident
        if (Math.Abs(f) < EPSILON)
        {
            if (min1.x >= min2.x)
            {
                IntersectionPoint = p1;
                return INTERSECTING | COINCIDENT;
            }
            else if (max1.x <= max2.x)
            {
                IntersectionPoint = p2;
                return INTERSECTING | COINCIDENT;
            }
            return COINCIDENT;
        }
        float t = d / f;
        IntersectionPoint = Clipped.Tri.GetVertex(ClippedStart) + (t * A);
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
    INSIDE = 1,
    BINSIDEA = 1,
    AINSIDEB = 2,
    CLIPPED = 3,
    ACLIPSB = 3,
    BCLIPSA = 4,
    COINCIDENT = 0,
}

public class TriClip
{
    public List<EdgeClip> EdgesClipped = new List<EdgeClip>();
    private int mCoincident;
    public const float EPSILON = 2e-7f;

    public TriClip()
    {
    }

    private bool TriAContainsTriB(Edge edgeA, Edge edgeB)
    {
        const int V2_IN = 0x20;
        const int V2_EDGE = 0x10;
        const int V1_IN = 8;
        const int V1_EDGE = 4;
        const int V0_IN = 2;
        const int V0_EDGE = 1;
        int[] inside = new int[3];
        int mask;

        inside[0] = edgeA.Tri.Contains(edgeB.Tri.GetVertex(0));
        inside[1] = edgeA.Tri.Contains(edgeB.Tri.GetVertex(1));
        inside[2] = edgeA.Tri.Contains(edgeB.Tri.GetVertex(2));
        mask = ((inside[2] > 0) ? V2_IN : ((inside[2] == 0) ? V2_EDGE : 0));
        mask |= ((inside[1] > 0) ? V1_IN : ((inside[1] == 0) ? V1_EDGE : 0));
        mask |= ((inside[0] > 0) ? V0_IN : ((inside[0] == 0) ? V0_EDGE : 0));
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
            return true;                         // EdgeB triangle inside EdgeA triangle
        }
        return false;
    }

    public ClipResult ClipTri(Edge edgeA, Edge edgeB, List<Triangle> clipped)
    {
        if (TriAContainsTriB(edgeA, edgeB))
        {
            return ClipResult.BINSIDEA;
        }
        List<EdgeClip> bclipped = new List<EdgeClip>();
        List<EdgeClip> aclipped = new List<EdgeClip>();
        int n, m;

        mCoincident = -1;
        n = ClipAgainstEdge(edgeA, edgeB, bclipped);
        m = ClipAgainstEdge(edgeB, edgeA, aclipped);
        if ((n == 2) &&
            ((bclipped[0].Status & EdgeClip.OUTSIDE) == 0))
        {
            if ((m != 2) ||
                ((bclipped[1].Status & EdgeClip.OUTSIDE) == 0) ||
                ((aclipped[0].Status & EdgeClip.OUTSIDE) != 0))
            {
                ClipResult r = ClipTriangles(edgeA.Tri, edgeB.Tri, bclipped[0], bclipped[1], clipped);
                switch (r)
                {
                    case ClipResult.CLIPPED: return ClipResult.ACLIPSB;
                    case ClipResult.INSIDE: return ClipResult.BINSIDEA;
                }
            }
        }
        else if ((m == 2) &&
                ((aclipped[0].Status & EdgeClip.OUTSIDE) == 0))
        {
             ClipResult r = ClipTriangles(edgeB.Tri, edgeA.Tri, aclipped[0], aclipped[1], clipped);
             switch (r)
             {
                 case ClipResult.CLIPPED: return ClipResult.BCLIPSA;
                 case ClipResult.INSIDE: return ClipResult.AINSIDEB;
             }
        }
        return ClipResult.OUTSIDE;
    }

    private int ClipAgainstEdge(Edge edgeA, Edge edgeB, List<EdgeClip> clipedges)
    {
        int intersected = 0;
        int[] clipstatus = new int[3];
        EdgeClip[] alledges = new EdgeClip[3];

        for (int i = 0; i < 3; ++i)
        {
            alledges[i] = new EdgeClip(edgeA, edgeB.Tri.Edges[i]);
            clipstatus[i] = alledges[i].Clip();

            if ((clipstatus[i] & EdgeClip.INTERSECTING) != 0)
            {
                if (clipstatus[i] == EdgeClip.INTERSECTING)
                {
                    clipedges.Insert(0, alledges[i]);
                }
                else
                {
                    clipedges.Add(alledges[i]);
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
    ClipResult ClipTriangles(Triangle triA, Triangle triB, EdgeClip edge1, EdgeClip edge2, List<Triangle> clipped)
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
        if (edge1.ClippedEnd == edge2.ClippedStart)
        {
            va = triB.GetVertex(edge1.ClippedEnd);
            vb = triB.GetVertex(edge1.ClippedStart);
            vc = triB.GetVertex(edge2.ClippedEnd);
        }
        else // edge1.EdgeBStart != edge2.EdgeBEnd
        {
            va = triB.GetVertex(edge1.ClippedStart);
            vb = triB.GetVertex(edge1.ClippedEnd);
            vc = triB.GetVertex(edge2.ClippedStart);
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
            if (!triA.Contains(tri))
            {
                clipped.Add(tri);
            }
            tri = new Triangle(vc, isect1, isect2);
            if (!triA.Contains(tri))
            {
                clipped.Add(tri);
            }
            return ClipResult.CLIPPED;

            case ISECTB:
            tri = new Triangle(va, isect1, isect2);
            if (!triA.Contains(tri))
            {
                clipped.Add(tri);
            }
            tri = new Triangle(vc, isect1, isect2);
            if (!triA.Contains(tri))
            {
                clipped.Add(tri);
            }
            return ClipResult.CLIPPED;

            case ISECTC:
            tri = new Triangle(va, isect1, isect2);
            if (!triA.Contains(tri))
            {
                clipped.Add(tri);
            }
            tri = new Triangle(vb, isect1, isect2);
            if (!triA.Contains(tri))
            {
                clipped.Add(tri);
            }
            return ClipResult.CLIPPED;

            case ISECTA | ISECTB:
            return (triA.Contains(vc) > 0) ? 
                    ClipResult.INSIDE : ClipResult.COINCIDENT;

            case ISECTA | ISECTC:
            return (triA.Contains(vb) > 0) ? 
                    ClipResult.INSIDE : ClipResult.COINCIDENT;

            case ISECTB | ISECTC:
            return (triA.Contains(va) > 0) ?
                    ClipResult.INSIDE : ClipResult.COINCIDENT;

            case 0:
            tri = new Triangle(va, isect1, isect2);
            if (!triA.Contains(tri))
            {
                clipped.Add(tri);
            }
            tri = new Triangle(vb, isect1, isect2);
            if (!triA.Contains(tri))
            {
                clipped.Add(tri);
            }
            tri = new Triangle(vb, vc, isect2);
            if (!triA.Contains(tri))
            {
                clipped.Add(tri);
            }
            return ClipResult.CLIPPED;
        }
        return ClipResult.OUTSIDE;
    }
}

