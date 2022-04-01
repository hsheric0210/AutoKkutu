// Copyright (c) 2014 The Chromium Embedded Framework Authors. All rights
// reserved. Use of this source code is governed by a BSD-style license that
// can be found in the LICENSE file.

package com.eric0210.autokkutu;

import java.awt.BorderLayout;
import java.awt.Component;
import java.awt.event.WindowAdapter;
import java.awt.event.WindowEvent;

import javax.swing.JFrame;
import javax.swing.JTextField;

import org.cef.CefApp;
import org.cef.CefApp.CefAppState;
import org.cef.CefClient;
import org.cef.CefSettings;
import org.cef.OS;
import org.cef.browser.CefBrowser;
import org.cef.browser.CefFrame;
import org.cef.browser.CefMessageRouter;
import org.cef.browser.CefMessageRouter.CefMessageRouterConfig;
import org.cef.callback.CefQueryCallback;
import org.cef.handler.CefAppHandlerAdapter;
import org.cef.handler.CefLoadHandler;
import org.cef.handler.CefMessageRouterHandler;
import org.cef.handler.CefMessageRouterHandlerAdapter;
import org.cef.network.CefRequest;
import org.cef.network.CefRequest.TransitionType;

/**
 * This is a simple example application using JCEF.
 * It displays a JFrame with a JTextField at its top and a CefBrowser in its
 * center. The JTextField is used to enter and assign an URL to the browser UI.
 * No additional handlers or callbacks are used in this example.
 * <p>
 * The number of used JCEF classes is reduced (nearly) to its minimum and should
 * assist you to get familiar with JCEF.
 * <p>
 * For a more feature complete example have also a look onto the example code
 * within the package "example.detailed".
 */
public class Main extends JFrame
{
	private static final long serialVersionUID = -5570653778104813836L;
	private final JTextField address_;
	private final CefApp cefApp_;
	private final CefClient client_;
	final CefBrowser browser_;
	private final Component browerUI_;

