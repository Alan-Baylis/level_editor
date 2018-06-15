using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using InternalRealtimeCSG;

namespace RealtimeCSG
{
	internal sealed partial class EditModeMeshModeGUI
	{
		static GUIContent			ContentMeshLabel;
//		static GUIContent			ContentBrushesLabel;
//		static GUIContent			ContentEdgesLabel;
		private static readonly GUIContent	ContentDefaultMaterial	= new GUIContent("Default");
		
		private static readonly GUILayoutOption labelWidth			= GUILayout.Width(23);
		private static readonly GUILayoutOption largeLabelWidth		= GUILayout.Width(80);
		private static readonly GUILayoutOption[] InSceneWidth		= new GUILayoutOption[] { GUILayout.Width(150) };

		/*
		static readonly CSGOperationType[] operationValues = new CSGOperationType[]
			{
				CSGOperationType.Additive,
				CSGOperationType.Subtractive,
				CSGOperationType.Intersecting
			};
		*/
		static void InitLocalStyles()
		{
			if (ContentMeshLabel != null)
				return;
			ContentMeshLabel	= new GUIContent(CSG_GUIStyleUtility.brushEditModeNames[(int)ToolEditMode.Mesh]);
//			ContentBrushesLabel	= new GUIContent(GUIStyleUtility.brushEditModeNames[(int)BrushEditMode.Brushes]);
//			ContentEdgesLabel	= new GUIContent("Edges");
		}

		static GUIContent	ContentFlip			= new GUIContent("Flip");
		static GUIContent	ContentFlipX		= new GUIContent("X");
		static ToolTip		TooltipFlipX		= new ToolTip("Flip X", "Flip the selection in the x direction", Keys.FlipSelectionX);
		static GUIContent	ContentFlipY		= new GUIContent("Y");
		static ToolTip		TooltipFlipY		= new ToolTip("Flip Y", "Flip the selection in the y direction", Keys.FlipSelectionY);
		static GUIContent	ContentFlipZ		= new GUIContent("Z");
		static ToolTip		TooltipFlipZ		= new ToolTip("Flip Z", "Flip the selection in the z direction", Keys.FlipSelectionZ);
		static GUIContent	ContentSnapToGrid	= new GUIContent("Snap to grid");
		static ToolTip		TooltipSnapToGrid	= new ToolTip(ContentSnapToGrid.text, "Snap the selection to the closest grid lines", Keys.SnapToGridKey);
	}
}
