/***************************************************************************
 *	                VIRTUAL REALITY PUBLIC SOURCE LICENSE
 * 
 * Date				: Sun January 1, 2006
 * Copyright		: (c) 2006-2014 by Virtual Reality Development Team. 
 *                    All Rights Reserved.
 * Website			: http://www.syndarveruleiki.is
 *
 * Product Name		: Virtual Reality
 * License Text     : packages/docs/VRLICENSE.txt
 * 
 * Planetary Info   : Information about the Planetary code
 * 
 * Copyright        : (c) 2014-2024 by Second Galaxy Development Team
 *                    All Rights Reserved.
 * 
 * Website          : http://www.secondgalaxy.com
 * 
 * Product Name     : Virtual Reality
 * License Text     : packages/docs/SGLICENSE.txt
 * 
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * Neither the name of the WhiteCore-Sim Project nor the
 *       names of its contributors may be used to endorse or promote products
 *       derived from this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE DEVELOPERS ``AS IS'' AND ANY
 * EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL THE CONTRIBUTORS BE LIABLE FOR ANY
 * DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
 * ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
 * SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
***************************************************************************/

using System;
using System.Xml;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Net;

using SmartInspect.Appender;
using SmartInspect.Util;
using SmartInspect.Repository;

namespace SmartInspect.Config
{
	/// <summary>
	/// Use this class to initialize the SmartInspect environment using an Xml tree.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Configures a <see cref="ILoggerRepository"/> using an Xml tree.
	/// </para>
	/// </remarks>
	/// <author>Nicko Cadell</author>
	/// <author>Gert Driesen</author>
	public sealed class XmlConfigurator
	{
		#region Private Instance Constructors

		/// <summary>
		/// Private constructor
		/// </summary>
		private XmlConfigurator() 
		{ 
		}

		#endregion Protected Instance Constructors

		#region Configure static methods

#if !NETCF
		/// <summary>
		/// Automatically configures the SmartInspect system based on the 
		/// application's configuration settings.
		/// </summary>
		/// <remarks>
		/// <para>
		/// Each application has a configuration file. This has the
		/// same name as the application with '.config' appended.
		/// This file is XML and calling this function prompts the
		/// configurator to look in that file for a section called
		/// <c>SmartInspect</c> that contains the configuration data.
		/// </para>
		/// <para>
		/// To use this method to configure SmartInspect you must specify 
		/// the <see cref="SmartInspectConfigurationSectionHandler"/> section
		/// handler for the <c>SmartInspect</c> configuration section. See the
		/// <see cref="SmartInspectConfigurationSectionHandler"/> for an example.
		/// </para>
		/// </remarks>
		/// <seealso cref="SmartInspectConfigurationSectionHandler"/>
#else
		/// <summary>
		/// Automatically configures the SmartInspect system based on the 
		/// application's configuration settings.
		/// </summary>
		/// <remarks>
		/// <para>
		/// Each application has a configuration file. This has the
		/// same name as the application with '.config' appended.
		/// This file is XML and calling this function prompts the
		/// configurator to look in that file for a section called
		/// <c>SmartInspect</c> that contains the configuration data.
		/// </para>
		/// </remarks>
#endif
        static public ICollection Configure()
        {
            return Configure(LogManager.GetRepository(Assembly.GetCallingAssembly()));
        }

#if !NETCF
        /// <summary>
        /// Automatically configures the <see cref="ILoggerRepository"/> using settings
        /// stored in the application's configuration file.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Each application has a configuration file. This has the
        /// same name as the application with '.config' appended.
        /// This file is XML and calling this function prompts the
        /// configurator to look in that file for a section called
        /// <c>SmartInspect</c> that contains the configuration data.
        /// </para>
        /// <para>
        /// To use this method to configure SmartInspect you must specify 
        /// the <see cref="SmartInspectConfigurationSectionHandler"/> section
        /// handler for the <c>SmartInspect</c> configuration section. See the
        /// <see cref="SmartInspectConfigurationSectionHandler"/> for an example.
        /// </para>
        /// </remarks>
        /// <param name="repository">The repository to configure.</param>
#else
		/// <summary>
		/// Automatically configures the <see cref="ILoggerRepository"/> using settings
		/// stored in the application's configuration file.
		/// </summary>
		/// <remarks>
		/// <para>
		/// Each application has a configuration file. This has the
		/// same name as the application with '.config' appended.
		/// This file is XML and calling this function prompts the
		/// configurator to look in that file for a section called
		/// <c>SmartInspect</c> that contains the configuration data.
		/// </para>
		/// </remarks>
		/// <param name="repository">The repository to configure.</param>
#endif
        static public ICollection Configure(ILoggerRepository repository)
        {
            ArrayList configurationMessages = new ArrayList();

            using (new LogLog.LogReceivedAdapter(configurationMessages))
            {
                InternalConfigure(repository);
            }

            repository.ConfigurationMessages = configurationMessages;

            return configurationMessages;
        }

