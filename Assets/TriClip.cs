using System;
using System.Collections.Generic;
using UnityEngine;

public class EdgeClip : Comparer<EdgeClip>
{
    public Triangle Clipper;
    public Triangle Clipped;
    public int ClipperEdgeStart;
    public int ClipperEdgeEnd;
    public int ClippedEdgeStart;
    public int ClippedEdgeEnd;
    public int Status;
    public int HashKey;
    public Vector3 IntersectionPoint;
    public Vector3 IsectBaryCoords;
    public const int INTERSECTING = 1;
    public const int OUTSIDE = 2;
    public const int COINCIDENT = 4;

    public EdgeClip(Triangle clipper, Triangle clipped, int clipperedge, int clippededge) 
    {
        Clipper = clipper;
        Clipped = clipped;
        ClipperEdgeStart = clipperedge;
        ClippedEdgeStart = clippededge;
        ClipperEdgeEnd = (clipperedge + 1) % 3;
        ClippedEdgeEnd = (clippededge + 1) % 3;
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
    *          TOUCHING if line segments touch but dont penetrate
    */
    private int FindIntersection()
    {
        Vector3 p1 = Clipped.GetVertex(ClippedEdgeStart);
        Vector3 p2 = Clipped.GetVertex(ClippedEdgeEnd);
        Vector3 p3 = Clipper.GetVertex(ClipperEdgeStart);
        Vector3 p4 = Clipper.GetVertex(ClipperEdgeEnd);
        Vector3 A = p2 - p1;
        Vector3 B = p3 - p4;
        Vector3 C = p1 - p3;
        float f = A.y * B.x - A.x * B.y;
        float e = A.x * C.y - A.y * C.x;
        float d = B.y * C.x - B.x * C.y;
        bool denomPositive = (f > 0);

        // check to see if they are coincident
        if (f == 0)
        {
            if (d == 0)
            {
                return COINCIDENT;
            }
            return 0;
        }
        if (d == f)
        {
            return 0;
        }
        float t = d / f;
        IntersectionPoint = Clipped.GetVertex(ClippedEdgeStart) + (t * A);
        Clipped.Bary(IntersectionPoint, ref IsectBaryCoords);

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

        if ((max1.x < min2.x) || (max2.x < min1.x))
        {
            return INTERSECTING | OUTSIDE;
        }
        return INTERSECTING;
    }

    public override int Compare(EdgeClip ec1, EdgeClip ec2)
    {
        return ec1.HashKey - ec2.HashKey;
    }
}

public enum ClipResult
{
    OUTSIDE = -1,
    INSIDE = 0,
    CLIPPED = 2,
    COINCIDENT = 1
}
public class TriClip
{
    public SortedList<int, EdgeClip> EdgesClipped = new SortedList<int, EdgeClip>();
    public Triangle Clipper;
    public Triangle Clipped;
    public int NumIntersected;
    public int NumCoincident;
    public int[] Inside = new int[3];
    public const float EPSILON = 2e-7f;

    public TriClip(Triangle clipper, Triangle clipped)
    {
        Clipper = clipper;
        Clipped = clipped;
        NumIntersected = 0;
        NumCoincident = 0;
        Inside[0] = Clipper.Contains(Clipped.GetVertex(0));
        Inside[1] = Clipper.Contains(Clipped.GetVertex(1));
        Inside[2] = Clipper.Contains(Clipped.GetVertex(2));
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
            return ClipResult.INSIDE;           // Clipped inside Clilpper
        }
        /*
         * Clip against the Clipper triangle,
         * check if any Clipper edge intersects
         * two Clipped edges
         */
        for (int i = 0; i < 3; ++i)
        {
            EdgeClip[] edges = ClipAgainstEdge(i);
            if (edges != null)
            {
                ClipResult r = ClipTriangles(edges[0], edges[1], clipped);
                if (r != ClipResult.OUTSIDE)
                {
                    return r;
                }
            }
        }
        if (NumIntersected > 1)
        {
            /*
             * Look for two Clipper edges that intersect
             * the same Clipped edge
             */
            IEnumerator<KeyValuePair<int, EdgeClip>> iter = EdgesClipped.GetEnumerator();
            while (iter.MoveNext())
            {
                EdgeClip ec1 = iter.Current.Value;
                EdgeClip ec2;
                if (ec1.Status == EdgeClip.INTERSECTING)
                {
                    while (iter.MoveNext())
                    {
                        ec2 = iter.Current.Value;
                        if ((ec2.Status & EdgeClip.INTERSECTING) != 0)
                        {
                            if ((ec1.ClipperEdgeStart == ec2.ClipperEdgeStart) &&
                                (ec1.ClipperEdgeEnd == ec2.ClipperEdgeEnd))
                            {
                                ClipResult r = ClipTriangles(ec1, ec2, clipped);
                                switch (r)
                                {
                                    case ClipResult.CLIPPED:
                                    case ClipResult.INSIDE:
                                    return r;

                                    case ClipResult.COINCIDENT:
                                    ec1.Status = EdgeClip.COINCIDENT;
                                    ec2.Status = EdgeClip.COINCIDENT;
                                    break;
                                }
                            }
                        }
                        else
                        {
                            ec1 = ec2;
                        }
                    }
                }
            }
        }
        return ClipResult.OUTSIDE;
    }

