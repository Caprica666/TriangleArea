﻿using System;
using System.CodeDom;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading;
using UnityEngine;

public class EdgeGroup
{
    protected EdgeGroup mLeftChild = null;
    protected EdgeGroup mRightChild = null;
    protected Triangle mTriangle = null;
    protected EdgeGroup mRoot = null;
    protected TriangleMesh mTriMesh;
    protected float mMaxX = 0;
    protected float mMinX = 0;

    private static readonly float EPSILON = 1e-5f;
    public enum ClipResult
    {
        OUTSIDE = -1,
        INSIDE = 0,
        CLIPPED = 1,
        INTERSECTED = 2
    }

    public EdgeGroup(Triangle tri, TriangleMesh mesh)
    {
        mTriMesh = mesh;
        mTriangle = tri;
        mMinX = tri.GetVertex(0).x;
        mMaxX = (tri.GetVertex(1).x >= tri.GetVertex(2).x) ?
                  tri.GetVertex(1).x : tri.GetVertex(2).x;
        mTriMesh.AddTriangle(tri);
        mRoot = this;
    }

    public EdgeGroup(Triangle tri, EdgeGroup root)
    {
        mTriMesh = root.mTriMesh;
        mRoot = root;
        mTriangle = tri;
        mMinX = tri.GetVertex(0).x;
        mMaxX = (tri.GetVertex(1).x >= tri.GetVertex(2).x) ?
                  tri.GetVertex(1).x : tri.GetVertex(2).x;
    }

    public void Clear()
    {
        mLeftChild = null;
        mRightChild = null;
        mTriangle = null;
    }

    public void Display(bool makemesh = false)
    {
        if (mTriMesh != null)
        {
             mTriMesh.Display();
        }
    }
    public void RemoveMe()
    {
        if ((mTriMesh != null) && (mTriangle != null))
        {
            mTriMesh.RemoveTriangle(mTriangle);
            mTriangle = null;
         }
    }

    public class AddResult : IEnumerator
    {
        ClipResult mResult = ClipResult.OUTSIDE;

        public AddResult(ClipResult clipresult)
        {
            mResult = clipresult;
        }
        public object Current
        {
            get { return mResult; }
        }

        public bool MoveNext() { return false; }

        public void Reset() { }
    }

    public IEnumerator Add(Triangle tri)
    {
        Display();
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        List<Triangle> clipped = new List<Triangle>();
        ClipResult r = AddInternal(tri, clipped);
        foreach (Triangle t in clipped)
        {
            yield return mRoot.Add(t);
        }
    }

    public ClipResult AddInternal(Triangle tri, List<Triangle> clipped)
    {
        if (tri.IsDegenerate())
        {
            return ClipResult.INSIDE;
        }
        if (mTriangle != null)
        {
            if (tri.Contains(mTriangle))        // mTriangle inside tri
            {
                mTriMesh.RemoveTriangle(mTriangle);
                mTriangle = null;
            }
            else
            {
                ClipResult r = Clip(tri, mTriangle, clipped);
                switch (r)
                {
                    case ClipResult.CLIPPED:    // tri was clipped
                    case ClipResult.INSIDE:     // tri inside another triangle
                    return r;
                }
            }
        }
        float xmin = tri.GetVertex(0).x;
        float xmax = (tri.GetVertex(1).x <= tri.GetVertex(2).x) ?
                      tri.GetVertex(1).x : tri.GetVertex(2).x;
        ClipResult result;
        if (mLeftChild != null)             // try to add to left tree
        {
            result = mLeftChild.AddInternal(tri, clipped);
            if (result != ClipResult.OUTSIDE)
            {
                return result;
            }
            if (xmin > mMinX)
            {
                return ClipResult.OUTSIDE;
            }
        }
        if (mRightChild != null)            // try to add on the right side
        {
            if (xmin >= mRightChild.mMinX)  // in right subtree?
            {
                result = mRightChild.AddInternal(tri, clipped);
                if (result != ClipResult.OUTSIDE)
                {
                    return result;
                }
            }
            if (xmin >= mMinX)
            {
                if (xmin < mRightChild.mMinX)
                {
                    EdgeGroup g = new EdgeGroup(tri, mRoot);
                    g.mRightChild = mRightChild;
                    mRightChild = g;            // add before right subtree
                    mTriMesh.AddTriangle(tri);
                    return ClipResult.CLIPPED;
                }
                return ClipResult.OUTSIDE;
            }
        }
        else if (xmin >= mMinX)             // add as the right subtree
        {
            mRightChild = new EdgeGroup(tri, mRoot);
            mTriMesh.AddTriangle(tri);
            return ClipResult.CLIPPED;
        }
        else if (mLeftChild != null)
        {
            if (xmin > mLeftChild.mMinX)    // add before left subtree
            {
                EdgeGroup g = new EdgeGroup(tri, mRoot);
                g.mLeftChild = mLeftChild;
                mLeftChild = g;
                mTriMesh.AddTriangle(tri);
                return ClipResult.CLIPPED;
            }
            return ClipResult.OUTSIDE;
        }
        else                               // add as the left subtree
        {
            mLeftChild = new EdgeGroup(tri, mRoot);
            mTriMesh.AddTriangle(tri);
            return ClipResult.CLIPPED;
        }
        return ClipResult.OUTSIDE;
    }

