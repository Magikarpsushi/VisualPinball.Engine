// Visual Pinball Engine
// Copyright (C) 2021 freezy and VPE Team
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using VisualPinball.Engine.Game;
using VisualPinball.Engine.Math;
using VisualPinball.Engine.VPT;
using VisualPinball.Engine.VPT.Surface;
using Object = UnityEngine.Object;

namespace VisualPinball.Unity.Editor
{
	public abstract class ItemInspector : UnityEditor.Editor
	{
		public abstract MonoBehaviour UndoTarget { get; }

		protected TableAuthoring _ta;

		private AdvancedDropdownState _itemPickDropdownState;

		private readonly Dictionary<string, MonoBehaviour> _refItems = new Dictionary<string, MonoBehaviour>();
		private readonly Dictionary<string, MonoBehaviour> _objItems = new Dictionary<string, MonoBehaviour>();

		private string[] _allMaterials = new string[0];
		private string[] _allTextures = new string[0];

		public static event Action<IIdentifiableItemAuthoring, string, string> ItemRenamed;

		#region Unity Events

		protected virtual void OnEnable()
		{
			Undo.undoRedoPerformed += OnUndoRedoPerformed;

// #if UNITY_EDITOR
// 			// for convenience move item behavior to the top of the list
// 			// we're opting to due this here as opposed to at import time since modifying objects
// 			// in this way caused them to not be part of the created object undo stack
// 			if (target != null && target is MonoBehaviour mb) {
// 				var numComp = mb.GetComponents<MonoBehaviour>().Length;
// 				if (mb is IItemColliderAuthoring || mb is IItemMeshAuthoring || mb is IItemMovementAuthoring) {
// 					numComp--;
// 				}
// 				for (var i = 0; i <= numComp; i++) {
// 					UnityEditorInternal.ComponentUtility.MoveComponentUp(mb);
// 				}
// 			}
// #endif

			_ta = (target as MonoBehaviour)?.gameObject.GetComponentInParent<TableAuthoring>();
			PopulateDropDownOptions();
		}

		private void OnUndoRedoPerformed()
		{
			switch (target) {
				case IItemMeshAuthoring meshItem:
					meshItem.IMainAuthoring.RebuildMeshes();
					break;
				case IItemMainRenderableAuthoring mainItem:
					mainItem.RebuildMeshes();
					break;
			}
		}

		protected virtual void OnDisable()
		{
			Undo.undoRedoPerformed -= OnUndoRedoPerformed;
		}

		public override void OnInspectorGUI()
		{
			if (!(target is IItemMainRenderableAuthoring item)) {
				return;
			}

			GUILayout.Space(10);
			if (GUILayout.Button("Force Update Mesh")) {
				item.RebuildMeshes();
			}
		}

		#endregion

		private void PopulateDropDownOptions()
		{
			if (_ta == null) return;

			if (_ta.Data.Materials != null) {
				_allMaterials = new string[_ta.Data.Materials.Length + 1];
				_allMaterials[0] = "- none -";
				for (var i = 0; i < _ta.Data.Materials.Length; i++) {
					_allMaterials[i + 1] = _ta.Data.Materials[i].Name;
				}
				Array.Sort(_allMaterials, 1, _allMaterials.Length - 1);
			}
			// if (_table.Textures != null) {
			// 	_allTextures = new string[_table.Textures.Count + 1];
			// 	_allTextures[0] = "- none -";
			// 	_table.Textures.Select(tex => tex.Name).ToArray().CopyTo(_allTextures, 1);
			// 	Array.Sort(_allTextures, 1, _allTextures.Length - 1);
			// }
		}

		protected void OnPreInspectorGUI()
		{
			if (!(target is IItemMainRenderableAuthoring item)) {
				return;
			}

			EditorGUI.BeginChangeCheck();
			var val = EditorGUILayout.TextField("Name", item.ItemData.GetName());
			if (EditorGUI.EndChangeCheck()) {
				FinishEdit("Name", false);
				item.ItemData.SetName(val);
			}

			EditorGUI.BeginChangeCheck();
			var newLock = EditorGUILayout.Toggle("IsLocked", item.IsLocked);
			if (EditorGUI.EndChangeCheck())
			{
				FinishEdit("IsLocked");
				item.IsLocked = newLock;
				SceneView.RepaintAll();
			}

			if (target is IIdentifiableItemAuthoring identity && target is MonoBehaviour bh) {
				if (identity.Name != bh.gameObject.name) {
					var oldName = identity.Name;
					identity.Name = bh.gameObject.name;
					ItemRenamed?.Invoke(identity, oldName, bh.gameObject.name);
				}
			}
		}

