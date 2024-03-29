﻿using LDD.Core.Primitives;
using LDD.Common.Simple3D;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LDD.Common.Maths;

namespace LDD.Core.Meshes
{
    public class OutlinesGenerator
    {
        public const float fPI = (float)Math.PI;
        public const float DEFAULT_LINE_THICKNESS = 0.013f;

        public float AngleThreshold { get; set; }
        public float ThicknessMultiplier { get; set; }

        public float OutlineThickness => DEFAULT_LINE_THICKNESS * ThicknessMultiplier;

        public struct SimpleEdge : IEquatable<SimpleEdge>
        {
            public Vector3 P1;
            public Vector3 P2;

            public SimpleEdge(Edge edge)
            {
                P1 = edge.P1.Position.Rounded();
                P2 = edge.P2.Position.Rounded();
            }

            public SimpleEdge(Vector3 p1, Vector3 p2)
            {
                P1 = p1;
                P2 = p2;
            }

            public bool Equals(SimpleEdge other)
            {
                return (other.P1.Equals(P1) && other.P2.Equals(P2)) || (other.P1.Equals(P2) && other.P2.Equals(P1));
            }

            public override bool Equals(object obj)
            {
                if (obj is SimpleEdge se)
                    return Equals(se);
                return base.Equals(obj);
            }

            public bool Contains(Vector3 pt)
            {
                return P1 == pt || P2 == pt;
            }

            public override int GetHashCode()
            {
                var hashCode = 162377905;
                int p1h = P1.GetHashCode();
                int p2h = P2.GetHashCode();

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
        }

        class FaceEdge
        {
            public Triangle Face { get; set; }
            public Vector3 P1 { get; set; }
            public Vector3 P2 { get; set; }

            public Vector3 FaceNormal { get; set; }
            public Vector3 EdgeNormal { get; set; }

            public FaceEdge(SimpleEdge edge, Triangle face)
            {
                Face = face;
                P1 = edge.P1;
                P2 = edge.P2;
                var v1 = face.GetVertexByPosition(P1);
                var v2 = face.GetVertexByPosition(P2);
                EdgeNormal = ((v1.Normal + v2.Normal) / 2f).Normalized().Rounded();
                FaceNormal = face.Normal.Rounded();
            }

            public float GetNormalDiff(FaceEdge other)
            {
                float angleDiff = Vector3.AngleBetween(EdgeNormal, other.EdgeNormal);
                if (angleDiff < 0)
                    angleDiff = (((float)Math.PI * 2f) + angleDiff) % ((float)Math.PI * 2f);
                return angleDiff;
            }
        }

        class HardEdge : IEquatable<HardEdge>
        {
            public Vector3 P1 { get; set; }
            public Vector3 P2 { get; set; }

            public Vector3 FaceNormal { get; set; }
            public Vector3 EdgeNormal { get; set; }
            public Vector3 OutlineDirection { get; set; }

            public HardEdge PrevEdge { get; set; }

            public HardEdge NextEdge { get; set; }

            public HardEdge(SimpleEdge edge, Triangle face)
            {
                P1 = edge.P1;
                P2 = edge.P2;
                var v1 = face.GetVertexByPosition(P1);
                var v2 = face.GetVertexByPosition(P2);

                EdgeNormal = ((v1.Normal + v2.Normal) / 2f).Normalized().Rounded();  
                FaceNormal = face.Normal.Rounded();
                OutlineDirection = Vector3.GetPerpendicular(P1, P2, face.GetCenter()).Normalized().Rounded();
            }

            public HardEdge(Vector3 p1, Vector3 p2, Vector3 faceNormal, Vector3 outlineDirection)
            {
                P1 = p1;
                P2 = p2;
                FaceNormal = faceNormal;
                OutlineDirection = outlineDirection;
            }

            public void CorrectOrder()
            {
                var line = (P2 - P1).Normalized();
                var avgFaceNorm = Vector3.Cross(line, OutlineDirection);
                if (Vector3.Distance(avgFaceNorm, FaceNormal) > 0.2)
                {
                    var tmp = P2;
                    P2 = P1;
                    P1 = tmp;
                }
            }

            public override int GetHashCode()
            {
                var hashCode = 162377905;
                int p1h = P1/*.Rounded(4)*/.GetHashCode();
                int p2h = P2/*.Rounded(4)*/.GetHashCode();

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

                hashCode = hashCode * -1521134295 + EdgeNormal.GetHashCode();
                hashCode = hashCode * -1521134295 + FaceNormal.GetHashCode();

                return hashCode;
            }
        
