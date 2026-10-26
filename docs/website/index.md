---
title: Thoughtfall
description: A small, free app for catching passing thoughts without losing your place. Write a few words, save them, and carry on.
tagline: A place for the thought you don't want to lose.
intro: A sentence, a question, something you noticed. Open a little window, write it down, and carry on with whatever you were doing.
downloadUrl: https://github.com/rattus-ratgirl/Thoughtfall/releases/download/v0.1.4/Thoughtfall-0.1.4-win-x64-setup.exe
releaseUrl: https://github.com/rattus-ratgirl/Thoughtfall/releases/tag/v0.1.4
version: 0.1.4
---

## Your next thought gets a blank page.

Sometimes you just want to get something down before it disappears. An idea for later. A bit of your day. A question you haven't worked out how to ask yet. It doesn't have to be important, or finished, or even particularly sensible.

Thoughtfall opens to an empty writing window. Your earlier entries stay out of sight, so a new thought doesn't turn into half an hour of rereading old ones. There's no title to come up with, no folder to choose, and nothing to organise before you can write.

## A few seconds, then back to it.

Thoughtfall starts quietly in the tray, ready for your next thought. On Windows, the capture shortcut works as soon as the app is running. On Linux and desktops without usable tray support, the writing window opens on launch instead.

1. **Bring it up.** With Thoughtfall running on Windows, press **Ctrl + Alt + J** from whatever you're doing. You can also choose **Capture a thought** from its icon near the clock.
2. **Write what you want to keep.** A few words or a few paragraphs. Press Enter for a new line, just as you'd expect.
3. **Save and carry on.** Click **Save** or press **Ctrl + Enter**. Your words are saved with the date and time, and the window gets out of the way.

Need to stop halfway through? Escape or closing the window puts the draft away until you open it again. Unsaved drafts are only kept while the app is running, so save anything you want to keep before quitting.

Choose **Close** to tuck the window away in the tray and leave Thoughtfall running. Choose **Quit** from the icon's menu when you're finished. On Linux and desktops without usable tray support, closing the window minimises it, and **Quit** is available in the writing window.

The Windows shortcut can be changed or switched off through **Capture shortcut…** in the window or the icon's menu. Leave Thoughtfall running for it to work; the shortcut can't open an app you've fully quit. Thoughtfall doesn't start automatically when you sign in.

## Come back to it when you want to.

When you want to read through what you've collected, choose **Export entries…**. Pick the dates you want, choose a format, and save a copy wherever you like. Both dates are included, and exporting leaves the saved originals alone.

**Markdown** gives you a text document arranged by year, month, and day, which you can read in a text editor or open in many writing apps. **JSON** is available too, if you want to use the entries in other software.

## Make it comfortable.

Thoughtfall starts with **Hearth**, the warm brown and peach colours of this website, and follows your computer's light or dark setting. Choose **Appearance…** to switch to **Forest**, a quiet green, or set either palette to always use light or dark. Light and dark changes apply when you save; changing between Forest and Hearth takes effect next time you start Thoughtfall.

## Your writing stays on your computer.

There's no account to create or cloud sync to set up. Saved thoughts live in a local file on your computer. Use **Data folder** in the capture window, or **Open data folder** from the icon's menu, to find them.

For a backup, quit Thoughtfall first, then copy that folder somewhere safe. Thoughtfall doesn't encrypt your writing, so protect it as you would any other personal files.

## A little more before you download.

Thoughtfall is free and open source. The Windows installer includes everything it needs, so there's no separate software to install first. It's for 64-bit Windows, and it installs for your user account.

The installer isn't code-signed yet. Windows may show an unknown-publisher warning or block installation. You can find the installer and release notes on [GitHub Releases](https://github.com/rattus-ratgirl/Thoughtfall/releases).

## At home on Linux, too.

**Debian packages for x64 and ARM64** are available for **Ubuntu 24.04 and 26.04** and **Debian 12 and 13**. They include the .NET runtime. You'll need a graphical desktop with X11 or XWayland.

Download the [x64 package](https://github.com/rattus-ratgirl/Thoughtfall/releases/download/v0.1.4/Thoughtfall-0.1.4-linux-x64.deb) for an Intel or AMD computer, or the [ARM64 package](https://github.com/rattus-ratgirl/Thoughtfall/releases/download/v0.1.4/Thoughtfall-0.1.4-linux-arm64.deb) for an ARM64 computer. Install the downloaded package with your package manager. Thoughtfall appears in your application menu; choosing it opens the writing window, even if the app is already running. You can also run `thoughtfall --capture`, or use that command with a shortcut configured in your desktop's settings. There's no built-in Linux global shortcut yet.

Linux installation is system-wide. Upgrading or removing the package keeps your saved thoughts and settings. Download updates through [GitHub Releases](https://github.com/rattus-ratgirl/Thoughtfall/releases); there's no automatic updater.

You can also [build and run Thoughtfall from source](https://github.com/rattus-ratgirl/Thoughtfall#run).

It's still a young app. If you find a problem or have an idea, [tell me on GitHub](https://github.com/rattus-ratgirl/Thoughtfall/issues).
