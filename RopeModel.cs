namespace Tiler.Editor;

using System;
using System.Numerics;
using System.Collections.Generic;
using System.Linq;

using static Raylib_cs.Raymath;

//--------------------------------------------------------------
//
// TODO: Clean this mess
//
//--------------------------------------------------------------

public class RopeProperties(
    int segmentLength,
    int collisionDepth,
    float segmentRadius,
    float gravity,
    float friction,
    float airFriction,
    bool stiff,
    float edgeDirection,
    float rigid,
    float selfPush,
    float sourcePush
) {
    public int SegmentLength = segmentLength;
    public int CollisionDepth = collisionDepth;
    public float SegmentRadius = segmentRadius;
    public float Gravity = gravity;
    public float Friction = friction;
    public float AirFriction = airFriction;
    public bool Stiff = stiff;
    public float EdgeDirection = edgeDirection;
    public float Rigid = rigid;
    public float SelfPush = selfPush;
    public float SourcePush = sourcePush;
}

public class RopeModel
{
    public Prop Prop;
    public Level Level;
    public RopeProperties Properties;
    public Vector2[] Segments;
    public int SegmentCount { get; private set; }

    public Vector2[] BezierHandles { get; set; }

    public enum EditTypes { Simulation, BezierPaths }
    public EditTypes EditType { get; set; }
    
    public enum RopeRelease { Left, None, Right }

    public RopeRelease Release = RopeRelease.None;

    public bool Gravity;

    internal void UpdateSegments(Vector2[] segments) {
        var oldLength = Segments.Length;
        var newLength = segments.Length;

        var deficit = newLength - oldLength;

        if (deficit == 0) return;

        var newVelocities = new Vector2[newLength];
        var newLastPositions = new Vector2[newLength];
        
        if (deficit > 0) {
            for (var i = 0; i < oldLength; i++) {
                newVelocities[i] = segmentVelocities[i];
                newLastPositions[i] = Segments[i];
            }

            for (var k = oldLength; k < newLength; k++) {
                newVelocities[k] = new Vector2(0, 0);
                newLastPositions[k] = segments[k];
            }
        } else {
            for (var j = 0; j < newLength; j ++) {
                newVelocities[j] = segmentVelocities[j];
                newLastPositions[j] = Segments[j];
            }
        }

        SegmentCount = newLength;

        Segments = segments;
        segmentVelocities = newVelocities;
        lastPositions = newLastPositions;
    }

    private Vector2[] segmentVelocities;
    private Vector2[] lastPositions;
    private readonly short[] rigidityArray = [-2, 2, -3, 3, -4, 4];
    private readonly (short, short)[] pushList =
    [
        (0, 0), (-1, 0), (-1, -1), (0, -1), (1, -1), (1, 0), (1, 1), (0, 1), (1, 1), (-1, 1)
    ];

    public RopeModel(Level level, Prop prop, RopeProperties properties, IEnumerable<Vector2> segments)
    {
        Level = level;
        Prop = prop;
        Properties = properties;
        
        Segments = [..segments];

        List<Vector2> velocities = [];
        List<Vector2> lastPositions = [];

        lastPositions = [..Segments];
        velocities = Segments.Select(_ => Vector2.Zero).ToList();

        segmentVelocities = [..velocities];
        this.lastPositions = [..lastPositions];

        EditType = EditTypes.Simulation;
        BezierHandles = [];
        Gravity = true;
    }

    public void ResetBezierHandles() {
        var quad = Prop.Quad;
        
        BezierHandles = [ quad.Center ];
    }

    private static (Vector2 a, Vector2 b) GetRopeEnds(Quad quad) => (
        (quad.TopLeft + quad.TopRight) / 2.0f, 
        (quad.TopRight + quad.BottomRight) / 2.0f
        );
    