            public float DistanceFrom(Vector3 point)
            {
                return Vector3.GetPerpendicularDistance(P1, P2, point);
            }

            public float DistanceFrom(Triangle tri)
            {
                float minDist = float.MaxValue;
                foreach (var v in tri.Vertices)
                {
                    var dist = DistanceFrom(v.Position);
                    if (dist < minDist)
                        minDist = dist;
                }
                return minDist;
            }

            public bool Equals(HardEdge other)
            {
                if (other == null)
                    return false;

                return (other.P1.Equals(P1) && other.P2.Equals(P2)) || (other.P1.Equals(P2) && other.P2.Equals(P1))
                    && other.EdgeNormal.Equals(EdgeNormal)
                    && other.FaceNormal.Equals(FaceNormal);
            }

            public bool Contains(Vector3 position)
            {
                return P1.Equals(position) || P2.Equals(position);
            }

            public Vector3 GetOpposite(Vector3 position)
            {
                return P1.Equals(position) ? P2 : P1;
            }

            public override bool Equals(object obj)
            {
                if (obj is HardEdge he)
                    return Equals(he);
                return base.Equals(obj);
            }

            public static bool operator ==(HardEdge e1, HardEdge e2)
            {
                if (e1 is null || e2 is null)
                    return e1 is null && e2 is null;
                return e1.Equals(e2);
            }

            public static bool operator !=(HardEdge e1, HardEdge e2)
            {
                return !(e1 == e2);
            }
        }
        
        class PlanarEdge
        {
            public Triangle Face { get; set; }
            public HardEdge OrigEdge { get; set; }
            public Vector3 P1 { get; set; }
            public Vector3 P2 { get; set; }
            public Vector3 OutlineDirection { get; set; }
            public Vector3 Direction { get; set; }

            public bool IsTriangleEdge { get; set; }

            public bool OutlineIsOutsideTriangle { get; set; }

            public PlanarEdgePair PrevEdgeInfo { get; set; }

            public PlanarEdgePair NextEdgeInfo { get; set; }

            public IEnumerable<PlanarEdgePair> SurroundingEdges
            {
                get
                {
                    if (PrevEdgeInfo != null)
                        yield return PrevEdgeInfo;
                    if (NextEdgeInfo != null)
                        yield return NextEdgeInfo;
                }
            }

            public bool IsUsedInIntersection => SurroundingEdges.Any(x => x.IsObtuse);

            public PlanarEdge(HardEdge edge, Triangle triangle, Plane plane)
            {
                Face = triangle;
                OrigEdge = edge;
                P1 = plane.ProjectPoint(edge.P1);
                P2 = plane.ProjectPoint(edge.P2);
                OutlineDirection = plane.ProjectVector(edge.OutlineDirection);

                IsTriangleEdge = triangle.ContainsVertex(edge.P1) &&
                    triangle.ContainsVertex(edge.P2) &&
                    triangle.Normal.Equals(edge.FaceNormal, 0.05f);


                Direction = (P2 - P1).Normalized();
            }

            public PlanarEdge(SimpleEdge edge, Vector3 outlineDirection, Triangle triangle, Plane plane)
            {
                Face = triangle;
                P1 = plane.ProjectPoint(edge.P1);
                P2 = plane.ProjectPoint(edge.P2);
                OutlineDirection = plane.ProjectVector(outlineDirection);

                Direction = (P2 - P1).Normalized();
            }

            public bool Contains(Vector3 position)
            {
                return P1.Equals(position) || P2.Equals(position);
            }

            public bool Colinear(PlanarEdge other)
            {
                if (!(other.Contains(P1) || other.Contains(P2)))
                    return false;

                float angleDiff = Vector3.AngleBetween(Direction, other.Direction);
                return float.IsNaN(angleDiff) || angleDiff < 0.08;
            }

            public bool IsVertexLinked(Vector3 vertex)
            {
                if (P1.Equals(vertex))
                    return OrigEdge.PrevEdge != null;
                else if (P2.Equals(vertex))
                    return OrigEdge.NextEdge != null;
                return false;
            }

            public bool IsOppositeVertexLinked(Vector3 vertex)
            {
                if (P2.Equals(vertex))
                    return OrigEdge.PrevEdge != null;
                else if (P1.Equals(vertex))
                    return OrigEdge.NextEdge != null;
                return false;
            }

            public Vector3 GetOppositeVertex(Vector3 vertex)
            {
                if (P1.Equals(vertex))
                    return P2;
                return P1;
            }

            public PlanarEdgePair GetConnectionInfo(PlanarEdge otherEdge)
            {
                if (PrevEdgeInfo?.ContainsEdge(otherEdge) ?? false)
                    return PrevEdgeInfo;

                if (NextEdgeInfo?.ContainsEdge(otherEdge) ?? false)
                    return NextEdgeInfo;

                return null;
            }