		#region Data Fields

		protected void ItemDataField(string label, ref float field, bool dirtyMesh = true, Action<float, float> onChanged = null)
		{
			EditorGUI.BeginChangeCheck();
			var val = EditorGUILayout.FloatField(label, field);
			if (EditorGUI.EndChangeCheck()) {
				FinishEdit(label, dirtyMesh);
				var fieldBefore = field;
				field = val;
				onChanged?.Invoke(fieldBefore, field);
			}
		}

		public void ItemDataSlider(string label, ref float field, float leftVal, float rightVal, bool dirtyMesh = true, Action<float, float> onChanged = null)
		{
			EditorGUI.BeginChangeCheck();
			var val = EditorGUILayout.Slider(label, field, leftVal, rightVal);
			if (EditorGUI.EndChangeCheck()) {
				FinishEdit(label, dirtyMesh);
				var fieldBefore = field;
				field = val;
				onChanged?.Invoke(fieldBefore, field);
			}
		}

		protected void ItemDataField(string label, ref int field, bool dirtyMesh = true)
		{
			EditorGUI.BeginChangeCheck();
			var val = EditorGUILayout.IntField(label, field);
			if (EditorGUI.EndChangeCheck()) {
				FinishEdit(label, dirtyMesh);
				field = val;
			}
		}

		public void ItemDataSlider(string label, ref int field, int leftVal, int rightVal, bool dirtyMesh = true)
		{
			EditorGUI.BeginChangeCheck();
			var val = EditorGUILayout.IntSlider(label, field, leftVal, rightVal);
			if (EditorGUI.EndChangeCheck()) {
				FinishEdit(label, dirtyMesh);
				field = val;
			}
		}

		protected void ItemDataField(string label, ref string field, bool dirtyMesh = true)
		{
			EditorGUI.BeginChangeCheck();
			var val = EditorGUILayout.TextField(label, field);
			if (EditorGUI.EndChangeCheck()) {
				FinishEdit(label, dirtyMesh);
				field = val;
			}
		}

		protected void ItemDataField(string label, ref bool field, bool dirtyMesh = true, Action<bool, bool> onChanged = null)
		{
			EditorGUI.BeginChangeCheck();
			var val = EditorGUILayout.Toggle(label, field);
			if (EditorGUI.EndChangeCheck()) {
				FinishEdit(label, dirtyMesh);
				var fieldBefore = field;
				field = val;
				onChanged?.Invoke(fieldBefore, field);
			}
		}

		protected void ItemDataField(string label, ref Vertex2D field, bool dirtyMesh = true)
		{
			EditorGUI.BeginChangeCheck();
			var val = EditorGUILayout.Vector2Field(label, field.ToUnityVector2()).ToVertex2D();
			if (EditorGUI.EndChangeCheck()) {
				FinishEdit(label, dirtyMesh);
				field = val;
			}
		}

		protected void ItemDataField(string label, ref Vertex3D field, bool dirtyMesh = true)
		{
			EditorGUI.BeginChangeCheck();
			var val = EditorGUILayout.Vector3Field(label, field.ToUnityVector3()).ToVertex3D();
			if (EditorGUI.EndChangeCheck()) {
				FinishEdit(label, dirtyMesh);
				field = val;
			}
		}

		protected void ItemDataField(string label, ref Engine.Math.Color field, bool dirtyMesh = true)
		{
			EditorGUI.BeginChangeCheck();
			var val = EditorGUILayout.ColorField(label, field.ToUnityColor()).ToEngineColor();
			if (EditorGUI.EndChangeCheck()) {
				FinishEdit(label, dirtyMesh);
				field = val;
			}
		}