	    static private void InternalConfigure(ILoggerRepository repository) 
		{
			LogLog.Debug(declaringType, "configuring repository [" + repository.Name + "] using .config file section");

			try
			{
				LogLog.Debug(declaringType, "Application config file is [" + SystemInfo.ConfigurationFileLocation + "]");
			}
			catch
			{
				// ignore error
				LogLog.Debug(declaringType, "Application config file location unknown");
			}

#if NETCF
			// No config file reading stuff. Just go straight for the file
			Configure(repository, new FileInfo(SystemInfo.ConfigurationFileLocation));
#else
			try
			{
				XmlElement configElement = null;
				configElement = System.Configuration.ConfigurationManager.GetSection("SmartInspect") as XmlElement;
				if (configElement == null)
				{
					// Failed to load the xml config using configuration settings handler
					LogLog.Error(declaringType, "Failed to find configuration section 'SmartInspect' in the application's .config file. Check your .config file for the <SmartInspect> and <configSections> elements. The configuration section should look like: <section name=\"SmartInspect\" type=\"SmartInspect.Config.SmartInspectConfigurationSectionHandler,SmartInspect\" />");
				}
				else
				{
					// Configure using the xml loaded from the config file
					InternalConfigureFromXml(repository, configElement);
				}
			}
			catch(System.Configuration.ConfigurationException confEx)
			{
				if (confEx.BareMessage.IndexOf("Unrecognized element") >= 0)
				{
					// Looks like the XML file is not valid
					LogLog.Error(declaringType, "Failed to parse config file. Check your .config file is well formed XML.", confEx);
				}
				else
				{
					// This exception is typically due to the assembly name not being correctly specified in the section type.
					string configSectionStr = "<section name=\"SmartInspect\" type=\"SmartInspect.Config.SmartInspectConfigurationSectionHandler," + Assembly.GetExecutingAssembly().FullName + "\" />";
					LogLog.Error(declaringType, "Failed to parse config file. Is the <configSections> specified as: " + configSectionStr, confEx);
				}
			}
#endif
		}

		/// <summary>
		/// Configures SmartInspect using a <c>SmartInspect</c> element
		/// </summary>
		/// <remarks>
		/// <para>
		/// Loads the SmartInspect configuration from the XML element
		/// supplied as <paramref name="element"/>.
		/// </para>
		/// </remarks>
		/// <param name="element">The element to parse.</param>
		static public ICollection Configure(XmlElement element) 
		{
            ArrayList configurationMessages = new ArrayList();

            ILoggerRepository repository = LogManager.GetRepository(Assembly.GetCallingAssembly());

            using (new LogLog.LogReceivedAdapter(configurationMessages))
            {
                InternalConfigureFromXml(repository, element);
            }

            repository.ConfigurationMessages = configurationMessages;

            return configurationMessages;
		}

