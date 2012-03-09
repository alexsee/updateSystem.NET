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
namespace updateSystemDotNet.Administration.Core.Publishing {
	internal sealed class publishResult {

		/// <summary>Gibt den Fehler zurück der beim Publishing aufgetreten ist. Nur wenn !<see cref="Successful"/></summary>
		public Exception Error { get; set; }

		/// <summary>Gibt True zurück wenn der Publishvorgang erfolgreich ausgeführt wurde, andernfalls False.</summary>
		public bool Successful { get { return Error == null; }}

		/// <summary>Gibt den Provider zurück, zu welchem dieses Result gehört.</summary>
		public IPublishProvider Provider { get; set; }

	}
}