	/**
	 * To display a simple browser window, it suffices completely to create an
	 * instance of the class CefBrowser and to assign its UI component to your
	 * application (e.g. to your content pane).
	 * But to be more verbose, this CTOR keeps an instance of each object on the
	 * way to the browser UI.
	 */
	private Main(final String startURL, final boolean useOSR, final boolean isTransparent)
	{
		// (1) The entry point to JCEF is always the class CefApp. There is only one
		//     instance per application and therefore you have to call the method
		//     "getInstance()" instead of a CTOR.
		//
		//     CefApp is responsible for the global CEF context. It loads all
		//     required native libraries, initializes CEF accordingly, starts a
		//     background task to handle CEF's message loop and takes care of
		//     shutting down CEF after disposing it.
		CefApp.addAppHandler(new CefAppHandlerAdapter(null)
		{
			@Override
			public void stateHasChanged(final CefAppState state)
			{
				// Shutdown the app if the native CEF part is terminated
				if (state == CefAppState.TERMINATED)
				{
					// calling System.exit(0) appears to be causing assert errors,
					// as its firing before all of the CEF objects shutdown.
					//System.exit(0);
				}
			}
		});
		final CefSettings settings = new CefSettings();
		settings.windowless_rendering_enabled = useOSR;
		cefApp_ = CefApp.getInstance(settings);

		// (2) JCEF can handle one to many browser instances simultaneous. These
		//     browser instances are logically grouped together by an instance of
		//     the class CefClient. In your application you can create one to many
		//     instances of CefClient with one to many CefBrowser instances per
		//     client. To get an instance of CefClient you have to use the method
		//     "createClient()" of your CefApp instance. Calling an CTOR of
		//     CefClient is not supported.
		//
		//     CefClient is a connector to all possible events which come from the
		//     CefBrowser instances. Those events could be simple things like the
		//     change of the browser title or more complex ones like context menu
		//     events. By assigning handlers to CefClient you can control the
		//     behavior of the browser. See example.detailed.SimpleFrameExample for an example
		//     of how to use these handlers.
		client_ = cefApp_.createClient();

		// (3) One CefBrowser instance is responsible to control what you'll see on
		//     the UI component of the instance. It can be displayed off-screen
		//     rendered or windowed rendered. To get an instance of CefBrowser you
		//     have to call the method "createBrowser()" of your CefClient
		//     instances.
		//
		//     CefBrowser has methods like "goBack()", "goForward()", "loadURL()",
		//     and many more which are used to control the behavior of the displayed
		//     content. The UI is held within a UI-Compontent which can be accessed
		//     by calling the method "getUIComponent()" on the instance of CefBrowser.
		//     The UI component is inherited from a java.awt.Component and therefore
		//     it can be embedded into any AWT UI.
		browser_ = client_.createBrowser(startURL, useOSR, isTransparent);
		browerUI_ = browser_.getUIComponent();

		// (4) For this minimal browser, we need only a text field to enter an URL
		//     we want to navigate to and a CefBrowser window to display the content
		//     of the URL. To respond to the input of the user, we're registering an
		//     anonymous ActionListener. This listener is performed each time the
		//     user presses the "ENTER" key within the address field.
		//     If this happens, the entered value is passed to the CefBrowser
		//     instance to be loaded as URL.
		address_ = new JTextField(startURL, 100);
		address_.addActionListener(e -> browser_.loadURL(address_.getText()));

		// (5) All UI components are assigned to the default content pane of this
		//     JFrame and afterwards the frame is made visible to the user.
		getContentPane().add(address_, BorderLayout.PAGE_START);
		getContentPane().add(browerUI_, BorderLayout.CENTER);

		class CustomCefMessageRouterHandler extends CefMessageRouterHandlerAdapter
		{
			@Override
			public boolean onQuery(final CefBrowser browser, final CefFrame frame, final long queryId, final String request, final boolean persistent, final CefQueryCallback callback)
			{
				if (request.startsWith("display:"))
				{
					System.out.println("Display text change: " + request.substring(8).trim());
					return true;
				}
				return false;
			}

			@Override
			public void onQueryCanceled(final CefBrowser browser, final CefFrame frame, final long queryId)
			{

			}
		}

		final CefMessageRouter msgRouter = CefMessageRouter.create(new CefMessageRouterConfig("displayUpdate", "displayUpdateCancel"), new CustomCefMessageRouterHandler());
		client_.addMessageRouter(msgRouter);

		client_.addLoadHandler(new CefLoadHandler()
		{
			@Override
			public void onLoadingStateChange(final CefBrowser browser, final boolean isLoading, final boolean canGoBack, final boolean canGoForward)
			{

			}

			@Override
			public void onLoadStart(final CefBrowser browser, final CefFrame frame, final TransitionType transitionType)
			{

			}

			@Override
			public void onLoadEnd(final CefBrowser browser, final CefFrame frame, final int httpStatusCode)
			{
				browser_.executeJavaScript("function queryCurrentText(){window.displayUpdate({request: 'display:'+$('.jjo-display').text()})}", browser_.getURL(), 0);
			}

			@Override
			public void onLoadError(final CefBrowser browser, final CefFrame frame, final ErrorCode errorCode, final String errorText, final String failedUrl)
			{

			}
		});

		new Thread(() ->
		{
			while(true)
			{
				browser_.executeJavaScript("queryCurrentText()", browser_.getURL(), 0);
				try
				{
					Thread.sleep(200);
				}
				catch (InterruptedException e)
				{
					e.printStackTrace();
				}
			}
		}).start();

		pack();
		setSize(800, 600);
		setVisible(true);

		final JFrame devtool = new JFrame();
		devtool.getContentPane().add(browser_.getDevTools().getUIComponent());
		devtool.pack();
		// devtool.setSize(1000, 1000);
		// devtool.repaint();
		devtool.setVisible(true);

		// (6) To take care of shutting down CEF accordingly, it's important to call
		//     the method "dispose()" of the CefApp instance if the Java
		//     application will be closed. Otherwise you'll get asserts from CEF.
		addWindowListener(new WindowAdapter()
		{
			@Override
			public void windowClosing(final WindowEvent e)
			{
				CefApp.getInstance().dispose();
				dispose();
			}
		});
	}

	public static void main(final String[] args)
	{
		// The simple example application is created as anonymous class and points
		// to Google as the very first loaded page. If this example is used on
		// Linux, it's important to use OSR mode because windowed rendering is not
		// supported yet. On Macintosh and Windows windowed rendering is used as
		// default. If you want to test OSR mode on those platforms, simply replace
		// "OS.isLinux()" with "true" and recompile.
		new Main("https://kkutu.org/", OS.isLinux(), false);
	}
}
