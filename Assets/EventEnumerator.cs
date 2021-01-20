using System;
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

    public VertexEvent MoveAfter(VertexEvent ev)
    {
        IComparer<VertexEvent> comparer = tree.Comparer;
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
            order = comparer.Compare(Current, ev);

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
        return MoveNext() ? Current : null;
    }

    public VertexEvent MoveBefore(VertexEvent ev)
    {
        IComparer<VertexEvent> comparer = tree.Comparer;
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
            order = comparer.Compare(Current, ev);
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
        return MovePrev() ? Current : null;
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

    public int CollectAfter(VertexEvent ev, List<VertexEvent> collected)
    {
        VertexEvent temp = MoveAfter(ev);
        int n = 0;
        VecCompare vcompare = new VecCompare();
        Vector3 point = ev.Point;

        while ((temp != null) &&
               (vcompare.Compare(temp.Point, ev.Point) == 0))
        {
            collected.Add(temp);
            if (MoveNext())
            {
                temp = Current;
                ++n;
            }
            else
            {
                break;
            }
        }
        return n;
    }

    public int CollectBefore(VertexEvent ev, List<VertexEvent> collected)
    {
        VertexEvent temp = MoveBefore(ev);
        int n = 0;
        VecCompare vcompare = new VecCompare();
        Vector3 point = ev.Point;

        while ((temp != null) &&
               (vcompare.Compare(temp.Point, ev.Point) == 0))
        {
            collected.Add(temp);
            if (MovePrev())
            {
                temp = Current;
                ++n;
            }
            else
            {
                break;
            }
        }
        return n;
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