		/// <summary>
		/// Configures the <see cref="ILoggerRepository"/> using the specified XML 
		/// element.
		/// </summary>
		/// <remarks>
		/// Loads the SmartInspect configuration from the XML element
		/// supplied as <paramref name="element"/>.
		/// </remarks>
		/// <param name="repository">The repository to configure.</param>
		/// <param name="element">The element to parse.</param>
		static public ICollection Configure(ILoggerRepository repository, XmlElement element) 
		{
            ArrayList configurationMessages = new ArrayList();

            using (new LogLog.LogReceivedAdapter(configurationMessages))
            {
                LogLog.Debug(declaringType, "configuring repository [" + repository.Name + "] using XML element");

                InternalConfigureFromXml(repository, element);
            }

            repository.ConfigurationMessages = configurationMessages;

            return configurationMessages;
		}

#if !NETCF
		/// <summary>
		/// Configures SmartInspect using the specified configuration file.
		/// </summary>
		/// <param name="configFile">The XML file to load the configuration from.</param>
		/// <remarks>
		/// <para>
		/// The configuration file must be valid XML. It must contain
		/// at least one element called <c>SmartInspect</c> that holds
		/// the SmartInspect configuration data.
		/// </para>
		/// <para>
		/// The SmartInspect configuration file can possible be specified in the application's
		/// configuration file (either <c>MyAppName.exe.config</c> for a
		/// normal application on <c>Web.config</c> for an ASP.NET application).
		/// </para>
		/// <para>
		/// The first element matching <c>&lt;configuration&gt;</c> will be read as the 
		/// configuration. If this file is also a .NET .config file then you must specify 
		/// a configuration section for the <c>SmartInspect</c> element otherwise .NET will 
		/// complain. Set the type for the section handler to <see cref="System.Configuration.IgnoreSectionHandler"/>, for example:
		/// <code lang="XML" escaped="true">
		/// <configSections>
		///		<section name="SmartInspect" type="System.Configuration.IgnoreSectionHandler" />
		///	</configSections>
		/// </code>
		/// </para>
		/// <example>
		/// The following example configures SmartInspect using a configuration file, of which the 
		/// location is stored in the application's configuration file :
		/// </example>
		/// <code lang="C#">
		/// using SmartInspect.Config;
		/// using System.IO;
		/// using System.Configuration;
		/// 
		/// ...
		/// 
		/// XmlConfigurator.Configure(new FileInfo(ConfigurationSettings.AppSettings["SmartInspect-config-file"]));
		/// </code>
		/// <para>
		/// In the <c>.config</c> file, the path to the SmartInspect can be specified like this :
		/// </para>
		/// <code lang="XML" escaped="true">
		/// <configuration>
		///		<appSettings>
		///			<add key="SmartInspect-config-file" value="log.config"/>
		///		</appSettings>
		///	</configuration>
		/// </code>
		/// </remarks>
#else
		/// <summary>
		/// Configures SmartInspect using the specified configuration file.
		/// </summary>
		/// <param name="configFile">The XML file to load the configuration from.</param>
		/// <remarks>
		/// <para>
		/// The configuration file must be valid XML. It must contain
		/// at least one element called <c>SmartInspect</c> that holds
		/// the SmartInspect configuration data.
		/// </para>
		/// <example>
		/// The following example configures SmartInspect using a configuration file, of which the 
		/// location is stored in the application's configuration file :
		/// </example>
		/// <code lang="C#">
		/// using SmartInspect.Config;
		/// using System.IO;
		/// using System.Configuration;
		/// 
		/// ...
		/// 
		/// XmlConfigurator.Configure(new FileInfo(ConfigurationSettings.AppSettings["SmartInspect-config-file"]));
		/// </code>
		/// <para>
		/// In the <c>.config</c> file, the path to the SmartInspect can be specified like this :
		/// </para>
		/// <code lang="XML" escaped="true">
		/// <configuration>
		///		<appSettings>
		///			<add key="SmartInspect-config-file" value="log.config"/>
		///		</appSettings>
		///	</configuration>
		/// </code>
		/// </remarks>
#endif
		static public ICollection Configure(FileInfo configFile)
		{
            ArrayList configurationMessages = new ArrayList();

            using (new LogLog.LogReceivedAdapter(configurationMessages))
            {
                InternalConfigure(LogManager.GetRepository(Assembly.GetCallingAssembly()), configFile);
            }

            return configurationMessages;
		}

		/// <summary>
		/// Configures SmartInspect using the specified configuration URI.
		/// </summary>
		/// <param name="configUri">A URI to load the XML configuration from.</param>
		/// <remarks>
		/// <para>
		/// The configuration data must be valid XML. It must contain
		/// at least one element called <c>SmartInspect</c> that holds
		/// the SmartInspect configuration data.
		/// </para>
		/// <para>
		/// The <see cref="System.Net.WebRequest"/> must support the URI scheme specified.
		/// </para>
		/// </remarks>
		static public ICollection Configure(Uri configUri)
		{
            ArrayList configurationMessages = new ArrayList();

            ILoggerRepository repository = LogManager.GetRepository(Assembly.GetCallingAssembly());

            using (new LogLog.LogReceivedAdapter(configurationMessages))
            {
                InternalConfigure(repository, configUri);
            }

            repository.ConfigurationMessages = configurationMessages;

            return configurationMessages;
		}

		/// <summary>
		/// Configures SmartInspect using the specified configuration data stream.
		/// </summary>
		/// <param name="configStream">A stream to load the XML configuration from.</param>
		/// <remarks>
		/// <para>
		/// The configuration data must be valid XML. It must contain
		/// at least one element called <c>SmartInspect</c> that holds
		/// the SmartInspect configuration data.
		/// </para>
		/// <para>
		/// Note that this method will NOT close the stream parameter.
		/// </para>
		/// </remarks>
		static public ICollection Configure(Stream configStream)
		{
            ArrayList configurationMessages = new ArrayList();

            ILoggerRepository repository = LogManager.GetRepository(Assembly.GetCallingAssembly());

            using (new LogLog.LogReceivedAdapter(configurationMessages))
            {
                InternalConfigure(repository, configStream);
            }

            repository.ConfigurationMessages = configurationMessages;

            return configurationMessages;
		}

#if !NETCF

