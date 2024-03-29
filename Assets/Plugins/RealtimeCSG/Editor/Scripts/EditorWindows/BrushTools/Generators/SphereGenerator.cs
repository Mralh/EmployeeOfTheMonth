﻿using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using InternalRealtimeCSG;

namespace RealtimeCSG
{
    internal sealed class SphereGenerator : GeneratorBase, IBrushGenerator
	{
		[NonSerialized] Vector3		worldPosition;
		[NonSerialized] Vector3		prevWorldPosition;
		[NonSerialized] CSGPlane?	hoverDefaultPlane;
		
		[NonSerialized] CSGPlane[]	firstSnappedPlanes	= null;
		[NonSerialized] Vector3[]	firstSnappedEdges	= null;
		[NonSerialized] CSGBrush	firstSnappedBrush	= null;
		[NonSerialized] bool		hadSphere = false;
		[NonSerialized] bool		prevIsHemisphere = false;
		

		[NonSerialized] int			prevSplits = -1;
		[NonSerialized] ControlMesh splitControlMesh;
		[NonSerialized] Shape		splitShape;
		[NonSerialized] uint?		sphereSmoothingGroup = null;

		// sphere specific
		[SerializeField] SphereSettings		settings		= new SphereSettings();

		protected override IShapeSettings ShapeSettings { get { return settings; } }

		public int		SphereSplits
		{
			get { return settings.sphereSplits; }
			set { if (settings.sphereSplits == value) return; Undo.RecordObject(this, "Modified Sphere Splits"); settings.sphereSplits = value; UpdateBaseShape(); }
		}

		public float     SphereOffset
		{ 
			get { return settings.sphereOffset; }
			set { if (settings.sphereOffset == value) return; Undo.RecordObject(this, "Modified Sphere Offset"); settings.sphereOffset = value; UpdateBaseShape(); }
		}

		public bool		SphereSmoothShading
		{
			get { return settings.sphereSmoothShading; }
			set { if (settings.sphereSmoothShading == value) return; Undo.RecordObject(this, "Modified Sphere Smoothing"); settings.sphereSmoothShading = value; UpdateBaseShape(); }
		}

		public bool		IsHemiSphere
		{
			get { return settings.isHemiSphere; }
			set { if (settings.isHemiSphere == value) return; Undo.RecordObject(this, "Modified Sphere Smoothing"); settings.isHemiSphere = value; UpdateBaseShape(); }
		}

		public float	SphereRadius
		{
			get { return settings.SphereRadius; }
			set { if (settings.SphereRadius == value) return; Undo.RecordObject(this, "Modified Sphere Radius"); settings.SphereRadius = value; UpdateBaseShape(); }
		}

		public override void PerformDeselectAll() { Cancel(); }
		public override void PerformDelete() { Cancel(); }

		public override void Init()
		{
			base.Init();
			Reset();
		}

		public override void Reset() 
		{
            settings.Reset();
			base.Reset();
			firstSnappedPlanes	= null;
			firstSnappedEdges	= null;
			firstSnappedBrush	= null;
			hadSphere = false;		
			prevSplits = -1;
			prevIsHemisphere = IsHemiSphere;
			splitControlMesh = null;
			splitShape = null;
			sphereSmoothingGroup = null;
		}

		public bool HotKeyReleased()
		{
			ResetVisuals();
			switch (editMode)
			{
				default:
				{
					return true;
				}
				case EditMode.CreateShape:
				{
					if (settings.vertices.Length == 1)
					{
						settings.AddPoint(worldPosition);
					}
					
					if ((settings.vertices[0] - settings.vertices[1]).sqrMagnitude <= MathConstants.EqualityEpsilon)
					{
						Cancel();
						return false;
					}
					return StartEditMode();
				}
				case EditMode.CreatePlane:
				{
					Cancel();
					return false;
				}
			}
		}

		protected override void MoveShape(Vector3 offset)
		{
			settings.MoveShape(offset);
		}

		
		void PaintRadiusMessage()
		{
			if (settings.vertices == null || settings.vertices.Length == 0)
				return;

			var endPosition = settings.vertices.Length == 1 ? worldPosition : settings.vertices[1];
			var centerPoint	= settings.vertices[0];
			var delta		= (endPosition - centerPoint).normalized;

			PaintUtility.DrawLength("radius: ", HandleUtility.GetHandleSize(centerPoint), Matrix4x4.identity, Vector3.Cross(buildPlane.normal, delta), centerPoint, endPosition, Color.white);
		}

