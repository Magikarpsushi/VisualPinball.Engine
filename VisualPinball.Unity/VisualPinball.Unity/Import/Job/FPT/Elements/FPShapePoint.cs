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
using VisualPinball.Engine.Math;
namespace VisualPinball.Unity.FP
{
	[System.Serializable]
    public class FPShapePoint
    {
        public Vertex3D position;
        public bool smooth;
        public bool automatic_texture_coordinate;
        public float texture_coordinate;

        public int left_guide = 0;
        public int left_upper_guide = 0;
        public int right_guide = 0;
        public int right_upper_guide = 0;
        public int top_wire = 0;
        public int ring_type = 0;

        public int single_leaf = 0;
        public int slingshot = 0;
        public int event_id = 0;

        public FPShapePoint()
        {
        }

        public FPShapePoint(Vertex3D position_, bool smooth_, bool auto_texture_coord, float texture_coord)
        {
            position = position_;
            smooth = smooth_;
            automatic_texture_coordinate = auto_texture_coord;
            texture_coordinate = texture_coord;
        }

		public DragPointData ToVpx()
		{
			var dp = new DragPointData(0F, 0F);

			dp.Center = FptUtils.mm2VpUnits(position);
			dp.IsSmooth = smooth;
			dp.IsSlingshot = slingshot > 0;

			dp.HasAutoTexture = automatic_texture_coordinate; //?
			dp.TextureCoord = texture_coordinate;

			dp.IsLocked = false;
			
			return dp;
		}
    }
}

