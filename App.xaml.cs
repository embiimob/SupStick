// SupStick - Open Source Decentralized Media Player
// Copyright (c) 2026 SupStick Contributors
// Licensed under the MIT License - see LICENSE file for details
// Project: https://github.com/embiimob/SupStick

namespace SupStick;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		return new Window(new AppShell());
	}
}
