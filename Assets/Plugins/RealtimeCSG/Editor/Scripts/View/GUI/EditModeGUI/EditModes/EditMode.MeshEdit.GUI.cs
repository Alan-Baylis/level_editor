using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using InternalRealtimeCSG;
using RealtimeCSG.Foundation;

namespace RealtimeCSG
{
	internal sealed partial class EditModeMeshModeGUI
	{
		static int SceneViewMeshOverlayHash = "SceneViewMeshOverlay".GetHashCode();

		static Rect lastGuiRect;
		public static Rect GetLastSceneGUIRect(EditModeMeshEdit tool)
		{
			return lastGuiRect;
		}

		public static void OnSceneGUI(Rect windowRect, EditModeMeshEdit tool)
		{
			CSG_GUIStyleUtility.InitStyles();
			InitLocalStyles();
			GUILayout.BeginHorizontal(CSG_GUIStyleUtility.ContentEmpty);
			{
				GUILayout.BeginVertical(CSG_GUIStyleUtility.ContentEmpty);
				{
					GUILayout.BeginVertical(CSG_GUIStyleUtility.ContentEmpty);
					{
						GUILayout.FlexibleSpace();

						CSG_GUIStyleUtility.ResetGUIState();

						GUIStyle windowStyle = GUI.skin.window;
						GUILayout.BeginVertical(ContentMeshLabel, windowStyle, CSG_GUIStyleUtility.ContentEmpty);
						{
							OnGUIContents(true, tool);
						}
						GUILayout.EndVertical();

						var currentArea = GUILayoutUtility.GetLastRect();
						lastGuiRect = currentArea;

						var buttonArea = currentArea;
						buttonArea.x += buttonArea.width - 17;
						buttonArea.y += 2;
						buttonArea.height = 13;
						buttonArea.width = 13;
						if (GUI.Button(buttonArea, GUIContent.none, "WinBtnClose"))
							EditModeToolWindowSceneGUI.GetWindow();
						TooltipUtility.SetToolTip(CSG_GUIStyleUtility.PopOutTooltip, buttonArea);

						int controlID = GUIUtility.GetControlID(SceneViewMeshOverlayHash, FocusType.Keyboard, currentArea);
						switch (Event.current.GetTypeForControl(controlID))
						{
							case EventType.MouseDown: { if (currentArea.Contains(Event.current.mousePosition)) { GUIUtility.hotControl = controlID; GUIUtility.keyboardControl = controlID; Event.current.Use(); } break; }
							case EventType.MouseMove: { if (currentArea.Contains(Event.current.mousePosition)) { Event.current.Use(); } break; }
							case EventType.MouseUp: { if (GUIUtility.hotControl == controlID) { GUIUtility.hotControl = 0; GUIUtility.keyboardControl = 0; Event.current.Use(); } break; }
							case EventType.MouseDrag: { if (GUIUtility.hotControl == controlID) { Event.current.Use(); } break; }
							case EventType.ScrollWheel: { if (currentArea.Contains(Event.current.mousePosition)) { Event.current.Use(); } break; }
						}
					}
					GUILayout.EndVertical();
				}
				GUILayout.EndVertical();
				GUILayout.FlexibleSpace();
			}
			GUILayout.EndHorizontal();
		}

		public static void OnInspectorGUI(EditorWindow window, float height)
		{
			lastGuiRect = Rect.MinMaxRect(-1, -1, -1, -1);
			var tool = EditModeManager.ActiveTool as EditModeMeshEdit;
			CSG_GUIStyleUtility.InitStyles();
			InitLocalStyles();
			OnGUIContents(false, tool);
		}