            public Vector3 GetCommonVertex(PlanarEdge otherEdge)
            {
                if (otherEdge.Contains(P1))
                    return P1;
                else if (otherEdge.Contains(P2))
                    return P2;
                return Vector3.Empty;
            }

            //public bool IsOtherEndClipped(PlanarEdge linkedEdge)
            //{
            //    var commonVert = GetCommonVertex(linkedEdge);
            //    if (PrevEdgeInfo != null && PrevEdgeInfo.CommonVertex != commonVert)
            //        return PrevEdgeInfo.IsObtuse;
            //    if (NextEdgeInfo != null && NextEdgeInfo.CommonVertex != commonVert)
            //        return NextEdgeInfo.IsObtuse;
            //    return false;
            //}

            public static float CalculateAngleBetween(PlanarEdge edge1, PlanarEdge edge2)
            {
                Vector3 commonVert;
                if (edge2.Contains(edge1.P1))
                    commonVert = edge1.P1;
                else if (edge2.Contains(edge1.P2))
                    commonVert = edge1.P2;
                else
                    return 0;

                var opp1 = edge1.GetOppositeVertex(commonVert);
                var dir1 = (opp1 - commonVert).Normalized();

                var avgNormal = Vector3.Avg(edge1.OutlineDirection, edge2.OutlineDirection).Normalized();

                var edgeAngleDiff = Vector3.AngleBetween(avgNormal, dir1);
                return edgeAngleDiff * 2f;
            }

            public bool IsConnectedTo(PlanarEdge other)
            {
                if (other == this)
                    return false;

                if (NextEdgeInfo != null && NextEdgeInfo.ContainsEdge(other))
                    return true;
                if (PrevEdgeInfo != null && PrevEdgeInfo.ContainsEdge(other))
                    return true;

                return false;
            }

            public void Disconnect(PlanarEdgePair edgePair)
            {
                if (PrevEdgeInfo?.IsEqual(edgePair) ?? false)
                    PrevEdgeInfo = null;

                if (NextEdgeInfo?.IsEqual(edgePair) ?? false)
                    NextEdgeInfo = null;
            }
        }

        class PlanarEdgePair
        {
            public PlanarEdge Edge1 { get; set; }
            public PlanarEdge Edge2 { get; set; }
            public Vector3 CommonVertex { get; set; }
            public float TotalAngle { get; set; }
            public float AngleDiff { get; set; }
            public bool IsObtuse => TotalAngle >= fPI;
            public Vector3 OutlineBisector { get; private set; }

            public PlanarEdgePair(PlanarEdge edge1, PlanarEdge edge2)
            {
                Edge1 = edge1;
                Edge2 = edge2;
                CommonVertex = edge1.GetCommonVertex(edge2);
                TotalAngle = PlanarEdge.CalculateAngleBetween(edge1, edge2);
                AngleDiff = Vector3.AngleBetween(edge1.OutlineDirection, edge2.OutlineDirection);
                OutlineBisector = Vector3.Avg(edge1.OutlineDirection, edge2.OutlineDirection);
            }

            public bool IsEqual(PlanarEdgePair pair)
            {
                return ContainsEdge(pair.Edge1) && ContainsEdge(pair.Edge2);
                //return (Edge1 == pair.Edge1 && Edge2 == pair.Edge2) ||
                //    (Edge2 == pair.Edge1 && Edge1 == pair.Edge2);
            }

            public bool ContainsEdge(PlanarEdge edge)
            {
                return Edge1 == edge || Edge2 == edge;
            }

            public PlanarEdge GetOtherEdge(PlanarEdge edge)
            {
                if (Edge1 == edge)
                    return Edge2;
                if (Edge2 == edge)
                    return Edge1;
                return null;
            }
        }

        class ProjectedEdge
        {
            public PlanarEdge PlanarEdge { get; set; }
            public Vector3 TargetVertex { get; set; }
            public Vector2 P1 { get; set; }
            public Vector2 P2 { get; set; }
            public Vector2 P3 { get; set; }

            public float MinX { get; set; }
            public float MaxX { get; set; }

            public Vector2[] UVs => new Vector2[] { P1, P2, P3 };

            public bool NeedsToBeClipped { get; set; }

            public bool IsOutsideTriangle { get; set; }

            public bool IsDeadEnd { get; set; }

            public ProjectedEdge CombineWith { get; set; }

