﻿using UnityEngine;
using System.Collections;
using UnityEditor;
using System.Collections.Generic;

namespace ReferenceExplorer
{
	public class FromObjectReferenceWindow :  EditorWindow
	{
		Vector2 current;
		List<ReferenceObject> referenceObjectList = new List<ReferenceObject> ();
		List<ReferenceObjectItem> refCompItems = new List<ReferenceObjectItem>();

		Texture fromRefImage;

		public bool ignoreSelfReference = false;


		//[MenuItem("Window/Referenced/From Object")]
		static void Init ()
		{
			var window = (FromObjectReferenceWindow)GetWindow (typeof(FromObjectReferenceWindow));
			window.title = "from";
			window.Show ();
		}


		public FromObjectReferenceWindow()
		{
			fromRefImage = AssetDatabase.LoadAssetAtPath ("Assets/SceneObjectReference/Editor/fromRef.png", typeof(Texture2D)) as Texture2D;
		}

		void OnEnable ()
		{
			SceneObjectUtility.UpdateGlovalReferenceList ();

			SceneView.onSceneGUIDelegate -= OnSceneGUI;
			SceneView.onSceneGUIDelegate += OnSceneGUI;
		}
	
		void OnDisable ()
		{
			SceneView.onSceneGUIDelegate -= OnSceneGUI;
		}
	
		void OnInspectorUpdate ()
		{
			Repaint ();
		}

		void OnHierarchyChange ()
		{
			SceneObjectUtility.UpdateGlovalReferenceList ();
		}
	
		public void OnSelectionChange ()
		{
			referenceObjectList.Clear ();
			SceneObjectUtility.UpdateGlovalReferenceList ();

			foreach( var selection in Selection.gameObjects)
			{
				SceneObjectUtility.FindReferenceObject (selection, referenceObjectList);
			}


			if( ignoreSelfReference )
			{
				foreach( var selection in Selection.gameObjects )
				{
					referenceObjectList.RemoveAll( item => item.referenceComponent.gameObject == selection );
				}
			}

			referenceObjectList.Sort ((x, y) => GetObjectID (x.referenceComponent) - GetObjectID (y.referenceComponent));

			refCompItems.Clear();
			foreach (var referenceObject in referenceObjectList) {
				if (! refCompItems.Exists( item => item.componentType == referenceObject.referenceComponent.GetType())) {
					refCompItems.Add (new ReferenceObjectItem(){ 
						componentType = referenceObject.referenceComponent.GetType(),
						isDisplay = true,
					});
				}
			}
		}
	
		void OnSceneGUI (SceneView sceneView)
		{
			if( Selection.activeGameObject == null )
				return;

			foreach( var selection in Selection.gameObjects)
			{
				SceneGuiLineWriter(selection);
			}
		}

		void SceneGuiLineWriter(GameObject selection)
		{
			var cameraTransform = SceneView.currentDrawingSceneView.camera.transform;
			var rotate = cameraTransform.rotation;
			var cameraPos = cameraTransform.position;
			
			Color shadowCol = new Color (0, 0, 0.5f, 0.06f);
			
			var enableTypeList = refCompItems.FindAll( item => item.isDisplay == true );

			//referenceObjectList.RemoveAll( item => (item is Component) == false);
			
			foreach (var refs in referenceObjectList.FindAll(item => ((Component)item.value).gameObject == selection )) {


				var obj = SceneObjectUtility.GetGameObject (refs.referenceComponent);
				
				var startPosition = selection.transform.position;
				var endPosition = obj.transform.position;
				
				var size = Vector3.Distance (endPosition, cameraPos) * 0.02f;
				
				if (startPosition == endPosition)
					continue;
				
				Handles.color = Color.blue;
				
				var diffPos = startPosition - endPosition;
				var tan = new Vector3 (diffPos.y, diffPos.x, diffPos.z);
				
				
				var startTan = startPosition;
				var endTan = endPosition + tan * 0.4f;
				
				Handles.CircleCap (1, endPosition, rotate, size);
				
				for (int i=0; i<3; i++)
					Handles.DrawBezier (startPosition, endPosition, startTan, endTan, shadowCol, null, (i + 1) * 5);
				Handles.DrawBezier (startPosition, endPosition, startTan, endTan, Color.blue, null, 1);
				Handles.Label (endPosition, obj.name);
			}	
		}

		int GetObjectID (object obj)
		{
			if (obj is Component)
				return ((Component)obj).GetInstanceID ();
			if (obj is GameObject)
				return ((GameObject)obj).GetInstanceID ();

			return -1;
		}
	
		public void OnGUI ()
		{	
			GUIStyle styles = new GUIStyle ();
			styles.margin.left = 10;
			styles.margin.top = 5;

			EditorGUILayout.BeginHorizontal("box");
			
			var iconSize = EditorGUIUtility.GetIconSize();
			EditorGUIUtility.SetIconSize(new Vector2(16, 16));
			GUILayout.Label(fromRefImage);
			EditorGUIUtility.SetIconSize(iconSize);
			
			EditorGUILayout.LabelField("Reference from any objects");
			
			EditorGUILayout.EndHorizontal();

			current = EditorGUILayout.BeginScrollView (current);



			try {

				foreach (var type in refCompItems) {

					var components = referenceObjectList.FindAll (item => item.referenceComponent.GetType() == type.componentType);

					EditorGUILayout.BeginVertical ("box");

					EditorGUILayout.BeginHorizontal();
					EditorGUI.BeginChangeCheck();
					type.isDisplay = EditorGUILayout.Foldout (type.isDisplay, type.componentType.Name);
					if( EditorGUI.EndChangeCheck() )
						SceneView.RepaintAll();

					EditorGUILayout.EndHorizontal();

					if( type.isDisplay == false ){
						EditorGUILayout.EndVertical();
						continue;
					}

					EditorGUI.indentLevel = 1;

					if( components[0].referenceComponent is MonoBehaviour )
					{
						var monoscript = MonoScript.FromMonoBehaviour((MonoBehaviour)components[0].referenceComponent);
						EditorGUILayout.ObjectField("script", monoscript, typeof(MonoScript), true);
					}


					foreach (var toComp in components) {
						EditorGUILayout.BeginHorizontal ();
						EditorGUILayout.ObjectField(toComp.referenceMemberName,  toComp.referenceComponent.gameObject , typeof(GameObject), true);
						EditorGUILayout.EndHorizontal ();
					}
					EditorGUILayout.EndVertical ();

					EditorGUI.indentLevel = 0;

				}

			} catch {
				referenceObjectList.Clear ();
			}


			EditorGUILayout.EndScrollView ();
		}

	}
}