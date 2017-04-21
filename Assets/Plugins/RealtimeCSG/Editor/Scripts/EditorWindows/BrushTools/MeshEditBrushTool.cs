using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using InternalRealtimeCSG;
using RealtimeCSG.Helpers;

namespace RealtimeCSG
{
	internal sealed class MeshEditBrushTool : ScriptableObject, IBrushTool
	{
		private static readonly int RectSelectionHash			= "vertexRectSelection".GetHashCode();
		private static readonly int MeshEditBrushToolHash		= "meshEditBrushTool".GetHashCode();
		private static readonly int MeshEditBrushPointHash		= "meshEditBrushPoint".GetHashCode();
		private static readonly int MeshEditBrushEdgeHash		= "meshEditBrushEdge".GetHashCode();
		private static readonly int MeshEditBrushPolygonHash	= "meshEditBrushPolygon".GetHashCode();


		private const float HoverTextDistance = 25.0f;

		public bool	UsesUnitySelection	{ get { return true; } }
		public bool IgnoreUnityRect		{ get { return true; } }

		private bool HavePointSelection { get { for (var t = 0; t < _brushSelection.Brushes.Length; t++) if (_brushSelection.States[t].HavePointSelection) return true; return false; } }
		private bool HaveEdgeSelection	{ get { for (var t = 0; t < _brushSelection.Brushes.Length; t++) if (_brushSelection.States[t].HaveEdgeSelection ) return true; return false; } }


		#region Tool edit modes
		private enum EditMode
		{
			None,
			MovingPoint,
			MovingEdge,
			MovingPolygon,

			ScalePolygon,

			RotateEdge
		};
		
		[NonSerialized] private EditMode		_editMode			= EditMode.None;
		
		[NonSerialized] private bool			_doMarquee;			//= false;
		#endregion



		[NonSerialized] private Transform		_rotateBrushParent;	//= null;
		[NonSerialized] private Vector3			_rotateStart		= MathConstants.zeroVector3;
		[NonSerialized] private Vector3			_rotateCenter		= MathConstants.zeroVector3;
		[NonSerialized] private Vector3			_rotateTangent		= MathConstants.zeroVector3;
		[NonSerialized] private Vector3			_rotateNormal		= MathConstants.zeroVector3;
		[NonSerialized] private CSGPlane		_rotatePlane;
		[NonSerialized] private float			_rotateRadius;				//= 0;
		[NonSerialized] private float			_rotateStartAngle;			//= 0; 
		[NonSerialized] private float			_rotateCurrentAngle;		//= 0;
		[NonSerialized] private float			_rotateCurrentSnappedAngle;	//= 0;
		[NonSerialized] private int				_rotationUndoGroupIndex		= -1;

		[NonSerialized] private bool			_movePlaneInNormalDirection	;//= false;
		[NonSerialized] private Vector3			_movePolygonOrigin;
		[NonSerialized] private Vector3			_movePolygonDirection;
		[NonSerialized] private Vector3			_worldDeltaMovement;
		[NonSerialized] private Vector3			_extraDeltaMovement			= MathConstants.zeroVector3;
		
		[SerializeField] private bool			_useHandleCenter;	//= false;
		[SerializeField] private Vector3		_handleCenter;
		[SerializeField] private Vector3		_startHandleCenter;
		[SerializeField] private Vector3		_startHandleDirection;
		[SerializeField] private Vector3		_handleScale		= Vector3.one;
		[SerializeField] private Vector3		_dragEdgeScale		= Vector3.one;
		[SerializeField] private Quaternion		_dragEdgeRotation;
		[SerializeField] private Vector3[]		_handleWorldPoints;	//= null;

		[NonSerialized] private int         _hoverOnEdgeIndex	    = -1;
		[NonSerialized] private int         _hoverOnPointIndex	    = -1;
		[NonSerialized] private int         _hoverOnPolygonIndex    = -1;
		[NonSerialized] private int         _hoverOnTarget		    = -1;
		
		[NonSerialized] private int	        _rectSelectionId        = -1;
		
		[NonSerialized] private bool        _mouseIsDragging;   //= false;
		[NonSerialized] private bool        _showMarquee;       //= false;
		[NonSerialized] private bool        _firstMove;		    //= false;

		[NonSerialized] private Camera		_startCamera;
		[NonSerialized] private Vector2		_startMousePoint;
		[NonSerialized] private Vector3		_originalPoint;
		[NonSerialized] private Vector2		_mousePosition;
		[NonSerialized] private CSGPlane	_movePlane;
		[NonSerialized] private bool        _usingControl;      //= false


		[SerializeField] private readonly TransformSelection    _transformSelection  = new TransformSelection();
		[SerializeField] private readonly BrushSelection        _brushSelection      = new BrushSelection();

		[SerializeField] private UnityEngine.Object[]   _undoAbleTransforms     = new UnityEngine.Object[0];
		[SerializeField] private UnityEngine.Object[]   _undoAbleBrushes		= new UnityEngine.Object[0];


		[SerializeField] private SpaceMatrices      _activeSpaceMatrices	= new SpaceMatrices();

		[NonSerialized] private bool				_isEnabled;     //= false;
		[NonSerialized] private bool				_hideTool;      //= false;

		public void SetTargets(FilteredSelection filteredSelection)
		{
			if (filteredSelection == null)
				return;

			var foundBrushes		= filteredSelection.GetAllContainedBrushes();
			_brushSelection.Select(foundBrushes);

			var foundTransforms = new HashSet<Transform>();
			if (filteredSelection.NodeTargets != null)
			{
				for (var i = 0; i < filteredSelection.NodeTargets.Length; i++)
				{
					if (filteredSelection.NodeTargets[i])
						foundTransforms.Add(filteredSelection.NodeTargets[i].transform);
				}
			}
			if (filteredSelection.OtherTargets != null)
			{
				for (var i = 0; i < filteredSelection.OtherTargets.Length; i++)
				{
					if (filteredSelection.OtherTargets[i])
						foundTransforms.Add(filteredSelection.OtherTargets[i]);
				}
			}
			
			_transformSelection.Select(foundTransforms.ToArray());
			var transformsAsObjects = _transformSelection.Transforms.ToList<UnityEngine.Object>();
			transformsAsObjects.Add(this);
			_undoAbleTransforms = transformsAsObjects.ToArray();

			var brushesAsObjects = _brushSelection.Brushes.ToList<UnityEngine.Object>();
			brushesAsObjects.Add(this);
			_undoAbleBrushes = brushesAsObjects.ToArray();

			_hideTool = filteredSelection.NodeTargets != null && filteredSelection.NodeTargets.Length > 0;

			if (!_isEnabled)
				return;

			ForceLineUpdate();
			_brushSelection.UpdateTargets();
			CenterPositionHandle();
			Tools.hidden = _hideTool;
		}

		public void OnEnableTool()
		{			
			_isEnabled		= true;
			_usingControl	= false;
			Tools.hidden	= _hideTool;

			ForceLineUpdate();
			_brushSelection.ResetSelection();
			_brushSelection.UpdateTargets();
			CenterPositionHandle();
			ResetTool();
		}

		public void OnDisableTool()
		{
			_isEnabled = false;
			Tools.hidden = false;
			_usingControl = false;
			ResetTool();
		}

		private void ResetTool()
		{
			_usingControl	= false;
			
			Grid.ForceGrid = false;
		}

		
		public bool UndoRedoPerformed()
		{
            _brushSelection.UpdateTargets();
            //UpdateTransformMatrices();
            //UpdateSelection(allowSubstraction: false);
            UpdateWorkControlMesh();
            //UpdateBackupPoints();
            _brushSelection.UndoRedoPerformed();

            CenterPositionHandle();
            ForceLineUpdate();
            SceneView.RepaintAll();
            return false;
		}


		#region Selection & Hover

		private void SelectMarquee(Camera camera, Rect rect, SelectionType selectionType)
        {
            if (rect.width <= 0 || rect.height <= 0)
                return;

            try
            {
			    var frustum			= CameraUtility.GetCameraSubFrustumGUI(camera, rect);
			    var selectedPoints  = SceneQueryUtility.GetPointsInFrustum(frustum.Planes, _brushSelection.Brushes, _brushSelection.States);

                Undo.RecordObject(this, "Select points");
                ControlMeshState.SelectPoints(_brushSelection.States, selectedPoints, selectionType, false);
                ForceLineUpdate();
            }
            finally
            {
                CenterPositionHandle();
            }
        }

        public bool DeselectAll()
		{
			try
			{
				if (_brushSelection.States == null ||
					_brushSelection.States.Length == 0)
					return false;
                
                Undo.RecordObject(this, "Deselect All");
				if (GUIUtility.hotControl == _rectSelectionId)
                {
                    GUIUtility.hotControl = 0;				
					GUIUtility.keyboardControl = 0;
					EditorGUIUtility.editingTextField = false;
                    _brushSelection.RevertSelection();
                    ForceLineUpdate();
                    SceneView.RepaintAll();
					return true;
				}
                
                if (ControlMeshState.DeselectAll(_brushSelection.States))
				{
					SceneView.RepaintAll();
					return true;
				}
                
                Selection.activeTransform = null;
				ForceLineUpdate();
				return true;
			}
			finally
			{
				CenterPositionHandle();
			}
		}
        

		private bool UpdateSelection(bool allowSubstraction = true)
		{
			try
			{
				if (_hoverOnTarget == -1 ||
					_hoverOnTarget >= _brushSelection.States.Length)
					return false;

				var hoverMeshState = _brushSelection.States[_hoverOnTarget];
				var selectionType = SelectionUtility.GetEventSelectionType();

				if (allowSubstraction == false)
				{
					selectionType = SelectionType.Replace;
					switch (_editMode)
					{
						case EditMode.MovingPoint:
					    {
					        if (hoverMeshState.IsPointSelectedIndirectly(_hoverOnPointIndex))
                                selectionType = SelectionType.Additive;
                            break;
					    }
						case EditMode.RotateEdge:
						case EditMode.MovingEdge:
						{
							if (hoverMeshState.IsEdgeSelectedIndirectly(_hoverOnEdgeIndex))
								selectionType = SelectionType.Additive;
							break;
						}
						case EditMode.MovingPolygon:
					    {
					        if (hoverMeshState.IsPolygonSelectedIndirectly(_hoverOnPolygonIndex))
                                selectionType = SelectionType.Additive;
                            break;
					    }
					}
				}

				Undo.RecordObject(this, "Update selection"); 
				if (selectionType == SelectionType.Replace)
					ControlMeshState.DeselectAll(_brushSelection.States);


				var needRepaint = false;

				for (var p = 0; p < hoverMeshState.PolygonSelectState.Length; p++)
					needRepaint = hoverMeshState.SelectPolygon(p, selectionType, onlyOnHover: true) || needRepaint;

				for (var e = 0; e < hoverMeshState.EdgeSelectState.Length; e++)
					needRepaint = hoverMeshState.SelectEdge(e, selectionType, onlyOnHover: true) || needRepaint;

				for (var b = 0; b < _brushSelection.States.Length; b++)
				{
					var curMeshState = _brushSelection.States[b];
					for (var p = 0; p < curMeshState.WorldPoints.Length; p++)
					{
						needRepaint = curMeshState.SelectPoint(p, selectionType, onlyOnHover: true) || needRepaint;
					}
				}



				ForceLineUpdate();
				return needRepaint;
			}
			finally
			{
				CenterPositionHandle();
			}
		}



		private MouseCursor _currentCursor = MouseCursor.Arrow;