		protected void ItemDataField<T>(string label, ref T field, bool dirtyMesh = true, string tooltip = "") where T : ScriptableObject
		{
			EditorGUI.BeginChangeCheck();
			var val = EditorGUILayout.ObjectField(new GUIContent(label, tooltip), field, typeof(T), false) as T;
			if (EditorGUI.EndChangeCheck()) {
				FinishEdit(label, dirtyMesh);
				field = val;
			}
		}

		protected void ItemReferenceField<TItemAuthoring, TItem, TData>(string label, string cacheKey, ref string field, bool dirtyMesh = true)
			where TItemAuthoring : ItemAuthoring<TItem, TData>
			where TData : ItemData where TItem : Item<TData>, IRenderable
		{
			if (!_refItems.ContainsKey(cacheKey) && _ta != null) {
				var currentFieldName = field;
				if (currentFieldName != null && _ta.TableContainer.Has<TItem>(currentFieldName)) {
					_refItems[cacheKey] = _ta.gameObject.GetComponentsInChildren<TItemAuthoring>(true)
						.FirstOrDefault(s => s.name == currentFieldName);
				}
			}

			EditorGUI.BeginChangeCheck();
			_refItems[cacheKey] = (TItemAuthoring)EditorGUILayout.ObjectField(label, _refItems.ContainsKey(cacheKey) ? _refItems[cacheKey] : null, typeof(TItemAuthoring), true);
			if (EditorGUI.EndChangeCheck()) {
				FinishEdit(label, dirtyMesh);
				field = _refItems[cacheKey] != null ? _refItems[cacheKey].name : string.Empty;
			}
		}

		protected void SurfaceField(string label, ref string field, bool dirtyMesh = true)
		{
			ItemReferenceField<SurfaceAuthoring, Surface, SurfaceData>(label, "surface", ref field, dirtyMesh);
		}

		protected void DropDownField<T>(string label, ref T field, string[] optionStrings, T[] optionValues, bool dirtyMesh = true, Action<T, T> onChanged = null) where T : IEquatable<T>
		{
			if (optionStrings == null || optionValues == null || optionStrings.Length != optionValues.Length) {
				return;
			}

			var selectedIndex = 0;
			for (var i = 0; i < optionValues.Length; i++) {
				if (optionValues[i].Equals(field)) {
					selectedIndex = i;
					break;
				}
			}
			EditorGUI.BeginChangeCheck();
			selectedIndex = EditorGUILayout.Popup(label, selectedIndex, optionStrings);
			if (EditorGUI.EndChangeCheck() && selectedIndex >= 0 && selectedIndex < optionValues.Length) {
				FinishEdit(label, dirtyMesh);
				var fieldBefore = field;
				field = optionValues[selectedIndex];
				onChanged?.Invoke(fieldBefore, field);
			}
		}

		protected void TextureFieldLegacy(string label, ref string field, bool dirtyMesh = true)
		{
			if (_ta == null) return;

			// if the field is set, but the tex isn't in our list, maybe it was added after this
			// inspector was instantiated, so re-grab our options from the table data
			if (!string.IsNullOrEmpty(field) && !_allTextures.Contains(field)) {
				PopulateDropDownOptions();
			}

			var selectedIndex = 0;
			for (var i = 0; i < _allTextures.Length; i++) {
				if (string.Equals(_allTextures[i], field, StringComparison.CurrentCultureIgnoreCase)) {
					selectedIndex = i;
					break;
				}
			}
			EditorGUI.BeginChangeCheck();
			selectedIndex = EditorGUILayout.Popup("[VPX] " + label, selectedIndex, _allTextures);
			if (EditorGUI.EndChangeCheck() && selectedIndex >= 0 && selectedIndex < _allTextures.Length) {
				FinishEdit(label, dirtyMesh);
				field = selectedIndex == 0 ? string.Empty : _allTextures[selectedIndex];
			}
		}

		protected void PhysicsMaterialField(string label, ref PhysicsMaterial prevMat)
		{
			EditorGUI.BeginChangeCheck();
			var newMat = (PhysicsMaterial)EditorGUILayout.ObjectField(label, prevMat, typeof(PhysicsMaterial), false);
			if (EditorGUI.EndChangeCheck()) {
				Undo.RecordObject(UndoTarget, "Change physics material of " + UndoTarget.name);
				prevMat = newMat;
			}
		}

