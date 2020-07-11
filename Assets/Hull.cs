
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Hull : MonoBehaviour
{
    private List<UnityEngine.Vector3> mHullVerts;
    private List<Triangle.Edge> mHull;
    private LineRenderer mCurLine = null;
    private Color mColor;

    public List<Triangle.Edge> Edges
    {
        get { return mHull; }
    }

    public List<Vector3> Vertices
    {
        get { return mHullVerts; }
    }

    public void Awake()
	{
		mCurLine = gameObject.AddComponent<LineRenderer>() as LineRenderer;
        mCurLine.useWorldSpace = false;
        mCurLine.widthMultiplier = 0.1f;
        mCurLine.material = new Material(Shader.Find("Unlit/Color"));
        mColor = mCurLine.material.color;
    }

    public IEnumerator MakeHull(List<Triangle.Edge> triverts, Color c)
    {
        List<Triangle.Edge> reversed = new List<Triangle.Edge>(triverts);

        reversed.Reverse();
        mColor = c;
        mCurLine.material.color = c;
        mHullVerts = new List<Vector3>();
        mHull = new List<Triangle.Edge>();
        yield return StartCoroutine(CalcUpperHull(triverts.GetEnumerator()));
        yield return new WaitForEndOfFrame();
        yield return StartCoroutine(CalcLowerHull(reversed.GetEnumerator()));
        mCurLine.positionCount = mHullVerts.Count;
        mCurLine.SetPositions(mHullVerts.ToArray());
        yield return new WaitForEndOfFrame();
    }

    public static float Cross2D(Vector3 v1, Vector3 v2, Vector3 v3)
    {
        Vector3 u1 = v2 - v1;
        Vector3 u2 = v3 - v2;
        return (u1.x * u2.y) - (u1.y * u2.x);
    }

    IEnumerator CalcUpperHull(IEnumerator iter)
    {
        Vector3 v1;
        Vector3 v2;

        iter.Reset();
        iter.MoveNext();
        v1 = ((Triangle.Edge)iter.Current).EdgeLine.Start;
        mHull.Add((Triangle.Edge)iter.Current);
        mHullVerts.Add(v1);
        iter.MoveNext();
        v2 = ((Triangle.Edge)iter.Current).EdgeLine.Start;
        mHull.Add((Triangle.Edge)iter.Current);
        mHullVerts.Add(v2);
        iter.MoveNext();

        while (true)
        {
            Triangle.Edge tv3 = (Triangle.Edge)iter.Current;
            Triangle.Edge tv2 = mHull[mHull.Count - 2];
            Triangle.Edge tv1 = mHull[mHull.Count - 1];
            float cross = Cross2D(tv1.EdgeLine.Start, tv2.EdgeLine.Start, tv3.EdgeLine.Start);
            int last = mHull.Count - 1;

            if (cross > 0)
            {
                mHull.Add(tv3);
                mHullVerts.Add(tv3.EdgeLine.Start);
            }
            else if (last >= 2)
            {
                mHull.RemoveAt(last);
                mHullVerts.RemoveAt(last);
                mCurLine.positionCount = mHullVerts.Count;
                mCurLine.SetPositions(mHullVerts.ToArray());
                yield return new WaitForEndOfFrame();
                continue;
            }
            else
            {
                mHull[last] = tv3;
                mHullVerts[last] = tv3.EdgeLine.Start;
            }
            mCurLine.positionCount = mHullVerts.Count;
            mCurLine.SetPositions(mHullVerts.ToArray());
            if (!iter.MoveNext())
            {
                break;
            }
            yield return new WaitForEndOfFrame();
        }
        yield return new WaitForEndOfFrame();
    }

    IEnumerator CalcLowerHull(IEnumerator iter)
    {
        Vector3 v1;
        Vector3 v2;
        Vector3 v3;
        int start = mHull.Count - 2;

 //       mHull.RemoveAt(start);
 //       mHullVerts.RemoveAt(start);
        iter.Reset();
        iter.MoveNext();
        v1 = ((Triangle.Edge) iter.Current).EdgeLine.Start;
        mHull.Add((Triangle.Edge) iter.Current);
        mHullVerts.Add(v1);
        iter.MoveNext();
        v2 = ((Triangle.Edge) iter.Current).EdgeLine.Start;
        mHull.Add((Triangle.Edge) iter.Current);
        mHullVerts.Add(v2);
        iter.MoveNext();

        while (true)
        {
            Triangle.Edge tv1 = mHull[mHull.Count - 1];
            Triangle.Edge tv2 = mHull[mHull.Count - 2];
            Triangle.Edge tv3 = (Triangle.Edge) iter.Current;

            tv3 = (Triangle.Edge) iter.Current;
            v1 = tv1.EdgeLine.Start;
            v2 = tv2.EdgeLine.Start;
            v3 = tv3.EdgeLine.Start;
            Vector3 u1 = v2 - v1;
            Vector3 u2 = v3 - v2;
            float cross = (u1.x * u2.y) - (u1.y * u2.x);
            int last = mHull.Count - 1;

            if (cross > 0)
            {
                mHull.Add(tv3);
                mHullVerts.Add(v3);
            }
            else if (last > start)
            {
                mHull.RemoveAt(last);
                mHullVerts.RemoveAt(last);
                mCurLine.positionCount = mHullVerts.Count;
                mCurLine.SetPositions(mHullVerts.ToArray());
                yield return new WaitForEndOfFrame();
                continue;
            }
            else
            {
                mHull[last] = tv3;
                mHullVerts[last] = v3;
            }
            mCurLine.positionCount = mHullVerts.Count;
            mCurLine.SetPositions(mHullVerts.ToArray());
            if (!iter.MoveNext())
            {
                break;
            }
            yield return new WaitForEndOfFrame();
        }
        yield return new WaitForEndOfFrame();
    }
}
