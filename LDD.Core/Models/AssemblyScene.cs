using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LDD.Core.Models
{
    public class AssemblyScene
    {
        public int RefID { get; set; }
        public List<SceneModel> Models { get; set; }
    }
}
