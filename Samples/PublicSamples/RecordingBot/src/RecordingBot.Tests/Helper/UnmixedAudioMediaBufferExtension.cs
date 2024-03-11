using Microsoft.Skype.Bots.Media;
using System;
using System.Reflection;

namespace RecordingBot.Tests.Helper
{
    public static class UnmixedAudioMediaBufferExtension
    {
        public static UnmixedAudioBuffer Data(ref this UnmixedAudioBuffer uab, IntPtr data)
        {
            return SetAProp(ref uab, "Data", data);
        }

        public static UnmixedAudioBuffer ActiveSpeakerId(ref this UnmixedAudioBuffer uab, uint id)
        {
            return SetAProp(ref uab, "ActiveSpeakerId", id);
        }

        public static UnmixedAudioBuffer Length(ref this UnmixedAudioBuffer uab, long length)
        {
            return SetAProp(ref uab, "Length", length);
        }

        public static UnmixedAudioBuffer OriginalSenderTimestamp(ref this UnmixedAudioBuffer uab, long timestamp)
        {
            return SetAProp(ref uab, "OriginalSenderTimestamp", timestamp);
        }

        /**
         * Uses reflection to forcibly set the value of an UnmixedAudioBuffer struct property.
         * This is done because the struct doesn't define setters for any of its properties.
         */
        private static UnmixedAudioBuffer SetAProp(ref UnmixedAudioBuffer uab, string key, object value)
        {
            PropertyInfo propInfo = typeof(UnmixedAudioBuffer).GetProperty(key);
            object boxed = uab;
            propInfo.SetValue(boxed, value, null);
            uab = (UnmixedAudioBuffer)boxed;
            return uab;
        }
    }
}
