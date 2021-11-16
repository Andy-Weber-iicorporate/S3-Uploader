using System;

namespace S3_Uploader.Editor.Extensions
{
    public static class Extensions
    {
        public static bool Between(this DateTime dt, DateTime start, DateTime end)
        {
            if (start < end) return dt >= start && dt <= end;
            return dt >= end && dt <= start;
        }
    }
}