            public void ValidateValues(float outlineThickness)
            {
                var minPos = Vector2.Min(P1, P2, P3);
                var maxPos = Vector2.Max(P1, P2, P3);

                if (minPos.Y < 0 && Math.Abs(minPos.Y) > outlineThickness)
                    NeedsToBeClipped = true;

                if (maxPos.Y < outlineThickness && (maxPos.Y - minPos.Y) > outlineThickness)
                    IsOutsideTriangle = true;

                //Dead End
                if (!PlanarEdge.IsVertexLinked(TargetVertex))
                {
                    //TO IMPROVE
                    IsDeadEnd = true;
                    NeedsToBeClipped = true;
                }
            }

            public void AdjustValues(float thicknessMultiplier = 1f)
            {
                var minPos = Vector2.Min(P1, P2, P3);

                for (int i = 0; i < 3; i++)
                {
                    var pt = UVs[i];
                    pt.X -= minPos.X;
                    pt.X /= thicknessMultiplier;
                    pt.Y /= thicknessMultiplier;
                    switch (i)
                    {
                        case 0:
                            P1 = pt;
                            break;
                        case 1:
                            P2 = pt;
                            break;
                        case 2:
                            P3 = pt;
                            break;
                    }
                }
            }
        }

        class LineSegment2D
        {
            public Vector2 P1 { get; set; }
            public Vector2 P2 { get; set; }
            public Line Equation { get; set; }

            public LineSegment2D(Vector2 p1, Vector2 p2)
            {
                P1 = p1;
                P2 = p2;
                Equation = Line.FromPoints(p1, p2);
            }

            public bool Intersects(LineSegment2D other, out Vector2 intersection)
            {
                intersection = Vector2.Empty;
                if (Intersects(other.Equation, out intersection))
                    return other.Intersects(Equation, out _);
                return false;
            }

            public bool Intersects(Line line, out Vector2 intersection)
            {
                intersection = Vector2.Empty;
                if (Equation.Intersect(line, out intersection))
                {
                    if (P1.Equals(intersection) || P2.Equals(intersection))
                        return true;
                    var segLength = (P2 - P1).Length;
                    //var interVec = (intersection - P1);
                    var maxDist = Math.Max((intersection - P1).Length, (intersection - P2).Length);
                    return maxDist <= segLength;
                }
                return false;
            }
        }

        class Triangle2D
        {
            public LineSegment2D[] Edges { get; set; }

            public Triangle2D(Triangle tri, Plane facePlane, Vector3 axis)
            {
                var tri1 = facePlane.ProjectPoint2D(axis, tri.V1.Position);
                var tri2 = facePlane.ProjectPoint2D(axis, tri.V2.Position);
                var tri3 = facePlane.ProjectPoint2D(axis, tri.V3.Position);
                Edges = new LineSegment2D[]
                {
                    new LineSegment2D(tri1, tri2),
                    new LineSegment2D(tri2, tri3),
                    new LineSegment2D(tri3, tri1),
                };
            }

            public bool IntersectsLine(LineSegment2D line)
            {
                return Edges.Any(x => x.Intersects(line, out _));
            }
        }

        class HardEdgeDictionary
        {
            public List<HardEdge> HardEdges { get; private set; }
            private Dictionary<Vector3, List<HardEdge>> InnerDict;
            
            public float BreakAngle { get; private set; }
            public float BreakAngleRad { get; private set; }

            public void Initialize(List<Triangle> triangles, float breakAngle)
            {
                BreakAngle = breakAngle;
                BreakAngleRad = breakAngle / 180f * fPI;

                InnerDict = new Dictionary<Vector3, List<HardEdge>>();
                HardEdges = CalculateHardEdges(triangles, BreakAngleRad);

                foreach (var hEdge in HardEdges)
                {
                    var p1Round = hEdge.P1.Rounded();
                    if (!InnerDict.ContainsKey(p1Round))
                        InnerDict.Add(p1Round, new List<HardEdge>());
                    InnerDict[p1Round].Add(hEdge);
                    var p2Round = hEdge.P2.Rounded();
                    if (!InnerDict.ContainsKey(p2Round))
                        InnerDict.Add(p2Round, new List<HardEdge>());
                    InnerDict[p2Round].Add(hEdge);
                }

                LinkEdges();
            }

