using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;


namespace Test.Console.CB.Dynamic.CompilerServices
{
    public class MyXmlReader
    {
        #region Fields

        //private XmlReader _reader;
        private readonly XElement _nodes;
        #endregion


        #region  Constructors & Destructor
        public MyXmlReader(string xmlFile)
        {
            var document = XDocument.Load(xmlFile);
            _nodes = document.Element("Nodes");
        }
        #endregion


        #region Methods
        public string ReadAttribute(string node, string attrName)
        {
            var element = _nodes.Element(node);
            return element?.Attribute(attrName).Value;
        }

        public IEnumerable<string> ReadAttributes(string node, IEnumerable<string> attrNames)
        {
            if (attrNames == null) throw new ArgumentNullException(nameof(attrNames));
            return attrNames.Select(attrName => ReadAttribute(node, attrName));
        }

        public IEnumerable<string> ReadAttributes(IEnumerable<string> nodes, string attrName)
        {
            if (nodes == null) throw new ArgumentNullException(nameof(nodes));
            return nodes.Select(node => ReadAttribute(node, attrName));
        }
        #endregion
    }
}