        /// <summary>
        /// Configures the <see cref="ILoggerRepository"/> using the specified configuration 
        /// file.
        /// </summary>
        /// <param name="repository">The repository to configure.</param>
        /// <param name="configFile">The XML file to load the configuration from.</param>
        /// <remarks>
        /// <para>
        /// The configuration file must be valid XML. It must contain
        /// at least one element called <c>SmartInspect</c> that holds
        /// the configuration data.
        /// </para>
        /// <para>
        /// The SmartInspect configuration file can possible be specified in the application's
        /// configuration file (either <c>MyAppName.exe.config</c> for a
        /// normal application on <c>Web.config</c> for an ASP.NET application).
        /// </para>
        /// <para>
        /// The first element matching <c>&lt;configuration&gt;</c> will be read as the 
        /// configuration. If this file is also a .NET .config file then you must specify 
        /// a configuration section for the <c>SmartInspect</c> element otherwise .NET will 
        /// complain. Set the type for the section handler to <see cref="System.Configuration.IgnoreSectionHandler"/>, for example:
        /// <code lang="XML" escaped="true">
        /// <configSections>
        ///		<section name="SmartInspect" type="System.Configuration.IgnoreSectionHandler" />
        ///	</configSections>
        /// </code>
        /// </para>
        /// <example>
        /// The following example configures SmartInspect using a configuration file, of which the 
        /// location is stored in the application's configuration file :
        /// </example>
        /// <code lang="C#">
        /// using SmartInspect.Config;
        /// using System.IO;
        /// using System.Configuration;
        /// 
        /// ...
        /// 
        /// XmlConfigurator.Configure(new FileInfo(ConfigurationSettings.AppSettings["SmartInspect-config-file"]));
        /// </code>
        /// <para>
        /// In the <c>.config</c> file, the path to the SmartInspect can be specified like this :
        /// </para>
        /// <code lang="XML" escaped="true">
        /// <configuration>
        ///		<appSettings>
        ///			<add key="SmartInspect-config-file" value="log.config"/>
        ///		</appSettings>
        ///	</configuration>
        /// </code>
        /// </remarks>
#else
		/// <summary>
		/// Configures the <see cref="ILoggerRepository"/> using the specified configuration 
		/// file.
		/// </summary>
		/// <param name="repository">The repository to configure.</param>
		/// <param name="configFile">The XML file to load the configuration from.</param>
		/// <remarks>
		/// <para>
		/// The configuration file must be valid XML. It must contain
		/// at least one element called <c>SmartInspect</c> that holds
		/// the configuration data.
		/// </para>
		/// <example>
		/// The following example configures SmartInspect using a configuration file, of which the 
		/// location is stored in the application's configuration file :
		/// </example>
		/// <code lang="C#">
		/// using SmartInspect.Config;
		/// using System.IO;
		/// using System.Configuration;
		/// 
		/// ...
		/// 
		/// XmlConfigurator.Configure(new FileInfo(ConfigurationSettings.AppSettings["SmartInspect-config-file"]));
		/// </code>
		/// <para>
		/// In the <c>.config</c> file, the path to the SmartInspect can be specified like this :
		/// </para>
		/// <code lang="XML" escaped="true">
		/// <configuration>
		///		<appSettings>
		///			<add key="SmartInspect-config-file" value="log.config"/>
		///		</appSettings>
		///	</configuration>
		/// </code>
		/// </remarks>
#endif
        static public ICollection Configure(ILoggerRepository repository, FileInfo configFile)
        {
            ArrayList configurationMessages = new ArrayList();

            using (new LogLog.LogReceivedAdapter(configurationMessages))
            {
                InternalConfigure(repository, configFile);
            }

            repository.ConfigurationMessages = configurationMessages;

            return configurationMessages;
        }
	    
		static private void InternalConfigure(ILoggerRepository repository, FileInfo configFile)
		{
			LogLog.Debug(declaringType, "configuring repository [" + repository.Name + "] using file [" + configFile + "]");

			if (configFile == null)
			{
				LogLog.Error(declaringType, "Configure called with null 'configFile' parameter");
			}
			else
			{
				// Have to use File.Exists() rather than configFile.Exists()
				// because configFile.Exists() caches the value, not what we want.
				if (File.Exists(configFile.FullName))
				{
					// Open the file for reading
					FileStream fs = null;
					
					// Try hard to open the file
					for(int retry = 5; --retry >= 0; )
					{
						try
						{
							fs = configFile.Open(FileMode.Open, FileAccess.Read, FileShare.Read);
							break;
						}
						catch(IOException ex)
						{
							if (retry == 0)
							{
								LogLog.Error(declaringType, "Failed to open XML config file [" + configFile.Name + "]", ex);

								// The stream cannot be valid
								fs = null;
							}
							System.Threading.Thread.Sleep(250);
						}
					}

					if (fs != null)
					{
						try
						{
							// Load the configuration from the stream
							InternalConfigure(repository, fs);
						}
						finally
						{
							// Force the file closed whatever happens
							fs.Close();
						}
					}
				}
				else
				{
					LogLog.Debug(declaringType, "config file [" + configFile.FullName + "] not found. Configuration unchanged.");
				}
			}
		}