		private EditMode SetHoverOn(EditMode editModeType, int target, int index = -1)
		{
			_hoverOnTarget = target;
			if (target == -1 || _hoverOnTarget >= _brushSelection.States.Length)
			{
				_hoverOnEdgeIndex = -1;
				_hoverOnPolygonIndex = -1;
				_hoverOnPointIndex = -1;
				return EditMode.None;
			}

			_hoverOnEdgeIndex = -1;
			_hoverOnPolygonIndex = -1;
			_hoverOnPointIndex = -1;
			if (index == -1)
				return EditMode.None;

			var newCursor = MouseCursor.Arrow;
			switch (editModeType)
			{
				case EditMode.RotateEdge:       _hoverOnEdgeIndex    = index; newCursor = MouseCursor.RotateArrow; break;
				case EditMode.MovingEdge:       _hoverOnEdgeIndex    = index; newCursor = MouseCursor.MoveArrow; break;
				case EditMode.MovingPoint:      _hoverOnPointIndex   = index; newCursor = MouseCursor.MoveArrow; break;
				case EditMode.MovingPolygon:    _hoverOnPolygonIndex = index; newCursor = MouseCursor.MoveArrow; break; 
			}

			if (_currentCursor == MouseCursor.Arrow)
				_currentCursor = newCursor;

			return editModeType;
		}
		private void UpdateMouseCursor()
		{
			if (GUIUtility.hotControl == _rectSelectionId &&
				!_movePlaneInNormalDirection &&
				GUIUtility.hotControl != 0)
				return;

			_currentCursor = CursorUtility.GetSelectionCursor(SelectionUtility.GetEventSelectionType());
		}
		
		private EditMode HoverOnPoint(ControlMeshState meshState, int brushIndex, int pointIndex)
		{
			var editMode = SetHoverOn(EditMode.MovingPoint, brushIndex, pointIndex);
			meshState.PointSelectState[pointIndex] |= SelectState.Hovering;
            /*
			// select an edge if it's aligned with this point by seeing if we also 
			// clicked on the second point on the edge that our point belongs to
			var brush       = _brushSelection.Brushes[brushIndex];
			var controlMesh = brush.ControlMesh;
			var edges       = controlMesh.Edges;
			for (var e = 0; e < edges.Length; e++)
			{
				var vertexIndex1 = edges[e].VertexIndex;
				if (vertexIndex1 != pointIndex)
					continue;

				var twinIndex		= edges[e].TwinIndex;
				var vertexIndex2	= edges[twinIndex].VertexIndex;

				var radius1 = meshState.WorldPointSizes[vertexIndex1] * 1.2f;
				var distance1 = HandleUtility.DistanceToCircle(meshState.WorldPoints[vertexIndex1], radius1);

				if ((meshState.PointSelectState[vertexIndex1] & SelectState.Selected) == SelectState.Selected ||
					(meshState.PointSelectState[vertexIndex2] & SelectState.Hovering) == SelectState.Hovering)
					continue;

				var radius2 = meshState.WorldPointSizes[vertexIndex2] * 1.2f;
				var distance2 = HandleUtility.DistanceToCircle(meshState.WorldPoints[vertexIndex2], radius2);

				if (Mathf.Abs(distance1 - distance2) >= MathConstants.DistanceEpsilon)
					continue;

				meshState.PointSelectState[vertexIndex2] |= SelectState.Hovering;

				var edgeStateIndex = meshState.HalfEdgeToEdgeStates[e] / 2;
				meshState.EdgeSelectState[edgeStateIndex] |= SelectState.Hovering;
			}*/
			return editMode;
		}

		private EditMode HoverOnPolygon(ControlMeshState meshState, int brushIndex, int polygonIndex)
		{
			var editMode = SetHoverOn(EditMode.MovingPolygon, brushIndex, polygonIndex);
			meshState.PolygonSelectState[polygonIndex] |= SelectState.Hovering;
            /*
			var brush					= _brushSelection.Brushes[brushIndex];
			var controlMesh				= brush.ControlMesh;
			var halfEdgeIndices			= controlMesh.Edges;
			var polygonEdgeIndices		= controlMesh.Polygons[polygonIndex].EdgeIndices;
			var halfEdgeToEdgeStates	= meshState.HalfEdgeToEdgeStates;

			for (var i = 0; i < polygonEdgeIndices.Length; i++)
			{
				var halfEdgeIndex = polygonEdgeIndices[i];
				if (halfEdgeIndex < 0 ||
					halfEdgeIndex >= halfEdgeIndices.Length)
					continue;
				var vertexIndex = halfEdgeIndices[halfEdgeIndex].VertexIndex;
				meshState.PointSelectState[vertexIndex] |= SelectState.Hovering;

				var edgeStateIndex = halfEdgeToEdgeStates[halfEdgeIndex] / 2;
				meshState.EdgeSelectState[edgeStateIndex] |= SelectState.Hovering;
			}
            //*/
			if (Tools.current == Tool.Scale || 
				SelectionUtility.CurrentModifiers == EventModifiers.Control)
				return editMode;

			var point1 = HandleUtility.WorldToGUIPoint(meshState.PolygonCenterPoints[polygonIndex]);
			var point2 = HandleUtility.WorldToGUIPoint(meshState.PolygonCenterPoints[polygonIndex] + (meshState.PolygonCenterPlanes[polygonIndex].normal * 10.0f));
			var delta = (point2 - point1).normalized;

			_currentCursor = CursorUtility.GetCursorForDirection(delta, 0);
			return editMode;
		}

		private EditMode HoverOnEdge(ControlMeshState meshState, int brushIndex, int edgeIndex)
		{
			//if (Tools.current == Tool.Scale)
			//	return EditMode.None;

			var brush = _brushSelection.Brushes[brushIndex];
			//var controlMesh = brush.ControlMesh;
			var surfaces = brush.Shape.Surfaces;
            
            var vertexIndex1 = meshState.Edges[(edgeIndex * 2) + 0];
			var vertexIndex2 = meshState.Edges[(edgeIndex * 2) + 1];
            /*
			meshState.PointSelectState[vertexIndex1] |= SelectState.Hovering;
			meshState.PointSelectState[vertexIndex2] |= SelectState.Hovering;*/
            
            var surfaceIndex1 = meshState.EdgeSurfaces[(edgeIndex * 2) + 0];
			var surfaceIndex2 = meshState.EdgeSurfaces[(edgeIndex * 2) + 1];

			if (surfaceIndex1 < 0 || surfaceIndex1 >= surfaces.Length ||
				surfaceIndex2 < 0 || surfaceIndex2 >= surfaces.Length)
				return EditMode.None;

			var editMode = EditMode.None;
			if (Tools.current != Tool.Rotate)
			{
				editMode = SetHoverOn(EditMode.MovingEdge, brushIndex, edgeIndex);

				var point1 = HandleUtility.WorldToGUIPoint(meshState.WorldPoints[vertexIndex1]);
				var point2 = HandleUtility.WorldToGUIPoint(meshState.WorldPoints[vertexIndex2]);
				var delta = (point2 - point1).normalized;

				_currentCursor = CursorUtility.GetCursorForEdge(delta);
			} else
			if (Tools.current == Tool.Rotate)
				editMode = SetHoverOn(EditMode.RotateEdge, brushIndex, edgeIndex);

			meshState.EdgeSelectState[edgeIndex] |= SelectState.Hovering;
            /*
			if ((meshState.EdgeSelectState[edgeIndex] & SelectState.Selected) == SelectState.Selected)
				return editMode;

			var localToWorldMatrix = meshState.BrushTransform.localToWorldMatrix;
			var surfaceNormal1 = localToWorldMatrix.MultiplyVector(meshState.PolygonCenterPlanes[surfaceIndex1].normal).normalized;
			var surfaceNormal2 = localToWorldMatrix.MultiplyVector(meshState.PolygonCenterPlanes[surfaceIndex2].normal).normalized;
				
			// note: can't use camera considering we're not sure which camera we're looking through w/ multiple sceneviews
			var cameraForward = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition).direction;
			var forward = cameraForward;
			if (Camera.current.orthographic)
			{
				var dot1 = Math.Abs(Vector3.Dot(surfaceNormal1, forward));
				var dot2 = Math.Abs(Vector3.Dot(surfaceNormal2, forward));
				if (dot1 > MathConstants.GUIAlignmentTestEpsilon) { surfaceIndex1 = -1; }
				if (dot2 > MathConstants.GUIAlignmentTestEpsilon) { surfaceIndex2 = -1; }
			} else
			{
				surfaceIndex1 = -1;
				surfaceIndex2 = -1;
			}

			if (surfaceIndex1 == -1 && surfaceIndex2 == -1)
				return editMode;

			var halfEdges = controlMesh.Edges;
			var polygons = controlMesh.Polygons;
			for (var p = 0; p < polygons.Length; p++)
			{
				if (p != surfaceIndex1 && p != surfaceIndex2)
					continue;

				var halfEdgeIndices = polygons[p].EdgeIndices;
				for (var i = 0; i < halfEdgeIndices.Length; i++)
				{
					var halfEdgeIndex	= halfEdgeIndices[i];
					var halfEdge		= halfEdges[halfEdgeIndex];
					var vertexIndex		= halfEdge.VertexIndex;

					meshState.PointSelectState[vertexIndex] |= SelectState.Hovering;

					var edgeStateIndex = meshState.HalfEdgeToEdgeStates[halfEdgeIndex] / 2;
					meshState.EdgeSelectState[edgeStateIndex] |= SelectState.Hovering;
				}
			}*/
			return editMode;
		}

		#endregion



		#region Actions

		public void SnapToGrid()
		{
			try
			{
				if (HavePointSelection)
				{
					Undo.RecordObjects(_undoAbleBrushes, "Snap points to grid");
					_brushSelection.PointSnapToGrid();
					UpdateWorkControlMesh(forceUpdate: true);
				} else
				{
					Undo.RecordObjects(_undoAbleTransforms, "Snap brushes to grid");
					BrushOperations.SnapToGrid(_brushSelection.Brushes);
				}
			}
			finally
			{
				CenterPositionHandle();
			}
		}



		private void ShapeCancelled()
		{
			CSGBrushEditorManager.EditMode = ToolEditMode.Mesh;
			GenerateBrushTool.ShapeCancelled -= ShapeCancelled;
			GenerateBrushTool.ShapeCommitted -= ShapeCommitted;
		}

		private void ShapeCommitted()
		{
			CSGBrushEditorManager.EditMode = ToolEditMode.Mesh;
			GenerateBrushTool.ShapeCancelled -= ShapeCancelled;
			GenerateBrushTool.ShapeCommitted -= ShapeCommitted;
		}


		private void ExtrudeSurface(bool drag)
		{
			GenerateBrushTool.ShapeCancelled += ShapeCancelled;
			GenerateBrushTool.ShapeCommitted += ShapeCommitted;

			var targetMeshState = _brushSelection.States[_hoverOnTarget];
			var brushLocalToWorld = targetMeshState.BrushTransform.localToWorldMatrix;

			var polygonPlane = _brushSelection.States[_hoverOnTarget].PolygonCenterPlanes[_hoverOnPolygonIndex];
			polygonPlane.Transform(brushLocalToWorld);

			var localNormal = targetMeshState.PolygonCenterPlanes[_hoverOnPolygonIndex].normal;
			var worldNormal = targetMeshState.BrushTransform.localToWorldMatrix.MultiplyVector(localNormal).normalized;

			if (Tools.pivotRotation == PivotRotation.Global)
				worldNormal = GeometryUtility.SnapToClosestAxis(worldNormal);

			var points = _brushSelection.States[_hoverOnTarget].WorldPoints;
			var pointIndices = _brushSelection.States[_hoverOnTarget].PolygonPointIndices[_hoverOnPolygonIndex];
			CSGBrushEditorManager.GenerateFromSurface(_brushSelection.Brushes[_hoverOnTarget], polygonPlane, worldNormal, points, pointIndices, drag);
		}

		private void MergeDuplicatePoints()
		{
			if (_editMode == EditMode.RotateEdge)
				return;

			try
			{
				Undo.RegisterCompleteObjectUndo(_undoAbleBrushes, "Merging vertices");
				ControlMeshUtility.MergeDuplicatePoints(_brushSelection.Brushes, _brushSelection.States);
				UpdateWorkControlMesh();
			}
			finally
			{
				CenterPositionHandle();
			}
		}

		private void MergeHoverEdgePoints()
		{
			try
			{
				Undo.RegisterCompleteObjectUndo(_undoAbleBrushes, "Merge edge-points");
				ControlMeshUtility.MergeHoverEdgePoints(_brushSelection.Brushes[_hoverOnTarget], _brushSelection.States[_hoverOnTarget], _hoverOnEdgeIndex);
				UpdateWorkControlMesh();
			}
			finally
			{
				CenterPositionHandle();
			}
		}

		private void MergeHoverPolygonPoints()
		{
			try
			{
				Undo.RegisterCompleteObjectUndo(_undoAbleBrushes, "Merge edge-points");
				ControlMeshUtility.MergeHoverPolygonPoints(_brushSelection.Brushes[_hoverOnTarget], _hoverOnPolygonIndex);
				UpdateWorkControlMesh();
			}
			finally
			{
				CenterPositionHandle();
			}
		}

