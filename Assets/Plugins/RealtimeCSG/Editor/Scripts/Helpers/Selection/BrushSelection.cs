using System;
using System.Collections.Generic;
using System.Linq;
using RealtimeCSG;
using UnityEngine;
using UnityEditor;

namespace InternalRealtimeCSG
{
    internal class BrushSelection
    {
        [SerializeField] public CSGBrush[]              Brushes         = new CSGBrush[0]; 
        [SerializeField] public Shape[]                 Shapes          = new Shape[0]; 
        [SerializeField] public ControlMesh[]           ControlMeshes   = new ControlMesh[0]; 
        [SerializeField] public ControlMeshState[]      States          = new ControlMeshState[0]; 

        [SerializeField] public ControlMesh[]           BackupControlMeshes = new ControlMesh[0]; 
        [SerializeField] public Matrix4x4[]             LocalToWorld        = new Matrix4x4[0]; 
        [SerializeField] public Transform[]             ModelTransforms     = new Transform[0];


        private bool HavePointSelection
        {
            get
            {
                for (var t = 0; t < Brushes.Length; t++)
                {
                    if (States[t].HavePointSelection)
                        return true;
                }
                return false;
            }
        }

        private bool HaveEdgeSelection
        {
            get
            {
                for (var t = 0; t < Brushes.Length; t++)
                    if (States[t].HaveEdgeSelection)
                        return true;
                return false;
            }
        }



        public void Select(HashSet<CSGBrush> foundBrushes)
        {
            if (Brushes == null || Brushes.Length == 0)
            {
                Brushes             = foundBrushes.ToArray();
                Shapes              = new Shape[Brushes.Length];
                ControlMeshes       = new ControlMesh[Brushes.Length];
                LocalToWorld        = new Matrix4x4[Brushes.Length];
                BackupControlMeshes = new ControlMesh[Brushes.Length];
                States              = new ControlMeshState[Brushes.Length];
                ModelTransforms     = new Transform[Brushes.Length];

                for (var i = 0; i < foundBrushes.Count; i++)
                    LocalToWorld[i] = MathConstants.identityMatrix;
            } else
            {
                // remove brushes that are no longer selected
                for (var i = Brushes.Length - 1; i >= 0; i--)
                {
                    if (foundBrushes.Contains(Brushes[i]))
                        continue;

                    ArrayUtility.RemoveAt(ref Brushes, i);
                    ArrayUtility.RemoveAt(ref Shapes, i);
                    ArrayUtility.RemoveAt(ref ControlMeshes, i);
                    ArrayUtility.RemoveAt(ref LocalToWorld, i);
                    ArrayUtility.RemoveAt(ref BackupControlMeshes, i);
                    ArrayUtility.RemoveAt(ref States, i);
                    ArrayUtility.RemoveAt(ref ModelTransforms, i);
                }

                // add new brushes that are added to the selection
                foreach (var newBrush in foundBrushes)
                {
                    if (Brushes.Contains(newBrush))
                        continue;

                    ArrayUtility.Add(ref Brushes, newBrush);
                    ArrayUtility.Add(ref Shapes, null);
                    ArrayUtility.Add(ref ControlMeshes, null);
                    ArrayUtility.Add(ref LocalToWorld, MathConstants.identityMatrix);
                    ArrayUtility.Add(ref BackupControlMeshes, null);
                    ArrayUtility.Add(ref States, null);
                    ArrayUtility.Add(ref ModelTransforms, null);
                }
            }
        }

        public void ResetSelection()
        {
            if (Brushes != null && States != null)
            {
                for (var i = 0; i < Brushes.Length; i++)
                {
                    States[i] = null;
                }
            }
        }

        public void UpdateTargets()
        {
            for (var i = 0; i < Brushes.Length; i++)
            {
                if (Brushes[i].ControlMesh == null)
                    continue;

                if (!Brushes[i].ControlMesh.IsValid)
                    Brushes[i].ControlMesh.IsValid = ControlMeshUtility.Validate(Brushes[i].ControlMesh, Brushes[i].Shape);
            
                LocalToWorld[i] = Brushes[i].transform.localToWorldMatrix;

                if (States[i] != null)
                    continue;

                States[i] = new ControlMeshState(Brushes[i]);
                if (Brushes[i].ControlMesh != null)
                    BackupControlMeshes[i] = Brushes[i].ControlMesh.Clone();

                Shapes[i]          = (Brushes[i].Shape == null) ? null : Brushes[i].Shape.Clone();
                ControlMeshes[i]   = BackupControlMeshes[i];
            }
            UpdateParentModelTransforms();
        }

        public void UpdateParentModelTransforms()
        {
            for (var i = 0; i < Brushes.Length; i++)
            {
                if (ModelTransforms[i] != null)
                    continue;

                var brushCache = InternalCSGModelManager.GetBrushCache(Brushes[i]);
                if (brushCache == null ||
                    brushCache.childData == null ||
                    brushCache.childData.ModelTransform == null)
                    continue;

                ModelTransforms[i] = brushCache.childData.ModelTransform;
            }
        }