        /// <summary>
        /// Configures the <see cref="ILoggerRepository"/> using the specified configuration 
        /// URI.
        /// </summary>
        /// <param name="repository">The repository to configure.</param>
        /// <param name="configUri">A URI to load the XML configuration from.</param>
        /// <remarks>
        /// <para>
        /// The configuration data must be valid XML. It must contain
        /// at least one element called <c>SmartInspect</c> that holds
        /// the configuration data.
        /// </para>
        /// <para>
        /// The <see cref="System.Net.WebRequest"/> must support the URI scheme specified.
        /// </para>
        /// </remarks>
        static public ICollection Configure(ILoggerRepository repository, Uri configUri)
        {
            ArrayList configurationMessages = new ArrayList();

            using (new LogLog.LogReceivedAdapter(configurationMessages))
            {
                InternalConfigure(repository, configUri);
            }

            repository.ConfigurationMessages = configurationMessages;

            return configurationMessages;
        }
	   
		static private void InternalConfigure(ILoggerRepository repository, Uri configUri)
		{
			LogLog.Debug(declaringType, "configuring repository [" + repository.Name + "] using URI ["+configUri+"]");

			if (configUri == null)
			{
				LogLog.Error(declaringType, "Configure called with null 'configUri' parameter");
			}
			else
			{
				if (configUri.IsFile)
				{
					// If URI is local file then call Configure with FileInfo
					InternalConfigure(repository, new FileInfo(configUri.LocalPath));
				}
				else
				{
					// NETCF dose not support WebClient
					WebRequest configRequest = null;

					try
					{
						configRequest = WebRequest.Create(configUri);
					}
					catch(Exception ex)
					{
						LogLog.Error(declaringType, "Failed to create WebRequest for URI ["+configUri+"]", ex);
					}

					if (configRequest != null)
					{
						// authentication may be required, set client to use default credentials
						try
						{
							configRequest.Credentials = CredentialCache.DefaultCredentials;
						}
						catch
						{
							// ignore security exception
						}

						try
						{
							WebResponse response = configRequest.GetResponse();
							if (response != null)
							{
								try
								{
									// Open stream on config URI
									using(Stream configStream = response.GetResponseStream())
									{
										InternalConfigure(repository, configStream);
									}
								}
								finally
								{
									response.Close();
								}
							}
						}
						catch(Exception ex)
						{
							LogLog.Error(declaringType, "Failed to request config from URI ["+configUri+"]", ex);
						}
					}
				}
			}
		}

        /// <summary>
        /// Configures the <see cref="ILoggerRepository"/> using the specified configuration 
        /// file.
        /// </summary>
        /// <param name="repository">The repository to configure.</param>
        /// <param name="configStream">The stream to load the XML configuration from.</param>
        /// <remarks>
        /// <para>
        /// The configuration data must be valid XML. It must contain
        /// at least one element called <c>SmartInspect</c> that holds
        /// the configuration data.
        /// </para>
        /// <para>
        /// Note that this method will NOT close the stream parameter.
        /// </para>
        /// </remarks>
        static public ICollection Configure(ILoggerRepository repository, Stream configStream)
        {
            ArrayList configurationMessages = new ArrayList();

            using (new LogLog.LogReceivedAdapter(configurationMessages))
            {
                InternalConfigure(repository, configStream);
            }

            repository.ConfigurationMessages = configurationMessages;

            return configurationMessages;
        }
	  