		void PaintSquare()
		{
			var wireframeColor = ColorSettings.BoundsOutlines;

			var endPosition = settings.vertices.Length == 1 ? worldPosition : settings.vertices[1];
			var centerPoint = settings.vertices[0];
			var height		= (endPosition - centerPoint).magnitude;

			var upVector = buildPlane.normal * height;

			var point0 = settings.vertices[0] + (gridTangent * settings.sphereRadius) + (gridBinormal * settings.sphereRadius);
			var point1 = settings.vertices[0] + (gridTangent * settings.sphereRadius) - (gridBinormal * settings.sphereRadius);
			var point2 = settings.vertices[0] - (gridTangent * settings.sphereRadius) - (gridBinormal * settings.sphereRadius);
			var point3 = settings.vertices[0] - (gridTangent * settings.sphereRadius) + (gridBinormal * settings.sphereRadius);
			
			var point4 = point0;
			var point5 = point1;
			var point6 = point2;
			var point7 = point3;

            var point8 = point0 + upVector;
            var point9 = point1 + upVector;
            var pointA = point2 + upVector;
            var pointB = point3 + upVector;
			if (!IsHemiSphere)
			{
				point4 -= upVector;
				point5 -= upVector;
				point6 -= upVector;
				point7 -= upVector;
			}

			var points = new Vector3[] { point8, point9, point9, pointA, pointA, pointB, pointB, point8,
										point4, point5, point5, point6, point6, point7, point7, point4,
										point8, point4, point9, point5, pointA, point6, pointB, point7};

			PaintUtility.DrawDottedLines(points, wireframeColor, 4.0f);

			if (settings.vertices.Length == 0)
				return;

			{
				var camera = Camera.current;
				var volume = new Vector3[8];

				var localBounds = new AABB();
				localBounds.Reset();
				localBounds.Add(toGridQuaternion * (point0 + upVector));
				localBounds.Add(toGridQuaternion * (point1 + upVector));
				localBounds.Add(toGridQuaternion * (point2 + upVector));
				localBounds.Add(toGridQuaternion * (point3 + upVector));
				if (IsHemiSphere)
					localBounds.Add(toGridQuaternion * point3);
				else
					localBounds.Add(toGridQuaternion * (point3 - upVector));
				BoundsUtilities.GetBoundsVertices(localBounds, volume);
				
				PaintUtility.RenderBoundsSizes(toGridQuaternion, fromGridQuaternion, camera, volume, Color.white, Color.white, Color.white, true, true, true);
			}
		}

		void PaintCircle(int id)
		{
			var temp		= Handles.color;
			var origMatrix	= Handles.matrix;
					
			Handles.matrix = MathConstants.identityMatrix;
			var rotation = Camera.current.transform.rotation;

			bool isValid;
			var realVertices = settings.GetVertices(buildPlane, worldPosition, gridTangent, gridBinormal, out isValid);
			if (realVertices != null && realVertices.Length >= 3)
			{
				var wireframeColor = ColorSettings.WireframeOutline;
				if (!shapeIsValid || !isValid)
					wireframeColor = Color.red;
				
				for (int i = 1; i < realVertices.Length; i++)
				{
					PaintUtility.DrawLine(realVertices[i - 1], realVertices[i], ToolConstants.oldLineScale, wireframeColor);
					PaintUtility.DrawDottedLine(realVertices[i - 1], realVertices[i], wireframeColor, 4.0f);
				}

				PaintUtility.DrawLine(realVertices[realVertices.Length - 1], realVertices[0], ToolConstants.oldLineScale, wireframeColor);
				PaintUtility.DrawDottedLine(realVertices[realVertices.Length - 1], realVertices[0], wireframeColor, 4.0f);
				
				if (realVertices.Length >= 3)
				{
					var color = ColorSettings.ShapeDrawingFill;
					PaintUtility.DrawPolygon(MathConstants.identityMatrix, realVertices, color);
				}

				PaintSquare();
			}



			if (settings.vertices != null && settings.vertices.Length > 0)
			{
				Handles.color = ColorSettings.PointInnerStateColor[0];
				for (int i = 0; i < settings.vertices.Length; i++)
				{
					float handleSize = GUIStyleUtility.GetHandleSize(settings.vertices[i]);
					float scaledHandleSize = handleSize * ToolConstants.handleScale;
					PaintUtility.SquareDotCap(id, settings.vertices[i], rotation, scaledHandleSize);
				}
				PaintRadiusMessage();
			}
						
			Handles.color = ColorSettings.PointInnerStateColor[3];
			{
				float handleSize = GUIStyleUtility.GetHandleSize(worldPosition);
				float scaledHandleSize = handleSize * ToolConstants.handleScale;
				PaintUtility.SquareDotCap(id, worldPosition, rotation, scaledHandleSize);
			}
			
			Handles.matrix = origMatrix;
			Handles.color = temp;
		}

