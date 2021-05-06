using System.Xml.Linq;

namespace LDD.Core.Primitives.Collisions
{
    public class CollisionSphere : Collision
    {
        private double _Radius;
        
        public double Radius
        {
            get => _Radius;
            set => SetPropertyValue(ref _Radius, value);
        }

        public CollisionSphere()
        {
        }

        public CollisionSphere(double radius, Transform transform)
        {
            Radius = radius;
            Transform = transform;
        }

        public override Common.Simple3D.Vector3d GetSize()
        {
            return new Common.Simple3D.Vector3d(Radius);
        }

        public override XElement SerializeToXml()
        {
            var elem = new XElement("Sphere");
            elem.AddNumberAttribute("radius", Radius);
            elem.Add(Transform.ToXmlAttributes());
            return elem;
        }

        public override void LoadFromXml(XElement element)
        {
            Transform = Transform.FromElementAttributes(element);
            Radius = element.ReadAttribute("radius", 1d);
        }
    }
}