            private void LinkEdges()
            {
                var tp1 = new Vector3(1.48f, 3.50971f, -0.8f);
                var tp2 = new Vector3(1.29029f, 3.32f, -0.8f);
                foreach (var edge in HardEdges)
                {
                    if (edge.Contains(tp1) && edge.Contains(tp2))
                    {

                    }
                    //if (edge.P1.Equals(new Vector3(1.5f, 0.42f, 1.16f)) &&
                    //    edge.P2.Equals(new Vector3(1.5f, 0.42f, -0.4f)))
                    //{

                    //}
                    if (edge.PrevEdge == null && GetBySharedVertex(edge, edge.P1, out HardEdge connectedEdge))
                    {
                        edge.PrevEdge = connectedEdge;
                        connectedEdge.NextEdge = edge;
                    }

                    if (edge.NextEdge == null && GetBySharedVertex(edge, edge.P2, out connectedEdge))
                    {
                        edge.NextEdge = connectedEdge;
                        connectedEdge.PrevEdge = edge;
                    }
                }
            }

            public IEnumerable<HardEdge> GetEdgesForTriangle(Triangle triangle)
            {
                var closeEdges = new List<HardEdge>();

                for (int i = 0; i < 3; i++)
                {
                    var triMapPos = triangle.Vertices[i].Position.Rounded();
                    if (InnerDict.ContainsKey(triMapPos))
                        closeEdges.AddRange(InnerDict[triMapPos]);
                }

                return closeEdges
                    .Distinct()
                    .Where(e => Vector3.AngleBetween(triangle.Normal, e.EdgeNormal) <= BreakAngleRad);
            }

            public bool GetBySharedVertex(HardEdge edge, Vector3 vertex, out HardEdge result)
            {
                result = null;
                if (InnerDict.TryGetValue(vertex, out List<HardEdge> sharedEdges))
                {
                    var nearEdges = sharedEdges
                        .Where(x => x != edge 
                            && Vector3.AngleBetween(x.FaceNormal, edge.FaceNormal) <= BreakAngleRad
                            && Vector3.AngleBetween(x.OutlineDirection, edge.OutlineDirection) <= fPI * 0.85
                        )
                        .OrderBy(x => Vector3.AngleBetween(x.FaceNormal, edge.FaceNormal))
                        .ThenBy(x => Vector3.AngleBetween(x.OutlineDirection, edge.OutlineDirection));

                    if (nearEdges.Count() > 0)
                    {
                        result = nearEdges.First();
                        return true;
                    }
                }
                return false;
            }
        }

        public OutlinesGenerator()
        {
            AngleThreshold = 35f;
            ThicknessMultiplier = 1f;
        }

        public OutlinesGenerator(float angleThreshold, float thicknessMultiplier)
        {
            AngleThreshold = angleThreshold;
            ThicknessMultiplier = thicknessMultiplier;
        }



        //public class GenerationTracker
        //{
        //    public Triangle CurrentTriangle { get; set; }
        //    public Vertex CurrentVertex { get; set; }
        //}

        public static IEnumerable<SimpleEdge> CalculateHardEdges(IEnumerable<Triangle> triangles, float breakAngle)
        {
            float breakAngleRad = breakAngle / 180f * fPI;
            var triangleList = triangles.ToList();
            var hardEdgeDict = new HardEdgeDictionary();
            hardEdgeDict.Initialize(triangleList, breakAngle);
            var simpleEdges = hardEdgeDict.HardEdges.Select(e => new SimpleEdge(e.P1, e.P2));
            simpleEdges = simpleEdges.Distinct();
            return simpleEdges;
        }

