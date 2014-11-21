﻿/***************************************************************************
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

using System.Collections.Generic;

namespace RunTimeCompiler
{
    /// <summary>
    ///     This class should contain all the information (extracted from a project file)
    ///     needed for UI or compilation.
    ///     The current structure of this class is based on the structure of a c#
    ///     project file, but it may be enhanced if the necessity arise during the
    ///     development of other project-readers.
    /// </summary>
    public class BasicProject
    {
        public string BuildOutput;

        /// <summary>
        ///     The list of the configuration files that are part of the project.
        /// </summary>
        public List<ProjectConfigFile> ConfigFiles = new List<ProjectConfigFile>();

        /// <summary>
        ///     The list of content files included.
        ///     Content files are files (usually documents) that are included
        ///     in the project, and usually copied to the output folder after build.
        ///     Common file types are .txt, .pdf, .html, but there is no restriction;
        ///     these can even be some source code files.
        /// </summary>
        public List<ProjectContentFile> ContentFiles = new List<ProjectContentFile>();

        public string ProjectFile;
        public string ProjectFolder;

        /// <summary>
        ///     The list of the referenced assemblies.
        /// </summary>
        public List<string> References = new List<string>();

        /// <summary>
        ///     The list of the resource files that are part of the project.
        /// </summary>
        public List<ProjectResourceFile> ResourceFiles = new List<ProjectResourceFile>();

        /// <summary>
        ///     All project settings including general settings (framework version,
        ///     project type winexe/dll/console...,assembly name etc) and the settings
        ///     for the active configuration (debug/release...) (output folder, warning
        ///     level etc).
        /// </summary>
        public ProjectSettings Settings = new ProjectSettings();

        /// <summary>
        ///     The list of the source code files that are part of the project.
        /// </summary>
        public List<ProjectSourceFile> SourceFiles = new List<ProjectSourceFile>();
    }

    /// <summary>
    ///     This class contains all important setting that are retrieved while
    ///     parsing the project file.
    ///     It includes general settings (framework version, project type
    ///     winexe/dll/console...,assembly name etc) and the settings
    ///     for the active configuration (debug/release...) (output folder,
    ///     warning level etc).
    /// </summary>
    public class ProjectSettings
    {
        public string AppDesignerFolder = string.Empty;
        public string AssemblyName = string.Empty;
        public string CompilationConstants = string.Empty;
        public string FileAlignment = "512";
        public string Optimize = string.Empty;
        public string OutputPath = string.Empty;
        public string OutputType = string.Empty;
        public string ProductVersion = string.Empty;
        public string ProjectGuid = string.Empty;
        public string RootNamespace = string.Empty;
        public string SchemaVersion = string.Empty;
        public string TargetFrameworkVersion = "v4.5";
        public string WarningLevel = string.Empty;
    }

    /// <summary>
    ///     This should probably be the base class for all other
    ///     project-file classes but as I do not know how the
    ///     structure of these classes will evolve as I'll add more
    ///     project-readers, I believe it is safer to keep them as
    ///     independent classes.
    /// </summary>
    public class ProjectContentFile
    {
        public bool CopyToOutputDirectory;
        public string Location = string.Empty;
        public string Name = string.Empty;
    }

    /// <summary>
    ///     This should probably derived from ProjectContentFile
    ///     but as I do not know how the structure of these classes
    ///     will evolve as I'll add more project-readers, I believe
    ///     it is safer to keep them as independent classes.
    /// </summary>
    public class ProjectResourceFile
    {
        public bool CopyToOutputDirectory;
        public string DependentUpon = string.Empty;
        public string Location = string.Empty;
        public string Name = string.Empty;
    }

    /// <summary>
    ///     This should probably derived from ProjectContentFile
    ///     but as I do not know how the structure of these classes
    ///     will evolve as I'll add more project-readers, I believe
    ///     it is safer to keep them as independent classes.
    /// </summary>
    public class ProjectSourceFile
    {
        public bool CopyToOutputDirectory;
        public string DependentUpon = string.Empty;
        public string Location = string.Empty;
        public string Name = string.Empty;
    }

    /// <summary>
    ///     This should probably derived from ProjectContentFile
    ///     but as I do not know how the structure of these classes
    ///     will evolve as I'll add more project-readers, I believe
    ///     it is safer to keep them as independent classes.
    /// </summary>
    public class ProjectConfigFile
    {
        public bool CopyToOutputDirectory;
        public string Location = string.Empty;
        public string Name = string.Empty;
    }
}