    public void Reset(Quad quad)
    {
        var (pointA, pointB) = GetRopeEnds(quad);
    
        var distance = Vector2Distance(pointA, pointB);
        
        // var segmentCount = distance / Init.SegmentLength;

        if (SegmentCount < 3) SegmentCount = 3;

        var step = distance / Properties.SegmentLength;

        List<Vector2> newPoints = [];
        List<Vector2> newLastPositions = [];
        
        for (var i = 0; i < SegmentCount; i++)
        {
            var mv = MoveToPoint(pointA, pointB, (i - 0.5f) * step);
            
            newPoints.Add(pointA + mv); 
            newLastPositions.Add(pointA + mv);
        }

        var newVelocities = new Vector2[newPoints.Count];

        Segments = [..newPoints];
        segmentVelocities = newVelocities;
        lastPositions = [..newLastPositions];
    }

    // ------------------------------------------------------------------------
    //
    //  The following code was copied - and modified, with permission, from
    //  https://github.com/pkhead/rained/blob/main/src/Rained/RopeModel.cs#L183
    //
    // ------------------------------------------------------------------------

    private struct Segment
    {
        public Vector2 Pos;
        public Vector2 LastPos;
        public Vector2 Vel;
    }

    private struct RopePoint
    {
        public Vector2 Loc;
        public Vector2 LastLoc;
        public Vector2 Frc;
        public Vector2 SizePnt;
    }

    private static Vector2 MoveToPoint(Vector2 a, Vector2 b, float t)
    {
        var diff = b - a;
        if (diff.LengthSquared() == 0) return Vector2.UnitY * t;
        return Vector2.Normalize(diff) * t;
    }

    // simplification of a specialized version of MoveToPoint where t = 1
    private static Vector2 Direction(Vector2 from, Vector2 to)
    {
        if (to == from) return Vector2.UnitY; // why is MoveToPoint defined like this??
        return Vector2.Normalize(to - from);
    }

    private static Vector2 GiveGridPos(Vector2 pos)
    {
        /*return new Vector2(
            MathF.Floor((pos.X / 20f) + 0.4999f),
            MathF.Floor((pos.Y / 20f) + 0.4999f)
        );*/
        return new Vector2(
            MathF.Floor(pos.X / 20f + 1f),
            MathF.Floor(pos.Y / 20f + 1f)
        );
    }

    private static Vector2 GiveMiddleOfTile(Vector2 pos)
    {
        return new Vector2(
            (pos.X * 20f) - 10f,
            (pos.Y * 20f) - 10f
        );
    }

    private static float Lerp(float A, float B, float val)
    {
        val = Math.Clamp(val, 0f, 1f);
        if (B < A)
        {
            (B, A) = (A, B);
            val = 1f - val;
        }
        return Math.Clamp(A + (B-A)*val, A, B);
    }

    private static bool DiagWI(Vector2 point1, Vector2 point2, float dig)
    {
        var rectHeight = MathF.Abs(point1.Y - point2.Y);
        var rectWidth = MathF.Abs(point1.X - point2.X);
        return (rectHeight * rectHeight) + (rectWidth * rectWidth) < dig*dig;
    }

    // wtf is this name?
    private int AfaMvLvlEdit(Vector2 p, int layer)
    {
        var x = (int)p.X - 1;
        var y = (int)p.Y - 1;
        
        var level = Level.Geos;
        
        if (x >= 0 && x < level.Width && y >= 0 && y < level.Height)
            return (int)level[y, x, layer];

        return 1;
    }

