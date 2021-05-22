// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.TeamsBot
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Threading;
    using Gdk;
    using Gtk;
    using Microsoft.Psi.Audio;
    using Microsoft.Psi.Data;
    using Microsoft.Psi.Imaging;
    using PsiEncodedImage = Microsoft.Psi.Imaging.EncodedImage;
    using PsiImage = Microsoft.Psi.Imaging.Image;

    /// <summary>
    /// Teams Bot test runner.
    /// </summary>
    public class Program
    {
        private static ITeamsBot CreateTeamsBot(Pipeline pipeline)
        {
            // create your Teams bot \psi component
            // return new ParticipantEngagementScaleBot(pipeline, TimeSpan.FromSeconds(1.0 / 15.0), 1920, 1080, true);
            return new ParticipantEngagementBallBot(pipeline, TimeSpan.FromSeconds(1.0 / 15.0), 1920, 1080);
        }

        private static PsiImporter OpenStore(Pipeline pipeline)
        {
            // open your recorded bot data store
            return PsiStore.Open(pipeline, "<insert_store_name>", @"<insert_store_path>");
        }

        [STAThread]
        private static void Main()
        {
            Application.Init();
            var bld = new Builder();
            bld.AddFromString(
                "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
                "<interface>" +
                    "<requires lib=\"gtk+\" version=\"3.12\"/>" +
                    "<object class=\"GtkWindow\" id=\"window\">" +
                        "<child>" +
                            "<object class=\"GtkImage\" id=\"image\">" +
                                "<property name=\"width_request\">1080</property>" +
                                "<property name=\"height_request\">768</property>" +
                                "<property name=\"visible\">True</property>" +
                                "<property name=\"can_focus\">False</property>" +
                                "<property name=\"stock\">gtk-missing-image</property>" +
                            "</object>" +
                        "</child>" +
                    "</object>" +
                "</interface>");
            var win = (Gtk.Window)bld.GetObject("window");
            win.DeleteEvent += (_, __) => Application.Quit();
            var image = (Gtk.Image)bld.GetObject("image");
            win.Show();

            new Thread(new ThreadStart(() =>
            {
                using (var pipeline = Pipeline.Create())
                {
                    var store = OpenStore(pipeline);

                    var meta = store.GetMetadata("ParticipantVideo");
                    var streamType = Type.GetType(meta.TypeName, false);
                    bool isDictionaryOfEncodedImageStream = streamType == typeof(Dictionary<string, Shared<PsiEncodedImage>>);
                    bool isDictionaryOfImageStream = streamType == typeof(Dictionary<string, Shared<PsiImage>>);
                    var audio = store.OpenStream<Dictionary<string, AudioBuffer>>("ParticipantAudio");

                    IProducer<Dictionary<string, Shared<PsiImage>>> video;
                    if (isDictionaryOfImageStream)
                    {
                        // unencoded participant streams
                        video = store.OpenStream<Dictionary<string, Shared<PsiImage>>>("ParticipantVideo");
                    }
                    else if (isDictionaryOfEncodedImageStream)
                    {
                        // encoded participant streams (decode them)
                        video =
                            store
                                .OpenStream<Dictionary<string, Shared<PsiEncodedImage>>>("ParticipantVideo")
                                .Select(dict => dict.Select(kv =>
                                {
                                    var img = kv.Value.Resource;
                                    var shared = ImagePool.GetOrCreate(img.Width, img.Height, PixelFormat.BGR_24bpp);
                                    shared.Resource.DecodeFrom(img, new ImageFromStreamDecoder());
                                    return (kv.Key, shared);
                                }).ToDictionary(x => x.Key, x => x.shared));
                    }
                    else
                    {
                        throw new ArgumentException("ParticipantVideo stream unsupported.");
                    }

                    video.Do(_ => Console.Write('.'));

                    var teamsBot = CreateTeamsBot(pipeline);
                    audio.PipeTo(teamsBot.AudioIn);
                    video.PipeTo(teamsBot.VideoIn);

                    teamsBot.ScreenShareOut.Resize(960, 540).Do(frame =>
                    {
                        var data = new byte[frame.Resource.Size];
                        Marshal.Copy(frame.Resource.ImageData, data, 0, frame.Resource.Size);

                        // swap R <-> G bytes for Gtk rendering
                        for (int i = 0; i < data.Length; i += 4)
                        {
                            var r = data[i];
                            data[i] = data[i + 2];
                            data[i + 2] = r;
                        }

                        var buf = new Pixbuf(data, true, 8, frame.Resource.Width, frame.Resource.Height, frame.Resource.Stride);
                        image.Pixbuf = buf;
                    });

                    pipeline.RunAsync();

                    Console.WriteLine("Press any key and close main window to exit...");
                    Console.ReadKey();
                    Console.WriteLine("Done");
                }
            })) { IsBackground = true }.Start();

            Application.Run();
            bld.Dispose();
        }
    }
}