		private void MergeSelected()
		{
			if (!HaveEdgeSelection)
			{
				if (_editMode == EditMode.MovingEdge &&
					_hoverOnTarget != -1 && _hoverOnTarget < _brushSelection.States.Length &&
					_hoverOnEdgeIndex != -1)
				{
					MergeHoverEdgePoints();
				}
				else
				if (_editMode == EditMode.MovingPolygon &&
					_hoverOnTarget != -1 && _hoverOnTarget < _brushSelection.States.Length &&
					_hoverOnPolygonIndex != -1)
				{
					MergeHoverPolygonPoints();
				}
			}

			MergeSelectedEdgePoints();
		}


		private void DoRotateBrushes(Vector3 rotationCenter, Vector3 rotationAxis, float rotationAngle)
		{
			try
			{
				Undo.RecordObjects(_undoAbleTransforms, "Transform brushes");
				_transformSelection.Rotate(rotationCenter, rotationAxis, rotationAngle);
			}
			finally
			{
				CenterPositionHandle();
			}
		}

		private void DoMoveControlPoints(Vector3 worldOffset)
		{
			try
			{
				Undo.RecordObjects(_undoAbleBrushes, "Move control-points");
				_brushSelection.TranslateControlPoints(worldOffset);
			}
			finally
			{
				CenterPositionHandle();
			}
		}

		private void DoRotateControlPoints(Vector3 center, Quaternion handleRotation, Quaternion rotationOffset)
		{
			try
			{
				Undo.RecordObjects(_undoAbleBrushes, "Rotate control-points");
				_brushSelection.RotateControlPoints(center, handleRotation, rotationOffset);
			}
			finally
			{
				CenterPositionHandle();
			}
		}

		private void DoScaleControlPoints(Quaternion rotation, Vector3 scale, Vector3 center)
		{
			try
			{
				Undo.RecordObjects(_undoAbleBrushes, "Scale control-points");
				_brushSelection.ScaleControlPoints(center, rotation, scale);
			}
			finally
			{
				CenterPositionHandle();
			}
		}

		private void DeleteSelectedPoints()
		{
			try
			{
				Undo.RegisterCompleteObjectUndo(_undoAbleBrushes, "Delete control-points");
				ControlMeshUtility.DeleteSelectedPoints(_brushSelection.Brushes, _brushSelection.States);
				UpdateWorkControlMesh();
			}
			finally
			{
				CenterPositionHandle();
			}
		}

		public void FlipX()
		{
			try
			{
				BrushOperations.FlipX(_brushSelection.Brushes);
				UpdateWorkControlMesh();
			}
			finally
			{
				CenterPositionHandle();
			}
		}

		public void FlipY()
		{
			try
			{
				BrushOperations.FlipY(_brushSelection.Brushes);
				UpdateWorkControlMesh();
			}
			finally
			{
				CenterPositionHandle();
			}
		}

		public void FlipZ()
		{
			try
			{
				BrushOperations.FlipZ(_brushSelection.Brushes);
				UpdateWorkControlMesh();
			}
			finally
			{
				CenterPositionHandle();
			}
		}

		private void MergeSelectedEdgePoints()
		{
			try
			{
				Undo.RegisterCompleteObjectUndo(_undoAbleBrushes, "Merge edge-points");
				ControlMeshUtility.MergeSelectedEdgePoints(_brushSelection.Brushes, _brushSelection.States);
				UpdateWorkControlMesh();
			}
			finally
			{
				CenterPositionHandle();
			}
		}

		#endregion





		private bool UpdateWorkControlMesh(bool forceUpdate = false)
		{
		    _brushSelection.UpdateWorkControlMesh(forceUpdate);
            _transformSelection.Update();
            return true;
		}

		private void UpdateBackupPoints()
		{
			for (var t = 0; t < _brushSelection.Brushes.Length; t++)
			{
				var workControlMesh = _brushSelection.ControlMeshes[t];
				_brushSelection.States[t].BackupPoints = new Vector3[workControlMesh.Vertices.Length];
				if (workControlMesh.Vertices.Length > 0)
				{
					Array.Copy(workControlMesh.Vertices,
								_brushSelection.States[t].BackupPoints,
								workControlMesh.Vertices.Length);
				}

				_brushSelection.States[t].BackupPolygonCenterPoints = new Vector3[_brushSelection.States[t].PolygonCenterPoints.Length];
				if (_brushSelection.States[t].PolygonCenterPoints.Length > 0)
				{
					Array.Copy(_brushSelection.States[t].PolygonCenterPoints,
								_brushSelection.States[t].BackupPolygonCenterPoints,
								_brushSelection.States[t].PolygonCenterPoints.Length);
				}

				_brushSelection.States[t].BackupPolygonCenterPlanes = new CSGPlane[_brushSelection.States[t].PolygonCenterPlanes.Length];
				if (_brushSelection.States[t].PolygonCenterPlanes.Length > 0)
				{
					Array.Copy(_brushSelection.States[t].PolygonCenterPlanes,
								_brushSelection.States[t].BackupPolygonCenterPlanes,
								_brushSelection.States[t].PolygonCenterPlanes.Length);
				}
			}
		}

		private void UpdateTransformMatrices()
		{
			_activeSpaceMatrices = SpaceMatrices.Create(Selection.activeTransform);
		}

        private bool UpdateRotationCircle()
		{
			switch (_editMode)
			{
				case EditMode.RotateEdge:
				{
					_rotateBrushParent = _brushSelection.ModelTransforms[_hoverOnTarget];
					if (_rotateBrushParent == null)
						return false;

					var meshState = _brushSelection.States[_hoverOnTarget];

					_rotateCenter = MathConstants.zeroVector3;
					for (var p = 0; p < meshState.WorldPoints.Length; p++)
					{
						_rotateCenter += meshState.WorldPoints[p];
					}
					_rotateCenter = (_rotateCenter / meshState.WorldPoints.Length);

					var pointIndex1 = meshState.Edges[(_hoverOnEdgeIndex * 2) + 0];
					var pointIndex2 = meshState.Edges[(_hoverOnEdgeIndex * 2) + 1];

					var vertex1 = meshState.WorldPoints[pointIndex1];
					var vertex2 = meshState.WorldPoints[pointIndex2];

					var camera = Camera.current;

					_rotateNormal = camera.orthographic ? camera.transform.forward.normalized : (vertex2 - vertex1).normalized;

					if (Tools.pivotRotation == PivotRotation.Global)
					{
						_rotateNormal = GeometryUtility.SnapToClosestAxis(_rotateNormal);
					}

					_rotatePlane = new CSGPlane(_rotateNormal, _rotateCenter);
					_rotateStart = ((vertex2 + vertex1) * 0.5f);
					_rotateStart = GeometryUtility.ProjectPointOnPlane(_rotatePlane, _rotateStart);
					var delta = (_rotateCenter - _rotateStart);
					_rotateTangent = -delta.normalized;

					var ray			= HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
					var newMousePos = _rotatePlane.Intersection(ray);
					_rotateStartAngle = GeometryUtility.SignedAngle(_rotateCenter - _rotateStart, _rotateCenter - newMousePos, _rotateNormal); 

					var handleSize = HandleUtility.GetHandleSize(_rotateCenter);
					_rotateRadius = Math.Max(delta.magnitude, handleSize);

					return true;
				}
			}
			return false;
		}

		private void UpdateGrid(Camera camera)
		{
			if (_hoverOnTarget == -1 || _hoverOnTarget >= _brushSelection.States.Length || 
				!camera)
			{
				return;
			}
			
			if (_hoverOnPolygonIndex != -1 &&
				_editMode == EditMode.MovingPolygon && 
				(SelectionUtility.CurrentModifiers & EventModifiers.Control) != EventModifiers.Control)
			{
				var targetMeshState		= _brushSelection.States[_hoverOnTarget];
				var brushLocalToWorld	= targetMeshState.BrushTransform.localToWorldMatrix;	
				var worldOrigin			= targetMeshState.PolygonCenterPoints[_hoverOnPolygonIndex];
				var worldDirection		= brushLocalToWorld.MultiplyVector(
											targetMeshState.PolygonCenterPlanes[_hoverOnPolygonIndex].normal).normalized;
				if (Tools.pivotRotation == PivotRotation.Global)
					worldDirection = GeometryUtility.SnapToClosestAxis(worldDirection);
				Grid.SetupRayWorkPlane(worldOrigin, worldDirection, ref _movePlane);
							
				_movePlaneInNormalDirection = true;
				_movePolygonOrigin		= worldOrigin;
				_movePolygonDirection	= worldDirection;
			} else
			if (_hoverOnEdgeIndex != -1 &&
				_editMode == EditMode.MovingEdge && 
				(SelectionUtility.CurrentModifiers & EventModifiers.Control) != EventModifiers.Control)
			{
				var targetMeshState		= _brushSelection.States[_hoverOnTarget];
				var brushLocalToWorld	= targetMeshState.BrushTransform.localToWorldMatrix;
				var pointIndex1			= targetMeshState.Edges[(_hoverOnEdgeIndex * 2) + 0];
				var pointIndex2			= targetMeshState.Edges[(_hoverOnEdgeIndex * 2) + 1];
				var vertex1				= targetMeshState.WorldPoints[pointIndex1];
				var vertex2				= targetMeshState.WorldPoints[pointIndex2];

				var worldOrigin			= _originalPoint;
				var worldDirection		= brushLocalToWorld.MultiplyVector(vertex2 - vertex1).normalized;

				if (Tools.current == Tool.Scale)
				{
					worldDirection = camera.transform.forward;
				}

				if (Tools.pivotRotation == PivotRotation.Global)
					worldDirection = GeometryUtility.SnapToClosestAxis(worldDirection);
				Grid.SetupWorkPlane(worldOrigin, worldDirection, ref _movePlane);
							
				_movePlaneInNormalDirection = true;
				_movePolygonOrigin		= worldOrigin;
				_movePolygonDirection	= worldDirection;
			} else
			{ 	
				Grid.SetupWorkPlane(_originalPoint, ref _movePlane);
				
				_movePlaneInNormalDirection = false;
			}
		}


		private Vector3 SnapMovementToPlane(Vector3 offset)
		{
			if (Math.Abs(_movePlane.a) > 1 - MathConstants.NormalEpsilon) offset.x = 0.0f;
			if (Math.Abs(_movePlane.b) > 1 - MathConstants.NormalEpsilon) offset.y = 0.0f;
			if (Math.Abs(_movePlane.c) > 1 - MathConstants.NormalEpsilon) offset.z = 0.0f;
            if (float.IsNaN(offset.x) || float.IsNaN(offset.y) || float.IsNaN(offset.z))
                offset = MathConstants.zeroVector3;
            return offset;
		}






	    internal class BrushOutlineInfo
        {
            public readonly BrushOutlineRenderer BrushOutlineRenderer = new BrushOutlineRenderer();
            public int LastLineMeshGeneration = -1;
            public int LastHandleGeneration = -1;
            internal void Destroy()
            {
                BrushOutlineRenderer.Destroy();
            }

        }

        private readonly Dictionary<SceneView, BrushOutlineInfo> _brushOutlineInfos = new Dictionary<SceneView, BrushOutlineInfo>();


	    internal BrushOutlineInfo GetBrushOutLineInfo(SceneView sceneView)
        {
            BrushOutlineInfo brushOutlineInfo;
            if (_brushOutlineInfos.TryGetValue(sceneView, out brushOutlineInfo))
                return brushOutlineInfo;
            
            brushOutlineInfo = new BrushOutlineInfo();
            _brushOutlineInfos[sceneView] = brushOutlineInfo;
            return brushOutlineInfo;
        }

        internal void OnDestroy()
        {
            foreach (var brushOutlineInfo in _brushOutlineInfos.Values)
                brushOutlineInfo.Destroy();
            _brushOutlineInfos.Clear();
        }