		static private void InternalConfigure(ILoggerRepository repository, Stream configStream)
		{
			LogLog.Debug(declaringType, "configuring repository [" + repository.Name + "] using stream");

			if (configStream == null)
			{
				LogLog.Error(declaringType, "Configure called with null 'configStream' parameter");
			}
			else
			{
				// Load the config file into a document
				XmlDocument doc = new XmlDocument();
				try
				{
#if (NETCF)
					// Create a text reader for the file stream
					XmlTextReader xmlReader = new XmlTextReader(configStream);
#else
					// Allow the DTD to specify entity includes
					XmlReaderSettings settings = new XmlReaderSettings();
                                        // .NET 4.0 warning CS0618: 'System.Xml.XmlReaderSettings.ProhibitDtd'
                                        // is obsolete: 'Use XmlReaderSettings.DtdProcessing property instead.'
#if !FRAMEWORK_4_0_OR_ABOVE
					settings.ProhibitDtd = false;
#else
					settings.DtdProcessing = DtdProcessing.Parse;
#endif

					// Create a reader over the input stream
					XmlReader xmlReader = XmlReader.Create(configStream, settings);
#endif
					
					// load the data into the document
					doc.Load(xmlReader);
				}
				catch(Exception ex)
				{
					LogLog.Error(declaringType, "Error while loading XML configuration", ex);

					// The document is invalid
					doc = null;
				}

				if (doc != null)
				{
					LogLog.Debug(declaringType, "loading XML configuration");

					// Configure using the 'SmartInspect' element
					XmlNodeList configNodeList = doc.GetElementsByTagName("SmartInspect");
					if (configNodeList.Count == 0)
					{
						LogLog.Debug(declaringType, "XML configuration does not contain a <SmartInspect> element. Configuration Aborted.");
					}
					else if (configNodeList.Count > 1)
					{
						LogLog.Error(declaringType, "XML configuration contains [" + configNodeList.Count + "] <SmartInspect> elements. Only one is allowed. Configuration Aborted.");
					}
					else
					{
						InternalConfigureFromXml(repository, configNodeList[0] as XmlElement);
					}
				}
			}
		}

		#endregion Configure static methods

		#region ConfigureAndWatch static methods

#if !NETCF

		/// <summary>
		/// Configures SmartInspect using the file specified, monitors the file for changes 
		/// and reloads the configuration if a change is detected.
		/// </summary>
		/// <param name="configFile">The XML file to load the configuration from.</param>
		/// <remarks>
		/// <para>
		/// The configuration file must be valid XML. It must contain
		/// at least one element called <c>SmartInspect</c> that holds
		/// the configuration data.
		/// </para>
		/// <para>
		/// The configuration file will be monitored using a <see cref="FileSystemWatcher"/>
		/// and depends on the behavior of that class.
		/// </para>
		/// <para>
		/// For more information on how to configure SmartInspect using
		/// a separate configuration file, see <see cref="M:Configure(FileInfo)"/>.
		/// </para>
		/// </remarks>
		/// <seealso cref="M:Configure(FileInfo)"/>
		static public ICollection ConfigureAndWatch(FileInfo configFile)
		{
            ArrayList configurationMessages = new ArrayList();

            ILoggerRepository repository = LogManager.GetRepository(Assembly.GetCallingAssembly());

            using (new LogLog.LogReceivedAdapter(configurationMessages))
            {
                InternalConfigureAndWatch(repository, configFile);
            }

            repository.ConfigurationMessages = configurationMessages;

            return configurationMessages;
		}

        /// <summary>
        /// Configures the <see cref="ILoggerRepository"/> using the file specified, 
        /// monitors the file for changes and reloads the configuration if a change 
        /// is detected.
        /// </summary>
        /// <param name="repository">The repository to configure.</param>
        /// <param name="configFile">The XML file to load the configuration from.</param>
        /// <remarks>
        /// <para>
        /// The configuration file must be valid XML. It must contain
        /// at least one element called <c>SmartInspect</c> that holds
        /// the configuration data.
        /// </para>
        /// <para>
        /// The configuration file will be monitored using a <see cref="FileSystemWatcher"/>
        /// and depends on the behavior of that class.
        /// </para>
        /// <para>
        /// For more information on how to configure SmartInspect using
        /// a separate configuration file, see <see cref="M:Configure(FileInfo)"/>.
        /// </para>
        /// </remarks>
        /// <seealso cref="M:Configure(FileInfo)"/>
        static public ICollection ConfigureAndWatch(ILoggerRepository repository, FileInfo configFile)
        {
            ArrayList configurationMessages = new ArrayList();

            using (new LogLog.LogReceivedAdapter(configurationMessages))
            {
                InternalConfigureAndWatch(repository, configFile);
            }

            repository.ConfigurationMessages = configurationMessages;

            return configurationMessages;
        }
	   
