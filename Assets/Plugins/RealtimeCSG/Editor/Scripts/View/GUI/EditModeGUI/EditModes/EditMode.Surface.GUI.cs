using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using InternalRealtimeCSG;
using RealtimeCSG.Legacy;
using RealtimeCSG.Components;

namespace RealtimeCSG
{
	internal sealed partial class EditModeSurfaceGUI
	{
		private static readonly int SceneViewSurfaceOverlayHash = "SceneViewSurfaceOverlay".GetHashCode();

		[NonSerialized]
		private static MaterialEditor	materialEditor	= null;

		static Material GetDragMaterial()
		{
			if (DragAndDrop.objectReferences != null &&
				DragAndDrop.objectReferences.Length > 0)
			{
				var dragMaterials = new List<Material>();
				foreach (var obj in DragAndDrop.objectReferences)
				{
					var dragMaterial = obj as Material;
					if (dragMaterial == null)
						continue;
					dragMaterials.Add(dragMaterial);
				}
				if (dragMaterials.Count == 1)
					return dragMaterials[0];
			}
			return null;
		}

		static void OnGUIContentsJustify(bool isSceneGUI, SelectedBrushSurface[] selectedBrushSurfaces)
		{
			GUILayout.BeginVertical(CSG_GUIStyleUtility.ContentEmpty);
			{
				if (!isSceneGUI)
				{
					GUILayout.BeginHorizontal(CSG_GUIStyleUtility.ContentEmpty);
					EditorGUILayout.LabelField(ContentJustifyLabel, largeLabelWidth);
				} else
					GUILayout.Label(ContentJustifyLabel);
				GUILayout.BeginVertical(CSG_GUIStyleUtility.ContentEmpty);
				{ 
					GUILayout.BeginHorizontal(CSG_GUIStyleUtility.ContentEmpty);
					{
						if (GUILayout.Button(ContentJustifyUpLeft,	justifyButtonLayout)) SurfaceUtility.JustifyLayout(selectedBrushSurfaces, -1,-1);
						TooltipUtility.SetToolTip(ToolTipJustifyUpLeft);
						if (GUILayout.Button(ContentJustifyUp,		justifyButtonLayout)) SurfaceUtility.JustifyLayoutY(selectedBrushSurfaces, -1);
						TooltipUtility.SetToolTip(ToolTipJustifyUp);
						if (GUILayout.Button(ContentJustifyUpRight, justifyButtonLayout)) SurfaceUtility.JustifyLayout(selectedBrushSurfaces,  1,-1);
						TooltipUtility.SetToolTip(ToolTipJustifyUpRight);
					}
					GUILayout.EndHorizontal();

					GUILayout.BeginHorizontal(CSG_GUIStyleUtility.ContentEmpty);
					{
						if (GUILayout.Button(ContentJustifyLeft,   justifyButtonLayout)) SurfaceUtility.JustifyLayoutX(selectedBrushSurfaces, -1);
						TooltipUtility.SetToolTip(ToolTipJustifyLeft);
						if (GUILayout.Button(ContentJustifyCenter, justifyButtonLayout)) SurfaceUtility.JustifyLayout(selectedBrushSurfaces,  0, 0);
						TooltipUtility.SetToolTip(ToolTipJustifyCenter);
						if (GUILayout.Button(ContentJustifyRight,  justifyButtonLayout)) SurfaceUtility.JustifyLayoutX(selectedBrushSurfaces,  1);
						TooltipUtility.SetToolTip(ToolTipJustifyRight);
					}
					GUILayout.EndHorizontal();

					GUILayout.BeginHorizontal(CSG_GUIStyleUtility.ContentEmpty);
					{
						if (GUILayout.Button(ContentJustifyDownLeft,  justifyButtonLayout)) SurfaceUtility.JustifyLayout(selectedBrushSurfaces, -1, 1);
						TooltipUtility.SetToolTip(ToolTipJustifyDownLeft);
						if (GUILayout.Button(ContentJustifyDown,	  justifyButtonLayout)) SurfaceUtility.JustifyLayoutY(selectedBrushSurfaces, 1);
						TooltipUtility.SetToolTip(ToolTipJustifyDown);
						if (GUILayout.Button(ContentJustifyDownRight, justifyButtonLayout)) SurfaceUtility.JustifyLayout(selectedBrushSurfaces,  1, 1);
						TooltipUtility.SetToolTip(ToolTipJustifyDownRight);
					}
					GUILayout.EndHorizontal();
				}
				GUILayout.EndVertical();
				
				if (!isSceneGUI)
				{
					GUILayout.EndHorizontal();
				}
			}
			GUILayout.EndVertical();
		}

