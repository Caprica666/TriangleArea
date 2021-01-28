using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LineEnumerator : RBTree<Edge>.Enumerator
{
    public LineEnumerator(RBTree<Edge> lines)
    : base(lines)
    {
    }

    public override void Reset()
    {
        stack.Clear();
        Intialize();
        version = tree.Version;
    }

    public Edge FindTopNeighbor(Edge l)
    {
        IComparer<Edge> comparer = tree.Comparer;
        int order;

        if (Root == null)
        {
            return null;
        }
        Reset();
        stack.Clear();
        stack.Push(Root);
        current = Root;
        while (stack.Count > 0)
        {
            current = stack.Peek();
            order = comparer.Compare(Current, l);

            // P > current node, move right or stop
            if (order < 0)
            {
                stack.Pop();
                if (current.Right != null)
                {
                    stack.Push(current.Right);
                    continue;
                }
                else
                {
                    break;
                }
            }
            // P < current node, move left or move next
            else if (order > 0)
            {
                if (current.Left != null)
                {
                    stack.Push(current.Left);
                    continue;
                }
                else
                {
                    break;
                }
            }
            else // P == current point
            {
                MoveNext();
                break;
            }
        }
        while (MoveNext())
        {
            if (Current.Tri != l.Tri)
            {
                if (l.Tri.ID != Current.Tri.ID)
                {
                    return Current;
                }
                Debug.LogError("ERROR: Different triangles have the same ID " + l.Tri.ID);
            }
        }
        return null;
    }

    public Edge FindBottomNeighbor(Edge l)
    {
        IComparer<Edge> comparer = tree.Comparer;
        int order;

        if (Root == null)
        {
            return null;
        }
        Reset();
        stack.Clear();
        stack.Push(Root);
        current = Root;
        while (stack.Count > 0)
        {
            current = stack.Peek();
            order = comparer.Compare(Current, l);
            // P >= current node, move right or stop
            if (order < 0)
            {
                if (current.Right != null)
                {
                    stack.Push(current.Right);
                    continue;
                }
                else
                {
                    break;
                }
            }
            // P < current node, move left or move prev
            else if (order > 0)
            {
                stack.Pop();
                if (current.Left != null)
                {
                    stack.Push(current.Left);
                    continue;
                }
                else
                {
                    break;
                }
            }
            else // P == current point
            {
                MovePrev();
                break;
            }
        }
        while (MovePrev())
        {
            if (Current.Tri != l.Tri)
            {
                if (l.Tri.ID != Current.Tri.ID)
                {
                    return Current;
                }
                Debug.LogError("ERROR: Different triangles have the same ID " + l.Tri.ID);
            }
        }
        return null;
    }

    public virtual bool MovePrev()
    {
        if (stack.Count == 0)
        {
            current = null;
            return false;
        }
        current = stack.Pop();
        RBTree<Edge>.Node node = current.Left;
        if (node != null)
        {
            stack.Push(node);
            node = node.Right;
            while (node != null)
            {
                stack.Push(node);
                node = node.Right;
            }
        }
        return true;
    }

    public override string ToString()
    {
        string s = "";
        foreach (RBTree<Edge>.Node n in stack)
        {
            s += n.Item.ToString() + '\n';
        }
        return s;
    }

};