		static private void InternalConfigureAndWatch(ILoggerRepository repository, FileInfo configFile)
		{
			LogLog.Debug(declaringType, "configuring repository [" + repository.Name + "] using file [" + configFile + "] watching for file updates");

			if (configFile == null)
			{
				LogLog.Error(declaringType, "ConfigureAndWatch called with null 'configFile' parameter");
			}
			else
			{
				// Configure SmartInspect now
				InternalConfigure(repository, configFile);

				try
				{
                    lock (m_repositoryName2ConfigAndWatchHandler)
                    {
                        // support multiple repositories each having their own watcher
                        ConfigureAndWatchHandler handler =
							(ConfigureAndWatchHandler)m_repositoryName2ConfigAndWatchHandler[configFile.FullName];

                        if (handler != null)
                        {
							m_repositoryName2ConfigAndWatchHandler.Remove(configFile.FullName);
                            handler.Dispose();
                        }

                        // Create and start a watch handler that will reload the
                        // configuration whenever the config file is modified.
                        handler = new ConfigureAndWatchHandler(repository, configFile);
						m_repositoryName2ConfigAndWatchHandler[configFile.FullName] = handler;
                    }
				}
				catch(Exception ex)
				{
					LogLog.Error(declaringType, "Failed to initialize configuration file watcher for file ["+configFile.FullName+"]", ex);
				}
			}
		}
#endif

		#endregion ConfigureAndWatch static methods

		#region ConfigureAndWatchHandler

#if !NETCF
		/// <summary>
		/// Class used to watch config files.
		/// </summary>
		/// <remarks>
		/// <para>
		/// Uses the <see cref="FileSystemWatcher"/> to monitor
		/// changes to a specified file. Because multiple change notifications
		/// may be raised when the file is modified, a timer is used to
		/// compress the notifications into a single event. The timer
		/// waits for <see cref="TimeoutMillis"/> time before delivering
		/// the event notification. If any further <see cref="FileSystemWatcher"/>
		/// change notifications arrive while the timer is waiting it
		/// is reset and waits again for <see cref="TimeoutMillis"/> to
		/// elapse.
		/// </para>
		/// </remarks>
		private sealed class ConfigureAndWatchHandler : IDisposable
		{
			/// <summary>
			/// Holds the FileInfo used to configure the XmlConfigurator
			/// </summary>
			private FileInfo m_configFile;

			/// <summary>
			/// Holds the repository being configured.
			/// </summary>
			private ILoggerRepository m_repository;

			/// <summary>
			/// The timer used to compress the notification events.
			/// </summary>
			private Timer m_timer;

			/// <summary>
			/// The default amount of time to wait after receiving notification
			/// before reloading the config file.
			/// </summary>
			private const int TimeoutMillis = 500;

            /// <summary>
            /// Watches file for changes. This object should be disposed when no longer
            /// needed to free system handles on the watched resources.
            /// </summary>
            private FileSystemWatcher m_watcher;

			/// <summary>
			/// Initializes a new instance of the <see cref="ConfigureAndWatchHandler" /> class to
            /// watch a specified config file used to configure a repository.
			/// </summary>
			/// <param name="repository">The repository to configure.</param>
			/// <param name="configFile">The configuration file to watch.</param>
			/// <remarks>
			/// <para>
			/// Initializes a new instance of the <see cref="ConfigureAndWatchHandler" /> class.
			/// </para>
			/// </remarks>
#if FRAMEWORK_4_0_OR_ABOVE
            [System.Security.SecuritySafeCritical]
#endif
            public ConfigureAndWatchHandler(ILoggerRepository repository, FileInfo configFile)
			{
				m_repository = repository;
				m_configFile = configFile;

				// Create a new FileSystemWatcher and set its properties.
				m_watcher = new FileSystemWatcher();

				m_watcher.Path = m_configFile.DirectoryName;
				m_watcher.Filter = m_configFile.Name;

				// Set the notification filters
				m_watcher.NotifyFilter = NotifyFilters.CreationTime | NotifyFilters.LastWrite | NotifyFilters.FileName;

				// Add event handlers. OnChanged will do for all event handlers that fire a FileSystemEventArgs
				m_watcher.Changed += new FileSystemEventHandler(ConfigureAndWatchHandler_OnChanged);
				m_watcher.Created += new FileSystemEventHandler(ConfigureAndWatchHandler_OnChanged);
				m_watcher.Deleted += new FileSystemEventHandler(ConfigureAndWatchHandler_OnChanged);
				m_watcher.Renamed += new RenamedEventHandler(ConfigureAndWatchHandler_OnRenamed);

				// Begin watching.
				m_watcher.EnableRaisingEvents = true;

				// Create the timer that will be used to deliver events. Set as disabled
                m_timer = new Timer(new TimerCallback(OnWatchedFileChange), null, Timeout.Infinite, Timeout.Infinite);
			}

