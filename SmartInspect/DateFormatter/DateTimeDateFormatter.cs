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
using System.Globalization;

namespace SmartInspect.DateFormatter
{
	/// <summary>
	/// Formats a <see cref="DateTime"/> as <c>"dd MMM yyyy HH:mm:ss,fff"</c>
	/// </summary>
	/// <remarks>
	/// <para>
	/// Formats a <see cref="DateTime"/> in the format 
	/// <c>"dd MMM yyyy HH:mm:ss,fff"</c> for example, 
	/// <c>"06 Nov 1994 15:49:37,459"</c>.
	/// </para>
	/// </remarks>
	/// <author>Nicko Cadell</author>
	/// <author>Gert Driesen</author>
	/// <author>Angelika Schnagl</author>
	public class DateTimeDateFormatter : AbsoluteTimeDateFormatter
	{
		#region Public Instance Constructors

		/// <summary>
		/// Default constructor.
		/// </summary>
		/// <remarks>
		/// <para>
		/// Initializes a new instance of the <see cref="DateTimeDateFormatter" /> class.
		/// </para>
		/// </remarks>
		public DateTimeDateFormatter()
		{
			m_dateTimeFormatInfo = DateTimeFormatInfo.InvariantInfo;
		}

		#endregion Public Instance Constructors

		#region Override implementation of AbsoluteTimeDateFormatter

		/// <summary>
		/// Formats the date without the milliseconds part
		/// </summary>
		/// <param name="dateToFormat">The date to format.</param>
		/// <param name="buffer">The string builder to write to.</param>
		/// <remarks>
		/// <para>
		/// Formats a DateTime in the format <c>"dd MMM yyyy HH:mm:ss"</c>
		/// for example, <c>"06 Nov 1994 15:49:37"</c>.
		/// </para>
		/// <para>
		/// The base class will append the <c>",fff"</c> milliseconds section.
		/// This method will only be called at most once per second.
		/// </para>
		/// </remarks>
		override protected void FormatDateWithoutMillis(DateTime dateToFormat, StringBuilder buffer)
		{
			int day = dateToFormat.Day;
			if (day < 10) 
			{
				buffer.Append('0');
			}
			buffer.Append(day);
			buffer.Append(' ');		

			buffer.Append(m_dateTimeFormatInfo.GetAbbreviatedMonthName(dateToFormat.Month));
			buffer.Append(' ');	

			buffer.Append(dateToFormat.Year);
			buffer.Append(' ');

			// Append the 'HH:mm:ss'
			base.FormatDateWithoutMillis(dateToFormat, buffer);
		}

		#endregion Override implementation of AbsoluteTimeDateFormatter

		#region Private Instance Fields

		/// <summary>
		/// The format info for the invariant culture.
		/// </summary>
		private readonly DateTimeFormatInfo m_dateTimeFormatInfo;

		#endregion Private Instance Fields
	}
}