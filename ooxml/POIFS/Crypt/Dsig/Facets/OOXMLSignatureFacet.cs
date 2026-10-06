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

namespace NPOI.POIFS.Crypt.Dsig.Facets
{
    using NPOI.OpenXml4Net.OPC;
    using NPOI.POIFS.Crypt.Dsig.Services;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Security.Cryptography.Xml;
    using System.Text.RegularExpressions;
    using System.Xml;


    /**
     * Office OpenXML Signature Facet implementation.
     * 
     * @author fcorneli
     * @see <a href="http://msdn.microsoft.com/en-us/library/cc313071.aspx">[MS-OFFCRYPTO]: Office Document Cryptography Structure</a>
     */
    public class OOXMLSignatureFacet : SignatureFacet
    {


        public override void preSign(
            XmlDocument document
            , List<Reference> references
            , List<XmlNode> objects)
        {
            AddManifestObject(document, references, objects);
            AddSignatureInfo(document, references, objects);
        }

        protected void AddManifestObject(
            XmlDocument document
            , List<Reference> references
            , List<XmlNode> objects)
        {

            List<Reference> manifestReferences = new List<Reference>();
            AddManifestReferences(manifestReferences);
            XmlElement manifest = document.CreateElement("Manifest", XML_DIGSIG_NS);
            foreach(Reference manifestReference in manifestReferences)
            {
                manifest.AppendChild(document.ImportNode(manifestReference.GetXml(), true));
            }

            String objectId = "idPackageObject"; // really has to be this value.
            List<XmlNode> objectContent = new List<XmlNode>();
            objectContent.Add(manifest);

            AddSignatureTime(document, objectContent);

            objects.Add(NewObject(document, objectId, objectContent));

            Reference reference = newReference("#" + objectId, null, XML_DIGSIG_NS + "Object", null, null);
            references.Add(reference);
        }