    public void Update(Quad quad, int layer)
    {
        if (SegmentCount < 3) return;
        
        var (posA, posB) = GetRopeEnds(quad);
        var segments = Segments;

        if (Properties.EdgeDirection > 0f)
        {
            var dir = Direction(posA, posB);
            if (Release != RopeRelease.Left)
            {
                // WARNING - indexing
                for (var A = 0; A <= segments.Length / 2f - 1; A++)
                {
                    var fac = 1f - A / (segments.Length / 2f);
                    fac *= fac;

                    segmentVelocities[A] += dir*fac*Properties.EdgeDirection;
                }

                var idealFirstPos = posA + dir * Properties.SegmentLength;
                segments[0] = new Vector2(
                    Lerp(segments[0].X, idealFirstPos.X, Properties.EdgeDirection),
                    Lerp(segments[0].Y, idealFirstPos.Y, Properties.EdgeDirection)
                );
            }

            if (Release != RopeRelease.Right)
            {
                // WARNING - indexing
                for (var A1 = 0; A1 <= segments.Length / 2f - 1; A1++)
                {
                    var fac = 1f - A1 / (segments.Length / 2f);
                    fac *= fac;
                    var A = segments.Length + 1 - (A1+1) - 1;
                    segmentVelocities[A] -= dir*fac*Properties.EdgeDirection;
                }

                var idealFirstPos = posB - dir * Properties.SegmentLength;
                segments[^1] = new Vector2(
                    Lerp(segments[^1].X, idealFirstPos.X, Properties.EdgeDirection),
                    Lerp(segments[^1].Y, idealFirstPos.Y, Properties.EdgeDirection)
                );
            }
        }

        if (Release != RopeRelease.Left)
        {
            segments[0] = posA;
            segmentVelocities[0] = Vector2.Zero;
        }

        if (Release != RopeRelease.Right)
        {
            segments[^1] = posB;
            segmentVelocities[^1] = Vector2.Zero;
        }

        for (var i = 0; i < segments.Length; i++)
        {
            lastPositions[i] = segments[i];
            segments[i] += segmentVelocities[i];
            segmentVelocities[i] *= Properties.AirFriction;
            if (Gravity) segmentVelocities[i].Y += Properties.Gravity;
        }

        for (var i = 1; i < segments.Length; i++)
        {
            ConnectRopePoints(i, i-1);
            if (Properties.Rigid > 0)
                ApplyRigidity(i);
        }

        for (var i = 2; i <= segments.Length; i++)
        {
            var a = segments.Length - i + 1;
            ConnectRopePoints(a-1, a);
            
            if (Properties.Rigid > 0)
                ApplyRigidity(i-1);
        }

        if (Properties.SelfPush > 0)
        {
            for (var A = 0; A < segments.Length; A++)
            {
                for (var B = 0; B < segments.Length; B++)
                {
                    if (A != B && DiagWI(segments[A], segments[B], Properties.SelfPush))
                    {
                        var dir = Direction(segments[A], segments[B]);
                        var dist = Vector2.Distance(segments[A], segments[B]);
                        var mov = dir * (dist - Properties.SelfPush);

                        segments[A] += mov * 0.5f;
                        segmentVelocities[A] += mov * 0.5f;
                        segments[B] -= mov * 0.5f;
                        segmentVelocities[B] -= mov * 0.5f;
                    }
                }
            }
        }

        if (Properties.SourcePush > 0)
        {
            for (var A = 0; A < segments.Length; A++)
            {
                segmentVelocities[A] += MoveToPoint(posA, segments[A], Properties.SourcePush) * Math.Clamp((A / (segments.Length - 1f)) - 0.7f, 0f, 1f);
                segmentVelocities[A] += MoveToPoint(posB, segments[A], Properties.SourcePush) * Math.Clamp((1f - (A / (segments.Length - 1f))) - 0.7f, 0f, 1f);

            }
        }

        for (var i = 1 + (Release != RopeRelease.Left ? 1:0); i <= segments.Length - (Release != RopeRelease.Right ? 1:0); i++)
        {
            PushRopePointOutOfTerrain(i-1, layer);
        }

        /*
        if(preview)then
            member("ropePreview").image.copyPixels(member("pxl").image,  member("ropePreview").image.rect, rect(0,0,1,1), {#color:color(255, 255, 255)})
            repeat with i = 1 to ropeModel.segments.count then
                adaptedPos = me.SmoothedPos(i)
                adaptedPos = adaptedPos - cameraPos*20.0
                adaptedPos = adaptedPos * previewScale
                member("ropePreview").image.copyPixels(member("pxl").image, rect(adaptedPos-point(1,1), adaptedPos+point(2,2)), rect(0,0,1,1), {#color:color(0, 0, 0)})
            end repeat
        end if
        */
    }

