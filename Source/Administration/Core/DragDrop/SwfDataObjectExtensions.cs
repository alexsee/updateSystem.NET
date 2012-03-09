﻿/**
 * updateSystem.NET
 * Copyright (c) 2008-2012 Maximilian Krauss <http://kraussz.com> eMail: max@kraussz.com
 *
 * This library is licened under The Code Project Open License (CPOL) 1.02
 * which can be found online at <http://www.codeproject.com/info/cpol10.aspx>
 * 
 * THIS WORK IS PROVIDED "AS IS", "WHERE IS" AND "AS AVAILABLE", WITHOUT
 * ANY EXPRESS OR IMPLIED WARRANTIES OR CONDITIONS OR GUARANTEES. YOU,
 * THE USER, ASSUME ALL RISK IN ITS USE, INCLUDING COPYRIGHT INFRINGEMENT,
 * PATENT INFRINGEMENT, SUITABILITY, ETC. AUTHOR EXPRESSLY DISCLAIMS ALL
 * EXPRESS, IMPLIED OR STATUTORY WARRANTIES OR CONDITIONS, INCLUDING
 * WITHOUT LIMITATION, WARRANTIES OR CONDITIONS OF MERCHANTABILITY,
 * MERCHANTABLE QUALITY OR FITNESS FOR A PARTICULAR PURPOSE, OR ANY
 * WARRANTY OF TITLE OR NON-INFRINGEMENT, OR THAT THE WORK (OR ANY
 * PORTION THEREOF) IS CORRECT, USEFUL, BUG-FREE OR FREE OF VIRUSES.
 * YOU MUST PASS THIS DISCLAIMER ON WHENEVER YOU DISTRIBUTE THE WORK OR
 * DERIVATIVE WORKS.
 */
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Runtime.Serialization;
using System.Windows.Forms;
using IDataObject = System.Runtime.InteropServices.ComTypes.IDataObject;

namespace updateSystemDotNet.Administration.Core.DragDrop {
	using ComIDataObject = IDataObject;

	public enum DropImageType {
		Invalid = -1,
		None = 0,
		Copy = DragDropEffects.Copy,
		Move = DragDropEffects.Move,
		Link = DragDropEffects.Link,
		Label = 6,
		Warning = 7
	}

	/// <summary>
	/// Provides extended functionality to the System.Windows.Forms.IDataObject interface.
	/// </summary>
	public static class SwfDataObjectExtensions {
		#region DLL imports

		[DllImport("gdiplus.dll")]
		private static extern bool DeleteObject(IntPtr hgdi);

		[DllImport("ole32.dll")]
		private static extern void ReleaseStgMedium(ref STGMEDIUM pmedium);

		#endregion // DLL imports

		/// <summary>
		/// Sets the drag image as the rendering of a control.
		/// </summary>
		/// <param name="dataObject">The DataObject to set the drag image on.</param>
		/// <param name="control">The Control to render as the drag image.</param>
		/// <param name="cursorOffset">The location of the cursor relative to the control.</param>
		public static void SetDragImage(this System.Windows.Forms.IDataObject dataObject, Control control, Point cursorOffset) {
			int width = control.Width;
			int height = control.Height;

			var bmp = new Bitmap(width, height);
			control.DrawToBitmap(bmp, new Rectangle(0, 0, width, height));

			SetDragImage(dataObject, bmp, cursorOffset);
		}