		static void OnGUIContentsJustifyScene(Rect rect, SelectedBrushSurface[] selectedBrushSurfaces)
		{
			Rect tempRect = rect;
			tempRect.x = rect.x + 4;
			tempRect.y = rect.y + 1;
			tempRect.width  = 213;
			tempRect.height = 16;
			GUI.Label(tempRect, ContentJustifyLabel);
			{
				tempRect.width = 20;
				tempRect.height = 23;
				{
					tempRect.y = rect.y + 20;

					tempRect.x = rect.x + 4;
					if (GUI.Button(tempRect, ContentJustifyUpLeft))
						SurfaceUtility.JustifyLayout(selectedBrushSurfaces, -1,-1);
					TooltipUtility.SetToolTip(ToolTipJustifyUpLeft, tempRect);

					tempRect.x = rect.x + 30;
					if (GUI.Button(tempRect, ContentJustifyUp))
						SurfaceUtility.JustifyLayoutY(selectedBrushSurfaces, -1);
					TooltipUtility.SetToolTip(ToolTipJustifyUp, tempRect);

					tempRect.x = rect.x + 56;
					if (GUI.Button(tempRect, ContentJustifyUpRight))
						SurfaceUtility.JustifyLayout(selectedBrushSurfaces,  1,-1);
					TooltipUtility.SetToolTip(ToolTipJustifyUpRight, tempRect);
				}
				{
					tempRect.y = rect.y + 45;

					tempRect.x = rect.x + 4;
					if (GUI.Button(tempRect, ContentJustifyLeft))
						SurfaceUtility.JustifyLayoutX(selectedBrushSurfaces, -1);
					TooltipUtility.SetToolTip(ToolTipJustifyLeft, tempRect);

					tempRect.x = rect.x + 30;
					if (GUI.Button(tempRect, ContentJustifyCenter))
						SurfaceUtility.JustifyLayout(selectedBrushSurfaces,  0, 0);
					TooltipUtility.SetToolTip(ToolTipJustifyCenter, tempRect);

					tempRect.x = rect.x + 56;
					if (GUI.Button(tempRect, ContentJustifyRight))
						SurfaceUtility.JustifyLayoutX(selectedBrushSurfaces,  1);
					TooltipUtility.SetToolTip(ToolTipJustifyRight, tempRect);
				}
				{
					tempRect.y = rect.y + 70;

					tempRect.x = rect.x + 4;
					if (GUI.Button(tempRect, ContentJustifyDownLeft))
						SurfaceUtility.JustifyLayout(selectedBrushSurfaces, -1, 1);
					TooltipUtility.SetToolTip(ToolTipJustifyDownLeft, tempRect);

					tempRect.x = rect.x + 30;
					if (GUI.Button(tempRect, ContentJustifyDown))
						SurfaceUtility.JustifyLayoutY(selectedBrushSurfaces, 1);
					TooltipUtility.SetToolTip(ToolTipJustifyDown, tempRect);

					tempRect.x = rect.x + 56;
					if (GUI.Button(tempRect, ContentJustifyDownRight))
						SurfaceUtility.JustifyLayout(selectedBrushSurfaces,  1, 1);
					TooltipUtility.SetToolTip(ToolTipJustifyDownRight, tempRect);
				}
			}
		}

		static void OnGUIContentsMaterialImage(bool isSceneGUI, Material material, bool mixedValues, SelectedBrushSurface[] selectedBrushSurfaces)
		{
			GUILayout.BeginHorizontal(materialWidth, materialHeight);
			{
				//if (materialEditor == null || prevMaterial != material)
				{
					var editor = materialEditor as Editor;
					Editor.CreateCachedEditor(material, typeof(MaterialEditor), ref editor);
					materialEditor = editor as MaterialEditor;
					//prevMaterial = material;
				}

				if (materialEditor != null)
				{
					var rect = GUILayoutUtility.GetRect(materialSize, materialSize);
					EditorGUI.showMixedValue = mixedValues;
					materialEditor.OnPreviewGUI(rect, GUIStyle.none);
					EditorGUI.showMixedValue = false;
				} else
				{
					GUILayout.Box(new GUIContent(), CSG_GUIStyleUtility.emptyMaterialStyle, materialWidth, materialHeight);
				}
			}
			GUILayout.EndHorizontal();
			var currentArea = GUILayoutUtility.GetLastRect();
			var currentPoint = Event.current.mousePosition;
			if (currentArea.Contains(currentPoint))
			{
				if (Event.current.type == EventType.DragUpdated &&
					GetDragMaterial())
				{
					DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
					Event.current.Use();
				}
				if (Event.current.type == EventType.DragPerform)
				{
					var new_material = GetDragMaterial();
					if (new_material)
					{
						SurfaceUtility.SetMaterials(selectedBrushSurfaces, new_material);
						CSGSettings.DefaultMaterial = new_material;
						CSGSettings.Save();
						GUI.changed = true;
						Event.current.Use();
					}
				}
			}
		}

		static void OnGUIContentsMaterialImage(Rect currentArea, Material material, bool mixedValues, SelectedBrushSurface[] selectedBrushSurfaces)
		{
			//GUILayout.BeginHorizontal(materialWidth, materialHeight);
			{
				//if (materialEditor == null || prevMaterial != material)
				{
					var editor = materialEditor as Editor;
					Editor.CreateCachedEditor(material, typeof(MaterialEditor), ref editor);
					materialEditor = editor as MaterialEditor;
					//prevMaterial = material;
				}

				if (materialEditor != null)
				{
					//var currentArea = GUILayoutUtility.GetRect(materialSize, materialSize);
					EditorGUI.showMixedValue = mixedValues;
					materialEditor.OnPreviewGUI(currentArea, GUIStyle.none);
					EditorGUI.showMixedValue = false;
				} else
				{
					GUI.Box(currentArea, GUIContent.none, CSG_GUIStyleUtility.emptyMaterialStyle);
				}
			}
			//GUILayout.EndHorizontal();
			//var currentArea = GUILayoutUtility.GetLastRect();
			var currentPoint = Event.current.mousePosition;
			if (currentArea.Contains(currentPoint))
			{
				if (Event.current.type == EventType.DragUpdated &&
					GetDragMaterial())
				{
					DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
					Event.current.Use();
				}
				if (Event.current.type == EventType.DragPerform)
				{
					var new_material = GetDragMaterial();
					if (new_material)
					{
						SurfaceUtility.SetMaterials(selectedBrushSurfaces, new_material);
						CSGSettings.DefaultMaterial = new_material;
						CSGSettings.Save();
						GUI.changed = true;
						Event.current.Use();
					}
				}
			}
		}

		sealed class SurfaceState
		{
			public Material material;
			public TexGen	currentTexGen = new TexGen();

			public bool		haveTexgen;
//			public bool		multipleColors;
			public bool		multipleTranslationX;
			public bool		multipleTranslationY;
			public bool		multipleScaleX;
			public bool		multipleScaleY;
			public bool		multipleRotationAngle;
			public bool		multipleMaterials;
			public bool?	textureLocked;

			public bool		foundHelperMaterial;

			public RenderSurfaceType?	firstRenderSurfaceType;
			public Material				firstMaterial;

			public bool                 enabled;
			public bool                 canSmooth;
			public bool                 canUnSmooth;
			public SelectedBrushSurface[]   selectedBrushSurfaces;

			public EditModeCommonGUI.SurfaceFlagState surfaceFlagState = new EditModeCommonGUI.SurfaceFlagState();