		void CreateSnappedPlanes()
		{
			firstSnappedPlanes = new CSGPlane[firstSnappedEdges.Length / 2];

			for (int i = 0; i < firstSnappedEdges.Length; i += 2)
			{
				var point0 = firstSnappedEdges[i + 0];
				var point1 = firstSnappedEdges[i + 1];

				var binormal = (point1 - point0).normalized;
				var tangent  = buildPlane.normal;
				var normal	 = Vector3.Cross(binormal, tangent);

				var worldPlane	= new CSGPlane(normal, point0);
				// note, we use 'inverse' of the worldToLocalMatrix because to transform a plane we'd need to do an inverse, 
				// and using the already inversed matrix we don't need to do a costly inverse.
				var localPlane		= GeometryUtility.InverseTransformPlane(firstSnappedBrush.transform.localToWorldMatrix, worldPlane);
				var	vertices		= firstSnappedBrush.ControlMesh.Vertices;
				var planeIsInversed	= false;
				for (int v = 0; v < vertices.Length; v++)
				{
					if (localPlane.Distance(vertices[v]) > MathConstants.DistanceEpsilon)
					{
						planeIsInversed = true;
						break;
					}
				}
				if (planeIsInversed)
					firstSnappedPlanes[i / 2] = worldPlane.Negated();
				else
					firstSnappedPlanes[i / 2] = worldPlane;
			}
		}

