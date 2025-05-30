﻿using System;

namespace Cameca.CustomAnalysis.PythonCore;

[Obsolete("Use new version of this control now found in Cameca.CustomAnalysis.Utilities. This version will no longer be updated.")]
public enum AppendScrollBehavior
{
	/// <summary>
	/// Do not customize scroll behavior on appending text
	/// </summary>
	Default = 0,
	/// <summary>
	/// Always scroll to bottom on appending text
	/// </summary>
	AlwaysToBottom,
	/// <summary>
	/// Scroll to bottom on appending text if already at bottom, else do not modify scroll behavior.
	/// Only keeps scrolling to bottom if already was at the bottom.
	/// </summary>
	FollowBottom,
}
