using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LDD.Core.Files.MeshStructures
{
    public struct MESH_BONE_WEIGHT
    {
        public int BoneID;
        public float Weight;

        public MESH_BONE_WEIGHT(int boneID, float weight)
        {
            BoneID = boneID;
            Weight = weight;
        }
    }
}