		protected override void HandleEditShapeEvents(Rect sceneRect)
		{
			if (!SceneTools.IsDraggingObjectInScene &&
				Event.current.type == EventType.Repaint)
			{			
				if (visualSnappedEdges != null)
					PaintUtility.DrawLines(visualSnappedEdges.ToArray(), ToolConstants.oldThickLineScale, ColorSettings.SnappedEdges);
				
				if (visualSnappedGrid != null)
				{
					var _origMatrix = Handles.matrix;
					Handles.matrix = MathConstants.identityMatrix;
					PaintUtility.DrawDottedLines(visualSnappedGrid.ToArray(), ColorSettings.SnappedEdges);
					Handles.matrix = _origMatrix;
				}
					
				if (visualSnappedBrush != null)
				{
					var brush_cache = InternalCSGModelManager.GetBrushCache(visualSnappedBrush);
					if (brush_cache != null &&
						brush_cache.compareTransformation != null &&
						brush_cache.childData != null &&
						brush_cache.childData.ModelTransform != null &&
						brush_cache.childData.ModelTransform)
					{
						var brush_translation = brush_cache.compareTransformation.modelLocalPosition + brush_cache.childData.ModelTransform.position;
						CSGRenderer.DrawOutlines(visualSnappedBrush.brushID, brush_translation, ColorSettings.SelectedOutlines, ColorSettings.SelectedOutlines, ColorSettings.SelectedOutlines, ColorSettings.SelectedOutlines);
					}						
				}
				
				var origMatrix	= Handles.matrix;
				Handles.matrix = MathConstants.identityMatrix;

				bool isValid;
				var realVertices		= settings.GetVertices(buildPlane, worldPosition, gridTangent, gridBinormal, out isValid);
				if (editMode == EditMode.EditShape)
					shapeIsValid = isValid;

				var wireframeColor		= ColorSettings.WireframeOutline;
				var topWireframeColor	= ColorSettings.BoundsEdgeHover;

				if (!shapeIsValid || !isValid)
					wireframeColor = Color.red;

				if (realVertices.Length > 0)
				{
					if (realVertices.Length >= 3)
					{
						var color = ColorSettings.ShapeDrawingFill;
						PaintUtility.DrawPolygon(MathConstants.identityMatrix, realVertices, color);
					}

					for (int i = 1; i < realVertices.Length; i++)
					{
						PaintUtility.DrawLine(realVertices[i - 1], realVertices[i], ToolConstants.oldLineScale, wireframeColor);
						PaintUtility.DrawDottedLine(realVertices[i - 1], realVertices[i], wireframeColor, 4.0f);
					}

					PaintUtility.DrawLine(realVertices[realVertices.Length - 1], realVertices[0], ToolConstants.oldLineScale, wireframeColor);
					PaintUtility.DrawDottedLine(realVertices[realVertices.Length - 1], realVertices[0], wireframeColor, 4.0f);

					if (editMode == EditMode.ExtrudeShape)
					{
						var delta = (base.centerPoint[1] - base.centerPoint[0]);
						for (int i = 1; i < realVertices.Length; i++)
						{
							PaintUtility.DrawLine(realVertices[i - 1] + delta, realVertices[i] + delta, ToolConstants.oldLineScale, topWireframeColor);
							PaintUtility.DrawDottedLine(realVertices[i - 1] + delta, realVertices[i] + delta, topWireframeColor, 4.0f);
						}

						for (int i = 0; i < realVertices.Length; i++)
						{
							PaintUtility.DrawLine(realVertices[i] + delta, realVertices[i], ToolConstants.oldLineScale, wireframeColor);
							PaintUtility.DrawDottedLine(realVertices[i] + delta, realVertices[i], wireframeColor, 4.0f);
						}

						PaintUtility.DrawLine(realVertices[realVertices.Length - 1] + delta, realVertices[0] + delta, ToolConstants.oldLineScale, topWireframeColor);
						PaintUtility.DrawDottedLine(realVertices[realVertices.Length - 1] + delta, realVertices[0] + delta, topWireframeColor, 4.0f);
					}

					PaintSquare();
					PaintRadiusMessage();
				}
				
				Handles.matrix = origMatrix;
			}
			
			var shapeType = Event.current.GetTypeForControl(shapeId);
			HandleKeyboard(shapeType);

			for (int i = 1; i < settings.vertices.Length; i++)
			{
				var id = settings.vertexIDs[i];
				var point_type = Event.current.GetTypeForControl(id);
				switch (point_type)
				{
					case EventType.Repaint:
					{
						if (SceneTools.IsDraggingObjectInScene)
							break;

						bool isSelected = id == GUIUtility.keyboardControl;
						var temp		= Handles.color;
						var origMatrix	= Handles.matrix;
					
						Handles.matrix = MathConstants.identityMatrix;
						var rotation = Camera.current.transform.rotation;


						if (isSelected)
						{
							Handles.color = ColorSettings.PointInnerStateColor[3];
						} else
						if (HandleUtility.nearestControl == id)
						{
							Handles.color = ColorSettings.PointInnerStateColor[1];
						} else						
						{
							Handles.color = ColorSettings.PointInnerStateColor[0];
						}

						float handleSize = GUIStyleUtility.GetHandleSize(settings.vertices[i]);
						float scaledHandleSize = handleSize * ToolConstants.handleScale;
						PaintUtility.SquareDotCap(id, settings.vertices[i], rotation, scaledHandleSize);
						
						Handles.matrix = origMatrix;
						Handles.color = temp;
						break;
					}

					case EventType.layout:
					{
						if ((Tools.viewTool != ViewTool.None && Tools.viewTool != ViewTool.Pan))
							break;

						var origMatrix = Handles.matrix;
						Handles.matrix = MathConstants.identityMatrix;
						float handleSize = GUIStyleUtility.GetHandleSize(settings.vertices[i]);
						float scaledHandleSize = handleSize * ToolConstants.handleScale;
						HandleUtility.AddControl(id, HandleUtility.DistanceToCircle(settings.vertices[i], scaledHandleSize));
						Handles.matrix = origMatrix;						
					
						break;
					}

					case EventType.ValidateCommand:
					case EventType.keyDown:
					{
						if (GUIUtility.hotControl == id)
						{
							if (Keys.CancelActionKey.IsKeyPressed())
							{
								Event.current.Use(); 
								break;
							}
						}
						break;
					}
					case EventType.keyUp:
					{
						if (GUIUtility.hotControl == id)
						{
							if (Keys.CancelActionKey.IsKeyPressed())
                            {
                                GUIUtility.hotControl = 0;
								GUIUtility.keyboardControl = 0;
								EditorGUIUtility.editingTextField = false;
								Event.current.Use(); 
								break;
							}
						}
						break;
					}

					case EventType.MouseDown:
					{
						if (!sceneRect.Contains(Event.current.mousePosition))
							break;
						if (Tools.viewTool != ViewTool.None && Tools.viewTool != ViewTool.Pan)
							break;
						if (GUIUtility.hotControl == 0 && HandleUtility.nearestControl == id && Event.current.button == 0)
						{
                            GUIUtility.hotControl = id;
							GUIUtility.keyboardControl = id;
							EditorGUIUtility.editingTextField = false; 
							EditorGUIUtility.SetWantsMouseJumping(1);
							Event.current.Use(); 
							break;
						}
						break;
					}
					case EventType.MouseDrag:
					{
						if (Tools.viewTool != ViewTool.None && Tools.viewTool != ViewTool.Pan)
							break;
						if (GUIUtility.hotControl == id && Event.current.button == 0)
						{
							Undo.RecordObject(this, "Modify shape");

							var mouseRay		= HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
							var alignedPlane	= new CSGPlane(Grid.CurrentWorkGridPlane.normal, settings.vertices[0]);
							var worldPosition	= buildPlane.Project(alignedPlane.Intersection(mouseRay));
							if (float.IsInfinity(worldPosition.x) || float.IsNaN(worldPosition.x) ||
								float.IsInfinity(worldPosition.y) || float.IsNaN(worldPosition.y) ||
								float.IsInfinity(worldPosition.z) || float.IsNaN(worldPosition.z))
								worldPosition = settings.vertices[i];

							ResetVisuals();
							if (snapFunction != null)
							{
								CSGBrush snappedOnBrush;
								worldPosition = snapFunction(worldPosition, buildPlane, ref base.visualSnappedEdges, out snappedOnBrush, ignoreBrushes: true);
							}
								
							base.visualSnappedGrid = Grid.FindAllGridEdgesThatTouchPoint(worldPosition);

                            settings.vertices[i] = worldPosition;

							UpdateBaseShape(true);

							GUI.changed = true;
							Event.current.Use(); 
							break;
						}
						break;
					}
					case EventType.MouseUp:
					{
						if (GUIUtility.hotControl != id)
							break;
						if (Tools.viewTool != ViewTool.None && Tools.viewTool != ViewTool.Pan)
							break;
						if (Event.current.button == 0)
                        {
							GUIUtility.hotControl = 0;
							GUIUtility.keyboardControl = 0;
							EditorGUIUtility.editingTextField = false;
							EditorGUIUtility.SetWantsMouseJumping(0);
							Event.current.Use(); 

							ResetVisuals();
							if (SphereRadius == 0)
							{
								Cancel();
							}
							break;
						}
						break;
					}
				}
				
			}
		}
		