    public EdgeClip[] ClipAgainstEdge(int clipperedge)
    {
        int intersectedInside = 0;
        EdgeClip ec1 = null;
        for (int i = 0; i < 3; ++i)
        {
            EdgeClip clipstatus = new EdgeClip(Clipper, Clipped, clipperedge, i);
            int s = clipstatus.Clip();
            if (s == 0)                         // skip if parallel
            {
                continue;
            }
            if (UpdateEdgeStatus(clipstatus))
            {
                continue;
            }
 /*           if (s == EdgeClip.COINCIDENT)
            {
                ++NumCoincident;
                EdgesClipped.Add(clipstatus.HashKey, clipstatus);
            }
            */
            else if (s == EdgeClip.INTERSECTING)     // clipper edge intersects 2 clipped edges
            {
                ++NumIntersected;
                EdgesClipped.Add(clipstatus.HashKey, clipstatus);
                if (++intersectedInside == 2)
                {
                    return new EdgeClip[] { ec1, clipstatus };
                }
                ec1 = clipstatus;
            }
            else if ((Clipped.Contains(Clipper.GetVertex(clipperedge)) > 0) ||
                     (Clipped.Contains(Clipper.GetVertex(clipstatus.ClipperEdgeEnd)) > 0))
            {
                ++NumIntersected;
                EdgesClipped.Add(clipstatus.HashKey, clipstatus);
            }
        }
        return null;
    }

    public bool UpdateEdgeStatus(EdgeClip ec1)
    {
        foreach (KeyValuePair<int, EdgeClip> p in EdgesClipped)
        {
            EdgeClip ec2 = p.Value;

            if ((ec1.ClippedEdgeStart == ec2.ClippedEdgeStart) &&
                (ec1.ClippedEdgeEnd == ec2.ClippedEdgeEnd) &&
                (ec1.ClipperEdgeStart == ec2.ClipperEdgeStart) &&
                (ec1.ClipperEdgeEnd == ec2.ClipperEdgeEnd))
            {
                if (ec1.Status < ec2.Status)
                {
                    ec2.Status = ec1.Status;
                    return true;
                }
            }
        }
        return false;
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
            va = Clipped.GetVertex(edge1.ClippedEdgeEnd);
            vb = Clipped.GetVertex(edge1.ClippedEdgeStart);
            vc = Clipped.GetVertex(edge2.ClippedEdgeEnd);
        }
        else // edge1.ClippedEdgeStart == edge2.ClippedEdgeEnd
        {
            va = Clipped.GetVertex(edge1.ClippedEdgeStart);
            vb = Clipped.GetVertex(edge1.ClippedEdgeEnd);
            vc = Clipped.GetVertex(edge2.ClippedEdgeStart);
        }
        temp1 = isect1 - isect2;
        if (temp1.sqrMagnitude <= EPSILON)
        {
            return ClipResult.OUTSIDE;
        }
        temp1 = va - edge1.IntersectionPoint;
        temp2 = va - edge2.IntersectionPoint;
        if ((temp1.sqrMagnitude > EPSILON) &&
            (temp2.sqrMagnitude > EPSILON))
        {
            mask &= ~ISECTA;
        }
        temp1 = vb - edge1.IntersectionPoint;
        temp2 = vb - edge2.IntersectionPoint;
        if ((temp1.sqrMagnitude > EPSILON) &&
            (temp2.sqrMagnitude > EPSILON))
        {
            mask &= ~ISECTB;
        }
        temp1 = vc - edge1.IntersectionPoint;
        temp2 = vc - edge2.IntersectionPoint;
        if ((temp1.sqrMagnitude > EPSILON) &&
            (temp2.sqrMagnitude > EPSILON))
        {
            mask &= ~ISECTC;
        }
        switch (mask)
        {
            case ISECTA:
            tri = new Triangle(vb, isect1, isect2, 0);
            clipped.Add(tri);
            tri = new Triangle(vc, isect1, isect2, 0);
            clipped.Add(tri);
            return ClipResult.CLIPPED;

            case ISECTB:
            tri = new Triangle(va, isect1, isect2, 0);
            clipped.Add(tri);
            tri = new Triangle(vc, isect1, isect2, 0);
            clipped.Add(tri);
            return ClipResult.CLIPPED;

            case ISECTC:
            tri = new Triangle(va, isect1, isect2, 0);
            clipped.Add(tri);
            tri = new Triangle(vb, isect1, isect2, 0);
            clipped.Add(tri);
            return ClipResult.CLIPPED;

            case ISECTA | ISECTB:
            return (Clipper.Contains(vc) > 0) ? 
                    ClipResult.INSIDE : ClipResult.COINCIDENT;

            case ISECTA | ISECTC:

            return (Clipper.Contains(vb) > 0) ? 
                    ClipResult.INSIDE : ClipResult.COINCIDENT;


            case ISECTB | ISECTC:
            return (Clipper.Contains(va) > 0) ?
                    ClipResult.INSIDE : ClipResult.COINCIDENT;

            case 0:
            tri = new Triangle(va, isect1, isect2, 0);
            clipped.Add(tri);
            tri = new Triangle(vb, isect1, isect2, 0);
            clipped.Add(tri);
            tri = new Triangle(vb, vc, isect2, 0);
            clipped.Add(tri);
            return ClipResult.CLIPPED;
        }
        return ClipResult.OUTSIDE;
    }
}

