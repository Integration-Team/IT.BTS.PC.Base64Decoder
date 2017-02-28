
namespace IT.BTS.PC.Base64Decoder
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Reflection;
    using System.Text;
    using System.Xml.XPath;
    using Microsoft.BizTalk.Component.Interop;
    using Microsoft.BizTalk.Message.Interop;
    using Microsoft.BizTalk.Streaming;
    using System.Security.Cryptography;
    using Microsoft.BizTalk.ExplorerOM;
    using Microsoft.Win32;

    [ComponentCategory(CategoryTypes.CATID_PipelineComponent)]
    [System.Runtime.InteropServices.Guid("624E145A-788C-49EC-AF6B-8DECBD43ED05")]
    [ComponentCategory(CategoryTypes.CATID_Any)]
    public class Base64Decoder : Microsoft.BizTalk.Component.Interop.IComponent, IBaseComponent, Microsoft.BizTalk.Component.Interop.IPersistPropertyBag, IComponentUI, IDisposable
    {
        private const string RegKeyBtsAdministration = @"SOFTWARE\Microsoft\BizTalk Server\3.0\Administration";
        private const string MgmtDbName = "MgmtDBName";
        private const string MgmtDbServer = "MgmtDBServer";
        private readonly System.Resources.ResourceManager _resourceManager = new System.Resources.ResourceManager("IT.BTS.PC.Base64Decoder.Base64Decoder", Assembly.GetExecutingAssembly());

        private string _xpath;

        public string XPath
        {
            get
            {
                return _xpath;
            }

            set
            {
                _xpath = value;
            }
        }

        #region IBaseComponent members
        /// <summary>
        /// Gets the name of the component
        /// </summary>
        [Browsable(false)]
        public string Name
        {
            get
            {
                return _resourceManager.GetString("COMPONENTNAME", System.Globalization.CultureInfo.InvariantCulture);
            }
        }

        /// <summary>
        /// Gets the version of the component
        /// </summary>
        [Browsable(false)]
        public string Version
        {
            get
            {
                return _resourceManager.GetString("COMPONENTVERSION", System.Globalization.CultureInfo.InvariantCulture);
            }
        }

        /// <summary>
        /// Gets the description of the component
        /// </summary>
        [Browsable(false)]
        public string Description
        {
            get
            {
                return _resourceManager.GetString("COMPONENTDESCRIPTION", System.Globalization.CultureInfo.InvariantCulture);
            }
        }

        /// <summary>
        /// Gets the component icon to use in BizTalk Editor
        /// </summary>
        [Browsable(false)]
        public IntPtr Icon
        {
            get
            {
                // ReSharper disable once PossibleNullReferenceException
                return ((System.Drawing.Bitmap)_resourceManager.GetObject("COMPONENTICON", System.Globalization.CultureInfo.InvariantCulture)).GetHicon();
            }
        }

        #endregion

        #region Public members
        /// <summary>
        /// Gets class ID of component for usage from unmanaged code.
        /// </summary>
        /// <param name="classid">
        /// Class ID of the component
        /// </param>
        public void GetClassID(out Guid classid)
        {
            classid = new Guid("74D070D5-00F5-49C2-9A61-373AF9D611D5");
        }

        /// <summary>
        /// not implemented
        /// </summary>
        public void InitNew()
        {
        }

        /// <summary>
        /// Loads configuration properties for the component
        /// </summary>
        /// <param name="pb">Configuration property bag</param>
        /// <param name="errlog">Error status</param>
        public virtual void Load(Microsoft.BizTalk.Component.Interop.IPropertyBag pb, int errlog)
        {
            var val = ReadPropertyBag(pb, "XPath");
            if (val != null)
            {
                XPath = (string)val;
            }
        }

        /// <summary>
        /// Saves the current component configuration into the property bag
        /// </summary>
        /// <param name="pb">Configuration property bag</param>
        /// <param name="clearDirty">The parameter is not used.</param>
        /// <param name="saveAllProperties">The parameter is not used.</param>
        public virtual void Save(Microsoft.BizTalk.Component.Interop.IPropertyBag pb, bool clearDirty, bool saveAllProperties)
        {
            WritePropertyBag(pb, "XPath", XPath);
        }

        /// <summary>
        /// The Validate method is called by the BizTalk Editor during the build 
        /// of a BizTalk project.
        /// </summary>
        /// <param name="obj">An Object containing the configuration properties.</param>
        /// <returns>The IEnumerator enables the caller to enumerate through a collection of strings containing error messages. These error messages appear as compiler error messages. To report successful property validation, the method should return an empty enumerator.</returns>
        public System.Collections.IEnumerator Validate(object obj)
        {
            // example implementation:
            // ArrayList errorList = new ArrayList();
            // errorList.Add("This is a compiler error");
            // return errorList.GetEnumerator();
            return null;
        }

        /// <summary>
        /// Implements IComponent.Execute method.
        /// </summary>
        /// <param name="pc">Pipeline context</param>
        /// <param name="inmsg">Input message</param>
        /// <returns>Original input message</returns>
        /// <remarks>
        /// IComponent.Execute method is used to initiate
        /// the processing of the message in this pipeline component.
        /// </remarks>
        public IBaseMessage Execute(IPipelineContext pc, IBaseMessage inmsg)
        {
            try
            {
                var bodyPart = inmsg.BodyPart;
                if (bodyPart != null)
                {
                    var dataStream = bodyPart.GetOriginalDataStream();
                    if (!dataStream.CanWrite)
                    {
                        dataStream = new VirtualStream(bodyPart.GetOriginalDataStream());
                    }
                    var xDoc = new XPathDocument(dataStream);
                    var xNav = xDoc.CreateNavigator();
                    xNav.MoveToRoot();
                    var xNodeIt = xNav.Select(_xpath);

                    while (xNodeIt.MoveNext())
                    {
                        var byte64 = Encoding.ASCII.GetBytes(xNodeIt.Current.Value);
                        var strmBase64 = new MemoryStream(byte64);
                        var strm = new CryptoStream(strmBase64, new FromBase64Transform(), CryptoStreamMode.Read);
                        bodyPart.Data = new FakeSeekableStream(strm);
                        pc.ResourceTracker.AddResource(strm);
                        pc.ResourceTracker.AddResource(strmBase64);
                    }
                    string messageType = RecognizeMessageType(pc, inmsg);
                    if (!String.IsNullOrEmpty(messageType))
                    {
                        inmsg.Context.Promote("MessageType"
         , "http://schemas.microsoft.com/BizTalk/2003/system-properties"
         , messageType);

                        string schemaStrongName = GetSchemaStrongName(messageType);

                        inmsg.Context.Promote("SchemaStrongName",
                            "http://schemas.microsoft.com/BizTalk/2003/system-properties"
                            , schemaStrongName);
                    }
                    pc.ResourceTracker.AddResource(dataStream);
                }
            }
            catch (Exception e)
            {
                throw new InvalidProgramException(e.Message);
            }

            return inmsg;
        }
        #endregion

        #region Private members
        /// <summary>
        /// Reads property value from property bag
        /// </summary>
        /// <param name="pb">Property bag</param>
        /// <param name="propName">Name of property</param>
        /// <returns>Value of the property</returns>
        [SuppressMessage("Microsoft.Performance", "CA1822:Mark members as static", Justification = "Generated by BizTalk")]
        private object ReadPropertyBag(Microsoft.BizTalk.Component.Interop.IPropertyBag pb, string propName)
        {
            object val = null;
            try
            {
                pb.Read(propName, out val, 0);
            }
            catch (ArgumentException)
            {
                return val;
            }
            catch (Exception e)
            {
                throw new InvalidProgramException(e.Message);
            }

            return val;
        }

        /// <summary>
        /// Writes property values into a property bag.
        /// </summary>
        /// <param name="pb">Property bag.</param>
        /// <param name="propName">Name of property.</param>
        /// <param name="val">Value of property.</param>
        [SuppressMessage("Microsoft.Performance", "CA1822:Mark members as static", Justification = "Generated by BizTalk")]
        private void WritePropertyBag(Microsoft.BizTalk.Component.Interop.IPropertyBag pb, string propName, object val)
        {
            try
            {
                pb.Write(propName, ref val);
            }
            catch (Exception e)
            {
                throw new InvalidProgramException(e.Message);
            }
        }

        #endregion
        #region IDisposable Members

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        protected virtual void Dispose(bool force)
        {
        }

        private string RecognizeMessageType(IPipelineContext pContext, IBaseMessage pInMsg)
        {
            var stream =
                new MarkableForwardOnlyEventingReadStream(
                    pInMsg.BodyPart.GetOriginalDataStream());

            try
            {
                string messagetype = Utils.GetDocType(stream);

                pInMsg.BodyPart.Data = stream;
                pContext.ResourceTracker.AddResource(stream);

                return messagetype;
            }

// ReSharper disable once EmptyGeneralCatchClause
            catch (Exception /* e */)
            {
            }

            return String.Empty;
        }

        private string GetSchemaStrongName(string messageType)
        {
            string schemaTns;
            string schemaRoot;
            if (messageType.Contains("#"))
            {
                schemaTns = messageType.Substring(0, messageType.IndexOf('#'));
                schemaRoot = messageType.Substring(messageType.IndexOf('#') + 1);
            }
            else
            {
                schemaTns = null;
                schemaRoot = messageType;
            }
            try
            {
                var explorerOm = new BtsCatalogExplorer { ConnectionString = BtsConnectionString };

                foreach (Schema schema in explorerOm.Schemas)
                    if (schema.RootName == schemaRoot && schema.TargetNameSpace == schemaTns)
                        return schema.AssemblyQualifiedName;
            }

// ReSharper disable once EmptyGeneralCatchClause
            catch (Exception)
            {
            }

            return String.Empty;
        }

        private static string BtsConnectionString
        {
            get
            {
                RegistryKey hKey = Registry.LocalMachine.OpenSubKey(RegKeyBtsAdministration, false);
                if (hKey != null)
                    return String.Format("Data Source={0};Initial Catalog={1};Integrated Security=SSPI;"
                        , hKey.GetValue(MgmtDbServer)
                        , hKey.GetValue(MgmtDbName));
                return null;
            }
        }
        #endregion
    }
}