		protected void MaterialFieldLegacy(string label, ref string field, bool dirtyMesh = true)
		{
			// if the field is set, but the material isn't in our list, maybe it was added after this
			// inspector was instantiated, so re-grab our mat options from the table data
			if (!string.IsNullOrEmpty(field) && !_allMaterials.Contains(field)) {
				PopulateDropDownOptions();
			}

			DropDownField("[VPX] " + label, ref field, _allMaterials, _allMaterials, dirtyMesh);
			if (_allMaterials.Length > 0 && field == _allMaterials[0]) {
				field = string.Empty; // don't store the none value string in our data
			}
		}

		protected void ObjectReferenceField<T>(string label, string pickerLabel, string noneLabel, string cacheKey, string field, Action<string> onSelected)
			where T: class, IIdentifiableItemAuthoring
		{
			var pos = EditorGUILayout.GetControlRect(true, 18f);
			pos = EditorGUI.PrefixLabel(pos, new GUIContent(label));

			MonoBehaviour obj = null;
			if (!_objItems.ContainsKey(cacheKey)) {
				if (!string.IsNullOrEmpty(field)) {
					obj = _ta.gameObject.GetComponentsInChildren<T>(true)
						.FirstOrDefault(s => s.Name == field) as MonoBehaviour;
					_objItems[cacheKey] = obj;
				}
			} else {
				obj = _objItems[cacheKey];
			}

			var content = obj == null
				? new GUIContent(noneLabel)
				: new GUIContent(obj.name, Icons.ByComponent(obj, IconSize.Small, IconColor.Orange));

			var id = GUIUtility.GetControlID(FocusType.Keyboard, pos);
			var objectFieldButton = GUI.skin.GetStyle("ObjectFieldButton");
			var suffixButtonPos = new Rect(pos.xMax - 19f, pos.y + 1, 19f, pos.height - 2);

			EditorGUIUtility.SetIconSize(new Vector2(12f, 12f));
			if (Event.current.type == EventType.MouseDown && pos.Contains(Event.current.mousePosition)) {

				if (obj != null && !suffixButtonPos.Contains(Event.current.mousePosition)) {
					EditorGUIUtility.PingObject(obj.gameObject);

				} else {
					if (_itemPickDropdownState == null) {
						_itemPickDropdownState = new AdvancedDropdownState();
					}

					var dropdown = new ItemSearchableDropdown<T>(
						_itemPickDropdownState,
						_ta,
						pickerLabel,
						item => {
							switch (item) {
								case null:
									_objItems[cacheKey] = null;
									onSelected(string.Empty);
									break;
								case MonoBehaviour mb:
									_objItems[cacheKey] = mb;
									onSelected(item.Name);
									break;
							}
						}
					);
					dropdown.Show(pos);
				}

			}
			if (Event.current.type == EventType.Repaint) {
				EditorStyles.objectField.Draw(pos, content, id, DragAndDrop.activeControlID == id, pos.Contains(Event.current.mousePosition));
				objectFieldButton.Draw(suffixButtonPos, GUIContent.none, id, DragAndDrop.activeControlID == id, suffixButtonPos.Contains(Event.current.mousePosition));
			}
		}

		#endregion

		protected virtual void FinishEdit(string label, bool dirtyMesh = true)
		{
			var undoLabel = $"Edit {label} of {target?.name}";
			if (dirtyMesh) {
				// set dirty flag true before recording object state for the undo so meshes will rebuild after the undo as well
				switch (target) {

					case IItemMeshAuthoring meshItem:
						Undo.RecordObjects(new Object[] {UndoTarget, UndoTarget.transform}, undoLabel);
						meshItem.IMainAuthoring.RebuildMeshes();
						break;

					case IItemColliderAuthoring colliderItem:
						Undo.RecordObject(UndoTarget, undoLabel);
						break;

					case IItemMainRenderableAuthoring mainItem:
						Undo.RecordObjects(new Object[] {UndoTarget, UndoTarget.transform}, undoLabel);
						mainItem.RebuildMeshes();
						break;
				}
			}

			if (target is IItemMainRenderableAuthoring item) {
				item.ItemDataChanged();
			}
		}
	}
}