        public void BackupSelection()
        {
            for (var t = 0; t < States.Length; t++)
            {
                if (States[t] == null)
                    continue;
                
                States[t].BackupSelection();
            }
        }

        public void RevertSelection()
        {
            for (var t = 0; t < States.Length; t++)
            {
                if (States[t] == null)
                    continue;
                
                States[t].RevertSelection();
            }
        }
        
        public bool HasSelectionChanged()
        {
            for (var t = 0; t < States.Length; t++)
            {
                if (States[t] == null)
                    continue;

                if (States[t].HasSelectionChanged())
                    return true;
            }
            return false;
        }

        public void UnHoverAll()
        {
            for (var t = 0; t < States.Length; t++)
            {
                if (States[t] == null)
                    continue;

                States[t].UnHoverAll();
            }
        }

        public void UndoRedoPerformed()
        {
            for (var t = 0; t < States.Length; t++)
            {
                if (States[t] == null ||
                    !Brushes[t])
                    continue;

                var controlMesh = Brushes[t].ControlMesh;
                var state = States[t];
                state.UpdatePoints(controlMesh);
            }
        }

        public bool UpdateWorkControlMesh(bool forceUpdate = false)
        {
            for (var t = Brushes.Length - 1; t >= 0; t--)
            {
                if (!Brushes[t])
                {
                    ArrayUtility.RemoveAt(ref Brushes, t);
                    continue;
                }

                if (!forceUpdate &&
                    ControlMeshes[t] != null &&
                    !ControlMeshes[t].IsValid)
                    continue;

                Shapes[t] = Brushes[t].Shape.Clone();
                ControlMeshes[t] = Brushes[t].ControlMesh.Clone();
                BackupControlMeshes[t] = ControlMeshes[t];
            }

            for (var i = 0; i < Brushes.Length; i++)
            {
                LocalToWorld[i] = Brushes[i].transform.localToWorldMatrix;
            }
            return true;
        }

        public void FindClosestIntersection(out int closestBrushIndex, out int closestSurfaceIndex)
        {
            var mouseWorldRay = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
            var rayStart = mouseWorldRay.origin;
            var rayVector = (mouseWorldRay.direction * (Camera.current.farClipPlane - Camera.current.nearClipPlane));
            var rayEnd = rayStart + rayVector;

            var minDistance = float.PositiveInfinity;
            closestBrushIndex = -1;
            closestSurfaceIndex = -1;
            for (var t = 0; t < Brushes.Length; t++)
            {
                var brush = Brushes[t];
                if (!Brushes[t] || !Brushes[t].isActiveAndEnabled)
                    continue;

                var parentModelTransform = ModelTransforms[t];
                if (parentModelTransform == null)
                    continue;

                var modelTranslation = parentModelTransform.position;

                BrushIntersection intersection;
                if (!SceneQueryUtility.FindBrushIntersection(brush, modelTranslation, rayStart, rayEnd, out intersection, forceUseInvisible: true))
                    continue;
                
                var distance = (intersection.worldIntersection - rayStart).magnitude;
                if (distance > minDistance)
                    continue;
                
                minDistance = distance;
                closestBrushIndex = t;
                closestSurfaceIndex = intersection.surfaceIndex;
            }
        }

        public Vector3[] GetSelectedWorldPoints()
        {
            var points  = new HashSet<Vector3>();
            for (var t = 0; t < States.Length; t++)
            {
                var meshState = States[t];
                var brushLocalToWorld = LocalToWorld[t];

                if (meshState.BackupPoints == null)
                    continue;
                
                foreach (var index in meshState.GetSelectedPointIndices())
                {
                    points.Add(brushLocalToWorld.MultiplyPoint(meshState.BackupPoints[index]));
                }
            }
            return points.ToArray();
        }

        public AABB GetSelectionBounds()
        {
            var newBounds = AABB.Empty;
            for (var t = 0; t < Brushes.Length; t++)
            {
                var meshState = States[t];

                for (var p = 0; p < meshState.PointSelectState.Length; p++)
                {
                    if ((meshState.PointSelectState[p] & SelectState.Selected) != SelectState.Selected)
                        continue;
                    newBounds.Add(meshState.WorldPoints[p]);
                }

                for (var e = 0; e < meshState.EdgeSelectState.Length; e++)
                {
                    if ((meshState.EdgeSelectState[e] & SelectState.Selected) != SelectState.Selected)
                        continue;

                    var index0 = meshState.Edges[(e * 2) + 0];
                    var index1 = meshState.Edges[(e * 2) + 1];

                    newBounds.Add(meshState.WorldPoints[index0]);
                    newBounds.Add(meshState.WorldPoints[index1]);
                }

                for (var p = 0; p < meshState.PolygonSelectState.Length; p++)
                {
                    if ((meshState.PolygonSelectState[p] & SelectState.Selected) != SelectState.Selected)
                        continue;

                    var indices = meshState.PolygonPointIndices[p];
                    for (var i = 0; i < indices.Length; i++)
                    {
                        var index = indices[i];

                        newBounds.Add(meshState.WorldPoints[index]);
                    }
                }
            }

            return newBounds;
        }
    }
}