			public void Init(EditModeSurface tool)
			{
				selectedBrushSurfaces = (tool == null) ? new SelectedBrushSurface[0] : tool.GetSelectedSurfaces();
				enabled = selectedBrushSurfaces.Length > 0;
				material = null;
				currentTexGen = new TexGen();

				surfaceFlagState.Init(surfaceState.selectedBrushSurfaces);

				haveTexgen				= false;
//				multipleColors			= !enabled;
				multipleTranslationX	= !enabled;
				multipleTranslationY	= !enabled;
				multipleScaleX			= !enabled;
				multipleScaleY			= !enabled;
				multipleRotationAngle	= !enabled;
				multipleMaterials		= !enabled;
				textureLocked			= null;

				foundHelperMaterial	= false;
				firstRenderSurfaceType = null;
				firstMaterial		= null;
				if (selectedBrushSurfaces.Length > 0)
				{
					for (var i = 0; i < selectedBrushSurfaces.Length; i++)
					{
						var brush			= selectedBrushSurfaces[i].brush;
						if (!brush)
							continue;

						var surfaceIndex	= selectedBrushSurfaces[i].surfaceIndex;
						if (surfaceIndex >= brush.Shape.Surfaces.Length)
						{
							Debug.LogWarning("surface_index >= brush.Shape.Surfaces.Length");
							continue; 
						}

						var texGenIndex	= brush.Shape.Surfaces[surfaceIndex].TexGenIndex;
						if (texGenIndex >= brush.Shape.TexGens.Length)
						{
							Debug.LogWarning("texGen_index >= brush.Shape.TexGens.Length");
							continue;
						}

						var brushCache	= InternalCSGModelManager.GetBrushCache(brush);
						var model		= (brushCache != null) ? brushCache.ChildData.Model : null;

						Material foundMaterial;
						var texGenFlags = brush.Shape.TexGenFlags[texGenIndex];
						if (model && (!model.IsRenderable))// || model.ShadowsOnly))
						{
							foundHelperMaterial = true;
							if (!firstRenderSurfaceType.HasValue)
								firstRenderSurfaceType = ModelTraits.GetModelSurfaceType(model);
							foundMaterial = null;
						} else
						if ((texGenFlags & TexGenFlags.NoRender) == TexGenFlags.NoRender)
						{
							foundHelperMaterial = true;
							if (!firstRenderSurfaceType.HasValue)
							{
								if ((texGenFlags & TexGenFlags.NoCastShadows) != TexGenFlags.NoCastShadows)
								{
									firstRenderSurfaceType = RenderSurfaceType.ShadowOnly;
								} else
								if ((texGenFlags & TexGenFlags.NoCollision) != TexGenFlags.NoCollision)
								{
									firstRenderSurfaceType = RenderSurfaceType.Collider;
								} else
								{
									firstRenderSurfaceType = RenderSurfaceType.Hidden;
								}
							}
							foundMaterial = null;
						} else
						{
							var surfaceMaterial = brush.Shape.TexGens[texGenIndex].RenderMaterial;
							if (!foundHelperMaterial)
							{
								var surfaceType		= MaterialUtility.GetMaterialSurfaceType(surfaceMaterial);
								if (!firstRenderSurfaceType.HasValue)
									firstRenderSurfaceType = surfaceType;
								foundHelperMaterial = surfaceType != RenderSurfaceType.Normal;
							}
							foundMaterial	= surfaceMaterial;
						}
						if ((texGenFlags & TexGenFlags.WorldSpaceTexture) == TexGenFlags.WorldSpaceTexture)
						{
							if (i == 0) textureLocked = false;
							else if (textureLocked.HasValue && textureLocked.Value)
								textureLocked = null;
						} else
						{
							if (i == 0) textureLocked = true;
							else if (textureLocked.HasValue && !textureLocked.Value)
								textureLocked = null;
						}
						if (foundMaterial != material)
						{
							if (!material)
							{
								firstMaterial = foundMaterial;
								material = foundMaterial;
							} else
								multipleMaterials = true;
						}
						if (!haveTexgen)
						{
							currentTexGen = brush.Shape.TexGens[texGenIndex];
							haveTexgen = true;
						} else
						{/*
							if (!multipleColors)
							{ 
								var color			= brush.Shape.TexGens[texGenIndex].Color;
								multipleColors		= currentTexGen.Color.a != color.a ||
													  currentTexGen.Color.b != color.b ||
													  currentTexGen.Color.g != color.g ||
													  currentTexGen.Color.r != color.r;
							}*/
							if (!multipleScaleX || !multipleScaleY)
							{ 
								var scale			= brush.Shape.TexGens[texGenIndex].Scale;
								multipleScaleX	= multipleScaleX || currentTexGen.Scale.x != scale.x;
								multipleScaleY	= multipleScaleY || currentTexGen.Scale.y != scale.y;
							}

							if (!multipleTranslationX || !multipleTranslationY)
							{ 
								var translation			= brush.Shape.TexGens[texGenIndex].Translation;
								multipleTranslationX	= multipleTranslationX || currentTexGen.Translation.x != translation.x;
								multipleTranslationY	= multipleTranslationY || currentTexGen.Translation.y != translation.y;
							}

							if (!multipleRotationAngle)
							{
								var rotationAngle		= brush.Shape.TexGens[texGenIndex].RotationAngle;
								multipleRotationAngle	= currentTexGen.RotationAngle != rotationAngle;
							}
						}
					}
					if (foundHelperMaterial && !firstMaterial)
					{
						if (firstRenderSurfaceType.HasValue)
							firstMaterial = MaterialUtility.GetSurfaceMaterial(firstRenderSurfaceType.Value);
						else
							firstMaterial = MaterialUtility.HiddenMaterial;
					}
				}			
				if (currentTexGen.Scale.x == 0.0f) currentTexGen.Scale.x = 1.0f;
				if (currentTexGen.Scale.y == 0.0f) currentTexGen.Scale.y = 1.0f;

				const float scale_round = 10000.0f;
				currentTexGen.Scale.x = Mathf.RoundToInt(currentTexGen.Scale.x * scale_round) / scale_round;
				currentTexGen.Scale.y = Mathf.RoundToInt(currentTexGen.Scale.y * scale_round) / scale_round;
				currentTexGen.Translation.x = Mathf.RoundToInt(currentTexGen.Translation.x * scale_round) / scale_round;
				currentTexGen.Translation.y = Mathf.RoundToInt(currentTexGen.Translation.y * scale_round) / scale_round;
				currentTexGen.RotationAngle = Mathf.RoundToInt(currentTexGen.RotationAngle * scale_round) / scale_round;

				canSmooth = SurfaceUtility.CanSmooth(surfaceState.selectedBrushSurfaces);
				canUnSmooth = SurfaceUtility.CanUnSmooth(surfaceState.selectedBrushSurfaces);
			}

		}