    private void ConnectRopePoints(int A, int B)
    {
        var segments = Segments;

        var dir = Direction(segments[A], segments[B]);
        var dist = Vector2.Distance(segments[A], segments[B]);

        if (Properties.Stiff || dist > Properties.SegmentLength)
        {
            var mov = dir * (dist - Properties.SegmentLength);

            segments[A] += mov * 0.5f;
            segmentVelocities[A] += mov * 0.5f;
            segments[B] -= mov * 0.5f;
            segmentVelocities[B] -= mov * 0.5f;
        }
    }

    private void ApplyRigidity(int A)
    {
        var segments = Segments;

        void func(int B2)
        {
            var B = A+1 + B2;
            if (B > 0 && B <= segments.Length)
            {
                var dir = Direction(segments[A], segments[B-1]);
                segmentVelocities[A] -= (dir * Properties.Rigid * Properties.SegmentLength)
                    / (Vector2.Distance(segments[A], segments[B-1]) + 0.1f + MathF.Abs(B2));
                segmentVelocities[B-1] += (dir * Properties.Rigid * Properties.SegmentLength)
                    / (Vector2.Distance(segments[A], segments[B-1]) + 0.1f + MathF.Abs(B2)); 
            }
        };

        func(-2);
        func(2);
        func(-3);
        func(3);
        func(-4);
        func(4);
    }

    private Vector2 SmoothPos(Quad quad, int A)
    {
        var (posA, posB) = GetRopeEnds(quad);
        var segments = Segments;

        if (A == 0)
        {
            if (Release != RopeRelease.Left)
                return posA;
            else
                return segments[A];
        }
        else if (A == segments.Length - 1)
        {
            if (Release != RopeRelease.Right)
                return posB;
            else
                return segments[A];
        }
        else
        {
            var smoothpos = (segments[A-1] + segments[A+1]) / 2f;
            return (segments[A] + smoothpos) / 2f;
        }
    }

    // not in the lingo source code
    private Vector2 SmoothPosOld(Quad quad, int A)
    {
        var (posA, posB) = GetRopeEnds(quad);
        var segments = Segments;

        if (A == 0)
        {
            if (Release != RopeRelease.Left)
                return posA;
            else
                return lastPositions[A];
        }
        else if (A == segments.Length - 1)
        {
            if (Release != RopeRelease.Right)
                return posB;
            else
                return lastPositions[A];
        }
        else
        {
            var smoothpos = (lastPositions[A-1] + lastPositions[A+1]) / 2f;
            return (lastPositions[A] + smoothpos) / 2f;
        }
    }

    private void PushRopePointOutOfTerrain(int A, int layer)
    {
        var segments = Segments;

        var p = new RopePoint
        {
            Loc = segments[A],
            LastLoc = lastPositions[A],
            Frc = segmentVelocities[A],
            SizePnt = Vector2.One * Properties.SegmentRadius
        };

        p = SharedCheckVCollision(p, Properties.Friction, layer);
        segments[A] = p.Loc;
        segmentVelocities[A] = p.Frc;

        var gridPos = GiveGridPos(segments[A]);
        
        loopFunc(new Vector2(0f, 0f));
        loopFunc(new Vector2(-1f, 0f));
        loopFunc(new Vector2(-1f, -1f));
        loopFunc(new Vector2(0f, -1));
        loopFunc(new Vector2(1f, -1));
        loopFunc(new Vector2(1f, 0f));
        loopFunc(new Vector2(1f, 1f));
        loopFunc(new Vector2(0f, 1f));
        loopFunc(new Vector2(-1f, 1f));

        void loopFunc(Vector2 dir)
        {
            if (AfaMvLvlEdit(gridPos+dir, layer) == 1)
            {
                var midPos = GiveMiddleOfTile(gridPos + dir);
                var terrainPos = new Vector2(
                    Math.Clamp(segments[A].X, midPos.X-10f, midPos.X+10f),
                    Math.Clamp(segments[A].Y, midPos.Y-10f, midPos.Y+10f)
                );
                terrainPos = ((terrainPos * 10f) + midPos) / 11f;

                var dir2 = Direction(segments[A], terrainPos);
                var dist = Vector2.Distance(segments[A], terrainPos);
                if (dist < Properties.SegmentRadius)
                {
                    var mov = dir2 * (dist-Properties.SegmentRadius);
                    segments[A] += mov;
                    segmentVelocities[A] += mov;
                }
            }
        }
    }