		static void ChooseCSGOperation(FilteredSelection filteredSelection, bool isSceneGUI, EditModeMeshEdit tool)
		{
			bool operations_enabled = tool != null &&
									(filteredSelection.NodeTargets.Length > 0 && filteredSelection.ModelTargets.Length == 0);

			EditorGUI.BeginDisabledGroup(!operations_enabled);
			{
				bool mixedValues = tool == null || ((filteredSelection.BrushTargets.Length == 0) && (filteredSelection.OperationTargets.Length == 0));
				CSGOperationType operationType = CSGOperationType.Additive;
				if (tool != null)
				{
					if (filteredSelection.BrushTargets.Length > 0)
					{
						operationType = filteredSelection.BrushTargets[0].OperationType;
						for (int i = 1; i < filteredSelection.BrushTargets.Length; i++)
						{
							if (filteredSelection.BrushTargets[i].OperationType != operationType)
							{
								mixedValues = true;
							}
						}
					}
					else
					if (filteredSelection.OperationTargets.Length > 0)
					{
						operationType = filteredSelection.OperationTargets[0].OperationType;
					}

					if (filteredSelection.OperationTargets.Length > 0)
					{
						for (int i = 0; i < filteredSelection.OperationTargets.Length; i++)
						{
							if (filteredSelection.OperationTargets[i].OperationType != operationType)
							{
								mixedValues = true;
							}
						}
					}
				}

				GUILayout.BeginVertical(isSceneGUI ? GUI.skin.box : GUIStyle.none);
				{
					bool passThroughValue = false;
					if (tool != null &&
						//filteredSelection.BrushTargets.Length == 0 && 
						filteredSelection.OperationTargets.Length > 0 &&
						filteredSelection.OperationTargets.Length == filteredSelection.NodeTargets.Length) // only operations
					{
						bool? passThrough = filteredSelection.OperationTargets[0].PassThrough;
						for (int i = 1; i < filteredSelection.OperationTargets.Length; i++)
						{
							if (passThrough.HasValue && passThrough.Value != filteredSelection.OperationTargets[i].PassThrough)
							{
								passThrough = null;
								break;
							}
						}

						mixedValues = !passThrough.HasValue || passThrough.Value;

						var ptMixedValues = !passThrough.HasValue;
						passThroughValue = passThrough.HasValue ? passThrough.Value : false;
						if (CSG_EditorGUIUtility.PassThroughButton(passThroughValue, ptMixedValues))
						{
							Undo.RecordObjects(filteredSelection.OperationTargets, "Changed CSG operation of nodes");
							foreach (var operation in filteredSelection.OperationTargets)
							{
								operation.PassThrough = true;
							}
							InternalCSGModelManager.CheckForChanges();
							EditorApplication.RepaintHierarchyWindow();
						}

						if (passThroughValue)
							operationType = (CSGOperationType)255;
					}
					EditorGUI.BeginChangeCheck();
					{
						operationType = CSG_EditorGUIUtility.ChooseOperation(operationType, mixedValues);
					}
					if (EditorGUI.EndChangeCheck() && tool != null)
					{
						Undo.RecordObjects(filteredSelection.NodeTargets, "Changed CSG operation of nodes");
						for (int i = 0; i < filteredSelection.BrushTargets.Length; i++)
						{
							filteredSelection.BrushTargets[i].OperationType = operationType;
						}
						for (int i = 0; i < filteredSelection.OperationTargets.Length; i++)
						{
							filteredSelection.OperationTargets[i].PassThrough = false;
							filteredSelection.OperationTargets[i].OperationType = operationType;
						}
						InternalCSGModelManager.CheckForChanges();
						EditorApplication.RepaintHierarchyWindow();
					}
				}
				GUILayout.EndVertical();
			}
			EditorGUI.EndDisabledGroup();
		}

