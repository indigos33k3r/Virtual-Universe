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
using System.Text;
using System.IO;

using SmartInspect.Util;
using SmartInspect.DateFormatter;
using SmartInspect.Core;

namespace SmartInspect.Util.PatternStringConverters
{
	/// <summary>
	/// Write the current date to the output
	/// </summary>
	/// <remarks>
	/// <para>
	/// Date pattern converter, uses a <see cref="IDateFormatter"/> to format 
	/// the current date and time to the writer as a string.
	/// </para>
	/// <para>
	/// The value of the <see cref="SmartInspect.Util.PatternConverter.Option"/> determines 
	/// the formatting of the date. The following values are allowed:
	/// <list type="definition">
	///		<listheader>
	/// 		<term>Option value</term>
	/// 		<description>Output</description>
	/// 	</listheader>
	///		<item>
	/// 		<term>ISO8601</term>
	/// 		<description>
	/// 		Uses the <see cref="Iso8601DateFormatter"/> formatter. 
	/// 		Formats using the <c>"yyyy-MM-dd HH:mm:ss,fff"</c> pattern.
	/// 		</description>
	/// 	</item>
	/// 	<item>
	/// 		<term>DATE</term>
	/// 		<description>
	/// 		Uses the <see cref="DateTimeDateFormatter"/> formatter. 
	/// 		Formats using the <c>"dd MMM yyyy HH:mm:ss,fff"</c> for example, <c>"06 Nov 1994 15:49:37,459"</c>.
	/// 		</description>
	/// 	</item>
	/// 	<item>
	/// 		<term>ABSOLUTE</term>
	/// 		<description>
	/// 		Uses the <see cref="AbsoluteTimeDateFormatter"/> formatter. 
	/// 		Formats using the <c>"HH:mm:ss,fff"</c> for example, <c>"15:49:37,459"</c>.
	/// 		</description>
	/// 	</item>
	/// 	<item>
	/// 		<term>other</term>
	/// 		<description>
	/// 		Any other pattern string uses the <see cref="SimpleDateFormatter"/> formatter. 
	/// 		This formatter passes the pattern string to the <see cref="DateTime"/> 
	/// 		<see cref="M:DateTime.ToString(string)"/> method.
	/// 		For details on valid patterns see 
	/// 		<a href="http://msdn.microsoft.com/library/default.asp?url=/library/en-us/cpref/html/frlrfsystemglobalizationdatetimeformatinfoclasstopic.asp">DateTimeFormatInfo Class</a>.
	/// 		</description>
	/// 	</item>
	/// </list>
	/// </para>
	/// <para>
	/// The date and time is in the local time zone and is rendered in that zone.
	/// To output the time in Universal time see <see cref="UtcDatePatternConverter"/>.
	/// </para>
	/// </remarks>
	/// <author>Nicko Cadell</author>
	internal class DatePatternConverter : PatternConverter, IOptionHandler
	{
		/// <summary>
		/// The <see cref="IDateFormatter"/> used to render the date to a string
		/// </summary>
		/// <remarks>
		/// <para>
		/// The <see cref="IDateFormatter"/> used to render the date to a string
		/// </para>
		/// </remarks>
		protected IDateFormatter m_dateFormatter;
	
		#region Implementation of IOptionHandler

		/// <summary>
		/// Initialize the converter options
		/// </summary>
		/// <remarks>
		/// <para>
		/// This is part of the <see cref="IOptionHandler"/> delayed object
		/// activation scheme. The <see cref="ActivateOptions"/> method must 
		/// be called on this object after the configuration properties have
		/// been set. Until <see cref="ActivateOptions"/> is called this
		/// object is in an undefined state and must not be used. 
		/// </para>
		/// <para>
		/// If any of the configuration properties are modified then 
		/// <see cref="ActivateOptions"/> must be called again.
		/// </para>
		/// </remarks>
		public void ActivateOptions()
		{
			string dateFormatStr = Option;
			if (dateFormatStr == null)
			{
				dateFormatStr = AbsoluteTimeDateFormatter.Iso8601TimeDateFormat;
			}
			
			if (string.Compare(dateFormatStr, AbsoluteTimeDateFormatter.Iso8601TimeDateFormat, true, System.Globalization.CultureInfo.InvariantCulture) == 0) 
			{
				m_dateFormatter = new Iso8601DateFormatter();
			}
			else if (string.Compare(dateFormatStr, AbsoluteTimeDateFormatter.AbsoluteTimeDateFormat, true, System.Globalization.CultureInfo.InvariantCulture) == 0)
			{
				m_dateFormatter = new AbsoluteTimeDateFormatter();
			}
			else if (string.Compare(dateFormatStr, AbsoluteTimeDateFormatter.DateAndTimeDateFormat, true, System.Globalization.CultureInfo.InvariantCulture) == 0)
			{
				m_dateFormatter = new DateTimeDateFormatter();
			}
			else 
			{
				try 
				{
					m_dateFormatter = new SimpleDateFormatter(dateFormatStr);
				}
				catch (Exception e) 
				{
					LogLog.Error(declaringType, "Could not instantiate SimpleDateFormatter with ["+dateFormatStr+"]", e);
					m_dateFormatter = new Iso8601DateFormatter();
				}	
			}
		}

		#endregion

		/// <summary>
		/// Write the current date to the output
		/// </summary>
		/// <param name="writer"><see cref="TextWriter" /> that will receive the formatted result.</param>
		/// <param name="state">null, state is not set</param>
		/// <remarks>
		/// <para>
		/// Pass the current date and time to the <see cref="IDateFormatter"/>
		/// for it to render it to the writer.
		/// </para>
		/// <para>
		/// The date and time passed is in the local time zone.
		/// </para>
		/// </remarks>
		override protected void Convert(TextWriter writer, object state) 
		{
			try 
			{
				m_dateFormatter.FormatDate(DateTime.Now, writer);
			}
			catch (Exception ex) 
			{
				LogLog.Error(declaringType, "Error occurred while converting date.", ex);
			}
		}

	    #region Private Static Fields

	    /// <summary>
	    /// The fully qualified type of the DatePatternConverter class.
	    /// </summary>
	    /// <remarks>
	    /// Used by the internal logger to record the Type of the
	    /// log message.
	    /// </remarks>
	    private readonly static Type declaringType = typeof(DatePatternConverter);

	    #endregion Private Static Fields
	}
}