    private RopePoint SharedCheckVCollision(RopePoint p, float friction, int layer)
    {
        var bounce = 0f;

        if (p.Frc.Y > 0f)
        {
            var lastGridPos = GiveGridPos(p.LastLoc);
            var feetPos = GiveGridPos(p.Loc + new Vector2(0f, p.SizePnt.Y + 0.01f));
            var lastFeetPos = GiveGridPos(p.LastLoc + new Vector2(0f, p.SizePnt.Y));
            var leftPos = GiveGridPos(p.Loc + new Vector2(-p.SizePnt.X + 1f, p.SizePnt.Y + 0.01f));
            var rightPos = GiveGridPos(p.Loc + new Vector2(p.SizePnt.X - 1f, p.SizePnt.Y + 0.01f));

            // WARNING - idk if lingo calculate the loop direction
            for (int q = (int)lastFeetPos.Y; q <= feetPos.Y; q++)
            {
                for (int c = (int)leftPos.X; c <= rightPos.X; c++)
                {
                    if (AfaMvLvlEdit(new(c, q), layer) == 1 && AfaMvLvlEdit(new Vector2(c, q-1f), layer) != 1)
                    {
                        if (lastGridPos.Y >= q && AfaMvLvlEdit(lastGridPos, layer) == 1)
                        {}
                        else
                        {
                            p.Loc.Y = ((q-1f)*20f) - p.SizePnt.Y;
                            p.Frc.X *= friction;
                            p.Frc.Y = -p.Frc.Y * bounce;
                            return p;
                        }
                    }
                }
            }
        }
        else if (p.Frc.Y < 0f)
        {
            var lastGridPos = GiveGridPos(p.LastLoc);
            var headPos = GiveGridPos(p.Loc - new Vector2(0f, p.SizePnt.Y + 0.01f));
            var lastHeadPos = GiveGridPos(p.LastLoc - new Vector2(0, p.SizePnt.Y));
            var leftPos = GiveGridPos(p.Loc + new Vector2(-p.SizePnt.X + 1f, p.SizePnt.Y + 0.01f));
            var rightPos = GiveGridPos(p.Loc + new Vector2(p.SizePnt.X - 1f, p.SizePnt.Y + 0.01f));

            // WARNING - idk if lingo calculates the loop direction
            for (int d = (int)headPos.Y; d <= lastHeadPos.Y; d++)
            {
                var q = lastHeadPos.Y - (d-headPos.Y);
                for (int c = (int)leftPos.X; c <= rightPos.X; c++)
                {
                    if (AfaMvLvlEdit(new(c, q), layer) == 1 && AfaMvLvlEdit(new(c, q+1f), layer) != 1)
                    {
                        if (lastGridPos.Y <= q && AfaMvLvlEdit(lastGridPos, layer) != 1)
                        {}
                        else
                        {
                            p.Loc.Y = (q*20f)+p.SizePnt.Y;
                            p.Frc.X *= friction;
                            p.Frc.Y = -p.Frc.Y * bounce;
                            return p;
                        }
                    }
                }
            }
        }

        return p;
    }
}
