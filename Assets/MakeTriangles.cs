using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MakeTriangles : MonoBehaviour
{
    public bool Test;
    public bool New;
    public bool Step;

    private TriangleMesh mTriMesh;
    private TriangleMesh mClipMesh;
    private TriangleMesh mTestMesh;
    private List<Triangle.Edge> mTriVerts;
    private TriangleList mClipList;
    private TriangleList mTriList;
    private Hull mHull;
    private Rect mBounds = new Rect(0, 0, 6, 6);
    private TriangleList mTest = new TriangleList();

    // Start is called before the first frame update
    void Start()
    {
        Init();
//        StartCoroutine("Area");
    }

    private void Init()
    {
        GameObject cliplist = GameObject.Find("ClipList");
        mTriMesh = gameObject.GetComponent<TriangleMesh>() as TriangleMesh;
        mClipMesh = cliplist.GetComponent<TriangleMesh>() as TriangleMesh;
        mTriList = new TriangleList();
        mTriList.TriMesh = mTriMesh;
        mClipList = new TriangleList();
        mClipList.TriMesh = mClipMesh;
        Component[] lines = gameObject.GetComponentsInChildren(typeof(LineRenderer));

        foreach (Component line in lines)
        {
            DestroyImmediate(line.gameObject);
        }
        mHull = null;
    }

    private void Update()
    {
        if (Test)
        {
            Test = false;
            mTriList.Clear();
            mClipList.Clear();
            mTriMesh.Clear();
            mClipMesh.Clear();
            Triangle t1 = new Triangle(new Vector3(-0.1f, -0.6f, 0),
                                       new Vector3(2.4f, -0.2f, 0),
                                       new Vector3(1.4f, -1.8f, 0), 0);
            Triangle t2 = new Triangle(new Vector3(2.6f, -1, 0),
                                      new Vector3(1, 0.4f, 0),
                                      new Vector3(1.9f, -1.5f, 0), 0);
            GameObject testlist = GameObject.Find("TestList");
            List<Triangle> tlist = new List<Triangle> { t1, t2 };
            mTestMesh = testlist.GetComponent<TriangleMesh>() as TriangleMesh;
            mTest.TriMesh = mTestMesh;
            mTest.Clear();
            mTestMesh.Clear();
            t1.TriColor = new Color(0, 1, 0.8f, 0.5f);
            t2.TriColor = new Color(1, 0, 0.8f, 0.5f);
            mTest.Add(t1, true);
            mTest.Add(t2, true);
            StartCoroutine("TestClip");
        }
        else if (New)
        {
            Init();
            New = false;
            mTest.Clear();
            if (mTestMesh != null)
            {
                mTestMesh.Clear();
            }
            StartCoroutine(Area());
        }
        else if (Step)
        {
            Step = false;
            if ((mTriVerts.Count >= 4) && (mTriList.Count > 1))
            {
                StartCoroutine(NewHull());
            }
        }
    }

    IEnumerator NewHull()
    {
        Color c = new Color(Random.value, Random.value, Random.value, 1);
        GameObject line = new GameObject("line");

        mTriVerts = mTriList.Edges;
        yield return new WaitForEndOfFrame();
        mHull = line.AddComponent<Hull>() as Hull;
        line.transform.SetParent(gameObject.transform, false);
        yield return new WaitForEndOfFrame();
        yield return StartCoroutine(mHull.MakeHull(mTriVerts, c));
        if (mHull.Edges.Count >= 4)
        {
            yield return StartCoroutine(mTriList.Clip(mHull.Edges, mClipList));
            yield return new WaitForEndOfFrame();
        }
    }

    IEnumerator Area()
    {
        mClipList.Clear();
        mClipMesh.Clear();
        mTriMesh.Clear();
        mTriMesh.NewTriangles(mBounds);
        mTriVerts = mTriMesh.PrepareTriangles(mTriList, true);
        yield return new WaitForEndOfFrame();
        yield return StartCoroutine(NewHull());
    }

    IEnumerator TestClip()
    {
        yield return new WaitForEndOfFrame();
        mTest.Display();
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        mClipList.Clear();
        yield return StartCoroutine(mTest.ClipAll(mClipList));
    }

}

   