		protected override void HandleCreateShapeEvents(Rect sceneRect)
		{
			bool		pointOnEdge			= false;
			bool		havePlane			= false;
			bool		vertexOnGeometry	= false;
			CSGBrush	vertexOnBrush		= null;
			
			CSGPlane	hoverBuildPlane		= buildPlane;
			var sceneView = (SceneView.currentDrawingSceneView != null) ? SceneView.currentDrawingSceneView : SceneView.lastActiveSceneView;
			var camera = sceneView.camera;
			if (camera != null &&
				camera.pixelRect.Contains(Event.current.mousePosition))
			{
				if (!hoverDefaultPlane.HasValue ||
					settings.vertices.Length == 0)
				{
					bool forceGrid = Grid.ForceGrid;
					Grid.ForceGrid = false;
					hoverDefaultPlane = Grid.CurrentGridPlane;
					Grid.ForceGrid = forceGrid;
					firstSnappedEdges = null;
					firstSnappedBrush = null;
					firstSnappedPlanes = null;
					base.geometryModel = null;
				}
				if (editMode == EditMode.CreatePlane)
				{
					BrushIntersection intersection;
					if (!camera.orthographic && !havePlane &&
						SceneQueryUtility.FindWorldIntersection(Event.current.mousePosition, out intersection, MathConstants.GrowBrushFactor))
					{
						worldPosition = intersection.worldIntersection;
						if (intersection.surfaceInverted)
							hoverBuildPlane = intersection.plane.Negated();
						else
							hoverBuildPlane = intersection.plane;
						vertexOnBrush = intersection.brush;

						vertexOnGeometry = true;
					} else
					{
						hoverBuildPlane = hoverDefaultPlane.Value;
						vertexOnBrush = null;

						var mouseRay = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
						worldPosition = hoverBuildPlane.Intersection(mouseRay);
						vertexOnGeometry = false;
					}
					
					ResetVisuals();
					if (snapFunction != null)
					{
						CSGBrush snappedOnBrush;
						worldPosition = snapFunction(worldPosition, hoverBuildPlane, ref visualSnappedEdges, out snappedOnBrush);
						if (snappedOnBrush != null)
						{
							pointOnEdge = (visualSnappedEdges != null &&
									  visualSnappedEdges.Count > 0);
							vertexOnBrush = snappedOnBrush;
							vertexOnGeometry = true;
						}
					}

					if (settings.vertices.Length == 1)
					{
						if (hoverBuildPlane.normal != MathConstants.zeroVector3)
						{
							editMode = EditMode.CreateShape;
							havePlane = true;
						}
					}
				} else
				{
					var mouseRay = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
					worldPosition = hoverBuildPlane.Intersection(mouseRay);

					ResetVisuals();
					if (snapFunction != null)
					{
						CSGBrush snappedOnBrush;
						worldPosition = snapFunction(worldPosition, hoverBuildPlane, ref visualSnappedEdges, out snappedOnBrush);
						if (snappedOnBrush != null)
						{
							pointOnEdge = (visualSnappedEdges != null &&
											visualSnappedEdges.Count > 0);
							vertexOnBrush = snappedOnBrush;
						}
					}
				}

				if (geometryModel == null && vertexOnBrush != null)
				{
					var brush_cache = InternalCSGModelManager.GetBrushCache(vertexOnBrush);
					if (brush_cache != null && brush_cache.childData != null && brush_cache.childData.Model)
						geometryModel = brush_cache.childData.Model;
				}

				if (worldPosition != prevWorldPosition)
				{
					prevWorldPosition = worldPosition;
					if (settings.vertices.Length > 0)
					{
						if (hadSphere || (settings.vertices[0] - worldPosition).sqrMagnitude > MathConstants.EqualityEpsilon)
						{
							hadSphere = true;
							UpdateBaseShape(true);
						}
					}
					if (Event.current.type != EventType.Repaint)
						SceneView.RepaintAll();
				}
				
				visualSnappedGrid = Grid.FindAllGridEdgesThatTouchPoint(worldPosition);
				visualSnappedBrush = vertexOnBrush;
			}
			
			Grid.SetForcedGrid(hoverBuildPlane);
			

			if (!SceneTools.IsDraggingObjectInScene &&
				Event.current.type == EventType.Repaint)
			{
				PaintSnapVisualisation();
				PaintCircle(base.shapeId);
			}
			

			var type = Event.current.GetTypeForControl(base.shapeId);
			switch (type)
			{
				case EventType.layout:
				{
					return;
				}

				case EventType.ValidateCommand:
				case EventType.keyDown:
				{
					if (GUIUtility.hotControl == base.shapeId)
					{
						if (Keys.PerformActionKey.IsKeyPressed() ||
							Keys.DeleteSelectionKey.IsKeyPressed() ||
							Keys.CancelActionKey.IsKeyPressed())
						{
							Event.current.Use();
						}
					}
					return;
				}
				case EventType.KeyUp:
				{
					if (GUIUtility.hotControl == base.shapeId)
					{
						if (Keys.CylinderBuilderMode.IsKeyPressed() ||
							Keys.PerformActionKey.IsKeyPressed())
						{
							HotKeyReleased(); 
							Event.current.Use();
							return;
						}
						if (Keys.DeleteSelectionKey.IsKeyPressed() ||
							Keys.CancelActionKey.IsKeyPressed())
						{
							Cancel();
							Event.current.Use();
							return;
						}
					}
					return;
				}

				case EventType.MouseDown:
				{
					if (!sceneRect.Contains(Event.current.mousePosition))
						break;
					if (Tools.viewTool != ViewTool.None && Tools.viewTool != ViewTool.Pan)
						return;
					if ((GUIUtility.hotControl != 0 && GUIUtility.hotControl != shapeEditID && GUIUtility.hotControl != base.shapeId) ||
						Event.current.button != 0)
						return;
					
					Event.current.Use();
					if (settings.vertices.Length == 0)
					{
						if ((GUIUtility.hotControl == 0 ||
							GUIUtility.hotControl == base.shapeEditID) && base.shapeId != -1)
                        {
							base.CalculateWorldSpaceTangents();
                            GUIUtility.hotControl = base.shapeId;
							GUIUtility.keyboardControl = base.shapeId;
							EditorGUIUtility.editingTextField = false; 
						}
					}

					if (GUIUtility.hotControl == base.shapeId && settings.vertices.Length < 2)
					{
						if (!float.IsNaN(worldPosition.x) && !float.IsInfinity(worldPosition.x) &&
							!float.IsNaN(worldPosition.y) && !float.IsInfinity(worldPosition.y) &&
							!float.IsNaN(worldPosition.z) && !float.IsInfinity(worldPosition.z))
						{
							if (hoverBuildPlane.normal.sqrMagnitude != 0)
								buildPlane = hoverBuildPlane;
							CalculateWorldSpaceTangents();

							if (settings.vertices.Length == 0)
							{
								if (pointOnEdge)
								{
									firstSnappedEdges = visualSnappedEdges.ToArray();
									firstSnappedBrush = visualSnappedBrush;
									firstSnappedPlanes = null;
								} else
								{
									firstSnappedBrush = null;
									firstSnappedEdges = null;
									firstSnappedPlanes = null;
								}
								planeOnGeometry = vertexOnGeometry;
							} else
							{
								if (firstSnappedEdges != null)
								{
									if (firstSnappedPlanes == null)
										CreateSnappedPlanes();

									bool outside = true;
									for (int i = 0; i < firstSnappedPlanes.Length; i++)
									{
										if (firstSnappedPlanes[i].Distance(worldPosition) <= MathConstants.DistanceEpsilon)
										{
											outside = false;
											break;
										}
									}

									planeOnGeometry = !outside;
								}

								if (vertexOnGeometry)
								{
									var plane = hoverDefaultPlane.Value;
									var distance = plane.Distance(worldPosition);
									plane.d += distance;
									hoverDefaultPlane = plane;

									for (int i = 0; i < settings.vertices.Length; i++)
									{
										if (!settings.onGeometryVertices[i])
										{
											settings.vertices[i] = GeometryUtility.ProjectPointOnPlane(plane, settings.vertices[i]);
											settings.onGeometryVertices[i] = true;
										}
									}
								}
							}
							ArrayUtility.Add(ref settings.onGeometryVertices, vertexOnGeometry);
							settings.AddPoint(worldPosition);
							SceneView.RepaintAll();
							if (settings.vertices.Length == 2)
							{
								HotKeyReleased();
							}
						}
					}
					return;
				}
				case EventType.MouseDrag:
				{
					if (Tools.viewTool != ViewTool.None && Tools.viewTool != ViewTool.Pan)
						break;
					if (GUIUtility.hotControl == base.shapeId && Event.current.button == 0)
					{
						Event.current.Use();
					}
					return;
				}
				case EventType.MouseUp:
				{
					if (GUIUtility.hotControl != base.shapeId)
						return;
					if (Tools.viewTool != ViewTool.None && Tools.viewTool != ViewTool.Pan)
						return;
					if (Event.current.button == 0)
					{
						Event.current.Use(); 

						ResetVisuals();
						if (settings.vertices.Length == 2)
						{
							GUIUtility.hotControl = 0;
							GUIUtility.keyboardControl = 0;
							EditorGUIUtility.editingTextField = false;

							editMode = EditMode.CreateShape;
							HotKeyReleased();
						}
					}
					return;
				}
			}
		}