		static SurfaceState surfaceState;

		private static void OnGUIContents(Rect rect, EditModeSurface tool)
		{
			//EditModeCommonGUI.StartToolGUI();

			if (Event.current.type == EventType.Layout)
			{
				surfaceState = new SurfaceState();
				surfaceState.Init(tool);
			}

			if (surfaceState == null)
				return;
			
			var leftStyle	= EditorStyles.miniButtonLeft;
			var middleStyle	= EditorStyles.miniButtonMid;
			var rightStyle	= EditorStyles.miniButtonRight;

			rect.x += 5;
			rect.y += 24;

			Rect tempRect = rect;
			EditorGUI.BeginDisabledGroup(!surfaceState.enabled);
			{
				{
					tempRect.Set(rect.x + 4, rect.y, 213, 134);
					GUI.Box(tempRect, GUIContent.none, GUI.skin.box);
					{
						EditorGUI.BeginDisabledGroup(surfaceState.material == null);
						{
							Material newMaterial; 
							EditorGUI.BeginChangeCheck();
							{
								EditorGUI.showMixedValue = surfaceState.multipleMaterials;
								tempRect.Set(rect.x+8, rect.y+4, 204, 16);
								newMaterial = EditorGUI.ObjectField(tempRect, surfaceState.material, typeof(Material), true) as Material;
								EditorGUI.showMixedValue = false;
							}
							if (EditorGUI.EndChangeCheck())
							{
								if (newMaterial)
								{
									SurfaceUtility.SetMaterials(surfaceState.selectedBrushSurfaces, newMaterial);
									CSGSettings.DefaultMaterial = newMaterial;
									CSGSettings.Save(); 
								}
							}
						}
						EditorGUI.EndDisabledGroup();

						{
							tempRect.Set(rect.x+9, rect.y+26, 100, 100);
							OnGUIContentsMaterialImage(tempRect, surfaceState.firstMaterial, surfaceState.multipleMaterials, surfaceState.selectedBrushSurfaces);
							tempRect.Set(rect.x+131-9, rect.y+28, 74, 91);
							OnGUIContentsJustifyScene(tempRect, surfaceState.selectedBrushSurfaces);
						}
					}

					tempRect.Set(rect.x+4, rect.y+138,  213, 22);
					GUI.Box(tempRect, GUIContent.none, GUI.skin.box);
					{						
						EditorGUI.BeginChangeCheck();
						{
							EditorGUI.showMixedValue = !surfaceState.textureLocked.HasValue;
							tempRect.Set(rect.x+8, rect.y+141, 205, 16);
							surfaceState.textureLocked = EditorGUI.ToggleLeft(tempRect, ContentLockTexture, surfaceState.textureLocked.HasValue ? surfaceState.textureLocked.Value : false);
							TooltipUtility.SetToolTip(ToolTipLockTexture, tempRect);
						}
						if (EditorGUI.EndChangeCheck())
						{
							SurfaceUtility.SetTextureLock(surfaceState.selectedBrushSurfaces, surfaceState.textureLocked.Value);
						}
					}

					tempRect.Set(rect.x+4, rect.y+164, 213, 58);
					GUI.Box(tempRect, GUIContent.none, GUI.skin.box);
					{
						{
							tempRect.Set(rect.x+8, rect.y+167, 56, 16);
							EditorGUI.LabelField(tempRect, ContentUVScale, EditorStyles.miniLabel);
							TooltipUtility.SetToolTip(ToolTipScaleUV, tempRect); 

							{ 
								EditorGUI.BeginChangeCheck();
								{
									EditorGUI.showMixedValue = surfaceState.multipleScaleX;
									tempRect.Set(rect.x + 68, rect.y + 167, 70, 16);
									surfaceState.currentTexGen.Scale.x = EditorGUI.FloatField(tempRect, surfaceState.currentTexGen.Scale.x);
								}
								if (EditorGUI.EndChangeCheck()) { SurfaceUtility.SetScaleX(surfaceState.selectedBrushSurfaces, surfaceState.currentTexGen.Scale.x); }	
								EditorGUI.BeginChangeCheck();
								{
									EditorGUI.showMixedValue = surfaceState.multipleScaleY;
									tempRect.Set(rect.x + 142, rect.y + 167, 70, 16);
									surfaceState.currentTexGen.Scale.y = EditorGUI.FloatField(tempRect, surfaceState.currentTexGen.Scale.y);
								}
								if (EditorGUI.EndChangeCheck()) { SurfaceUtility.SetScaleY(surfaceState.selectedBrushSurfaces, surfaceState.currentTexGen.Scale.y); }
							}
						}
						{
							tempRect.Set(rect.x+8, rect.y+185, 56, 16);
							EditorGUI.LabelField(tempRect, ContentOffset, EditorStyles.miniLabel);
							TooltipUtility.SetToolTip(ToolTipOffsetUV, tempRect);

							{ 
								EditorGUI.BeginChangeCheck();
								{
									EditorGUI.showMixedValue = surfaceState.multipleTranslationX;
									tempRect.Set(rect.x + 68, rect.y + 185, 70, 16);
									surfaceState.currentTexGen.Translation.x = EditorGUI.FloatField(tempRect, surfaceState.currentTexGen.Translation.x);
								}
								if (EditorGUI.EndChangeCheck()) { SurfaceUtility.SetTranslationX(surfaceState.selectedBrushSurfaces, surfaceState.currentTexGen.Translation.x); }
								
								EditorGUI.BeginChangeCheck();
								{
									EditorGUI.showMixedValue = surfaceState.multipleTranslationY;
									tempRect.Set(rect.x + 142, rect.y + 185, 70, 16);
									surfaceState.currentTexGen.Translation.y = EditorGUI.FloatField(tempRect, surfaceState.currentTexGen.Translation.y);
								}
								if (EditorGUI.EndChangeCheck()) { SurfaceUtility.SetTranslationY(surfaceState.selectedBrushSurfaces, surfaceState.currentTexGen.Translation.y); }
							}
						}
						{
							tempRect.Set(rect.x+8, rect.y+203, 56, 16);
							EditorGUI.LabelField(tempRect, ContentRotate, EditorStyles.miniLabel);
							TooltipUtility.SetToolTip(ToolTipRotation, tempRect);

							{ 
								EditorGUI.BeginChangeCheck();
								{
									EditorGUI.showMixedValue = surfaceState.multipleRotationAngle;
									tempRect.Set(rect.x+68, rect.y+203, 57, 16);
									surfaceState.currentTexGen.RotationAngle = EditorGUI.FloatField(tempRect, surfaceState.currentTexGen.RotationAngle);
								}
								if (EditorGUI.EndChangeCheck()) { SurfaceUtility.SetRotationAngle(surfaceState.selectedBrushSurfaces, surfaceState.currentTexGen.RotationAngle); }
							}
							{
								tempRect.Set(rect.x+129, rect.y+203, 42, 15);
								if (GUI.Button(tempRect, ContentRotate90Negative, leftStyle))
								{ SurfaceUtility.AddRotationAngle(surfaceState.selectedBrushSurfaces, -90.0f); } 
								TooltipUtility.SetToolTip(ToolTipRotate90Negative, tempRect);

								tempRect.x = rect.x + 171;
								if (GUI.Button(tempRect, ContentRotate90Positive, rightStyle))
								{ SurfaceUtility.AddRotationAngle(surfaceState.selectedBrushSurfaces, +90.0f); }
								TooltipUtility.SetToolTip(ToolTipRotate90Positive, tempRect);
							}
						}
					}

					tempRect.Set(rect.x+4, rect.y+226, 213, 76);
					GUI.Box(tempRect, GUIContent.none, GUI.skin.box);
					{
						{
							tempRect.Set(rect.x+8, rect.y+229,  45, 16);
							GUI.Label(tempRect, ContentFit, EditorStyles.miniLabel);

							tempRect.Set(rect.x+57, rect.y+229, 49, 15);
							if (GUI.Button(tempRect, ContentFitX, leftStyle))
							{ SurfaceUtility.FitSurfaceX(surfaceState.selectedBrushSurfaces); }
							TooltipUtility.SetToolTip(ToolTipFitX, tempRect);

							tempRect.Set(rect.x+106, rect.y+229, 57, 15);
							if (GUI.Button(tempRect, ContentFitXY, middleStyle))
							{ SurfaceUtility.FitSurface (surfaceState.selectedBrushSurfaces); }
							TooltipUtility.SetToolTip(ToolTipFitXY, tempRect);

							tempRect.Set(rect.x+164, rect.y+229, 49, 15);
							if (GUI.Button(tempRect, ContentFitY , rightStyle ))
							{ SurfaceUtility.FitSurfaceY(surfaceState.selectedBrushSurfaces); }
							TooltipUtility.SetToolTip(ToolTipFitY, tempRect);
						}
						{
							tempRect.Set(rect.x+8, rect.y+247, 45, 16);
							GUI.Label(tempRect, ContentReset, EditorStyles.miniLabel);

							tempRect.Set(rect.x+57, rect.y+247, 50, 15);
							if (GUI.Button(tempRect, ContentResetX , leftStyle  ))
							{ SurfaceUtility.ResetSurfaceX(surfaceState.selectedBrushSurfaces); }
							TooltipUtility.SetToolTip(ToolTipResetX, tempRect);

							tempRect.Set(rect.x+107, rect.y+247, 58, 15);
							if (GUI.Button(tempRect, ContentResetY , rightStyle ))
							{ SurfaceUtility.ResetSurface (surfaceState.selectedBrushSurfaces); }
							TooltipUtility.SetToolTip(ToolTipResetXY, tempRect);

							tempRect.Set(rect.x+164, rect.y+247, 49, 15);
							if (GUI.Button(tempRect, ContentResetY , rightStyle ))
							{ SurfaceUtility.ResetSurfaceY(surfaceState.selectedBrushSurfaces); }
							TooltipUtility.SetToolTip(ToolTipResetY, tempRect);
						}
						{
							tempRect.Set(rect.x+8, rect.y+265, 45, 16);
							GUI.Label(tempRect, ContentFlip, EditorStyles.miniLabel);

							tempRect.Set(rect.x+57, rect.y+265, 49, 15);
							if (GUI.Button(tempRect, ContentFlipX , leftStyle  ))
							{ SurfaceUtility.FlipX(surfaceState.selectedBrushSurfaces);  }
							TooltipUtility.SetToolTip(ToolTipFlipX, tempRect);

							tempRect.Set(rect.x+106, rect.y+265, 57, 15);
							if (GUI.Button(tempRect, ContentFlipXY, middleStyle))
							{ SurfaceUtility.FlipXY(surfaceState.selectedBrushSurfaces); }
							TooltipUtility.SetToolTip(ToolTipFlipXY, tempRect);

							tempRect.Set(rect.x+164, rect.y+265, 49, 15);
							if (GUI.Button(tempRect, ContentFlipY , rightStyle ))
							{ SurfaceUtility.FlipY(surfaceState.selectedBrushSurfaces);  }
							TooltipUtility.SetToolTip(ToolTipFlipY, tempRect);
						}
						{
							tempRect.Set(rect.x+8, rect.y+283, 45, 16);
							GUI.Label(tempRect, ContentScale, EditorStyles.miniLabel);

							tempRect.Set(rect.x+57, rect.y+283, 79, 15);
							if (GUI.Button(tempRect, ContentDoubleScale , leftStyle  ))
							{ SurfaceUtility.MultiplyScale(surfaceState.selectedBrushSurfaces, 2.0f); }
							TooltipUtility.SetToolTip(ToolTipDoubleScale, tempRect);

							tempRect.Set(rect.x+136, rect.y+283, 77, 15);
							if (GUI.Button(tempRect, ContentHalfScale   , rightStyle ))
							{ SurfaceUtility.MultiplyScale(surfaceState.selectedBrushSurfaces, 0.5f); }
							TooltipUtility.SetToolTip(ToolTipHalfScale, tempRect);
						}
					}

					tempRect.Set(rect.x+4, rect.y+306, 213, 56);
					GUI.Box(tempRect, GUIContent.none, GUI.skin.box);
					{
						tempRect.Set(rect.x+8-4, rect.y+327 - 19, 205, 15);
						EditModeCommonGUI.OnSurfaceFlagButtons(tempRect, surfaceState.surfaceFlagState, surfaceState.selectedBrushSurfaces);
						{
							EditorGUI.BeginDisabledGroup(!surfaceState.canSmooth);
							{
								tempRect.Set(rect.x+8, rect.y+344, 94, 15);
								if (GUI.Button(tempRect, ContentSmoothSurfaces, leftStyle))
								{ SurfaceUtility.Smooth(surfaceState.selectedBrushSurfaces); }
								TooltipUtility.SetToolTip(ToolTipSmoothSurfaces, tempRect);
							}
							EditorGUI.EndDisabledGroup();
							EditorGUI.BeginDisabledGroup(!surfaceState.canUnSmooth);
							{
								tempRect.Set(rect.x+102, rect.y+344, 112, 15);
								if (GUI.Button(tempRect, ContentUnSmoothSurfaces, rightStyle))
								{ SurfaceUtility.UnSmooth(surfaceState.selectedBrushSurfaces); }
								TooltipUtility.SetToolTip(ToolTipUnSmoothSurfaces, tempRect);
							}
							EditorGUI.EndDisabledGroup();
						}
					}
				}
			}
			EditorGUI.EndDisabledGroup();
			EditorGUI.showMixedValue = false;
		}
		
