﻿using System;
using AutoKkutu.Constants;

namespace AutoKkutu.Modules.AutoEntering;

public sealed record AutoEnterParameter(bool DelayEnabled, int DelayInMillis, bool DelayPerCharEnabled, bool SimulateInput, PathFinderParameter PathFinderParams, string Content = "", int WordIndex = 0)
{
	public int RealDelay => CalcRealDelay(Content);
	public int CalcRealDelay(string content) => DelayInMillis * (DelayPerCharEnabled ? (content ?? throw new ArgumentNullException(nameof(content))).Length : 1);

	public bool CanSimulateInput => DelayEnabled && DelayPerCharEnabled && SimulateInput;
}