		protected override void CreateControlIDs()
		{
			base.CreateControlIDs();

			if (settings.vertices.Length > 0)
			{
				if (settings.vertexIDs == null ||
					settings.vertexIDs.Length != settings.vertices.Length)
					settings.vertexIDs = new int[settings.vertices.Length];
				for (int i = 0; i < settings.vertices.Length; i++)
				{
					settings.vertexIDs[i] = GUIUtility.GetControlID(ShapeBuilderPointHash, FocusType.Passive);
				}
			}			
		}

		public override void HandleEvents(Rect sceneRect)
		{
			base.HandleEvents(sceneRect);

			if (Event.current.type == EventType.MouseUp)
			{
				if (GUIUtility.hotControl == 0 &&
					Event.current.modifiers == EventModifiers.None &&
					!mouseIsDragging && Event.current.button == 0 &&
					(Tools.viewTool == ViewTool.None || Tools.viewTool == ViewTool.Pan))
				{
					ResetVisuals();
					Event.current.Use();
					Commit();
				}
			}
		}
		
		public bool OnShowGUI(bool isSceneGUI)
		{
			return SphereGeneratorGUI.OnShowGUI(this, isSceneGUI);
		}


		public override AABB GetShapeBounds()
		{
			return ShapeSettings.CalculateBounds(gridTangent, gridBinormal);
		}

