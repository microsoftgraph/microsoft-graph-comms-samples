// ***********************************************************************
// Assembly         : RecordingBot.Services
// Author           : dannygar
// Created          : 09-08-2020
//
// Last Modified By : dannygar
// Last Modified On : 09-09-2020
// ***********************************************************************
// <copyright file="AudioFileUtils.cs" company="Microsoft">
//     Copyright ©  2020
// </copyright>
// <summary></summary>
// ***********************************************************************
using System.IO;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using RecordingBot.Model.Constants;
using RecordingBot.Services.ServiceSetup;

namespace RecordingBot.Services.Util
{
    /// <summary>
    /// Class AudioFileUtils.
    /// </summary>
    public static class AudioFileUtils
    {
        private const string STEREO = "stereo";

        /// <summary>
        /// Creates the file path.
        /// </summary>
        /// <param name="rootFolder">The root folder.</param>
        /// <param name="fileName">Name of the file.</param>
        /// <returns>System.String.</returns>
        public static string CreateFilePath(string rootFolder, string fileName)
        {
            var path = Path.Combine(rootFolder, fileName);
            var fInfo = new FileInfo(path);
            if (fInfo.Directory != null && !fInfo.Directory.Exists)
            {
                fInfo.Directory.Create();
            }

            return path;
        }


        /// <summary>
        /// Converts to stereo.
        /// </summary>
        /// <param name="monoFilePath">The mono file path.</param>
        /// <returns>System.String.</returns>
        public static string ConvertToStereo(string monoFilePath)
        {
            var outputFilePath = monoFilePath.Substring(0, monoFilePath.Length - 4) + monoFilePath.Substring(monoFilePath.Length-4).Replace(".wav", $"-{STEREO}.wav");
            using (var inputReader = new AudioFileReader(monoFilePath))
            {
                // convert our mono ISampleProvider to stereo
                var stereo = new MonoToStereoSampleProvider(inputReader);

                // write the stereo audio out to a WAV file
                WaveFileWriter.CreateWaveFile16(outputFilePath, stereo);
            }

            return outputFilePath;
        }


        /// <summary>
        /// Resamples the audio.
        /// </summary>
        /// <param name="audioFilePath">The audio file path.</param>
        /// <param name="resamplerSettings">The resampler settings.</param>
        /// <param name="isStereo">The mono to stereo conversion indicator.</param>
        /// <returns>System.String.</returns>
        public static string ResampleAudio(string audioFilePath, WAVSettings resamplerSettings, bool isStereo)
        {
            var stereoFlag = (isStereo)? $"-{STEREO}" : "";
            var outFile = audioFilePath.Substring(0, audioFilePath.Length - 4) + audioFilePath.Substring(audioFilePath.Length - 4).Replace(".wav", $"-{resamplerSettings.SampleRate/1000}kHz{stereoFlag}.wav");
            using (var reader = new WaveFileReader(audioFilePath))
            {
                var outFormat = new WaveFormat((int)resamplerSettings.SampleRate, (isStereo)? 2 : 1);
                using (var resampler = new MediaFoundationResampler(reader, outFormat))
                {
                    resampler.ResamplerQuality = resamplerSettings.Quality * AudioConstants.HighestSamplingQualityLevel / 100 
                                                 ?? AudioConstants.HighestSamplingQualityLevel;
                    WaveFileWriter.CreateWaveFile(outFile, resampler);
                }
            }

            return outFile;
        }
    }
}