        public void GenerateOutlines(IEnumerable<Triangle> triangles)
        {
            float breakAngleRad = AngleThreshold / 180f * fPI;

            var triangleList = triangles.ToList();

            var sw = Stopwatch.StartNew();
            var hardEdgeDict = new HardEdgeDictionary();
            hardEdgeDict.Initialize(triangleList, AngleThreshold);
            sw.Stop();
            Console.WriteLine($"Calculate Hard Edges => {sw.Elapsed}");

            sw.Restart();
            //var tp1 = new Vector3(1.48f, 3.50971f, -0.8f);
            //var tp2 = new Vector3(1.29029f, 3.32f, -0.8f);
            //var tp3 = new Vector3(0f, 5.96f, -0.8f);

            foreach (var triangle in triangleList)
            {
                var facePlane = new Plane(triangle.GetCenter(), triangle.Normal);
     
                var planarEdges = CalculatePlanarEdges(triangle, hardEdgeDict);

                var projectedEdges = new List<ProjectedEdge>();

                //if (triangle.ContainsVertex(tp1) && triangle.ContainsVertex(tp2) && triangle.ContainsVertex(tp3))
                //{

                //}

                foreach (var vert in triangle.Vertices)
                {
                    var edgesConnectedToVertex = planarEdges.Where(x => x.Contains(vert.Position)).ToList();

                    // remove edges that must be intersected with edges that are not connected to the current vertex
                    if (edgesConnectedToVertex.Any(x => x.IsUsedInIntersection))
                    {
                        var interEdges = edgesConnectedToVertex.Where(x => x.IsUsedInIntersection).ToList();
                        foreach (var edge in interEdges)
                        {
                            if (!edgesConnectedToVertex.Any(e => edge.IsConnectedTo(e)))
                                edgesConnectedToVertex.Remove(edge);
                        }
                    }

                    var projections = edgesConnectedToVertex.Select(x => ProjectTriangle(x, triangle, vert.Position)).ToList();

                    projections.RemoveAll(p => p.IsOutsideTriangle);

                    if (projections.Count(p => p.IsDeadEnd) >= 2)
                    {
                        var deadEnds = projections.Where(p => p.IsDeadEnd);
                        var triCenter = triangle.GetCenter();
                        var projectionToKeep = deadEnds.OrderBy(p => Vector3.Distance(vert.Position + p.PlanarEdge.OutlineDirection, triCenter)).First();
                        projections.RemoveAll(p => p.IsDeadEnd && p != projectionToKeep);
                    }

                    if (projections.Count == 2)
                    {
                        var edge1 = projections[0].PlanarEdge;
                        var edge2 = projections[1].PlanarEdge;

                        var projection1 = projections[0];
                        var projection2 = projections[1];

                        if (edge1.Colinear(edge2) && !(projection1.NeedsToBeClipped || projection2.NeedsToBeClipped))
                        {
                            projectedEdges.Add(projection1);
                        }
                        else
                        {
                            bool mustIntersect = false;

                            var edgeLinkInfo = edge1.GetConnectionInfo(edge2);

                            if (edgeLinkInfo != null)
                                mustIntersect = edgeLinkInfo.IsObtuse;
                            else
                            {
                                var angleBetweenEdges = PlanarEdge.CalculateAngleBetween(edge1, edge2);
                                mustIntersect = angleBetweenEdges >= fPI;
                            }
                            
                            if (mustIntersect)
                            {
                                projection1.NeedsToBeClipped = true;
                                projection2.NeedsToBeClipped = true;
                            }

                            projection1.CombineWith = projection2;
                            projection2.CombineWith = projection1;
                            projectedEdges.Add(projection1);
                            projectedEdges.Add(projection2);
                        }
                    }
                    else if (projections.Count == 1)
                    {
                        var projection = projections[0];
                        
                        projectedEdges.Add(projection);

                        if (projection.NeedsToBeClipped)
                            projectedEdges.Add(CreateClippingEdge(projection, vert.Position, facePlane));
                    }
                    else if (projections.Count > 2)
                    {

                    }
                }

                if (projectedEdges.Any())
                {
                    int pairIdx = 0;
                    bool combineStandAlones = projectedEdges.Count() > 3;

                    while (pairIdx < 3 && projectedEdges.Any())
                    {
                        var projection = projectedEdges.OrderByDescending(x => x.CombineWith != null)
                            .FirstOrDefault();
                        projectedEdges.Remove(projection);

                        var nextEdge = projection.CombineWith;

                        //if (combineStandAlones && nextEdge == null)
                        //{
                        //    nextEdge = projectedEdges
                        //        .Where(x => !projection.NeedsToBeClipped && x.IntersectWith == null)
                        //        .FirstOrDefault();
                        //}

                        if (nextEdge != null)
                            projectedEdges.Remove(nextEdge);

                        SetOutlineCoords(triangle, pairIdx++, projection, nextEdge);
                        
                    }
                }
            }

            sw.Stop();
            Console.WriteLine($"Calculate Outlines => {sw.Elapsed}");
        }