		public override bool Commit()
        {
            isFinished = true;
            CleanupGrid();

            if (SphereRadius == 0 || generatedBrushes == null || generatedBrushes.Length < 1 ||
				!UpdateBaseShape(true))
            {
                Cancel();
                return false;
            }

            EndCommit();
            return true;
		}

		private bool GenerateSphere(float radius, int splits, CSGModel parentModel, CSGBrush brush, out ControlMesh controlMesh, out Shape shape)
        {
			if (prevSplits != splits || prevIsHemisphere != IsHemiSphere || splitControlMesh == null || splitShape == null)
			{
				splitControlMesh = null;
				splitShape = null;
				BrushFactory.CreateCubeControlMesh(out splitControlMesh, out splitShape, Vector3.one);

				var axi = new Vector3[] { Vector3.up, Vector3.left, Vector3.forward };
				List<int> intersectedEdges = new List<int>();
				float step = 1.0f / (float)(splits + 1);
				float offset;
				for (int i = 0; i < axi.Length; i++)
				{
					var normal = axi[i];
					offset = 0.5f - step;
					while (offset > 0.0f)
					{
						ControlMeshUtility.CutMesh(splitControlMesh, splitShape, new CSGPlane(-normal, -offset), ref intersectedEdges);
						if (i != 0 || !IsHemiSphere)
						{
							ControlMeshUtility.CutMesh(splitControlMesh, splitShape, new CSGPlane(normal, -offset), ref intersectedEdges);
						}
						offset -= step;
					}
					if (i != 0 || !IsHemiSphere)
					{
						if ((splits & 1) == 1)
							ControlMeshUtility.CutMesh(splitControlMesh, splitShape, new CSGPlane(normal, 0), ref intersectedEdges);
					}
				}

				if (IsHemiSphere)
				{
					var cuttingPlane = new CSGPlane(Vector3.up, 0);
					intersectedEdges.Clear();
					if (ControlMeshUtility.CutMesh(splitControlMesh, splitShape, cuttingPlane, ref intersectedEdges))
					{
						var edge_loop = ControlMeshUtility.FindEdgeLoop(splitControlMesh, ref intersectedEdges);
						if (edge_loop != null)
						{
							if (ControlMeshUtility.SplitEdgeLoop(splitControlMesh, splitShape, edge_loop))
							{
								Shape foundShape;
								ControlMesh foundControlMesh;
								ControlMeshUtility.FindAndDetachSeparatePiece(splitControlMesh, splitShape, cuttingPlane, out foundControlMesh, out foundShape);
							}
						}
					}
				}

				for (int i = 0; i < splitControlMesh.Vertices.Length; i++)
				{
					splitControlMesh.Vertices[i] = splitControlMesh.Vertices[i].normalized;
				}
				if (!ControlMeshUtility.Triangulate(null, splitControlMesh, splitShape))
				{
					Debug.Log("!ControlMeshUtility.IsConvex");
					controlMesh = null;
					shape = null;
					return false;
				} 
				ControlMeshUtility.FixTexGens(splitControlMesh, splitShape);

				if (!ControlMeshUtility.IsConvex(splitControlMesh, splitShape))
				{
					Debug.Log("!ControlMeshUtility.IsConvex");
					controlMesh = null;
					shape = null;
					return false;
				}
				ControlMeshUtility.UpdateTangents(splitControlMesh, splitShape);
				prevSplits = splits;
				prevIsHemisphere = IsHemiSphere;
			}

			if (splitControlMesh == null || splitShape == null || !splitControlMesh.IsValid)
			{
				Debug.Log("splitControlMesh == null || splitShape == null || !splitControlMesh.IsValid");
				controlMesh = null;
				shape = null;
				return false;
			}

			controlMesh = splitControlMesh.Clone();
			shape = splitShape.Clone();

			/*
			float angle_offset = GeometryUtility.SignedAngle(gridTangent, delta / sphereRadius, buildPlane.normal);
			angle_offset -= 90;

			angle_offset += sphereOffset;
			angle_offset *= Mathf.Deg2Rad;

			Vector3 p1 = MathConstants.zeroVector3;
			for (int i = 0; i < realSplits; i++)
			{
				var angle = ((i * Mathf.PI * 2.0f) / (float)realSplits) + angle_offset;

				p1.x = (Mathf.Sin(angle) * sphereRadius);
				p1.z = (Mathf.Cos(angle) * sphereRadius);
			}
			*/

			for (int i = 0; i < controlMesh.Vertices.Length; i++)
			{
				var vertex = controlMesh.Vertices[i];
				vertex *= radius;
				controlMesh.Vertices[i] = vertex;
			}

			for (int i = 0; i < shape.Surfaces.Length; i++)
			{
				var plane = shape.Surfaces[i].Plane;
				plane.d *= radius;
				shape.Surfaces[i].Plane = plane;
			}

			bool smoothShading = SphereSmoothShading;
			if (!sphereSmoothingGroup.HasValue && smoothShading)
			{
				sphereSmoothingGroup = SurfaceUtility.FindUnusedSmoothingGroupIndex();
			}

			for (int i = 0; i < shape.TexGenFlags.Length; i++)
			{
				shape.TexGens[i].SmoothingGroup = smoothShading ? sphereSmoothingGroup.Value : 0;
			}

			return true;
        }