	    private void ForceLineUpdate()
        {
            var currentMeshGeneration = InternalCSGModelManager.MeshGeneration;
            var removeKeys = new List<SceneView>();
            foreach (var brushOutlineInfo in _brushOutlineInfos)
            {
                if (!brushOutlineInfo.Key)
                {
                    brushOutlineInfo.Value.Destroy();
                    removeKeys.Add(brushOutlineInfo.Key);
                    continue;
                }
                brushOutlineInfo.Value.LastLineMeshGeneration = currentMeshGeneration - 1000;
            }
            foreach (var key in removeKeys)
                _brushOutlineInfos.Remove(key);
        }

	    private void UpdateLineMeshes()
        {
            var sceneView = (SceneView.currentDrawingSceneView == null) ? null : SceneView.lastActiveSceneView;
            if (!sceneView)
                return;


            for (var t = 0; t < _brushSelection.Brushes.Length; t++)
                _brushSelection.States[t].UpdateHandles(sceneView.camera, _brushSelection.BackupControlMeshes[t]);

            var brushOutlineInfo = GetBrushOutLineInfo(sceneView);
            if (brushOutlineInfo == null)
                return;
            
            var brushOutlineRenderer = brushOutlineInfo.BrushOutlineRenderer;

            var currentMeshGeneration   = InternalCSGModelManager.MeshGeneration;
            for (var t = 0; t < _brushSelection.Brushes.Length; t++)
            {
                var brush = _brushSelection.Brushes[t];
                if (!brush)
                    continue;

                var meshState = _brushSelection.States[t];
                var modelTransform = _brushSelection.ModelTransforms[t];
                if (modelTransform &&
                    meshState.WorldPoints.Length != 0 &&
                    meshState.Edges.Length != 0)
                    continue;

                brushOutlineInfo.LastLineMeshGeneration = currentMeshGeneration - 1000;
                _brushSelection.UpdateParentModelTransforms();
                break;
            }

            if (brushOutlineInfo.LastLineMeshGeneration == currentMeshGeneration)
                return;

            brushOutlineInfo.LastLineMeshGeneration = currentMeshGeneration;

            for (var t = 0; t < _brushSelection.Brushes.Length; t++)
                _brushSelection.States[t].UpdateMesh(_brushSelection.BackupControlMeshes[t],
                                                                _brushSelection.ControlMeshes[t].Vertices);

            brushOutlineRenderer.Update(sceneView.camera, _brushSelection.Brushes, _brushSelection.ControlMeshes, _brushSelection.States);
        }

        private void OnPaint()
        {
            var sceneView = (SceneView.currentDrawingSceneView == null) ? null : SceneView.lastActiveSceneView;
            if (!sceneView)
                return;

            var brushOutlineInfo = GetBrushOutLineInfo(sceneView);
            if (brushOutlineInfo == null)
                return;

            if (_movePlaneInNormalDirection &&
				_hoverOnTarget != -1 && _hoverOnTarget < _brushSelection.States.Length &&
				_hoverOnPolygonIndex != -1)
			{
				_currentCursor = CursorUtility.GetCursorForDirection(_movePolygonDirection, 90);
			}

			var currentSceneView = SceneView.currentDrawingSceneView;
			if (currentSceneView != null)
			{
				var windowRect = new Rect(0, 0, currentSceneView.position.width, currentSceneView.position.height - GUIStyleUtility.BottomToolBarHeight);
				EditorGUIUtility.AddCursorRect(windowRect, _currentCursor);
			}

			var origMatrix = Handles.matrix;
			Handles.matrix = MathConstants.identityMatrix;

			var currentTool = Tools.current;
			{
				brushOutlineInfo.BrushOutlineRenderer.RenderOutlines();
				
				if (//currentTool != Tool.Scale && 
					(_showMarquee || !_mouseIsDragging || (_editMode == EditMode.MovingPoint || _editMode == EditMode.MovingEdge)))
				{
					for (var t = 0; t < _brushSelection.Brushes.Length; t++)
					{
						var meshState		= _brushSelection.States[t];
						var modelTransform	= _brushSelection.ModelTransforms[t];
						if (modelTransform == null ||
							meshState.WorldPoints == null)
							continue;
								
						PaintUtility.DrawDoubleDots(meshState.WorldPoints, 
													meshState.WorldPointSizes, 
													meshState.WorldPointColors, 
													meshState.WorldPoints.Length);
					}
				}

				if (!Camera.current.orthographic && !_showMarquee && (!_mouseIsDragging || _editMode == EditMode.MovingPolygon))
				{
					for (var t = 0; t < _brushSelection.Brushes.Length; t++)
					{
						var meshState		= _brushSelection.States[t];
						var modelTransform  = _brushSelection.ModelTransforms[t];
						if (modelTransform == null)
							continue;
								
						PaintUtility.DrawDoubleDots(meshState.PolygonCenterPoints, 
													meshState.PolygonCenterPointSizes, 
													meshState.PolygonCenterColors, 
													meshState.PolygonCenterPoints.Length);
					}
				}

				if (currentTool == Tool.Rotate && _editMode == EditMode.RotateEdge)
				{
					//if (rotateBrushParent != null)
					{
						if (_mouseIsDragging)
						{
							PaintUtility.DrawRotateCircle(_rotateCenter, _rotateNormal, _rotateTangent, _rotateRadius, 0, _rotateStartAngle, _rotateCurrentSnappedAngle, 
															ColorSettings.RotateCircleOutline);
							PaintUtility.DrawRotateCirclePie(_rotateCenter, _rotateNormal, _rotateTangent, _rotateRadius, 0, _rotateStartAngle, _rotateCurrentSnappedAngle, 
															ColorSettings.RotateCircleOutline);
						} else
						{
							var camera = Camera.current;
							var inSceneView = camera.pixelRect.Contains(Event.current.mousePosition);
							if (inSceneView && UpdateRotationCircle())
							{
								PaintUtility.DrawRotateCircle(_rotateCenter, _rotateNormal, _rotateTangent, _rotateRadius, 0, _rotateStartAngle, _rotateStartAngle, 
																ColorSettings.RotateCircleOutline);
							}
						}
					}
				}

				if ((Tools.current != Tool.Scale && Tools.current != Tool.Rotate && 
					(SelectionUtility.CurrentModifiers == EventModifiers.Shift || SelectionUtility.CurrentModifiers != EventModifiers.Control)) 
					&& _hoverOnTarget != -1 && _hoverOnPolygonIndex != -1
					)
				{
					var t = _hoverOnTarget;				
					var p = _hoverOnPolygonIndex;
						
					if (t >= 0 && t < _brushSelection.States.Length)
					{
						var targetMeshState = _brushSelection.States[t];
						var modelTransform = _brushSelection.ModelTransforms[t];
						if (modelTransform != null)
						{
							if (_hoverOnTarget == t &&
								p == _hoverOnPolygonIndex)
								Handles.color = ColorSettings.PolygonInnerStateColor[(int)(SelectState.Selected | SelectState.Hovering)];

							if (p < targetMeshState.PolygonCenterPoints.Length)
							{
								var origin = targetMeshState.PolygonCenterPoints[p];

								var localNormal = targetMeshState.PolygonCenterPlanes[p].normal;
								var worldNormal = targetMeshState.BrushTransform.localToWorldMatrix.MultiplyVector(localNormal).normalized;

								Handles.matrix = MathConstants.identityMatrix;

								if (Tools.pivotRotation == PivotRotation.Global)
									worldNormal = GeometryUtility.SnapToClosestAxis(worldNormal);

								PaintUtility.DrawArrowCap(origin, worldNormal, HandleUtility.GetHandleSize(origin));
								Handles.matrix = MathConstants.identityMatrix;
							}
						}
					}
				}


				if ((SelectionUtility.CurrentModifiers == EventModifiers.Shift) &&// || Tools.current == Tool.Scale) &&
					_hoverOnPolygonIndex != -1 && _hoverOnTarget != -1 && _hoverOnTarget < _brushSelection.States.Length)
				{
					var targetMeshState = _brushSelection.States[_hoverOnTarget];
					var modelTransform	= _brushSelection.ModelTransforms[_hoverOnTarget];
					if (modelTransform != null)
					{
						if (Camera.current.pixelRect.Contains(Event.current.mousePosition))
						{
							var origin = targetMeshState.PolygonCenterPoints[_hoverOnPolygonIndex];

							var textCenter2D = HandleUtility.WorldToGUIPoint(origin);
							textCenter2D.y += HoverTextDistance * 2;

							var textCenterRay = HandleUtility.GUIPointToWorldRay(textCenter2D);
							var textCenter = textCenterRay.origin + textCenterRay.direction * ((Camera.current.farClipPlane + Camera.current.nearClipPlane) * 0.5f);

							Handles.color = Color.black;

							if (SelectionUtility.CurrentModifiers == EventModifiers.Shift)
							{
								Handles.DrawLine(origin, textCenter);
								PaintUtility.DrawScreenText(textCenter2D, "Extrude");
							}
						}
					}
				}

				if (currentTool != Tool.Rotate && currentTool != Tool.Scale)
				{
					if (_hoverOnEdgeIndex != -1 &&
						_hoverOnTarget >= 0 && _hoverOnTarget < _brushSelection.States.Length)
					{
						var meshState = _brushSelection.States[_hoverOnTarget];
						if (((_hoverOnEdgeIndex * 2) + 1) < meshState.Edges.Length)
						{
							Handles.matrix = origMatrix;
							var pointIndex1 = meshState.Edges[(_hoverOnEdgeIndex * 2) + 0];
							var pointIndex2 = meshState.Edges[(_hoverOnEdgeIndex * 2) + 1];
							var vertexA = meshState.WorldPoints[pointIndex1];
							var vertexB = meshState.WorldPoints[pointIndex2];

							var lineDelta = (vertexB - vertexA);
							var length = lineDelta.magnitude;

							var lineCenter		= (vertexB + vertexA) * 0.5f;
							var textCenter2D	= HandleUtility.WorldToGUIPoint(lineCenter);
							var brushCenter2D	= HandleUtility.WorldToGUIPoint(meshState.BrushCenter);

							var vertex2dA = HandleUtility.WorldToGUIPoint(vertexA);
							var vertex2dB = HandleUtility.WorldToGUIPoint(vertexB);
							var line2DDelta = vertex2dB - vertex2dA;
							var centerDelta = brushCenter2D - vertex2dA;//textCenter2D;

							var dot = line2DDelta.x * centerDelta.x + line2DDelta.y * centerDelta.y;
							var det = line2DDelta.x * centerDelta.y - line2DDelta.y * centerDelta.x;
							var angle = Mathf.Atan2(det, dot);

							if (Mathf.Sign(angle) < 0)
								line2DDelta = -line2DDelta;
							line2DDelta.y = -line2DDelta.y;
							line2DDelta.Normalize();
							line2DDelta *= HoverTextDistance;

							textCenter2D.x -= line2DDelta.y;
							textCenter2D.y -= line2DDelta.x;

							var textCenterRay = HandleUtility.GUIPointToWorldRay(textCenter2D);
							var textCenter = textCenterRay.origin + textCenterRay.direction * ((Camera.current.farClipPlane + Camera.current.nearClipPlane) * 0.5f);

							Handles.color = Color.black;
							Handles.DrawLine(lineCenter, textCenter);
								
							PaintUtility.DrawScreenText(textCenter2D, Units.ToRoundedDistanceString(length));

							Handles.matrix = MathConstants.identityMatrix;
						}
					}
				}
			}

			Handles.matrix = origMatrix;
		}

		private void RenderOffsetText()
		{
			var delta = _worldDeltaMovement + _extraDeltaMovement;
			if (Tools.pivotRotation == PivotRotation.Local)
			{
				if (_activeSpaceMatrices == null)
					_activeSpaceMatrices = SpaceMatrices.Create(Selection.activeTransform);

				delta = GridUtility.CleanPosition(_activeSpaceMatrices.activeLocalToWorld.MultiplyVector(delta).normalized);
			}
							
			var textCenter2D = Event.current.mousePosition;
			textCenter2D.y += HoverTextDistance * 2;
							
			var lockX	= (Mathf.Abs(delta.x) < MathConstants.ConsideredZero) && (CSGSettings.LockAxisX || (Mathf.Abs(_movePlane.a) >= 1 - MathConstants.EqualityEpsilon));
			var lockY	= (Mathf.Abs(delta.y) < MathConstants.ConsideredZero) && (CSGSettings.LockAxisY || (Mathf.Abs(_movePlane.b) >= 1 - MathConstants.EqualityEpsilon));
			var lockZ	= (Mathf.Abs(delta.z) < MathConstants.ConsideredZero) && (CSGSettings.LockAxisZ || (Mathf.Abs(_movePlane.c) >= 1 - MathConstants.EqualityEpsilon));
					
			var text	= Units.ToRoundedDistanceString(delta, lockX, lockY, lockZ);
			PaintUtility.DrawScreenText(textCenter2D, text);
		}


