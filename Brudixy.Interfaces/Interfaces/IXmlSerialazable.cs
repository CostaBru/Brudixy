using System.Xml.Linq;

namespace Brudixy.Interfaces
{
    public interface IXmlSerializable
    {
        XElement ToXml();
        void FromXml(XElement element);
    }
}