        internal override bool UpdateBaseShape(bool registerUndo = true)
		{
			if (editMode == EditMode.CreatePlane)
				return false;

			float radius = SphereRadius;
			if (editMode == EditMode.CreateShape)
			{
				centerPoint[0] = settings.vertices[0];
				radius = (settings.vertices[0] - worldPosition).magnitude;
			}

            GenerateBrushObjects(1);
			
            if (radius == 0 || generatedBrushes == null || generatedBrushes.Length < 1)
			{
				InternalCSGModelManager.skipRefresh = false;
                HideGenerateBrushes();
                return false;
            }
			
            //UpdateBrushOperation(height);

            bool failures = false;
            bool modifiedHierarchy = false;
            if (generatedGameObjects != null && generatedGameObjects.Length > 0)
            {
                for (int i = generatedGameObjects.Length - 1; i >= 0; i--)
                {
                    if (generatedGameObjects[i])
                        continue;
                    ArrayUtility.RemoveAt(ref generatedGameObjects, i);
                }
            }
            if (generatedGameObjects == null || generatedGameObjects.Length == 0)
            {
                Cancel();
                return false;
            }
            if (generatedGameObjects != null && generatedGameObjects.Length > 0)
            {
                if (registerUndo)
                    Undo.RecordObjects(generatedGameObjects, "Created Sphere");
                
                var brush = generatedBrushes[0];
                if (brush && brush.gameObject)
                {
                    ControlMesh newControlMesh;
                    Shape newShape;
                    if (GenerateSphere(radius, settings.sphereSplits, parentModel, brush, out newControlMesh, out newShape))
                    {
                        if (!brush.gameObject.activeSelf)
                        {
                            modifiedHierarchy = true;
                            brush.gameObject.SetActive(true);
                        }
						
						brush.Shape = newShape;
						brush.ControlMesh = newControlMesh;
						if (registerUndo)
							EditorUtility.SetDirty(brush);
						ControlMeshUtility.UpdateBrushMesh(brush, brush.ControlMesh, brush.Shape);
                    } else
                    {
                        failures = true;
                        if (brush.gameObject.activeSelf)
                        {
                            modifiedHierarchy = true;
                            brush.gameObject.SetActive(false);
                        }
                    }
                }
            }

            try
            {
                InternalCSGModelManager.skipRefresh = true;
                if (registerUndo)
                    EditorUtility.SetDirty(this);
                //CSGModelManager.External.ForceModelUpdate(parentModel.modelID); 
                InternalCSGModelManager.Refresh(forceHierarchyUpdate: modifiedHierarchy);
            }
            finally
            {
                InternalCSGModelManager.skipRefresh = false;
            }
            return !failures;
		}
	}
}