		private int _meshEditBrushToolId;
		private void CreateControlIDs()
		{
			_meshEditBrushToolId = GUIUtility.GetControlID(MeshEditBrushToolHash, FocusType.Keyboard);
			HandleUtility.AddDefaultControl(_meshEditBrushToolId);
			
			_rectSelectionId = GUIUtility.GetControlID(RectSelectionHash, FocusType.Keyboard);
			if (_brushSelection.States == null)
				return;

			for (var t = 0; t < _brushSelection.States.Length; t++)
			{
				var meshState = _brushSelection.States[t];
				if (meshState == null)
					continue;
				
				for (var p = 0; p < meshState.WorldPoints.Length; p++)
					meshState.PointControlId[p] = GUIUtility.GetControlID(MeshEditBrushPointHash, FocusType.Keyboard);
					
				for (var e = 0; e < meshState.Edges.Length / 2; e++)
					meshState.EdgeControlId[e] = GUIUtility.GetControlID(MeshEditBrushEdgeHash, FocusType.Keyboard);
					
				for (var p = 0; p < meshState.PolygonCenterPoints.Length; p++)
					meshState.PolygonControlId[p] = GUIUtility.GetControlID(MeshEditBrushPolygonHash, FocusType.Keyboard);
			}
		}

		private void CenterPositionHandle()
		{
			if (_brushSelection.Brushes.Length <= 0)
			{
				_useHandleCenter = false;
                return;
			}

		    var bounds = _brushSelection.GetSelectionBounds();
			if (!bounds.Valid)
			{
				_useHandleCenter = false;
				return;
			}
            
            _handleCenter = bounds.Center;
			_handleScale = Vector3.one;
			_useHandleCenter = true;
		}
		

		private Quaternion GetRealHandleRotation()
		{
			var rotation = Tools.handleRotation;
			if (Tools.pivotRotation == PivotRotation.Local)
			{
				var polygonSelectedCount = 0;
				for (var t = 0; t < _brushSelection.States.Length; t++)
				{
					var targetMeshState = _brushSelection.States[t];
					for (var p = 0; p < targetMeshState.PolygonCount; p++)
					{
						if (!targetMeshState.IsPolygonSelected(p))
							continue;

						polygonSelectedCount++;
						var localNormal		= targetMeshState.PolygonCenterPlanes[p].normal;
						var worldNormal		= targetMeshState.BrushTransform.localToWorldMatrix.MultiplyVector(localNormal).normalized;
						if (worldNormal.sqrMagnitude < MathConstants.EqualityEpsilonSqr)
							continue;
						rotation = Quaternion.LookRotation(worldNormal);

						if (Vector3.Dot(rotation * Vector3.forward, worldNormal) < 0)
							rotation = Quaternion.Inverse(rotation);

						if (polygonSelectedCount > 1)
							break;
					}
					if (polygonSelectedCount > 1)
						break;
				}
				if (polygonSelectedCount != 1)
					rotation = Tools.handleRotation;
			}
			if (rotation.x <= MathConstants.EqualityEpsilon &&
				rotation.y <= MathConstants.EqualityEpsilon &&
				rotation.z <= MathConstants.EqualityEpsilon &&
				rotation.w <= MathConstants.EqualityEpsilon)
				rotation = Quaternion.identity;

			return rotation;
		}

		private void DrawScaleBounds(Camera camera, Quaternion rotation, Vector3 scale, Vector3 center, Vector3[] worldPoints)
		{
			var lockX	= ((Mathf.Abs(scale.x) - 1) < MathConstants.ConsideredZero) && (CSGSettings.LockAxisX || (Mathf.Abs(_movePlane.a) >= 1 - MathConstants.EqualityEpsilon));
			var lockY	= ((Mathf.Abs(scale.y) - 1) < MathConstants.ConsideredZero) && (CSGSettings.LockAxisY || (Mathf.Abs(_movePlane.b) >= 1 - MathConstants.EqualityEpsilon));
			var lockZ	= ((Mathf.Abs(scale.z) - 1) < MathConstants.ConsideredZero) && (CSGSettings.LockAxisZ || (Mathf.Abs(_movePlane.c) >= 1 - MathConstants.EqualityEpsilon));
			
			var text	= Units.ToRoundedScaleString(scale, lockX, lockY, lockZ);
			PaintUtility.DrawScreenText(_handleCenter, HoverTextDistance * 3, text);

			var bounds = BoundsUtilities.GetBounds(worldPoints, rotation, scale, center);
			var outputVertices = new Vector3[8];
			BoundsUtilities.GetBoundsVertices(bounds, outputVertices);
			PaintUtility.RenderBoundsSizes(Quaternion.Inverse(rotation), rotation, camera, outputVertices, Color.white, Color.white, Color.white, true, true, true);
		}



		[NonSerialized] private Vector2 _prevMousePos;