        /// <summary>
        /// Generates the list of edges that the outline would intersect the triangle
        /// </summary>
        /// <param name="face"></param>
        /// <param name="hardEdgeDict"></param>
        List<PlanarEdge> CalculatePlanarEdges(Triangle face, HardEdgeDictionary hardEdgeDict)
        {
            var facePlane = new Plane(face.GetCenter(), face.Normal);

            var closeEdges = hardEdgeDict.GetEdgesForTriangle(face);

            var planarEdges = closeEdges.Select(x => new PlanarEdge(x, face, facePlane)).ToList();

            var edgePairs = new List<PlanarEdgePair>();

            //Link prev/next edges
            foreach (var planarEdge in planarEdges)
            {

                if (planarEdge.OrigEdge.PrevEdge != null)
                {
                    var prevEdge = planarEdges.FirstOrDefault(x => x.OrigEdge == planarEdge.OrigEdge.PrevEdge);
                    if (prevEdge != null)
                    {
                        planarEdge.PrevEdgeInfo = new PlanarEdgePair(planarEdge, prevEdge);
                        if (!edgePairs.Any(x => x.IsEqual(planarEdge.PrevEdgeInfo)))
                            edgePairs.Add(planarEdge.PrevEdgeInfo);
                    }
                }

                if (planarEdge.OrigEdge.NextEdge != null)
                {
                    var nextEdge = planarEdges.FirstOrDefault(x => x.OrigEdge == planarEdge.OrigEdge.NextEdge);
                    if (nextEdge != null)
                    {
                        planarEdge.NextEdgeInfo = new PlanarEdgePair(planarEdge, nextEdge);
                        if (!edgePairs.Any(x => x.IsEqual(planarEdge.NextEdgeInfo)))
                            edgePairs.Add(planarEdge.NextEdgeInfo);
                    }
                }
            }

            //check if the outline (offset from the edge) intersects the triangle
            var edgesToRemove = new List<PlanarEdge>();

            LineSegment2D CreateOutlineLine(PlanarEdge edge, Vector3 sharedPt, Vector3 axis)
            {
                var oppPt = edge.GetOppositeVertex(sharedPt);
                var p1 = sharedPt + edge.OutlineDirection * OutlineThickness;
                var p2 = oppPt + edge.OutlineDirection * OutlineThickness;
                var p1_2d = facePlane.ProjectPoint2D(axis, p1);
                var p2_2d = facePlane.ProjectPoint2D(axis, p2);
                return new LineSegment2D(p1_2d, p2_2d);
            }

            //Remove edges that the outline would not intersect with the triangle
            foreach (var edgePair in edgePairs)
            {
                if (edgePair.AngleDiff >= fPI * 0.998f)//dead end
                {
                    edgePair.Edge1.Disconnect(edgePair);
                    edgePair.Edge2.Disconnect(edgePair);
                    continue; 
                }

                var tri2d = new Triangle2D(face, facePlane, edgePair.OutlineBisector);
                var line1 = CreateOutlineLine(edgePair.Edge1, edgePair.CommonVertex, edgePair.OutlineBisector);
                var line2 = CreateOutlineLine(edgePair.Edge2, edgePair.CommonVertex, edgePair.OutlineBisector);
                Vector2 outlineInter = Vector2.Empty;

                if (Line.AreParallel(line1.Equation, line2.Equation))
                    continue;//colinear

                if (!line1.Equation.Intersect(line2.Equation, out outlineInter))
                    continue;

                line1.P1 = outlineInter;
                line2.P1 = outlineInter;

                bool isLine1Good = edgePair.Edge1.IsTriangleEdge || tri2d.IntersectsLine(line1);
                bool isLine2Good = edgePair.Edge2.IsTriangleEdge || tri2d.IntersectsLine(line2);

                if (!isLine1Good && !isLine2Good)
                {
                    //edgePair.Edge1.OutlineIsOutsideTriangle = true;
                    //edgesToRemove.Add(edgePair.Edge1);
                    //edgePair.Edge2.OutlineIsOutsideTriangle = true;
                    //edgesToRemove.Add(edgePair.Edge2);
                }
                else if (!isLine1Good)
                {
                    edgePair.Edge1.OutlineIsOutsideTriangle = true;
                    edgesToRemove.Add(edgePair.Edge1);
                }
                else if (!isLine2Good)
                {
                    edgePair.Edge2.OutlineIsOutsideTriangle = true;
                    edgesToRemove.Add(edgePair.Edge2);
                }
            }

            planarEdges.RemoveAll(e => edgesToRemove.Contains(e));

            //Unlink edges that were removed
            foreach (var pEdge in planarEdges)
            {
                if (pEdge.PrevEdgeInfo != null)
                {
                    var otherEdge = pEdge.PrevEdgeInfo.GetOtherEdge(pEdge);
                    if (!planarEdges.Contains(otherEdge))
                        pEdge.PrevEdgeInfo = null;
                }

                if (pEdge.NextEdgeInfo != null)
                {
                    var otherEdge = pEdge.NextEdgeInfo.GetOtherEdge(pEdge);
                    if (!planarEdges.Contains(otherEdge))
                        pEdge.NextEdgeInfo = null;
                }
            }

            return planarEdges;
        }

