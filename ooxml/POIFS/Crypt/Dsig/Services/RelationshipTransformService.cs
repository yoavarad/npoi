/* ====================================================================
   Licensed to the Apache Software Foundation (ASF) under one or more
   contributor license agreements.  See the NOTICE file distributed with
   this work for Additional information regarding copyright ownership.
   The ASF licenses this file to You under the Apache License, Version 2.0
   (the "License"); you may not use this file except in compliance with
   the License.  You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
==================================================================== */

/* ====================================================================
   This product Contains an ASLv2 licensed version of the OOXML signer
   package from the eID Applet project
   http://code.google.com/p/eid-applet/source/browse/tRunk/README.txt  
   Copyright (C) 2008-2014 FedICT.
   ================================================================= */


namespace NPOI.POIFS.Crypt.Dsig.Services
{
    using NPOI.OpenXml4Net.OPC;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Security.Cryptography.Xml;
    using System.Xml;

    /**
     * Implementation of the OPC RelationshipTransform (ECMA-376 Part 2, 13.2.4.24).
     *
     * The transform keeps the relationships selected by SourceId / SourceType,
     * removes all other content, defaults TargetMode to Internal and sorts the
     * relationships by Id. The result is a node set, which is canonicalized by the
     * following C14N transform.
     */
    public class RelationshipTransformService : Transform
    {
        public const String TRANSFORM_URI = "http://schemas.openxmlformats.org/package/2006/RelationshipTransform";

        private static readonly String[] KEPT_ATTRIBUTES = { "Id", "Target", "TargetMode", "Type" };

        private readonly List<String> sourceIds = new List<String>();
        private readonly List<String> sourceTypes = new List<String>();
        private XmlDocument output;

        public RelationshipTransformService()
        {
            Algorithm = TRANSFORM_URI;
        }

        /**
         * Relationship Transform parameter specification class.
         */
        public class RelationshipTransformParameterSpec
        {
            internal readonly List<String> sourceIds = new List<String>();

            public void AddRelationshipReference(String relationshipId)
            {
                sourceIds.Add(relationshipId);
            }

            public bool HasSourceIds()
            {
                return sourceIds.Count > 0;
            }
        }

        public RelationshipTransformService(RelationshipTransformParameterSpec spec) : this()
        {
            if(spec != null)
            {
                sourceIds.AddRange(spec.sourceIds);
            }
        }

        public List<String> SourceIds
        {
            get { return sourceIds; }
        }

        public List<String> SourceTypes
        {
            get { return sourceTypes; }
        }

        public override Type[] InputTypes
        {
            get { return new Type[] { typeof(Stream), typeof(XmlDocument) }; }
        }

        public override Type[] OutputTypes
        {
            get { return new Type[] { typeof(XmlDocument) }; }
        }

        public override void LoadInnerXml(XmlNodeList nodeList)
        {
            sourceIds.Clear();
            sourceTypes.Clear();
            if(nodeList == null)
            {
                return;
            }
            foreach(XmlNode node in nodeList)
            {
                if(!(node is XmlElement el) || el.NamespaceURI != PackageNamespaces.DIGITAL_SIGNATURE)
                {
                    continue;
                }
                if(el.LocalName == "RelationshipReference")
                {
                    sourceIds.Add(el.GetAttribute("SourceId"));
                }
                else if(el.LocalName == "RelationshipsGroupReference")
                {
                    sourceTypes.Add(el.GetAttribute("SourceType"));
                }
            }
        }

        protected override XmlNodeList GetInnerXml()
        {
            XmlDocument doc = new XmlDocument();
            XmlElement holder = doc.CreateElement("holder");
            foreach(String id in sourceIds)
            {
                XmlElement el = doc.CreateElement("mdssi", "RelationshipReference", PackageNamespaces.DIGITAL_SIGNATURE);
                el.SetAttribute("SourceId", id);
                holder.AppendChild(el);
            }
            foreach(String type in sourceTypes)
            {
                XmlElement el = doc.CreateElement("mdssi", "RelationshipsGroupReference", PackageNamespaces.DIGITAL_SIGNATURE);
                el.SetAttribute("SourceType", type);
                holder.AppendChild(el);
            }
            return holder.ChildNodes;
        }

        public override void LoadInput(object obj)
        {
            XmlDocument src;
            if(obj is XmlDocument doc)
            {
                src = doc;
            }
            else if(obj is Stream stream)
            {
                src = DsigUtil.LoadXml(stream);
            }
            else
            {
                throw new ArgumentException("unsupported input type: " + obj?.GetType());
            }
            output = Transform(src, sourceIds, sourceTypes);
        }

        public override object GetOutput()
        {
            return output;
        }

        public override object GetOutput(Type type)
        {
            if(type != typeof(XmlDocument) && !type.IsSubclassOf(typeof(XmlDocument)))
            {
                throw new ArgumentException("unsupported output type: " + type);
            }
            return output;
        }

        /**
         * Applies the relationship transform to a relationships document.
         */
        public static XmlDocument Transform(XmlDocument src, ICollection<String> sourceIds, ICollection<String> sourceTypes)
        {
            SortedDictionary<String, XmlElement> selected = new SortedDictionary<String, XmlElement>(StringComparer.Ordinal);
            XmlElement srcRoot = src.DocumentElement;
            if(srcRoot != null)
            {
                foreach(XmlNode node in srcRoot.ChildNodes)
                {
                    if(!(node is XmlElement el) || el.LocalName != "Relationship" || el.NamespaceURI != PackageNamespaces.RELATIONSHIPS)
                    {
                        continue;
                    }
                    String id = el.GetAttribute("Id");
                    if(sourceIds.Contains(id) || (sourceTypes != null && sourceTypes.Contains(el.GetAttribute("Type"))))
                    {
                        selected[id] = el;
                    }
                }
            }

            XmlDocument result = new XmlDocument { PreserveWhitespace = true, XmlResolver = null };
            XmlElement root = result.CreateElement("Relationships", PackageNamespaces.RELATIONSHIPS);
            root.SetAttribute("xmlns", PackageNamespaces.RELATIONSHIPS);
            result.AppendChild(root);
            foreach(XmlElement el in selected.Values)
            {
                XmlElement rel = result.CreateElement("Relationship", PackageNamespaces.RELATIONSHIPS);
                foreach(String name in KEPT_ATTRIBUTES)
                {
                    if(el.HasAttribute(name))
                    {
                        rel.SetAttribute(name, el.GetAttribute(name));
                    }
                }
                if(!rel.HasAttribute("TargetMode"))
                {
                    rel.SetAttribute("TargetMode", "Internal");
                }
                root.AppendChild(rel);
            }
            return result;
        }
    }
}