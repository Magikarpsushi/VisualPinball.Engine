﻿// Visual Pinball Engine
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
using NLog;
using UnityEditor;
using UnityEngine;
using VisualPinball.Engine.VPT;
using VisualPinball.Engine.VPT.Flipper;
using VisualPinball.Engine.VPT.Surface;
using Logger = NLog.Logger;

namespace VisualPinball.Unity
{
	public static class FlipperExtensions
	{
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		public static IConvertedItem InstantiateGameObject(this Flipper flipper, IItem item, IMaterialProvider materialProvider)
		{
			var prefab = UnityEngine.Resources.Load<GameObject>("Prefabs/Flipper");
			var obj = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
			obj!.name = item.Name;

			return new ConvertedItem<Flipper, FlipperData, FlipperAuthoring>(obj, true);

			// var obj = new GameObject(item.Name);
			// var convertedItem = new ConvertedItem<Flipper, FlipperData, FlipperAuthoring>(obj, flipper);
			// switch (flipper.SubComponent) {
			// 	case ItemSubComponent.None:
			// 		convertedItem.SetColliderAuthoring<FlipperColliderAuthoring>(materialProvider);
			// 		convertedItem.AddMeshAuthoring<FlipperBaseMeshAuthoring>(FlipperMeshGenerator.Base);
			// 		convertedItem.AddMeshAuthoring<FlipperRubberMeshAuthoring>(FlipperMeshGenerator.Rubber);
			// 		break;
			//
			// 	case ItemSubComponent.Collider: {
			// 		Logger.Warn("Cannot parent a flipper collider to a different object than a flipper!");
			// 		break;
			// 	}
			//
			// 	case ItemSubComponent.Mesh: {
			// 		Logger.Warn("Cannot parent a flipper mesh to a different object than a flipper!");
			// 		break;
			// 	}
			//
			// 	default:
			// 		throw new ArgumentOutOfRangeException();
			// }
			//
			// return convertedItem.AddConvertToEntity();
		}
	}
}