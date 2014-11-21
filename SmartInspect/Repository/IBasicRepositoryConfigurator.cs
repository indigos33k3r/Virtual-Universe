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

namespace SmartInspect.Repository
{
	/// <summary>
	/// Basic Configurator interface for repositories
	/// </summary>
	/// <remarks>
	/// <para>
	/// Interface used by basic configurator to configure a <see cref="ILoggerRepository"/>
	/// with a default <see cref="SmartInspect.Appender.IAppender"/>.
	/// </para>
	/// <para>
	/// A <see cref="ILoggerRepository"/> should implement this interface to support
	/// configuration by the <see cref="SmartInspect.Config.BasicConfigurator"/>.
	/// </para>
	/// </remarks>
	/// <author>Nicko Cadell</author>
	/// <author>Gert Driesen</author>
	public interface IBasicRepositoryConfigurator
	{
		/// <summary>
		/// Initialize the repository using the specified appender
		/// </summary>
		/// <param name="appender">the appender to use to log all logging events</param>
		/// <remarks>
		/// <para>
		/// Configure the repository to route all logging events to the
		/// specified appender.
		/// </para>
		/// </remarks>
        void Configure(Appender.IAppender appender);

        /// <summary>
        /// Initialize the repository using the specified appenders
        /// </summary>
        /// <param name="appenders">the appenders to use to log all logging events</param>
        /// <remarks>
        /// <para>
        /// Configure the repository to route all logging events to the
        /// specified appenders.
        /// </para>
        /// </remarks>
        void Configure(params Appender.IAppender[] appenders);
	}
}