        protected void AddManifestReferences(List<Reference> manifestReferences)
        {

            OPCPackage ooxml = signatureConfig.GetOpcPackage();
            List<PackagePart> relsEntryNames = ooxml.GetPartsByContentType(ContentTypes.RELATIONSHIPS_PART);

            OOXMLURIDereferencer dereferencer = new OOXMLURIDereferencer();
            dereferencer.SetSignatureConfig(signatureConfig);
            String digestMethodUri = signatureConfig.GetDigestMethodUri();

            HashSet<String> digestedPartNames = new HashSet<String>();
            foreach(PackagePart pp in relsEntryNames)
            {
                String relsPartName = pp.PartName.Name;
                String baseUri = Regex.Replace(relsPartName, "(.*)/_rels/.*", "$1/");

                XmlDocument relsDoc;
                using(Stream relsStream = pp.GetInputStream())
                {
                    relsDoc = DsigUtil.LoadXml(relsStream);
                }

                RelationshipTransformService.RelationshipTransformParameterSpec parameterSpec =
                    new RelationshipTransformService.RelationshipTransformParameterSpec();
                foreach(XmlNode node in relsDoc.DocumentElement.ChildNodes)
                {
                    if(!(node is XmlElement relationship)
                        || relationship.LocalName != "Relationship"
                        || relationship.NamespaceURI != PackageNamespaces.RELATIONSHIPS)
                    {
                        continue;
                    }
                    String relationshipType = relationship.GetAttribute("Type");

                    /*
                     * ECMA-376 Part 2 - 3rd edition
                     * 13.2.4.16 Manifest Element
                     * "The producer shall not create a Manifest element that references any data outside of the package."
                     */
                    if("External".Equals(relationship.GetAttribute("TargetMode"), StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    if(!IsSignedRelationship(relationshipType))
                        continue;

                    String target = relationship.GetAttribute("Target");
                    String partName = (target.Length > 0 && target[0] == '/')
                        ? target
                        : new Uri(new Uri("http://package" + baseUri), target).AbsolutePath;

                    PackagePart pp2 = dereferencer.FindPart(partName);
                    if(pp2 == null)
                    {
                        // dangling relationship - neither the relationship nor the part is signed
                        continue;
                    }

                    parameterSpec.AddRelationshipReference(relationship.GetAttribute("Id"));
                    String contentType = pp2.ContentType;

                    if(relationshipType.EndsWith("customXml")
                        && !(contentType.Equals("inkml+xml") || contentType.Equals("text/xml")))
                    {
                        continue;
                    }

                    String canonicalPartName = pp2.PartName.Name;
                    if(digestedPartNames.Add(canonicalPartName))
                    {
                        // We only digest a part once.
                        String uri = canonicalPartName + "?ContentType=" + contentType;
                        byte[] digestValue;
                        using(Stream partStream = pp2.GetInputStream())
                        {
                            digestValue = DsigUtil.DigestReference(partStream, null, digestMethodUri);
                        }
                        manifestReferences.Add(newReference(uri, null, null, null, digestValue));
                    }
                }

                if(parameterSpec.HasSourceIds())
                {
                    List<Transform> transforms = new List<Transform>();
                    transforms.Add(new RelationshipTransformService(parameterSpec));
                    transforms.Add(newTransform(DsigUtil.C14N));
                    String uri = relsPartName + "?ContentType=" + ContentTypes.RELATIONSHIPS_PART;
                    byte[] digestValue;
                    using(Stream relsStream = pp.GetInputStream())
                    {
                        digestValue = DsigUtil.DigestReference(relsStream, transforms, digestMethodUri);
                    }
                    manifestReferences.Add(newReference(uri, transforms, null, null, digestValue));
                }
            }
        }


        protected void AddSignatureTime(XmlDocument document, List<XmlNode> objectContent)
        {
            /*
             * SignatureTime
             */
            String nowStr = signatureConfig.GetExecutionTime().ToUniversalTime()
                .ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture);

            XmlElement sigTime = document.CreateElement("mdssi", "SignatureTime", OO_DIGSIG_NS);
            XmlElement format = document.CreateElement("mdssi", "Format", OO_DIGSIG_NS);
            format.InnerText = "YYYY-MM-DDThh:mm:ssTZD";
            sigTime.AppendChild(format);
            XmlElement value = document.CreateElement("mdssi", "Value", OO_DIGSIG_NS);
            value.InnerText = nowStr;
            sigTime.AppendChild(value);

            objectContent.Add(NewSignatureProperties(document, sigTime, "idSignatureTime"));
        }

        protected void AddSignatureInfo(XmlDocument document,
            List<Reference> references,
            List<XmlNode> objects)
        {
            XmlElement sigV1 = document.CreateElement("SignatureInfoV1", MS_DIGSIG_NS);
            XmlElement manifestHashAlgorithm = document.CreateElement("ManifestHashAlgorithm", MS_DIGSIG_NS);
            manifestHashAlgorithm.InnerText = signatureConfig.GetDigestMethodUri();
            sigV1.AppendChild(manifestHashAlgorithm);

            List<XmlNode> objectContent = new List<XmlNode>();
            objectContent.Add(NewSignatureProperties(document, sigV1, "idOfficeV1Details"));

            String objectId = "idOfficeObject";
            objects.Add(NewObject(document, objectId, objectContent));

            Reference reference = newReference("#" + objectId, null, XML_DIGSIG_NS + "Object", null, null);
            references.Add(reference);
        }

        private XmlElement NewSignatureProperties(XmlDocument document, XmlNode content, String propertyId)
        {
            XmlElement signatureProperty = document.CreateElement("SignatureProperty", XML_DIGSIG_NS);
            signatureProperty.SetAttribute("Id", propertyId);
            signatureProperty.SetAttribute("Target", "#" + signatureConfig.GetPackageSignatureId());
            signatureProperty.AppendChild(content);
            XmlElement signatureProperties = document.CreateElement("SignatureProperties", XML_DIGSIG_NS);
            signatureProperties.AppendChild(signatureProperty);
            return signatureProperties;
        }

        private static XmlElement NewObject(XmlDocument document, String objectId, List<XmlNode> content)
        {
            XmlElement xo = document.CreateElement("Object", XML_DIGSIG_NS);
            if(objectId != null)
            {
                xo.SetAttribute("Id", objectId);
            }
            foreach(XmlNode n in content)
            {
                xo.AppendChild(n);
            }
            return xo;
        }

        protected static String GetRelationshipReferenceURI(String zipEntryName)
        {
            return "/"
                + zipEntryName
                + "?ContentType=application/vnd.openxmlformats-package.relationships+xml";
        }

        protected static String GetResourceReferenceURI(String resourceName, String contentType)
        {
            return "/" + resourceName + "?ContentType=" + contentType;
        }

        protected static bool IsSignedRelationship(String relationshipType)
        {
            //LOG.Log(POILogger.DEBUG, "relationship type: " + relationshipType);
            foreach(String signedTypeExtension in signed)
            {
                if(relationshipType.EndsWith(signedTypeExtension))
                {
                    return true;
                }
            }
            if(relationshipType.EndsWith("customXml"))
            {
                //LOG.Log(POILogger.DEBUG, "customXml relationship type");
                return true;
            }
            return false;
        }

        public static String[] contentTypes = {
        /*
         * Word
         */
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.fontTable+xml",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.settings+xml",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.styles+xml",
        "application/vnd.openxmlformats-officedocument.theme+xml",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.websettings+xml",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.numbering+xml",

        /*
         * Word 2010
         */
        "application/vnd.ms-word.stylesWithEffects+xml",

        /*
         * Excel
         */
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sharedStrings+xml",
        "application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml",
        "application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml",
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml",

        /*
         * Powerpoint
         */
        "application/vnd.openxmlformats-officedocument.presentationml.presentation.main+xml",
        "application/vnd.openxmlformats-officedocument.presentationml.slideLayout+xml",
        "application/vnd.openxmlformats-officedocument.presentationml.slideMaster+xml",
        "application/vnd.openxmlformats-officedocument.presentationml.slide+xml",
        "application/vnd.openxmlformats-officedocument.presentationml.tableStyles+xml",

        /*
         * Powerpoint 2010
         */
        "application/vnd.openxmlformats-officedocument.presentationml.viewProps+xml",
        "application/vnd.openxmlformats-officedocument.presentationml.presProps+xml"
    };

        /**
         * Office 2010 list of signed types (extensions).
         */
        public static String[] signed = {
        "powerPivotData", //
        "activeXControlBinary", //
        "attachedToolbars", //
        "connectorXml", //
        "downRev", //
        "functionPrototypes", //
        "graphicFrameDoc", //
        "groupShapeXml", //
        "ink", //
        "keyMapCustomizations", //
        "legacyDiagramText", //
        "legacyDocTextInfo", //
        "officeDocument", //
        "pictureXml", //
        "shapeXml", //
        "smartTags", //
        "ui/altText", //
        "ui/buttonSize", //
        "ui/controlID", //
        "ui/description", //
        "ui/enabled", //
        "ui/extensibility", //
        "ui/helperText", //
        "ui/imageID", //
        "ui/imageMso", //
        "ui/keyTip", //
        "ui/label", //
        "ui/lcid", //
        "ui/loud", //
        "ui/pressed", //
        "ui/progID", //
        "ui/ribbonID", //
        "ui/showImage", //
        "ui/showLabel", //
        "ui/supertip", //
        "ui/target", //
        "ui/text", //
        "ui/title", //
        "ui/tooltip", //
        "ui/userCustomization", //
        "ui/visible", //
        "userXmlData", //
        "vbaProject", //
        "wordVbaData", //
        "wsSortMap", //
        "xlBinaryIndex", //
        "xlExternalLinkPath/xlAlternateStartup", //
        "xlExternalLinkPath/xlLibrary", //
        "xlExternalLinkPath/xlPathMissing", //
        "xlExternalLinkPath/xlStartup", //
        "xlIntlMacrosheet", //
        "xlMacrosheet", //
        "customData", //
        "diagramDrawing", //
        "hdphoto", //
        "inkXml", //
        "media", //
        "slicer", //
        "slicerCache", //
        "stylesWithEffects", //
        "ui/extensibility", //
        "chartColorStyle", //
        "chartLayout", //
        "chartStyle", //
        "dictionary", //
        "timeline", //
        "timelineCache", //
        "aFChunk", //
        "attachedTemplate", //
        "audio", //
        "calcChain", //
        "chart", //
        "chartsheet", //
        "chartUserShapes", //
        "commentAuthors", //
        "comments", //
        "connections", //
        "control", //
        "customProperty", //
        "customXml", //
        "diagramColors", //
        "diagramData", //
        "diagramLayout", //
        "diagramQuickStyle", //
        "dialogsheet", //
        "drawing", //
        "endnotes", //
        "externalLink", //
        "externalLinkPath", //
        "font", //
        "fontTable", //
        "footer", //
        "footnotes", //
        "glossaryDocument", //
        "handoutMaster", //
        "header", //
        "hyperlink", //
        "image", //
        "mailMergeHeaderSource", //
        "mailMergeRecipientData", //
        "mailMergeSource", //
        "notesMaster", //
        "notesSlide", //
        "numbering", //
        "officeDocument", //
        "oleObject", //
        "package", //
        "pivotCacheDefinition", //
        "pivotCacheRecords", //
        "pivotTable", //
        "presProps", //
        "printerSettings", //
        "queryTable", //
        "recipientData", //
        "settings", //
        "sharedStrings", //
        "sheetMetadata", //
        "slide", //
        "slideLayout", //
        "slideMaster", //
        "slideUpdateInfo", //
        "slideUpdateUrl", //
        "styles", //
        "table", //
        "tableSingleCells", //
        "tableStyles", //
        "tags", //
        "theme", //
        "themeOverride", //
        "transform", //
        "video", //
        "viewProps", //
        "volatileDependencies", //
        "webSettings", //
        "worksheet", //
        "xmlMaps", //
        "ctrlProp", //
        "customData", //
        "diagram", //
        "diagramColorsHeader", //
        "diagramLayoutHeader", //
        "diagramQuickStyleHeader", //
        "documentParts", //
        "slicer", //
        "slicerCache", //
        "vmlDrawing" //
    };
    }
}