		static void OnGUIContents(bool isSceneGUI, EditModeMeshEdit tool)
		{
			EditModeCommonGUI.StartToolGUI();

			var filteredSelection = EditModeManager.FilteredSelection;

			var defaultMaterial = CSGSettings.DefaultMaterial;
			GUILayout.BeginVertical(isSceneGUI ? InSceneWidth : CSG_GUIStyleUtility.ContentEmpty);
			{/*
				GUILayout.BeginHorizontal(GUIStyleUtility.ContentEmpty);
				{
					
					GUILayout.BeginVertical(isSceneGUI ? GUI.skin.box : GUIStyle.none);
					{
						//GUILayout.Label(Keys.VerticalMoveMode.ToString() + " to dragging brush up/down", EditorStyles.miniLabel);
						GUILayout.Label("Control (hold) to drag polygon on it's plane", EditorStyles.miniLabel);
						GUILayout.Label("Shift (hold) to drag extrude polygon", EditorStyles.miniLabel);
						GUILayout.Label("Shift (hold) to chamfer edges and vertices", EditorStyles.miniLabel);
					}
					GUILayout.EndVertical();
				}
				GUILayout.EndHorizontal();*/
				ChooseCSGOperation(filteredSelection, isSceneGUI, tool);
				GUILayout.BeginHorizontal(CSG_GUIStyleUtility.ContentEmpty);
				{
					if (isSceneGUI)
					{
						GUILayout.BeginVertical(CSG_GUIStyleUtility.ContentEmpty);
						{
							EditorGUI.BeginChangeCheck();
							{
								defaultMaterial = CSG_EditorGUIUtility.MaterialImage(defaultMaterial, small: true);
							}
							if (EditorGUI.EndChangeCheck() && defaultMaterial)
							{
								CSGSettings.DefaultMaterial = defaultMaterial;
								CSGSettings.Save();
							}
						}
						GUILayout.EndVertical();
					}
					GUILayout.BeginVertical(CSG_GUIStyleUtility.ContentEmpty);
					{
						bool have_nodes = tool != null && (filteredSelection.NodeTargets.Length > 0);

						EditorGUI.BeginDisabledGroup(!have_nodes);
						{
							GUIStyle left = EditorStyles.miniButtonLeft;
							GUIStyle middle = EditorStyles.miniButtonMid;
							GUIStyle right = EditorStyles.miniButtonRight;

							GUILayout.BeginHorizontal(CSG_GUIStyleUtility.ContentEmpty);
							{
								if (isSceneGUI)
									EditorGUILayout.LabelField(ContentFlip, labelWidth);
								else
									EditorGUILayout.LabelField(ContentFlip, largeLabelWidth);
								if (GUILayout.Button(ContentFlipX, left)) { tool.FlipX(); }
								TooltipUtility.SetToolTip(TooltipFlipX);
								if (GUILayout.Button(ContentFlipY, middle)) { tool.FlipY(); }
								TooltipUtility.SetToolTip(TooltipFlipY);
								if (GUILayout.Button(ContentFlipZ, right)) { tool.FlipZ(); }
								TooltipUtility.SetToolTip(TooltipFlipZ);
							}
							GUILayout.EndHorizontal();

							/*
							EditorGUILayout.LabelField(ContentEdgesLabel);
							GUILayout.BeginHorizontal(GUIStyleUtility.ContentEmpty);
							{
								EditorGUI.BeginDisabledGroup(!tool.CanSmooth());
								{ 
									if (GUILayout.Button("Smooth"))		{ tool.Smooth(); }
								}
								EditorGUI.EndDisabledGroup();
								EditorGUI.BeginDisabledGroup(!tool.CanUnSmooth());
								{
									if (GUILayout.Button("Un-smooth"))	{ tool.UnSmooth(); }
								}
								EditorGUI.EndDisabledGroup();
							}
							GUILayout.EndHorizontal();
							*/
						
							if (GUILayout.Button(ContentSnapToGrid)) { tool.SnapToGrid(); }
							TooltipUtility.SetToolTip(TooltipSnapToGrid);
						}
						EditorGUI.EndDisabledGroup();
					}
					GUILayout.EndVertical();
				}
				GUILayout.EndHorizontal();
				GUILayout.Space(2);
				if (!isSceneGUI)
				{
					EditorGUILayout.Space();
					GUILayout.BeginHorizontal(CSG_GUIStyleUtility.ContentEmpty);
					{
						EditorGUILayout.LabelField(ContentDefaultMaterial, largeLabelWidth);
						GUILayout.BeginVertical(CSG_GUIStyleUtility.ContentEmpty);
						{
							EditorGUI.BeginChangeCheck();
							{
								defaultMaterial = EditorGUILayout.ObjectField(defaultMaterial, typeof(Material), true) as Material;
							}
							if (EditorGUI.EndChangeCheck() && defaultMaterial)
							{
								CSGSettings.DefaultMaterial = defaultMaterial;
								CSGSettings.Save();
							}
						}
						GUILayout.Space(2);
						GUILayout.BeginHorizontal(CSG_GUIStyleUtility.ContentEmpty);
						{
							GUILayout.Space(5);
							defaultMaterial = CSG_EditorGUIUtility.MaterialImage(defaultMaterial, small: false);
						}
						GUILayout.EndHorizontal();
						GUILayout.EndVertical();
					}
					GUILayout.EndHorizontal();
					/*
					// Unity won't let us do this
					GUILayout.BeginVertical(GUIStyleUtility.ContentEmpty);
					OnGUIContentsMaterialInspector(first_material, multiple_materials);
					GUILayout.EndVertical();
					*/
				} else
				{ 
					EditorGUI.BeginChangeCheck();
					{
						defaultMaterial = EditorGUILayout.ObjectField(defaultMaterial, typeof(Material), true) as Material;
					}
					if (EditorGUI.EndChangeCheck() && defaultMaterial)
					{
						CSGSettings.DefaultMaterial = defaultMaterial;
						CSGSettings.Save();
					}
				}
			}
			GUILayout.EndVertical();
			EditorGUI.showMixedValue = false;
		}
	}
}
