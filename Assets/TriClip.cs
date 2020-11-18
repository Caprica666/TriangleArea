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
    public const int VERTEX = 2;
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
            return INTERSECTING | VERTEX;
        }
        if ((d == f) || (e == f))
        {
            return INTERSECTING | VERTEX;
        }
        if ((d == 0) && (e == 0))
        {
            return INTERSECTING | VERTEX;
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
    public int NumIntersected;
    public int[] Inside = new int[3];
    public const float EPSILON = 2e-7f;

    public TriClip(Edge clipper, Edge clipped)
    {
        Clipper = clipper;
        Clipped = clipped;
        NumIntersected = 0;
        Inside[0] = Clipper.Tri.Contains(Clipped.Tri.GetVertex(0));
        Inside[1] = Clipper.Tri.Contains(Clipped.Tri.GetVertex(1));
        Inside[2] = Clipper.Tri.Contains(Clipped.Tri.GetVertex(2));
    }

    public ClipResult Clip(List<Triangle> clipped)
    {
        const int V2_IN = 0x20;
        const int V2_EDGE = 0x10;
        const int V1_IN = 8;
        const int V1_EDGE = 4;
        const int V0_IN = 2;
        const int V0_EDGE = 1;
        int mask = ((Inside[2] > 0) ? V2_IN : ((Inside[2] == 0) ? V2_EDGE : 0));
        mask |= ((Inside[1] > 0) ? V1_IN : ((Inside[1] == 0) ? V1_EDGE : 0));
        mask |= ((Inside[0] > 0) ? V0_IN : ((Inside[0] == 0) ? V0_EDGE : 0));
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
            return ClipResult.INSIDE;           // Clipped inside Clipper
        }
        /*
         * Clip against the Clipper triangle,
         * check if the Clipper edge intersects
         * two Clipped edges
         */
        EdgeClip[] edges = ClipAgainstEdge(Clipper);
        Edge adjacent = Clipper.Tri.Edges[(Clipper.EdgeIndex + 2) % 3];

        /*
         * Check which edges of the Clipped triangle intersect the Clipper edge.
         * If no intersection, clip against the adjacent Clipper edge.
         */
        if (edges == null)
        {
            edges = ClipAgainstEdge(adjacent);
            if (edges == null)
            {
                return ClipResult.OUTSIDE;
            }
        }
        /*
         * If only one edge of the Clipped triangle intersects,
         * clip against the adjacent Clipper edge.
         * If the adjacent edge clips two edges, return those results.
         * If both Clipper edges each intersect, combine these results.
         */
        else if (edges[1] != null)
        {
            if ((edges[1].Status & EdgeClip.INTERSECTING) == 0)
            {
                EdgeClip[] edges2 = ClipAgainstEdge(adjacent);
                if ((edges2 != null) &&
                    ((edges2[0].Status & EdgeClip.INTERSECTING) != 0))
                {
                    if ((edges2[1] != null) &&
                        ((edges2[1].Status & EdgeClip.INTERSECTING) != 0))
                    {
                        edges = edges2;
                    }
                    else
                    {
                        edges[1] = edges2[0];
                    }
                }
            }
        }
        if (edges[1] == null)
        {
            return ClipResult.OUTSIDE;
        }
        ClipResult r = ClipTriangles(edges[0], edges[1], clipped);
        switch (r)
        {
            case ClipResult.CLIPPED:
            case ClipResult.INSIDE:
            return r;
        }
        return ClipResult.OUTSIDE;
    }

    public EdgeClip[] ClipAgainstEdge(Edge clipper)
    {
        int intersected = 0;
        int[] clipstatus = new int[3];
        EdgeClip[] alledges = new EdgeClip[3];
        EdgeClip[] clipedges = new EdgeClip[2];

        for (int i = 0; i < 3; ++i)
        {
            alledges[i] = new EdgeClip(clipper, Clipped.Tri.Edges[i]);
            clipstatus[i] = alledges[i].Clip();

            if ((clipstatus[i] & EdgeClip.INTERSECTING) != 0)
            {
                if (++intersected > 1)
                {
                    clipedges[1] = alledges[i];
                    return clipedges;
                }
                clipedges[0] = alledges[i];
            }
        }
        if (intersected > 0)
        {
            return clipedges;
        }
/*        for (int i = 0; i < 3; ++i)
        {
            switch (clipstatus[i])
            {
                case EdgeClip.INTERSECTING | EdgeClip.VERTEX:
                case EdgeClip.INTERSECTING | EdgeClip.OUTSIDE:
                clipedges[1] = alledges[i];
                return clipedges;

                case EdgeClip.COINCIDENT:
                if ((((clipedges[0].Clipped.EdgeIndex + 1) % 3) == alledges[i].Clipped.EdgeIndex) ||
                    (((alledges[i].Clipped.EdgeIndex - 1) % 3) == clipedges[0].Clipped.EdgeIndex))
                {
                    clipedges[1] = alledges[i];
                    return clipedges;
                }
                break;
            }
        }
        */
        return null;
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