		public void HandleEvents(Rect sceneRect)
		{
			var originalEventType = Event.current.type;
			switch (originalEventType)
			{
				case EventType.MouseMove:
				{
					_mouseIsDragging = false;
					break;
				}
				case EventType.MouseDown:
				{
					_mouseIsDragging = false;
					_prevMousePos = Event.current.mousePosition;
					break;
				}
				case EventType.MouseDrag:
				{
					if (!_mouseIsDragging && (_prevMousePos - Event.current.mousePosition).sqrMagnitude > 4.0f)
						_mouseIsDragging = true;
					break;
				}
			}


			if (!SceneTools.IsDraggingObjectInScene &&
				Event.current.GetTypeForControl(_meshEditBrushToolId) == EventType.Repaint)
			{
				OnPaint();
			}
            
			CreateControlIDs();


			var camera = Camera.current;
			if (_useHandleCenter)
			{
				RealtimeCSG.Helpers.CSGHandles.InitFunction init = delegate
				{
					UpdateTransformMatrices();
					UpdateSelection(allowSubstraction: false);
					UpdateWorkControlMesh();
					UpdateBackupPoints();
					UpdateGrid(_startCamera);
					_handleWorldPoints = _brushSelection.GetSelectedWorldPoints();
					CenterPositionHandle();
					_startHandleCenter = _handleCenter;
					_usingControl = true;
				};

				if (GUIUtility.hotControl == 0)
				{
					_handleWorldPoints = null;
					_handleScale = Vector3.one;
				}

				switch (Tools.current)
				{
					case Tool.None:
					case Tool.Rect: break;
					case Tool.Rotate:
					{
						RealtimeCSG.Helpers.CSGHandles.InitFunction shutdown = delegate
						{
							MergeDuplicatePoints();
							if (UpdateWorkControlMesh(forceUpdate: true))
								UpdateBackupPoints();
							else
								Undo.PerformUndo();
							InternalCSGModelManager.CheckSurfaceModifications(_brushSelection.Brushes, true);
							_usingControl = false;
						};
						
						var handleRotation = GetRealHandleRotation();
						var newRotation = PaintUtility.HandleRotation(_handleCenter, handleRotation, init, shutdown);
						if (GUI.changed)
						{
							ForceLineUpdate();
							DoRotateControlPoints(_handleCenter, handleRotation, Quaternion.Inverse(handleRotation) * newRotation);
							GUI.changed = false;
						}
						break;
					}
					case Tool.Scale:
					{
						var rotation = GetRealHandleRotation();
						RealtimeCSG.Helpers.CSGHandles.InitFunction shutdown = delegate
						{
							MergeDuplicatePoints();
							if (UpdateWorkControlMesh())
							{
								if (_editMode == EditMode.ScalePolygon)
								{
									_brushSelection.Brushes = null;
									SetTargets(CSGBrushEditorManager.FilteredSelection);
								}
								else
									UpdateBackupPoints();
							} else
								Undo.PerformUndo();
							_usingControl = false;
						};

						var newHandleScale = PaintUtility.HandleScale(_handleScale, _handleCenter, rotation, init, shutdown);
						if (GUI.changed)
						{
							ForceLineUpdate();
							var newScale = newHandleScale;
							if (float.IsInfinity(newScale.x) || float.IsNaN(newScale.x) ||
								float.IsInfinity(newScale.y) || float.IsNaN(newScale.y) ||
								float.IsInfinity(newScale.z) || float.IsNaN(newScale.z)) newScale = Vector3.zero;

							if (newScale.x <= MathConstants.EqualityEpsilon) { newScale.x = 0.0f; }
							if (newScale.y <= MathConstants.EqualityEpsilon) { newScale.y = 0.0f; }
							if (newScale.z <= MathConstants.EqualityEpsilon) { newScale.z = 0.0f; }
							
							DoScaleControlPoints(rotation, newScale, _startHandleCenter);
							_handleScale = newHandleScale;
							GUI.changed = false;
						}
						if (_usingControl)
						{
							DrawScaleBounds(camera, rotation, newHandleScale, _startHandleCenter, _handleWorldPoints);
						}
						break;
					}
					case Tool.Move:
					{
						var rotation = GetRealHandleRotation();
						RealtimeCSG.Helpers.CSGHandles.InitFunction shutdown = delegate
						{
							MergeDuplicatePoints();
							if (UpdateWorkControlMesh(forceUpdate: true))
								UpdateBackupPoints();
							else
								Undo.PerformUndo();
							InternalCSGModelManager.CheckSurfaceModifications(_brushSelection.Brushes, true);
							_usingControl = false;
						};
						var newHandleCenter = PaintUtility.HandlePosition(_handleCenter, rotation, _handleWorldPoints, init, shutdown);
						if (GUI.changed)
						{
							ForceLineUpdate();
							var offset = newHandleCenter - _handleCenter;
							_worldDeltaMovement += offset;
							DoMoveControlPoints(_worldDeltaMovement);
							_handleCenter = _startHandleCenter + _worldDeltaMovement;
							GUI.changed = false;
						}
						if (_usingControl)
						{
							var lockX	= (Mathf.Abs(_worldDeltaMovement.x) < MathConstants.ConsideredZero) && (CSGSettings.LockAxisX || (Mathf.Abs(_movePlane.a) >= 1 - MathConstants.EqualityEpsilon));
							var lockY	= (Mathf.Abs(_worldDeltaMovement.y) < MathConstants.ConsideredZero) && (CSGSettings.LockAxisY || (Mathf.Abs(_movePlane.b) >= 1 - MathConstants.EqualityEpsilon));
							var lockZ	= (Mathf.Abs(_worldDeltaMovement.z) < MathConstants.ConsideredZero) && (CSGSettings.LockAxisZ || (Mathf.Abs(_movePlane.c) >= 1 - MathConstants.EqualityEpsilon));
					
							var text	= Units.ToRoundedDistanceString(_worldDeltaMovement, lockX, lockY, lockZ);
							PaintUtility.DrawScreenText(_handleCenter, HoverTextDistance * 3, text);
						}
						break;
					}
				}

			}


            
			if (Event.current.type == EventType.Repaint)
			{
				if (_showMarquee &&
				    GUIUtility.hotControl == _rectSelectionId && 
				    camera.pixelRect.Contains(_startMousePoint))
			    {
				    PaintUtility.DrawSelectionRectangle(CameraUtility.PointsToRect(_startMousePoint, Event.current.mousePosition));
			    }
            }

			try
			{
				switch (Event.current.type)
				{
					case EventType.MouseDown:
					{
						if (!sceneRect.Contains(Event.current.mousePosition))
							break;
						if (GUIUtility.hotControl != 0 ||
							Event.current.button != 0)
							break;


						if (SelectionUtility.CurrentModifiers == EventModifiers.Shift)
						{
							if (_editMode == EditMode.MovingPolygon)
								Event.current.Use();
							break;
						}

						_doMarquee = false;
						_showMarquee = false;
						_firstMove = true;
						_extraDeltaMovement = MathConstants.zeroVector3;
						_worldDeltaMovement = MathConstants.zeroVector3;

						var newControlId = -1;
						if (_hoverOnTarget != -1 && _hoverOnTarget < _brushSelection.States.Length)
						{
							UpdateWorkControlMesh();
							switch (_editMode)
							{
								case EditMode.RotateEdge:
								{
									newControlId = _brushSelection.States[_hoverOnTarget].EdgeControlId[_hoverOnEdgeIndex];
									if (!UpdateRotationCircle())
									{
										break;
									}

									_rotateCurrentAngle = _rotateStartAngle;
									_rotateCurrentSnappedAngle = _rotateStartAngle;
											
									Undo.IncrementCurrentGroup();
									_rotationUndoGroupIndex = Undo.GetCurrentGroup();
									break;
								}
								case EditMode.MovingEdge:
								{
									newControlId = _brushSelection.States[_hoverOnTarget].EdgeControlId[_hoverOnEdgeIndex];
									break;
								}
								case EditMode.MovingPoint:
								{
									newControlId = _brushSelection.States[_hoverOnTarget].PointControlId[_hoverOnPointIndex];
									break;
								}
								case EditMode.MovingPolygon:
								{
									newControlId = _brushSelection.States[_hoverOnTarget].PolygonControlId[_hoverOnPolygonIndex];
									if (Tools.current == Tool.Scale)
									{
										_editMode = EditMode.ScalePolygon;
									}
									break;
								}
							}
									
						}
						
						if (newControlId != -1)
						{
							GUIUtility.hotControl				= newControlId;
							GUIUtility.keyboardControl			= newControlId;
							EditorGUIUtility.editingTextField	= false;
							Event.current.Use();

						} else
						//if (!doCloneDragging)
						{
							_doMarquee		= true;
							_startMousePoint = Event.current.mousePosition;
                                
							SceneView.RepaintAll();
						}
						break;
					}

					case EventType.MouseDrag:
					{
						if (_doMarquee)
						{
							if (GUIUtility.hotControl == 0)
							{
								_doMarquee = true;
								GUIUtility.hotControl = _rectSelectionId;
								GUIUtility.keyboardControl = _rectSelectionId;
								EditorGUIUtility.editingTextField = false;
							} else
								_doMarquee = false;
						}
						if (_editMode != EditMode.MovingPolygon ||
							SelectionUtility.CurrentModifiers != EventModifiers.Shift)
							break;

						ExtrudeSurface(drag: true);
						Event.current.Use();
						break;
					}

					case EventType.MouseUp:
					{
						if (_mouseIsDragging || _showMarquee)
							break;
						
						if (_editMode == EditMode.MovingPolygon)
						{
							if (SelectionUtility.CurrentModifiers == EventModifiers.Shift)
							{
								if (Tools.current == Tool.Move)
								{
									ExtrudeSurface(drag: false);
									Event.current.Use();
									break;
								}
							}
						}
						if (UpdateSelection())
							SceneView.RepaintAll();
						else
							SelectionUtility.DoSelectionClick();
						break;
					}

					case EventType.KeyDown:
					{
						if (GUIUtility.hotControl != 0)
							break;
						if (Keys.CancelActionKey.IsKeyPressed()) { Event.current.Use(); break; }
						if (Keys.SnapToGridKey.IsKeyPressed()) { Event.current.Use(); break; }
						if (Keys.MergeEdgePoints.IsKeyPressed() && HaveEdgeSelection) { Event.current.Use(); break; }
						if (Keys.HandleSceneKeyDown(CSGBrushEditorManager.CurrentTool, false)) { Event.current.Use(); break; }
						if (Keys.FlipSelectionX.IsKeyPressed()) { Event.current.Use(); break; }
						if (Keys.FlipSelectionY.IsKeyPressed()) { Event.current.Use(); break; }
						if (Keys.FlipSelectionZ.IsKeyPressed()) { Event.current.Use(); break; }
						else break;
					}

					case EventType.KeyUp:
					{
						if (GUIUtility.hotControl != 0)
							break;
						if (Keys.SnapToGridKey.IsKeyPressed()) { SnapToGrid(); Event.current.Use(); break; }
						if (Keys.MergeEdgePoints.IsKeyPressed() && HaveEdgeSelection) { MergeSelected(); Event.current.Use(); break; }
						if (Keys.HandleSceneKeyUp(CSGBrushEditorManager.CurrentTool, false)) { Event.current.Use(); break; }
						if (Keys.FlipSelectionX.IsKeyPressed()) { FlipX(); Event.current.Use(); break; }
						if (Keys.FlipSelectionY.IsKeyPressed()) { FlipY(); Event.current.Use(); break; }
						if (Keys.FlipSelectionZ.IsKeyPressed()) { FlipZ(); Event.current.Use(); break; }
						else break;
					}

					case EventType.ValidateCommand:
					{
						if (GUIUtility.hotControl != 0)
							break;
						if ((Event.current.commandName == "SoftDelete" || Event.current.commandName == "Delete") && HavePointSelection) { Event.current.Use(); break; }
						if (Keys.CancelActionKey.IsKeyPressed()) { Event.current.Use(); break; }
						if (Keys.SnapToGridKey.IsKeyPressed()) { Event.current.Use(); break; }
						if (Keys.CloneDragActivate.IsKeyPressed()) { Event.current.Use(); break; }
						if (Keys.MergeEdgePoints.IsKeyPressed() && HaveEdgeSelection) { Event.current.Use(); break; }
						if (Keys.HandleSceneValidate(CSGBrushEditorManager.CurrentTool, false)) { Event.current.Use(); break; }
						if (Keys.FlipSelectionX.IsKeyPressed()) { Event.current.Use(); break; }
						if (Keys.FlipSelectionY.IsKeyPressed()) { Event.current.Use(); break; }
						if (Keys.FlipSelectionZ.IsKeyPressed()) { Event.current.Use(); break; }
						else break;
					}

					case EventType.ExecuteCommand:
					{
						if (GUIUtility.hotControl != 0)
							break;
						if (HavePointSelection &&
                            (Event.current.commandName == "SoftDelete" || Event.current.commandName == "Delete"))
						{
							DeleteSelectedPoints();
							Event.current.Use();
							break;
						}
						break;
					}
				
					case EventType.Layout:
					{
						UpdateMouseCursor();

						if (_brushSelection.Brushes == null)
						{
							break;
						}
						if (_brushSelection.States.Length != _brushSelection.Brushes.Length)
						{
							break;
						}

						Matrix4x4 origMatrix = Handles.matrix;
						Handles.matrix = MathConstants.identityMatrix;
						try
						{
							var currentTool = Tools.current;
						
							var inSceneView = camera && camera.pixelRect.Contains(Event.current.mousePosition);
							
							UpdateLineMeshes();
							
							if (!inSceneView || _mouseIsDragging || GUIUtility.hotControl != 0)
								break;

							_hoverOnEdgeIndex	= -1;
							_hoverOnPointIndex	= -1;
							_hoverOnPolygonIndex = -1;
							_hoverOnTarget		= -1;

							var cameraPlane = GUIStyleUtility.GetNearPlane(camera);

							var hoverControl = 0;
							var hotControl = GUIUtility.hotControl;
							for (int t = 0; t < _brushSelection.Brushes.Length; t++)
							{
								//var brush = _brushSelection.Brushes[t];
								var meshState = _brushSelection.States[t];

								for (int j = 0, e = 0; j < meshState.Edges.Length; e++, j += 2)
								{
									var newControlId = meshState.EdgeControlId[e];
									if (hotControl == newControlId) hoverControl = newControlId;
									var distance = meshState.GetClosestEdgeDistance(cameraPlane, meshState.Edges[j + 0], meshState.Edges[j + 1]);
									HandleUtility.AddControl(newControlId, distance);
								}

								for (var p = 0; p < meshState.PolygonCenterPoints.Length; p++)
								{
									var newControlId = meshState.PolygonControlId[p];
									if (hotControl == newControlId) hoverControl = newControlId;
									if (camera.orthographic || meshState.PolygonCenterPointSizes[p] <= 0)
										continue;

									var center = meshState.PolygonCenterPoints[p];
									if (_useHandleCenter &&
                                            (_handleCenter - center).sqrMagnitude <= MathConstants.EqualityEpsilonSqr)
										continue;
										
									var radius = meshState.PolygonCenterPointSizes[p] * 1.2f;
									var centerDistance = HandleUtility.DistanceToCircle(center, radius);
									HandleUtility.AddControl(newControlId, centerDistance);
								}

								for (var p = 0; p < meshState.WorldPoints.Length; p++)
								{
									var newControlId = meshState.PointControlId[p];
									if (hotControl == newControlId) hoverControl = newControlId;
									
									var center = meshState.WorldPoints[p];
									if (_useHandleCenter &&
                                            (_handleCenter - center).sqrMagnitude <= MathConstants.EqualityEpsilonSqr)
										continue;

									var radius = meshState.WorldPointSizes[p] * 1.2f;
									var distance = HandleUtility.DistanceToCircle(center, radius);
									HandleUtility.AddControl(newControlId, distance);
								}
							}

							try
							{
								var closestBrushIndex = -1;
								var closestSurfaceIndex = -1;
								_brushSelection.FindClosestIntersection(out closestBrushIndex, out closestSurfaceIndex);
								if (closestBrushIndex != -1)
								{
									var meshState	 = _brushSelection.States[closestBrushIndex];
									var newControlId = meshState.PolygonControlId[closestSurfaceIndex];
									HandleUtility.AddControl(newControlId, 5.0f);
								}
							}
							catch
							{}
							
							var nearestControl = HandleUtility.nearestControl;
							if (nearestControl == _meshEditBrushToolId) nearestControl = 0; // liar

							if (hoverControl != 0) nearestControl = hoverControl;
							else if (hotControl != 0) nearestControl = 0;

							var doRepaint = false;
							if (nearestControl == 0)
							{
							    _brushSelection.BackupSelection();
                                _brushSelection.UnHoverAll();
							    doRepaint = _brushSelection.HasSelectionChanged();
								_editMode = EditMode.None;
							} else
							{
								var newEditMode = EditMode.None;
                                _brushSelection.BackupSelection();
                                _brushSelection.UnHoverAll();
                                for (var t = 0; t < _brushSelection.Brushes.Length; t++)
								{
									var meshState = _brushSelection.States[t];
									if (newEditMode == EditMode.None)
									{
										if (newEditMode == EditMode.None && 
												currentTool != Tool.Rotate)
										{
											for (var p = 0; p < meshState.WorldPoints.Length; p++)
											{
												if (meshState.PointControlId[p] != nearestControl)
													continue;

												var worldPoint = meshState.WorldPoints[p];
												for (var t2 = 0; t2 < _brushSelection.Brushes.Length; t2++)
												{
													if (t2 == t)
														continue;

													var meshState2 = _brushSelection.States[t2];
													for (var p2 = 0; p2 < meshState2.WorldPoints.Length; p2++)
													{
														var worldPoint2 = meshState2.WorldPoints[p2];
														if ((worldPoint- worldPoint2).sqrMagnitude < MathConstants.EqualityEpsilonSqr)
														{
															meshState2.PointSelectState[p2] |= SelectState.Hovering;
															break;
														}
													}
												}
												newEditMode = HoverOnPoint(meshState, t, p);
												break;
											}
										}

										if (newEditMode == EditMode.None)
										{
											for (var p = 0; p < meshState.PolygonCenterPoints.Length; p++)
											{
												if (meshState.PolygonControlId[p] != nearestControl)
													continue;
												
												newEditMode = HoverOnPolygon(meshState, t, p);
												break;
											}
										}

										if (newEditMode == EditMode.None)
										{
											for (var e = 0; e < meshState.EdgeControlId.Length; e++)
											{
												if (meshState.EdgeControlId[e] != nearestControl)
													continue;
												
												newEditMode = HoverOnEdge(meshState, t, e);
												break;
											}
										}
									}
                                }
                                doRepaint = _brushSelection.HasSelectionChanged();
                                _editMode = newEditMode;
							}

							if (doRepaint)
							{
								ForceLineUpdate();
								SceneView.RepaintAll();
							}
						}
						finally
						{
							Handles.matrix = origMatrix;
						}
						break;
					}
				}

				var currentHotControl = GUIUtility.hotControl;
				if (currentHotControl == _rectSelectionId)
				{
					var type = Event.current.GetTypeForControl(_rectSelectionId);
					switch (type)
					{
						case EventType.MouseDrag:
						{
							if (Event.current.button != 0)
								break;
							
							//Debug.Log(editMode);
							if (!_showMarquee)
							{
							    if ((_startMousePoint - Event.current.mousePosition).sqrMagnitude >
							            (MathConstants.MinimumMouseMovement * MathConstants.MinimumMouseMovement))
                                {
                                    _brushSelection.BackupSelection();
                                    _showMarquee = true;
							    }
							    break;
							}

							Event.current.Use();

						    if (_brushSelection != null &&
                                _brushSelection.States.Length == _brushSelection.Brushes.Length)
						    {
                                var selectionType = SelectionUtility.GetEventSelectionType();
                                SelectMarquee(camera, CameraUtility.PointsToRect(_startMousePoint, Event.current.mousePosition), selectionType);
						    }
						    SceneView.RepaintAll();
							break;
						}
						case EventType.MouseUp:
						{
							_movePlaneInNormalDirection = false;
							_doMarquee = false;
							_showMarquee = false;
							
							_startCamera = null;
							Grid.ForceGrid = false;

							GUIUtility.hotControl = 0;
							GUIUtility.keyboardControl = 0;
							EditorGUIUtility.editingTextField = false;
							Event.current.Use();

							if (!_mouseIsDragging || !_showMarquee)
							{
								break;
							}

                            if (_brushSelection != null &&
                                _brushSelection.States.Length == _brushSelection.Brushes.Length)
                            {
                                var selectionType = SelectionUtility.GetEventSelectionType();
                                SelectMarquee(camera, CameraUtility.PointsToRect(_startMousePoint, Event.current.mousePosition), selectionType);
                            }
                            break;
						}
					}
				} else
				if (_brushSelection.States != null)
				{
					for (var t = 0; t < _brushSelection.States.Length; t++)
					{
						var meshState = _brushSelection.States[t];

						for (int p = 0; p < meshState.WorldPoints.Length; p++)
						{
							if (currentHotControl == meshState.PointControlId[p])
							{
								var type = Event.current.GetTypeForControl(meshState.PointControlId[p]);
								switch (type)
								{
									case EventType.KeyDown:
									{
										if (Keys.MergeEdgePoints.IsKeyPressed() && HaveEdgeSelection) { Event.current.Use(); break; }
										break;
									}

									case EventType.ValidateCommand:
									{
										if ((Event.current.commandName == "SoftDelete" || Event.current.commandName == "Delete") && HavePointSelection) { Event.current.Use(); break; }
										if (Keys.MergeEdgePoints.IsKeyPressed()) { Event.current.Use(); break; }
										break;
									}

									case EventType.KeyUp:
									{
										if (Keys.MergeEdgePoints.IsKeyPressed()) { MergeSelected(); Event.current.Use(); break; }
										break;
									}
									
									case EventType.ExecuteCommand:
									{
									    if (HavePointSelection &&
                                            (Event.current.commandName == "SoftDelete" || Event.current.commandName == "Delete"))
									    {
									        DeleteSelectedPoints(); Event.current.Use();
                                            break;
									    }
										break;
									}

									case EventType.MouseDrag:
									{
										if (Event.current.button != 0)
											break;
										
										if (Tools.current == Tool.Scale)
											break;

										//Debug.Log(editMode);
										Event.current.Use();
										if (_firstMove)
										{
											_extraDeltaMovement = MathConstants.zeroVector3;
											_worldDeltaMovement = MathConstants.zeroVector3;
											_startCamera = camera;
											UpdateTransformMatrices();
											UpdateSelection(allowSubstraction: false);
										}
			
										if (//_prevYMode != Grid.YMoveModeActive || 
												_firstMove)
										{
											//_prevYMode = Grid.YMoveModeActive;
											if (_firstMove)
												_originalPoint = meshState.WorldPoints[p];
											UpdateWorkControlMesh();
											UpdateBackupPoints();
											UpdateGrid(_startCamera);
											_firstMove = true;
											_extraDeltaMovement += _worldDeltaMovement;
										}
																		
										var lockX = (CSGSettings.LockAxisX || (Mathf.Abs(_movePlane.a) >= 1 - MathConstants.EqualityEpsilon));
										var lockY = (CSGSettings.LockAxisY || (Mathf.Abs(_movePlane.b) >= 1 - MathConstants.EqualityEpsilon));
										var lockZ = (CSGSettings.LockAxisZ || (Mathf.Abs(_movePlane.c) >= 1 - MathConstants.EqualityEpsilon));					
			
										if (CSGSettings.SnapVector == MathConstants.zeroVector3)
										{
											CSGBrushEditorManager.ShowMessage("Positional snapping is set to zero, cannot move.");
											break;
										} else
										if (lockX && lockY && lockZ)
										{
											CSGBrushEditorManager.ShowMessage("All axi are disabled (X Y Z), cannot move.");
											break;
										}
										CSGBrushEditorManager.ResetMessage();	

										var mouseRay		= HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
										var intersection	= _movePlane.Intersection(mouseRay);
										if (float.IsNaN(intersection.x) || float.IsNaN(intersection.y) || float.IsNaN(intersection.z))
											break;

										intersection			= GeometryUtility.ProjectPointOnPlane(_movePlane, intersection);
			
										if (_firstMove)
										{
											_originalPoint = intersection;
											_worldDeltaMovement = MathConstants.zeroVector3;
											_firstMove = false;
										} else
										{
											_worldDeltaMovement = SnapMovementToPlane(intersection - _originalPoint);
										}
						
										// try to snap selected points against non-selected points
										var doSnapping = CSGSettings.SnapToGrid ^ SelectionUtility.IsSnappingToggled;
										if (doSnapping)
										{
											var worldPoints = _brushSelection.GetSelectedWorldPoints();
											//for (int i = 0; i < worldPoints.Length; i++)
											//	worldPoints[i] = GeometryUtility.ProjectPointOnPlane(movePlane, worldPoints[i]);// - center));
											_worldDeltaMovement = Grid.SnapDeltaToGrid(_worldDeltaMovement, worldPoints, snapToSelf: true);
										} else
										{
											_worldDeltaMovement = Grid.HandleLockedAxi(_worldDeltaMovement);
										}

										DoMoveControlPoints(_worldDeltaMovement);
										CenterPositionHandle();
										SceneView.RepaintAll();
										break;
									}
									case EventType.MouseUp:
									{
										_movePlaneInNormalDirection = false;
									
										_startCamera = null;
										Grid.ForceGrid = false;

										GUIUtility.hotControl = 0;
										GUIUtility.keyboardControl = 0;
										EditorGUIUtility.editingTextField = false;
										Event.current.Use();

										if (!_mouseIsDragging)
											break;

										MergeDuplicatePoints();
										if (!UpdateWorkControlMesh())
										{				
											Undo.PerformUndo();
										} else
										{
											UpdateBackupPoints();
										}
										break;
									}
									case EventType.Repaint:
									{
										if (Tools.current == Tool.Scale)
											break;
										if (_editMode != EditMode.ScalePolygon)
											RenderOffsetText();
										break;
									}
								}
								break;
							}
						}

						for (int e = 0; e < meshState.Edges.Length / 2; e++)
						{
							if (currentHotControl == meshState.EdgeControlId[e])
							{
								var type = Event.current.GetTypeForControl(meshState.EdgeControlId[e]);
								switch (type)
								{
									case EventType.KeyDown:
									{
										if (Keys.MergeEdgePoints.IsKeyPressed() && HaveEdgeSelection) { Event.current.Use(); break; }
										break;
									}

									case EventType.ValidateCommand:
									{
										if ((Event.current.commandName == "SoftDelete" || Event.current.commandName == "Delete") && HavePointSelection) { Event.current.Use(); break; }
										if (Keys.MergeEdgePoints.IsKeyPressed()) { Event.current.Use(); break; }
										break;
									}

									case EventType.KeyUp:
									{
										if (Keys.MergeEdgePoints.IsKeyPressed()) { MergeSelected(); Event.current.Use(); break; }
										break;
									}
									
									case EventType.ExecuteCommand:
									{
									    if (HavePointSelection &&
                                            (Event.current.commandName == "SoftDelete" || Event.current.commandName == "Delete"))
									    {
									        DeleteSelectedPoints(); Event.current.Use();
                                            break;
									    }
										break;
									}

									case EventType.MouseDrag:
									{
										if (Event.current.button != 0)
											break;

										//Debug.Log(editMode);
										Event.current.Use();
										if (_editMode == EditMode.RotateEdge)
										{
											if (_rotateBrushParent == null)
												break;

											if ((CSGSettings.SnapRotation % 360) <= 0)
											{
												CSGBrushEditorManager.ShowMessage("Rotational snapping is set to zero, cannot rotate.");
												break;
											}
											CSGBrushEditorManager.ResetMessage();

											var ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
											var newMousePos = _rotatePlane.Intersection(ray);

											_rotateCurrentAngle = GeometryUtility.SignedAngle(_rotateCenter - _rotateStart, _rotateCenter - newMousePos, _rotateNormal);
											_rotateCurrentSnappedAngle = GridUtility.SnappedAngle(_rotateCurrentAngle - _rotateStartAngle,
																									SelectionUtility.IsSnappingToggled) + _rotateStartAngle;
												
											DoRotateBrushes(_rotateCenter, _rotateNormal, _rotateCurrentSnappedAngle - _rotateStartAngle);
											SceneView.RepaintAll();
											break;
										}

										if (Tools.current != Tool.Move &&
											Tools.current != Tool.Scale)
											break;

                                        if (_firstMove)
                                        {
                                            _extraDeltaMovement = MathConstants.zeroVector3;
											_worldDeltaMovement = MathConstants.zeroVector3;
											_startCamera = camera;
											UpdateTransformMatrices();
											UpdateSelection(allowSubstraction: false);
                                        }
			
										if (//_prevYMode != Grid.YMoveModeActive || 
											_firstMove)
                                        {
                                            //_prevYMode = Grid.YMoveModeActive;
                                            //if (_firstMove)
                                            {
												var originalVertexIndex1 = meshState.Edges[(e * 2) + 0];
												var originalVertexIndex2 = meshState.Edges[(e * 2) + 1];
												var originalVertex1 = meshState.WorldPoints[originalVertexIndex1];
												var originalVertex2 = meshState.WorldPoints[originalVertexIndex2];

												var ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);

												float squaredDist, s;
												Vector3 closestRay;
												_originalPoint = MathUtils.ClosestPtSegmentRay(originalVertex1, originalVertex2, ray, out squaredDist, out s, out closestRay);
											}
											UpdateWorkControlMesh();
											UpdateBackupPoints();
											UpdateGrid(_startCamera);
											_firstMove = true;
											_extraDeltaMovement += _worldDeltaMovement;
                                        }

										var lockX = (CSGSettings.LockAxisX || (Mathf.Abs(_movePlane.a) >= 1 - MathConstants.EqualityEpsilon));
										var lockY = (CSGSettings.LockAxisY || (Mathf.Abs(_movePlane.b) >= 1 - MathConstants.EqualityEpsilon));
										var lockZ = (CSGSettings.LockAxisZ || (Mathf.Abs(_movePlane.c) >= 1 - MathConstants.EqualityEpsilon));					
			
										if (CSGSettings.SnapVector == MathConstants.zeroVector3)
										{
											CSGBrushEditorManager.ShowMessage("Positional snapping is set to zero, cannot move.");
											break;
										} else
										if (lockX && lockY && lockZ)
										{
											CSGBrushEditorManager.ShowMessage("All axi are disabled (X Y Z), cannot move.");
											break;
										}
										CSGBrushEditorManager.ResetMessage();

										var mouseRay		= HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
										var intersection	= _movePlane.Intersection(mouseRay);
										if (float.IsNaN(intersection.x) || float.IsNaN(intersection.y) || float.IsNaN(intersection.z))
											break;

										intersection			= GeometryUtility.ProjectPointOnPlane(_movePlane, intersection);
                                            
                                        if (_firstMove)
										{
											_originalPoint = intersection;
											_worldDeltaMovement = MathConstants.zeroVector3;
											

											_handleWorldPoints = _brushSelection.GetSelectedWorldPoints();
                                            _dragEdgeScale = Vector3.one;
											_dragEdgeRotation = GetRealHandleRotation();

											var rotation			= _dragEdgeRotation;
											var inverseRotation		= Quaternion.Inverse(rotation);

											var delta		= (_originalPoint - _handleCenter);
											var distance	= delta.magnitude;
											_startHandleDirection = (delta / distance);
											_startHandleDirection = GeometryUtility.SnapToClosestAxis(inverseRotation * _startHandleDirection);
											_startHandleDirection = rotation * _startHandleDirection;
											
											_startHandleCenter = _handleCenter;

											_firstMove = false;
										} else
										{
											_worldDeltaMovement = SnapMovementToPlane(intersection - _originalPoint);
										}
			
										// try to snap selected points against non-selected points
										var doSnapping = CSGSettings.SnapToGrid ^ SelectionUtility.IsSnappingToggled;
										if (doSnapping)
										{
										    var worldPoints = _handleWorldPoints;
											//for (int i = 0; i < worldPoints.Length; i++)
											//	worldPoints[i] = GeometryUtility.ProjectPointOnPlane(movePlane, worldPoints[i]);// - center));
											_worldDeltaMovement = Grid.SnapDeltaToGrid(_worldDeltaMovement, worldPoints, snapToSelf: true);
										} else
										{
											_worldDeltaMovement = Grid.HandleLockedAxi(_worldDeltaMovement);
										}

										if (Tools.current == Tool.Move)
										{
											DoMoveControlPoints(_worldDeltaMovement);
										}
										if (Tools.current == Tool.Scale)
										{
											var rotation			= _dragEdgeRotation;
											var inverseRotation		= Quaternion.Inverse(rotation);
											
											var start	= GeometryUtility.ProjectPointOnInfiniteLine(_originalPoint, _startHandleCenter, _startHandleDirection);
											var end		= GeometryUtility.ProjectPointOnInfiniteLine(intersection, _startHandleCenter, _startHandleDirection);
											
												
											var oldDistance	= inverseRotation * (start - _startHandleCenter);
											var newDistance	= inverseRotation * (end - _startHandleCenter);
											if (Mathf.Abs(oldDistance.x) > MathConstants.DistanceEpsilon) _dragEdgeScale.x = newDistance.x / oldDistance.x;
											if (Mathf.Abs(oldDistance.y) > MathConstants.DistanceEpsilon) _dragEdgeScale.y = newDistance.y / oldDistance.y;
											if (Mathf.Abs(oldDistance.z) > MathConstants.DistanceEpsilon) _dragEdgeScale.z = newDistance.z / oldDistance.z;
											
											if (float.IsNaN(_dragEdgeScale.x) || float.IsInfinity(_dragEdgeScale.x)) _dragEdgeScale.x = 1.0f;
											if (float.IsNaN(_dragEdgeScale.y) || float.IsInfinity(_dragEdgeScale.y)) _dragEdgeScale.y = 1.0f;
											if (float.IsNaN(_dragEdgeScale.z) || float.IsInfinity(_dragEdgeScale.z)) _dragEdgeScale.z = 1.0f;
											
											_dragEdgeScale.x = Mathf.Round(_dragEdgeScale.x / CSGSettings.SnapScale) * CSGSettings.SnapScale;
											_dragEdgeScale.y = Mathf.Round(_dragEdgeScale.y / CSGSettings.SnapScale) * CSGSettings.SnapScale;
											_dragEdgeScale.z = Mathf.Round(_dragEdgeScale.z / CSGSettings.SnapScale) * CSGSettings.SnapScale;

											DoScaleControlPoints(rotation, _dragEdgeScale, _startHandleCenter);
										}
										CenterPositionHandle();
										SceneView.RepaintAll();
										break;
									}
									case EventType.MouseUp:
									{
										_movePlaneInNormalDirection = false;
			 
										_startCamera = null;
										Grid.ForceGrid = false;
										EditorGUIUtility.SetWantsMouseJumping(0);
										GUIUtility.hotControl = 0;
										GUIUtility.keyboardControl = 0;
										EditorGUIUtility.editingTextField = false;
										Event.current.Use();

										if (!_mouseIsDragging)
											break;

										if (_editMode == EditMode.RotateEdge)
										{
											Undo.CollapseUndoOperations(_rotationUndoGroupIndex);
											UpdateWorkControlMesh();
										}

										MergeDuplicatePoints();
										if (!UpdateWorkControlMesh(forceUpdate: true))
										{				
											Undo.PerformUndo();
										} else
										{
											UpdateBackupPoints();
										}
										InternalCSGModelManager.CheckSurfaceModifications(_brushSelection.Brushes, true);
										break;
									}
									case EventType.Repaint:
									{
										if (Tools.current == Tool.Move)
										{
											if (_editMode != EditMode.RotateEdge)
												RenderOffsetText();
										} else
										if (Tools.current == Tool.Scale)
										{
											if (_handleWorldPoints == null)
												return;

											var realScale = _dragEdgeScale;
											if (realScale.x < 0) realScale.x = 0;
											if (realScale.y < 0) realScale.y = 0;
											if (realScale.z < 0) realScale.z = 0;

											DrawScaleBounds(camera, _dragEdgeRotation, realScale, _startHandleCenter, _handleWorldPoints);
										}
										break;
									}
								}
								break;
							}
						}

						for (int p = 0; p < meshState.PolygonCenterPoints.Length; p++)
						{
							if (currentHotControl == meshState.PolygonControlId[p])
							{
								var type = Event.current.GetTypeForControl(meshState.PolygonControlId[p]);
								switch (type)
								{
									case EventType.KeyDown:
									{
										if (Keys.MergeEdgePoints.IsKeyPressed() && HaveEdgeSelection) { Event.current.Use(); break; }
										break;
									}

									case EventType.ValidateCommand:
									{
										if ((Event.current.commandName == "SoftDelete" || Event.current.commandName == "Delete") && HavePointSelection) { Event.current.Use(); break; }
										if (Keys.MergeEdgePoints.IsKeyPressed()) { Event.current.Use(); break; }
										break;
									}

									case EventType.KeyUp:
									{
										if (Keys.MergeEdgePoints.IsKeyPressed()) { MergeSelected(); Event.current.Use(); break; }
										break;
									}
									
									case EventType.ExecuteCommand:
									{
									    if (HavePointSelection &&
                                            (Event.current.commandName == "SoftDelete" || Event.current.commandName == "Delete"))
									    {
									        DeleteSelectedPoints(); Event.current.Use();
                                            break;
									    }
										break;
									}

									case EventType.MouseDrag:
									{					
										if (Event.current.button != 0)
											break;

										//Debug.Log(editMode);
										Event.current.Use();
										
										if (_firstMove)
										{
											EditorGUIUtility.SetWantsMouseJumping(1);
											_mousePosition = Event.current.mousePosition;
											_extraDeltaMovement = MathConstants.zeroVector3;
											_worldDeltaMovement = MathConstants.zeroVector3;
											_startCamera = camera;
											UpdateTransformMatrices();
											UpdateSelection(allowSubstraction: false);
										} else
										{
											_mousePosition += Event.current.delta;
										}
			
										if (//_prevYMode != Grid.YMoveModeActive || 
												_firstMove)
										{
											//_prevYMode = Grid.YMoveModeActive;
											if (_firstMove)
											{
												_originalPoint = meshState.PolygonCenterPoints[p];
												//UpdateWorkControlMesh();
											}
											UpdateBackupPoints();
											UpdateGrid(_startCamera);
											_firstMove = true;
											_extraDeltaMovement += _worldDeltaMovement;
										}
			
										var lockX = (CSGSettings.LockAxisX || (Mathf.Abs(_movePlane.a) >= 1 - MathConstants.EqualityEpsilon));
										var lockY = (CSGSettings.LockAxisY || (Mathf.Abs(_movePlane.b) >= 1 - MathConstants.EqualityEpsilon));
										var lockZ = (CSGSettings.LockAxisZ || (Mathf.Abs(_movePlane.c) >= 1 - MathConstants.EqualityEpsilon));					
			
										if (CSGSettings.SnapVector == MathConstants.zeroVector3)
										{
											CSGBrushEditorManager.ShowMessage("Positional snapping is set to zero, cannot move.");
											break;
										} else
										if (lockX && lockY && lockZ)
										{
											CSGBrushEditorManager.ShowMessage("All axi are disabled (X Y Z), cannot move.");
											break;
										}
										CSGBrushEditorManager.ResetMessage();

										var mouseRay		= HandleUtility.GUIPointToWorldRay(_mousePosition);
										var intersection	= _movePlane.Intersection(mouseRay);
										if (float.IsNaN(intersection.x) || float.IsNaN(intersection.y) || float.IsNaN(intersection.z))
											break;
										
										if (_movePlaneInNormalDirection && _editMode != EditMode.ScalePolygon)
										{
											intersection	= GridUtility.CleanPosition(intersection);
											intersection	= GeometryUtility.ProjectPointOnInfiniteLine(intersection, _movePolygonOrigin, _movePolygonDirection);
										} else
										{
											intersection	= GeometryUtility.ProjectPointOnPlane(_movePlane, intersection);
										}

										if (_firstMove)
										{
											_originalPoint = intersection;
											_worldDeltaMovement = MathConstants.zeroVector3;
											_firstMove = false;
										} else
										{
											_worldDeltaMovement = SnapMovementToPlane(intersection - _originalPoint);
										}

										// try to snap selected points against non-selected points
										var doSnapping = CSGSettings.SnapToGrid ^ SelectionUtility.IsSnappingToggled;
										if (doSnapping)
										{
											var worldPoints = _brushSelection.GetSelectedWorldPoints();
											if (_movePlaneInNormalDirection && _editMode != EditMode.ScalePolygon)
											{
												var worldLineOrg	= _movePolygonOrigin;
												var worldLineDir	= _movePolygonDirection;
												_worldDeltaMovement = Grid.SnapDeltaToRay(new Ray(worldLineOrg, worldLineDir), _worldDeltaMovement, worldPoints);
											} else
											{
												//for (int i = 0; i < worldPoints.Length; i++)
												//	worldPoints[i] = GeometryUtility.ProjectPointOnPlane(movePlane, worldPoints[i]);// - center));
												_worldDeltaMovement = Grid.SnapDeltaToGrid(_worldDeltaMovement, worldPoints, snapToSelf: true);
											}
										} else
										{
											_worldDeltaMovement = Grid.HandleLockedAxi(_worldDeltaMovement);
										}

										switch (_editMode)
										{
											case EditMode.MovingPolygon: DoMoveControlPoints(_worldDeltaMovement); break;
										//	case EditMode.ScalePolygon:  DoScaleControlPoints(worldDeltaMovement, meshState.polygonCenterPoints[p]); break;
										}
										CenterPositionHandle();
										SceneView.RepaintAll();
										break;
									}
									case EventType.MouseUp:
									{
										_movePlaneInNormalDirection = false;
									
										_startCamera = null;
										Grid.ForceGrid = false;

										EditorGUIUtility.SetWantsMouseJumping(0);
										GUIUtility.hotControl = 0;
										GUIUtility.keyboardControl = 0;
										EditorGUIUtility.editingTextField = false;
										Event.current.Use();

										if (!_mouseIsDragging)
											break;

										MergeDuplicatePoints();
										if (!UpdateWorkControlMesh())
										{				
											Undo.PerformUndo();
										} else
										{
											if (_editMode == EditMode.ScalePolygon)
											{
												_brushSelection.Brushes = null;
												SetTargets(CSGBrushEditorManager.FilteredSelection); 
											} else
												UpdateBackupPoints();
										}
										break;
									}
									case EventType.Repaint:
									{
										if (_editMode != EditMode.ScalePolygon)
											RenderOffsetText();
										break;
									}
								}
								break;
							}
						}
					}
				}
			}
			finally
			{ 
				if (originalEventType == EventType.MouseUp) { _mouseIsDragging = false; }
			}
		}


































		
		public void OnInspectorGUI(EditorWindow window)
		{
			MeshToolGUI.OnInspectorGUI(window);
		}
		
		public Rect GetLastSceneGUIRect()
		{
			return MeshToolGUI.GetLastSceneGUIRect(this);
		}

		public bool OnSceneGUI()
		{
			if (_brushSelection.Brushes == null)
				return false;
			
			MeshToolGUI.OnSceneGUI(this);
			return true;
		}
	}
}
