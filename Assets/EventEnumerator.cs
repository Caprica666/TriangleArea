﻿using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EventEnumerator : RBTree<VertexEvent>.Enumerator
{
    public EventEnumerator(RBTree<VertexEvent> tree)
    : base(tree)
    {
    }

    public override void Reset()
    {
        stack.Clear();
        Intialize();
        version = tree.Version;
    }

    public VertexEvent FindAt(Vector3 point)
    {
        IComparer<VertexEvent> comparer = tree.Comparer;
        VecCompare vcompare = new VecCompare();
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
            order = vcompare.Compare(Current.Point, point);

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
                    return null;
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
                    return null;
                }
            }
            else // P == current point
            {
                break;
            }
        }     
        return Current;
    }

    public virtual bool MovePrev()
    {
        if (stack.Count == 0)
        {
            current = null;
            return false;
        }
        current = stack.Pop();
        RBTree<VertexEvent>.Node node = current.Left;
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

    public int CollectAt(VertexEvent ev, List<VertexEvent> collected)
    {
        Vector3 point = ev.Point;
        VertexEvent temp = FindAt(point);
        Stack<RBTree<VertexEvent>.Node> saveStack = new Stack<RBTree<VertexEvent>.Node>(stack.Reverse());
        int i, n = -1;

        if (temp == null)
        {
            return n;
        }
        i = CollectBefore(ev, collected);
        if (i >= 0)
        {
            n = i;
        }
        stack = saveStack;
        MoveNext();
        i = CollectAfter(ev, collected);
        if (i >= 0)
        {
            n = i;
        }
        if (collected.Count == 0)
        {
            return -1;
        }
        return n;
    }

    private int CollectAfter(VertexEvent ev, List<VertexEvent> collected)
    {
        int n = -1;
        VecCompare vcompare = new VecCompare();
        Vector3 point = ev.Point;

        while (MoveNext())
        {
            if (ev == Current)
            {
                n = collected.Count;
            }
            if (vcompare.Compare(Current.Point, point) == 0)
            {
                collected.Add(Current);
            }
            else
            {
                break;
            }
        }
        return n;
    }

    private int CollectBefore(VertexEvent ev, List<VertexEvent> collected)
    {
        int i = -1;
        VecCompare vcompare = new VecCompare();
        Vector3 point = ev.Point;

        while (MovePrev())
        {
            if (ev == Current)
            {
                i = collected.Count;
            }
            if (vcompare.Compare(Current.Point, point) == 0)
            {
                collected.Insert(0, Current);
            }
            else
            {
                break;
            }
        }
        if (i >= 0)
        {
            return collected.Count - i - 1;
        }
        return -1;
    }

    public override string ToString()
    {
        string s = "";
        foreach (RBTree<VertexEvent>.Node n in stack)
        {
            s += n.Item.ToString() + '\n';
        }
        return s;
    }

};
