using System;
using System.Collections.Generic;
using UnityEngine;

public class EdgeClip
{
    public Triangle Clipper;
    public Triangle Clipped;
    public int ClipperEdgeStart;
    public int ClipperEdgeEnd;
    public int ClippedEdgeStart;
    public int ClippedEdgeEnd;
    public int Status;
    public Vector3 IntersectionPoint;
    public Vector3 IsectBaryCoords;
    public const int INTERSECTING = 1;
    public const int TOUCHING = 2;
    public const int COINCIDENT = 4;
    public const int OUTSIDE = 4;

    public EdgeClip(Triangle clipper, Triangle clipped, int clipperedge, int clippededge)
    {
        Clipper = clipper;
        Clipped = clipped;
        ClipperEdgeStart = clipperedge;
        ClippedEdgeStart = clippededge;
        ClipperEdgeEnd = (clipperedge + 1) % 3;
        ClippedEdgeEnd = (clippededge + 1) % 3;
    }

    public int Clip()
    {
        Status = FindIntersection();
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
}

public enum ClipResult
{
    OUTSIDE = -1,
    INSIDE = 0,
    CLIPPED = 2,
    INTERSECTED = 1
}
public class TriClip
{
    public List<EdgeClip> EdgesClipped = new List<EdgeClip>();
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
        EdgeClip edge1 = null;

        int j = 0;
        for (int i = 0; i < 3; ++i)
        {
            int numintersected = ClipAgainstEdge(i);
            if (numintersected >= 2)
            {
                int r = ClipTriangles(EdgesClipped[EdgesClipped.Count - 2],
                                  EdgesClipped[EdgesClipped.Count - 1],
                                  clipped);
                if (r > 0)
                {
                    return ClipResult.CLIPPED;
                }
                if (r == 0)
                {
                    return ClipResult.INSIDE;
                }
            }

        }
        if (NumIntersected > 1)
        {
            /*
             * Look for two Clipper edges that intersect
             * the same Clipped edge
             */
            foreach (EdgeClip ec1 in EdgesClipped)
            {
                if (ec1.Status == EdgeClip.INTERSECTING)
                {
                    foreach (EdgeClip ec2 in EdgesClipped)
                    {
                        if ((ec2 != ec1) &&
                            ((ec2.Status & EdgeClip.INTERSECTING) != 0) &&
                            (ec1.ClipperEdgeStart == ec2.ClipperEdgeStart))
                        {
                            int r = ClipTriangles(ec1, ec2, clipped);
                            if (r > 0)
                            {
                                return ClipResult.CLIPPED;
                            }
                            if (r == 0)
                            {
                                return ClipResult.INSIDE;
                            }
                        }
                    }
                    break;
                }
            }
        }
        if (NumCoincident > 0)
        {
            foreach (EdgeClip ec in EdgesClipped)
            {
                if (ec.Status == EdgeClip.COINCIDENT)
                {
                    if (Clipper.Contains(Clipped.GetVertex((ec.ClippedEdgeEnd + 1) % 3)) > 0)
                    {
                        return ClipResult.INSIDE;
                    }
                }
            }
        }
        return ClipResult.OUTSIDE;
    }

    public int ClipAgainstEdge(int clipperedge)
    {
        int intersectedInside = 0;
        for (int i = 0; i < 3; ++i)
        {
            EdgeClip clipstatus = new EdgeClip(Clipper, Clipped, clipperedge, i);
            int s = clipstatus.Clip();
            if (s == 0)                         // skip if parallel
            {
                continue;
            }
            if (s == EdgeClip.COINCIDENT)
            {
                ++NumCoincident;
                EdgesClipped.Add(clipstatus);
            }
            else if (s == EdgeClip.INTERSECTING)     // clipper edge intersects 2 clipped edges
            {
                ++NumIntersected;
                EdgesClipped.Add(clipstatus);
                if (++intersectedInside == 2)
                {
                    return 2;
                }
            }
            else if ((Clipped.Contains(Clipper.GetVertex(clipperedge)) > 0) ||
                     (Clipped.Contains(Clipper.GetVertex(clipstatus.ClipperEdgeEnd)) > 0))
            {
                ++NumIntersected;
                EdgesClipped.Add(clipstatus);
            }
        }
        return intersectedInside;
    }

    /*
     * returns -1 if intersections points are the same
     * returns 0 if clipped triangle inside clipper triangle
     * returns 1 if clipped triangles were generated
     */
    int ClipTriangles(EdgeClip edge1, EdgeClip edge2, List<Triangle> clipped)
    {
        Vector3 temp1;
        Vector3 temp2;
        Triangle tri;
        const int ISECTA = 1;
        const int ISECTB = 2;
        const int ISECTC = 4;
        int mask = ISECTA | ISECTB | ISECTC;
        Vector3 va = Clipped.GetVertex(edge1.ClippedEdgeEnd);
        Vector3 vb = Clipped.GetVertex(edge1.ClippedEdgeStart);
        Vector3 vc = (edge1.ClippedEdgeEnd == edge2.ClippedEdgeStart) ?
                     Clipped.GetVertex(edge2.ClippedEdgeEnd) :
                     Clipped.GetVertex(edge2.ClippedEdgeStart);
        Vector3 isect1 = edge1.IntersectionPoint;
        Vector3 isect2 = edge2.IntersectionPoint;

        temp1 = isect1 - isect2;
        if (temp1.sqrMagnitude <= EPSILON)
        {
            return -1;
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
            return 1;

            case ISECTB:
            tri = new Triangle(va, isect1, isect2, 0);
            clipped.Add(tri);
            tri = new Triangle(vc, isect1, isect2, 0);
            clipped.Add(tri);
            return 1;

            case ISECTC:
            tri = new Triangle(va, isect1, isect2, 0);
            clipped.Add(tri);
            tri = new Triangle(vb, isect1, isect2, 0);
            clipped.Add(tri);
            return 1;

            case ISECTA | ISECTB:
            if (Clipper.Contains(vc) > 0)
            {
                return 0;
            }
            break;

            case ISECTA | ISECTC:
            if (Clipper.Contains(vb) > 0)
            {
                return 0;
            }
            break;

            case ISECTB | ISECTC:
            if (Clipper.Contains(va) > 0)
            {
                return 0;
            }
            break;

            case 0:
            tri = new Triangle(va, isect1, isect2, 0);
            clipped.Add(tri);
            tri = new Triangle(vb, isect1, isect2, 0);
            clipped.Add(tri);
            tri = new Triangle(vb, vc, isect2, 0);
            clipped.Add(tri);
            return 1;
        }
        return -1;
    }
}