		/// <summary>
		/// Sets the drag image.
		/// </summary>
		/// <param name="dataObject">The DataObject to set the drag image on.</param>
		/// <param name="image">The drag image.</param>
		/// <param name="cursorOffset">The location of the cursor relative to the image.</param>
		public static void SetDragImage(this System.Windows.Forms.IDataObject dataObject, Image image, Point cursorOffset) {
			var shdi = new ShDragImage();

			Win32Size size;
			size.cx = image.Width;
			size.cy = image.Height;
			shdi.sizeDragImage = size;

			Win32Point wpt;
			wpt.x = cursorOffset.X;
			wpt.y = cursorOffset.Y;
			shdi.ptOffset = wpt;

			shdi.crColorKey = Color.Magenta.ToArgb();

			// This HBITMAP will be managed by the DragDropHelper
			// as soon as we pass it to InitializeFromBitmap. If we fail
			// to make the hand off, we'll delete it to prevent a mem leak.
			IntPtr hbmp = GetHbitmapFromImage(image);
			shdi.hbmpDragImage = hbmp;

			try {
				var sourceHelper = (IDragSourceHelper) new DragDropHelper();

				try {
					sourceHelper.InitializeFromBitmap(ref shdi, (ComIDataObject) dataObject);
				}
				catch (NotImplementedException ex) {
					throw new Exception(
						"A NotImplementedException was caught. This could be because you forgot to construct your DataObject using a DragDropLib.DataObject",
						ex);
				}
			}
			catch {
				DeleteObject(hbmp);
			}
		}

		/// <summary>
		/// Gets an HBITMAP from any image.
		/// </summary>
		/// <param name="image">The image to get an HBITMAP from.</param>
		/// <returns>An HBITMAP pointer.</returns>
		/// <remarks>
		/// The caller is responsible to call DeleteObject on the HBITMAP.
		/// </remarks>
		private static IntPtr GetHbitmapFromImage(Image image) {
			if (image is Bitmap) {
				return ((Bitmap) image).GetHbitmap();
			}
			else {
				var bmp = new Bitmap(image);
				return bmp.GetHbitmap();
			}
		}

		/// <summary>
		/// Sets the drop description for the drag image manager.
		/// </summary>
		/// <param name="dataObject">The DataObject to set.</param>
		/// <param name="type">The type of the drop image.</param>
		/// <param name="format">The format string for the description.</param>
		/// <param name="insert">The parameter for the drop description.</param>
		/// <remarks>
		/// When setting the drop description, the text can be set in two part,
		/// which will be rendered slightly differently to distinguish the description
		/// from the subject. For example, the format can be set as "Move to %1" and
		/// the insert as "Temp". When rendered, the "%1" in format will be replaced
		/// with "Temp", but "Temp" will be rendered slightly different from "Move to ".
		/// </remarks>
		public static void SetDropDescription(this System.Windows.Forms.IDataObject dataObject, DropImageType type,
		                                      string format, string insert) {
			if (format != null && format.Length > 259)
				throw new ArgumentException("Format string exceeds the maximum allowed length of 259.", "format");
			if (insert != null && insert.Length > 259)
				throw new ArgumentException("Insert string exceeds the maximum allowed length of 259.", "insert");

			// Fill the structure
			DropDescription dd;
			dd.type = (int) type;
			dd.szMessage = format;
			dd.szInsert = insert;

			((IDataObject) dataObject).SetDropDescription(dd);
		}

		public static void DisableDropDescription(this System.Windows.Forms.IDataObject dataObject) {
			DropDescription description;
			description.type = -1;
			description.szMessage = null;
			description.szInsert = null;
			((IDataObject) dataObject).SetDropDescription(description);
		}