			/// <summary>
			/// Event handler used by <see cref="ConfigureAndWatchHandler"/>.
			/// </summary>
			/// <param name="source">The <see cref="FileSystemWatcher"/> firing the event.</param>
			/// <param name="e">The argument indicates the file that caused the event to be fired.</param>
			/// <remarks>
			/// <para>
			/// This handler reloads the configuration from the file when the event is fired.
			/// </para>
			/// </remarks>
			private void ConfigureAndWatchHandler_OnChanged(object source, FileSystemEventArgs e)
			{
				LogLog.Debug(declaringType, "ConfigureAndWatchHandler: "+e.ChangeType+" [" + m_configFile.FullName + "]");

				// Deliver the event in TimeoutMillis time
				// timer will fire only once
				m_timer.Change(TimeoutMillis, Timeout.Infinite);
			}

			/// <summary>
			/// Event handler used by <see cref="ConfigureAndWatchHandler"/>.
			/// </summary>
			/// <param name="source">The <see cref="FileSystemWatcher"/> firing the event.</param>
			/// <param name="e">The argument indicates the file that caused the event to be fired.</param>
			/// <remarks>
			/// <para>
			/// This handler reloads the configuration from the file when the event is fired.
			/// </para>
			/// </remarks>
			private void ConfigureAndWatchHandler_OnRenamed(object source, RenamedEventArgs e)
			{
				LogLog.Debug(declaringType, "ConfigureAndWatchHandler: " + e.ChangeType + " [" + m_configFile.FullName + "]");

				// Deliver the event in TimeoutMillis time
				// timer will fire only once
				m_timer.Change(TimeoutMillis, Timeout.Infinite);
			}

			/// <summary>
			/// Called by the timer when the configuration has been updated.
			/// </summary>
			/// <param name="state">null</param>
			private void OnWatchedFileChange(object state)
			{
				XmlConfigurator.InternalConfigure(m_repository, m_configFile);
			}

            /// <summary>
            /// Release the handles held by the watcher and timer.
            /// </summary>
#if FRAMEWORK_4_0_OR_ABOVE
            [System.Security.SecuritySafeCritical]
#endif
            public void Dispose()
            {
                m_watcher.EnableRaisingEvents = false;
                m_watcher.Dispose();
                m_timer.Dispose();
            }
		}
#endif

		#endregion ConfigureAndWatchHandler

		#region Private Static Methods

		/// <summary>
		/// Configures the specified repository using a <c>SmartInspect</c> element.
		/// </summary>
		/// <param name="repository">The hierarchy to configure.</param>
		/// <param name="element">The element to parse.</param>
		/// <remarks>
		/// <para>
		/// Loads the SmartInspect configuration from the XML element
		/// supplied as <paramref name="element"/>.
		/// </para>
		/// <para>
		/// This method is ultimately called by one of the Configure methods 
		/// to load the configuration from an <see cref="XmlElement"/>.
		/// </para>
		/// </remarks>
		static private void InternalConfigureFromXml(ILoggerRepository repository, XmlElement element) 
		{
			if (element == null)
			{
				LogLog.Error(declaringType, "ConfigureFromXml called with null 'element' parameter");
			}
			else if (repository == null)
			{
				LogLog.Error(declaringType, "ConfigureFromXml called with null 'repository' parameter");
			}
			else
			{
				LogLog.Debug(declaringType, "Configuring Repository [" + repository.Name + "]");

				IXmlRepositoryConfigurator configurableRepository = repository as IXmlRepositoryConfigurator;
				if (configurableRepository == null)
				{
					LogLog.Warn(declaringType, "Repository [" + repository + "] does not support the XmlConfigurator");
				}
				else
				{
					// Copy the xml data into the root of a new document
					// this isolates the xml config data from the rest of
					// the document
					XmlDocument newDoc = new XmlDocument();
					XmlElement newElement = (XmlElement)newDoc.AppendChild(newDoc.ImportNode(element, true));

					// Pass the configurator the config element
					configurableRepository.Configure(newElement);
				}			
			}
		}

		#endregion Private Static Methods

	    #region Private Static Fields

        /// <summary>
        /// Maps repository names to ConfigAndWatchHandler instances to allow a particular
        /// ConfigAndWatchHandler to dispose of its FileSystemWatcher when a repository is 
        /// reconfigured.
        /// </summary>
        private readonly static Hashtable m_repositoryName2ConfigAndWatchHandler = new Hashtable();

	    /// <summary>
	    /// The fully qualified type of the XmlConfigurator class.
	    /// </summary>
	    /// <remarks>
	    /// Used by the internal logger to record the Type of the
	    /// log message.
	    /// </remarks>
	    private readonly static Type declaringType = typeof(XmlConfigurator);

	    #endregion Private Static Fields
	}
}