    /* 
     * Clips the first triangle against the second.
     * @returns OUTSIDE = triangle 1 outside triangle 2
     *          INSIDE = triangle 1 inside triangle 2
     *          CLIPPED = triangle 1 was clipped by triangle 2
     *          INTERSECTED = triangle 2 may be clipped by triangle 1
     */
    public ClipResult Clip(Triangle tri1, Triangle tri2, List<Triangle> clipped)
    {
        if (tri1 == tri2)
        {
            return ClipResult.OUTSIDE;
        }
        int[] inside = new int[3];
        int[] edgeshit = new int[] { -1, -1, -1 };
        const int V2_IN = 0x20;
        const int V2_EDGE = 0x10;
        const int V1_IN = 8;
        const int V1_EDGE = 4;
        const int V0_IN = 2;
        const int V0_EDGE = 1;
        const int V0_ISECT = 1;
        const int V1_ISECT = 2;
        const int V2_ISECT = 4;
        int c1 = -1;
        int c2 = -1;
        int c3 = -1;
        int intersections = 0;
        int coincident = 0;
        Vector3[] isect = new Vector3[3];
        int mask;

        inside[0] = tri2.Contains(tri1.GetVertex(0));
        inside[1] = tri2.Contains(tri1.GetVertex(1));
        inside[2] = tri2.Contains(tri1.GetVertex(2));
        mask = ((inside[2] > 0) ? V2_IN : (inside[2] == 0) ? V2_EDGE : 0);
        mask |= ((inside[1] > 0) ? V1_IN : (inside[1] == 0) ? V1_EDGE : 0);
        mask |= ((inside[0] > 0) ? V0_IN : (inside[0] == 0) ? V0_EDGE : 0);
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
            return ClipResult.INSIDE;           // tri1 inside tri2
        }
        c1 = Clip(tri1, tri2.GetEdge(0).EdgeLine, ref isect[0], ref edgeshit[0], clipped);
        switch (c1)
        {
            case (int) ClipResult.INSIDE:
            ++coincident;
            break;

            case 2:           // 2 intersections, was clipped
            return ClipResult.CLIPPED;

            case 1:            // only one intersection
            intersections |= V0_ISECT;
            break;
        }
        c2 = Clip(tri1, tri2.GetEdge(1).EdgeLine, ref isect[1], ref edgeshit[1], clipped);
        switch (c2)
        {
            case (int) ClipResult.INSIDE:
            ++coincident;
            break;

            case 2:           // 2 intersections, was clipped
            return ClipResult.CLIPPED;

            case 1:             // 2 intersections, different edges
            intersections |= V1_ISECT;
            break;
        }
        c3 = Clip(tri1, tri2.GetEdge(2).EdgeLine, ref isect[2], ref edgeshit[2], clipped);
        switch (c3)
        {
            case (int) ClipResult.INSIDE:
            ++coincident;
            break;

            case 2:           // 2 intersections, was clipped
            return ClipResult.CLIPPED;

            case 1:             // 2 intersections, different edges
            intersections |= V2_ISECT;
            break;
        }
        switch (intersections)
        {
            case V0_ISECT | V2_ISECT:
            if (edgeshit[0] == edgeshit[2]) // only edge2 intersected
            {
                LineSegment line = tri2.GetEdge(2).EdgeLine;
                Vector3 v1 = new Vector3(-10000, line.EvaluateAtX(-10000), 0);
                Vector3 v2 = new Vector3(10000, line.EvaluateAtX(10000), 0);
                LineSegment edgeExtended = new LineSegment(v1, v2);
                if (Clip(tri1, edgeExtended, ref isect[2], ref edgeshit[2], clipped) == 2)
                {
                    return ClipResult.CLIPPED;
                }
            }
            else if (AddTriangles(tri1.GetVertex(0), tri1.GetVertex(1), tri1.GetVertex(2), isect[0], isect[2], clipped))
            {
                return ClipResult.CLIPPED;
            }
            return ClipResult.OUTSIDE;

            case V1_ISECT | V2_ISECT:       // only edge 1 intersected
            if (edgeshit[1] == edgeshit[2])
            {
                LineSegment line = tri2.GetEdge(1).EdgeLine;
                Vector3 v1 = new Vector3(-10000, line.EvaluateAtX(-10000), 0);
                Vector3 v2 = new Vector3(10000, line.EvaluateAtX(10000), 0);
                LineSegment edgeExtended = new LineSegment(v1, v2);
                if (Clip(tri1, edgeExtended, ref isect[1], ref edgeshit[1], clipped) == 2)
                {
                    return ClipResult.CLIPPED;
                }
            }
            else if (AddTriangles(tri1.GetVertex(2), tri1.GetVertex(1), tri1.GetVertex(0), isect[1], isect[2], clipped))
            {
                return ClipResult.CLIPPED;
            }
            return ClipResult.OUTSIDE;

            case V0_ISECT | V1_ISECT:       // only edge 0 intersected
            if (edgeshit[0] == edgeshit[1])
            {
                LineSegment line = tri2.GetEdge(0).EdgeLine;
                Vector3 v1 = new Vector3(-10000, line.EvaluateAtX(-10000), 0);
                Vector3 v2 = new Vector3(10000, line.EvaluateAtX(10000), 0);
                LineSegment edgeExtended = new LineSegment(v1, v2);
                if (Clip(tri1, edgeExtended, ref isect[0], ref edgeshit[0], clipped) == 2)
                {
                    return ClipResult.CLIPPED;
                }
            }
            else if (AddTriangles(tri1.GetVertex(0), tri1.GetVertex(1), tri1.GetVertex(2), isect[0], isect[1], clipped))
            {
                return ClipResult.CLIPPED;
            }
            return ClipResult.OUTSIDE;
        }
 /*       if ((mask == (V0_EDGE | V1_EDGE | V2_EDGE)) && (coincident >= 2))
        {
            return ClipResult.INSIDE;
        }
        */
        return ClipResult.OUTSIDE;
    }

    /*
     * Clips a triangle against a line.
     * @returns 2 = 2 intersections on this edge
     *          1 = only one edge intersection
     *          INSIDE = triangle edge coincident with line
     *          OUTSDIDE = triangle was not clipped by this edge
     *          COINCIDENT = edge is coincident
     *          1 = only one edge intersection
     */
    public int Clip(Triangle tri1, LineSegment edge2, ref Vector3 isect, ref int edgeindex, List<Triangle> clipped)
    {
        Vector3 isect0 = new Vector3();
        Vector3 isect1 = new Vector3();
        Vector3 isect2 = new Vector3();
        int i0 = tri1.Intersects(0, edge2, ref isect0);
        int i1 = tri1.Intersects(1, edge2, ref isect1);
        int i2 = tri1.Intersects(2, edge2, ref isect2);
        int mask;
        const int HITS_EDGE2 = 0x20;
        const int TOUCHES_EDGE2 = 0x10;
        const int HITS_EDGE1 = 8;
        const int TOUCHES_EDGE1 = 4;
        const int HITS_EDGE0 = 2;
        const int TOUCHES_EDGE0 = 1;

        mask = ((i2 > 0) ? HITS_EDGE2 : 0) | ((i2 == 0) ? TOUCHES_EDGE2 : 0);
        mask |= ((i1 > 0) ? HITS_EDGE1 : 0) | ((i1 == 0) ? TOUCHES_EDGE1 : 0);
        mask |= ((i0 > 0) ? HITS_EDGE0 : 0) | ((i0 == 0) ? TOUCHES_EDGE0 : 0);

        switch (mask)
        {
            case HITS_EDGE0 | HITS_EDGE1:   // first edge intersects input edge
            case HITS_EDGE0 | TOUCHES_EDGE1:
            case TOUCHES_EDGE0 | HITS_EDGE1:
            isect = isect0;
            edgeindex = 0;
            if (AddTriangles(tri1.GetVertex(1), tri1.GetVertex(0), tri1.GetVertex(2), isect0, isect1, clipped))
            {
                return 2;
            }
            return (int) ClipResult.OUTSIDE;

            case HITS_EDGE0 | HITS_EDGE2:   // third edge intersects input edge
            case HITS_EDGE0 | TOUCHES_EDGE2:
            case TOUCHES_EDGE0 | HITS_EDGE2:
            isect = isect2;
            edgeindex = 2;
            if (AddTriangles(tri1.GetVertex(0), tri1.GetVertex(1), tri1.GetVertex(2), isect0, isect2, clipped))
            {
                return 2;
            }
            return (int) ClipResult.OUTSIDE;

            case HITS_EDGE1 | HITS_EDGE2:  // second edge intersects input edge
            case HITS_EDGE1 | TOUCHES_EDGE2:
            case TOUCHES_EDGE1 | HITS_EDGE2:
            isect = isect1;
            edgeindex = 1;
            if (AddTriangles(tri1.GetVertex(2), tri1.GetVertex(1), tri1.GetVertex(0), isect1, isect2, clipped))
            {
                return 2;
            }
            return (int) ClipResult.OUTSIDE;

            case HITS_EDGE1:
            isect = isect1;
            edgeindex = 1;
            return 1;

            case HITS_EDGE2:
            isect = isect2;
            edgeindex = 2;
            return 1;

            case HITS_EDGE0:
            isect = isect0;
            edgeindex = 0;
            return 1;
/*
            case HITS_EDGE0 | TOUCHES_EDGE1 | TOUCHES_EDGE2:
            if (i1 == (int) LineSegment.IntersectResult.TOUCHING)
            {
                if (AddTriangles(tri1.GetVertex(1), tri1.GetVertex(0), tri1.GetVertex(2), isect0, isect1, clipped))
                {
                    return 2;
                }
            }
            else if (i2 == (int) LineSegment.IntersectResult.TOUCHING)
            {
                if (AddTriangles(tri1.GetVertex(0), tri1.GetVertex(1), tri1.GetVertex(2), isect0, isect2, clipped))
                {
                    return 2;
                }
            }
            return 1;

            case HITS_EDGE1 | TOUCHES_EDGE0 | TOUCHES_EDGE2:
            if (i0 == (int) LineSegment.IntersectResult.TOUCHING)
            {
                if (AddTriangles(tri1.GetVertex(1), tri1.GetVertex(0), tri1.GetVertex(2), isect0, isect1, clipped))
                {
                    return 2;
                }
            }
            else if (i2 == (int) LineSegment.IntersectResult.TOUCHING)
            {
                if (AddTriangles(tri1.GetVertex(2), tri1.GetVertex(1), tri1.GetVertex(0), isect1, isect2, clipped))
                {
                    return 2;
                }
            }
            return 1;

            default:
            if ((i0 == (int) LineSegment.IntersectResult.COINCIDENT) ||
                (i1 == (int) LineSegment.IntersectResult.COINCIDENT) ||
                (i2 == (int) LineSegment.IntersectResult.COINCIDENT))
            {
                return (int) ClipResult.INSIDE;
            }
            */
        }
        return (int) ClipResult.OUTSIDE;
    }

    bool AddTriangles(Vector3 va, Vector3 vb, Vector3 vc, Vector3 isect1, Vector3 isect2, List<Triangle> clipped)
    {
        int count = (mTriMesh != null) ? mTriMesh.VertexCount : 0;
        Vector3 temp1;
        Vector3 temp2;

        temp1 = isect1 - isect2;
        if (temp1.sqrMagnitude <= EPSILON)
        {
            return false;
        }
        temp1 = va - isect2;
        temp2 = va - isect1;
        if ((temp1.sqrMagnitude <= EPSILON) ||
            (temp2.sqrMagnitude <= EPSILON))
        {
            return false;
        }
        temp1 = vb - isect2;
        temp2 = vb - isect1;
        if ((temp1.sqrMagnitude > EPSILON) &&
            (temp2.sqrMagnitude > EPSILON))
        {
            Triangle tri = new Triangle(isect1, isect2, va);
            Triangle tri1 = new Triangle(vb, isect1, isect2, count);

            clipped.Add(tri);
            clipped.Add(tri1);
            temp1 = vc - isect2;
            temp2 = vc - isect1;
            if ((temp1.sqrMagnitude > EPSILON) &&
                (temp2.sqrMagnitude > EPSILON))
            {
                Triangle tri2 = new Triangle(vb, isect2, vc, count);
                clipped.Add(tri2);
            }
            return true;
        }
        return false;
    }
}