		/// <summary>
		/// Sets managed data to a clipboard DataObject.
		/// </summary>
		/// <param name="dataObject">The DataObject to set the data on.</param>
		/// <param name="format">The clipboard format.</param>
		/// <param name="data">The data object.</param>
		/// <remarks>
		/// Because the underlying data store is not storing managed objects, but
		/// unmanaged ones, this function provides intelligent conversion, allowing
		/// you to set unmanaged data into the COM implemented IDataObject.</remarks>
		public static void SetDataEx(this System.Windows.Forms.IDataObject dataObject, string format, object data) {
			DataFormats.Format dataFormat = DataFormats.GetFormat(format);

			// Initialize the format structure
			var formatETC = new FORMATETC();
			formatETC.cfFormat = (short) dataFormat.Id;
			formatETC.dwAspect = DVASPECT.DVASPECT_CONTENT;
			formatETC.lindex = -1;
			formatETC.ptd = IntPtr.Zero;

			// Try to discover the TYMED from the format and data
			TYMED tymed = GetCompatibleTymed(format, data);
			// If a TYMED was found, we can use the system DataObject
			// to convert our value for us.
			if (tymed != TYMED.TYMED_NULL) {
				formatETC.tymed = tymed;

				// Set data on an empty DataObject instance
				var conv = new System.Windows.Forms.DataObject();
				conv.SetData(format, true, data);

				// Now retrieve the data, using the COM interface.
				// This will perform a managed to unmanaged conversion for us.
				STGMEDIUM medium;
				((IDataObject) conv).GetData(ref formatETC, out medium);
				try {
					// Now set the data on our data object
					((IDataObject) dataObject).SetData(ref formatETC, ref medium, true);
				}
				catch {
					// On exceptions, release the medium
					ReleaseStgMedium(ref medium);
					throw;
				}
			}
			else {
				// Since we couldn't determine a TYMED, this data
				// is likely custom managed data, and won't be used
				// by unmanaged code, so we'll use our custom marshaling
				// implemented by our COM IDataObject extensions.

				((IDataObject) dataObject).SetManagedData(format, data);
			}
		}

		/// <summary>
		/// Gets a system compatible TYMED for the given format.
		/// </summary>
		/// <param name="format">The data format.</param>
		/// <param name="data">The data.</param>
		/// <returns>A TYMED value, indicating a system compatible TYMED that can
		/// be used for data marshaling.</returns>
		private static TYMED GetCompatibleTymed(string format, object data) {
			if (IsFormatEqual(format, DataFormats.Bitmap) && data is Bitmap)
				return TYMED.TYMED_GDI;
			if (IsFormatEqual(format, DataFormats.EnhancedMetafile))
				return TYMED.TYMED_ENHMF;
			if (data is Stream
			    || IsFormatEqual(format, DataFormats.Html)
			    || IsFormatEqual(format, DataFormats.Text) || IsFormatEqual(format, DataFormats.Rtf)
			    || IsFormatEqual(format, DataFormats.OemText)
			    || IsFormatEqual(format, DataFormats.UnicodeText) || IsFormatEqual(format, "ApplicationTrust")
			    || IsFormatEqual(format, DataFormats.FileDrop)
			    || IsFormatEqual(format, "FileName")
			    || IsFormatEqual(format, "FileNameW"))
				return TYMED.TYMED_HGLOBAL;
			if (IsFormatEqual(format, DataFormats.Dib) && data is Image)
				return TYMED.TYMED_NULL;
			if (IsFormatEqual(format, typeof (Bitmap).FullName))
				return TYMED.TYMED_HGLOBAL;
			if (IsFormatEqual(format, DataFormats.EnhancedMetafile) || data is Metafile)
				return TYMED.TYMED_NULL;
			if (IsFormatEqual(format, DataFormats.Serializable) || (data is ISerializable)
			    || ((data != null) && data.GetType().IsSerializable))
				return TYMED.TYMED_HGLOBAL;

			return TYMED.TYMED_NULL;
		}

		/// <summary>
		/// Compares the equality of two clipboard formats.
		/// </summary>
		/// <param name="formatA">First format.</param>
		/// <param name="formatB">Second format.</param>
		/// <returns>True if the formats are equal. False otherwise.</returns>
		private static bool IsFormatEqual(string formatA, string formatB) {
			return string.CompareOrdinal(formatA, formatB) == 0;
		}

		/// <summary>
		/// Gets managed data from a clipboard DataObject.
		/// </summary>
		/// <param name="dataObject">The DataObject to obtain the data from.</param>
		/// <param name="format">The format for which to get the data in.</param>
		/// <returns>The data object instance.</returns>
		public static object GetDataEx(this System.Windows.Forms.IDataObject dataObject, string format) {
			// Get the data
			object data = dataObject.GetData(format, true);

			// If the data is a stream, we'll check to see if it
			// is stamped by us for custom marshaling
			if (data is Stream) {
				object data2 = ((IDataObject) dataObject).GetManagedData(format);
				if (data2 != null)
					return data2;
			}

			return data;
		}
	}
}