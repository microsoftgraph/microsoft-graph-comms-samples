// ***********************************************************************
// Assembly         : RecordingBot.Services
// Author           : JasonTheDeveloper
// Created          : 09-07-2020
//
// Last Modified By : dannygar
// Last Modified On : 09-07-2020
// ***********************************************************************
// <copyright file="AudioProcessor.cs" company="Microsoft">
//     Copyright ©  2020
// </copyright>
// <summary></summary>
// ***********************************************************************
using NAudio.Wave;
using RecordingBot.Model.Constants;
using RecordingBot.Services.Contract;
using RecordingBot.Services.ServiceSetup;
using RecordingBot.Services.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;

namespace RecordingBot.Services.Media
{
    /// <summary>
    /// Class AudioProcessor.
    /// Implements the <see cref="RecordingBot.Services.Util.BufferBase{RecordingBot.Services.Media.SerializableAudioMediaBuffer}" />
    /// </summary>
    /// <seealso cref="RecordingBot.Services.Util.BufferBase{RecordingBot.Services.Media.SerializableAudioMediaBuffer}" />
    public class AudioProcessor : BufferBase<SerializableAudioMediaBuffer>
    {
        /// <summary>
        /// The writers
        /// </summary>
        readonly Dictionary<string, WaveFileWriter> _writers = new Dictionary<string, WaveFileWriter>();

        /// <summary>
        /// The processor identifier
        /// </summary>
        private readonly string _processorId = null;

        /// <summary>
        /// The settings
        /// </summary>
        private readonly AzureSettings _settings;

        /// <summary>
        /// Initializes a new instance of the <see cref="AudioProcessor" /> class.
        /// </summary>
        /// <param name="settings">The settings.</param>
        public AudioProcessor(IAzureSettings settings)
        {
            _processorId = Guid.NewGuid().ToString();
            _settings = (AzureSettings)settings;
        }

        /// <summary>
        /// Processes the specified data.
        /// </summary>
        /// <param name="data">The data.</param>
        protected override async Task Process(SerializableAudioMediaBuffer data)
        {
            if (data.Timestamp == 0)
            {
                return;
            }

            var path = Path.Combine(Path.GetTempPath(), BotConstants.DefaultOutputFolder, _settings.MediaFolder, _processorId);

            // First, write all audio buffer, unless the data.IsSilence is checked for true, into the all speakers buffer
            var all = "all";
            var all_writer = _writers.ContainsKey(all) ? _writers[all] : InitialiseWavFileWriter(path, all);

            if (data.Buffer != null)
            {
                // Buffers are saved to disk even when there is silence.
                // If you do not want this to happen, check if data.IsSilence == true.
                await all_writer.WriteAsync(data.Buffer, 0, data.Buffer.Length).ConfigureAwait(false);
            }

            if (data.SerializableUnmixedAudioBuffers != null)
            {
                foreach (var s in data.SerializableUnmixedAudioBuffers)
                {
                    if (string.IsNullOrWhiteSpace(s.AdId) || string.IsNullOrWhiteSpace(s.DisplayName))
                    {
                        continue;
                    }

                    var id = s.AdId;

                    var writer = _writers.ContainsKey(id) ? _writers[id] : InitialiseWavFileWriter(path, id);

                    // Write audio buffer into the WAV file for individual speaker
                    await writer.WriteAsync(s.Buffer, 0, s.Buffer.Length).ConfigureAwait(false);

                    // Write audio buffer into the WAV file for all speakers
                    await all_writer.WriteAsync(s.Buffer, 0, s.Buffer.Length).ConfigureAwait(false);

                }
            }
        }

        /// <summary>
        /// Initialises the wav file writer.
        /// </summary>
        /// <param name="rootFolder">The root folder.</param>
        /// <param name="id">The identifier.</param>
        /// <returns>WavFileWriter.</returns>
        private WaveFileWriter InitialiseWavFileWriter(string rootFolder, string id)
        {
            var path = AudioFileUtils.CreateFilePath(rootFolder, $"{id}.wav");

            // Initialize the Wave Format using the default PCM 16bit 16K supported by Teams audio settings
            var writer = new WaveFileWriter(path, new WaveFormat(
                rate: AudioConstants.DefaultSampleRate,
                bits: AudioConstants.DefaultBits,
                channels: AudioConstants.DefaultChannels));

            _writers.Add(id, writer);

            return writer;
        }

        /// <summary>
        /// Finalises the wav writing and returns a list of all the files created
        /// </summary>
        /// <returns>System.String.</returns>
        public async Task<string> Finalise()
        {
            //drain the un-processed buffers on this object
            while (Buffer.Count > 0)
            {
                await Task.Delay(200);
            }

            var archiveFile = Path.Combine(Path.GetTempPath(), BotConstants.DefaultOutputFolder, _settings.MediaFolder, _processorId, $"{Guid.NewGuid()}.zip");

            try
            {
                using (var stream = File.OpenWrite(archiveFile))
                {
                    using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Create))
                    {
                        // drain all the writers
                        foreach (var writer in _writers.Values)
                        {
                            var localFiles = new List<string>();
                            var localArchive = archive; //protect the closure below
                            var localFileName = writer.Filename;
                            localFiles.Add(writer.Filename);
                            await writer.FlushAsync();
                            writer.Dispose();

                            // Is Resampling and/or mono to stereo conversion required?
                            if (_settings.AudioSettings.WavSettings != null)
                            {
                                // The resampling is required
                                localFiles.Add(AudioFileUtils.ResampleAudio(localFileName, _settings.AudioSettings.WavSettings, _settings.IsStereo));
                            }
                            else if (_settings.IsStereo) // Is Stereo audio required?
                            {
                                // Convert mono WAV to stereo
                                localFiles.Add(AudioFileUtils.ConvertToStereo(localFileName));
                            }

                            // Remove temporary saved local WAV file from the disk
                            foreach (var localFile in localFiles)
                            {
                                await Task.Run(() =>
                                {
                                    var fInfo = new FileInfo(localFile);
                                    localArchive.CreateEntryFromFile(localFile, fInfo.Name, CompressionLevel.Optimal);
                                    File.Delete(localFile);
                                }).ConfigureAwait(false);
                            }
                        }
                    }
                }
            }
            finally
            {
                await End();
            }

            return archiveFile;
        }
    }
}
