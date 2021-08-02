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

using UnityEditor;
using UnityEngine;
using VisualPinball.Engine.VPT;
using VisualPinball.Engine.VPT.Bumper;

namespace VisualPinball.Unity
{
	public static class BumperExtensions
	{
		public static IConvertedItem InstantiateGameObject(this Bumper bumper, IItem item, IMaterialProvider materialProvider)
		{
			var prefab = RenderPipeline.Current.PrefabProvider.CreateBumper();
			var obj = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
			obj.name = item.Name;
			return new ConvertedItem<Bumper, BumperData, BumperAuthoring>(obj, true);
		}
	}
}
