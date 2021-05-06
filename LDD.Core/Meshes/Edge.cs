﻿using LDD.Common.Simple3D;
using System.Collections.Generic;

namespace LDD.Core.Meshes
{
    public class Edge : IEqualityComparer<Edge>
    {
        public Vertex P1 { get; set; }
        public Vertex P2 { get; set; }

        public Vector3 EdgeNormal => ((P1.Normal + P2.Normal) / 2f).Normalized();

        public static bool CompareByPosition = false;

        public Vector3 Vector => (P2.Position - P1.Position).Normalized();

        public Edge(Vertex p1, Vertex p2)
        {
            P1 = p1;
            P2 = p2;
        }

        public override bool Equals(object obj)
        {
            if (obj is Edge edge)
            {
                return Equals(edge, CompareByPosition);
            }
            return false;
        }

        public bool Equals(Edge edge, bool compareByPos)
        {
            if (compareByPos)
            {
                return (edge.P1.Position.Equals(P1.Position) && edge.P2.Position.Equals(P2.Position)) ||
                    (edge.P1.Position.Equals(P2.Position) && edge.P2.Position.Equals(P1.Position));
            }
            else
            {
                return (edge.P1.Equals(P1) && edge.P2.Equals(P2)) || (edge.P1.Equals(P2) && edge.P2.Equals(P1));
            }
        }

        public override int GetHashCode()
        {
            var hashCode = 162377905;
            int p1h = CompareByPosition ? P1.Position.Rounded(4).GetHashCode() : P1.GetHashCode();
            int p2h = CompareByPosition ? P2.Position.Rounded(4).GetHashCode() : P2.GetHashCode();

            if (p1h < p2h)
            {
                hashCode = hashCode * -1521134295 + p1h;
                hashCode = hashCode * -1521134295 + p2h;
            }
            else
            {
                hashCode = hashCode * -1521134295 + p2h;
                hashCode = hashCode * -1521134295 + p1h;
            }
            return hashCode;
        }

        public bool Contains(Vertex vertex, bool checkPosition = false)
        {
            if (checkPosition)
                return P1.Position.Equals(vertex.Position) || P2.Position.Equals(vertex.Position);
            return P1.Equals(vertex) || P2.Equals(vertex);
        }

        public bool Contains(Vector3 position)
        {
            return P1.Position.Equals(position) || P2.Position.Equals(position);
        }

        public bool Contains(Vector3 position, float precision)
        {
            return P1.Position.Equals(position, precision) || P2.Position.Equals(position, precision);
        }

        public bool IsInside(Vector3 pos)
        {
            var maxV = (P2.Position - P1.Position);
            var diff = (pos - P1.Position);
            if (diff.Length <= 0.001f)
                return true;
            return diff.Normalized() == maxV.Normalized() && diff.Length <= maxV.Length;
        }

        public bool Equals(Edge x, Edge y)
        {
            return x.Equals(y);
        }

        public int GetHashCode(Edge obj)
        {
            return obj.GetHashCode();
        }
    }
}
