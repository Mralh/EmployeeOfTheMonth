using System;
using System.Collections.Generic;
using UnityEngine;
using InternalRealtimeCSG;
using UnityEditor;

namespace RealtimeCSG
{
    internal partial class ControlMeshState
    {
        // selection

        [SerializeField] public SelectState[]   PointSelectState;
        [SerializeField] public SelectState[]   EdgeSelectState;
        [SerializeField] public SelectState[]   PolygonSelectState;

        // backup stuff

        [SerializeField] public SelectState[]   BackupPointSelectState;
        [SerializeField] public SelectState[]   BackupEdgeSelectState;
        [SerializeField] public SelectState[]   BackupPolygonSelectState;

        public void BackupSelection()
        {
            if (PointSelectState != null)
            {
                BackupPointSelectState = new SelectState[PointSelectState.Length];
                Array.Copy(PointSelectState, BackupPointSelectState, PointSelectState.Length);
            }

            if (EdgeSelectState != null)
            {
                BackupEdgeSelectState = new SelectState[EdgeSelectState.Length];
                Array.Copy(EdgeSelectState, BackupEdgeSelectState, EdgeSelectState.Length);
            }

            if (PolygonSelectState != null)
            {
                BackupPolygonSelectState = new SelectState[PolygonSelectState.Length];
                Array.Copy(PolygonSelectState, BackupPolygonSelectState, PolygonSelectState.Length);
            }
        }

        public void RevertSelection()
        {
            if (BackupPointSelectState != null && PointSelectState != null &&
                BackupPointSelectState.Length == PointSelectState.Length)
                Array.Copy(BackupPointSelectState, PointSelectState, PointSelectState.Length);

            if (BackupEdgeSelectState != null && EdgeSelectState != null &&
                BackupEdgeSelectState.Length == EdgeSelectState.Length)
                Array.Copy(BackupEdgeSelectState, EdgeSelectState, EdgeSelectState.Length);

            if (BackupPolygonSelectState != null && PolygonSelectState != null &&
                BackupPolygonSelectState.Length == PolygonSelectState.Length)
                Array.Copy(BackupPolygonSelectState, PolygonSelectState, PolygonSelectState.Length);

            DestroySelectionBackup();
        }

        public void DestroySelectionBackup()
        {
            BackupPointSelectState = null;
            BackupEdgeSelectState = null;
            BackupPolygonSelectState = null;
        }

        public bool HasSelectionChanged()
        {
            if (PointSelectState != null)
            {
                for (var p = 0; p < PointSelectState.Length; p++)
                {
                    if (PointSelectState[p] != BackupPointSelectState[p])
                        return true;
                }
            }

            if (BackupEdgeSelectState != null)
            {
                for (var e = 0; e < EdgeSelectState.Length; e++)
                {
                    if (EdgeSelectState[e] != BackupEdgeSelectState[e])
                        return true;
                }
            }

            if (BackupPolygonSelectState != null)
            {
                for (var e = 0; e < PolygonSelectState.Length; e++)
                {
                    if (PolygonSelectState[e] != BackupPolygonSelectState[e])
                        return true;
                }
            }
            return false;
        }

        public float GetClosestEdgeDistance(CSGPlane cameraPlane, int pointIndex0, int pointIndex1)
        {
            if (pointIndex0 < 0 || pointIndex0 >= WorldPoints.Length ||
                pointIndex1 < 0 || pointIndex1 >= WorldPoints.Length)
                return float.PositiveInfinity;

            var point0 = WorldPoints[pointIndex0];
            var point1 = WorldPoints[pointIndex1];

            var distance = GUIStyleUtility.DistanceToLine(cameraPlane, point0, point1) * 3.0f;
            var minDistance = distance;
            if (!(Mathf.Abs(minDistance) < 4.0f))
                return minDistance;

            var surfaceIndex1 = EdgeSurfaces[pointIndex0];
            var surfaceIndex2 = EdgeSurfaces[pointIndex1];
            
            for (var p = 0; p < PolygonCenterPoints.Length; p++)
            {
                if (p != surfaceIndex1 &&
                    p != surfaceIndex2)
                    continue;

                var polygonCenterPoint          = PolygonCenterPoints[p];
                var polygonCenterPointOnLine    = GeometryUtility.ProjectPointOnInfiniteLine(PolygonCenterPoints[p], point0, (point1 - point0).normalized);
                var direction                   = (polygonCenterPointOnLine - polygonCenterPoint).normalized;

                var nudgedPoint0 = point0 - (direction * 0.05f);
                var nudgedPoint1 = point1 - (direction * 0.05f);

                var otherDistance = GUIStyleUtility.DistanceToLine(cameraPlane, nudgedPoint0, nudgedPoint1);
                if (otherDistance < minDistance)
                {
                    minDistance = otherDistance;
                }
            }

            return minDistance;
        }


        public HashSet<short> GetSelectedPointIndices()
        {
            var indices = new HashSet<short>();

            indices.Clear();
            for (var p = 0; p < PointSelectState.Length; p++)
            {
                if ((PointSelectState[p] & SelectState.Selected) != SelectState.Selected)
                    continue;
                indices.Add((short)p);
            }
            for (var e = 0; e < EdgeSelectState.Length; e++)
            {
                if ((EdgeSelectState[e] & SelectState.Selected) != SelectState.Selected)
                    continue;
                
                indices.Add((short)Edges[(e * 2) + 0]);
                indices.Add((short)Edges[(e * 2) + 1]);
            }
            for (var p = 0; p < PolygonSelectState.Length; p++)
            {
                if ((PolygonSelectState[p] & SelectState.Selected) != SelectState.Selected)
                    continue;

                var pointIndices = PolygonPointIndices[p];
                for (var i = 0; i < pointIndices.Length; i++)
                {
                    indices.Add((short)pointIndices[i]);
                }
            }

            return indices;
        }
    }
}