        ProjectedEdge ProjectTriangle(PlanarEdge planarEdge, Triangle triangle, Vector3 vertex)
        {
            var plane = new Plane(vertex, triangle.Normal);

            var edgeP1 = plane.ProjectPoint2D(planarEdge.Direction, planarEdge.P1);
            var edgeP2 = plane.ProjectPoint2D(planarEdge.Direction, planarEdge.P2);

            var edge = new ProjectedEdge()
            {
                PlanarEdge = planarEdge,
                TargetVertex = vertex,
                P1 = plane.ProjectPoint2D(planarEdge.Direction, triangle.V1.Position),
                P2 = plane.ProjectPoint2D(planarEdge.Direction, triangle.V2.Position),
                P3 = plane.ProjectPoint2D(planarEdge.Direction, triangle.V3.Position),
                MinX = Math.Min(edgeP1.X, edgeP2.X),
                MaxX = Math.Max(edgeP1.X, edgeP2.X),
            };

            if (planarEdge.OrigEdge != null)//prevents errors from temp objects
                edge.ValidateValues(OutlineThickness);

            return edge;
        }

        ProjectedEdge CreateClippingEdge(ProjectedEdge baseEdge, Vector3 edgeEnd, Plane facePlane)
        {
            var triangle = baseEdge.PlanarEdge.Face;
            var oppVert = baseEdge.PlanarEdge.GetOppositeVertex(edgeEnd);
            var outlineDir = (edgeEnd - oppVert).Normalized();
            
            var interEdge = new HardEdge(
                edgeEnd,
                edgeEnd + baseEdge.PlanarEdge.OutlineDirection,
                facePlane.Normal,
                outlineDir);

            interEdge.CorrectOrder();
            var planarEndEdge = new PlanarEdge(interEdge, triangle, facePlane);
            var interProjected = ProjectTriangle(planarEndEdge, triangle, edgeEnd);
            interProjected.NeedsToBeClipped = true;

            baseEdge.CombineWith = interProjected;
            interProjected.CombineWith = baseEdge;

            return interProjected;
        }

        void SetOutlineCoords(Triangle triangle, int coordIdx, ProjectedEdge e1, ProjectedEdge e2)
        {
            var mode = (e1.NeedsToBeClipped || (e2?.NeedsToBeClipped ?? false)) ?
                RoundEdgeData.EdgeCombineMode.Intersection : RoundEdgeData.EdgeCombineMode.Union;
            if (e2 == null && mode == RoundEdgeData.EdgeCombineMode.Intersection)
                mode = RoundEdgeData.EdgeCombineMode.Union;
            //if (e2 == null && mode == RoundEdgeData.EdgeCombineMode.Union)
            //    mode = RoundEdgeData.EdgeCombineMode.Intersection;

            e1.AdjustValues(ThicknessMultiplier);

            if (e2 != null)
                e2.AdjustValues(ThicknessMultiplier);

            for (int i = 0; i < 3; i++)
            {
                var uv1 = e1.UVs[i];
                var uv2 = e2 != null ? e2.UVs[i] : RoundEdgeData.EmptyCoord;

                triangle.Indices[i].RoundEdgeData.SetCoordsPair(coordIdx, uv2, uv1, mode);
            }
        }

        private static List<HardEdge> CalculateHardEdges(List<Triangle> triangleList, float breakAngle)
        {
            var sharedEdges = new Dictionary<SimpleEdge, List<Triangle>>();

            foreach (var triangle in triangleList)
            {
                for (int i = 0; i < 3; i++)
                {
                    var simpleEdge = new SimpleEdge(triangle.Edges[i]);

                    if (simpleEdge.P1.Equals(simpleEdge.P2))
                        continue;

                    if (!sharedEdges.ContainsKey(simpleEdge))
                        sharedEdges.Add(simpleEdge, new List<Triangle>());
                    sharedEdges[simpleEdge].Add(triangle);
                }
            }

            var hardEdges = new List<HardEdge>();
            //var tp1 = new Vector3(1.48f, 3.50971f, -0.8f);
            //var tp2 = new Vector3(1.29029f, 3.32f, -0.8f);
            foreach (var kv in sharedEdges)
            {
                //if (kv.Key.Contains(tp1) && kv.Key.Contains(tp2))
                //{

                //}
                if (kv.Value.Count == 1)
                {
                    hardEdges.Add(new HardEdge(kv.Key, kv.Value[0]));
                    continue;
                }

                var faceEdges = kv.Value.Select(x => new FaceEdge(kv.Key, x)).ToList();

                foreach (var faceEdge in faceEdges)
                {
                    var hasCloseFace = faceEdges
                        .Any(x => x != faceEdge && x.GetNormalDiff(faceEdge) < breakAngle);

                    if (!hasCloseFace)
                        hardEdges.Add(new HardEdge(kv.Key, faceEdge.Face));
                }
            }

            hardEdges.ForEach(x => x.CorrectOrder());
            return hardEdges;
        }
    }
}