		private static void OnGUIContents(bool isSceneGUI, EditModeSurface tool)
		{
			EditModeCommonGUI.StartToolGUI();

			if (Event.current.type == EventType.Layout)
			{
				surfaceState = new SurfaceState();
				surfaceState.Init(tool);
			}

			if (surfaceState == null)
				return;
			
			var leftStyle	= isSceneGUI ? EditorStyles.miniButtonLeft  : GUI.skin.button;
			var middleStyle	= isSceneGUI ? EditorStyles.miniButtonMid   : GUI.skin.button;
			var rightStyle	= isSceneGUI ? EditorStyles.miniButtonRight : GUI.skin.button;

			EditorGUI.BeginDisabledGroup(!surfaceState.enabled);
			{
				GUILayout.BeginVertical(CSG_GUIStyleUtility.ContentEmpty);
				{
					EditorGUILayout.Space();
					
					GUILayout.BeginVertical(GUIStyle.none);
					{						
						EditorGUI.BeginChangeCheck();
						{
							EditorGUI.showMixedValue = !surfaceState.textureLocked.HasValue;
							surfaceState.textureLocked = EditorGUILayout.ToggleLeft(ContentLockTexture, surfaceState.textureLocked.HasValue ? surfaceState.textureLocked.Value : false);
							TooltipUtility.SetToolTip(ToolTipLockTexture);
						}
						if (EditorGUI.EndChangeCheck())
						{
							SurfaceUtility.SetTextureLock(surfaceState.selectedBrushSurfaces, surfaceState.textureLocked.Value);
						}							
					}
					GUILayout.EndVertical();

					GUILayout.BeginVertical(GUIStyle.none);
					{ 				
						GUILayout.BeginHorizontal(CSG_GUIStyleUtility.ContentEmpty);
						{
							EditorGUILayout.LabelField(ContentUVScale, largeLabelWidth);
							TooltipUtility.SetToolTip(ToolTipScaleUV); 

							GUILayout.BeginHorizontal(CSG_GUIStyleUtility.ContentEmpty);
							{ 
								EditorGUILayout.LabelField(ContentUSymbol, unitWidth);
								EditorGUI.BeginChangeCheck();
								{
									EditorGUI.showMixedValue = surfaceState.multipleScaleX;
									surfaceState.currentTexGen.Scale.x = EditorGUILayout.FloatField(surfaceState.currentTexGen.Scale.x, minFloatFieldWidth);
								}
								if (EditorGUI.EndChangeCheck()) { SurfaceUtility.SetScaleX(surfaceState.selectedBrushSurfaces, surfaceState.currentTexGen.Scale.x); }	
								EditorGUILayout.LabelField(ContentVSymbol, unitWidth);
								EditorGUI.BeginChangeCheck();
								{
									EditorGUI.showMixedValue = surfaceState.multipleScaleY;
									surfaceState.currentTexGen.Scale.y = EditorGUILayout.FloatField(surfaceState.currentTexGen.Scale.y, minFloatFieldWidth);
								}
								if (EditorGUI.EndChangeCheck()) { SurfaceUtility.SetScaleY(surfaceState.selectedBrushSurfaces, surfaceState.currentTexGen.Scale.y); }
							}
							GUILayout.EndHorizontal();
						}
						GUILayout.EndHorizontal();
						GUILayout.BeginHorizontal(CSG_GUIStyleUtility.ContentEmpty);
						{
							EditorGUILayout.LabelField(ContentOffset, largeLabelWidth);
							TooltipUtility.SetToolTip(ToolTipOffsetUV);

							GUILayout.BeginHorizontal(CSG_GUIStyleUtility.ContentEmpty);
							{ 
								EditorGUILayout.LabelField(ContentUSymbol, unitWidth);
								EditorGUI.BeginChangeCheck();
								{
									EditorGUI.showMixedValue = surfaceState.multipleTranslationX;
									surfaceState.currentTexGen.Translation.x = EditorGUILayout.FloatField(surfaceState.currentTexGen.Translation.x, minFloatFieldWidth);
								}
								if (EditorGUI.EndChangeCheck()) { SurfaceUtility.SetTranslationX(surfaceState.selectedBrushSurfaces, surfaceState.currentTexGen.Translation.x); }
								
								EditorGUILayout.LabelField(ContentVSymbol, unitWidth);
								EditorGUI.BeginChangeCheck();
								{
									EditorGUI.showMixedValue = surfaceState.multipleTranslationY;
									surfaceState.currentTexGen.Translation.y = EditorGUILayout.FloatField(surfaceState.currentTexGen.Translation.y, minFloatFieldWidth);
								}
								if (EditorGUI.EndChangeCheck()) { SurfaceUtility.SetTranslationY(surfaceState.selectedBrushSurfaces, surfaceState.currentTexGen.Translation.y); }
							}
							GUILayout.EndHorizontal();
						}
						GUILayout.EndHorizontal();
						GUILayout.BeginHorizontal(CSG_GUIStyleUtility.ContentEmpty);
						{
							EditorGUILayout.LabelField(ContentRotate, largeLabelWidth);
							TooltipUtility.SetToolTip(ToolTipRotation);

							GUILayout.BeginVertical(CSG_GUIStyleUtility.ContentEmpty);
								
							GUILayout.BeginHorizontal(CSG_GUIStyleUtility.ContentEmpty);
							{ 
								EditorGUI.BeginChangeCheck();
								{
									EditorGUI.showMixedValue = surfaceState.multipleRotationAngle;
									surfaceState.currentTexGen.RotationAngle = EditorGUILayout.FloatField(surfaceState.currentTexGen.RotationAngle, minFloatFieldWidth);
									EditorGUILayout.LabelField(ContentAngleSymbol, unitWidth);
								}
								if (EditorGUI.EndChangeCheck()) { SurfaceUtility.SetRotationAngle(surfaceState.selectedBrushSurfaces, surfaceState.currentTexGen.RotationAngle); }
							}
							GUILayout.EndHorizontal();
							
							var buttonWidth = new GUILayoutOption[0];
							GUILayout.BeginHorizontal(CSG_GUIStyleUtility.ContentEmpty);
							{
								if (GUILayout.Button(ContentRotate90Negative, leftStyle,  buttonWidth)) { SurfaceUtility.AddRotationAngle(surfaceState.selectedBrushSurfaces, -90.0f); }
								TooltipUtility.SetToolTip(ToolTipRotate90Negative);
								if (GUILayout.Button(ContentRotate90Positive, rightStyle, buttonWidth)) { SurfaceUtility.AddRotationAngle(surfaceState.selectedBrushSurfaces, +90.0f); }
								TooltipUtility.SetToolTip(ToolTipRotate90Positive);
							}
							GUILayout.EndHorizontal();
							GUILayout.EndVertical();
						}
						GUILayout.EndHorizontal();
					}
					GUILayout.EndVertical();

					EditorGUILayout.Space();
					
					GUILayout.BeginVertical(GUIStyle.none);
					{ 				
						GUILayout.BeginHorizontal(CSG_GUIStyleUtility.ContentEmpty);
						{
							GUILayout.Label(ContentFit, largeLabelWidth);
							if (GUILayout.Button(ContentFitX , leftStyle  )) { SurfaceUtility.FitSurfaceX(surfaceState.selectedBrushSurfaces); }
							TooltipUtility.SetToolTip(ToolTipFitX);
							if (GUILayout.Button(ContentFitXY, middleStyle)) { SurfaceUtility.FitSurface(surfaceState.selectedBrushSurfaces); }
							TooltipUtility.SetToolTip(ToolTipFitXY);
							if (GUILayout.Button(ContentFitY , rightStyle )) { SurfaceUtility.FitSurfaceY(surfaceState.selectedBrushSurfaces); }
							TooltipUtility.SetToolTip(ToolTipFitY);
						}
						GUILayout.EndHorizontal();
						GUILayout.BeginHorizontal(CSG_GUIStyleUtility.ContentEmpty);
						{
							GUILayout.Label(ContentReset, largeLabelWidth);
							if (GUILayout.Button(ContentResetX , leftStyle  )) { SurfaceUtility.ResetSurfaceX(surfaceState.selectedBrushSurfaces); }
							TooltipUtility.SetToolTip(ToolTipResetX);
							if (GUILayout.Button(ContentResetXY, middleStyle)) { SurfaceUtility.ResetSurface(surfaceState.selectedBrushSurfaces);  }
							TooltipUtility.SetToolTip(ToolTipResetXY);
							if (GUILayout.Button(ContentResetY , rightStyle )) { SurfaceUtility.ResetSurfaceY(surfaceState.selectedBrushSurfaces); }
							TooltipUtility.SetToolTip(ToolTipResetY);
							
						}
						GUILayout.EndHorizontal();
						GUILayout.BeginHorizontal(CSG_GUIStyleUtility.ContentEmpty);
						{
							GUILayout.Label(ContentFlip, largeLabelWidth);
							if (GUILayout.Button(ContentFlipX , leftStyle  ))	{ SurfaceUtility.FlipX(surfaceState.selectedBrushSurfaces); }
							TooltipUtility.SetToolTip(ToolTipFlipX);
							if (GUILayout.Button(ContentFlipXY, middleStyle))	{ SurfaceUtility.FlipXY(surfaceState.selectedBrushSurfaces); }
							TooltipUtility.SetToolTip(ToolTipFlipXY);
							if (GUILayout.Button(ContentFlipY , rightStyle ))	{ SurfaceUtility.FlipY(surfaceState.selectedBrushSurfaces); }
							TooltipUtility.SetToolTip(ToolTipFlipY);
						}
						GUILayout.EndHorizontal();
						GUILayout.BeginHorizontal(CSG_GUIStyleUtility.ContentEmpty);
						{
							GUILayout.Label(ContentScale, largeLabelWidth);
							if (GUILayout.Button(ContentDoubleScale , leftStyle  ))	{ SurfaceUtility.MultiplyScale(surfaceState.selectedBrushSurfaces, 2.0f); }
							TooltipUtility.SetToolTip(ToolTipDoubleScale);
							if (GUILayout.Button(ContentHalfScale   , rightStyle ))	{ SurfaceUtility.MultiplyScale(surfaceState.selectedBrushSurfaces, 0.5f); }
							TooltipUtility.SetToolTip(ToolTipHalfScale);
						}
						GUILayout.EndHorizontal();
					}
					GUILayout.EndVertical();

					EditorGUILayout.Space();
					
					OnGUIContentsJustify(false, surfaceState.selectedBrushSurfaces);
						
					EditorGUILayout.Space();
						
					GUILayout.BeginVertical(GUIStyle.none);
					{
						EditModeCommonGUI.OnSurfaceFlagButtons(surfaceState.surfaceFlagState, surfaceState.selectedBrushSurfaces, false);
						GUILayout.BeginHorizontal(CSG_GUIStyleUtility.ContentEmpty);
						{
							EditorGUI.BeginDisabledGroup(!surfaceState.canSmooth);
							{
								if (GUILayout.Button(ContentSmoothSurfaces, leftStyle)) { SurfaceUtility.Smooth(surfaceState.selectedBrushSurfaces); }
								TooltipUtility.SetToolTip(ToolTipSmoothSurfaces);
							}
							EditorGUI.EndDisabledGroup();
							EditorGUI.BeginDisabledGroup(!surfaceState.canUnSmooth);
							{
								if (GUILayout.Button(ContentUnSmoothSurfaces, rightStyle)) { SurfaceUtility.UnSmooth(surfaceState.selectedBrushSurfaces); }
								TooltipUtility.SetToolTip(ToolTipUnSmoothSurfaces);
							}
							EditorGUI.EndDisabledGroup();
						}
						GUILayout.EndHorizontal();
					}
					GUILayout.EndVertical();
						
					EditorGUILayout.Space();
					Material new_material;
					GUILayout.BeginHorizontal(CSG_GUIStyleUtility.ContentEmpty);
					{
						EditorGUILayout.LabelField(ContentMaterial, largeLabelWidth);
						GUILayout.BeginVertical(CSG_GUIStyleUtility.ContentEmpty);
						{
							EditorGUI.BeginChangeCheck();
							{
								EditorGUI.showMixedValue = surfaceState.multipleMaterials;
								new_material = EditorGUILayout.ObjectField(surfaceState.material, typeof(Material), true) as Material;
								EditorGUI.showMixedValue = false;
							}
							if (EditorGUI.EndChangeCheck())
							{
								if (!new_material)
									new_material = MaterialUtility.MissingMaterial;
								SurfaceUtility.SetMaterials(surfaceState.selectedBrushSurfaces, new_material);
							}
						}
						GUILayout.Space(2);
						GUILayout.BeginHorizontal(CSG_GUIStyleUtility.ContentEmpty);
						{
							GUILayout.Space(5);
							OnGUIContentsMaterialImage(false, surfaceState.firstMaterial, surfaceState.multipleMaterials, surfaceState.selectedBrushSurfaces);
						}
						GUILayout.EndHorizontal();
						GUILayout.EndVertical();
					}
					GUILayout.EndHorizontal();
				}
				GUILayout.EndVertical();
			}
			EditorGUI.EndDisabledGroup();
			EditorGUI.showMixedValue = false;
		}
		
