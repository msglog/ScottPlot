﻿using Microsoft.UI.Xaml.Controls;
using ScottPlot.Control;
using System;
using System.Collections.Generic;
using System.IO;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;

namespace ScottPlot.WinUI;

public class Menu
{
    readonly WinUIPlot ThisControl;

    public Menu(WinUIPlot thisControl)
    {
        ThisControl = thisControl;
    }

    public ContextMenuItem[] GetDefaultContextMenuItems()
    {
        ContextMenuItem saveImage = new()
        {
            Label = "Save Image",
            OnInvoke = OpenSaveImageDialog,
        };

        ContextMenuItem copyImage = new()
        {
            Label = "Copy to Clipboard",
            OnInvoke = CopyImageToClipboard,
        };

        return new ContextMenuItem[] { saveImage, copyImage };
    }

    public MenuFlyout GetContextMenu(IPlotControl plotControl)
    {
        MenuFlyout menu = new();

        foreach (var curr in plotControl.Interaction.ContextMenuItems)
        {
            var menuItem = new MenuFlyoutItem { Text = curr.Label };
            menuItem.Click += (s, e) => curr.OnInvoke(plotControl);

            menu.Items.Add(menuItem);
        }

        return menu;
    }

    public async void OpenSaveImageDialog(IPlotControl plotControl)
    {
        FileSavePicker dialog = new()
        {
            SuggestedFileName = plotControl.Interaction.DefaultSaveImageFilename
        };
        dialog.FileTypeChoices.Add("PNG Files", new List<string>() { ".png" });
        dialog.FileTypeChoices.Add("JPEG Files", new List<string>() { ".jpg", ".jpeg" });
        dialog.FileTypeChoices.Add("BMP Files", new List<string>() { ".bmp" });
        dialog.FileTypeChoices.Add("WebP Files", new List<string>() { ".webp" });
        dialog.FileTypeChoices.Add("SVG Files", new List<string>() { ".svg" });

#if NET6_0_WINDOWS10_0_18362 // https://github.com/microsoft/CsWinRT/blob/master/docs/interop.md#windows-sdk
        // TODO: launch a pop-up window or otherwise inform if AppWindow is not set before using save-dialog
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(ThisControl.AppWindow);
        WinRT.Interop.InitializeWithWindow.Initialize(dialog, hwnd);
#endif

        var file = await dialog.PickSaveFileAsync();

        if (file != null)
        {
            // TODO: launch a pop-up window indicating if extension is invalid or save failed
            ImageFormat format = ImageFormatLookup.FromFilePath(file.Name);
            PixelSize lastRenderSize = plotControl.Plot.RenderManager.LastRender.FigureRect.Size;
            plotControl.Plot.Save(file.Path, (int)lastRenderSize.Width, (int)lastRenderSize.Height, format);
        }
    }

    public void CopyImageToClipboard(IPlotControl plotControl)
    {
        PixelSize lastRenderSize = plotControl.Plot.RenderManager.LastRender.FigureRect.Size;
        byte[] bytes = plotControl.Plot.GetImage((int)lastRenderSize.Width, (int)lastRenderSize.Height).GetImageBytes();

        var stream = new InMemoryRandomAccessStream();
        stream.AsStreamForWrite().Write(bytes);

        var content = new DataPackage();
        content.SetBitmap(RandomAccessStreamReference.CreateFromStream(stream));

        Clipboard.SetContent(content);
    }

}