		static Rect lastGuiRect;
		public static Rect GetLastSceneGUIRect(EditModeSurface tool)
		{
			return lastGuiRect;
		}
		

		static Rect sceneGUIRect = new Rect(0, 0, 232, 0);
		public static void OnSceneGUI(Rect windowRect, EditModeSurface tool)
		{
			CSG_GUIStyleUtility.InitStyles();
			InitLocalStyles();

			var maxHeight = windowRect.height - 80;
			
			CSG_GUIStyleUtility.ResetGUIState();
						
			GUIStyle windowStyle = GUI.skin.window;

			var height = Math.Min(395, maxHeight);
			var y = windowRect.height - height;// 252

			var currentArea = sceneGUIRect;
			currentArea.y = y;
			currentArea.height = height;
			GUI.Box(currentArea, ContentSurfacesLabel, windowStyle);
			{
				OnGUIContents(currentArea, tool);
			}

			lastGuiRect = currentArea;
						
			var buttonArea = currentArea;
			buttonArea.x += buttonArea.width - 17;
			buttonArea.y += 2;
			buttonArea.height = 13;
			buttonArea.width = 13;
			if (GUI.Button(buttonArea, GUIContent.none, "WinBtnClose"))
				EditModeToolWindowSceneGUI.GetWindow();

			TooltipUtility.SetToolTip(CSG_GUIStyleUtility.PopOutTooltip, buttonArea);
			int controlID = GUIUtility.GetControlID(SceneViewSurfaceOverlayHash, FocusType.Keyboard, currentArea);
			switch (Event.current.GetTypeForControl(controlID))
			{
				case EventType.MouseDown:	{ if (currentArea.Contains(Event.current.mousePosition)) { GUIUtility.hotControl = controlID; GUIUtility.keyboardControl = controlID; Event.current.Use(); } break; }
				case EventType.MouseMove:	{ if (currentArea.Contains(Event.current.mousePosition)) { Event.current.Use(); } break; }
				case EventType.MouseUp:		{ if (GUIUtility.hotControl == controlID) { GUIUtility.hotControl = 0; GUIUtility.keyboardControl = 0; Event.current.Use(); } break; }
				case EventType.MouseDrag:	{ if (GUIUtility.hotControl == controlID) { Event.current.Use(); } break; }
				case EventType.ScrollWheel: { if (currentArea.Contains(Event.current.mousePosition)) { Event.current.Use(); } break; }
			}
		}

		public static void OnInspectorGUI(EditorWindow window, float height)
		{
			lastGuiRect = Rect.MinMaxRect(-1, -1, -1, -1);
			var tool = EditModeManager.ActiveTool as EditModeSurface;

			CSG_GUIStyleUtility.InitStyles();
			InitLocalStyles();
			OnGUIContents(false, tool);
		}
	}
}
